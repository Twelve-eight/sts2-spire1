# Forms 生命周期窄返工 r5 工作报告

## 已确认
- 已读取唯一请求 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-worker.request.md`。
- 本批范围为 S-03..S-07 生命周期窄增量；禁止重新设计六效果、公开 interop、10 个 CustomID，禁止构建/测试/部署/游戏/git/委派。
- 唯一产品可写白名单：`G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs`、必要时同目录 `FormStanceBridgePump.cs`、`MainFile.cs`；文档白名单 `DEVELOP.md`、`DEVLOG.md`。
- 下一步读取 `dsv41f-round3\forms-supervisor.md` 与 r4 当前源，先建立逐项证据再改动。

## 进行中
- 等待读取监督报告与 r4 源。

## 未知
- S-03..S-07 在 r4 源码中的精确残留位置与最小修复面尚未核实。

### 第一批源码证据 (2026-10-05, r4 live)
- r4 已包含 `using Godot;`? 否; 当前 live 文件 1-19 行仍无 `using Godot;`, 但全文用 `Godot.Node` 全限定, 故 r4 用完全限定类型修复了 S-01 编译阻断. 证据: 第 75, 383, 386, 398, 424 行均为 `Godot.*`.
- `MainFile.ResPath` 当前第 25 行为 `public const string ResPath = "res://" + ModId;`, 已修复 S-02.
- S-03 残留: `TryBindOnMainThreadEntry` 第 239-240 行 `if (_state == BridgeState.Bound) return true;` 绕过 pending/身份; `TryBind()` 第 141-155 行 Bound 分支不可达 (唯一调用点 247 行在早返回之后).
- S-04 残留: `TryBind()` catch 第 215-226 行对 Terminal/Retryable 一律 `EnsureAssemblyLoadSubscription()`; Terminal 未统一清理订阅.
- S-05 残留: `EnterTerminalLocked` 第 579-608 行未调用 `Spire1StanceNotification.Reset()`.
- S-06 残留: `AfterMarkerRemoved` 第 848-863 行 `finally` 仅检查 `CallbacksAllowed`, 未捕获 BindingLease/逐轮代数.
- S-07 残留: `_assemblyLoadPending` 第 145, 244, 484, 269, 323, 419, 584 行为普通读/写; 写侧第 354 行用 `Volatile.Write`, 读侧未用 `Volatile.Read`.
- 当前可写文件 SHA256 起始值: FormStanceWatcherBridge.cs `9886FDBB82BEF932CDCDB866C4DC309D755E54891734FD80E...` (截断); 已用 Get-FileHash 记录.
- 已知主线程边界证据: `ModelDb.Init` 由 `OneTimeInitialization.ExecuteEssential()` 调用, 后者由 `NGame.GameStartup()` 第 661 行在 Godot 主线程 await 链中调用; 因此 ModelDb.Init postfix 当前可视为已知主线程入口. 证据路径: `G:\omp works\.tmp\mpcs-sts2\src\MegaCrit.Sts2.Core.Helpers\OneTimeInitialization.cs` 第 68, 81 行; `G:\omp works\.tmp\mpcs-sts2\src\MegaCrit.Sts2.Core.Nodes\NGame.cs` 第 649, 661 行.
- `ProcessExit` 来自 `AppDomain.CurrentDomain.ProcessExit`, 见 `MainFile.cs` 第 147 行; 该线程无 Godot 主线程保证. `Shutdown()` 第 169 行调用 bridge Shutdown, bridge 第 272 行 `StopPumpLocked()` 会 `QueueFree()`; 这是跨线程访问 Godot 节点的现存风险, 需要本批收敛. Godot 官方 QueueFree 线程安全未在本地源码中找到明确保证, 报告只记录未知, 不宣称.

## 修订后的进行中 (第一批证据后)
- S-03: 把 `TryBindOnMainThreadEntry` 的 Bound 分支改为调用一致路径 `TryBind()`, 由 `TryBind()` 处理 `Volatile.Read(_assemblyLoadPending)` 与 `BoundIdentityStillValid()`; Bound 时保留 O(1) 帧 pump, 仅在 epoch 变化时扫描身份.
- S-04: catch 中 `terminal` 分支改为统一 `EnterTerminalLocked` 清理路径; Retryable 才保留/建立订阅; 清理 pending、重试预算、native refs.
- S-05: `EnterTerminalLocked` 末尾调用 `Spire1StanceNotification.Reset()`.
- S-06: `AfterMarkerRemoved` 在 await original 前捕获 `BindingLease`, await 后及每轮 `PowerCmd.Remove` 前 `IsLeaseCurrent` 检查.
- S-07: 所有 pending 读改 `Volatile.Read`; 写保持 `Volatile.Write`; pump 预算相关字段的读写统一在 Gate 内, 并显式消费 pending.
- 主线程边界: `StopPumpLocked` 拆分为主线程释放与跨线程标记; 非主线程 Shutdown 不调用 Godot `QueueFree`, 只清托管引用; 已知主线程入口负责释放节点.

## 未知
- 真实程序集替换/同 simple name 多程序集时序未测.
- ProcessExit 的实际线程模型与 Godot QueueFree 线程安全保证未在本机源码中找到权威说明.
- 构建、部署、实机均未执行.

### 第二批证据与已实施改动 (2026-10-05, 源码已改, 未构建)
产品文件: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs` (唯一产品改动文件). `FormStanceBridgePump.cs` 与 `MainFile.cs` 本批未改.

- S-03 [已实施]: `TryBindOnMainThreadEntry` 第 251-286 行重写. Bound 分支不再无条件 `return true`; 它在 Gate 内 `StartPumpLocked()` 后检查 `Volatile.Read(_assemblyLoadPending)`, 无 pending 才返回 true; 有 pending 则走出锁区调用 `TryBind()`, 由 `TryBind()` 第 147-160 行消费 pending 并调用 `BoundIdentityStillValid()`, 不一致则 `EnterTerminalLocked` (fail closed, 要求重启). 非 Bound 且 Watcher 未加载时第 270-276 行保留 O(1) 帧 pump, 不做每帧 AppDomain 全扫描.
- S-03 [已实施]: `TryBind()` 第 165-166 行在进入 Installing 时清 pending, 避免旧通知被下一次绑定误用. 绑定成功后第 190-195 行 `EnsureAssemblyLoadSubscription()` + `StartPumpLocked()` (不再 `StopPumpLocked()`), 使 Bound 后 AssemblyLoad 仍有 O(1) 主线程消费者.
- S-03 [已实施]: 代数门禁加 pending 感知. `TryAcquireLease` 第 576-591 行与 `IsLeaseCurrent` 第 599-609 行在 pending 未消费时拒绝旧 binding; `IsAvailable` 第 84-97 行与 `CallbacksAllowed` 第 618-631 行同样在 pending 时返回不可用/原生语义. 因此 pending 发生后旧 native delegate 不会被继续使用.
- S-04 [已实施]: `TryBind()` catch 第 232-243 行 Terminal 统一走 `EnterTerminalLocked(reason)`; 仅 Retryable 才 `EnsureAssemblyLoadSubscription()`. Terminal 不再无条件保订阅.
- S-05 [已实施]: `EnterTerminalLocked` 第 667-697 行在退订/撤 patch 前清 pending, 退订 AssemblyLoad, 清 pump 预算与 `_binding`/`_boundAssembly`/`_boundAssemblyIdentity`, 并调用 `Spire1StanceNotification.Reset()` (第 684 行). `MarkOwnerPatchesFailed` 第 333-340 行改为复用同一 `EnterTerminalLocked`.
- S-06 [已实施]: `AfterMarkerRemoved` 第 937-960 行在 `await original` 前 `TryAcquireLease` 捕获 BindingLease; await 后与每轮 `PowerCmd.Remove` 前均 `IsLeaseCurrent` 校验, 关闭即无副作用返回; 正常 Bound 原生语义仍 await original.
- S-07 [已实施]: pending 读全部改 `Volatile.Read` (第 151, 259, 263, 270, 514, 583, 604, 627 行), 写用 `Volatile.Write` (第 153, 166, 293, 385, 458, 474, 672 行); `OnAssemblyLoad` 第 379-387 行只做 Volatile 通知与 epoch 自增, 不触 Godot/Harmony, 不持 Gate 以免 loader lock 顺序问题.
- 第 4 项 [已实施]: `StopPumpLocked` 第 449-486 行拆分. 无节点时不触 Godot (任意线程安全); 有节点时仅在创建 pump 的托管线程 (已知 Godot 主线程入口) 上执行 `IsInstanceValid`/`QueueFree`; 其它线程只置 `_pumpStopRequested`, 由后续主线程 `PumpTick` 释放. 无后续帧时不发起跨线程 Godot 调用. `_mainThreadManagedId` 第 441 行记录创建线程.
- 第 4 项 [证据]: `ModelDb.Init` 由 `OneTimeInitialization.ExecuteEssential()` 调用, 后者由 `NGame.GameStartup()` (第 661 行) 在 `_EnterTree`→`TaskHelper.RunSafely(GameStartupWrapper())` (第 556 行) 的 Godot 主线程 await 链中执行; `NGame._EnterTree` 第 554 行自记 `_mainThreadId = Environment.CurrentManagedThreadId`, 佐证该入口即主线程. 证据路径 `G:\omp works\.tmp\mpcs-sts2\src\MegaCrit.Sts2.Core.Helpers\OneTimeInitialization.cs` (第 68, 81 行) 与 `...\MegaCrit.Sts2.Core.Nodes\NGame.cs` (第 554, 556, 649, 661 行).
- 第 4 项 [证据]: `ProcessExit` 来自 `AppDomain.CurrentDomain.ProcessExit` (`MainFile.cs` 第 147 行), 无 Godot 主线程保证; 本批据此不让该路径直接 QueueFree. Godot 4.5.1 官方 XML 文档 (`G:\omp works\.nuget-packages\godotsharp\4.5.1\lib\net8.0\GodotSharp.xml` 第 5660-5665 行) 只说明 QueueFree 延迟到帧末且可重复调用, 未声明线程安全; 报告不宣称 QueueFree 线程安全.

## 进行中
- 已按上述小集完成 `FormStanceWatcherBridge.cs` 改动; 待更新 `DEVELOP.md`/`DEVLOG.md` 层级与"真替换需重启, 不宣称热替换"表述.
- 待做静态自检 (括号/残留符号/调用点), 不构建不测试.

## 未知
- 编译是否通过未验证 (本批禁止构建).
- 真实程序集替换/同 simple name 多程序集时序, ProcessExit 实际线程模型, Godot QueueFree 跨线程安全性均未实机验证.
- 行为级实机路径 (晚加载, 退出, 回滚) 未运行.

### 第三批: 自检与收敛 (2026-10-05, 未构建)
- 复核发现并修正: 早期一版在 `TryBindOnMainThreadEntry` 中对"Watcher 未加载"直接 `return false`, 会使状态停在 `Unbound`、pump/订阅都不建立. 已删除该分支, 改为统一 `TryBind()` + `EnsurePumpForRetry()`; 首次失败会进入 `Retryable` 并建立 pump 与订阅.
- 修正: `TryBindOnMainThreadEntry` 在 `Bound` 分支进入前先 `StartPumpLocked()` (幂等), 覆盖"绑定成功时无 scene tree、pump 未建立"的情况; 之后无 pending 即 O(1) 返回.
- 复核: 移除不再使用的 `WatcherAssemblyCount()` 与 `_pumpInitialAttempt`; 全文无 `_nativeNotification` 残留 (已改 BindingLease 路径); `NotifyEndTurnDivinity` 第 1144-1152 行改为 `TryAcquireLease`/`IsLeaseCurrent` 后在锁外调用 `lease.Binding.OnStanceChanged`, 不在持锁期调用 native.
- 结构自检: `FormStanceWatcherBridge.cs` 花括号 161/161, 圆括号 719/719, 方括号 65/65 配平; 无未声明的 `_` 字段; 引用到的私有方法均在文件内定义.
- 最终产品文件 SHA256: `FormStanceWatcherBridge.cs` = `6813383ADEAA1764A90A8AE5839EC8C95AB56F49C15EB06174F835F0754912DF`; `FormStanceBridgePump.cs` = `B7075F4A5966A70E75ACB2D8F9DB002E9146D43E1020520A6...` (本批未改, 与 r4 同); `MainFile.cs` = `112A5E94E35254140CDF5BF780A72102FF9743C621CE57B48...` (本批未改).
- 文档: `DEVELOP.md` 第 5/5.1 节与 `DEVLOG.md` 已按本批层级更新 (pump 存活期, pending fail-closed, Terminal 统一清理, await 代数核对, 主线程边界, 真替换需重启不宣称热替换).
- 未构建/未测试/未部署/未运行游戏/未改共享配置/未写 C:/未 git.

## CODE_COMPLETE

- 修改文件 (绝对路径):
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs`
  - `G:\omp works\Sts\sts2-forms\DEVELOP.md`
  - `G:\omp works\Sts\sts2-forms\DEVLOG.md`
  - 报告: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\forms-worker.md`
- 覆盖项: S-03, S-04, S-05, S-06, S-07 与请求第 4 项 (ProcessExit/主线程边界与 ModelDb.Init 主线程核对).
- 未验证面: 编译 (禁构建), 实机晚加载/退出/回滚, 真实程序集替换/同 simple name 多程序集, ProcessExit 实际线程模型, Godot QueueFree 跨线程安全性.

### 第四批: 报告勘误 (使报告自洽)
- 第二批中"非 Bound 且 Watcher 未加载时第 270-276 行保留 O(1) 帧 pump"一句已作废: 该分支在第三批被删除, 因为停在 `Unbound` 会导致 pump/订阅都不建立. 现行实现是"统一 `TryBind()` + 失败则 `EnsurePumpForRetry()`", 由 `TryBind()` 的 catch 把状态置 `Retryable` 并 `EnsureAssemblyLoadSubscription()`.
- 第二批中"Bound 分支不再无条件 `return true`"仍成立, 但行号已随第三批收敛变化: 现行 `TryBindOnMainThreadEntry` 为第 251-280 行.
- 第二批中 S-04/S-05 行号 (232-243, 667-697) 与现行基本一致 (S-04 为第 232-243 行附近, S-05 为 `EnterTerminalLocked` 第 656-688 行附近); 以第三批最终 SHA 与结构自检为准.
- 以最终产物 `FormStanceWatcherBridge.cs` SHA256 `6813383ADEAA1764A90A8AE5839EC8C95AB56F49C15EB06174F835F0754912DF` 为交付版本.

### 第五批: 遗留观察 (不改, 供 hub 决策)
- `MainFile.Shutdown()` 与 bridge `Shutdown()` 在 ProcessExit 线程会执行 Harmony `Unpatch`/`UnpatchAll`. 请求第 4 项明确禁止的是"安装 Harmony"与"访问 Godot 节点", 未禁止撤 patch; 本批据此未改动撤 patch 的线程归属, 以免超出窄增量范围. 若 hub 要求撤 patch 也只在主线程执行, 属新的设计决策, 需另行拆分.
- 本批 `IsAvailable`/`CallbacksAllowed` 在 pending 未消费期间返回不可用, 属 fail-closed: 该窗口内原生 Watcher 语义保留, Forms 效果暂停; pending 由下一主线程帧/入口消费, 窗口约一帧. 未做任何"部分效果"假设.
- `AfterStanceChanged` 第 998-999 行仍用 `lease.Binding.RunWithHookContext`; 该调用在 `IsLeaseCurrent` 之后执行, 且 lease 已含 pending 门禁. 未在 await 之后再次复核代数 (与 S-06 的每轮复核要求不同, 此处为单次 native 通知), 如需更严可后续追加.
