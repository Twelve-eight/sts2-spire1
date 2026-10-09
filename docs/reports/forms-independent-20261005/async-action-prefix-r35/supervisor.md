# r35 同批监督窄审（PlayPrefix Task 边界）

## 已确认

- 2026-10-05 13:52（Asia/Shanghai）：已读取 `central-gate-notice.request.md` 与 `gate-notice.json`。本批 Worker `01a10a98-a015-79a0-9844-263a4f37554e`，`Status=completed`、`TimedOut=false`、`NativeTool=multi_agent_v1.wait_agent`、`WorkerResult=CODE_COMPLETE`；门禁成立，未用 r33 门禁替代。
- `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs` 当前 SHA256 = `5088D2E6E906CF7B22EF0F95B42A60EFBC09170E871E419A89801E391986C929`，与 gate-notice 记录一致。
- 无其它 delta：`FormStanceMode.cs` = `15271CF355E6303C09D0FCE56286CF5137385AD788ABEB48D1871CCE490F5034`、`FormStanceModifier.cs` = `CDA047A214E93CBDBFB21C524A2DEC79204ABCC4157CB0F778F8600DE824F5E2`、`MainFile.cs` = `156227F421F446480930E1C184182C86C641746D8DAED9908A6A8BF254A14F8C`，三者与 r33 完全一致，未被本批触碰。
- 本文件相对 r33 只改两处：第 3 行新增 `using System.Threading.Tasks;`；`PlayPrefix`（现第 186-202 行）改为 `private static bool PlayPrefix(PlayCardAction __instance, ref Task __result)`。`RemovePrefix`（第 168-176 行）、`EnsureInstalled`（第 56-121 行）、`ProofHoldsLocked`（第 123-136 行）、`HasExactlyOnePrefixLocked`（第 138-150 行）、owner `Forms.FormStanceSafety`（第 26 行）、两 target 解析（第 63-79 行）均与 r33 逐字一致，未放宽 Remove/live/teardown 守卫。
- 同一项目已存在经前轮使用的同型模式：`FormStanceWatcherBridge.cs` 第 1049-1054 行 `KeepDivinityUntilNextTurnPrefix(PowerModel __instance, ref Task __result)` 与第 1057-1063 行 `StanceChangedPrefix(Player __0, ref Task __result)` 均为「prefix 返回 false + 赋 `ref Task __result`」跳过原方法，证明本仓库 Harmony 版本支持该返回/注入形态。
- `PlayPrefix` 边界正确性（逐条对照请求）：
  - 普通/未命中分支 `return true`（第 201 行）→ 原 `ExecuteAction()` 方法体照常执行。
  - 命中不可用分支：`Task.FromException(exception)`（第 198 行）赋给 `ref Task __result`，随后 `return false`（第 199 行）跳过原方法体——**有** `return false`，非“只返回 Task”。
  - 异常对象同一：`catch (Exception exception)`（第 192 行）直接把捕获对象交给 `Task.FromException`，未包装、未改文本、未吞掉；异常仍以 fault 形式对外可见（非 Cancelled/Pending/通过）。
  - 该 `catch` 覆盖“检测本身抛异常”的场景，正是 `worker.request.md` 明确允许的「或该检测发生异常时返回 Task.FromException 原异常对象并 return false」；非超出授权的行为。

## 已确认（官方控制流核对，GameAction 可结算 Finished/AfterFinished）

- 权威入口 `MegaCrit.Sts2.Core.GameActions\GameAction.cs`：`Execute()` 第 116 行；第 125 行 `_executionTask = TaskHelper.RunSafely(ExecuteAction())` 在 `try`（第 135 行）之外。r33 缺陷是 `ExecuteAction()` 被 prefix 同步抛出，导致第 125 行本身抛出、`_executionTask` 未赋值。r35 后 prefix 不再同步抛出：Harmony 跳过原方法并返回 faulted `Task`，第 125 行正常赋值得 `_executionTask`。
- `TaskHelper.cs` 第 21-42 行：`RunSafely` → `LogTaskExceptions`，`await task`（第 30 行）对 faulted Task 抛出其异常（单异常经 await 解包为原 `InvalidOperationException` 对象），`catch` 第 32-40 行记录日志/Sentry/触发 `UnobservedFault` 后 `throw;` 重抛同一对象；故 `_executionTask` 为 faulted，`_executionTask.Exception`（`GameAction.cs` 第 63 行）为该异常的 `AggregateException` 形态——与 r30 权威证据 `actionFaultRootType=System.AggregateException`、内层 `InvalidOperationException(Forms unavailable...)` 同形。
- `GameAction.Execute()` 第 135-166 行：第 137 行 `await TaskHelper.WhenAny(_executionTask, _pauseForPlayerChoiceTaskSource.Task)` 在第 135 行 `try` 内，faulted `_executionTask` 在此抛出 → 第 139 行 `finally` 执行：`_executionTask.IsCompleted`（faulted 亦为 completed）为真 → 第 145 行 `State=Finished`、第 146 行 `JustBeforeFinished`、第 147 行 `_completionSource.TrySetResult()`（CompletionTask 结算）、第 148 行 `AfterFinished`。异常无 catch 兜底，随后从 `Execute()` 传出为 faulted Task。
- 队列结算：`GameAction.cs` 第 112 行 `AfterFinished += afterFinished`（即 `ActionQueueSet.PopAction`）。`ActionQueueSet.cs` 第 560-605 行 `PopAction` 从队首移除该 action，并在 `action.Exception != null` 时第 597 行 `SetException`。`ActionExecutor.cs` 第 151 行 `Task actionTask = readyAction.Execute()` 得 faulted Task；第 152 行 `!actionTask.IsCompleted` 为假直接跳过等待，第 156-159 行记录 fault，第 180-182 行解绑并 `GetReadyAction()` 取下一个 action。r33 的「队首残留 `Executing` → `ActionQueueSet.cs` 第 228-230 行 `InvalidOperationException`、队列无法推进」路径被消除。

## 进行中

- 无。3 分钟窄审范围（PlayPrefix Task 边界 + 原两 target/owner 安装健康证明保持）已完成。

## 未知

- 未编译、未 lint、未测试、未实机：本窄审按请求只做源码级静态核对，非运行时复现。
- 未运行时验证：Terminal/Shutdown 下 prefix 的真实 fault 结算、owner/target 运行时计数（应 owner 2、单 target 各 1）、无能量/HP/marker 副作用、普通 Bound/非 Forms 局控制，均由 hub/r34 集中验证。
- r34 新测试与新控制器尚未冻结，本结论不以其为生产证据。

## 结论

- 状态：**SUPERVISION_PASS（静态）**。
- 通过项：PlayPrefix 不再同步 throw；true 分支原方法执行；false 分支 `ref Task __result = Task.FromException(同一异常对象)` 且确有 `return false`；异常对象同一、未包装/未吞；GameAction 可进入原 await/try/finally/Finished/AfterFinished 并完成 CompletionTask/PopAction 队列结算；RemovePrefix 与原两 target/owner 原子安装/回滚/`IsInstalled` 健康证明保持不变；无其它文件 delta、无新 owner/硬引用/线程/资源。
- 未发现 P1/P2 阻塞项；本结论仅为静态源码级，不代表实机通过。