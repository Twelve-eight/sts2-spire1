# Void transaction identity supervisor review - rework2 - 2026-10-02

## 审查协议

- Implementation worker: `01a0fdbc-8a41-7e92-9b58-aab4ae6596ad`。
- 已先通过 Codex 原生 `wait_threads` 等待该 worker 完成最终 turn；其状态现为 `idle/completed`，随后才开始读取工作树与实现报告。
- 唯一审查报告路径：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-reviewer-rework2-20261003.md`。
- 禁止事项：本轮不构建、不测试、不部署、不启动游戏、不修改产品代码；仅写本审查报告。

## 已确认

### 第一条增量证据：worker 交付面与边界

- 已读取 worker 报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-rework2-20261003.md`。
- worker 明确报告第二轮已落盘的实现范围为：`VoidFormPlayTransactionPatch.cs`、`VoidFormEffectPower.cs`、`TransactionScenarios.cs` 及其报告；未构建、未运行探针、未部署、未启动游戏。
- worker 报告将 REWORK 1 至 4 标为“源码级闭合、动态边界未知”，并记录了 probe metadata shim 与 action bridge 探针仍需中央阶段验证；因此本 supervisor 不得把静态结论写成运行时 PASS。
- 本次审查从 worker 完成后开始；此前没有读取其实现内容或基于其未完成状态下结论审查。

## 进行中

- 正在逐行核对：
  1. `PowerAtStart == null` 且 wrapper 未启动时的 token owner/cleanup；
  2. payment-to-wrapper 的显式一对一 action handoff；
  3. payment 成功但 action 未到 wrapper 的 cancel/completion 清理；
  4. stale-only token + no-payment wrapper 反例探针；
  5. consumed generation、tokenless allowance、无硬依赖、线程/调度边界；
  6. 本地反编译 `PlayCardAction` 的 ExecuteAction/CancelAction 路径与动态 Harmony target。

## 未知

- 尚未形成最终 PASS/REWORK 判定。
- 尚未运行任何构建、测试、探针或游戏；所有后续结论先标为源码/反编译静态证据，直到报告末尾给出明确判定。

## 已确认

### 第二条增量证据：动态 Harmony target 当前缺少实际目标标记

- `VoidFormPlayTransactionPatch.cs:780-781` 与 `:799-800` 只有名为 `TargetMethod()` 的普通私有方法；其前面没有 `[HarmonyTargetMethod]`。
- 同文件 `:14-19` 只在 `SPIRE1_FORM_MOD && !GODOT` 下定义 probe-only 的 `HarmonyTargetMethodAttribute` 类型，但源码中没有任何 `[HarmonyTargetMethod]` 使用点；生产构建也不会使用这个 shim。
- 本地 Harmony 2.4.2 反编译 `G:\omp works\.tmp\harmony.cs:8005-8057` 显示 `GetBulkMethods()` 通过 `RunMethod<HarmonyTargetMethod, MethodBase>(...)` 查找带 `HarmonyTargetMethod` 辅助标记的方法；`harmony.cs:6235-6239` 明确该属性的作用是“Specifies the TargetMethod function in a patch class”。因此不能把“方法名是 TargetMethod”当作等价标记。
- 这直接影响 `PlayCardAction.ExecuteAction` 与 `CancelAction` 两个动态 patch 的注册：即使 `ResolveNativePlayCardMethodForPatch()` 静态解析逻辑正确，当前 patch class 未满足 Harmony 的动态目标发现契约，源码级不能确认 patch 会安装。

### 进行中

- 继续核对原生 action 的异步/取消路径、token cleanup 的精确性、probe 是否真正覆盖 action cancel/completion，以及其它硬依赖/线程边界。

### 未知

- 尚未构建或运行 Harmony，因此不能用运行结果替代上述源码与 Harmony 反编译证据。

## 已确认

### 第三条增量证据：REWORK 4 的 action cancel/completion 探针未落盘

- `TransactionScenarios.cs` 目前只出现 `ProbeBeginAction`/`ProbeEndAction`（`:531-535`）以及 side-turn、AfterRemoved、stale-only manual wrapper 等场景；全文没有 `CancelNativeActionForPatch`、`ObserveNativeActionCompletionForPatch` 或等价的 native action cancel/completion 调用。
- `VoidFormPlayTransactionPatch.cs:567-631` 确实存在生产 action bridge 的 `EnterNativeActionForPatch`、`CancelNativeActionForPatch`、`ObserveNativeActionCompletionForPatch`，但现有 probe 没有直接验证这两个清理入口。`ProbeEndAction`（`:664-676`）是 probe lease 的普通结束路径，不等价于 native `PlayCardAction.CancelAction` 或 `ExecuteAction` Task completion。
- 因此“payment 成功但 action 未到 wrapper”的两个 cleanup 边界，当前只有源码控制流，没有对应的 action cancel/completion 窄探针证据；不能把 REWORK 3/4 写成已验证闭合。

### 进行中

- 继续检查 owner orphan cleanup 是否覆盖所有未启动 wrapper 形状，以及 action bridge 与原生 `PlayCardAction` 的调度/线程边界。

### 未知

- action cancel/completion 的真实 Harmony 调用和探针运行结果均未知。

## 已确认

### 第四条增量证据：原生 action 路径与取消并不自动终止 ExecuteAction

- 本地反编译 `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:169879-169921` 显示 `PlayCardAction.ExecuteAction` 的关键顺序为：恢复 card、校验、`await _card.SpendResources()`、构造 `PlayerChoiceContext`、再 `await _card.OnPlayWrapper(...)`。
- 同文件 `:169927-169945` 的 `PlayCardAction.CancelAction` 只做 card queue / hand UI 清理，没有 payment token 参数，也没有停止 `ExecuteAction` 的 Task。
- 基类 `GameAction.Cancel()`（`:169606-169616`）将状态设为 `Canceled`、调用 `CancelAction()`、把 completion source 延后设为 canceled，但没有取消 `_executionTask`；基类 `Execute()`（`:169528-169540`）把已调用的 `ExecuteAction()` 交给 `TaskHelper.RunSafely` 并等待执行 Task 或 player-choice pause。
- 因此产品代码里的 `CancelNativeActionForPatch()` 只能先精确取消 token 并关闭 action context，不能假设原生 `ExecuteAction` 已停止；随后仍可能继续进入 `SpendResources`/`OnPlayWrapper`。当前代码对此采用 fail-closed token 状态，但没有直接 action-cancel race 探针，且 `AsyncLocal<ActionState>` 跨 `TaskHelper.RunSafely`/player-choice continuation 的保持仍是未知边界。

### 进行中

- 正在收口 consumed generation、tokenless allowance、owner bucket 作用域和无硬依赖检查。

### 未知

- 原生 `TaskHelper.RunSafely` 是否在全部实际 action continuation 上保留同一个 `ExecutionContext`，没有本轮运行证据。

## 已确认

### 第五条增量证据：消费代际、tokenless 语义与硬依赖

- 消费代际静态闭合面：`VoidFormEffectPower.cs:243-259` 只有在 `pendingFreePlay` 精确等于该 `CardPlay` 时才释放 reservation、清 blocked、记录 `consumedCard/consumedToken` 并置 `consumed=true`；`ClearFailedPlay()` 的消费回滚仅在显式 `rollbackConsumed=true` 时执行。
- 当前所有 transaction cleanup 调用点（`VoidFormPlayTransactionPatch.cs:202-203, 474-501`）都传 `rollbackConsumed:false`；因此 wrapper tail fault、action cancel/completion 或 owner/power cleanup 不会把已经由 `AfterCardPlayed` 成功消费的 allowance 回滚。
- `BeginPlay()` 对 auto-play 在 `:224-230` 直接取消 token，不进入 manual allowance 消费路径；`ClaimForBeforeCardPlayed()` 在 `:275-304` 要求 exact action、exact token、`SpendCompleted`、`Reserved`、`PowerAtStart` 和 exact reservation，未提供 token 的 wrapper 不会消费。
- `ClaimForPlay()` 在 `:425-445` 只从 `ActionState.Spend` 取得 token，未回退到同卡 bucket 的唯一候选；同卡 bucket 只用于 `CancelUnclaimedCompletedTokens()` 的 fail-closed stale 清理。
- 本审查范围内三个源码文件没有 `Watcher`、`AutoAnthony`、`AutoAnthonyWatcher`、`DirectConnectIP`、`ActsFromThePast`、`StancePower`、`StanceCmd` 等硬类型/程序集引用；`Spire1.json` 仍无这些依赖声明。`Spire1.csproj` 中出现的 `AutoAnthony` 仅为“无编译期引用”的注释，不能作为 AssemblyRef 证据，但未发现实际 `<Reference>`。

## 进行中

- 正在补齐最终静态结论、阻断项与明确 PASS/REWORK 判定。

## 未知

- 未生成新的 AssemblyRef/TypeDef 产物；硬依赖结论当前仅限源码、项目文件和 manifest 静态扫描。


# 最终判定：REWORK

## 阻断项

1. **动态 Harmony target 没有实际注册标记。**
   - `VoidFormPlayTransactionPatch.cs:780-781`、`:799-800` 定义了 `TargetMethod()`，但源码没有 `[HarmonyTargetMethod]`。
   - `:14-19` 的 probe-only shim 只是定义属性类型，并没有使用点；生产构建也不依赖该 shim。
   - 本地 Harmony 2.4.2 的 `PatchClassProcessor` 在 `harmony.cs:7788-7797` 只把带 auxiliary attribute 的方法收入 `auxilaryMethods`，随后 `GetBulkMethods():8052` 通过 `RunMethod<HarmonyTargetMethod>` 调用它；方法同名不能替代 attribute。
   - 结果：`PlayCardAction.ExecuteAction` / `CancelAction` action bridge 目前不能作为已安装的生产路径接受，REWORK 2/3 的有效性被阻断。必须先补真实 `[HarmonyTargetMethod]` 使用并保留 probe shim 的编译隔离，再进入构建验证。

2. **缺少原生 action cancel/completion 的直接窄探针。**
   - `TransactionScenarios.cs` 只有 `ProbeBeginAction` / `ProbeEndAction`，没有直接调用 `CancelNativeActionForPatch` 或 `ObserveNativeActionCompletionForPatch`。
   - `ProbeEndAction` 不是 `PlayCardAction.CancelAction`，也不是 `ExecuteAction` Task completion；不能证明 payment 成功但 action 未到 wrapper 时两个生产清理入口各自生效。
   - 必须增加至少两个独立场景：`EnterNativeActionForPatch -> payment success -> CancelNativeActionForPatch`，以及 `EnterNativeActionForPatch -> payment success -> ObserveNativeActionCompletionForPatch`，均断言 token、reservation、owner/power bucket 和 allowance 的精确结果；仍不得用同卡 bucket 猜测替代 handoff。

3. **线程/调度边界仍无动态证据。**
   - 原生 `GameAction.Execute()` 在反编译路径中调用 `TaskHelper.RunSafely(ExecuteAction())`；`Cancel()` 只设 canceled 状态并调用 `CancelAction()`，不终止 `_executionTask`。
   - 当前实现依赖 `AsyncLocal<ActionState>` 让 `SpendResources` continuation 与随后 `OnPlayWrapper` 共享 action identity；源码逻辑采取 fail-closed 是正确方向，但没有 probe/实机证据确认跨 `TaskHelper.RunSafely`、player-choice pause、cancel race 的 execution context 保持。
   - 该项在动态 target 修正和直接 action probe 完成前仍必须标为未知，不能给 PASS。

## 分面结论

- **REWORK 1（PowerAtStart 为空且 wrapper 未启动的 owner/cleanup）：源码层基本闭合，但未动态通过。** `BeginSpend` 将 token 放入 `OwnerTokens`；`CancelOrphanTokensForOwner` 在 `BeforeSideTurnStart` / `AfterRemoved` 处理 `PowerAtStart == null && BlockedPower == null && !OnPlayStarted`。当前 owner bucket 是所有 `SpendResources` token 的桶，因此该清理边界的作用域仍需运行验证，避免在 Void power 生命周期中误取消同 owner 的非 Void payment。
- **REWORK 2（payment-to-wrapper 一对一 handoff）：源码层方向正确。** `ClaimForPlay` 只认 `ActionState.Spend`、exact card、exact action、completed、未取消、未启动 wrapper；未回退到同卡唯一候选。无 handoff 的 stale/manual wrapper fail-closed 并清理 stale token。
- **REWORK 3（payment 成功但 action 未到 wrapper 的取消/完成）：实现入口存在，但生产动态 target 注册和直接探针均未闭合。** 因此不能接受为 PASS。
- **REWORK 4（stale-only 反例）：stale-only manual wrapper 场景已落盘；但 action cancel/completion 两条生产边界没有直接场景，覆盖不足。**
- **consumed generation：源码层保留 exact `consumedCard` / `consumedToken`，cleanup 统一 `rollbackConsumed:false`，未发现本轮把成功消费回滚的静态路径。**
- **tokenless allowance：源码层未发现 tokenless wrapper 消费路径；auto-play 直接取消 token，不消费 manual allowance。**
- **硬依赖：本审查范围的三个源码文件无 Watcher/AutoAnthony 等硬类型引用，manifest 未声明对应前置项；但未构建，因此没有新的 AssemblyRef 产物证据。**

## 未执行事项

- 按请求未构建、未测试、未部署、未启动游戏，也未修改产品代码。
- 因此本报告不提供任何探针 PASS 数、DLL 行为或实机结论。

## 返工完成条件

- 在两个动态 patch class 的 `TargetMethod()` 上使用真实 `[HarmonyTargetMethod]`，并让 probe-only shim 只在 probe 条件下提供同名 attribute。
- 增加直接覆盖 native cancel 与 native completion 的窄探针，验证 orphan token 的 card/owner/power/action 四侧清理及 allowance 不误消费/不误回滚。
- 由中央会话完成构建、probe、AssemblyRef/manifest 门禁，并在隔离 test copy 中验证真实 `PlayCardAction` 的 normal/cancel/completion 路径；未完成前保持 REWORK。
