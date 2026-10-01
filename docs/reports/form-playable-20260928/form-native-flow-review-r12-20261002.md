# FormNativeSmokeRunner 真实战斗 gate、超时与 action 结果一致性只读审查

- 审查日期: 2026-10-01
- 范围: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`, `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormNativeSmokePatch.cs`
- 实现报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\form-native-smoke-worker-r12-20261001.md`
- 用户指定路由: `6.1sol via agentrouter`
- 本审查限制: 不构建, 不测试, 不部署, 不启动游戏, 不改产品代码, 不改共享配置或 Steam 安装.

## 已确认

- [检查面 0] 已读取 `G:\omp works\AGENTS.md`, `G:\omp works\Sts\sts2-spire1\docs\DEVELOP-form-playable-20260928.md`, `G:\omp works\.tooling\subagent-report-protocol.md` 和实现报告. 实现报告描述了真实 `ModelDb`/`FormStanceModifier`/run/room/card/action 链路及最近的逐帧 gate、终止性超时、detached drain 和 `actionFailureLatched` 修订, 但这些是待本次独立源码回读核验的声明, 不能直接作为本审查 PASS 证据.

- [检查面 1] P1 / 静态 PASS - 普通启动 gate 与禁止路径: `FormNativeSmokePatch.cs:7-13` 只在 `NGame._Ready` postfix 调用 `FormNativeSmokeRunner.TryStart`; `FormNativeSmokeRunner.cs:47-60` 先解析命令行, 无 `--form-native-smoke*` 时在 `:50-53` 直接返回, 不创建异步入口. 只有命令行请求且 `Interlocked.Exchange` 首次成功时才调用 `TaskHelper.RunSafely`. 触发条件是普通启动或重复 `_Ready`; 当前控制流不会进入 smoke 任务. 静态扫描命令为 `Select-String` 检查 `autoslay|TestMode|NGame\.Quit|PowerCmd\.Apply|Task\.FromResult\(condition\)|Hook|ApplyForm`; 结果均为无匹配, 仅 `Snapshot` 的 `WatcherFormStancePower` 类型过滤存在于 `:764-772`, 没有直接应用 Form power 或伪造 Hook. 最小修复范围为无. 缺失证据是未启动目标二进制, 尚未证明 Harmony postfix 扫描、命令行传递和 `TaskHelper.RunSafely` 运行时行为.

- [检查面 2] P1 / API 链路静态 PASS, 真实 Form gate REWORK: `FormNativeSmokeRunner.cs:219-227,723-750` 从当前 `ModelDb.AllCharacters` 查找 `WatcherMod.Watcher`, 使用 `ActModel.GetDefaultList` 和默认 Act 的 regular/elite encounter, 并按精确 `ModelDb.AllCards` entry 查找 `WATCHER_VIGILANCE|WATCHER_ERUPTION_P|WATCHER_BLASPHEMY`. `:313-328` 把 `ModelDb.Modifier<FormStanceModifier>().ToMutable()` 传入真实 `NGame.Instance.StartNewSingleplayerRun(..., shouldSave:false, GameMode.Custom, ...)`; `:345-357` 使用真实 `RunManager.EnterRoomDebug`; `:379-414` 依次调用 `CombatState.CreateCard`, `CardPileCmd.Add(..., PileType.Hand, skipVisuals:true)`, `new PlayCardAction`, `ActionQueueSynchronizer.RequestEnqueue`; `:417-420` 等待真实 `CompletionTask`. 这些调用不是 autoslay/TestMode、直接 power 注入或伪造 Hook. 但是 `:375-377,753-798` 的 `FormStanceMode.IsSelected`, native stance 和 form carrier/effect 只进入 `before/after` 证据, 没有被转成成功 gate; `:455-489` 的 `actionPassed` 只验证 action 完成, 未要求 `formModeSelected == true` 或存在预期 form carrier/effect. 触发条件是 modifier 未被真实应用但卡牌 action 仍完成, 当前控制流仍可能写 `status=passed`. 最小修复范围是把预期真实 Form 状态加入不可逆场景成功判定并在缺失时写失败, 不改变真实 API 链. 缺失证据是未运行真实单人局, 未证明 modifier/Watcher bridge/卡牌在目标二进制中的绑定和实际姿态状态.

- [检查面 3] P1 / 逐帧 gate 与 detached 收束 REWORK: `FormNativeSmokeRunner.cs:812-840` 确实用 `DateTimeOffset` deadline、真实 `game.AwaitProcessFrame()` 和 `Task.WhenAny` 做 run/combat/play 条件轮询, 不是 `Task.FromResult(condition)`. `:313-328,345-357,383-391` 的 run/room/card operation 也分别有 120/120/60 秒等待, timeout 时 `:963-1010` 记录 `DetachedOperation`, 抛 `TerminalOperationException`, `:247-251` 阻止后续场景, `:521-550,1041-1095` 用共享 10 秒 drain, 未收束时跳过 cleanup. 但全局条件 deadline 在 `:818` 只包住循环本身; 每次 `:819` 和 `:828-830` 的 `InvokeOnMainThreadWithTimeoutAsync` 另有独立 10 秒 gate, 因此主线程 gate 卡住时等待可超过声明的 condition timeout. 更严重的是 `:834-837` 超时的 `AwaitProcessFrame` 只调用 `ObserveDetachedTask`, 没有加入 `terminalOperations`/有界 drain; `:883-891` 的 deferred main-thread callback timeout 也没有可取消或 drain 记录. 触发条件是 Godot 主线程停顿或 deferred callback 延迟, 当前控制流可能在留下未收束 frame/gate task 后继续 cleanup 或最终退出. 最小修复范围是让 gate/frame detached task 进入同一终止性 drain, 并让每次 gate 使用剩余 deadline 而不是独立延长 10 秒. 缺失证据是未运行目标 Godot 调度, 未证明这些 task 是否在目标二进制中必然及时完成.

- [检查面 3b] P1 / startup completion task 同样未纳入收束: `FormNativeSmokeRunner.cs:178-181,801-810` 通过真实 `game.GameStartupComplete` 做 120 秒等待, 但调用 `AwaitOperationWithTimeoutAsync` 时未传 `terminalOnFailure` 或 `detachedOperations`; startup task timeout 后 `RunAsync` 直接写失败并返回, 没有对遗留 task 做观察/有界 drain. 这不会继续 scenario loop, 但仍可能在最终 quit 前留下未收束启动 task, 与本轮要求的 detached operation 收束契约不一致. 最小修复范围是复用统一 detached operation 记录和终止性 drain; 缺失证据仍是未运行真实启动 task 超时路径.

- [检查面 3c] P1 / action timeout 后场景隔离 REWORK: `FormNativeSmokeRunner.cs:417-420` 等待 `PlayCardAction.CompletionTask` 时未传 `terminalOnFailure:true` 或 `detachedOperations`; `:431-439` timeout 只设置 `actionFailureLatched` 并调用 `TryCancelActionAsync`, `:624-656` 最多等待一次 `action.Cancel()` gate, 没有等待或记录原 `CompletionTask` 的最终终态. `:476-489` 将 action 标记 failed, 但没有设置 `result["terminalFailure"]`; `RunAsync:247-251` 因而允许下一个场景继续. cancel gate timeout 还会在 `:650-655` 被压成 evidence 字段, 同样不进入 detached drain. 触发条件是 action completion 超时、取消请求延迟或取消后的 completion 仍 pending; 当前代码可在前一场 action 未收束时执行下一场景和 cleanup, 不满足 detached operation 阻止后续场景并有界 drain. 最小修复范围是把 action completion/cancel deferred task 纳入终止性操作集合, 统一 drain 后再 cleanup; 未收束时跳过 cleanup 并保持 terminal failure. 缺失证据是未构造真实 action timeout/cancel, 未证明 `GameAction.Cancel()` 在目标二进制中同步终止 `CompletionTask`.

- [检查面 4] P1 / action timeout、取消、异常的不可逆锁存静态 PASS, 非异常失败分支 REWORK: `FormNativeSmokeRunner.cs:422-447` 在 `CompletionTask` 的 `OperationCanceledException`、`TimeoutException` 和普通异常路径先把 `actionFailureLatched` 设为 `true`; `:458-464` 的成功判定还要求锁存为假、`failure` 为空、`State == Finished`、`Exception == null`、`CompletionTask.Status == RanToCompletion` 且没有新增 `TaskHelper.UnobservedFault`; `:512-515,553-568` 在外层异常和 finally 再把锁存值写入 `cardPlay`. 因而已记录的 timeout/cancel/exception 不会因 action 后续延迟变成 Finished 而恢复为 passed. 但 `:455-489` 中若 action 已完成却 `State != Finished`, 或仅因 `hasNewUnobservedFault` 为真而失败, 代码不设置 `actionFailureLatched`, 也不把原因写入 `currentActionResult["failure"]`; 外层 `result["failure"]` 可为失败而 `cardPlay.failure=null`、`failureLatched=false`. 触发条件是非异常 action 不变量失败或新增 unobserved fault, 当前 JSON evidence 与 scenario status 不可逆一致性不足. 最小修复范围是所有 `actionPassed == false` 分支统一锁存并写入 action failure/evidence, 使 outer status 与 cardPlay failure 同源. 缺失证据是未产生真实 action timeout/cancel/exception/unobserved-fault JSON.

- [检查面 4b] P1 / action JSON `status` 永远停在 enqueued: `FormNativeSmokeRunner.cs:608-621` 在 `CreateActionResult` 写入 `status="enqueued"`; `:672-688` 的 `RecordActionEvidence` 更新 `state`, `completionTaskOutcome`, `cancelled`, `exception`, `completedUtc` 和 `failure`, 但从未更新 `status`. 因而即使 `:458-481` 判定真实 action `Finished` 且 `CompletionTask` `RanToCompletion` 并把 scenario 写成 `passed`, `cardPlay.status` 仍为 `enqueued`; cancel/fault/timeout 也同样保留旧状态. 触发条件是任何 action 结束, 当前 JSON evidence 与同一 action 的 state/outcome 不一致. 最小修复范围是让 status 从同一份 action state/completion evidence 派生并在每次记录时更新. 缺失证据是未生成真实 action JSON, 但该字段未写回是源码事实.

- [检查面 5] P1 / 外层 status 与 scenario/exit 结果 REWORK: `FormNativeSmokeRunner.cs:133-139` 在 `RunAsync` 返回非零或抛异常时保留 `exitCode=1/2`; 但 `:145-152` 的 final JSON 仅以 `quitStatus.StartsWith("executed")` 决定 `status="completed"`, 完全不检查 `exitCode`. 因此 startup/bridge/scenario/invalid 失败后只要 `SceneTree.Quit` gate 执行, final JSON 会同时出现 `status=completed` 与 `exitCode=1|2`; `:242-250` 的 per-scenario failed/terminal evidence 也不会改变该 final status. 触发条件是任何业务失败但最终退出调用成功. 最小修复范围是让 final status 以业务 exitCode 和 quit outcome 的合取决定, 并保留失败原因/场景摘要. 缺失证据是未运行真实进程, 未确认 `SceneTree.Quit` 调用后的最终 JSON 落盘时序.

- [检查面 5b] P1 / JSON evidence 写入失败不反映到外层结果: `FormNativeSmokeRunner.cs:1098-1125` 在报告环境变量为空、路径不在 G:、目录/序列化/写文件异常时只 return 或记录日志, 不改变 scenario/final payload, 不改变 `exitCode`, 也不让 `RunAsync` 失败. 触发条件是未配置报告目录、配置被拒绝或文件写失败; 当前控制流可以在没有任何 JSON evidence 的情况下让 final `status=completed` (同样受 `:147-149` 的 exitCode 忽略影响). 最小修复范围是让 evidence write result 进入不可逆失败状态, 至少使 final status/exitCode 反映报告缺失. 缺失证据是未实际写报告目录, 未测试目标运行时的权限和文件系统状态.

## 进行中

- 源码检查面已完成. 本次没有修改两个目标产品文件, 没有构建、测试、部署、启动游戏或改动共享配置. 不再添加静态修复; REWORK 项留给后续实现批次.

## 未知

- 未验证目标二进制中的 Harmony `NGame._Ready` patch 是否加载并按预期只在命令行 gate 后启动.
- 未验证真实 `ModelDb`、`FormStanceModifier`、Watcher bridge、default act/encounter、三张 Watcher 卡、`CreateCard`/`CardPileCmd.Add`/`PlayCardAction`/`RequestEnqueue`/`CompletionTask` 在目标运行时的绑定和签名兼容性.
- 未验证真实 run/combat/play process frame 时序, 主线程 gate 的 deferred callback 执行, gate timeout, `AwaitProcessFrame` timeout, operation timeout, detached drain, cleanup skipped 和最终 `SceneTree.Quit` 时序.
- 未验证真实 action timeout/cancel/exception, `GameAction.Cancel()` 对 `CompletionTask` 的最终影响, `TaskHelper.UnobservedFault` 的实际回传, JSON 是否在退出前完整落盘.
- 未执行任何真实游戏运行, 因此本报告不把源码推理写成实机通过, 不把隔离或历史报告写成当前运行证据.

## 结论

- 本审查范围结论: **REWORK**.
- 静态确认的 PASS 仅限普通启动 gate、禁止路径缺失和真实 API 调用链存在. 仍有可直接触发的静态缺口: Form 真实状态没有成为成功 gate; gate/frame/startup detached task 没有统一有界 drain; action timeout 后可能继续后续场景; 非异常 action 失败未统一锁存; `cardPlay.status` 永远是 `enqueued`; final status 忽略业务 `exitCode`; JSON 写入失败不传播.
- 真实运行状态仍未知. 按用户要求本轮已完成只读审查并返回 `completed`.
