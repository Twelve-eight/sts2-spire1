# stable-guard-api-scout-r15 review

范围: 只读引擎API审查. 不改代码/不构建/不测试/不运行游戏/不执行 git.
唯一契约: 已选 Forms 局在 Watcher 桥 Terminal/Shutdown 撤 patch 后必须有外层 fail-closed 入口.
模型/路由: global:deepseek-v4.1-flash / wb2api / xhigh (当前 harness 原生只读审查员).
边界: 以下全部为源码控制流证据, 非实机复现; 未运行游戏. 最多 3 项输出, 已在 5 分钟内收敛.

## 已确认

### R15-01 (P0) FormStanceModifier 确实存活于原生 listener 链, 且存在支付前 hook, 但不是 BeforeCardPlayed

- `FormStanceModifier` 继承链: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceModifier.cs` -> `BaseLib.Abstracts.CustomModifierModel` -> `MegaCrit.Sts2.Core.Models.ModifierModel` -> `AbstractModel`; `ModifierModel.ShouldReceiveCombatHooks => true` (`research\engine-dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs:28`).
- 存活证据 (与 Watcher patch 无关): `RunState.IterateHookListeners` 在 `childCombatState == null` 时 `list.AddRange(Modifiers)` (`research\engine-dllsrc\MegaCrit.Sts2.Core.Runs\RunState.cs:573`); 战斗内 `CombatState.IterateHookListeners` 也 `list.Add(Modifiers[n])` (`research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatState.cs:471-474`), 而 `CombatState` 由 `CombatRoom` 以 `runState?.Modifiers` 构造 (`research\engine-dllsrc\MegaCrit.Sts2.Core.Rooms\CombatRoom.cs:83`). 因此只要 `FormStanceModifier` 仍在该局的 `RunState.Modifiers` 中, Watcher patch 全撤后它仍被原生 hook 调用.
- `BeforeCardPlayed` 不早于支付: 手动出牌 `PlayCardAction.ExecuteAction` 先 `CanPlay` (`PlayCardAction.cs:85`), 再 `SpendResources()` (`:92`), 最后 `OnPlayWrapper` (`:103`); 而 `Hook.BeforeCardPlayed` 在 `CardModel.OnPlayWrapper` 内 (`CardModel.cs:1926`). 故 `AbstractModel.BeforeCardPlayed` (`AbstractModel.cs:468`) 在支付之后, 不能作为 fail-closed 支付前入口.
- 可用的支付前 native model hook: `public virtual bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)` (`AbstractModel.cs:2347`), 经 `Hook.ShouldPlay(ICombatState, CardModel, out AbstractModel?, AutoPlayType)` (`Hook.cs:2370`) 在 `CardModel.CanPlay` 调用 (`CardModel.cs:1746`), 位于 `SpendResources` 之前. 手动出牌路径: `PlayCardAction.cs:85` -> `CardModel.CanPlay` -> `Hook.ShouldPlay` -> `FormStanceModifier.ShouldPlay`. 自动出牌路径: `CardCmd.AutoPlay` 在 `:63` 先 `Hook.ShouldPlay`, 再于 `:122` `BeforeCardAutoPlayed`, 最后 `:130` `OnPlayWrapper`; 即 `ShouldPlay` 也是自动出牌最前的 model hook.
- 语义限制: `ShouldPlay` 返回 false 是"阻止/取消这张牌", 不是"显式失败中止本局"; 若要在已选 Forms 局且桥丢失时显式失败, 应在该 hook 中 throw (异常传播见 R15-02). 本项未改代码.

证据命令:
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\AbstractModel.cs' -Pattern 'ShouldPlay|BeforeCardPlayed'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Hooks\Hook.cs' -Pattern 'public static bool ShouldPlay'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs' -Pattern 'CanPlay|SpendResources|OnPlayWrapper'`

尚缺实机证据: 已选 Forms 局桥丢失后 `ShouldPlay` throw 的真实游戏表现未验证.

### R15-02 (P0) 异常传播: 出牌异常在交互模式被 ActionExecutor 吞掉后继续; 敌方回合异常杀死 turn loop

- 出牌侧精确链路: `PlayCardAction.ExecuteAction` (`PlayCardAction.cs:62`) -> `CanPlay` (`:85`) -> `Hook.ShouldPlay` (`Hook.cs:2370`) -> `FormStanceModifier.ShouldPlay`. 若在 `ShouldPlay` throw, 异常从 `CanPlay` 向上抛出, 不会到达 `SpendResources()` (`PlayCardAction.cs:92`) 与 `OnPlayWrapper` (`:103`), 所以支付与出牌副作用不会发生. 这是 fail-closed 的时序基础.
- `GameAction.Execute` 用 `_executionTask = TaskHelper.RunSafely(ExecuteAction());` (`GameAction.cs:125`), `TaskHelper.RunSafely` 记 Log/Sentry 后 **rethrow** (`TaskHelper.cs:26-41`); 随后 `await TaskHelper.WhenAny(_executionTask, _pauseForPlayerChoiceTaskSource.Task)` (`GameAction.cs:137`) 会重新观察该异常.
- 交互模式 (正常游戏): `ActionExecutor.ExecuteActions` 走 `Task actionTask = readyAction.Execute();` (`ActionExecutor.cs:151`), 循环等待完成后仅 `if (actionTask.IsFaulted) Log.Error(...)` (`:156-159`) -- **不 rethrow, 不 await, 不中止队列**, 随后继续 `GetReadyAction()` 处理下一个 action. 因此出牌侧 throw 的结果是: 该 action faulted, 被记为 Error, 战斗流程继续 (是否卡死取决于后续是否还有可推进的 action/回合). 这是"真实 action 被中止, 但异常被吞后继续", 不是可靠的本局终止.
- 非交互模式 (AutoSlay/测试): `await readyAction.Execute();` (`ActionExecutor.cs:143`) 在 `try` 内; 异常落入 `catch (Exception exception)` (`:182-186`) -> `_queueTaskCompletionSource.SetException(exception)` + `throw`, 队列以异常收束.
- 敌方回合/非 GameAction 路径: `CombatManager.ExecuteEnemyTurn` (`CombatManager.cs:1405-1438`) 由 turn loop `RunTurnLoopAfter` 的 `try` 包裹 (`CombatManager.cs:499-529`); 非取消异常落入 `catch (Exception ex2)` (`:516-529`), 记 `Log.Error("... the combat is stuck until the room is restarted")` + `StuckCombatException` + Sentry, 然后 rethrow -> turn loop 死亡, 战斗卡死直到房间重启. 若 fail-closed 入口挂在敌方回合 hook (见 R15-03), 这是"响亮且 fail-closed"的结果.
- 结论: 出牌侧 fail-closed 会阻止支付/出牌但异常被交互执行器吞掉; 敌方回合侧 fail-closed 会终止 turn loop 并使战斗卡死. 二者都不是"整局安全回菜单"; 尚缺实机证据确认游戏 UI 在两种路径下的真实表现.

证据命令:
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\ActionExecutor.cs' -Pattern 'Task actionTask|IsFaulted|Log.Error|await readyAction.Execute'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatManager.cs' -Pattern 'RunTurnLoopAfter|catch \(Exception ex2\)|stuck until the room is restarted'`

### R15-03 (P1) 单 modifier hook 不能覆盖全部路径; 最少核心 engine Harmony 目标只需补一个 PowerCmd.Remove

- 单 `FormStanceModifier` hook 无法覆盖全部三条路径, 因为原生变姿态 **退出** 走 `PowerCmd.Remove(PowerModel?)` (`PowerCmd.cs:291-299`): 它只调用 `power.RemoveInternal()` + `power.AfterRemoved(...)`, **不经过** `Hook.BeforePowerAmountChanged` / `Hook.AfterPowerAmountChanged` (对比 `PowerCmd.Apply` 在 `:124`/`:160` 与 `ModifyAmount` 在 `:231`/`:249` 会派发这两个 hook). 桥被撤后 Watcher 的 `RemoveAllStances` 仍走这条原生删除路径 (`watcher-WatcherCombatHelper-current.cs:573-578`), modifier 侧没有任何 engine-wide "power removed" listener hook 可以接管.
- 可用原生 model hook 覆盖 (无需新 owner, 无需 Watcher Assembly/MethodInfo/delegate):
  - 支付前出牌/自动出牌: `public virtual bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)` (`AbstractModel.cs:2347`).
  - 所有 live 伤害 (含非牌伤害/敌方攻击/毒): `public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)` (`AbstractModel.cs:608`), 派发点 `Hook.BeforeDamageReceived` (`Hook.cs:415-421`), 在 `CreatureCmd.Damage` 内位于 `Hook.ModifyDamage` (`CreatureCmd.cs:283`) 之后、`DamageBlockInternal`/`LoseHpInternal` (`:287`/`:293`) 之前 -- 此时 HP 尚未扣除, throw 可阻止本次伤害结算. 该路径不经过 UI 预览 (`DamageVar` 预览走 `Hook.ModifyDamage` 但不走 `CreatureCmd.Damage`), 因此不会误伤预览.
  - 原生变姿态进入/数值变化: `public virtual Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)` (`AbstractModel.cs:1057`), 派发点 `Hook.BeforePowerAmountChanged` (`Hook.cs:1020-1027`), 在 `PowerCmd.Apply` (`:124`) / `ModifyAmount` (`:231`) 内、`ApplyInternal`/`SetAmount` 之前.
  - 敌方回合边界: `public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)` (`AbstractModel.cs:1247`), 派发点 `Hook.BeforeSideTurnStart` (`Hook.cs:1156-1170`); 以及 `public virtual Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)` (`AbstractModel.cs:1354`), 派发点 `Hook.BeforeSideTurnEnd` (`Hook.cs:1244-1274`, VeryEarly 在 `:1255`). 这两者都在原生 Divinity 的 `AfterSideTurnEnd` 之前 (后者是更晚的 `Hook.AfterSideTurnEnd`, `Hook.cs:1279`; Watcher 的退出在 `watcher-divinity-current.cs:61-69`), 因此 `BeforeSideTurnEndVeryEarly` 是"原生 Divinity 自动退姿态之前"的可用 fail-closed 边界.
- 最少核心 engine Harmony 目标 (仅补上述唯一缺口): `MegaCrit.Sts2.Core.Commands.PowerCmd` 的 `public static async Task Remove(PowerModel? power)` (`research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291`), 前缀在"已选 Forms 局 + 桥不可用 + 被移除的是姿态 marker"时 throw. 判定已选局可用 `power.Owner.Player?.RunState?.Modifiers.Any(m => m is FormStanceModifier)` (只引用本项目类型); 判定姿态 marker 用 power 的 `Id.Entry` / 类型名字符串即可, **不需要持有 Watcher Assembly、MethodInfo 或 delegate**, 满足请求约束. 该 patch 是 engine 稳定公开签名, 不随 Watcher 程序集身份变化.
- 结论: 单 modifier hook 覆盖不了"原生变姿态退出"这一条; 最小方向是 modifier 原生 model hooks (出牌/伤害/变姿态进入/回合边界) + 一个 engine 级 `PowerCmd.Remove(PowerModel?)` Harmony prefix. 尚缺实机证据: `PowerCmd.Remove` prefix throw 在交互模式下的真实表现 (按 R15-02, 它不在 GameAction 内, 会向上传播到调用它的 Watcher 协程, 最终去向未验证).

证据命令:
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs' -Pattern 'public static async Task Remove|BeforePowerAmountChanged'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\CreatureCmd.cs' -Pattern 'Hook.ModifyDamage|BeforeDamageReceived|LoseHpInternal'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Models\AbstractModel.cs' -Pattern 'ShouldPlay|BeforeDamageReceived|BeforePowerAmountChanged|BeforeSideTurnStart|BeforeSideTurnEndVeryEarly'`

### R15-04 (P1) Shutdown 清理与存活 guard 的最小化处理

- 最小方向 (不需要新 owner, 不依赖 Harmony): fail-closed 入口放在 `FormStanceModifier` 自身的原生 model hooks 上. `FormStanceModifier` 是该局 `RunState.Modifiers` 的序列化成员 (`RunState.cs:296` 读档 `save.Modifiers.Select(ModifierModel.FromSerializable)`, `:344` 写入 `Modifiers`), 桥的 `Shutdown`/`EnterTerminalLocked` 只清 `_binding` 与撤 Watcher patch (R10 已查), **不会移除 `RunState.Modifiers`**. 因此桥撤 patch 后 modifier 仍存活, 下一次原生 hook 派发即可 fail-closed; 不需要存活新 owner, 也没有额外热卸载清理面.
- 唯一的 Harmony 目标 `PowerCmd.Remove(PowerModel?)` prefix 建议在 mod 初始化时一次性安装并保持进程生命周期, 与 Watcher 桥的 Shutdown/Unpatch **分离** (不同 HarmonyId, 不被桥的 rollback/Unpatch 触及), 因此不存在"Shutdown 完整清理 vs 存活 guard"的矛盾: guard 本身不参与桥的 Shutdown 清理. 不宣称热卸载.
- 若将来确实必须引入新存活 owner: 需要绑定一个真实非 ProcessExit 的关闭语义 (例如 RunState 结束/房间拆除事件) 并在 DEVLOG 记录 process 退出策略; 本审查未找到现成可用的非 ProcessExit 关闭点, 属于未知, 不作为结论.
- 未把"菜单 Bound / 三姿态首回合已通过"当作此面通过.

尚缺实机证据: 桥 Shutdown/EnterTerminalLocked 后下一次原生 hook 的真实触发点与 UI 表现; `PowerCmd.Remove` prefix 在真实游戏内的异常去向.

## 进行中

- 无. 已在 5 分钟内收敛为 3 个可执行项 (R15-02/03/04), 未继续扩大范围.

## 未知

- 未做任何实机/动态复现; 全部为源码控制流证据.
- 未验证出牌侧 faulted action 被 ActionExecutor 吞掉后战斗是否最终卡死或可继续.
- 未验证 `PowerCmd.Remove` prefix throw 在 Watcher 协程/敌方回合中的真实传播与 UI 表现.
- 未验证游戏是否存在真实的非 ProcessExit `MainFile.Shutdown()` 调用点 (R10 已列为未知).
- 未扩 UI/多人/性能; 未验证 r5 S-03..S-07.
