# StS2 Mock 测试模式（Mock Testing, EA build）— sts2-spire1 知识库

> 定位：开发方法卷（G 系列的补充）。StS2 引擎**自带 46 个 Mock 类**（分布在 12 个 `.Mocks`/`.Mock` 命名空间），配合 `TestMode.IsOn` 构成官方的无头测试基建。我们的移植工作已经在用同构手法（SplashOwnSetSubtractPatch 保留 mock 分支、`TestMode.IsOn` 跳过 visuals——`Creature.CreateVisuals`、`PlayCardAction.CancelAction` 均有 TestMode 分支）。本卷把这套模式固化为可复用的开发方法。
> 来源：`research/engine-dllsrc/`。置信度 **高**。

---

## T1 Mock 家族清单

**全家福（46 类）** — 出处 `find -path "*Mock*"`。置信度：**高**
```
Powers.Mocks 18（最多——power 行为最难做集成环境，mock 密度最高）
Encounters.Mocks 8 / Monsters.Mocks 6（战斗环境侧：MockAttackMonster、
  MockAttackAndSummonEncounter 等——用最小怪群驱动战斗循环）
Cards.Mocks（MockAttackCard/MockSkillCard/MockPowerCard/MockCurseCard/
  MockStatusCard/MockQuestCard/MockCardModel/MockTurnEndInHandRecorderCard）
Afflictions.Mocks 3（MockNoUnplayableAffliction/MockSelfDamageAffliction/MockUselessAffliction）
Orbs.Mock 2 / Enchantments.Mocks 1 / Events.Mocks 1 / Potions.Mocks 1 /
  PotionPools 1 / Saves.Test 2 / Map 2
```

## T2 Mock 设计模式（以 MockAttackCard 为样本）

**样本**：`Models.Cards.Mocks/MockAttackCard.cs` — 置信度：**高**
```
MockAttackCard : MockCardModel（基类再接 CardModel）
  可编程字段：_hitCount、_fromOsty、_targetingType（默认 AnyEnemy）
  CanonicalVars = [DamageVar(6,Move), OstyDamageVar(6,Move), BlockVar(0,Move)]
```
模式要点：
1. **Mock 即真实 CardModel 子类**——走全管线（钩子/命令/历史），不是替身对象；测试=构造最小真实语义的模型组合。
2. **可编程默认值**（6 伤/1 击/指定目标）让测试断言无需查表。
3. `TestMode.IsOn` 在引擎侧短路**表现层**（CreateVisuals 返回 null、CancelAction 跳过 UI 队列）——逻辑与视觉分离的闸门。
4. MockTurnEndInHandRecorderCard：把"回合尾自动打出"行为做成**记录器** mock——验证时序的探针模式（对应我方 coverage.js 的 `Playing (\S+)` 正则捕获）。

## T3 对本项目的落地方法

- SplashOwnSetSubtractPatch 的 `_mockGeneratedCard` 分支已是同构：**补丁必须保留 mock 分支**（引擎测试直接引用官方 Splash.OnPlay，删掉 mock 支持会破坏上游 TestMode 流程）。
- 新移植卡的验证策略分层：①Mock 层（BaseLib 测试 或 自建 Mock* 卡，断言 OnPlay 效果）→ ②控制台单卡验证（`card play`）→ ③autoslay 全局冒烟。①②无头可跑，③只兜底崩溃。
- 新建 Encounters/Monsters mock 的价值：给"跨池选牌/事件赠卡"类特性做**确定性战斗环境**（复刻 MockAttackAndSummonEncounter 的构成方式）。

## 开放问题

1. BaseLib 是否复用引擎 TestMode（还是自有 test 通道）未取证。
2. Mock 类在正式包里是否被裁剪（assembly 未裁——依赖 TestMode 运行时门）。
