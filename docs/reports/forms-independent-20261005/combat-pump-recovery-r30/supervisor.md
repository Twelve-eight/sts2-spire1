WAITING_GATE

等待 hub 对本批 worker 的真实 native wait 结果为 completed，并创建本目录 gate-notice.json 且发通知。门禁未满足前不读取在写源码、不做静态审核、不构建/lint/test、不写代码、不运行游戏、不做 Git 操作、不委派、不调用其它 harness/model。

## 已确认
- 已读取监督请求文件：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\combat-pump-recovery-r30\supervisor.request.md`。
- 本轮监督范围仅为：`G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs` 与 `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceBridgePump.cs`。
- 唯一可写路径为：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\combat-pump-recovery-r30\supervisor.md`。
- 用户指定模型与路由为：`global:deepseek-v4.1-flash` / `wb2api` / `reasoning xhigh` / `native Codex` / `no fallback`。
- 当前状态：WAITING_GATE；尚未获得门禁通知。

## 进行中
- 静态审核尚未开始。计划在门禁后按 `worker.request.md` 审核真实生产消费者、paused/main-thread 可靠入口、Godot 动态脚本注册假设、仅 pending/epoch 扫描而非每帧 scan、retry 预算与 Bound 存在、benign 同 identity 不过度 Terminal、重复创建无叠 consumer、Shutdown/Terminal stop 与信号退订主线程边界、支付前保护与无硬引用。
- 每个检查面完成后将立即追加落盘，不攒至最后一次性输出。
- 若发现 P1，结论将为 NEEDS_REWORK；PASS 仅代表静态审核通过，真实收敛仍由 hub 验收。

## 未知
- 尚未读取任何在写源码，尚未核对 API 来源与准确行号。
- 尚未确认 worker 是否完成、gate-notice.json 是否已创建、门禁通知是否已收到。
- 尚未有构建、lint、测试、运行、实机或真实收敛证据。

## FACE 0 - GATE/FROZEN HASH

已确认:
- gate-notice.json 存在, Worker=01a10a59-9d7d-78a0-a0ca-8da361f017e4, Status=completed, TimedOut=false, WorkerResult=CODE_COMPLETE, NativeTool=multi_agent_v1.wait_agent, CheckedAt=2026-10-05T12:51:48.9875515+08:00.
- 审核按门禁哈希冻结; 下列哈希由 Get-FileHash 复核.
- G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs | SHA256=2253452C50E3688906DE4DDD035B4AEA669E6DD7DFA4AA095B74F52C4440ADA8 | Lines=1360
- G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceBridgePump.cs | SHA256=35EAABCAA7191CEE31F8AA7EB8960CC8B80A850F2B34FA72F3505FF6904BA1DC | Lines=28

## FACE 1 - 生产消费者入口与 Godot 动态脚本注册假设 (静态)

已确认 (有证据):
- 入口链 (源码, 只读): `FormStanceModePatch.cs:9-20` `[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]` postfix -> `TryBindOnMainThreadEntry()`; `FormStanceMainMenuRetryPatch.cs:13-22` `[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]` postfix -> 同一入口. 两处均在 `TryBindOnMainThreadEntry` 返回 false 后再调 `EnsurePumpForRetry()` (双重调用).
- `FormStanceWatcherBridge.TryBindOnMainThreadEntry` (FormStanceWatcherBridge.cs:256-285) 在 Bound 时先 `StartPumpLocked()` (265) 再查 pending; 非 Bound/Terminal/ShuttingDown/Installing 返回 false; 否则 `TryBind()` (281), 失败再 `EnsurePumpForRetry()` (283).
- 消费者本体: `FormStanceBridgePump.cs:17-27` `_Ready` 设 `ProcessMode = Node.ProcessModeEnum.Always`; `_Process` 调 `FormStanceWatcherBridge.PumpTick()`.
- 权威 API: GodotSharp 4.5.1 XML `G:\omp works\.nuget\packages\godotsharp\4.5.1\lib\net8.0\GodotSharp.xml` - `Node.ProcessModeEnum.Always` (4816-4819) "Always process. Keeps processing, ignoring SceneTree.Paused."; `GodotObject.CallDeferred` (3807); `Callable.CallDeferred` (473); `Node.IsInsideTree` (5286); `Node.QueueFree` (5660-5664) "safe to call multiple times", "freed after all other deferred calls are finished".
- 引擎内权威对照: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Helpers\GodotTreeExtensions.cs:9-19` 官方 `AddChildSafely` 在 `parent.IsNodeReady() || !parent.IsInsideTree()` 才同步 AddChild, 否则 `parent.CallDeferred(Node.MethodName.AddChild, child)`; 与 worker 修复方向一致.

进行中:
- 未把 "动态程序集脚本注册" 当作已证前提; 本轮不依赖该假设: 消费者改为靠 Godot Node `_Process` (节点真实入树后由引擎调用), 与动态脚本注册无关.
- 双重 `EnsurePumpForRetry()` 造成节点 churn (旧 orphan QueueFree + 新建), 待 FACE 2 判定是否影响收敛/是否 P1.

未知:
- 无新字节实机: 延迟挂载节点在 paused SceneTree 下是否按 Always 每帧运行, 未验证.
## FACE 2 - 根因证据与消费者唯一性 (静态)

已确认 (有证据):
- 真实日志证明修复前根因: `G:\omp works\.tmp\forms-independent-20261005\native-r18-terminal-r27\b1-terminal\appdata\SlayTheSpire2\logs\godot.log:343-353` `ERROR: Parent node is busy setting up children, add_child() failed.` 且 C# backtrace `[2] StartPumpLocked -> [3] TryBind -> [4] TryBindOnMainThreadEntry -> [5] ModelDb.Init_Patch6 -> [7] NGame.GameStartup`. 与 worker 声明一致.
- 消费者创建点唯一: 全仓 grep `new FormStanceBridgePump` 仅 `FormStanceWatcherBridge.cs:449`; 无 `.tscn/.gd/.tres` 引用. 故"重复创建无叠 consumer"只需审 `StartPumpLocked` 的幂等/收敛逻辑.
- 修复后 `StartPumpLocked` (411-475) 收敛规则: `_pumpNode != null` 时, 若 `IsInstanceValid && IsInsideTree()` 直接 return (417-418); 否则 QueueFree 旧节点并置 null 后重建 (423-429). 该规则保证同一时刻至多一个新节点, 但**不保证只有一个活 consumer**: 旧节点若已 `QueueFree` 但本帧仍 inside tree 且未真正释放, 新节点可能同帧创建, 出现短暂双 consumer.
- `AttachPumpDeferred` (483-526) 的 ownership 检查为 `ReferenceEquals(_pumpNode, pump)` (487). 由于 `StartPumpLocked` 在替换时会把 `_pumpNode` 指向新节点, 旧节点已排队的 deferred callable 会因 ReferenceEquals 失败直接 return (488), 即 stale deferred callback 不会 attach 旧节点, 但**也不会释放旧节点**; 释放只能依赖先前 `QueueFree()` (424) 或 `StopPumpLocked`.
- 结果: stale deferred callback 成为 no-op 是成立的; 但旧 orphan 的释放完全依赖 `QueueFree` 生效, 若 `StartPumpLocked` 在 `_pumpNode != null` 分支中 `QueueFree` 后立即 `_pumpNode = null`, 之后若同一帧有其它入口再次进入, 会出现"旧节点排队释放 + 新节点"并存, 直到 deferred 队列处理完.

进行中:
- 待判定: 上述短暂双 consumer 是否构成 P1 (见 FACE 3).
- 待判定: `AttachPumpDeferred` 中 `root` 引用在跨帧 deferred 后是否仍有效 (FACE 4).

未知:
- 无实机证据; 上述均为源码/引擎文档静态推理, 不称实机复现.
## FACE 3 - deferred callback 所有权 / orphan 换代 / 双 Ensure churn (静态)

已确认 (有证据):
- delta 边界精确: 新 411-549 行替换旧 411-470 行; 新 1-410 与旧 1-410 逐行相同, 新 550-1360 与旧 471-1281 逐行相同 (Compare 脚本 head/tail 校验). 即本轮只改 pump 安装/挂载/停止区, 支付前保护 (FormStanceModifier.ShouldPlay, 另一文件) 与其余全部逻辑未被触碰.
- stale deferred callback 所有权: `StartPumpLocked` 在替换分支先 `_pumpNode.QueueFree()` 再 `_pumpNode = null` (424-425), 新节点重新赋值 `_pumpNode` (450). `AttachPumpDeferred` 入口 `if (!ReferenceEquals(_pumpNode, pump)) return;` (487-488) 使旧节点的已排队 callback 成为 no-op; 旧节点由 424 行的 QueueFree 释放. 无叠挂载.
- 双 Ensure 只造成小型 churn: ModelDb.Init postfix (`FormStanceModePatch.cs:16-19`) 与 NMainMenu._Ready postfix (`FormStanceMainMenuRetryPatch.cs:19-21`) 在 `TryBindOnMainThreadEntry` 内部已调一次 `EnsurePumpForRetry` (283) 后又各调一次. 同帧两次 `StartPumpLocked` 时: 第一次建 pump1 且 AddChild 被 busy parent 静默拒绝 (pump1 不在树) -> 排 deferred; 第二次因 `_pumpNode=pump1` 且 `IsInsideTree()==false` (415-418 不满足) 进入替换分支 -> pump2 + 新 deferred. idle 时 Attach(pump1) 因 ReferenceEquals 失败 no-op, Attach(pump2) 成功 AddChild -> 唯一活 consumer 入树. 结论: 仅多一次 Node 分配+QueueFree, 不阻止有效 pump 入树.
- AddChild 成功时无 churn: 若第一次 AddChild 已成功 (menu 场景已就绪), 第二次进入 417-418 直接 return.
- orphan 释放路径完整: 每个由 `StartPumpLocked` 产生的 orphan (`GetParent()==null`) 都会在 469-474 排一个 deferred; 被替换时由 424 释放; 被 Stop 时由 528-565 释放.

进行中:
- 理论残留 (非 P1, 静态未证): 若 deferred callback 在 `_state` 恰为 `Installing` 时执行, 495-499 的守卫会 return 而不释放, 留下 `_pumpNode=orphan` 且无待处理 deferred; 之后仅靠下一次主线程入口 (`TryBindOnMainThreadEntry`/`EnsurePumpForRetry`) 替换自愈. 生产中 ModelDb.Init 与 NMainMenu._Ready 不在同帧, 且 PumpTick 只由在树 pump 触发, 该窗口静态上极窄.

未知:
- 无新字节实机; deferred AddChild 在真实 paused SceneTree 下是否每帧消费 pending, 未验证.
## FACE 4 - Shutdown/Terminal 与主线程边界, queued-free 节点有效性 (静态)

已确认 (有证据):
- 生产 teardown 入口: `MainFile.Shutdown` (MainFile.cs:185-225) 第一步即 `FormStanceWatcherBridge.Shutdown()` (192); `OnProcessExit` (227-237) 调 Shutdown. ProcessExit 回调线程非 Godot 主线程 -> Shutdown 必须 off-main 安全.
- `Shutdown` (294-346) 在 Gate 下先发布 `ShuttingDown` + `_bindingGeneration++` (303-307), 再 `StopPumpLocked()` (311), 之后才 unpatch (314-336) 与退订 (343). 顺序满足"先停用后拆补丁".
- off-main 安全: `StopPumpLocked` (528-565) 在 `Environment.CurrentManagedThreadId != _mainThreadManagedId` 时**只**置 `_pumpStopRequested = true` 并 return (544-549), 不触碰任何 Godot API; 释放延后到主线程 `PumpTick` (579-583) 或 `AttachPumpDeferred` (489-494). `AppDomain.AssemblyLoad -=` (374) 不触 Godot. 结论: off-main Shutdown 不产生 off-main Godot 调用.
- Terminal 路径同构: `EnterTerminalLocked` (746-776) 也先 `_bindingGeneration++`/清 pending (750-751), 再 `StopPumpLocked()` (760), 再退订 (761) 与 unpatch (764-774). 生产 Terminal 入口 (TryBind 163/241, MarkOwnerPatchesFailed 358) 均在主线程.
- queued-free 节点有效性: `StartPumpLocked` 的 `QueueFree` (424) 只在 `_pumpNode` **不在树**或**已失效**时执行 (417-418 的在树+有效组合已提前 return). 因此被换代节点必然不在树, 其 deferred callback 因 `ReferenceEquals` 失败 (487-488) 不会再次 AddChild; 双 `QueueFree` 亦安全 (GodotSharp.xml:5663 "safe to call QueueFree multiple times").
- 旧代码对照 (备份 `r30-FormStanceWatcherBridge.cs.bak:413`): 旧条件 `_state is not (Retryable or Bound) || _pumpNode != null` 使静默失败后的 orphan **永久阻塞**后续所有尝试 (P0 根因). 新代码 415-429 对"非在树节点"执行替换, 从而自愈. 这是本 delta 的核心修正.

进行中:
- 静态理论残留 (非 P1): `AttachPumpDeferred` 在 `_state == Installing` 或 `root` 失效时 (495-499) 直接 return 且不释放; 因 Installing 全程持 Gate 且与 deferred 同在主线程, 生产不可达; `root` 失效属场景拆除, 由后续主线程入口替换自愈.

未知:
- 无实机: ProcessExit 期间 off-main Shutdown 后, 主线程是否仍有一次 PumpTick 完成 QueueFree, 未验证.
## FACE 5 - Bound 无每帧扫描, 仅 pending/epoch 触发; retry 预算与 bound (静态)

已确认 (有证据):
- `OnAssemblyLoad` (383-392) 仅做 `Volatile.Write(pending,true)` (389) + `Interlocked.Increment(epoch)` (391); 无 Godot/Harmony/loader-lock 操作, 满足"AssemblyLoad 只发 pending/epoch".
- Bound 路径: `PumpTick` 590-595 在 Bound 时只读 `_assemblyLoadPending`; 为 false 立即 return, 不调 `GetAssemblies()`. 全文件仅 3 处 `AppDomain.CurrentDomain.GetAssemblies()`: 736 (`BoundIdentityStillValid`), 780 (`ValidateBinding`), 1295 (`Spire1StanceNotification.Resolve`). 前两者分别只在 pending 被消费 (TryBind 151-154 / PumpTick attempt 635-636 -> TryBindOnMainThreadEntry 268-269) 或绑定尝试时执行; 1295 只在 `AfterStanceChanged` 的 Spire1 通知路径, 不在每帧 tick. 结论: 正常 Bound **无每帧 scan**.
- `IsAvailable` (84-97) 与 `CallbacksAllowed` (697-710) 仅做状态+Volatile 读, O(1), 不扫程序集.
- Retryable 预算: `PumpMaxAttempts = 120` (396), `PumpSkipFrames = 3` (397), `PumpIdleRearmFrames = 120` (567). 新 epoch 只重置一次预算 (600-607); 预算耗尽后每 120 帧单次重试 (608-619), 有界且不每帧 scan.

进行中:
- Retryable 慢重试仍会周期性调用 `ValidateBinding` (含一次 GetAssemblies) —— 每约 2 秒一次, 属设计意图的有界重试, 非每帧扫描. 静态判定可接受.

未知:
- 无实机: pending 置位后在 paused SceneTree 中实际多快被 PumpTick 消费, 未验证.
## FACE 6 - 收尾判定 (静态)

已确认 (有证据):
- 动态脚本注册假设已可证: 用反射读取中央编译产物 `G:\omp works\.tmp\forms-independent-20261005\forms-build-r30\Forms.dll` (SHA256 1598FE564B8D29A3CFE5BB49CF5F9EA295EC36DC7D3CB3E8BA71DCED9B296C86, 121344 bytes), 程序集带 `Godot.AssemblyHasScriptsAttribute(new Type[2] { FormStanceBridgePump, MainFile })`, `FormStanceBridgePump` 带 `[Godot.ScriptPathAttribute("res://FormsCode/FormStanceBridgePump.cs")]`, 且 `_Ready`/`_Process` 覆写存在. 该程序集无 `Watcher` AssemblyRef (GetReferencedAssemblies 校验). 故节点入树后由引擎正常驱动 `_Process`, 不依赖未证假设.
- `Callable.From(...).CallDeferred()` 有引擎内权威先例: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Helpers\GodotTreeExtensions.cs:90-93` 同写法; GodotSharp 文档 `Callable.CallDeferred` 于 idle 帧执行 (GodotSharp.xml:473-478).
- 无 P1: 三个重点面均无 fail-closed 违背 —— stale deferred 因 ReferenceEquals 成 no-op 且旧节点由 QueueFree 释放; queued-free 节点不在树, 不会二次 AddChild; off-main Shutdown 只发布 stop 标志, Godot 释放延后到主线程.

结论:
- **SUPERVISION_PASS (静态)**. 本轮 delta (新 411-549 行) 精确修复旧 413 行 "orphan 永久阻塞" 的 P0 根因, 未改动 delta 之外任何逻辑 (新 1-410 / 550-1360 与旧 1-410 / 471-1281 逐行相同), 未放松支付前保护, 未引入 Watcher AssemblyRef.
- 尚缺实机 (归 hub, 不称已通过): 新字节在真实运行中 40s 内由 pending 收敛 Terminal; 两 probe 执行; paused SceneTree 下 Always 每帧消费; UI `NEndTurnButton.HasPlayableCard->ShouldPlay` 额外异常是否消失.

进行中:
- 无待复核的静态阻塞项.

未知:
- 真实收敛未验证; 本 PASS 仅静态, 不构成实机验收.

## 路由记录 (AGENTS.md 4b)
- 本批监督由当前 Codex 原生设施执行; 用户指定模型 global:deepseek-v4.1-flash / 路由 wb2api / reasoning xhigh / no fallback. 未启动其它 harness, 未委派, 未更换模型.