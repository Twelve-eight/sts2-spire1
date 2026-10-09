# r33 同批监督窄审

## 已确认

- 2026-10-05 13:35（Asia/Shanghai）：已读取 `central-gate-notice.request.md`。`gate-notice.json` 记录 Worker `01a10a88-1734-7040-8c2f-f1906f8dbfc5`、`Status=completed`、`TimedOut=false`、`NativeTool=multi_agent_v1.wait_agent`、`WorkerResult=CODE_COMPLETE`；门禁条件成立。
- 已按 gate-notice 的 4 个绝对路径重新计算 SHA256，当前值与 gate-notice 完全一致：
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs` = `15271CF355E6303C09D0FCE56286CF5137385AD788ABEB48D1871CCE490F5034`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs` = `CDA047A214E93CBDBFB21C524A2DEC79204ABCC4157CB0F778F8600DE824F5E2`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs` = `B722AD5770D8D4DA02EB51EC5D0DA395157A728AD38873B71DA2A330B41AFAB7`
  - `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs` = `156227F421F446480930E1C184182C86C641746D8DAED9908A6A8BF254A14F8C`
- 已读取 4 文件源码与引擎权威 `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs`。权威入口为 `protected override async Task ExecuteAction()`（第 62 行）；原方法体先执行 `NCardPlayQueue.Instance?.UpdateCardBeforeExecution(this)`（第 65 行），随后 `CanPlay`（第 85 行）、`SpendResources`（第 92 行）、`OnPlayWrapper`（第 103 行）。
- `FormStanceSafetyGuard.PlayPrefix`（第 185-189 行）通过 Harmony prefix 挂在同一 `ExecuteAction()` 上；prefix 在方法体首行前执行，因此满足“先于 UI/支付/历史/副作用”的入口顺序。该 prefix 调用 `ThrowIfSelectedFormsActionUnavailable`（`FormStanceMode.cs` 第 93-97 行），不可用状态会显式抛 `Forms unavailable: restart game process...`（第 99-101 行），不是 cancel/pending。
- 查询路径已隔离：`FormStanceModifier.ShouldPlay`（`FormStanceModifier.cs` 第 46-49 行）只返回 `!IsSelectedFormsCombatUnavailable(card.CombatState)`，不再抛异常；`IsSelectedFormsCombatUnavailable`（`FormStanceMode.cs` 第 64-86 行）对非 Forms、非当前/结束/teardown、Bound 均返回 false，仅“选中 Forms + 当前 live combat + 桥不可用”返回 true。r30 证据 `G:\omp works\.tmp\forms-independent-20261005\native-r30-terminal-r27\b1-terminal\forms-binding-loss-binding-loss-terminal.json` 中 `unobservedFaultEvidence[0]` 的 `NEndTurnButton.HasPlayableCard -> ShouldPlay` 抛错栈，在该实现下不再由 ShouldPlay 抛出。
- 真实动作路径保留显式拒绝语义：`FormStanceMode.ThrowIfSelectedFormsActionUnavailable`（第 93-97 行）复用同一 predicate 并抛 `Unavailable()`（第 99-101 行）；`RemovePrefix`（`FormStanceSafetyGuard.cs` 第 167-175 行）仍调用原 throw 版本 `ThrowIfSelectedFormsCombatUnavailable`，非 stance/null 直接返回 true，语义未变。
- Safety owner 身份/证明满足要求：owner 固定 `Forms.FormStanceSafety`（第 25 行）；`EnsureInstalled` 精确解析 `PowerCmd.Remove(PowerModel)`（第 62-68 行）与 `PlayCardAction.ExecuteAction()`（第 71-77 行）；`ProofHoldsLocked`（第 122-135 行）经 `HasExactlyOnePrefixLocked`（第 137-149 行）要求两个 target 各恰好 1 个本 owner 且 PatchMethod 匹配的前缀。`MainFile.PatchesHealthy`（`MainFile.cs` 第 45-46 行）包含 `FormStanceSafetyGuard.IsInstalled`。
- 安装原子性/回滚：两个 `harmony.Patch`、状态赋值与证明包在同一 try（第 81-98 行）；任一失败进入 catch（第 99-118 行），`UnpatchAll(HarmonyId)` 回滚并清空两个 target/prefix、`_installed=false` 后重抛；`MainFile.Initialize` 捕获后调用 `MarkOwnerPatchesFailed`（第 65-70 行），桥进入 Terminal，不会发布 Bound。
- 普通扫描不重叠：`InstallOwnPatches` 显式 `if (type == typeof(FormStanceSafetyGuard)) continue;`（`MainFile.cs` 第 105-106 行），且该类型无 `[HarmonyPatch]` 特性；扫描只处理本程序集（第 103 行）。
- Shutdown/Terminal 驻留：`MainFile.Shutdown`（第 186-211 行）只 `UnpatchAll(ModId)`，不触碰 `Forms.FormStanceSafety`；`FormStanceWatcherBridge.Shutdown`（第 294-346 行）与 `EnterTerminalLocked`（第 746-776 行）只按桥自身 `HarmonyId` 撤桥补丁。故两个 safety prefix 在 Shutdown/Terminal 后仍驻留。
- 无硬引用/工作线程 Godot：`FormStanceSafetyGuard.cs` 只引用 `System`、`System.Reflection`、`HarmonyLib`、引擎 `Commands/GameActions/Models` 与本项目类型；不引用 Watcher/Spire1 程序集，不创建线程、不访问 Godot。`MainFile` 未新增线程或 Godot 访问。
- **P1（明确 NEEDS_REWORK）**：`PlayPrefix`（`FormStanceSafetyGuard.cs` 第 185-189 行）在 async 方法的 Harmony prefix 中直接抛异常，异常发生在编译器生成的 async stub 之前，属于同步抛出，不会被 `ExecuteAction` 的异步状态机捕获为“该 action 的 faulted Task”。引擎调用点 `MegaCrit.Sts2.Core.GameActions\GameAction.cs` 第 116-134 行把 `_executionTask = TaskHelper.RunSafely(ExecuteAction())`（第 125 行）放在 `try`（第 135 行开始）之外；同步异常会直接冒出 `GameAction.Execute()`，使 `State` 停留在 `Executing`、`CompletionTask` 永不完成。`MegaCrit.Sts2.Core.GameActions\ActionExecutor.cs` 第 151 行调用 `readyAction.Execute()`，异常由第 192-196 行捕获并 `SetException`/重抛，表现为 executor/UnobservedFault 而不是 action fault。随后 `ActionQueueSet.GetReadyAction()`（`ActionQueueSet.cs` 第 228-230 行）对仍为 `Executing` 的队首 action 抛 `InvalidOperationException`，队列无法继续推进。对照 r30 证据：`forms-binding-loss-binding-loss-terminal.json` 的 `actionRejectionEvidence[*].action` 是 `status=faulted`、`state=Finished`、`completionTaskOutcome=completed`、`exception=System.AggregateException`；这正是“body/返回的 Task faulted”形态，而非同步 prefix 抛出的形态。最小修复：把 `PlayPrefix` 改为非抛出、用 `ref Task __result` 返回 `Task.FromException(<同一 Forms unavailable 异常>)` 并 `return false` 跳过原方法（需为 `FormStanceMode` 提供 internal 异常工厂或等价的 Task 工厂），从而恢复 action 级 fault/CompletionTask 语义并保持显式 restart 拒绝。

## 进行中

- 无。窄审 4 文件 delta 已完成，结论为静态 NEEDS_REWORK（唯一 P1 见上）。

## 未知

- 未编译：本窄审按请求不构建、不 lint、不测试；P1 为源码级控制流证据，非编译或运行时复现。
- 未实机：Terminal/Shutdown 下 prefix 的真实 fault 形态、owner/target 运行时计数、无能量/HP/marker 副作用、正常三形态与普通局控制均无本轮实机证据。
- r34 新测试与新控制器尚未冻结，本结论不以其为生产证据。

## 结论

- 状态：**NEEDS_REWORK**（P1 一项，源码级证据）。
- 通过项：查询只禁用不抛、Safety owner 精确证明、安装原子性/回滚、Shutdown/Terminal 驻留、普通扫描不重叠、无硬引用/工作线程 Godot 均静态成立。
- 不通过项：真实动作入口的显式拒绝被同步 prefix 抛出实现，未形成 action 级 fault，可能导致 action 队列卡死；需按上述最小修复改回 faulted Task 语义后再审。
## 已确认（P1 队列影响补充，2026-10-05 13:43 Asia/Shanghai）

- 同步抛出后 `GameAction.Execute()` 的 `_executionTask` 从未被赋值；`GameAction.State` 保持 `Executing`，`_completionSource` 从未 `TrySetResult`，故 `CompletionTask` 永不完成（`GameAction.cs` 第 116-166 行）。
- `ActionExecutor.ExecuteActions` 的 `catch (Exception)`（`ActionExecutor.cs` 第 192-196 行）会给 `_queueTaskCompletionSource` 设异常并重抛；该任务由 `TaskHelper.RunSafely`（`ActionExecutor.cs` 第 119 行）承载，异常进入 `TaskHelper.UnobservedFault` 并触发日志/Sentry。
- 队首 action 因 `AfterFinished` 从未触发，`PopAction` 未被回调，仍留在 `actionQueue.actions[0]`；下一次 `GetReadyAction()` 命中状态校验 `gameAction2.State != WaitingForExecution && != ReadyToResumeExecuting`（`ActionQueueSet.cs` 第 228-230 行），对仍为 `Executing` 的队首抛 `InvalidOperationException`，队列无法推进。
- 与 r30 权威证据的差异（同一 `forms-binding-loss-binding-loss-terminal.json`）：
  - `actionRejectionEvidence[*].action` 显示 `status=faulted`、`state=Finished`、`completionTaskOutcome=completed`、`exception=AggregateException(Forms unavailable...)`；该形态要求异常来自 `ExecuteAction()` 返回的 Task，而非 Harmony prefix 同步抛出。
  - r30 的另一类 `unobservedFaultEvidence[0]` 来自 `ShouldPlay`（UI 查询路径），本轮 `ShouldPlay` 已改为非抛出，属已修复面。
- 交叉影响（不改，仅记录）：`VoidFormPlayCardActionExecutePatch`（`VoidFormPlayTransactionPatch.cs` 第 829-843 行）的 postfix 把 `__result` 包成 `ObserveNativeActionCompletionAsync(result, state)`。若修复改为 prefix `return false` 并设 `ref Task __result = Task.FromException(...)`，该 postfix 仍会对 faulted Task 执行 `finally` 清理（第 636-643 行 `Cancel`/`CloseAction`），不会泄漏 ActionState；但 prefix 与 postfix 的执行/跳过顺序需在修复后用真实动作复验，确认 `__result` 被替换为 faulted Task 且 postfix 仍运行。