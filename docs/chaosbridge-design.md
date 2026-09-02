# Design: ChaosBridge (working title) — universal AutoAnthony character compat

> 用户需求(2026-09-02):"构想一个使任何自定义角色都被 AutoAnthony 自动兼容的 mod,
> 直接选项可选某角色全部映射到无色池就行。"
> 本文为设计稿(方案论证);实现按此文档走。

## 1. 问题陈述

Auto-Anthonyology(工坊 3786611028)按局随机生成卡池,但其识别面
`ChaosCharacterMapping.From(CharacterModel)` 硬编码五个引擎角色(`is Ironclad` 等)。
**任何 BaseLib 自定义角色 / 第三方 modded 角色**(Spire1 三角色、工坊观者、AFTP、
未来一切角色 mod)都不被识别 → `DeactivateRun()` → 随机池对该角色静默失效。

Spire1 仓内的桥接(AutoAnthonyCompatBridge)已为自家三角色 + 工坊观者解决;
本设计把同样的模式抽成**独立通用 mod**,服务整个 modded 角色生态。

## 2. 核心机制(从 Spire1 桥接提炼,全部经冒烟验证)

### 2.1 三层映射模型

一个"要吃随机池的角色"需要三个独立决策,不能混为一谈(Spire1 观者实践实锤):

| 层 | 决策 | 约束 |
|---|---|---|
| **激活载体**(activation carrier) | `From(Character) → GeneratedCharacter` 返回谁 | 不能返回 `Colorless`(NormalizeCharacters 剥掉它→激活链空转 Deactivate);**载体只管让激活/快照/MP 契约跑通**,用一个伪角色(Ironclad)即可 |
| **池身份**(pool identity) | 该角色 `CardPool` getter 指向哪 | 指向 `ColorlessCardPool` 即吃无色生成池(AA 的 ColorlessPoolContentsPatch 已做内容替换,`GetCards(Colorless)` 按需 Build、ActiveSeed 确定可复现);或指向任一 `ChaosXxxCardPool` |
| **起手策略**(starting deck) | 替换还是保留 | AA 的 `ReplaceStartingDeck` 语义按载体角色的 Basic 槽;若载体与池不同名(观者情形),保留原生起手最忠实 |

### 2.2 默认全自动策略(零配置可用)

对**每个**未被 AA 原生识别的角色,默认:
- 激活载体 = `Ironclad`(最稳:生成质量有保障,MP 快照路径最常被测)
- 池身份 = **无色池**(用户点名"全部映射到无色池就行";机制中立——通用操作,任何角色都能合法打出,观者冒烟 360 张混沌无色卡实证)
- 起手 = 保留原生(无 Basic 槽可伪造)

即:**装上即全角色生效,无需任何配置**。

### 2.3 识别面(哪些角色算"modded")

- 枚举 `ModelDb.AllCharacters`(启动期 + 每次进选人屏懒扫描)
- 排除五个引擎角色(AA 原生认得:Ironclad/Silent/Defect/Necrobinder/Regent)
- 排除已注册映射的角色(比如 Spire1 桥接已映射的——**双桥共存规则**:ChaosBridge 检测到 Spire1 桥接已用时让位,后装者不重复 patch 同一 getter;实现上 patch 前查目标 getter 是否已被 Harmony patch 过:`Harmony.GetPatchInfo(getter) != null` 则跳过并记日志)
- 对每个剩余角色:类型反射拿 `CardPool`/`StartingDeck` getter,prefix 池 getter,From postfix 兜映射

### 2.4 From 补丁(与 Spire1 桥同构)

Postfix `ChaosCharacterMapping.From` 三个重载:
- `From(CharacterModel)`:原 null 且入参 ∈ 已注册 modded 角色 → 返回激活载体
- `From(SerializableRun)`/`From(RunHistory)`:按 CharacterId.Entry 补条目(modded 角色 Entry = BaseLib 前缀型 `XXX-YYY` 或裸 `SLUG`——两者都按注册表字符串匹配,注册表在启动扫描时建立:character type → Id.Entry)

### 2.5 加载时序

无依赖边 → ModManager 拓扑序不保证在 AutoAnthony 之后:
- initializer 直探 AppDomain(同 Spire1 LoadHook)
- `AssemblyLoad` 事件兜底
- **顺序无关性**:ChaosBridge patch AA 的 `From`(AA 自己的 initializer 已把它挂上与否不影响我们 patch 静态方法本体);modded 角色的 getter patch 目标是各自 mod 的类,同样在 AssemblyLoad 到达时补挂——所以兜底监听需要盯**所有**后续装配的 mod 程序集,不只 AutoAnthony(对每个新装配集扫描一遍 AllCharacters 增量注册)

### 2.6 配置面(用户点名"直接选项可选某角色")

ModConfig(BaseLib `SimpleModConfig`)配置文件 `ChaosBridge.cfg`:

```
DefaultMapping = Colorless          # auto|off|Colorless|Ironclad|Silent|Defect|Necrobinder|Regent
PerCharacter:
  SPIRE1-IRONCLAD = auto             # 覆盖默认(示例:Spire1 已有自己的桥 → off/让位)
  WATCHER = Colorless
```

- `DefaultMapping=auto`(出厂):2.2 的全自动
- `off`:该角色(或全体)不参与,回原版池
- 具体池名:该角色池 getter 指向对应 `ChaosXxxCardPool`(想给某角色配红池的进阶玩家)
- 激活载体恒为 Ironclad(对用户隐藏;暴露只会引起"为什么不是紫池"的误会——AA 根本没有紫池)

### 2.7 多人语义

- 桥对 AA 的 MP 契约透明(同一 From 口;快照 host 权威、双端同 mod 集)
- 配置是**本地**的 → 属于卷七 C 档分歧源!对策(对 AA 快照模式无影响,因为池内容由
  ActiveSeed+快照决定,不读 ChaosBridge 配置;ChaosBridge 配置只影响"哪个角色被映射"):
  **双端配置不一致时,一端映射一端不映射 → 该角色激活集不同 → 分歧**。
  缓解:进 lobby 时本地校验(对每个 modded 角色比较映射存在性)并弹警告——引擎握手只对
  gameplay mod 清单,管不到 mod 内配置。文档明示:改配置需全队对齐(与 Act4Heart
  keys_enable 教训同族,P-12)。

## 3. 工程结构(独立 mod,非 Spire1 子模块)

```
chaosbridge/
├─ ChaosBridge.json        # id: ChaosBridge; dependencies: [BaseLib]
├─ ChaosBridge.csproj      # 条件引用 .tmp/interop-refs/AutoAnthony.dll(同 Spire1 模式)
├─ src/
│  ├─ Bootstrap.cs         # ModInitializer; 探测 AA + 装配集监听
│  ├─ Registry.cs          # modded 角色注册表(type/entry → 映射决策)
│  ├─ FromPostfix.cs       # ChaosCharacterMapping.From x3 postfix(反射挂载)
│  ├─ PoolPrefix.cs        # 角色池 getter prefix(反射挂载;Harmony.GetPatchInfo 让位检查)
│  ├─ Scanner.cs           # ModelDb.AllCharacters 增量扫描(排除引擎五角色+已映射)
│  └─ Config.cs            # SimpleModConfig: DefaultMapping + PerCharacter 覆盖
```

预估 ~400 行 + 配置。全部强类型引用 AA 公开 API(漂移炸构建),internal(ChaosCharacterMapping)
反射;cctor 无外部类型(int 折叠,Spire1 blocker 教训)。

## 4. 与 Spire1 桥的共存

Spire1 已内置自家桥(铁甲/猎手/缺陷 + 工坊观者)。ChaosBridge 装上后:
- Spire1 角色已被 Spire1 桥 patch(GetPatchInfo 非空)→ ChaosBridge 让位,日志记明
- 若用户在 ChaosBridge.cfg 给 Spire1 角色显式配了不同池 → ChaosBridge 日志警告
  "Spire1 自有桥优先;如需 ChaosBridge 接管,在 Spire1.cfg 关闭其 interop"(预留开关)
- 终态:Spire1 mod 可把自己的桥提取成对 ChaosBridge 的依赖声明(可选的未来整合)

## 5. 验证计划

1. 单元面:注册表扫描正确排除引擎角色/已 patch 角色(启动日志打印决策表)
2. 冒烟:auto 装机下,AutoSlayer 多种子扫,任一 modded 角色局查:
   - `ChaosBridge: <Char> -> carrier Ironclad, pool Colorless` 日志行
   - 混沌卡出牌数 > 0 且全为 CHAOS_COLORLESS
   - 原生起手保留
3. 配置面:PerCharacter=off 局,该角色原版池
4. MP:双端同配置,modded 角色局完整跑(快照路径)
5. 共存:Spire1 + ChaosBridge 双装,Spire1 三角色仍走自家映射(让位日志)

## 6. 风险登记

| 风险 | 缓解 |
|---|---|
| AA 版本更新改 From/池 API | 强类型引用使构建失败(强制重审计);反射挂载点集中两处 |
| modded 角色池 getter 非 virtual/不可 patch | patch 失败逐角色记 Error 不炸全局;文档列已知不兼容 |
| 观者类"特殊钩子"角色(姿态/预言 UI) | 无色池通用操作不含其专属机制,无 UI 依赖;AA 执行器对任何角色合法(卷七实锤) |
| 配置本地化 → MP 分歧 | §2.7:文档+lobby 警告(Act4Heart P-12 同族教训) |
| 与 Spire1 桥重复 patch | GetPatchInfo 让位检查 |
