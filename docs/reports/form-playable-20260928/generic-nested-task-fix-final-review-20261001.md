## 已确认

- 本轮以当前工作树最终源码为准，使用用户指定的 `6.1sol` / `agentrouter` 约束；只做静态审查，不构建、不运行测试、不部署、不启动游戏。产品源码不修改；本报告是唯一写入目标。
- 最新 generic helper 的精确行号已重新读取：`CreateDetachedTaskDrain<T>` 为 `FormNativeSmokeRunner.cs:1436-1443`，`AwaitNestedTaskAsync<T>` 为 `:1445-1452`，gate 超时接入为 `:1459-1473`。`CreateDetachedTaskDrain<T>` 对 `T` 可赋值为 `Task` 的返回类型转入两层 await；`T` 不是 `Task` 时直接返回 outer invocation。`AwaitNestedTaskAsync<T>` 在 `:1447` 等待 outer callback completion，在 `:1448-1450` 取得并等待 inner `Task`。
- 当前静态证据表明，实际 `Task<T>` API 返回值形成的 `Task<Task<T>>` 会进入该 helper：outer `Task<T>` 的 `T` 是 `Task<RunState>`、`Task<AbstractRoom>`、`Task<CardPileAddResult>` 或 `Task<float>` 时，`typeof(Task).IsAssignableFrom(typeof(T))` 为真，helper 会等待 deferred callback 完成并继续等待其返回的 inner operation；普通非 Task 返回值不展开。
- `DetachedOperation` 的兼容逻辑仍在 `FormNativeSmokeRunner.cs:1519-1525`，其中 `Task<Task>` 仍调用 `Unwrap()`；因此 generic helper 与既有非泛型嵌套任务路径同时保留。

- **AwaitProcessFrame terminal failure 已闭合。** `WaitForConditionWithTimeoutAsync` 在 `FormNativeSmokeRunner.cs:1317-1354` 和 `:1357-1391` 都会对已返回的 `frameTask` 执行 `await`；取消或异常在 `:1338-1348`、`:1372-1382` 被包装为 `TerminalOperationException`，不会作为普通条件失败继续循环。frame task 超时在 `:1326-1335` 或 `:1360-1369` 注册 `DetachedOperation` 后立即终止当前条件等待；当前 generic helper 使 submission gate 超时时的 `Task<Task<float>>` 也等待 outer callback 和 inner frame task。
- **总 deadline 剩余预算静态成立。** 条件等待在 `:1276-1305` 建立总 deadline，condition gate 在 `:1289-1293` 使用 `BoundedMainThreadGateTimeout(remaining)`，gate 返回后在 `:1294-1300` 重新拒绝已越过 deadline 的结果。process-frame submission 在 `:1307-1324` 重新计算剩余预算、使用同一 gate 上限并在返回后再次检查；frame 完成后 `:1384-1390` 再次拒绝迟到成功。`BoundedMainThreadGateTimeout` 的当前实现为 `min(remaining, 10 seconds)`（`:1430-1434`）。
- `WaitWithTimeoutAsync`（`:1251-1267`）同样先受 main-thread gate 限制，再对已返回 operation 使用显式 timeout；相关启动、run、room、card injection 和 action completion 调用均传入 `terminalOnFailure: true`，超时/取消/异常会进入终止性错误路径。
- **pre-quit final evidence 顺序和语义成立。** `RunAndQuitAsync` 在 `FormNativeSmokeRunner.cs:141-172` 先构造 `status=pending` 或 `failed`、`quitStatus=pending`、`quitDrainSettled=false`、`quitDrainOutcome=not-observed`、`finalEvidencePhase=pre-quit` 的 payload，并在 `:159` 调用同步的 `WriteJsonIfConfigured("final", ...)`；`SceneTree.Quit` 只在 `:174` 之后经 `QuitOnMainThreadAsync` 请求。`WriteJsonIfConfigured` 在 `:1722-1746` 使用 `JsonSerializer.Serialize` 后同步 `File.WriteAllText`，正常成功返回即已有诚实可解析的 pre-quit evidence；首次写失败会在 `:160-171` 标记失败、记录日志并 retry，未把失败伪装成成功。Quit 后的 `:175-231` 只在实际 quit 状态、bounded drain、业务 exit code 和 evidence write 条件均满足时写出 `completed`。
- **cleanup gate 与 failure latch 静态闭合。** Action completion 的 timeout、取消、异常在 `:591-648` 锁存 `actionFailureLatched` 和 `terminalFailure`；`actionPassed` 在 `:658-678` 同时检查 latch、action 状态、completion task 状态、异常和新增 unobserved fault，失败分支在 `:702-718` 固化 action evidence。最终 form gate 只有在 `:688-700` 通过且 `formFailureLatched` 未置位时才允许场景 `passed`。`finally` 的 `:784-818` 先对全部 detached operations 做 bounded drain；未收束或 drain 失败会把 cleanup 标为 skipped 并置 terminal failure，只有 `cleanupAllowed && runManager != null` 才在 `:818-834` 调用真实 `RunManager.CleanUp`。cleanup 自身的 deferred gate 若失败，会在 `:844-870` 单独 drain 并保持失败/跳过证据。
- terminal failure 从场景传播到场景循环：`RunScenarioAsync` 在 `:721-730` 对 `TerminalOperationException` 设置 `terminalFailure`，`RunAsync` 在 `:391-404` 写出该场景 evidence 后遇到该标志即停止后续场景；startup/setup 失败分支也在 `:286-307`、`:363-387` 做同样的失败证据和 bounded drain 处理。
- **普通入口、线程边界和禁止旁路静态通过。** `TryStart`（`:47-60`）在无 smoke 参数时立即返回，只在请求存在且 `_started` 首次置位后启动 `RunAndQuitAsync`；主流程使用真实 `StartNewSingleplayerRun`（`:472-488`）、`EnterRoomDebug`（`:508-521`）、`CardPileCmd.Add`（`:554-563`）和 `PlayCardAction` 入队（`:573-590`），未见 fake hook 或直接 Power 注入。`InvokeOnMainThreadAsync`（`:1394-1427`）在主线程直接执行，否则只通过 `Callable.From(...).CallDeferred()` 回到主线程；运行状态、combat/card/action、snapshot、cleanup、AwaitProcessFrame 和 `SceneTree.Quit` 的游戏访问均从该边界进入。当前文件静态扫描未发现 `.Result`、`.Wait(...)`、`Task.Run(...)`、`Process.Start`、`PowerCmd` 或 `AddPower`。
- **等待均有边界。** 普通 operation 使用 gate/operation timeout（`:1459-1473`、`:1554-1602`），条件/frame 使用总 deadline，detached drain 使用单次 `DetachedDrainSeconds=10` 总截止点（`:1632-1687`）。`AwaitNestedTaskAsync` 的 inner await 只作为 detached operation 交给上述 bounded drain 观察；当前控制流没有同步阻塞或无界等待。

**最终结论：PASS（仅限静态审查）。** 当前最终源码中的 generic nested-task 修复已覆盖 `Task<Task>` 与 `Task<Task<T>>` 的 outer callback 和 inner operation，非 Task 返回值不展开，且保留 `Task<Task>.Unwrap()` 兼容逻辑。旧报告中针对 helper 修复前快照的 REWORK 结论不适用于当前源码；本轮未发现新的静态契约缺陷。
## 进行中

- 本轮要求的所有静态检查面均已完成；无待继续项。

## 未知

- 未构建、未运行测试、未部署、未启动游戏；编译器接受度、运行时调度以及真实退出时序均未验证。
