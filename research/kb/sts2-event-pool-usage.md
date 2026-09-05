# StS2 事件/修正器的用池普查（Event & Modifier Pool Usage, EA build）— sts2-spire1 知识库

## 本卷范围
`Models.Events/` + `Models.Modifiers/` 中全部 15 个触碰卡池的文件逐一建档：赠卡入口（`CardFactory.CreateForReward` + `CardCreationOptions` builder）、池来源分类、稀有度/谓词/旗标、RNG 流。把 `pool-architecture.md` I2b（池归属契约的消费面）与 I9（隐式数量要求）落到具体调用点。
来源：`research/engine-dllsrc/`。清单以 `grep -rln "CreateForReward|GetDistinctForCombat|GetForCombat|CardPool"` 为权威（一个 Node walk 版本漏报 7 文件——方法卷 M19 教训）。置信度 **高**=源码直接可证。

---

## 1. 赠卡通用 API

**E01 CreateForReward + CardCreationOptions** — 出处各调用点（下表）。置信度：**高**
```
options = CardCreationOptions.ForNonCombatWithDefaultOdds(poolList[, predicate])
                        | ForNonCombatWithUniformOdds(poolList[, predicate])
                        | new CardCreationOptions(poolList, source, CardRarityOddsType.*)
          .WithFlags(NoRarityModification | NoCardPoolModifications | NoUpgradeRoll
                     | ForceRarityOddsChange | IsCardReward)
result = CardFactory.CreateForReward(owner, count, options)
```
- `poolList` 多为**单元素列表**（本卷所有 vanilla 调用点都是单池）——赠卡语义是"从指定池抽"，不是跨池并集。
- `UniformOdds`（均匀稀有度）常配 `NoRarityModification`；`predicate` 做类型/稀有度过滤。
- `NoCardPoolModifications` 旗（BrainLeech/InfestedAutomaton/TheFutureOfPotions/AllStar）：跳过遗物类对池内容的修改（如 CharacterCards 的商人池钩子）——**旗的默认值决定 I2b 消费面是否生效**。
- 数量契约（I9）：`count` 大于池内 eligible 数即异常（RoomFullOfCheese 8 Common 事故）。

## 2. 事件（7 个）

**E02 池来源分类** — 出处各文件（行号见源码，重导出漂移以方法名定位）。置信度：**高**

| 事件 | 池来源 | Odds/谓词 | 数量 |
|---|---|---|---|
| BrainLeech | `ColorlessCardPool`（选择赠）+ `owner.Character.CardPool`（默认 odds） | DefaultOdds | 选择数 |
| EndlessConveyor | `ColorlessCardPool` | DefaultOdds | 1 |
| InfestedAutomaton | `owner.Character.CardPool` ×2 处 | DefaultOdds + Type==Power 谓词 | 1 |
| RoomFullOfCheese | `owner.Character.CardPool` | **UniformOdds + Common 谓词** | **8**（I2c 事故源） |
| TheFutureOfPotions | `owner.Character.CardPool` | UniformOdds + 药水映射类型/稀有度 | 按药水 |
| Trial | `owner.Character.CardPool` | DefaultOdds | 3（CardReward） |
| ColorfulPhilosophers | **硬编码 5 池色序数组**（Necrobinder/Ironclad/…） | — | — |

要点：**绝大多数事件用 `owner.Character.CardPool`（自己的池）**，无跨池并集——与卡牌普查（`sts2-cross-pool-cards.md` C01，Splash 唯一跨池）互证：**vanilla 的赠卡体系从不做"全角色池并集"**；跨池需求由调用方自行列池或集合运算。ColorfulPhilosophers 的硬编码色序是 I2a（注册表可见性）在事件层的又一实例——新角色不在数组里就被它无视。

## 3. 修正器（8 个）

**E03 Modifiers 的池消费** — 出处同 E02 方法。置信度：**高**

| 修正器 | 池来源 | 备注 |
|---|---|---|
| AllStar | `ColorlessCardPool` | UniformOdds + 全部旗标 |
| BigGameHunter | `player.Character.CardPool`（经 WithCardPools 重设） | 读写 NoCardPoolModifications 旗 |
| CharacterCards | `player.Character.CardPool` | **`ModifyMerchantCardPool` 钩子实现者**——商人池可被按角色改写（池修改的消费面） |
| CursedRun | `CurseCardPool.GetUnlockedCards` + `Rng.Niche` | 诅咒池 + 独立流 |
| Draft / SealedDeck | `player.Character.CardPool` | RegularEncounter odds；SealedDeck **30 张**（大量消耗池） |
| Insanity / Specialized | `player.Character.CardPool` | UniformOdds |

**E04 RNG 分账（事件/修正器侧）** — 出处各文件。置信度：**高**
`owner.PlayerRng.Rewards`（EndlessConveyor 药水）、`RunState.Rng.Niche`（CursedRun）、事件本地 `base.Rng`（Trial）、`Rng.Chaotic`（Trial 编号）、卡生成固定 `Rng.CombatCardGeneration`（战斗内，`sts2-cross-pool-cards.md` C05）。移植时逐条对号，禁止混流。

## 4. 移植规则补充（在 `sts2-cross-pool-cards.md` C06 之上）

**E05** — 置信度：**高**
1. 事件/修正器赠卡一律走 CreateForReward + 单池 options；**不要**自造"多池并集"（vanilla 无此先例，需要跨池时用卡面明确的两段选择——Splash 模式）。
2. 新事件对自家角色池提数量/谓词要求时，先跑容量契约（P5/I2c），并把要求写进事件规格。
3. `NoCardPoolModifications` 旗决定遗物/修正器钩子是否生效——移植"赠卡事件"时按原版旗标逐位对照。
4. ColorfulPhilosophers 类"硬编码池数组"在 mod 生态下必须改为注册表驱动（或经 ChaosBridge 类桥接补齐）——硬编码 = I2a 事故预定。

## 5. 开放问题 / 低置信项

1. `CardCreationOptions` 的完整旗标位与默认 odds 数值表（RegularEncounter 等三类的具体概率）未逐字段取证。
2. `CardFactory.CreateForReward` 内层循环的 black-list 语义（去重 + 耗尽抛异常）来自事故反推（DEVLOG 战果 #6），未逐行复读源码。
3. `ModifyMerchantCardPool` 钩子的全部实现者（CharacterCards 之外）未穷举。
