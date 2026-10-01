# 姿态形态真实战斗 smoke runner r15 实现报告

- 日期: 2026-10-01
- 角色: bounded implementation
- 用户指定模型: 6.1sol
- 用户指定路由: agentrouter
- 限制: 只做源码静态实现与回读；不构建、不测试、不部署、不启动游戏。
- 产品白名单: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`、`G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs`

## 已确认

### 检查面 0：读取 r15 请求与 r14 状态

已读取 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\worker-form-smoke-r15-request.txt`、r14 实现报告和 r14 监督报告。r15 要求在当前源码上完成 A-F，尤其是逐点回读全部主线程 gate、条件等待、JSON 写入调用点和 detached operation 收束；不得把静态证据写成实机通过。

当前源码行数为 1530；r14 报告仅记录部分缺口，不能假设所有调用点已传递 detached list，必须以当前落盘源码为准。

## 进行中

- 正在分段读取 `FormNativeSmokeRunner.cs` 的 action、scenario、等待和收尾控制流，并建立所有相关调用点清单。

## 未知

- 未构建、未测试、未部署、未启动游戏；目标二进制中 Godot deferred 调度、真实 action 时序、形态效果和最终退出/JSON 时序均未知。

### 检查面 1：当前调用点静态发现

已逐个读取当前源码中的调用点。确认 r14 落盘状态仍有可直接修复的签名/收束缺口：

- 多处调用已经传入 `detachedOperations`，但 `InvokeOnMainThreadWithTimeoutAsync` 当前签名只有 `(operation, description)`，导致当前源码调用契约不一致；r15 将把 detached 列表设为必传，强制覆盖全部调用点。
- `WaitWithTimeoutAsync` 的调用已传第四个参数，但方法定义没有该参数；startup operation 的主线程提交和 completion task 需要统一进入 `startupOperations`。
- `WaitForConditionWithTimeoutAsync` 当前没有 detached 列表参数；process-frame timeout 只观察临时对象，不进入 scenario 的 `terminalOperations`。
- `RunScenarioAsync` 在创建 `terminalOperations` 前读取 `RunManager.Instance`，该早期 gate timeout 可能无法进入本场景 finally drain。
- `QuitOnMainThreadAsync` 当前没有接收 `quitOperations`，最终 quit deferred gate timeout 不能进入 quit bounded drain。
- action 的终态字段已存在 `RecordActionEvidence`/`DeriveActionStatus`，但 r15 仍需把最终 evidence gate 放在统一 drain 之前，避免其 timeout 后新增 detached operation 未被收束。

以上均为源码静态证据；尚未构建或运行。

## 已确认

### 检查面 2：r15 deferred gate 与条件等待实现

已完成源码修改，仍未构建/测试：

- `InvokeOnMainThreadWithTimeoutAsync` 现在要求调用方传入 `List<DetachedOperation>`；gate timeout 会把 deferred completion task 封装为 `DetachedOperation`，登记、观察并抛出带 detached 引用的 `MainThreadGateException`。未声称可以取消 `Callable` 或 underlying task。
- `WaitWithTimeoutAsync` 现在接收 detached 列表，并把主线程提交和 operation completion 都交给同一 bounded drain；startup 使用 `startupOperations`。
- `WaitForConditionWithTimeoutAsync` 现在接收 scenario 的 `terminalOperations`；condition gate、process-frame gate 都登记 detached task；process-frame 等待超时不再只做临时观察，而是进入 terminal drain 并抛出 `TerminalOperationException`。
- `RunScenarioAsync` 将 `RunManager` 获取移入 try/finally 覆盖范围；scenario 内的全部主线程 gate 和三类条件等待均传入 `terminalOperations`。
- `QuitOnMainThreadAsync` 接收 `quitOperations`，最终 quit deferred gate timeout 进入 quit bounded drain。
- 最终 action evidence 已移动到 terminal drain 之前；其 evidence gate timeout 新增的 detached operation 会被同一轮 drain 收束。
- final cleanup gate 的 detached task 也会在 cleanup 异常后执行独立 bounded drain；未收束时明确写 `cleanup=skipped`、`cleanupSkipped=true` 和隔离原因。

上述是源码静态实现证据；尚无真实调度或进程退出证据。

### 检查面 3：action evidence、场景状态与收尾顺序

- `actionPassed` 的异常评估路径、State 非 `Finished` 路径和新增 `TaskHelper.UnobservedFault` 路径都先把 `actionFailureLatched` 设为 `true`；失败原因写入同一个 `currentActionResult["failure"]`，随后再次调用 `RecordActionEvidenceAsync`，因此 `cardPlay.failure` 与场景 `result.failure` 使用同源 action evidence。
- `RecordActionEvidence` 依据 action `State`、`CompletionTask`、cancelled、exception 和 failure 派生 `status`；初始状态为 `pending`，不会固定为 `enqueued`。超时、取消、fault、completed 和普通 failed 分支具有可区分终态。
- final payload 仍以 `exitCode == 0`、`quitStatus` 以 `executed` 开头且 quit detached drain settled 三者合取后才写 `completed`；业务失败不会因 `SceneTree.Quit` 调用成功而变成完成。
- 最终 action evidence 现在先于 scenario terminal drain；evidence gate 自身 timeout 会进入该 drain。只有 bounded drain 后才允许最终 `RunManager.CleanUp`；cleanup gate 的迟到 callback 也有独立 bounded drain，未收束则写 `cleanup=skipped`。

### 检查面 4：JSON evidence 失败传播

- `WriteJsonIfConfigured` 已返回 `JsonWriteResult`；未配置、路径不在 G:、目录创建、序列化和写文件异常均返回失败并记录原因。
- `TryWriteScenarioEvidence` 首次写入失败后把 scenario 标记为 failed/terminalFailure/evidenceWriteFailure，追加 failure 并尝试一次失败态重写；无论重写结果如何返回 false，`RunAsync` 将其作为 `sharedFailure` 并返回非零，阻止后续场景。
- invalid/startup/bridge 分支的 scenario evidence failure 也会返回非零；final evidence failure 将 final payload 改为 failed、记录 evidenceWriteFailure 和 failure 并记录日志。受 `SceneTree.Quit` 调用顺序限制，无法在 finally 内回写已经传给 Godot 的 OS exit code；该边界仍需实机确认。

以上均为源码静态证据；未构建或运行。


## 已确认

### 检查面 5：逐个调用点回读与白名单核对

静态回读结果：

- `InvokeOnMainThreadWithTimeoutAsync` 调用点均为三参数形式，当前行号集合覆盖 setup（318-329）、scenario（417-659、798-808）、cancel/evidence（889-927）、condition/helper（1232-1271）和 final quit（1352-1359）；每个调用均传入对应的 `setupOperations`、`terminalOperations`、`quitOperations` 或 helper 接收的列表。
- `WaitWithTimeoutAsync` 仅由 startup 调用（238-242），传入 `startupOperations`；定义（1226-1242）同时登记主线程 gate 和 completion task。
- `WaitForConditionWithTimeoutAsync` 的三个 scenario 调用（465-470、498-503、504-509）均传入 `terminalOperations`；定义（1244-1288）把 process-frame timeout 登记到该列表。
- `WriteJsonIfConfigured` 调用点（194、202、227、1563、1575）均读取 `JsonWriteResult`；scenario 写入失败由 `TryWriteScenarioEvidence` 返回 false 并让 `RunAsync` 返回非零。
- `FormNativeSmokePatch.cs` 保持原有 `NGame._Ready` postfix，仅调用 `FormNativeSmokeRunner.TryStart`；没有新增入口、Power 直接注入、TestMode、autoslay 或伪造 Hook。
- 已执行源码级括号/分隔符平衡扫描，结果为 `delimiter-scan: balanced`。这不是构建或测试；本轮未运行编译器、测试、游戏或部署。

## 进行中

- r15 白名单实现已落盘；没有待继续的源码修改面。
- 待后续中央流程执行构建、部署隔离检查和真实 smoke；这些不属于本轮，且本轮明确禁止执行。

## 未知

- 未构建，不能把当前源码称为可编译产物。
- 未启动游戏，未知目标二进制中 `Callable.CallDeferred`、`AwaitProcessFrame`、run/room/action completion、cancel、cleanup、quit 和 bounded drain 的真实时序。
- 未验证三场景真实 Watcher 卡牌是否在运行时自然进入预期 native stance、carrier 和两个 effect；r13 gate 仅为源码静态保留。
- 未验证 JSON 文件权限、进程退出前 final evidence 是否完整落盘；final evidence 写入失败时，代码可记录失败并把 payload 改为 failed，但无法在 `SceneTree.Quit` 已调用后回写操作系统 exit code。

## 结论

r15 已完成允许范围内的静态收尾：action failure/status 一致性、final status 合取、JSON 写入失败传播、所有主线程 gate/condition/evidence/quit 调用点的 detached registration，以及未收束时阻止 cleanup 的 bounded drain 语义均已落盘。验证状态仍为“源码静态完成，实机未知”，不能据此宣称姿态形态 mod 已最终可玩。
