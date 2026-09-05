# 卡池架构与玩法语义不变量（Pool Architecture & Gameplay Invariants）— sts2-spire1 知识库

> 本卷回答"引擎怎么组织'谁拥有哪些卡'，以及哪些代码**依赖**这个组织方式而引擎并不强制"。这是两起实机事故（Splash 候选集、AutoAnthony 对第三方角色失效）的共同根因层，也是"冒烟测不出、只有读懂游戏的人才能发现"类 bug 的知识库解法。
> 来源：StS1 javap 字节码（desktop-1.0.jar v2.x）+ StS2 反编译 C#（engine-dllsrc）+ 本仓 DEVLOG 实录（修复 #10、Session 25/27、GA 修正 af6d1d7、ROOM_FULL_OF_CHEESE 契约）。每条不变量给出：**陈述 / vanilla 为何安全 / mod 如何打破 / 正确实现模式 / 检测手段（含冒烟为何测不出）**。
> 关联：`../sts1-kb/mechanics/card-rewards.md`（奖励管线）、`chaosbridge-design.md`、`engine-facts.md`。

---

## 1. 两代池架构

**I0a StS1** — 出处 `AbstractDungeon#initializeCardPools`（`../sts1-kb/mechanics/card-rewards.md` R01）。置信度：**高**
池 = `common/uncommon/rareCardPool`（按玩家颜色构建）+ `colorlessCardPool`（全体共享）+ `curseCardPool` + `srcUncommonCardPool` 备份。**角色→颜色的绑定是引擎约定**（Ironclad=红/Silent=绿/Defect=蓝/Watcher=紫），角色卡池覆写点 = `AbstractPlayer.getCardPool(list)`（虚方法）。
**I0b StS2** — 出处 `Models/CardPoolModel.cs`、`Models/ModelDb.cs`。置信度：**高**
`CardPoolModel` 抽象基类（`AllCards = GenerateAllCards() + ModHelper.ConcatModelsFromMods`，`AllCardIds` 是 HashSet 供 O(1) 归属查询）；`CharacterModel.CardPool` 为抽象属性（行 105）——**每角色必须给出一个池对象**。`ModelDb.AllCharacters` 在 vanilla 是**硬编码 5 元素数组**（Ironclad/Silent/Regent/Necrobinder/…行 145-150），第三方角色靠 Harmony patch（BaseLib 系）注入；`ModelDb.AllCards` = 全池并集 ∪ 全角色起始牌组（Distinct）。

**I0b+ 池内容注册通道（2026-09-05 补充，关闭本卷开放问题 3）** — 出处 `MegaCrit.Sts2.Core.Modding/ModHelper.cs#AddModelToPool/#ConcatModelsFromMods`。置信度：**高**
```
AddModelToPool(poolType, modelType): 登记进 _moddedContentForPools[poolType].modelsToAdd（List<Type>）
  若该池已 isFrozen → 抛 InvalidOperationException("too late! add content before the game is initialized")
ConcatModelsFromMods(poolModel, pool): 首次访问该池时置 isFrozen=true（冻结），随后
  pool.Concat(modelsToAdd 按 ModelDb.GetById 解析的实例)
```
⇒ 三条仲裁事实：①**注册时序契约**——AddModelToPool 必须发生在该池首次 `AllCards` 访问之前（与 chaosbridge 笔记"初始化期不能枚举 AllCharacters"同族：内容注册 vs 首次枚举的竞态）；②追加序 = 注册序，mod 卡排在官方卡之后（随机均匀性不受影响，顺序敏感消费者会感知）；③跨池重复卡在单池视角合法存在、本层不去重（I3 的机制根源；全局 Distinct 只在 ModelDb.AllCards）。BaseLib 的 `[Pool]` 属性 + ContentPatches（`GetCustomAttribute<PoolAttribute>() ?? throw`，Attribute 的 Inherited=true ⇒ 走基类链解析）只是该引擎通道的**发现层**——GA 事故即"链上解析到了错误归属"，池归属 lint（tools/pool-audit.mjs）必须复刻同样的继承链语义。

**I0c 引擎角色五件套（2026-09-05 补充）** — 出处 `Models.Characters/{Ironclad,Silent,Regent,Necrobinder,Defect}.cs`。置信度：**高**
每角色四绑定：`CardPool`（各自专属池）/ `PotionPool` / `StartingRelics`（各 1 枚：BurningBlood / RingOfTheSnake / DivineRight / BoundPhylactery / CrackedCore）/ `StartingDeck`（数组字面量：Ironclad 10、Silent 12、Regent 10、Necrobinder 10、Defect 10）。⇒ **官方 StS2 自带 Defect**——我方移植的"一代 Defect"与其同池同名卡碰撞是**结构性**的（SharedCardReuse/Splash 案的深层背景），任何按卡名匹配的兼容层都必须考虑官方 Defect 卡的存在。

**I2c-baseline StS2 官方池容量基线（2026-09-05，GenerateAllCards 数组直证）** — 出处 `Models.CardPools/*.cs`。置信度：**高**
```
角色池：Ironclad 90 / Silent 91 / Regent 91 / Necrobinder 91 / Defect 91
共享池：Colorless 65 / Curse 18 / Event 28 / Quest 4 / Deprived 13 / Deprecated 1
对照（我方 mod，tools/pool-audit.mjs 基线）：Spire1CardPool 39（8 直挂+31 孪生）、
  Spire1LegacyPool 87、官方四池引用 Defect 37/Silent 41/Watcher 78/Colorless 8/Curse 12
```
⇒ 容量契约（I2c ≥8 Common）在 vanilla 池上裕量巨大；事故只发生在自建小池（当年 Spire1 Ironclad Common=6）。新池/新角色的容量审计以本表为对照基线。

## 2. 不变量 I1：可调用集合 ≠ 池对象（Splash 事故）

**陈述**：任何"从其他角色选牌"类机制，候选集必须是**集合差**——`全体可获取卡牌 − 当前角色可调用集合`（按卡牌 Id 计算）——而不是"全体池对象列表减去 owner 的池对象"。
**vanilla 为何安全**：vanilla 中 `owner.CardPool` 恰好包含该角色全部可调用卡（共享卡全部在无色池、不在任何颜色池），"减去自己的池" ≡ "减去自己的可调用集合"。
**mod 如何打破**：我方移植层 `SharedCardReuse` 把官方卡（如超能光束 HyperBeam）注入移植角色的可调用集合，但这些卡**仍属官方池对象**。官方 SPLASH 的原实现 `list.Remove(owner.CardPool)` 只按对象排除 → 玩移植 Defect 时官方 Defect 池未被排除 → "其他角色"选出本角色已有的卡（DEVLOG 修复 #10，用户实机报告）。
**正确实现**：`SplashOwnSetSubtractPatch`——前缀重写 OnPlay，候选 = 全角色攻击牌集合 **按 Id.Entry 集合差** 移除持有者可调用集合；对原版角色零行为变化。
**检测**：静态——审计所有"池对象排除/包含"式代码（`Remove(...CardPool)`、`Where(p => p != owner.CardPool)` 类模式）；语义评审——任何"跨池取牌"特性必须在规格里写明集合运算定义。**冒烟为何测不出**：随机选择未必命中违规卡、选错了也不崩、只是"玩家已拥有的卡出现在不该出现的地方"——AutoSlay 只看崩溃/异常/覆盖，不做玩法语义判断。

## 3. 不变量 I2：池注册契约与引擎注册表可见性（AutoAnthony/GA 事故）

**陈述**：三条子契约，全部源自"引擎特性按注册表行走"：
- **I2a 角色注册**：`ModelDb.AllCharacters` vanilla 为硬编码数组；一切行走它的特性（AutoAnthony 的 `ChaosCharacterMapping.From` 类型门、稀有纪元发放等）**看不见**未被 patch 进去的第三方角色（工坊观者、Spire1 角色最初零随机牌，DEVLOG Session 25）。
- **I2b 卡归属**：每张卡类必须显式声明池归属（StS2 = `[Pool(typeof(XxxCardPool))]` 或继承链）；漏挂 = **静默继承父类池**（GeneticAlgorithm 漏挂 → 遗传算法变成红牌，af6d1d7 修正）。凡"枚举某池做随机/变换"的特性（AutoAnthony 打乱、Pandora 式变形、事件赠牌）都会因此漏掉或错收卡。
- **I2c 池容量契约**：事件/效果可能要求池内有 N 张不重复的某稀有度卡（ROOM_FULL_OF_CHEESE 要求 ≥8 张不重复 Common，否则 `CreateForReward` 抛异常崩溃）。新角色入池必须过容量契约（SharedCardReuse 的引入动因之一）。
**vanilla 为何安全**：官方角色全部按颜色规范建池、每池卡量充足、无第三方注册问题。
**mod 如何打破**：偷懒不建池（或复用官方池）→ 打乱/变形类 mod 对该角色整体失效或错收；漏挂 [Pool] → 卡牌错色错池；池太小 → 事件崩溃。
**正确实现**：新角色 checklist——①显式 [Pool] 声明（带数字类名注意扫描正则）；②Common≥8 容量契约（不足用 SharedCardReuse 补）；③第三方兼容层按 ChaosBridge 模式（池身份+池内容双重接管，见 chaosbridge-design.md）。
**检测**：静态 lint（本仓 `tools/pool-audit.mjs`：解析 Cards/*.cs 继承链与 [Pool] 缺失——GA 类 bug 的机械检测，注意带数字类名）；动态——`.tmp/night/coverage.js` 覆盖计算器；语义评审——任何新角色/新卡入池 PR 必须跑 lint + 容量契约核对。**冒烟为何测不出**：AutoAnthony 失效=什么都没发生（零异常零日志错误）；错色卡要打到那张卡且有人记得它该是什么颜色。

## 4. 不变量 I3：颜色池不相交与"无色=共享"假设

**陈述**：vanilla 隐含假设"颜色池两两不相交；全体共享的卡只在无色池"。
**mod 如何打破**：SharedCardReuse（官方卡同入两池）、chaos 池（池内容运行时整体替换）、移植卡（同名同效果跨池）。凡依赖"从池颜色推断卡独占性"的代码（卡背颜色、池语义文案、"其他角色"判定）都可能失真。
**检测**：跨池 cardID 重复报告（`.tmp/duplicate-cards-report.md` 模式）；任何新复用卡入池时更新该报告并复核 Splash 类消费者。
**状态**：StS1 侧已系统化（SplashOwnSetSubtractPatch 按 Id 差集后对重复免疫）；StS2 侧 `CardPoolModel.AllCardIds` 的 HashSet 结构天然支持差集——**移植新"跨池选牌"卡时直接用 Id 集合运算，勿复制池对象排除法**。

## 5. 开发模式提升空间（本卷的行动结论）

**G1 KB 优先的集合运算规格**：涉及"从池选牌/变形/赠卡"的特性，开发前必须在规格里写清：候选全集定义、排除集定义（按 Id 还是按池对象）、容量下限、第三方角色行为。无规格不开工。
**G2 语义评审门（asker 位）**：pool/registry 相关改动合入前，由评审角色对照本卷不变量清单逐条提问（I1 集合差？I2a/b/c 契约？I3 重复影响面？）——这是"agent 读 KB 发现问题"的具体化；机械冒烟在此类问题上零检出能力（两起事故均为用户实机揭示，DEVLOG 原话级证据）。
**G3 机械 lint 补位**：`tools/pool-audit.mjs`（[Pool] 归属缺失检测 + SharedCardReuse 孪生白名单交叉核对，2026-09-05 首跑基线：310 类全部可解析归属、0 孤儿；Spire1CardPool=8 直挂+31 孪生、Legacy=87、官方四池+无色/诅咒/状态/事件池各有归属）+ 既有 coverage.js（池容量/覆盖）构成静态防线；lint 产出并入 PR 检查。注意两个扫描坑（已入 research-methods.md）：类名/基类捕获须兼容 C# 主构造函数 `class Cleave() : Spire1Card(...)`；[Pool] 属性 Inherited=true——归属可来自基类链（GA 事故即基类链解析结果错了）。
**G4 冒烟的边界声明**：autoslay 类工具的检出域 = 崩溃/异常/资产缺失/覆盖增长；**玩法语义偏差（选错集合、对第三方失效、错色错池）不在其检出域**——排期时不得以"冒烟绿"作为此类特性的放行依据。

## 6. 开放问题 / 低置信项

1. 工坊观者未建紫色池的动机（偷懒 vs 色号耗尽顾虑）未与作者求证——chaosbridge 用"接管既有池"绕开而非回答该问题。
2. StS1 `getCardPool` 覆写点的全部 vanilla 调用方未穷举（奖励/商店/事件各一处已证）。
3. ~~ModHelper.ConcatModelsFromMods 语义~~ **已结案**（2026-09-05，I0b+）：追加序=注册序、首次访问即冻结、AddModelToPool 冻结后抛异常、本层不去重。置信度：**高**。
