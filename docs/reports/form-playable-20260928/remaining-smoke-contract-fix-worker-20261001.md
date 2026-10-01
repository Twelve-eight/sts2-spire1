# Form Native Smoke 剩余契约修复工作报告

- 日期: 2026-10-01
- 状态: `completed`（仅源码修改与静态回读；未构建、未测试、未部署、未启动游戏）
- 路由: `6.1sol`，经 `agentrouter`，使用当前继承模型；本任务未委派其它代理。
- 配对监督: 当前工具门禁为 `BLOCKED`；按用户指示未等待，不把监督缺失写成通过。
- 唯一可写产品文件: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`
- 报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\remaining-smoke-contract-fix-worker-20261001.md`

## 已确认

- 已读取本轮请求文件、`FormNativeSmokeRunner.cs`、独立审查报告 `remaining-smoke-contract-fix-independent-review-20261001.md`、前置审查报告和工作区入口约束。
- 已保留既有 `DetachedOperation` 嵌套任务修复：`Task<Task>` 仍通过 `Unwrap()` 展开；未改动普通启动路径和请求参数解析。
- `AwaitProcessFrame` 终止语义已修复：`WaitForConditionWithTimeoutAsync` 对已完成的 process-frame task 统一执行 `await` 观察；取消或故障被包装为 `TerminalOperationException`，不会继续下一场景。未完成的 frame task 仍注册为 `DetachedOperation`，继续使用 bounded detached drain。
- condition deadline 已修复：condition gate 与 process-frame submission gate 均传入 `BoundedMainThreadGateTimeout(remaining)`，语义为 `min(remaining, MainThreadGateSeconds)`；condition gate 返回后、frame submission gate 返回后以及 frame 完成后均重新检查总 deadline，迟到的 `true` 或成功 frame 不再被接受。
- final JSON 竞态已修复：`RunAndQuitAsync` 在调用 `QuitOnMainThreadAsync` 之前先构造并同步写入 `final` evidence；首次写入失败仍保留 `evidenceWriteFailure`、日志和一次 retry，然后才请求 `SceneTree.Quit`。
- pre-quit final 字段语义明确且不伪造退出结果：
  - `status`: 业务 `exitCode == 0` 时为 `pending`，否则为 `failed`；Quit 请求前绝不写 `completed`。
  - `exitCode`: 保留业务 smoke 退出码。
  - `quitStatus`: `pending`，表示 Quit 尚未请求/未观察。
  - `quitDrainSettled`: `false`，不声称 drain 已收束。
  - `quitDrainOutcome`: `not-observed`。
  - `finalEvidencePhase`: `pre-quit`。
- Quit 请求后的更新仍保留既有语义：记录实际 `quitStatus`，bounded drain 结果写入 `quitDrainSettled` 与 `quitDrainOutcome`（`settled`/`not-settled`），可迁移为 `quitDetachedOperations`；`quitDrainFailure`、`failure`、`evidenceWriteFailure` 和 final JSON retry 仍保留。只有没有 `evidenceWriteFailure`、业务退出码为 0、Quit 返回 `executed*` 且 drain settled 时才把状态更新为 `completed`。
- 本轮新增问题已由源码与独立审查报告复核：`DetachedOperation` 原先只展开精确 `Task<Task>`；`InvokeOnMainThreadWithTimeoutAsync<T>` 对 `Task<RunState>`、`Task<AbstractRoom>`、`Task<float>`、`Task<CardPileAddResult>` 等返回值形成 `Task<Task<T>>`，迟到 gate 的 detached drain 不能等待底层 operation。
- generic nested-task 修复已落盘：gate timeout 现在调用 `CreateDetachedTaskDrain<T>`；当 `typeof(Task).IsAssignableFrom(typeof(T))` 时，`AwaitNestedTaskAsync<T>` 先 `await` outer `Task<T>`，再把返回的 `Task` 作为 inner 继续 `await`；普通非 Task 返回值仍直接保留 outer task。
- 受影响泛型 API 已覆盖同一 gate timeout 控制流：`StartNewSingleplayerRun` 的 `Task<RunState>`（调用锚点 `:472`）、`EnterRoomDebug` 的 `Task<AbstractRoom>`（`:508`）、`CardPileCmd.Add` 的 `Task<CardPileAddResult>`（`:554`）和 `AwaitProcessFrame` 的 `Task<float>`（`:1318`）。
- 控制流不变量已保留：非 Task 返回值不展开；所有 nested drain 继续由 `DrainDetachedOperationsAsync` 以 bounded deadline 观察；超时仍标记 detached operation 并阻止后续 cleanup/场景推进；现有精确 `Task<Task>.Unwrap()` 兼容逻辑仍在 `:1523`。
- 静态回读结果：`CreateDetachedTaskDrain<T>` 位于 `:1436-1443`，outer/inner 两层 await 位于 `:1445-1451`，gate timeout 接入位于 `:1466-1468`；final evidence 的 pre-quit 写入 `:159` 早于 Quit 调用 `:174`；`GENERIC_NESTED_STATIC_READBACK=PASS`。未运行编译器或测试命令。

## 进行中

- 本轮源码实现与静态回读已完成；无剩余白名单源码修改。
- 后续如获单独授权，可再做构建、运行时 nested-task 迟到回调、退出竞态、取消/故障 frame 和实际 JSON 可解析性验证；本轮严格不执行这些动作。

## 未知

- 未构建，未知当前工作树是否存在与本轮无关的编译问题。
- 未运行测试、未启动游戏，未知 Godot 实际 iteration 退出时序和真实 generic inner task 延迟下的运行结果；本报告只声明源码与静态控制流证据。
- 若报告目录本身不可写，代码只能按既有 `evidenceWriteFailure` 与 retry 语义记录失败，不能把文件系统写入失败伪称为可解析 evidence。