# Form native smoke review r12

审查对象：实现者 `01a0f7ca-5741-7d20-a179-483daf549a56` 负责的真实单战斗 headless 载体。
审查入口：收到主会话精确文本 `START REVIEW` 后开始。
审查方式：先读取实现者报告，再读取白名单产品 diff；本轮未构建、未部署、未启动游戏、未修改产品代码或构建产物。

首次审查结论：**REWORK**。首次发现的 3 项 P1 已在返工快照中逐项复核。`r`n`r`n二次监督当前结论：**REWORK**。返工已静态修复原 3 项 P1 的直接缺陷，但仍有 1 项 P1 异步重入风险和 1 项 P2 结果一致性风险；真实运行状态仍为 **未知**，不能宣称 PASS。
真实运行状态：**未知**；本轮没有足够实机证据宣称 PASS。

## 已确认

### [P1] 条件等待实际上不会等待，战斗链可能在状态成立前继续

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:271-294`
  - 辅助实现：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:455-470`
- 触发条件：`StartNewSingleplayerRun` 返回后，`runManager.IsInProgress` 尚未成立；或房间已请求但 `CombatManager.Instance.IsInProgress` / `PlayerTurnPhase.Play` 尚未成立。
- 契约：真实 run、真实房间和真实出牌 action 必须按状态顺序完成；等待条件为假时必须继续等待，而不是把“当前为假”当成完成。
- 控制流证据：
  - `RunScenarioAsync` 将三个状态条件包装成 `() => Task.FromResult(condition)`。
  - `WaitWithTimeoutAsync` 在 `Task.WhenAny` 中收到这个已完成的 `Task` 后立即返回；它没有轮询，也没有检查条件值本身。
  - 因此 `runManager.DebugOnlyGetState()`、`player.PlayerCombatState` 和 `CombatManager.Instance` 可能在真实初始化完成前被读取，后续失败可能被误报为模型、房间或 action 失败。
- 复现命令（只读静态复核，本轮未执行游戏）：
  ```powershell
  $p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; $l=Get-Content -LiteralPath $p; $l[270..293]; $l[454..469]
  ```
  运行路径模板（本轮未执行）：
  ```powershell
  $env:SPIRE1_FORM_SMOKE_REPORT='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\smoke-review-r12'; & 'E:\Slay the Spire 2\SlayTheSpire2.exe' --rendering-driver opengl3 --form-native-smoke=calm
  ```
- 最小修复范围：将这三处条件改成真实的有界轮询，例如每帧/短延迟检查 `Func<bool>`，超时后抛出 `TimeoutException`；或使用发行版已经提供的状态完成 task。不要继续复用 `Func<Task>` + `Task.FromResult(bool)` 组合。
- 尚缺实机证据：未证明当前二进制在 run 建立、combat 建立和 Play phase 建立时恰好能够依赖这些状态；未产生任何真实战斗 JSON。

### [P1] 三个关键真实操作没有被超时边界包住，一次性退出不具备严格有界性

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:262-269`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:281-285`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:301-303`
- 触发条件：真实二进制在 `StartNewSingleplayerRun`、`EnterRoomDebug` 或 `CardPileCmd.Add` 内部因加载、同步、Godot 生命周期或 action 队列异常而长期不完成。
- 契约：单次 smoke 必须有界；任何真实启动、进入房间、注入手牌和退出路径都不能留下无限等待。
- 控制流证据：
  - `StartNewSingleplayerRun(...)`、`EnterRoomDebug(...)` 和 `CardPileCmd.Add(...)` 都是直接 `await`。
  - 现有 `WaitWithTimeoutAsync` 只包住后续状态检查和 `PlayCardAction.CompletionTask`，没有包住上述三个真实操作。
  - 因此这些 task 若永不完成，`RunAndQuitAsync` 不会进入 `finally`，`SceneTree.Quit(exitCode)` 也不会执行。
- 复现命令（只读静态复核，本轮未执行游戏）：
  ```powershell
  $p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; $l=Get-Content -LiteralPath $p; $l[261..268]; $l[280..284]; $l[300..302]
  ```
- 最小修复范围：为这三个 await 引入明确的超时包装，并定义超时后的清理/取消策略；超时后必须确保 action、room sync 和 run state 不再继续向已安排的 `CleanUp`/`SceneTree.Quit` 重入。若底层 API 不支持取消，至少要在失败路径记录超时、执行安全清理并保证进程最终退出。
- 尚缺实机证据：未测量真实二进制的启动、房间同步、手牌注入和退出耗时；未证明任何超时分支可回收或可退出。

### [P1] action 取消或 action 等待超时时，JSON 会丢失 action 状态与 `action.Exception`

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:309-337`
  - 异常出口：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:339-343`
  - 真实契约来源：`G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:169449-169466,169606-169611`
- 触发条件：`PlayCardAction` 在执行前被取消，或 `CompletionTask` 在 `ActionTimeoutSeconds` 内未完成。
- 契约：JSON 必须区分成功、失败、取消、未知和未实测，并记录 action 的状态、完成情况、取消状态和 `action.Exception`；不能把取消只表现成一个外层 `TaskCanceledException`。
- 控制流证据：
  - `actionResult` 只在 `await WaitWithTimeoutAsync(() => action.CompletionTask, ...)` 之后才写入 `result["cardPlay"]`。
  - 真实 `GameAction.Cancel()` 会将状态设为 `GameActionState.Canceled`，并把 `CompletionTask` 设置为 canceled；因此该 await 会先抛出，`actionResult`、`action.State` 和 `action.Exception` 的记录代码不会执行。
  - 外层 `catch` 只写 `result["failure"] = exception.ToString()`，没有引用局部 `action`，所以取消/超时 JSON 不能证明 action 的最终状态，也没有观察该 action 的 `Exception`。
- 复现命令（只读静态复核，本轮未执行游戏）：
  ```powershell
  $p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; $l=Get-Content -LiteralPath $p; $l[308..342]
  ```
- 最小修复范围：把 `action` 和 `actionResult` 提升到可由异常路径访问的作用域；分别捕获取消、超时和普通异常，在所有路径填充 `State`、`CompletionTask` 结果、`cancelled`、`Exception` 和 `failure`，并在清理前确认底层 action 不会继续执行。不要仅依赖外层异常字符串。
- 尚缺实机证据：未让真实 `PlayCardAction` 走取消、超时或底层执行异常分支；现有实现未产生可供核对的 `cardPlay` 失败 JSON。

### [确认通过] 普通启动门禁和一次性入口形状符合要求

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs:7-14`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:41-54`
  - 参数解析：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:56-116`
- 证据：无匹配参数时在 `:44-47` 直接返回；只有匹配 smoke 参数且 `_started` 首次置位时才创建 `RunAndQuitAsync`。静态扫描未发现 `autoslay`、`TestMode`、`NGame.Quit()`、`PowerCmd.Apply` 或直接构造/应用 form power 的 bypass。
- 控制流：普通启动只执行参数解析和一次整数门禁，不订阅 `TaskHelper.UnobservedFault`，不写 JSON，不创建 smoke task。
- 复核命令：
  ```powershell
  rg -n 'autoslay|TestMode|PowerCmd\.Apply|NGame\.Quit|WatcherFormStancePower' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs' 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'
  ```
- 尚缺实机证据：未在普通启动和带参数启动各运行一次，未验证 Harmony 对目标发行版的实际扫描、窗口和进程退出行为。

### [确认通过] 代码确实采用要求的原生 run、modifier、room 和 action 队列入口

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:204-227`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:262-285`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:296-337`
- 证据：
  - 角色从 `ModelDb.AllCharacters` 查找 `WatcherMod.Watcher`。
  - 卡牌从 `ModelDb.AllCards` 按 `WATCHER_VIGILANCE`、`WATCHER_ERUPTION_P`、`WATCHER_BLASPHEMY` 精确查找。
  - run 使用 `NGame.Instance.StartNewSingleplayerRun(..., shouldSave:false, ..., GameMode.Custom, ...)`，modifier 使用 `ModelDb.Modifier<FormStanceModifier>().ToMutable()`。
  - 房间使用 `RunManager.EnterRoomDebug`；卡牌使用 `CombatState.CreateCard`、`CardPileCmd.Add`、`PlayCardAction` 和 `ActionQueueSynchronizer.RequestEnqueue`，完成状态从 `CompletionTask` 读取。
- 尚缺实机证据：以上为源码/研究源码证据，不是目标二进制中的真实战斗证据；尚未确认 Watcher 类型、encounter、卡牌执行和 form bridge 在当前部署中全部绑定成功。

## 进行中

- 本轮静态审查已完成；没有继续中的代码审查、构建或运行任务。
- 需要实现者先修复三个 P1 后，再由主会话集中构建、部署到测试副本并运行真实 smoke；不得使用 Steam 安装或共享 `mod_configs` 作为验证环境。

## 未知

- 未构建，故未知目标发行版是否接受 `NGame._Ready` Harmony patch、`MapPointType`、`GameMode`、`PlayerTurnPhase`、`PileType.GetPile`、`CardModel.TargetType`、`GameAction.State` 等实际签名。
- 未启动游戏，故未知 `FormStanceWatcherBridge.TryBind()` 是否在当前进程成功、`FormStanceModifier` 是否进入真实 run、Watcher native stance 是否按场景变化、form carrier/effect 是否被正确创建。
- 未启动游戏，故未知 `EnterRoomDebug` 是否创建真正的单战斗、敌方是否可作为目标、三张真实 Watcher 卡是否能被 `PlayCardAction` 成功消费。
- 未启动游戏，故未知 Godot 主线程恢复、`RunManager.CleanUp(graceful:false)`、延迟 action/同步 task 与最终 `SceneTree.Quit(exitCode)` 之间是否存在重入或提前退出风险。
- 未写入任何产品代码、构建产物、部署目录、游戏安装或共享 `mod_configs`；本报告是静态审查结论，不宣称真实战斗通过。


## 二次监督复核

审查对象：返工后的 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs` 与实现报告 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-worker-r12-20261001.md`。

### [确认修复] 原 P1-1：条件等待已改为逐帧有界检查

- 绝对路径与行号：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:281-310,584-611`。
- 证据：run、combat、Play phase 现在传入 `Func<bool>`，由 `WaitForConditionWithTimeoutAsync` 在 `NGame.AwaitProcessFrame()` 后重新检查；已移除原先的 `Task.FromResult(condition)` 伪等待。
- 结论：原 P1-1 的直接缺陷已静态修复。
- 尚缺实机证据：未证明目标二进制的 process-frame 恢复、真实 run 初始化和战斗时序。

### [确认修复] 原 P1-2：真实启动、入房和卡牌注入已纳入等待边界

- 绝对路径与行号：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:269-279,292-322,613-634`。
- 证据：`StartNewSingleplayerRun`、`EnterRoomDebug`、`CardPileCmd.Add` 均经过 `AwaitOperationWithTimeoutAsync`；超时会进入场景异常路径和最终 `SceneTree.Quit` 路径。
- 结论：原 P1-2 的“直接 await 无边界”已静态修复；但底层 API 没有取消 token，返工报告也明确只做遗留 task 观察，不能把这等同于底层操作已停止。
- 尚缺实机证据：未测量超时后的底层 task 是否仍会改变 `RunManager`、房间或卡堆状态。

### [确认修复] 原 P1-3：action 失败/取消证据已提升到异常可见作用域

- 绝对路径与行号：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:257-258,328-389,413-487`。
- 证据：`action` 与 `actionResult` 在 try 外声明；取消、超时和普通异常均进入 `RecordActionEvidence`，JSON 记录 `state`、`completionTaskOutcome`、`cancelled`、`exception`、`failure`。
- 结论：原 P1-3 的“只留下外层异常字符串”已静态修复。
- 尚缺实机证据：未产生真实取消、超时或 `action.Exception` JSON。

### [P1] 超时后的真实 operation 仍会脱离监督并可能与清理、下一场景或退出重入

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:269-322`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:613-649`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:201-230,370-408`
- 触发条件：`StartNewSingleplayerRun`、`EnterRoomDebug` 或 `CardPileCmd.Add` 超时，但底层 task 随后仍完成或抛异常。
- 契约：超时必须阻止真实 operation 继续改变当前 run，或在进入下一场景/清理/进程退出前等待其确定终态；不能只观察异常日志。
- 控制流证据：
  - `AwaitOperationWithTimeoutAsync` 超时后只调用 `ObserveDetachedTask(operation, description)`，随后抛出 `TimeoutException`。
  - `ObserveDetachedTask` 仅安装 `ContinueWith` 记录 fault；成功完成不会阻止，也不会向主流程报告。
  - `RunScenarioAsync` 随后执行 `finally` 清理并返回；`RunAsync` 写 JSON 后继续 `foreach` 的下一场景，最终还会执行进程退出。
  - 返工报告确认这些发行版 API 没有取消 token，因此当前实现没有证明底层 operation 已被取消或隔离。
- 复现命令（只读静态复核，本轮未执行）：
  ```powershell
  $p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; $l=Get-Content -LiteralPath $p; $l[268..321]; $l[612..648]; $l[200..229]
  ```
- 最小修复范围：超时后不要继续下一个场景；将超时转为终止性失败，先保证遗留 operation 进入确定终态，或明确隔离并在最终退出前完成一次有界收束。若底层无法取消，应让 smoke 进入单场景终止路径，不允许遗留 run/room/card task 与后续场景共享 `RunManager`。
- 尚缺实机证据：未触发真实超时，未证明遗留 task 会不会在 `CleanUp`、下一场景或 `SceneTree.Quit` 前后重入。

### [P2] action 超时后可能被误报为 PASS，导致 JSON 结果自相矛盾

- 绝对路径与行号：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:342-369`。
- 触发条件：`AwaitOperationWithTimeoutAsync(action.CompletionTask, ...)` 先因 timeout 抛出；`TryCancelAction` 记录了 timeout failure，但 action 随后在主流程检查前完成为 `Finished`，且没有 `action.Exception` 或新的 unobserved fault。
- 契约：一旦本次 action 经过 timeout/cancel/error 路径，外层场景必须保持 failed；`cardPlay.failure` 不能与外层 `status=passed` 冲突。
- 控制流证据：`TryCancelAction` 在 `:342-345` 写入失败原因；但 `actionPassed` 在 `:354-357` 只检查 `State`、`Exception`、`CompletionTask.Status` 和 unobserved fault，没有检查 `actionResult["failure"]` 或是否发生过 timeout/cancel。因此存在“cardPlay 记录 timeout、外层 status 却变成 passed”的竞态窗口。
- 复现命令（只读静态复核，本轮未执行）：
  ```powershell
  $p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; $l=Get-Content -LiteralPath $p; $l[341..368]
  ```
- 最小修复范围：增加不可逆的 `actionTimedOutOrCancelled`/failure 状态，令 `actionPassed` 同时要求该状态未发生且 `actionResult["failure"] == null`；发生 timeout、cancel 或普通异常后不得重新变成 passed。
- 尚缺实机证据：未构造真实 action timeout 后延迟完成的时序，尚未看到矛盾 JSON。

## 二次复核结论

- **REWORK**：原 3 项 P1 的直接静态缺陷已修复，但当前仍有上述 P1 异步重入风险和 P2 PASS/失败证据不一致风险。
- **未知**：未构建、未测试、未部署、未启动游戏；真实 Watcher bridge、真实战斗、真实 action 和 Godot 生命周期均未验证。
- **不通过 PASS**：在上述两项问题修复并完成测试副本实机证据前，不得宣称 PASS。

## 第三轮监督复核

审查对象：最新返工后的 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`、`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs` 及实现报告的最新追加内容。

### [确认修复] 原 P1 异步重入的主流程隔离策略已静态落地

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:229-233`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:277-336,390-480`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:729-861`
- 证据：真实 run、房间和卡注入 operation 以 `terminalOnFailure:true` 注册 detached task；终止性失败在 `RunAsync` 中停止后续场景；清理前执行单一十秒 deadline 的 drain；未收束时写入 `isolated-unfinished-operation` 并跳过 `RunManager.CleanUp`。
- 结论：原 P1 所指出的“超时后继续下一场景并在遗留 operation 上清理”的直接控制流已静态修复。当前代码没有声称底层 API 已被取消，并把未收束边界写入 JSON。
- 尚缺实机证据：未触发真实超时，未验证 drain 内完成、fault、cancel 和十秒隔离分支。

### [确认修复] 原 P2 action PASS/失败结果竞态已静态锁存

- 绝对路径与行号：`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:265,345-388,447-498`。
- 证据：`actionFailureLatched` 在取消、超时和普通异常路径不可逆置为 `true`；`actionPassed` 同时要求 failure 未锁存、`actionResult["failure"]` 为空、真实 `Finished`、无 exception、`CompletionTask` 为 `RanToCompletion` 且无新增 unobserved fault。
- 结论：原 P2 所指出的“timeout 后延迟完成又被报告为 passed”已静态修复。
- 尚缺实机证据：未触发真实 action timeout 后延迟完成、cancel 或底层 exception 时序。

### [P1] 终止性 timeout 路径仍可能在非 Godot 主线程直接调用 Godot API，退出保证未闭合

- 绝对路径与行号：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:121-142`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs:623-634,749-766,807-861`
  - 主线程契约证据：`G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:554,739-746`
  - 现有跨线程 Godot 安全模式：`G:\omp works\Sts2\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Helpers\GodotTreeExtensions.cs:103-110`
- 触发条件：真实 operation 或 detached drain 超时。此时 `Task.WhenAny` 可能由 `Task.Delay` 获胜，continuation 不再由 `AwaitProcessFrame` 的 Godot signal 恢复；`RunScenarioAsync`、`RunAsync` 和 `RunAndQuitAsync.finally` 可能继续在非主线程执行。
- 契约：`NGame`/Godot 对象的访问、`RunManager.CleanUp` 和 `SceneTree.Quit` 必须遵守 Godot 主线程生命周期；超时路径尤其必须保证进程能够退出，不能把退出建立在未验证的跨线程 Godot 调用上。
- 控制流证据：
  - `RunAndQuitAsync.finally` 无论 continuation 所在线程都直接执行 `game.GetTree().Quit(exitCode)`。
  - `AwaitOperationWithTimeoutAsync` 和 `DrainDetachedOperationsAsync` 的超时分支依赖 `Task.Delay`，没有 `NGame.IsMainThread()` 检查，也没有 `CallDeferred`/主线程 completion gate。
  - 发行版在 `_Ready` 保存主线程 id，并提供 `NGame.IsMainThread()`；现有 Godot 安全扩展在非主线程使用 `CallDeferred`，而本载体没有采用同等保护。
- 复现命令（只读静态复核，本轮未执行）：
  ```powershell
  $p='G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs'; $l=Get-Content -LiteralPath $p; $l[120..141]; $l[622..633]; $l[748..765]; $l[806..860]
  ```
- 最小修复范围：为 `CleanUp` 和 `SceneTree.Quit` 建立明确的主线程门禁；非主线程只通过发行版可用的 deferred/main-thread completion 机制提交，且要记录提交失败和最终退出状态。对 `RunScenarioAsync` 的 Godot 状态读取与 cleanup 同样确认恢复到主线程后再执行；不能仅凭 `Task` 已完成推断 continuation 在 Godot 主线程。
- 尚缺实机证据：未在目标二进制触发 operation timeout、drain timeout 或无 process-frame 情况；未证明当前 `SceneTree.Quit` 在这些分支中实际退出，也未证明 `RunManager.CleanUp` 不会跨线程运行。

## 第三轮结论

- **REWORK**：原 P1 异步重入隔离和原 P2 action 失败锁存已静态确认修复，但新增发现的 P1 主线程退出/清理风险仍未闭合。
- **未知**：本轮仍未构建、未测试、未部署、未启动游戏；真实 bridge、单战斗、三张卡、超时 drain 和 Godot 线程恢复均未知。
- **不通过 PASS**：在主线程门禁修复并完成测试副本实机证据前，不得宣称 PASS。
