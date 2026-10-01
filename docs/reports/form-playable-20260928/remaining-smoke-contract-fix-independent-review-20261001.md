## 已确认

- 本轮按本地请求在当前继承会话中使用 `6.1sol` / `agentrouter` 约束执行；未委派、未启动其它代理运行时。审查范围仅为 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`、`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs` 和实现者报告；本结论仅限静态审查。

- **P1：generic nested Task 仍未被完整展开，bounded detached drain 契约不成立。** `FormNativeSmokeRunner.cs:1502-1505` 只对精确类型 `Task<Task>` 执行 `Unwrap()`。本地发行版源码/API 确认：`NGame.StartNewSingleplayerRun` 返回 `Task<RunState>`（`G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:212370`）、`RunManager.EnterRoomDebug` 返回 `Task<AbstractRoom>`（`:40505`）、`NodeUtil.AwaitProcessFrame` 返回 `Task<float>`（`G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes.GodotExtensions\NodeUtil.cs:16`）、`CardPileCmd.Add` 返回 `Task<CardPileAddResult>`（`G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\CardPileCmd.cs:324`）。这些调用经 `InvokeOnMainThreadWithTimeoutAsync<T>`（目标文件 `:1436-1454`）提交时，gate 超时登记的是 `Task<Task<T>>`；它不匹配 `task is Task<Task>`，所以 `DetachedOperation` 只等待外层 completion task。若 deferred callback 及时返回但底层 run/room/card/frame task 仍 pending，drain 会误报“已收束”，随后可能进入 cleanup 或 quit。受影响调用点为 `:472-488`、`:508-521`、`:554-563`、`:1318-1369`。实现者报告所称的既有 `Task<Task>.Unwrap()` 已保留，但不足以覆盖这些实际 generic 返回类型。

- `AwaitProcessFrame` 的**已返回 frame task 路径**已具备终止性：`FormNativeSmokeRunner.cs:1338-1348` 和 `:1372-1382` 对取消/故障执行 `await` 并包装为 `TerminalOperationException`；`:1360-1369` 对 frame 超时登记 detached operation 并抛终止性异常；`:1384-1390` 拒绝跨过总 deadline 后才完成的 frame。发行版 `NodeUtil.cs:16-29` 明确列出节点离树或取消时的 `OperationCanceledException`/`TaskCanceledException`，因此该异常不会按普通场景失败继续。

- condition gate 与 process-frame submission gate 的**剩余预算算法**静态成立：`FormNativeSmokeRunner.cs:1276-1300`、`:1307-1324` 都传入 `BoundedMainThreadGateTimeout(remaining)`，并在 gate 返回后重新检查总 deadline；`:1357-1390` 在 frame 完成后再次检查。`:1430-1434` 的实现为 `min(remaining, MainThreadGateSeconds)`，迟到的 `true` 或成功 frame 不会被接受。前述 generic nested Task 缺口仍使“submission gate 自身超时后的底层 operation 必然被 drain”这一更强契约不成立。

- terminal failure 的传播和场景阻断在普通已观察异常路径上成立：`RunScenarioAsync` 的 `TerminalOperationException` 分支位于 `FormNativeSmokeRunner.cs:721-730`；`RunAsync` 在 `:391-404` 写当前场景 evidence 后遇到 `terminalFailure` 即停止后续场景。`finally` 在 `:784-800` 先 drain detached operations，未收束时设置 `cleanupSkipped`；只有收束后才在 `:818-834` 调用真实 `RunManager.CleanUp`。但 generic `Task<Task<T>>` 误判会绕过这个保护的真实目的，因此不能把 cleanup gate 评为完整 PASS。

- action failure latch 与 JSON evidence 的普通控制流静态成立：action completion 的超时/取消/异常在 `FormNativeSmokeRunner.cs:600-648` 锁存；`actionPassed == false` 在 `:702-718` 再次锁存并写入失败 evidence；form gate 失败在 `:688-700` 设置 `formFailureLatched`。`TryWriteScenarioEvidence`（`:1670-1691`）在写入失败时标记 `terminalFailure` 并重试，失败后停止场景循环。

- **pre-quit final evidence 静态 PASS。** `RunAndQuitAsync` 在 `FormNativeSmokeRunner.cs:144-172` 构造 `pending`/`failed`、`quitStatus=pending`、`quitDrainSettled=false`、`quitDrainOutcome=not-observed`、`finalEvidencePhase=pre-quit` 的 payload，并在 `:159` 同步调用 `WriteJsonIfConfigured("final", ...)`；`SceneTree.Quit` 仅在 `:174` 之后通过 `QuitOnMainThreadAsync` 请求。`WriteJsonIfConfigured` 的 `File.WriteAllText` 位于 `:1722-1727`，调用返回即完成本次同步写入；首写失败会在 `:160-171` 记录 `evidenceWriteFailure` 并于 Quit 前重试。pre-quit payload 不声称 `completed` 或已观察 quit outcome。Quit 后更新在 `:175-231`，且最终 `completed` 仍要求业务 `exitCode==0`、`quitStatus` 为 `executed*`、drain settled 且无 evidence write failure。

- 普通入口和禁止旁路静态 PASS：`FormNativeSmokePatch.cs:7-14` 只有 `NGame._Ready` Harmony postfix；`FormNativeSmokeRunner.cs:47-60` 在没有 `--form-native-smoke*` 请求时立即返回，并用 `_started` 防重复。真实链路使用 Watcher character、真实 run modifier、真实 `StartNewSingleplayerRun`/`EnterRoomDebug`/`CardPileCmd.Add`/`PlayCardAction`；未发现 `Task.Run`、`Process.Start`、同步 `.Wait()`/`.Result`、直接 `PowerCmd`/`AddPower` 注入或 fake hook。`RequestEnqueue` 只入队真实 `PlayCardAction`（`:583-590`）。

- 线程边界静态核对：目标文件中的主要 Godot/Game 状态访问和命令调用均位于 `InvokeOnMainThreadWithTimeoutAsync` 的 callback（RunManager、combat/player 状态、snapshot、action、cleanup、`AwaitProcessFrame`、`SceneTree.Quit`）；detached drain 本身不碰 Godot 对象。结果初始化处的 `encounter.Id.Entry`/`encounter.RoomType`（`:417-425`）是已传入模型的元数据读取，不构成当前代码中发现的 Godot Node 旁路；实际 continuation 调度和 Watcher bridge 的运行时线程行为仍未实测。

- **最终结论：REWORK（仅限静态审查）。** 三项新修复中的 `AwaitProcessFrame` 已返回任务的 terminal failure、condition/process-frame 剩余预算和 pre-quit final evidence 顺序均有源码证据；但 `Task<Task>.Unwrap()` 只覆盖非泛型 `Task`，未覆盖实际使用的 `Task<Task<T>>`，这会使迟到 main-thread callback 的底层 operation 脱离 bounded drain，并可能错误放行 cleanup/quit。该 P1 缺口未修复前不能给 PASS。

## 进行中

- 本轮静态检查面已完成；没有待继续的静态源码面。产品代码未修改，仅增量写入本报告。

## 未知

- 未构建、未运行测试、未部署、未启动或停止游戏；未知当前工作树的编译状态以及目标二进制中的真实 deferred 调度、frame 取消时序、cleanup/quit 时序和最终 JSON 实际落盘结果。
- 未人为制造主线程 gate 延迟、generic async callback 迟到、节点离树或进程退出竞态；P1 结论来自当前源码的泛型任务类型、detached drain 控制流和本地发行版签名，不冒称实机复现。
- pre-quit 写入在正常文件 I/O 返回路径上具备同步顺序证据；报告目录不可写、后续 post-quit 同名文件更新被进程退出打断等文件系统边界仍未由本轮运行验证。