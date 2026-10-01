# generic nested task fix 监督审查报告

- 日期: 2026-10-02
- 范围: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`
- 实现者报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\remaining-smoke-contract-fix-worker-20261001.md`
- 用户指定模型/路由: `6.1sol` / `agentrouter`
- 审查限制: 仅静态审查；不构建、不测试、不部署、不启动游戏；不修改产品代码。
- 同批门禁: 已对实现者 `01a0f909-fce7-7ba2-9aa3-84ee94607f21` 使用当前原生 hub wait；返回 `status=completed`、`timed_out=false`，随后才开始读取代码和报告。实现者报告中此前的监督 `BLOCKED` 记录不替代本次已通过的完成门禁。
- 结论: **PASS（仅限静态审查）**。

## 已确认

### 门禁与范围

- 实现者本轮 follow-up 已完成且 wait 未超时；实现者摘要声明只修改目标代码文件，未构建、未测试、未部署、未启动游戏。
- 未委派新代理；本审查只使用当前会话的静态读取和文本检索。

### nested-task gate drain

- 参考 API 索引确认四类受影响调用的真实返回类型：`StartNewSingleplayerRun` 为 `Task<RunState>`（`research/engine-api-index.md:20770`），`EnterRoomDebug` 为 `Task<AbstractRoom>`（`:21602`），`CardPileCmd.Add` 为 `Task<CardPileAddResult>`（`:556`），`AwaitProcessFrame` 为 `Task<float>`（`:12030`）。代码调用锚点分别为 `FormNativeSmokeRunner.cs:472`、`:508`、`:554`、`:1318`，因此经 `Func<T>` 推断后确实形成 `Task<Task<T>>`。
- `CreateDetachedTaskDrain<T>`（`FormNativeSmokeRunner.cs:1436-1443`）先用 `typeof(Task).IsAssignableFrom(typeof(T))` 区分返回值为 `Task`/`Task<T>` 的泛型分支；`AwaitNestedTaskAsync<T>`（`:1445-1451`）先等待 outer `Task<T>`，再等待其返回的 inner `Task`。因此 `Task` 对应的 `Task<Task>` 与 `Task<T>` 对应的 `Task<Task<T>>` 都走同一条两层 drain，不再依赖 `Task<Task>` 精确类型匹配。
- gate timeout 的 `DetachedOperation` 创建点（`:1466-1468`）确实传入 `CreateDetachedTaskDrain(invocation)`；非 Task 返回值仍直接传递 outer task（`:1438-1441`）。当前其它 `new DetachedOperation` 调用点传入的都是已经展开的 `Task`（process-frame task 或 operation task），没有发现绕过 gate helper 的当前 nested generic 调用路径。
- `DetachedOperation` 中既有精确 `Task<Task>.Unwrap()` 兼容分支（`:1519-1525`）仍保留；它没有被当作泛型 gate timeout 的唯一展开机制。

### 无同步阻塞、无无界等待

- 对目标文件的静态文本检索未发现 `.Result`、`.Wait(`、`Task.Run`、`Task.WaitAll`、`Task.WaitAny`、`GetAwaiter().GetResult`、`Thread.Sleep` 或 `new Thread`。
- gate 等待使用 `Task.WhenAny(invocation, Task.Delay(gateTimeout))`（`:1459-1473`）；普通 operation 使用显式 `timeout`（`:1554-1602`）；detached drain 使用单一 `DetachedDrainSeconds` 截止时间（`:1632-1686`）。这些路径均有有限等待边界。
- nested drain 的 outer/inner 任一层若迟迟不完成，会由合并后的 `Task` 进入 bounded detached drain；未把 inner task 遗留成已完成的假证据。

### 既有三项修复

- frame terminal failure：`WaitForConditionWithTimeoutAsync` 在 frame 已完成时仍执行 `await frameTask` 并把异常包装为 `TerminalOperationException`（`:1326-1354`、`:1372-1383`）；未完成 frame 则注册 detached operation 并终止当前场景（`:1326-1335`、`:1360-1369`），不会继续下一场景。
- condition budget：condition submission 和 process-frame submission 都传入 `BoundedMainThreadGateTimeout(remaining)`（`:1289-1293`、`:1318-1322`）；condition gate 返回后（`:1294-1300`）、frame submission 返回后（`:1323-1335`）以及 frame 完成后（`:1384-1391`）都会重新检查总 deadline，迟到的 `true` 或成功 frame 不会被接受。
- pre-quit final evidence：`RunAndQuitAsync` 在 `QuitOnMainThreadAsync`（`:174`）之前同步写入 `final` evidence（`:158-172`）。pre-quit payload 使用 `pending`/`failed`、`quitStatus=pending`、`quitDrainSettled=false`、`quitDrainOutcome=not-observed`、`finalEvidencePhase=pre-quit`（`:144-151`），没有在 Quit 请求前伪造 `completed`。Quit 后才写入实际 quit/drain 结果并按业务退出码、Quit 状态、drain 状态和 evidence 写入失败共同决定最终状态（`:174-231`）。

### cleanup、failure latch、JSON、正常启动和边界

- `RunScenarioAsync` 在最终 cleanup 前先 drain 所有 detached operations（`:784-814`）；未收束或 drain 出错时明确设置 `terminalFailure`、`cleanupSkipped` 和原因，不继续调用 `RunManager.CleanUp`。收束后 cleanup 仍通过 `InvokeOnMainThreadWithTimeoutAsync`（`:818-883`），cleanup gate 自身超时也会进入新的 bounded drain。
- action 结果判定显式包含 `actionFailureLatched`（`:660-668`）；超时、取消、异常、评价失败和最终 action evidence 失败路径都会锁存失败并阻止把 action 作为成功（`:600-648`、`:670-719`、`:752-781`）。form gate 失败也会锁存并把场景置为 failed（`:688-700`）。场景级 `terminalFailure` 会在外层循环阻止继续推进下一场景（`:391-404`）。
- 场景 JSON 写入失败会设置 `status=failed`、`terminalFailure=true`、`evidenceWriteFailure`，记录 failure 并执行一次 retry（`:1689-1710`）；final JSON 在 pre-quit 和 post-quit 两个阶段都保留失败证据。写入路径经 `Path.GetFullPath` 后只接受 `G:\`/`G:/`，未见写入 C: 的旁路（`:1718-1753`）。本项只确认源码控制流，未确认实际文件解析。
- 正常启动路径由 `TryStart` 的请求参数门控和 `_started` 单次闩锁保护（`:47-60`）；无 `--form-native-smoke` 请求时直接返回，不会启动 smoke 或 Quit。当前文件中的引擎访问均通过 `InvokeOnMainThreadAsync`/`InvokeOnMainThreadWithTimeoutAsync` 或显式的 bounded operation/frame await；未发现用同步等待绕过主线程 gate 的路径。

## 进行中

- 本轮代码与报告的静态监督审查已完成；无剩余审查动作。
- 本报告只批准当前源码控制流的 PASS，不扩大为编译、测试、实机、JSON 文件或运行时线程行为通过。

## 未知

- 未构建，未知当前工作树是否存在编译问题。
- 未运行测试、未启动游戏，未知 Godot 实际 iteration 退出时序、真实 generic inner task 延迟、取消/故障组合和实际 JSON 文件解析结果。
- `FormStanceWatcherBridge.TryBind()` 的外部实现不在本次请求范围内；本报告只确认该调用在本文件中是启动阶段的绑定/可用性检查，未对外部实现内部的运行时线程安全作独立证明。
