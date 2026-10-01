
## 本轮只读审查增量 - 检查面 0（2026-10-02）

### 已确认
- 已读取 `G:\omp works\AGENTS.md`、`G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md`、`G:\omp works\.tooling\subagent-report-protocol.md` 和实现报告 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-worker-r12-20261001.md`。
- 该实现报告把本轮证据限定为源码/研究资料静态证据，并明确记载未构建、未测试、未部署、未启动游戏；因此当前不能把任何主线程门禁、cleanup 或 quit 行为写成实机证据。

### 进行中
- 独立回读唯一范围文件 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`，逐项核对主线程 API、deferred completion、状态读取、cleanup、drain、失败分支和最终 quit 的行号与控制流。

### 未知
- 尚未形成对目标源码的独立 PASS/REWORK 结论；真实运行时的 deferred callback 调度、非主线程 continuation、gate 失败、cleanup skipped 后退出和最终 JSON 落盘仍未知。

## 本轮只读审查增量 - 检查面 1：主线程 API 与本地 completion（2026-10-02）

### 已确认
- `NGame._Ready` 在 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes\NGame.cs:554` 保存 `Environment.CurrentManagedThreadId`；`NGame.IsMainThread()` 在同文件 `:739-747` 以当前 managed thread id 比较该值。目标文件只在 `FormNativeSmokeRunner.cs:845` 使用该门禁判断。
- `AwaitProcessFrame` 的权威研究源码为 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Nodes.GodotExtensions\NodeUtil.cs:16-30`：返回 `Task<float>`，读取 `GetTreeOrNull`，等待 `SceneTree.ProcessFrame`，再读取节点有效性与 process delta。目标文件在 `:827-830` 只把 `game.AwaitProcessFrame()` 的创建提交到主线程 gate。
- Godot 4.5.1 XML 元数据 `G:\omp works\Sts\sts2-spire1\.nuget\godotsharp\4.5.1\lib\net8.0\GodotSharp.xml:371-376,473-479` 确认 `Callable.From(System.Action)` 构造 `Callable`，`Callable.CallDeferred(Godot.Variant[])` 是在 idle frame 延迟调用且无 `<returns>`；只读反射也确认该方法返回 `System.Void`。因此 `CallDeferred` 本身不是 completion task。
- 目标文件 `FormNativeSmokeRunner.cs:843-876` 的本地 completion 证据明确：主线程直接执行后用 `Task.FromResult` 包装同步结果；非主线程创建 `TaskCompletionSource<T>(RunContinuationsAsynchronously)`，`Callable.From(...).CallDeferred()` 仅负责提交，deferred callback 通过 `TrySetResult` 或 `TrySetException` 完成本地 task，提交异常在 `:872-875` 转成 `MainThreadGateException`。这不是把 `CallDeferred` 的返回值误当成 task。
- `FormNativeSmokeRunner.cs:879-891` 对本地 invocation task 和十秒 `Task.Delay` 做 `Task.WhenAny`；超时被标为 gate phase `timeout`。静态扫描未发现 `Task.Run`；唯一显式 `TaskScheduler.Default` 在 `:1013-1038` 的 detached observer，不调用 Godot API。

### 进行中
- 需要继续把所有 Godot 状态读取、`RunManager.CleanUp`、`SceneTree.Quit`、process-frame timeout、detached drain 与最终 JSON 状态串成完整控制流。

### 未知
- `AwaitProcessFrame` 在 `ToSignal` 返回后的实际 continuation 是否由目标运行时稳定恢复到 Godot 主线程，当前只有源码形状证据，没有实机证据。
- `CallDeferred` callback 超过 `InvokeOnMainThreadWithTimeoutAsync` 十秒边界后的实际调度与副作用未知；当前 gate timeout 不会把该 callback 纳入 `terminalOperations`，这一静态边界将在下一检查面核对其是否造成 cleanup/quit 重入风险。


## 本轮只读审查增量 - 检查面 2：状态读取、cleanup、drain 与 quit（2026-10-02）

### 已确认
- 目标文件中两处 `RunManager.CleanUp` 只有 `FormNativeSmokeRunner.cs:291-300` 和 `:575-586`，均在 `InvokeOnMainThreadWithTimeoutAsync` callback 内。发行版 `RunManager.CleanUp` 研究源码 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Runs\RunManager.cs:1569-1615` 会访问 `NAudioManager`、多个 UI Node、`CombatManager`、同步器和 `State`，因此这两个调用经过 gate 的静态边界成立。
- 目标文件的 Godot/game 状态读取和调用按静态回读均放在 gate callback: startup task 读取 `:178-181`; `RunManager.Instance`、run state、player、combat phase、target、snapshot、card、action 创建和 enqueue 为 `:279-414`; action 状态、`CompletionTask` 和 `Exception` 判定为 `:458-480,608-688`; process-frame 创建为 `:812-840`。未发现目标文件中 `RunManager`、`CombatManager`、`Player`、`Snapshot` 或 `GameAction` 状态读取脱离这些 callback 的直接调用。
- `SceneTree.Quit` 只有 `FormNativeSmokeRunner.cs:894-912` 这一处，实际 `game.GetTree().Quit(exitCode)` 在 `:898-904` 的 gate callback 内。`GodotSharp.xml:208590-208595` 的权威元数据说明 `SceneTree.Quit(int)` 是在当前 iteration 结束时退出，因此 `quitStatus` 只能证明 callback 已执行或 gate 失败，不能证明进程已经退出。
- terminal run/room/card task 分别在 `:313-328`、`:345-357`、`:383-391` 以 `terminalOnFailure:true` 加入 `terminalOperations`; `DrainDetachedOperationsAsync` 在 `:1041-1096` 共享十秒 deadline, 记录 `completed`、`task-reported-canceled`、`faulted` 或 `isolated-unfinished-operation`，并明确 `underlyingCancellationRequested:false`。`RunScenarioAsync` 在 `:520-549,573-602` 只有 drain 成功才允许最终 cleanup; 未收束或 drain 异常会写 `cleanup: skipped`、`cleanupSkipped:true` 并阻止 cleanup。
- 非主线程 observer 的唯一显式 continuation 为 `FormNativeSmokeRunner.cs:1013-1038`; 它只更新 `DetachedOperation` 并记录错误，不访问 Godot 对象。目标文件没有 `Task.Run`。

### 已确认的 REWORK 风险
- [P1] 主线程 gate timeout 没有收束已提交 callback。`FormNativeSmokeRunner.cs:857-876` 创建的 `TaskCompletionSource` 只存在于 `InvokeOnMainThreadAsync` 局部变量; `:883-891` 的 timeout 返回后不保存 `Callable`、`completion.Task` 或 callback handle, 也没有取消/排队 drain。因而 callback 可能在 caller 已记录 `MainThreadGateException(... at timeout)`、已进入 cleanup 或已提交 quit 后才执行; 若 callback 随后抛错, `TrySetException` 的结果也没有被观察。submit、callback 在 timeout 之前的异常分别有 `:872-875`、`:866-869` 证据, 但 timeout 之后的 callback outcome 没有明确证据。
- [P1] process-frame timeout 没有进入本轮 detached drain。`FormNativeSmokeRunner.cs:827-837` 在 `AwaitProcessFrame` 的 `frameTask` 超时后只创建临时 `DetachedOperation` 交给 `ObserveDetachedTask`, 没有加入 `terminalOperations` 或 `result`; 而最终 drain 只消费 `:521-525` 的 `terminalOperations`。因此该路径仍会以 `cleanupAllowed == true` 进入 `:573-596` 的 final cleanup, 不能证明 cleanup 前 frame task 已收束, 也不会产生 `detachedOperations`/`cleanupSkipped` 证据。
- [P2] startup 和 action completion 的 timeout 也不进入同一 drain: `WaitWithTimeoutAsync` 的 startup task 在 `:801-810` 调用 `AwaitOperationWithTimeoutAsync` 时不传 `detachedOperations`; action 在 `:417-420` 超时后由 `:424-439` 请求取消, 但 `CompletionTask` 没有被加入 bounded drain。该静态边界不能证明 cleanup 前这些异步操作已经停止。

### 未知
- 未构建、未测试、未部署、未启动游戏。真实 deferred callback 是否在 gate timeout 后重入, `AwaitProcessFrame` task 是否会在 cleanup 前结束, `cleanup skipped` 分支是否按预期到达最终 quit, 以及 `SceneTree.Quit` 后 final JSON 是否在进程退出前落盘, 均未知。
- `quitStatus` 的 `executed-main-thread` 或 `executed-deferred-main-thread` 是调用路径状态, 不是操作系统进程已退出状态; 这点只能由源码和 Godot API 文档确认, 不能写成实机退出证据。


## 本轮只读审查增量 - 检查面 3：失败证据、非主线程 continuation 与最终 quit（2026-10-02）

### 已确认
- gate 的 submit/callback/timeout 三类静态错误路径分别存在明确 phase: `FormNativeSmokeRunner.cs:872-875` 是 `submit`; `:866-869` 是 deferred callback exception; `:883-891` 是 `timeout`。场景通用 catch 在 `:502-516` 将 `MainThreadGateException` 或 `TimeoutException` 变为 `terminalFailure`; final cleanup catch 在 `:589-596` 写入 `cleanupFailure`; final quit catch 在 `:909-912` 返回 `failed: ...`。
- `RunAsync` 在 `:183-190` 处理 startup gate/operation 失败; scenario result 在 `:242-251` 写入 JSON 并在 `terminalFailure == true` 时停止后续场景。因此已有失败路径不会静默继续到下一个场景, 但 gate timeout 后遗留 callback 的后续 outcome 仍不在该结果中。
- `RunAndQuitAsync` 的 final JSON 在 `:140-152` 同时写 `exitCode` 和 `quitStatus`; `status` 仅按 `quitStatus.StartsWith("executed")` 判定。它能明确区分 quit gate 成功和失败, 但 `status == completed` 不是 smoke 通过, 因为 `exitCode` 仍可能是 `1`。
- `TaskCompletionSource` 使用 `RunContinuationsAsynchronously` (`:857`), 所以 deferred callback 中的 `TrySetResult` 不要求把 `RunScenarioAsync` continuation inline 到 Godot callback。后续 continuation 可以在非主线程运行, 而目标文件的 game/Godot 访问点仍通过 `InvokeOnMainThreadWithTimeoutAsync`; `TaskScheduler.Default` observer (`:1037`) 不调用 Godot。

### 进行中
- 本轮已完成源码证据串联; 仅保留实际运行时调度、遗留 callback 收束和最终退出时序为未验证边界。

### 未知
- 没有可在本轮执行的实机复现命令: 按用户要求禁止构建、测试、部署、启动游戏。可复核的只读静态检查是对目标文件检索 `InvokeOnMainThreadWithTimeoutAsync`、`AwaitProcessFrame`、`RunManager.CleanUp`、`SceneTree.Quit`、`Task.Run` 和 `terminalOperations` 的行号, 其结果已记录在检查面 1 和 2。
- `RunContinuationsAsynchronously` 只约束本地 gate completion 的 continuation 调度, 不证明 `AwaitProcessFrame` 内部 `ToSignal` continuation、run/room/card task 或 action task 的所有内部 continuation 都在 Godot 主线程; 这需要发行版运行时证据。

## 审查结论

- 本审查范围结论: **REWORK**。
- 通过部分: `NGame.IsMainThread`、`AwaitProcessFrame` 的真实签名和 `Callable.CallDeferred` 的无返回值语义有静态证据; local `TaskCompletionSource` 形状正确; `RunManager.CleanUp`、`SceneTree.Quit` 和目标文件列出的主要 Godot 状态读取均经过 gate; 未使用 `Task.Run`; terminal run/room/card task 有 bounded drain 和 `cleanup skipped` 证据。
- 必须修正的边界: (1) `InvokeOnMainThreadWithTimeoutAsync` 超时后没有追踪或有界收束已提交 deferred callback; (2) `AwaitProcessFrame` 超时产生的 `frameTask` 没有进入 `terminalOperations` 的 drain, 仍可在 `cleanupAllowed == true` 时进入 cleanup; (3) startup/action completion timeout 没有统一的遗留 task 收束证据。至少前两项使当前源码不能证明 cleanup/quit 不会与延迟主线程 callback 或 frame task 重入。
- 最小修复范围: 为每次 deferred gate 保留可观察的 callback completion 和超时后的隔离状态, 在 callback 迟到时记录 outcome 并禁止或隔离原 operation; 将 process-frame task 纳入同一 bounded drain, drain 失败时沿用 `cleanup skipped`; 对 startup/action completion 明确同样的 drain 或证明其超时后不会再访问 Godot/game 状态。此处只给出审查结论, 本轮未改代码。
- 真实运行仍未知: 本轮没有构建、测试、部署或启动游戏, 没有产生实机 JSON, 没有验证 gate submit/callback/timeout、drain timeout、cleanup skipped、最终 `SceneTree.Quit` 或进程退出时序。以上结论全部是源码、研究源码、GodotSharp XML/元数据的静态证据。

状态: completed
