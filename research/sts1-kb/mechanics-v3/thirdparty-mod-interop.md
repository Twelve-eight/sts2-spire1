# StS2 多人同步机制卷七——第三方 mod 桥接与联机契约(2026-09-01 拆解)

> 生成:2026-09-01。来源:AutoAnthony 0.2.217(工坊 3786611028)与 Act4Heart 1.1.7(工坊 3747537811)反编译源逐类精读 + 实机分歧取证(.tmp/divergence-1647/)。
> 卷四-卷六回答引擎原生机制;本卷回答"**第三方内容 mod 怎么和引擎同步面、和其它 mod 交互**"——激活链、池替换面、快照契约、本地配置分歧源、Spire1 桥接实现。
> 拆解产物:`.tmp/autoanthony/`(765 文件)、`.tmp/act4heart/`(46 文件)。所有结论标注源文件。

---

## 0. 核心结论(先读)

1. **内容 mod 的多人同步 = 引擎同步面 + mod 自建契约两层**。AutoAnthony 的随机卡池完全复用引擎 ModelDb/池/ActionQueue(卡牌本体零自定义网络面),只自建了"host 权威池快照"一条契约;Act4Heart 则几乎全部依赖**每端本地重放**(配置+种子派生),后者是分歧温床。
2. **AutoAnthony 激活链单一入口**:`ChaosCharacterMapping.From(CharacterModel)` 类型检查五引擎角色——不认识的角色(所有 BaseLib 自定义角色)静默 `DeactivateRun()`。桥接 = Postfix 三个 From 重载,单点修全链(单人/多人/存档/历史)。
3. **池替换是全局的,起手替换是 per-character 的**——两个语义不能搞混(见 §2.3,Spire1 桥接曾因此返工)。
4. **Act4Heart 冒火精英是教科书级结构性分歧源**:地图标记走本地配置门控的本地钩子,不跨网;与卷四"合法分歧面"清单同族但**非法**(双端配置漂移→checksum 分歧)。
5. **mod 间加载顺序无保证**(拓扑序只认 manifest dependencies):桥接层必须 AssemblyLoad 事件兜底。
6. **编译期引用第三方 dll 是安全且推荐的桥接模式**(Private=false + 运行时同简单名解析),API 漂移会让构建当场失败——比静默漂移好。

---

## 1. Auto-Anthonyology(AutoAnthony 0.2.217)全架构

### 1.1 模型层:514 张空壳卡 + 每局注入定义

- **514 个 `ChaosCardModel` 子类**(每角色 52:Ironclad 92=10 基本+20 常+35 罕+25 稀+2 古、Silent 94、Defect/Necrobinder/Regent 92、Colorless 52)——静态注册,`ChaosModelDbReadyPatch`(ModelDb.InitIds Postfix)永久入库。
- **效果全靠 `ChaosCardDefinition` 驱动**:`ChaosRunDefinitions.ForSlot(Character, Slot)` 按激活期生成的定义(种子+模式)供 `ChaosCardModel.OnPlay/Type/Rarity/TargetType/Pool/Title/Description` 消费。**卡牌类本身无状态**——这是它能"每局全换"而不动 ModelDb 的核心。
- 存档面:`ExtraDamage/ExtraBlock/ResolvedSpecialXValue` 是 `[SavedProperty]`;定义本体经 `ChaosPoolSnapshotModifier`(一个 ModifierModel!)随 run 序列化。

### 1.2 激活链(单一入口,四条路径全经此口)

```
NGame.StartNewSingleplayerRun ──Prefix──> ChaosCharacterMapping.From(CharacterModel) ─┐
NGame.StartNewMultiplayerRun  ──Prefix──> (逐 player) From(player.character)          ├─ null? → DeactivateRun()+放行
RunState.FromSerializable     ──Prefix──> From(SerializableRun)(按 CharacterId)      ┤
RunHistory 页                  ────────> From(RunHistory)(按 ModelId)                ┘
```

- `From` 全部**类型检查**(`is Ironclad` 等)或**字符串比对**(`CharacterId == ModelDb.Character<Ironclad>().Id`)。BaseLib 自定义角色(前缀 ID)永不命中。
- 单人激活:`SeedBeforeSingleplayerPatch` → `ChaosRunDefinitions.ActivateAsync(character, seed)`(异步生成,带进度 overlay,失败回退原池)→ 调原始 StartNewSingleplayerRun(`_callingOriginal` 重入门)。
- **多人激活**(SeedBeforeMultiplayerPatch):host 在 lobby 期生成池,经 `ChaosPoolSnapshotModifier` 挂进 run modifiers 传给 client;client `ActivateFromSave` 恢复,`RegeneratedCards != 0` 直接抛(`The authoritative multiplayer pool snapshot required N regenerated cards`)——**host 权威快照契约,拒绝端上重生成**。旧版本兼容路径:deterministic peer generation(同 seed 双端各自生成)。
- 多人 host 生成期:`MultiplayerGenerationModePatch`/`MultiplayerGenerationMarkerTransportPatch` 把 marker 塞进 lobby 传输,run 创建时 reattach。

### 1.3 池替换面(全局语义!)

- **引擎角色的 `CardPool` getter 被 Prefix 补丁替换**(IroncladPoolPatch 等 5 个):`CharacterPoolPatchRouting.ReplacePool<T>(ref __result)` → `IsRunActive` 时 `ModelDb.CardPool<ChaosXxxCardPool>()`。**只查 IsRunActive,不查本局角色是谁**——因为 PrismaticGem/ColorfulPhilosophers/UnlockState.CharacterCardPools/Kaleidoscope/Splash 等全池枚举者会把所有角色的池拉进奖励/商店候选。
- **NormalizeCapturedPools**(CardCreationOptions.GetPossibleCards Prefix):把捕获的旧引擎池重绑到 Chaos 池——**IL 实锤是干净 isinst 五连,miss 原样返回**(`((ChaosRegentCardPool)(object)pool)` 是 ilspy 伪影,勿信)。
- **起手替换是 per-character**:`ReplaceStartingDeck(character)` 查 `IsCharacterRunActive(character) && ActiveReplaceStartingCards`,产 `ChaosCardRegistry.Canonical(character, slot) × BasicCountFor`。
- **ColorlessCardPool 内容也被替换**(ColorlessPoolContentsPatch):Chaos 无色 52 张(+PreserveOriginal 模式拼官方)。
- 卡牌总览过滤(ChaosCardLibraryPoolPatch,NCardLibrary._Ready Postfix)纯 UI。

### 1.4 多人一致性契约边界

- 随机卡**打出效果**走引擎 ActionQueue/CardPlayAction——零自定义网络面,与原生卡同权。
- 唯一自建契约 = 池快照(见 1.2)。快照指纹校验+拒绝重生成。
- **已知自身缺口**(其启动自审计在 mod 池共存时报错):`Expected 65 complete v111 Colorless cards, found 73`——它按官方裸数校验 Colorless 池,Spire1/AFTP 等注入无色卡即触发(Error 日志,不阻塞)。

### 1.5 与 Spire1 的桥接(已实现,commit f680a2a+496ad54)

- Postfix 三个 `From` 重载:原 null 且入参是 SPIRE1 角色 → 补 GeneratedCharacter 映射(Ironclad/Silent/Defect;Watcher 归档不参与)。原非 null 绝不干涉。
- Prefix 我方三角色 `CardPool` getter:`IsRunActive` → Chaos 池(**全局语义,与引擎角色一致**——初版误加 IsCharacterRunActive 门控,会导致棱镜类漏出一代卡,已修)。
- Prefix 我方三角色 `StartingDeck` getter:per-character + `ActiveReplaceStartingCards` → `Canonical × BasicCountFor`(同构 ReplaceStartingDeck)。
- 加载顺序:AutoAnthonyLoadHook(AssemblyLoad 事件兜底 + initializer 直试)。
- 编译期引用:`.tmp/interop-refs/AutoAnthony.dll`(gitignore)+ `SPIRE1_AUTOANTHONY` 条件符号;缺席产空壳。internal 类(ChaosCharacterMapping)经 `Type.GetType` 反射取方法再 Harmony patch;公开 API(ChaosRunDefinitions/ChaosCardRegistry/Chaos*CardPool)强类型引用。
- **多人**:桥接对 AutoAnthony 自身契约透明(全经同一 From 口);双端装双 mod;同名角色(引擎 Ironclad+SPIRE1 Ironclad)共享一个生成池(NormalizeCharacters 按枚举去重)。

---

## 2. Act4Heart 1.1.7 联机契约与分歧源

### 2.1 冒火精英(Super Elite)完整机制

- **标记**:`GreenKeyHooks.ModifyGeneratedMapLate`(每端本地)→ `MarkSuperElite`:`act_index ≤ 2` 且未全员持翡翠钥匙时,取本幕全部精英点(PointType==6),`new Rng(seed+act_index, "se_coord")` 选一个挂 `SuperEliteQuest`(本地 AbstractModel,**不跨网序列化**)。
- **进战 buff**:`BeforeCombatStart` → `DoSuperEliteBuff`:当前点是精英且 IsPointMarked → `new Rng(seed+CurrentActIndex+1, "se_buff").NextInt(0,4)` 四选一:力量/金属化(MetallicizePowerA4h)/再生(RegeneratePowerA4h)/最大生命+25%,对**全体敌人**施加。
- **数值**(dolso 配置):金属化 = act_mult×act + add = 2×act+2(act1=4... 实测 17 与高进阶/多幕一致)。
- **门控**:`ModMain.current_config.keys_enable`——**每端本地** dolso.act4_heart.config,ConfigSynchronizer 只做 host→client 广播(ConfigMessage)+ 版本号问询(ValidateConfigMessage,host 单方计数),**无双端一致性校验**。

### 2.2 分歧机理(2026-09-01 实锤复盘)

双端 keys_enable 不一致 → 地图钩子只在 host 端挂 quest → client 端 `IsPointMarked=false` 不加 buff → host 端怪物多 power → 第一个 checksum(战斗开始后 After player turn start)分歧,client 被踢。**进房前 client 看不到火特效**=最直观的前兆信号(用户观察即取证)。

### 2.3 结构教训:mod 挂地图/战斗钩子的三档安全级

| 档 | 模式 | 例子 | 联机安全 |
|---|---|---|---|
| A | 纯种子派生+双端同源(不读本地配置/本地状态) | 引擎地图拓扑、AutoAnthony se_buff 的 RNG 部分 | ✅ 天然一致 |
| B | 经网络同步面同步的 mod 状态 | AutoAnthony 池快照(ModifierModel)、AFTP 事件选项消息 | ✅ 契约内一致 |
| C | **本地配置门控的本地钩子,产物不跨网** | Act4Heart 冒火标记(keys_enable)、SecretPortal 墙钟门(卷五) | ❌ 配置/时钟漂移即分歧 |

写 mod 侧内容时:能升 B 不留 C;必须 C 时(用户本地开关),**mod 有义务在握手/进房时校验双端配置一致并阻断**,而不是静默分歧。

---

## 3. 桥接方法论(可复用 SOP)

1. **找单一入口**:内容 mod 的激活/识别几乎总有集中点(AutoAnthony=ChaosCharacterMapping.From)。Postfix 单点 < 逐调用方打补丁。
2. **尊重原语义再扩展**:原返回非 null 不干涉(引擎角色路径零扰动);替换语义(全局 vs per-character)逐字对齐原实现,必要时读 IL 消反编译伪影。
3. **无依赖探测**:manifest 不声明硬依赖(缺依赖=整 mod Failed);运行时 `AppDomain.GetAssemblies()` 探测 + AssemblyLoad 事件兜底加载顺序。
4. **编译期引用+条件符号**:HintPath 指向 gitignore 的本地副本,`Private=false`;`#if` 包住全部强类型代码,缺席产空壳——构建机无该 mod 也能出包。
5. **公开 API 强类型、internal 反射**:强类型让上游 API 漂移炸构建(强制重审计);internal 经 `Type.GetType("全名, 程序集名")`+GetMethods 反射,Harmony patch 签名仍须与目标严格一致(`ref GeneratedCharacter?` 等)。
6. **冒烟验证桥接生效**:看日志关键行(本例 `[Spire1] AutoAnthony bridge applied (N patch groups)` + `AutoAnthony bridge: <Char> -> <Pool>`),AutoSlayer 随机选角需多种子扫。

---

## 4. 证据锚点

### AutoAnthony(.tmp/autoanthony/)
- ChaosBootstrap.cs(ModInitializer,514 槽位声明,107 patch 类)
- ChaosRunDefinitions.cs(ActivateAsync/ActivateFromSave/BasicCountFor/NormalizeCharacters 去重/SupportedPools)
- ChaosCardRegistry.cs(Types/Canonical/IsGeneratedCardId)
- ChaosCharacterMapping.cs(三个 From 重载,类型检查+Id 比对)
- CharacterPoolPatchRouting.cs(ReplacePool 只查 IsRunActive;ReplaceStartingDeck per-character;NormalizeCapturedPools——IL isinst 五连实锤)
- SeedBeforeSingleplayerPatch/SeedBeforeMultiplayerPatch/SeedBeforeLoadPatch(四条激活路径)
- ChaosModelDbReadyPatch(ModelDb.InitIds Postfix 永久注册)
- ChaosPoolSnapshot/ChaosPoolSnapshotModifier(快照契约,ModifierModel 载体)
- ColorlessPoolContentsPatch/ChaosCardLibraryPoolPatch(无色池/总览)
- ColorfulPhilosophersChaosPoolPatch(全池枚举者的替代处理)

### Act4Heart(.tmp/act4heart/)
- GreenKeyHooks.cs(MarkSuperElite L72-119/DoSuperEliteBuff L139-203/IsPointMarked L32-46)
- Dolso/ConfigSynchronizer.cs(ConfigMessage 广播+ValidateConfigMessage 版本问询,无一致性校验)
- Dolso/ConfigReader.cs(本地 dolso.<snake>.config + FileSystemWatcher 热重载)
- Powers/MetallicizePowerA4h.cs(A4H 前缀来源)

### 引擎(engine-dllsrc/)
- ModManager.cs L193-317(拓扑排序)/L786-877(TryLoadMod:LoadFromAssemblyPath→initializer)
- UnlockState.cs L111(CharacterCardPools 虚分派)/ModelDb.cs(GetEntry/Slugify)
- CardCreationOptions.cs(WithCardPools 捕获面)
- ColorfulPhilosophers.cs/Splash.cs/Kaleidoscope.cs/PrismaticGem.cs/BigGameHunter.cs(全池枚举者)

### 取证
- .tmp/divergence-1647/state-divergence-report.txt(唯一 differ 字段:creatures[5].powers METALLICIZE_POWER_A4H:17 host-only;双端 mod 清单)
- research/audits/upgrade-text-diff-20260901.md 附录(分歧 RCA)
- mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs + AutoAnthonyLoadHook.cs(桥接实现)
