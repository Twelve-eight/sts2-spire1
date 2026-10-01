# Form native smoke worker report

## 已确认

- 2026-10-01: 已确认当前白名单中的两个产品文件均不存在, 报告文件已存在且仅包含三段固定标题. `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs` 暴露 `GameStartupComplete`, `Instance` 和 `StartNewSingleplayerRun`; 现有研究在 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\runtime-scout.md:179-207` 记录了这些真实启动接口与 `shouldSave:false` 的约束. 当前尚未写入任何产品代码, 未构建, 未测试, 未部署, 未启动游戏.

## 进行中

- 正在核对真实 ModelDb, Watcher 桥接, encounter, 卡牌和退出路径, 以及两个白名单文件的现有调用约定.

## 未知

- 真实 Watcher 卡 ID, encounter 类型和当前二进制的所有 API 签名尚未在本轮源码核对中确认.

- 2026-10-01: 已确认真实运行链路的源码证据: `NGame.StartNewSingleplayerRun(CharacterModel, bool, IReadOnlyList<ActModel>, IReadOnlyList<ModifierModel>, string, GameMode, int, DateTimeOffset?)` 会创建真实 `RunState`, 调用 `RunManager.SetUpNewSingleplayer` 并继续 `StartRun`; `RunManager.EnterRoomDebug(RoomType, MapPointType, AbstractModel?, bool)` 会走真实房间退出、同步、创建和进入路径; `PlayCardAction(CardModel, Creature?)` 是真实出牌 action, 其执行会校验手牌并消费卡牌; `RunManager.ActionQueueSynchronizer.RequestEnqueue` 与 action 的 `CompletionTask` 是要求的排队/完成通道; `CardPileCmd.Add(CardModel, PileType, ...)` 是真实卡堆注入入口. 这些均来自 `research/engine-dllsrc` 对应源码, 尚未在游戏进程中验证.
- 2026-10-01: 已确认生产 `FormStanceWatcherBridge` 提供 `IsAvailable`, `TryBind()`, `CurrentKind(Player)` 和 `KindOfMarker(Type?)`; `FormStanceMode.IsSelected(Player?)` 根据真实 run modifier 判断选择状态. 生产桥接会从已加载的 `Watcher` 程序集绑定原生 marker, 本实现不复制 Watcher 类型或添加编译期 Watcher AssemblyRef.
- 2026-10-01: 已确认本轮载体必须使用真实 `ModelDb`/run API 和 `FormStanceModifier` modifier, 不得调用 `NGame.Quit()`, 不得走 `--autoslay`/`TestMode`/伪造 Hook; 代码实现仍未开始.

- 2026-10-01: Watcher 卡牌证据已确认: `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\WatcherVigilance.cs:13,25`、`WatcherEruption_P.cs:14,26`、`WatcherBlasphemy.cs:10,21` 分别定义真实 `WatcherMod.WatcherVigilance`、`WatcherMod.WatcherEruption_P`、`WatcherMod.WatcherBlasphemy`; `WatcherCardPool.cs:24-25,85` 将这些类型注册到真实 Watcher 卡池. 运行时实现只按 `ModelDb.AllCards` 的精确 `Id.Entry` (`WATCHER_VIGILANCE`, `WATCHER_ERUPTION_P`, `WATCHER_BLASPHEMY`) 查找, 找不到即 fail closed, 不复制这些类型.
- 2026-10-01: Watcher 角色的真实类型证据为 `G:\omp works\Sts\sts2-spire1\.tmp\watchermod\WatcherMod\Watcher.cs:1-3` 所属 `WatcherMod` 程序集以及 `ModelDb` 运行时模型集合; 实现将从当前已加载的 `ModelDb.AllCharacters` 按类型全名查找 `WatcherMod.Watcher`, 不引入编译期 Watcher 引用.
- 2026-10-01: Act/encounter/API 行号已确认: `MegaCrit.Sts2.Core.Models.ActModel.GetDefaultList()` 位于 `research/_decomp/game/sts2.decompiled.cs:71813-71838`; `ActModel.AllRegularEncounters` 和 `AllEliteEncounters` 位于同文件 `:71393-71398`; `ModelDb.AllCards` 与 `AllCharacters` 位于 `research/_decomp/monsters/MegaCrit.Sts2.Core.Models.ModelDb.decompiled.cs:80-96,127-135`; `NGame.StartNewSingleplayerRun` 位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:1137-1143`; `RunManager.EnterRoomDebug` 位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunManager.cs:1091-1158`; `CombatState.CreateCard` 位于 `research/_decomp/monsters/CombatState.txt:168-181`; `CardPileCmd.Add(CardModel, PileType, ...)` 位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.Commands/CardPileCmd.cs:324-344`; `ActionQueueSynchronizer.RequestEnqueue(GameAction)` 位于 `research/_decomp/game/sts2.decompiled.cs:171782-171799`.
- 2026-10-01: 故障和状态通道行号已确认: `TaskHelper.UnobservedFault` 与 `RunSafely` 位于 `research/_decomp/game/sts2.decompiled.cs:168788-168820`; `GameAction.CompletionTask` 和 `Exception` 位于同文件 `:169447-169450`; `CombatState.Enemies` 位于 `research/_decomp/monsters/CombatState.txt:63-66`; `RunManager.CleanUp(bool graceful)` 位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.Runs/RunManager.cs:1565-1598`. 载体将保留这些真实完成、异常、取消和清理通道.

## 进行中

- 2026-10-01: 已按白名单新增 `mod/Spire1Code/Patches/FormNativeSmokePatch.cs` 和 `mod/Spire1Code/Run/FormNativeSmokeRunner.cs`. Patch 仅挂接 `NGame._Ready`; runner 先精确解析 `--form-native-smoke`、`--form-native-smoke=calm|wrath|divinity`、冒号形式和三个场景后缀, 普通启动在解析后立即返回, 不创建 Task, 不订阅故障事件, 不写文件.
- 2026-10-01: 已实现真实链路: 等待 `GameStartupComplete`, 从当前 `ModelDb` 查找 `WatcherMod.Watcher`, 使用 `ActModel.GetDefaultList()` 和默认 Act 的真实 regular/elite encounter, 调用 `StartNewSingleplayerRun(shouldSave:false, GameMode.Custom, FormStanceModifier, fixed seed)`, 使用 `EnterRoomDebug(..., showTransition:false)`, 等待真实 combat/play phase, 通过 `CombatState.CreateCard`、`CardPileCmd.Add`、`PlayCardAction`、`ActionQueueSynchronizer.RequestEnqueue` 和 `CompletionTask` 完成真实卡牌执行.
- 2026-10-01: 已实现 `calm`、`wrath`、`divinity` 三个独立场景, 分别精确查找 `WATCHER_VIGILANCE`、`WATCHER_ERUPTION_P`、`WATCHER_BLASPHEMY`. 每个场景独立写 JSON 结果和失败原因; `SPIRE1_FORM_SMOKE_REPORT` 未设置时不写仓库, 设置时仅接受 `G:` 路径. 结果记录进入前/后 native stance、form carrier/effect 类型、`FormStanceMode.IsSelected`、bridge availability、energy、hand count、turn number、combat round、target HP/powers、action 状态/完成/取消/异常和 `TaskHelper.UnobservedFault`.
- 2026-10-01: 已实现一次性入口、显式超时、超时计时器取消、每场景结束 `RunManager.CleanUp(graceful:false)`, 最终仅调用当前进程 `SceneTree.Quit(exitCode)`. 未调用 `NGame.Quit()`, 未使用 `--autoslay`、`TestMode`、伪造 Hook 或直接应用 Form power.

## 未知

- 2026-10-01: 本轮只完成源码静态实现和回读, 未构建、未测试、未部署、未启动游戏; 因此真实二进制加载、真实 Watcher 角色/卡牌/encounter 绑定、真实 action 完成和 JSON 实机内容均待主会话集中验证.
- 2026-10-01: 未能在本轮静态阶段证明目标发行版的 `MapPointType`、`GameMode`、`PlayerTurnPhase` 等引用与当前研究源码完全同版; 实现使用研究源码中同名公开 API, 该边界必须由主会话构建验证, 不在此处宣称通过.

- 2026-10-01: 当前实现快照已复核并追加: 白名单产品文件 `mod/Spire1Code/Patches/FormNativeSmokePatch.cs:1-15` 与 `mod/Spire1Code/Run/FormNativeSmokeRunner.cs:1-502` 已落盘. Patch 在 `FormNativeSmokePatch.cs:7-14` 仅挂接 `NGame._Ready` 并调用 runner; runner 的普通启动 gate 位于 `FormNativeSmokeRunner.cs:41-54`, 只有命令行解析结果 `SmokeRequest.Requested == true` 才通过一次性 `Interlocked.Exchange` 创建异步入口. 精确支持 `--form-native-smoke`、三个场景后缀、`=`/`:` 场景形式的条件位于 `:56-116`; 无匹配参数在 `:43-47` 直接返回, 不创建 Task, 不订阅 `TaskHelper.UnobservedFault`, 不写 JSON.
- 2026-10-01: 当前关键实现行号: 启动完成等待 `:161-175`; bridge 绑定与 fail-closed `:177-195`; 当前 `ModelDb` Watcher、默认 Act、真实 encounter 查找及场景循环 `:198-227`、` :384-412`; 真实 run 创建、`shouldSave:false`、`GameMode.Custom`、`FormStanceModifier`、固定 seed 及 `EnterRoomDebug(..., showTransition:false)` `:250-285`; combat/play phase gate `:287-294`; 真实 `CombatState.CreateCard`、`CardPileCmd.Add`、`PlayCardAction`、`RequestEnqueue`、`CompletionTask` 和 action 异常/取消判定 `:296-337`; cleanup 与仅当前进程退出 `:345-363`、`:118-139`; 结果快照 `:414-452`; 显式超时 `:455-470`; G: 路径 JSON gate `:472-499`.
- 2026-10-01: 本快照的未验证边界已明确: 未构建、未测试、未部署、未启动游戏, 因而尚未证明目标发行版对 `NGame._Ready` Harmony 扫描、`ModelDb` Watcher 运行时绑定、`ActModel`/encounter mutable 类型、`PlayerTurnPhase`/`MapPointType`/`GameMode` 引用、`PileType.GetPile`、`CardModel.TargetType`、`GameAction.State` 以及各属性访问的实际签名和二进制兼容性. 尚未证明真实 Watcher 卡牌在当前战斗中可被 `CreateCard`/`Add` 后成功进入 `PlayCardAction`, 也未证明每个场景的 stance/effect/Power 字段和 `TaskHelper.UnobservedFault` 在实机上会按预期回传. 未执行任何游戏进程、Steam 安装或共享 `mod_configs` 操作; 路由记录继续按用户指定的 `6.1sol via agentrouter` 处理.

- 2026-10-01: 根据监督报告 `form-native-smoke-review-r12-20261001.md:7-70` 开始本轮 P1 修复计划. 仅修改两个原实现白名单文件: `mod/Spire1Code/Run/FormNativeSmokeRunner.cs` 和 `mod/Spire1Code/Patches/FormNativeSmokePatch.cs`; 不修改其它产品文件、构建产物、共享配置或安装目录, 不构建、不测试、不部署、不启动游戏. 计划按顺序修复: P1-1 将 run/combat/play 条件改为基于 `NGame.AwaitProcessFrame()` 的真正有界轮询; P1-2 用统一超时包装 `StartNewSingleplayerRun`、`EnterRoomDebug` 和 `CardPileCmd.Add`, 超时后观察遗留 task、执行 `RunManager.CleanUp` 并保证最终 `SceneTree.Quit`; P1-3 将 `PlayCardAction` 与 `CompletionTask` 证据提升到异常可见作用域, 在取消、超时和普通异常路径记录 State、CompletionTask outcome、cancelled、Exception 和 failure.
- 2026-10-01: 本轮已确认边界: `NGame.GameStartupComplete` 是发行版真实完成 task, 可继续使用; `NGame.AwaitProcessFrame()` 的研究源码证据位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes.GodotExtensions/NodeUtil.cs:16-30`, 因此轮询不使用 `Task.FromResult(condition)`. `StartNewSingleplayerRun`、`EnterRoomDebug`、`CardPileCmd.Add` 的发行版签名和现有 API 证据已记录在本报告既有 `## 已确认`; 这些 API 没有传入取消 token, 本轮只能采用超时观察、记录遗留 task、尝试真实 `GameAction.Cancel()` 及安全清理, 不声称能够取消底层 run/room/card task. `GameAction.Cancel()`、`CompletionTask`、`State`、`Exception` 的证据位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.GameActions/GameAction.cs:46-64,203-209`.

- 2026-10-01: P1-1 已修复. `FormNativeSmokeRunner.cs:281-310` 的 run/combat/play 条件现在通过 `WaitForConditionWithTimeoutAsync(NGame, Func<bool>, ...)` 逐帧检查真实状态; `:584-611` 使用 `NGame.AwaitProcessFrame()` 和 wall-clock deadline, 条件为假时不会提前返回, 超时抛出 `TimeoutException`. 已移除这些条件处的 `Task.FromResult(condition)` 伪等待. 未验证边界: 未启动游戏, 尚未证明目标发行版的 Godot process frame 恢复和三类真实状态实际时序.

- 2026-10-01: P1-2 已修复. `FormNativeSmokeRunner.cs:269-279` 为 `StartNewSingleplayerRun` 增加 `RunTimeoutSeconds`; `:292-299` 为 `EnterRoomDebug` 增加 `CombatTimeoutSeconds`; `:319-322` 为 `CardPileCmd.Add` 增加 `CardInjectionTimeoutSeconds`; 统一包装位于 `:613-634`, 超时会记录遗留 task 的观察 continuation 并抛出 `TimeoutException`, 外层 `:371-407` 进入清理, `RunAndQuitAsync` 仍在 `:118-139` 的 finally 调用当前进程 `SceneTree.Quit`. 发行版研究源码未发现这些 API 的取消 token, 因此未宣称能取消底层 task; 仅使用真实 `GameAction.Cancel()` 处理出牌 action, run/room/card task 以超时观察加 `RunManager.CleanUp(graceful:false)` 收束. 未验证边界: 未启动游戏, 尚未证明超时后底层 task 的最终完成时序不会在进程退出前重入.

- 2026-10-01: P1-3 已修复. `PlayCardAction` 和结果字典提升到 `FormNativeSmokeRunner.cs:257-258`; 结果创建在 `:413-426`, 证据收集在 `:455-471`, 包含 `state`, `completionTaskOutcome`, `cancelled`, `exception`, `failure` 和完成时间. `:331-349` 分别捕获 `OperationCanceledException`, `TimeoutException` 和普通异常; `:428-453` 在取消/超时路径尝试真实 `action.Cancel()` 并立即记录; 外层异常与 finally `:371-390` 仍把 action 证据写入 `cardPlay`, 因而不会只留下外层异常字符串. `Snapshot` 当前在 `:547-561` 分离记录真实 form carrier 和 form effect 类型. 未验证边界: 未构建、未测试、未启动游戏, 尚未产生真实取消、超时或 action.Exception JSON, `CompletionTask` 在 deferred cancel 后的最终状态仍需实机确认.

## 本轮计划与已确认边界

- 2026-10-01: 已读取二次监督报告 `form-native-smoke-review-r12-20261001.md`. 本轮只修复剩余一个 P1 和一个 P2, 只编辑两个原实现白名单产品文件与本唯一报告, 不构建、不测试、不启动游戏、不部署、不修改共享配置、Steam 安装或构建产物.
- 本轮计划一: 将 `StartNewSingleplayerRun`、`EnterRoomDebug`、`CardPileCmd.Add` 超时升级为终止性场景失败. 记录未取消事实和 detached task, 不继续 `RunAsync` 的后续场景; 在最终 `SceneTree.Quit` 前对遗留 task 做一次有界 drain, 超时仍未完成时记录明确的 `isolated-unfinished-operation` 隔离边界, 不声称底层 task 已取消.
- 本轮计划二: 增加 action 的不可逆本地失败锁存字段. 取消、超时、普通异常和取消请求失败都置为失败; `actionPassed` 同时要求失败锁存为假且 `cardPlay.failure` 为空, finally 继续记录最终 action evidence.
- 已确认边界: 发行版 `StartNewSingleplayerRun`、`EnterRoomDebug`、`CardPileCmd.Add` 的现有调用签名没有取消 token; 本轮不猜测或新增取消 API. `GameAction.Cancel()` 只用于真实 `PlayCardAction`, 不能据此声称 run、room 或 card injection task 已取消.
- 未验证边界: 本轮不触发真实超时, 未验证目标二进制中遗留 task 的最终完成时序、清理与退出之间的实际交互; 代码修改完成后仍不宣称构建、测试或实机通过.

## 本轮 P1 修复完成

- 2026-10-01: P1 异步重入已静态修复. `FormNativeSmokeRunner.cs:277-289,302-311,331-336` 将 `StartNewSingleplayerRun`、`EnterRoomDebug`、`CardPileCmd.Add` 通过 `AwaitOperationWithTimeoutAsync` 以 `terminalOnFailure:true` 包装, 并收集 `terminalOperations`.
- `FormNativeSmokeRunner.cs:390-399` 将上述操作的超时或失败转换为 `TerminalOperationException`, 写入 `terminalFailure`, `terminalOperation` 和 `terminalOperationOutcome`; `RunAsync` 在 `:229-233` 检测终止性失败并停止后续场景, 不再进入下一场景.
- `FormNativeSmokeRunner.cs:694-753` 明确记录底层 task 没有取消能力: 超时只保存真实 task 的 detached 观察对象, 不声称取消; `:772-814` 在 `RunScenarioAsync` 清理前执行一次共享十秒有界 drain, 完成、task-reported-canceled、faulted 或 `isolated-unfinished-operation` 均写入 `detachedOperations`, 并记录 `underlyingCancellationRequested:false` 和 `isolationBoundary`.
- `FormNativeSmokeRunner.cs:416-430` 在 drain 后执行真实 `RunManager.CleanUp(graceful:false)`; `RunAndQuitAsync` 的最终退出仍在 `:122-144` 通过当前进程 `game.GetTree().Quit(exitCode)` 完成.
- P1 未验证边界: 未触发真实 run、room 或 card injection 超时, 未验证目标二进制的遗留 task 是否在十秒 drain 内完成; `isolated-unfinished-operation` 只表示明确隔离边界, 不表示底层 task 已停止或已取消. 本轮未构建、未测试、未部署、未启动游戏.

## 本轮 P2 修复完成

- 2026-10-01: P2 action 结果一致性已静态修复. `FormNativeSmokeRunner.cs:265-266` 增加本地不可逆 `actionFailureLatched` 字段; `:352-365` 在取消、超时和普通异常路径先锁存失败, 再写 action evidence.
- `FormNativeSmokeRunner.cs:371-376` 的 `actionPassed` 现在同时要求 `!actionFailureLatched`、`actionResult["failure"] is null`、真实 `Finished`、无 `action.Exception`、`CompletionTask` 为 `RanToCompletion` 且没有新增 `TaskHelper.UnobservedFault`, 因此 action 发生 timeout、cancel 或 exception 后即使延迟变为 Finished 也不能恢复为 passed.
- `FormNativeSmokeRunner.cs:421-426,457-462` 在 finally 和初始结果中记录 `failureLatched`; `:493-510` 继续记录最终 `state`、`completionTaskOutcome`、`cancelled`、`exception`、`failure` 和完成时间. 外层异常在 `:402-410` 也会锁存 action 失败后再写最终证据.
- P2 未验证边界: 未构造真实 action timeout 后延迟完成、真实 cancel 或底层 exception 的运行时序, 未生成实机 JSON; 本轮未构建、未测试、未部署、未启动游戏.

## 本轮 P1 收束策略追加计划

- 2026-10-01: 根据最新监督要求, 调整 detached operation 收束顺序. `DrainDetachedOperationsAsync` 将返回是否所有遗留 task 在单次有界 drain 内达到终态; 仅达到终态时允许调用真实 `RunManager.CleanUp(graceful:false)`.
- 若 drain 超时仍为 `isolated-unfinished-operation`, 或 drain 自身失败, 写入 `cleanup: skipped`、终止性失败和隔离边界, 不调用 `RunManager.CleanUp`, 不进入下一场景, 依靠 `RunAndQuitAsync` finally 的当前进程 `SceneTree.Quit` 结束本次单场景 smoke.
- 不新增或猜测底层取消 API, 不声称 detached task 已取消. 本轮只修改原实现白名单文件和本唯一报告, 不构建、不测试、不启动游戏、不部署.

## 本轮 P1 收束顺序修复完成

- 2026-10-01: 已按最新监督要求调整 `FormNativeSmokeRunner.cs:416-452,457-480`. `RunScenarioAsync` 先执行 detached operation 的有界 drain, 由 `cleanupAllowed` 接收收束结果; drain 未在十秒边界内完成时写入 `cleanup: skipped`、`cleanupSkipped:true`、`cleanupSkipReason`、`terminalFailure:true` 和 `isolated-unfinished-operation`, 不调用 `RunManager.CleanUp`.
- `FormNativeSmokeRunner.cs:457-480` 只有 `cleanupAllowed` 为真时才调用真实 `RunManager.CleanUp(graceful:false)`; drain 失败也进入 cleanup skipped 分支. `FormNativeSmokeRunner.cs:229-233` 使终止性失败阻止后续场景, `RunAndQuitAsync` 的最终当前进程 `SceneTree.Quit` 路径保持不变.
- `FormNativeSmokeRunner.cs:807-861` 的 `DrainDetachedOperationsAsync` 以单个十秒 deadline 汇总所有 detached task 的终态; `completed`、`task-reported-canceled`、`faulted` 视为已收束, `isolated-unfinished-operation` 返回 false. 记录仍明确为 `underlyingCancellationRequested:false`, 不声称底层 task 已取消.
- 未验证边界: 未触发真实 detached task 超时或 drain 隔离, 未验证目标二进制在 cleanup skipped 后是否能按预期直接到达最终 `SceneTree.Quit`; 本轮未构建、未测试、未部署、未启动游戏.

## 第四轮主线程安全修复计划与已确认边界

- 2026-10-01: 已读取第三轮监督结论. 本轮只修复剩余 P1 主线程安全问题, 只编辑两个原实现白名单产品文件与本唯一报告, 不构建、不测试、不启动游戏、不部署、不修改其它产品文件、共享配置、Steam 安装或构建产物.
- 计划一: 先依据发行版 `NGame.IsMainThread()`、`NGame.AwaitProcessFrame()`、`Node.CallDeferred` 和现有 `GodotTreeExtensions` 安全模式建立主线程门禁; 不使用 `Task.Run` 作为线程切换, 不猜测未确认的 deferred completion API.
- 计划二: 将 `RunScenarioAsync` 中的 Godot 状态读取、真实 `RunManager.CleanUp` 和 detached drain 后的状态决策恢复到 Godot 主线程 gate; 非主线程只提交主线程工作并等待明确 completion, 提交失败或无法收束时记录终止性失败并跳过 cleanup.
- 计划三: 将 `RunAndQuitAsync` 最终 `SceneTree.Quit` 包入主线程门禁, 记录提交失败和最终退出状态; 保持不继续下一场景、detached drain 有界、未收束跳过 cleanup、actionFailureLatched 不可逆和真实 run/modifier/room/card/action API.
- 已确认边界: 第三轮报告已确认 `NGame.IsMainThread()` 位于 `research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:554`, `_Ready` 保存主线程信息位于 `:739-746`; 跨线程 Godot 安全模式使用 `CallDeferred` 位于 `G:\omp works\Sts2\sts2-spire1\research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/GodotTreeExtensions.cs:103-110`. 本轮先使用这些已确认 API, 不声称底层 operation 已取消.
- 未验证边界: 当前目标二进制未运行, 尚未证明 `CallDeferred` 的 deferred completion 形状、主线程 gate 的实际恢复时序、cleanup/quit 在超时和 drain 隔离分支中的实机结果; 本轮仍不构建、不测试、不启动游戏.

## 第四轮检查面一: 主线程 API 证据已确认

- 2026-10-01: 已核对发行版 `NGame.IsMainThread()` 与 `_Ready` 保存主线程 id 的证据: `research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:554,739-746`.
- 2026-10-01: 已核对发行版现有跨线程安全模式: `research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/GodotTreeExtensions.cs:103-110` 在 `NGame.IsMainThread()` 为假时使用 `node.CallDeferred(Node.MethodName.QueueFree)`. 项目现有用法还显示 `Callable.From(...).CallDeferred()` 为提交式 API, 未发现其返回 completion task 的证据; 因此实现将用本地 `TaskCompletionSource` 包住 deferred callback, 不声称 `CallDeferred` 自身返回 Task.
- 2026-10-01: 已确认本轮不使用 `Task.Run` 作为线程切换, 不新增取消 API, 不修改白名单外文件.

## 第四轮检查面二: 主线程门禁实现完成

- 2026-10-01: `FormNativeSmokeRunner.cs:841-890,913-925` 新增主线程门禁. 主线程直接执行; 非主线程使用发行版已有 `Callable.From(...).CallDeferred()` 提交, 由本地 `TaskCompletionSource` 等待 deferred callback 完成; `CallDeferred` 提交、callback 执行和有界等待失败均转换为 `MainThreadGateException`, 不使用 `Task.Run`.
- 2026-10-01: `FormNativeSmokeRunner.cs:799-838` 将 startup completion、run/combat/play 条件读取和 `AwaitProcessFrame` 创建均通过主线程门禁. `FormNativeSmokeRunner.cs:279-483` 将 `RunManager` 状态读取、真实 run/room/card/action 创建与入队、快照和 action 判定全部通过主线程门禁; `:621-685` 将 action cancel 和最终 evidence 访问也门禁化.
- 2026-10-01: `FormNativeSmokeRunner.cs:570-599` 将最终 `RunManager.CleanUp(graceful:false)` 只放在主线程门禁内; gate 失败记录 `cleanupFailure`、`terminalFailure` 并阻止下一场景. detached drain 仍不访问 Godot 对象, 未收束仍跳过 cleanup.
- 2026-10-01: `FormNativeSmokeRunner.cs:124-153,892-911` 将最终 `SceneTree.Quit(exitCode)` 通过主线程门禁提交, 有界等待后记录 `quitStatus` 到日志和 `final` JSON; 提交失败、deferred timeout 或 callback 异常不会被伪报为已退出.
- 未验证边界: 未触发真实非主线程 continuation、CallDeferred 提交失败、主线程 gate timeout、cleanup gate failure 或 quit gate failure; 未构建、未测试、未启动游戏、未部署, 因此不宣称目标二进制的 Godot 线程恢复和最终退出已实机通过.

## 第四轮检查面三: 约束回读完成

- 2026-10-01: 静态回读确认 `FormNativeSmokeRunner.cs:296,582` 是唯一两个 `RunManager.CleanUp` 调用点, 均位于 `InvokeOnMainThreadWithTimeoutAsync` callback 内; `:829` 是唯一 `game.AwaitProcessFrame()` 调用点, 位于主线程 gate callback 内; `:901` 是唯一 `SceneTree.Quit` 调用点, 位于最终 quit gate callback 内.
- 2026-10-01: 静态回读未发现 `Task.Run`, `NGame.Quit`, `autoslay`, `TestMode`, `PowerCmd.Apply` 或 `Task.FromResult(condition)`. 真实 `StartNewSingleplayerRun`、`EnterRoomDebug`、`CardPileCmd.Add`、`PlayCardAction`、`RequestEnqueue` 和 `CompletionTask` 链路仍保留, `actionFailureLatched` 与终止性场景停止逻辑仍保留.
- 2026-10-01: 白名单状态仅包含唯一报告、`FormNativeSmokePatch.cs` 和 `FormNativeSmokeRunner.cs` 三个未跟踪文件; 本轮未写其它产品文件、共享配置、安装目录或构建产物.
- 未验证边界: 上述为静态回读, 未构建、未测试、未启动游戏、未部署; 未验证目标二进制中 deferred callback 是否按预期执行、最终 quit JSON 是否在进程退出前落盘, 不宣称 PASS.

## 最终静态回读与主线程 completion 证据

- 2026-10-02: 已完成最后一次静态回读, 未修改产品代码. `FormNativeSmokeRunner.cs:843-877` 的主线程门禁先用 `NGame.IsMainThread()` 分支; 主线程直接执行 operation, 非主线程创建 `TaskCompletionSource<T>(RunContinuationsAsynchronously)`.
- 2026-10-02: `CallDeferred` completion 证据已确认: `FormNativeSmokeRunner.cs:860-870` 使用 `Callable.From(...).CallDeferred()` 提交 callback; callback 成功调用 `completion.TrySetResult(operation())`, 异常调用 `completion.TrySetException(new MainThreadGateException(...))`; `:876` 返回 `completion.Task`. 因此等待的是本地 callback completion task, 不是声称 `CallDeferred()` 自身返回 Task. `FormNativeSmokeRunner.cs:879-891` 再以 `Task.WhenAny(invocation, Task.Delay(MainThreadGateSeconds))` 对门禁等待设置有界超时, timeout 映射为 `MainThreadGateException`.
- 2026-10-02: 主线程 API 研究证据已再次核对: `research/engine-dllsrc/MegaCrit.Sts2.Core.Nodes/NGame.cs:554` 在 `_Ready` 记录主线程 id, `:739-746` 实现 `IsMainThread()`; `research/engine-dllsrc/MegaCrit.Sts2.Core.Helpers/GodotTreeExtensions.cs:103-110` 在非主线程通过 `node.CallDeferred(...)` 访问 Godot 节点. 这些证据支持当前 gate 形状, 但不证明目标二进制运行时一定按预期调度 callback.
- 2026-10-02: 实现行号已确认: 条件逐帧轮询和 `AwaitProcessFrame()` 门禁为 `FormNativeSmokeRunner.cs:812-840`; final cleanup 的 `runManager.IsInProgress` 读取与 `RunManager.CleanUp(graceful:false)` 仅在门禁 callback 内为 `:573-596`; final `SceneTree.Quit(exitCode)` 仅在门禁 callback 内为 `:894-912`. 本回读未发现其它 `RunManager.CleanUp`, `AwaitProcessFrame` 或 `SceneTree.Quit` 调用点.
- 2026-10-02: 约束静态扫描再次确认: 未发现 `Task.Run`, `NGame.Quit`, `autoslay`, `TestMode`, `PowerCmd.Apply` 或 `Task.FromResult(condition)`; 仅存在 `FormNativeSmokeRunner.cs:849` 的主线程直接结果封装 `Task.FromResult(operation())`, 不属于条件等待. 真实 run/modifier/room/card/action API、detached drain、cleanup skipped、不可逆 `actionFailureLatched` 和后续场景终止逻辑仍保留.
- 2026-10-02: 未验证边界保持明确: 未构建、未测试、未启动游戏、未部署. 未验证真实非主线程 continuation 是否都经过 gate, `CallDeferred` callback 是否在目标二进制中实际执行, gate submit/callback/timeout 失败分支, cleanup skipped 后的最终退出时序, `SceneTree.Quit` 是否在进程退出前完成, detached operation 是否在 drain 内收束, 以及 final JSON 是否成功落盘. 不据此宣称实机通过或底层 task 已取消.
- 2026-10-02: 本轮写入范围回读仅涉及唯一报告追加; 未修改其它产品文件、共享配置、Steam 安装、构建产物或部署目录. 按要求不构建、不测试、不启动游戏.
