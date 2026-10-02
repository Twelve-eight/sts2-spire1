# Void transaction identity worker, rework 5

- 日期：2026-10-03
- 范围：仅修复 native in-flight cancel/completion race probe 的时序；不修改 production transaction logic。
- 实现报告路径：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-rework5-20261002.md`
- 前置审查：只读取 `void-transaction-identity-reviewer-rework4-20261002.md` 与 `tools/form-effects-probe/TransactionScenarios.cs`。
- 本轮限制：不构建、不测试、不部署、不启动游戏。

## 已确认（编辑前证据）

- 当前 race 场景为 `transaction.void_native_inflight_cancel_completion_race_is_idempotent`，位于 `TransactionScenarios.cs` 约第 390 行。
- `completion` 由 `ObserveNativeActionCompletionForPatch(completionGate.Task, action)` 创建，`completionGate` 使用 `RunContinuationsAsynchronously`，随后以 `Check.False(completion.IsCompleted, ...)` 确认 gate 尚未释放。
- 编辑前源码在约第 419-430 行同时启动 `cancel` 与 `release` 两个 `Task.Run`，二者都等待 `raceStart.Task`，随后 `Task.WhenAll(cancel, release)`；源码没有 cancel 完成栅栏，因而 `completionGate.SetResult(true)` 可能早于 `CancelNativeActionForPatch(action)` 返回。
- 既有 race 场景的后续断言位于约第 433-454 行，覆盖双入口重复清理、同卡下一 generation 消费、以及旧 action 延迟清理不回滚成功的 `AfterCardPlayed` allowance；这些断言必须保留。

## 进行中

- 将无序的两个并发 task 改为确定的 cancel-first 顺序：observer 先保持在未完成的 `completionGate.Task` 上；随后同步完成 `CancelNativeActionForPatch(action)`；确认调用返回后才释放 `completionGate`；最后等待 observer。
- 仅编辑 `TransactionScenarios.cs`，不编辑 production transaction 文件。

## 未知

- 本轮编辑前未构建、未运行 probe，尚无编译或运行时证据。
- Harmony 实际安装、真实 action queue 时序及隔离游戏行为留待主会话中央验证。
## 已完成（编辑后证据）

- 已将 `TransactionScenarios.cs:401` 的 race 起点改为单一确定顺序，不再创建两个无序并发 `Task.Run`，也不再使用 `raceStart` 或 `Task.WhenAll(cancel, release)`。
- 确切源码顺序如下（以编辑后文件行号为准）：
  1. `:411-414` 调用 `ObserveNativeActionCompletionForPatch(completionGate.Task, action)`，其中 `completionGate.Task` 尚未完成；`:414` 的 `Check.False(completion.IsCompleted, ...)` 确认 observer 尚未越过 gate。
  2. `:419` 同步调用 `CancelNativeActionForPatch(action)`；该调用返回即构成 cancel-first 完成栅栏。
  3. 只有在 `:419` 返回后，`:420` 才执行 `completionGate.SetResult(true)`。
  4. `:421` 最后 `await completion`，等待 completion observer 继续并完成清理。
- 双入口幂等断言仍在 `:423-428`：重复执行 native cancel 与 native completion，并检查 token/reservation 只被清理一次。
- 同卡后续 generation 消费断言仍在 `:430-437`。
- 旧 action 延迟清理不得回滚已成功 `AfterCardPlayed` allowance 的断言仍在 `:439-444`。
- 本 probe 没有使用 `ProbeEndAction` 作为 native callback 证据；仍直接调用 production 的 `CancelNativeActionForPatch` 与 `ObserveNativeActionCompletionForPatch`。
- 已执行 `git diff --check`：退出码 0；输出仅为既有 LF/CRLF 转换警告，无 whitespace error。

## 未知

- 本轮仍未构建、未运行 probe，未产生编译、运行时、Harmony 安装或隔离游戏证据。