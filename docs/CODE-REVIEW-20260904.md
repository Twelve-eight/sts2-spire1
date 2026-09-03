# CODE-REVIEW-20260904 — sts2-spire1 批判审阅(最终版)

> 审阅方式:两片由子代理完成(interop-patches、cards A-F),其余主会话单线完成;
> 子代理高危发现均经主会话独立复核。审阅中产出两个可复跑工具:
> `tools/audit-card-fidelity.mjs`(304 卡三方数值对比:一代 jar javap ↔ 引擎反编译 ↔ 我方实现)
> 与 `tools/audit-monster-hp.mjs`(66 怪 HP 对比)。
> HEAD 9453ebc。发现均带 文件:行 证据;不确定值标 UNCONFIRMED。

## 总评

代码库整体质量高于典型同规模 mod:命令 API 纪律、per-type Harmony 异常隔离、
字节码引注习惯、联机确定性意识都已成体系。本次审阅的**系统性发现**是:
(1)历史审计全部是"我方→引擎"单向去重,从未做过"一代→我方"覆盖审计,导致
约 35-40 张一代卡无实现且无人察觉,"M4 完成"断言因此失真;
(2)复用通道的"逐字段核对"断言被审计证伪(5 张卡与一代不同仍在役);
(3)生态侧(AFTP+Act4Heart)存在一个真实的玩家可见缺口:三幕 boss 零奖励。
以下按严重度排列。

---

## P0(2 项,均在互操作层,已复核成立)

### R2 [P0] Pandora 修复的池补丁落到基类 getter,变全局补丁
- `Interop/AutoAnthonyCompatBridge.cs:185-188` 对 `WatcherCardPool` patch
  `AllCards`/`AllCardIds`;工坊 Watcher 反编译 `.tmp/watchermod/WatcherMod/WatcherCardPool.cs:6`
  只有 `class WatcherCardPool : CardPoolModel`,**未重声明**这两个属性 → Harmony
  经基类解析,补丁实际钉在 `CardPoolModel.get_AllCards/get_AllCardIds`,对**全部卡池**生效。
- 两个方法(:212-253)均无 `__instance` 参数与类型守卫。混沌局激活时:每个池的
  AllCards 被换为混沌无色卡、AllCardIds 被并集污染 → 非混沌卡 `CardModel.Pool`
  反查失配 → `InvalidProgramException` 或池身份错配;并与 AA 同一基类 getter 双写,
  结果取决于加载顺序;prefix 跳过原方法还使 `_allCards` 缓存永不建立。
- 对照 AA 自身 `ColorlessPoolContentsPatch`(带 `__instance` + `is ColorlessCardPool` 分派)。
- **修复**:两方法加 `CardPoolModel __instance` 参数并以 `poolType.IsInstanceOfType(__instance)`
  守卫,或改 patch `WatcherCardPool.GenerateAllCards`(工坊池真实重写的成员)。
- 修复引入于 e40db70;PDB2805 冒烟未直接命中 Pandora,结构性验证不可能发现本问题。

### R3 [P0] 存档/历史 postfix 字典用错,工坊观者存档加载必抛
- `AutoAnthonyCompatBridge.cs:323`(FromSavePostfix)与 `:333`(FromHistoryPostfix):
  `.Where(e => TryMap(e, out _))` 过滤通过后 `.Select(e => (GeneratedCharacter)EntryMap[e!])`;
  "WATCHER" 只在 `ThirdPartyEntryMap`(:170 注册),回查 `EntryMap` 必抛
  KeyNotFoundException → 含观者条目的存档加载/历史页直接失败。
- **修复**:使用 TryMap 的 out 值,或按 EntryMap→ThirdPartyEntryMap 顺序取值。

---

## P1(6 项)

### R1 [P1] AFTP 生态:三幕 boss 零奖励(用户指定审查项;根因在生态,修复进本仓)
- 引擎 `RewardsSet.WithRewardsFromRoom`(`.tmp/dllsrc/.../RewardsSet.cs:88-91`):
  boss 房且 `CurrentActIndex >= Acts.Count-1` → 空奖励集;标准 boss 奖励
  (:238-241)= 金币 + 药水 roll + 3 卡。
- Act4Heart `Act4Hooks.FixAct3Boss_IL_`(.tmp/act4heart/.../Act4Hooks.cs:118-142)
  把三处 `Acts.Count-1` 改写成 `Count-2`(含发奖口)。MoveType 语义经反编译
  游戏自带 0Harmony.dll 内嵌 `MonoMod.Cil.MoveType{Before=0,AfterLabel=1,After=2}`
  实证:`(MoveType)2`=After,`val.Prev` 正是 `Ldc_I4_1`,改写精确。
- 其 `ModelDb.get_Acts` IL 钩子(:221-249)无条件追加 TheEnding 进全局幕表 →
  生态栈开局 `RunState.Acts.Count=4`(旁证:GeneralHooks.cs:60 钥匙门检查
  `state.Acts[CurrentActIndex+1] is TheEnding`)。
- 结果:三幕 boss(index 2)落 `2>=2` → **零奖励,与钥匙无关**;Act4Heart 全栈
  无补发钩子(grep 证实)。作者意图是让 GenerateRooms/Aubergine 侧把三幕按"非终幕"
  处理,同一改写套在发奖口上方向相反。
- **修复**(遵守 §0"生态补丁进本仓"指令):`RewardsSet.WithRewardsFromRoom` postfix——
  当 `room.RoomType==Boss && CurrentActIndex==Acts.Count-2 && Acts[CurrentActIndex+1]`
  是第四幕(Act4Heart 的或本仓 fallback 的 TheEnding)时,按 :238-241 原始构成补发
  金币+药水 roll+3 卡(私有成员经 AccessTools)。纯原版/AFTP-only 零影响;
  四幕 boss 不触发;逐玩家实例,MP 语义不变。待办:实机冒烟三态
  (有钥匙/无钥匙/纯三幕)。

### R5 [P1] 复用通道 5 张卡与一代不同仍被注入现役池("逐字段核对"断言被证伪)
`SharedCardReuse` 注入的引擎孪生中,五张经 jar↔引擎对比确认**不是**同名同数值:
| 卡 | StS1(jar 权威) | 引擎孪生 | 后果 |
|---|---|---|---|
| Claw | 升级 +2(升级伤害 3→5,增量 2) | 升级 Damage+1、Increase+1(4/+3) | 机器人池 Claw+ 数值错 |
| Barrage | 基伤 4 | 基伤 5 | 池内白嫖 +1 |
| Flechettes | 基伤 4 | 基伤 5 | 同上 |
| Chill | 升级加 Innate、保留 Exhaust(jar `isInnate=true`) | 升级**移除** Exhaust | 升级语义错 |
| Darkness | 升级加"触发全部暗球被动一次"(jar upgrade 仅换文案) | 升级触发**两次** | 升级语义错 |
- 根因:8781855"57 张官方孪生逐字段核对"实际只对带注释的前段执行了逐字段核对;
  尾段(Reboot/AllForOne/Barrage/Chill/Claw/CreativeAi/Darkness…)为批量追加。
  DEVELOP §7a 的陷阱清单(Defect writers 结论)是对的,两次修复波没有交叉引用它。
- **修复**:把 Claw/Barrage/Flechettes/Chill/Darkness 移出 Reuse 清单、恢复我方
  忠实类现役(我方 Claw/Chill/Darkness 类已在 Spire1LegacyPool,Barrage/Flechettes
  需确认我方类是否存在);或按表逐项核对引擎孪生后**有据**保留。

### R6 [P1] 一代→我方覆盖缺口 ~35-40 张卡,"M4 完成"断言失真
从未做过的审计方向。KB 全集(排除 deprecated)对比 我方类∪复用通道,按颜色:
- RED 67/75:**DoubleTap、Exhume、Hemokinesis、Offering、PerfectedStrike、SearingBlow 缺**。
- BLUE 70/75:**Amplify、Electrodynamics、LockOn 缺**。
- COLORLESS 26/58:缺 Apotheosis、Chrysalis、Discovery、HandOfGreed、Mayhem、
  Metamorphosis、Panacea、Panache、PanicButton、Purity、SadisticNature、
  SecretTechnique、SecretWeapon、TheBomb、ThinkingAhead、Transmutation、Violence、
  MasterOfStrategy、Magnetism、Enlightenment、Forethought、Impatience、
  JackOfAllTrades、DeepBreath、DramaticEntrance、MindBlast、Apparition 等
  (其中 ChooseCalm/ChooseWrath 由 Wish 合并实现、Void/Shiv 命名假缺口)。
- GREEN/PURPLE 名义缺基础打击/防御(改名实现,实际全覆盖)✓。
- 其中 9 张引擎已有同名卡(Hemokinesis、Offering、PerfectedStrike、Shiv、Slimed、
  Apotheosis、Discovery、HandOfGreed、TheBomb)→ 若字段一致应进 Reuse(§7a),
  若不一致应自建——现状两头都不占。
- 诅咒缺 CurseOfTheBell/Normality/Pride/Writhe(需先确认是否有授予来源,UNCONFIRMED)。
- **修复**:按清单补齐或以 DEVELOP.md 明文记录豁免;今后把覆盖矩阵纳入常规审计
  (工具已具备)。

### R7 [P1] 稀有度错标 10 张(玩家可见,共享无色池为主)
jar+KB 双源确认(与 cards A-F 代理发现合并):
- 无色池:BandageUp、Blind、Finesse、FlashOfSteel、**GoodInstincts、SwiftStrike、
  Trip**(后三张为本次审计新发现)——StS1 均为 Uncommon,实现均为 Common。
- 红卡:Brutality(Rare 实现 Uncommon)、**LimitBreak(Rare 实现 Uncommon,新发现)**。
- 根因:无色卡 StS2 无同名物、无历史审计方向覆盖;红卡两张系创作疏漏。
- **修复**:十处 `CardRarity.Common/Uncommon` 一行改动;跑一次审计工具确认归零。

### R8 [P1] 圣遗物层覆盖 25-29/180+,无复用机制、无豁免决策记录
- `Relics/` 29 类(含基类),无 SharedRelicReuse 等价物;四角色 RelicPool 只含自建类。
  一代圣遗物约 180+,每角色池 40+。对比 DEVELOP.md 的愿景("characters, cards,
  relics, powers, potions"),圣遗物/药水层是最大的未量化缺口;
  文档以"25 遗物"为成就口径,未见"有意缩减"决策记录。
- **修复**:要么开列豁免清单入 DEVELOP,要么规划补齐批次(复用机制先行:
  StS2 引擎同名词缀圣遗物如 Metallicize/Regenerate 类可先盘点)。

---

## P2(7 项)

- **R9 遭遇表缺口**:§7d 权威表 Act2 strong 的"3 Cultists"无实现
  (Encounters/ 无 ThreeCultists*);"Shelled Parasite and Fungi"未组队
  (ShelledParasiteEncounter 单怪)。其余各幕 weak/strong/elite/boss 与 §7d 一一对应 ✓。
- **R10 条件事件无出现条件**:§7c 标记为"run 条件门控"的事件(AccursedBlacksmith、
  Bonfire、Designer、Duplicator、FaceTrader、FountainOfCurseRemoval、Lab、Nloth、
  NoteForYourself、WeMeetAgain、WomanInBlue、KnowingSkull、TheJoust)在本仓全部
  为空 Acts(共享)或定幕(Act2),无 IsAllowed/条件出现逻辑——StS1 中它们受
  资源/持有物条件约束,现实现会过频出现。需逐事件核对条件后补门控
  (事件内部选项条件≠出现条件)。
- **R11 互操作补丁无异常隔离**(interop 代理 P2,复核成立):bridge 的六个
  prefix/postfix 与 FromSave/FromHistory 均裸奔;因挂在 `get_AllCards` 这类高频口,
  一次抛出可在任意引擎路径炸局。建议逐补丁 try/catch→日志→放行。
- **R12 Fission 整卡错译+绕过命令 API**(cards A-F 代理 P1,主会话维持 P2/P1 边界:
  玩家可见数值与文案全错,且 :32-37 手写 `queue.Remove`/`orb.RemoveInternal`):
  按 jar 语义重写(逐球 1 能量+抽 1;升级 Remove→Evoke),同步修 loc 与升级文案;
  若确无命令可用须在 DEVELOP 记录例外。
- **R13 Evaluate+ 塞入未升级 Insight**:`CombatState.CreateCard<T>` 不继承升级态
  (dllsrc 佐证),升级文案失真;需在创建后调用升级路径。
- **R14 interop 代理 P3 升 P2 一项**:`MpIgnoreModDiffPatch.cs:42-62` 联机握手哈希
  不符默认放行(config 默认 On)——二进制漂移兜底为零,建议哈希不符需独立二次开关。
- **R15 Burn 未实现 StS1 升级**(A-F 代理 P2):StS1 Burn 可被升级(+2/4 伤),
  `MaxUpgradeLevel=>0` 沿用 StS2 语义;至少入 DEVELOP 已知偏差清单。

## P3(择要)

- 基建:两件初始圣遗物与引擎同名(BurningBlood 逐行同机制;RingOfTheSnake 引擎走
  `ModifyHandDraw`、我方走 `BeforeCombatStart` 独立抽 2)——按 §7a 复用或在 doc comment
  写明差异字段;Spire1LegacyPool 注释与 Strike/Defend 变体仍挂活跃池矛盾;
  DEVELOP §2c 配置表与 Spire1Config 实际(PureSts1Pools/DebugShowLocKeys/
  IgnoreMpModDifferences/EnableSkipNodeButton)漂移。
- 卡牌:7 张自建同名卡缺 §7a 差异字段 doc comment(BiasedCognition/Defragment/
  DemonForm/Expertise/Finesse/FlashOfSteel/Fusion);FTL/Finisher 计数口径 UNCONFIRMED
  待 jar;选项卡 type Power→Skill 为已注释适配(影响"打出 Power"统计);Burst 多
  Exhaust(legacy);Discipline 标题字面 "DEPRECATED Discipline" 玩家可见且弃用原因
  无记载;Dualcast target Self 与同类 None 口径不一。
- 互操作:RestSite finalizer 返回类型应为 `Exception?`;AssemblyLoad 事件内 Harmony
  打点且失败后永不摘除;MaxFloor 转译器单模式无失败告警;ThirdPartyLocFix 每次
  切语言重合并;ArchivedCharacterGatePatch.cs:65 空遍历死代码。
- 数据:§7c 权威清单自身漏了 SecretPortal(真实一代三幕事件,已实现且带 FLAG:
  传送分支未实现,仅 [Leave],有充分引擎 API 论证)——"权威清单"也要有复核机制。

## 明确未发现(负结论同样重要)

- 怪物 HP:66 只全部与 jar 一致(36 自动通过,其余形态差异人工核实;
  SpireGrowth/WrithingMass 两个"不匹配"为解析噪声);Ascension 分支普遍正确。
- 卡牌数值:304 张我方实现卡的 cost/伤害/格挡/magic/升级 delta 经三方审计,
  除上列稀有度外**零数值偏差**;A-F 代理逐行精读 140 张仅报 1 张整卡错译。
- 无 RNG/wall-clock/静态可变状态类联机分歧源(A-F/G-Z 扫描 + interop 代理全量)。
- Harmony 22 个补丁目标全部存在于当前引擎版本,签名/ref/返回语义正确
  (interop 代理对照反编译逐一核过)。
- 事件幕门控:Act1/Act2/Act3 名单与 §7c 完全一致(Sssserpent 即 Liars Game,
  命名差异);六大神龛全为共享 ✓。
- 保留(retain)家族(SandsOfTime/WindmillStrike/Perseverance/Meditate)语义与
  jar 一致;Recursion 与 StS1 RedoAction 逐指令等价(含未知球种防御)。

---

# 第二部分:反思(应 2026-09-04 用户指令)

## 1. 本次工作过程的自省

- **子代理连环死亡的教训**:9 并发 → 全灭;误读"omp 配置是路由来源"后错误诊断;
  用户以"agentrouter 后台无子代理流量"证伪我的推断;最终 3 并发仍被平台
  "user concurrency limit exceeded" 拒绝,单发才稳定。**修正路径**:证据优先
  (后台流量是硬证据),诊断假设必须可证伪;约束下的正确形态是
  "单子代理跑深度切片 + 主会话并行做独立维度"(R1 即主会话成果,不受并发限制)。
- **误诊根因**:我把本机另一个工具(omp)的配置当成了自己的运行时事实。
  工具边界(我是 ZCode,不是 omp)应当是诊断前的第一道检查。
- **脚本化审计的收益超预期**:卡牌三方对比 5 轮迭代后把 304 张卡压到 0 数值偏差
  (暴露 10 张稀有度错 + 5 张复用错),而此前三轮人工 critic 均未发现——
  **人工精读适合机制语义,数值保真必须机械化**(两个方向不可互替)。
- **审计方向偏科**:历史所有卡牌审计都是 mod→StS2(去重/覆盖 drain),
  StS1→mod(保真/覆盖)方向只有 A-F 代理的一次人工扫描。
  覆盖审计(R6/R8)是第一个做这个方向的,立刻挖出 ~40 卡 + 155 遗物缺口。
- **主会话并发限制下的产量**:R1(生态奖励链路,含 MoveType 实证)、R6(覆盖矩阵)、
  R7(稀有度)、怪物 HP 审计、遭遇/事件门控矩阵,全部产自主会话——
  说明"子代理不可用"不等于"审阅不可行",串行+工具化即可推进。

## 2. DEVLOG/文档断言复核(本次证伪与确认)

| 断言 | 出处 | 复核结果 |
|---|---|---|
| "57 张官方孪生逐字段核对" | kb/defect-pool-case.md(8781855) | **证伪**:5 张与一代不同(R5) |
| "M4 content essentially complete" | DEVELOP §0/§1 | **失真**:R6 覆盖缺口 ~40 卡;R8 遗物层未量化 |
| "counts corrected 2026-08-30: 306 cards" | DEVELOP §0 | 文件数属实;但"306"不等于"一代全集" |
| "bridge path runtime verification CLOSED" | DEVLOG session 26 | 形式成立,但 PDB2805 未直接命中 Pandora;R2 正是该路径上的真 bug——**"closed" 口径过强** |
| "spike slime caps / Louse 权重等" | reverify-values-20260826 | 抽查成立(M2/M3/M5 结论与我方 jar 数据一致) |
| "怪物状态机死锁已修未回退" | session 10/26 | 成立(66 怪 HP+状态机结构核过) |
| "Harmony 全部签名正确" | session 26 CodeQualityCritic | 本次复核仍成立,但"correct"口径未覆盖 R2 的**钉错目标**问题——签名对 ≠ 目标对 |
| "event act gating 权威清单" | DEVELOP §7c | Act1/2/3 名单成立;**清单自身漏 SecretPortal** |

规律:**"已核对/已闭环"类断言若无可复跑工具支撑,半衰期约一个修复波**。
8781855 的复用波推翻了前人结论但没留复核工具;本次 tools/audit-*.mjs 补上了
这个缺环,今后断言应绑定"复跑命令"而非"某日某会话声称"。

## 3. 理论沉淀(入库 research/kb/review-theory-20260904.md)

1. **跨版本孪生漂移律**:同名内容在两个引擎间的差异集中于"升级路径"(upgrade
   delta/升级增删关键词),而非基础数值——基础数在移植时逐字段抄写容易,升级
   delta 是第二个思维通道,最易被跳过。本次 5 张复用错卡全部错在升级通道。
2. **验证不对称律**:"我方→官方"验证(去重、泄漏检查)与"官方→我方"验证
   (保真、覆盖)是两个正交方向;只做一个方向会产生"自洽但残缺"的库。
   覆盖矩阵(全集→实现)是成本最低、发现率最高的缺失方向。
3. **补丁目标解析语义**:Harmony 对"目标类型上不存在的成员"沿继承链解析,
   prefix/postfix 落点可以是基类——**补丁的作用域等于解析结果的声明域**,
   不写 `__instance` 守卫的自定义池补丁必然全局化。守卫是自定义内容补丁的
   必备件,不是可选优化。
4. **生态栈的"终幕条件"耦合**:StS2 引擎以 `Acts.Count-1` 表达"这是最后一幕",
   第四幕 mod 依赖改写该常量;凡引用同一表达式的消费点(发奖/地图/房间生成/
   遗物调整)都会被同一 IL 改写波及——**以 Count 比较表达的语义不能局部改写**,
   每个消费点都要独立审视方向性(R1:发奖口方向恰好相反)。
5. **断言半衰期律**(见上表规律):无工具绑定的验证断言按修复波衰减;
   报告/DEVLOG 中的"已验证"必须附带复跑入口。
6. **代理舰队并发约束**:平台并发上限是硬约束而非瞬态故障;正确的编排是
   "深度切片=单子代理,横向维度=主会话并行",而非重试并发。

## 4. 后续行动建议(优先序)

1. 修 R2/R3(互操作 P0)→ 构建部署 → R1 的 postfix 补丁 → 三态冒烟。
2. R7 十行稀有度修复 + 审计工具回归归零;R5 五张卡出池决策。
3. R6/R8 覆盖批次:先做"引擎已有孪生"的 9 卡核对(工具现成),再排自建批次。
4. R9/R10 遭遇与事件门控小批补齐;R14 联机哈希开关拆分。
5. 把 `tools/audit-card-fidelity.mjs --scope=all` 与 `tools/audit-monster-hp.mjs`
   纳入每次内容批次后的标准回归(替换"逐字段核对"口头断言)。
