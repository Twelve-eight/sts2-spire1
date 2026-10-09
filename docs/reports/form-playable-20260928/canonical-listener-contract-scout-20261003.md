# canonical listener 契约只读审查

范围: `mod\Spire1Code\Cards\Spire1Card.cs`、`Config\Spire1Config.cs`、`Run\FormNativeSmokeRunner.cs`,以及权威反编译 `.tmp\dllsrc` 与 `research\_decomp`。
证据类型: 仅源码/反编译静态阅读与只读命令。未构建, 未运行游戏, 未修改产品代码。

## 已确认

1. [P0 风险, 源码层已确认] `Spire1DeckGrantGuard` 的订阅会返回 canonical `Spire1.Spire1Code.Cards.Strike` 实例, 且该实例会进入 `IRunState.IterateHookListeners` 的返回值。
   - 触发条件: `Spire1Config` 静态构造执行 `EnsureSubscribed()` (`mod\Spire1Code\Config\Spire1Config.cs:28-31`), 随后任一 run hook 枚举监听器时, `ResolveRepresentatives()` 调用 `ModelDb.Card<Strike>()` 并 `yield return representative` (`mod\Spire1Code\Cards\Spire1Card.cs:213-220`、`367-394`)。
   - 权威契约: `RunState.IterateHookListeners(ICombatState?)` 在遍历 run 自带 card/relic/potion/modifier/badge/scaling 之后, 无条件追加 `ModHelper.IterateAllRunStateSubscribers(this)` 的结果 (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Runs\RunState.cs:545-596`, 特别 584-587)。
   - 当前控制流: 订阅回调 `_ => ResolveRepresentatives()` 与 `RunHookSubscriptionDelegate` 的期望一致; 因此 canonical `Strike` 是合法 listener 元素, 不是仅注册 id。
   - 可复现命令: `rg -n "ResolveRepresentatives|ModelDb.Card<Strike>|IterateAllRunStateSubscribers" "G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\Spire1Card.cs" "G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Runs\RunState.cs"`。
   - 最小修复范围: 暂无。此条本身不是缺陷, 是后续两条契约结论的前提。
   - 尚缺的实机证据: 未在该 run 中打印 `IterateHookListeners` 的实际元素类型与 `IsCanonical` 值; 本结论仅证明源码路径成立。


2. [P0 缺陷, 源码层已确认] canonical `Spire1.Spire1Code.Cards.Strike` 作为 run hook listener 进入 `Hook.AfterDeath` 时, `HookPlayerChoiceContext.GetOwner` 会读取 canonical `CardModel.Owner`, 该 getter 调用 `AssertMutable()` 并对非 mutable 实例抛出 `CanonicalModelException`。异常发生在 `GetOwner` 内, 不是回调内部, 因而会中断整个 `AfterDeath` 枚举。
   - 触发条件: 任一 `Hook.AfterDeath` 调用且 canonical listener 已解析。`Hook.AfterDeath` 对 `runState.IterateHookListeners(combatState)` 的每个 `model` 新建 `HookPlayerChoiceContext(model, ...)` (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:462-475`, 特别 469-472`); 该构造函数调用 `Owner = GetOwner(Source, combatState)` (`...\MegaCrit.Sts2.Core.GameActions.Multiplayer\HookPlayerChoiceContext.cs:88-95`)。
   - 权威契约: `GetOwner` 先判断 `source is CardModel`, 是则直接读取 `cardModel.Owner` (`HookPlayerChoiceContext.cs:97-106`); `CardModel.Owner` getter 第一步是 `AssertMutable()` (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs:334-340`); `AssertMutable()` 在 `!IsMutable` 时抛 `CanonicalModelException` (`...\MegaCrit.Sts2.Core.Models\AbstractModel.cs:122-128`); `IsCanonical => !IsMutable` (`AbstractModel.cs:33-35`)。`CardModel.Owner` 文档本身写明 canonical card model 上为 null (`CardModel.cs:327-333`), 但实现先抛异常再返回 null。
   - 当前控制流: `ResolveRepresentatives()` 返回 `ModelDb.Card<Strike>()` (`mod\Spire1Code\Cards\Spire1Card.cs:367-394`), 这是 canonical 实例; `RunState.IterateHookListeners` 把它追加进枚举 (`RunState.cs:584-587`)。因此任何真实 `AfterDeath` 枚举一旦到达该 listener 就抛 `CanonicalModelException`, 与死亡来源无关。
   - 可复现命令: `powershell -NoProfile -Command "$p='G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\CardModel.cs'; (Get-Content -LiteralPath $p)[326..349]"`; 以及读取 `HookPlayerChoiceContext.cs:88-106`、`Hook.cs:462-475`。
   - 最小修复范围: 不再订阅 canonical `CardModel`; 用一个非 `CardModel` 的 run-hook singleton/model 承载 `ShouldAddToDeck(CardModel card)` 边界。该类必须不覆盖 `AfterDeath` 的 owner 需求, 且 `ShouldAddToDeck` 只对 `card is Spire1Card && Spire1CardsGateSnapshot.Closed` 返回 false。若坚持订阅 CardModel, 必须改为 mutable clone 并绑定真实 `Owner`, 这会引入每个 run 的实例生命周期与 owner 来源问题, 不是最小方案。
   - 尚缺的实机证据: 现有实机日志只显示 canonical 异常; 尚未在死亡路径单独打印 `GetOwner` 的抛出行与 listener 顺序。

3. [P1 风险, 源码层已确认] 即使绕过 `AfterDeath`, canonical `Strike` 作为 listener 对 `ShouldAddToDeck` 的语义是成立的: `Hook.ShouldAddToDeck` 会对所有 listener 调用 `item.ShouldAddToDeck(card)`, 首个 false 立即返回并设置 `preventer`; `CardPileCmd.Add(..., PileType.Deck)` 在 false 分支调用 `preventer.AfterAddToDeckPrevented(card)`, 将 `result.success=false`, 且在所有结果都失败时提前返回, 不执行 `cardPile.AddInternal`。
   - 触发条件: cards gate closed, `LargeCapsule` 或其它路径走 `CardPileCmd.Add(card, PileType.Deck)`。
   - 权威契约: `Hook.ShouldAddToDeck` (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:2093-2105`); `CardPileCmd.Add` 的 deck 检查 (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Commands\CardPileCmd.cs:447-473`, 特别 453-463 与 470-472`); `CardPileCmd.Add` 拒绝无 owner 卡 (`CardPileCmd.cs:324-330`)。
   - 当前控制流: `Spire1Card.ShouldAddToDeck` 检查入参 `card is Spire1Card && Closed` 后返回 false (`mod\Spire1Code\Cards\Spire1Card.cs:137-144`)。canonical listener 只是承载该 override, 入参是新卡 clone 而不是 `this`, 所以判定不依赖 listener 自身的 owner。
   - 可复现命令: 读取上述三个行段, 或用 `Select-String` 在 `Hook.cs`/`CardPileCmd.cs` 定位 `ShouldAddToDeck` 与 `PileType.Deck`。
   - 最小修复范围: 与 P0 同一处订阅替换; `CardPileCmd`/`Hook` 行为不需要改。
   - 尚缺的实机证据: 尚缺 closed-gate `LargeCapsule` 在替换 listener 后的真实回合日志; 仅有源码契约。

4. [P1 风险, 源码层已确认] 订阅回调在静态构造阶段注册，`ModelDb.Card<Strike>()` 要到首次 hook 枚举时才惰性解析，因此 P0 的异常不会在注册时暴露，而是在运行中的首次 `AfterDeath` 等带 `HookPlayerChoiceContext` 的 hook 上暴露。
   - 触发条件: run 中首次出现需要 `IterateHookListeners` 且构造 `HookPlayerChoiceContext` 的 hook；`Hook.AfterDeath` 是已确认路径之一。
   - 权威契约: `ModHelper.SubscribeForRunStateHooks` 只存 delegate，不调用它 (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Modding\ModHelper.cs:100-113`); `IterateAllRunStateSubscribers` 在枚举时调用 `runHookSubscriber.del(runState)` (`ModHelper.cs:141-158`); guard 的 delegate 是 `_ => ResolveRepresentatives()` (`mod\Spire1Code\Cards\Spire1Card.cs:219`), `ResolveRepresentatives()` 内部才 `ModelDb.Card<Strike>()` (`Spire1Card.cs:367-394`)。
   - 当前控制流: 注册成功只能证明 delegate 存在，不能证明 canonical 实例可用；`IsSubscriberConfirmed` 只探测 subscriber id，不探测 `GetOwner` 安全性 (`Spire1Card.cs:183-197`、`255-303`)。
   - 可复现命令: 读取 `ModHelper.cs:100-158` 与 `Spire1Card.cs:199-253`、`367-394`。
   - 最小修复范围: 替换 listener 模型时，同时更新确认逻辑或在 singleton 上提供可探测的身份；不要让 `IsSubscriberConfirmed` 继续把 canonical `CardModel` 视为安全证明。
   - 尚缺的实机证据: 未运行游戏验证首次异常的实际调用栈。

5. [P0 最小修复可行性, 源码层已确认] 不使用 canonical `CardModel` 的最小边界方案存在: 订阅一个 `ModifierModel`/`SingletonModel`/`BadgeModel` 等非 owner-bearing 的 `AbstractModel`, 在该类上 override `ShouldAddToDeck(CardModel card)` 返回 `!(card is Spire1Card && Closed)`。
   - 触发条件: 该 listener 进入 `RunState.IterateHookListeners` 后, 所有走 `Hook.ShouldAddToDeck` 的 deck 添加都会被它审查。
   - 权威契约: `Hook.ShouldAddToDeck` 只要求 listener 是 `AbstractModel`, 调用虚方法 `ShouldAddToDeck` 并读取返回值 (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs:2093-2105`); `AbstractModel.ShouldAddToDeck` 默认 true (`...\MegaCrit.Sts2.Core.Models\AbstractModel.cs:2172-2180`); `GetOwner` 只对 CardModel/RelicModel/PotionModel/AfflictionModel/EnchantmentModel/PowerModel 解引用 owner, 其余返回 null 后回退到 `combatState?.Players.FirstOrDefault()` (`G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.GameActions.Multiplayer\HookPlayerChoiceContext.cs:97-106`)。
   - 当前控制流: `ModifierModel` 是引擎 run listener 集合中的正式成员 (`RunState.cs:573` 的 `list.AddRange(Modifiers)`), 且不是 owner-bearing 类型; canonical `ModifierModel` 也不会像 `CardModel.Owner` 那样在 `GetOwner` 中调用 `AssertMutable()`。因此它既能参与 `AfterDeath`/turn hook 枚举而不抛 canonical 异常, 又能承载 `ShouldAddToDeck` 拒绝。项目内已有真实子类 `FormStanceModifier` (`mod\Spire1Code\Forms\FormStanceModifier.cs:8`) 与 `Spire1ContentSnapshotModifier` (`mod\Spire1Code\Run\Spire1ContentSnapshotModifier.cs:17`)。
   - 可复现命令: 读取 `HookPlayerChoiceContext.cs:88-106`、`Hook.cs:2093-2105`、`RunState.cs:545-587`、`AbstractModel.cs:2172-2180`。
   - 最小修复范围: 新增一个最小 `CustomSingletonModel`/`ModifierModel` 子类承载 `ShouldAddToDeck`; 将 `Spire1DeckGrantGuard.SubscriptionId` 的 delegate 改为返回该实例; `Spire1Config` 静态构造不变。若用 `ModifierModel`, 必须确认不在 Good/Bad 池中出现 (Alignment None 或等价隔离); 若用 `CustomSingletonModel(HookType.Run)`, 它通过 BaseLib 自身订阅 `Id.Entry` 并返回 `[this]`, 不需要手写反射探针。
   - 尚缺的实机证据: 尚未在 test copy 上运行替换后的版本, 未观察 `AfterDeath` 与 closed-gate LargeCapsule grant 的真实日志。

6. [P1 风险, 源码层已确认] canonical `CardModel` 作为 run listener 还会在所有使用 `HookPlayerChoiceContext` 的 hook 上触发同类 `CanonicalModelException`, 不只是 `AfterDeath`。
   - 触发条件: 至少 `Hook.AfterDiedToDoom` (`Hook.cs:496-510` 的 505)、`BeforeFlush` (`544-561`)、`BeforeSideTurnStart` (`1155-1169` 的 1165)、`BeforeSideTurnEndVeryEarly/Early/...` (`1245-1301`) 都直接构造 `HookPlayerChoiceContext(model, ...)`。
   - 权威契约: 构造函数第三参数路径一律执行 `Owner = GetOwner(Source, combatState)` (`HookPlayerChoiceContext.cs:88-95`), 因而 canonical `CardModel` 的 `Owner` getter 必然被读取。
   - 当前控制流: 这些 hook 的 listener 集来自 `IterateCombatHookListeners` 或 `runState.IterateHookListeners(combatState)`, 都会包含 ModHelper run-state subscribers (`RunState.cs:584-587`; `Hook.cs` 的 `IterateCombatHookListeners` 会转发 run listeners)。
   - 可复现命令: 读取上述 `Hook.cs` 行段并检索 `new HookPlayerChoiceContext`。
   - 最小修复范围: 与第 5 条同一处 listener 替换。
   - 尚缺的实机证据: 未运行游戏逐一触发这些 hook。

7. [P2 风险, 源码层已确认] `Spire1DeckGrantGuard.IsSubscriberConfirmed` 的现有证明只检查 `ModHelper._runHookSubscribers` 中存在 id, 不检查 delegate 产出的 listener 是否 owner-safe 或是否仍为 canonical model。
   - 触发条件: 任何消费 `DeckGrantSubscriberConfirmed` 的生命周期 gate 在 canonical listener 已注册但会导致运行时抛异常时仍可能把它当成完整证明。
   - 权威契约: `TryProbeSubscriberPresent` 反射枚举 subscriber 列表并比较 `id` 字段 (`mod\Spire1Code\Cards\Spire1Card.cs:260-303`); `ResolveSubscribersField` 解析 `_runHookSubscribers` 或同形泛型静态集合 (`Spire1Card.cs:305-365`)。
   - 当前控制流: `Spire1LargeCapsuleGatePatch.DeckGrantSubscriberConfirmed` 直接委托该属性 (`mod\Spire1Code\Patches\Spire1LargeCapsuleGatePatch.cs:485-486`)。
   - 可复现命令: 读取 `Spire1Card.cs:183-365` 与 `Spire1LargeCapsuleGatePatch.cs:479-486`。
   - 最小修复范围: 若替换为 singleton listener, 把确认改为探测 id 后同时验证 delegate 返回的模型类型/非 canonical 状态; 或者移除这层反射证明, 改由 listener 自身的注册失败日志与生命周期 gate 判定。
   - 尚缺的实机证据: 未在运行中验证反射探针与 delegate 类型之间的漂移场景。

8. [P2 风险, 源码层已确认] 当前没有实机证据证明 `GetOwner` 拒绝是这次实机 canonical 异常的唯一来源, 只能确认它是源码层必然的 P0 路径之一。
   - 权威契约: `Hook.AfterDeath` 与其它 7 处 `new HookPlayerChoiceContext` 是确定的读取点; `CardModel.Owner` 的 `AssertMutable` 是确定的抛点。
   - 当前控制流: P0 结论足以支持在不修改产品代码前提下的修复建议, 但需要 test copy 实机日志才能把具体一次崩溃绑定到该行。
   - 可复现命令: 在 test copy 只读路径上检索既有日志中 `CanonicalModelException` 与 `HookPlayerChoiceContext` 的共现行号; 本次未执行游戏。
   - 最小修复范围: 不需要为确认结论再改产品代码; 实机验证应延后到允许运行游戏的一轮。
   - 尚缺的实机证据: 具体调用栈、触发 listener 顺序、当日 run 的 ModHelper subscriber 列表快照。
## 进行中

- [已完成] `Hook.ShouldAddToDeck` 与 `CardPileCmd.Add(..., PileType.Deck)` 的失败分支已核对; 结论见第 3 条。
- [已完成] `HookPlayerChoiceContext.GetOwner` 与 `Hook.AfterDeath` 上下文构造已核对; 结论见第 2 条。`GetOwner` 对 canonical CardModel 不是"拒绝", 而是在读取 Owner 时直接抛 `CanonicalModelException`。

## 未知

- 具体一次实机崩溃是否由此处触发 (需要允许运行游戏后的日志); 以及替换 listener 后 closed-gate 行为的实机确认。



