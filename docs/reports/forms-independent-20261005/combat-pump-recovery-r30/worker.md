# r30 生产消费者窄修 worker

范围: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs, FormStanceBridgePump.cs
唯一报告: 本文件. 无构建/测试/游戏/委派.

## 已确认

### P0 — pump 节点被 Godot 静默拒绝入树, 消费者永不运行 (根因)

- 绝对路径与行号: G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs (修复前 411-447 行, 关键 `root.AddChild(pump)` 在 434 行).
- 触发条件: 生产桥在 `ModelDb.Init` postfix (`FormStanceModePatch.AfterModelDbInit`) 内首次 `TryBind()` -> `StartPumpLocked()`. 此时 NGame 正处在 `_EnterTree` 设置子节点阶段.
- 权威契约: Godot 引擎日志 G:\omp works\.tmp\forms-independent-20261005\native-r18-terminal-r27\b1-terminal\appdata\SlayTheSpire2\logs\godot.log:343-350 — `ERROR: Parent node is busy setting up children, add_child() failed. Consider using add_child.call_deferred(child) instead.`, C# backtrace `[2] StartPumpLocked` -> `[3] TryBind` -> `[4] TryBindOnMainThreadEntry` -> `[5] ModelDb.Init_Patch6` -> `[7] NGame.<GameStartup>`. GodotSharp 权威 API: G:\.nuget\packages\godotsharp\4.5.1\lib\net8.0\GodotSharp.xml:3807 (`GodotObject.CallDeferred` 在 idle 执行), :473 (`Callable.CallDeferred`), :5286 (`Node.IsInsideTree`), :5659 (`Node.QueueFree`).
- 当前控制流 (修复前): `root.AddChild(pump)` 被拒后 **不抛异常**, 原 catch (:445) 未触发 (日志中无 `Forms: main-thread pump install failed`). 但 `_pumpNode = pump` 仍在 :435 赋值, 节点却不在 SceneTree 内, 因此 `FormStanceBridgePump._Ready` (设置 `ProcessMode.Always`) 与 `_Process` (`PumpTick`) 永不运行.
- 后果: 重复 `Assembly.Load` Watcher 后 `OnAssemblyLoad` 仅置 `_assemblyLoadPending`/epoch (:389-391), 没有主线程消费者把它收敛. 与 BASELINE 一致: 40s 仍 pending, `bridgeRemoved=false`, 两 probe 未执行, `safetyPassed=false` (result.json / forms-binding-loss-binding-loss-terminal.json). 另 `TryBindOnMainThreadEntry` 的 `StartPumpLocked()` (:265) 因 `_pumpNode != null` (:413) 直接返回, 消费者无法自愈.
- 已排除: 不是 `ProcessMode.Always` 无效 (节点根本不在树中); 不是 AssemblyLoad 未订阅 (日志 :367 Bound 后已订阅); 不是 TryBind 未被调用 (backtrace 证明已调用).
- 证据等级: 生产源码 + 真实 r27 运行日志 (baseline), 非新字节复现.

### 修复已写入 (生产 2 文件)

- G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs
  - 411-429 行: 入口改为收敛单消费者 — 已有 `_pumpNode` 且 `IsInstanceValid && IsInsideTree()` 时返回; 否则 QueueFree 旧节点并重建, 重复 init/菜单/绑定不叠 consumer.
  - 446-474 行: 保留同步 `root.AddChild(pump)`; 若因 busy parent 被静默拒绝 (或抛异常), 统一在 469-473 行判定 `pump.GetParent() == null` 后 `Godot.Callable.From(() => AttachPumpDeferred(root, pump)).CallDeferred()` 在 idle 主线程重挂.
  - 476-524 行: 新增 `AttachPumpDeferred` — 在 `Gate` 下重校验 `ReferenceEquals(_pumpNode, pump)`; Terminal/ShuttingDown/stop 时走主线程 `StopPumpLocked()` 释放; 否则 idle AddChild; 再被拒则清空 `_pumpNode` 并 QueueFree, 不留 dead node/scaffold. 被替换的陈旧 callable 因 ReferenceEquals 失败成为 no-op.
- G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceBridgePump.cs: **未改动**, SHA256 35EAABCAA7191CEE31F8AA7EB8960CC8B80A850F2B34FA72F3505FF6904BA1DC. `_Ready` 设 `ProcessMode.Always`, `_Process` 调 `PumpTick` 保持; 修复点是确保该节点真正入树.
- 改动后 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs SHA256 = 2253452C50E3688906DE4DDD035B4AEA669E6DD7DFA4AA095B74F52C4440ADA8 (1361 行). 修复前备份 G:\omp works\.tmp\forms-independent-20261005\r30-FormStanceWatcherBridge.cs.bak SHA256 = 6F8425EA6A31A813B37DEFCD68DB7C80C76C7688E3CF7E6D503704B4E8DD69FE. 结构自检: 花括号/圆括号平衡, `_pumpAttachPending` 无残留引用.
- 保持条件核对: main-thread only (AddChild/QueueFree 仅由已知主线程入口或 idle Callable 触发); AssemblyLoad 回调仍只发 pending/epoch; Bound 每帧 O(1) (仅 pending 时扫身份); Retryable 有界预算未改; 未强行解除暂停或改其它节点 ProcessMode; 未把 pending 直接当 Terminal.

## 进行中

- 已用 `GodotTreeExtensions.AddChildSafely` (G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Helpers\GodotTreeExtensions.cs:9-19) 作为引擎内权威对照: 官方写法同样是 `parent.IsNodeReady() || !parent.IsInsideTree()` 才同步 AddChild, 否则 `CallDeferred(Node.MethodName.AddChild, child)`. 本修复方向与引擎自身实践一致.
- 仍待静态复核: `AttachPumpDeferred` 与 `Shutdown`/`EnterTerminalLocked` 的跨线程释放边界; `StopPumpLocked` 在 pump 已 QueueFree 但未入树时的 `_mainThreadManagedId` 判定.

## 未知

- FLAG: 无新字节实机证据. 延迟挂载在 paused SceneTree 下是否按 Always 每帧运行, 重复 Watcher 是否 40s 内收敛 Terminal, 两 probe 是否执行, UI `NEndTurnButton.HasPlayableCard->ShouldPlay` 额外 Forms unavailable 异常是否随 state 收敛消失 — 全部归 hub 连续实机.
- 不把源码推理称为实机复现; 不称安全通过.

## CODE_COMPLETE

- G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs
- G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceBridgePump.cs (未改动)
- G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\combat-pump-recovery-r30\worker.md
