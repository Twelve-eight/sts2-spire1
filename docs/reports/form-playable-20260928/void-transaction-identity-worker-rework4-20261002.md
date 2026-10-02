# Void transaction identity worker report - rework4 - 2026-10-02

## 范围与门禁

- 本轮范围严格限制为 `tools/form-effects-probe/TransactionScenarios.cs` 与本报告。
- 不构建、不测试、不部署、不启动游戏；不修改 production transaction logic。
- 先读取 reviewer 第三轮报告与当前 `TransactionScenarios.cs`，再记录本条首证据，之后才允许编辑源码。

## 已确认（首条增量证据，编辑前）

- 已读取 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-reviewer-rework3-20261002.md`。
- reviewer 明确指出 `TransactionScenarios.cs:388` 存在源码字面量 `\n\n`，不是两个真实换行；该项是本轮阻断项。
- 编辑前静态扫描 `rg -n '\\\\n|\\\\r'` 只应命中该伪换行位置；本轮需在编辑后重新扫描并确认源码不再命中 backslash-n/backslash-r 伪换行。
- 编辑前已有 `transaction.void_native_cancel_releases_unstarted_payment` 与 `transaction.void_native_completion_releases_unstarted_payment` 两个直接调用 production native cleanup 入口的场景，但当前代码仅使用 `Task.CompletedTask` 和顺序调用，未覆盖“completion task 保持未完成、cancel 与 completion 交错”的真实 in-flight race。
- 本轮将只在 probe 场景中增加该 race，使用 gate 控制未完成的 completion task，先完成 payment/action handoff，再调用 `CancelNativeActionForPatch`，随后释放 gate 让 `ObserveNativeActionCompletionForPatch` 继续；不得以 `ProbeEndAction` 代替 native cancel/completion 证据。

## 进行中

- 修复伪换行。
- 增加 native in-flight execution-task cancel race 场景，并保持已有 native cancel、native completion、stale-only、orphan side-turn、AfterRemoved、explicit action handoff、tokenless allowance、tail fault 场景。

## 未知

- 本轮禁止构建与运行，因此新 probe 的编译、运行时 action/task 调度、真实 Harmony 安装和跨 continuation 行为仍未知，必须由主会话后续中央验证。

## 已确认（伪换行修复与静态门禁）

- 已将原 `TransactionScenarios.cs:388` 的源码字面量 `\n\n` 改为两个真实 LF 换行；当前 `transaction.void_no_token_manual_wrapper_does_not_consume_allowance` 从独立源码行开始（当前约 `:478`）。
- 编辑后对完整源码执行 `Select-String` 的 `\\n|\\r` 扫描，结果为 `PASS: no backslash-n/backslash-r literal in source`；源码中不再命中 backslash-n/backslash-r 伪换行。
- 对 `TransactionScenarios.cs` 执行 `git diff --check`，无 whitespace error；命令仅报告既有的 `LF will be replaced by CRLF` 警告。
- 当前本轮 scoped status 只有 `TransactionScenarios.cs` 已修改和本报告未跟踪；`VoidFormPlayTransactionPatch.cs`、`VoidFormEffectPower.cs` 在本轮 scoped status 中无变化。

## 已确认（新增 in-flight native cancel/completion race probe）

- 新场景：`transaction.void_native_inflight_cancel_completion_race_is_idempotent`，位于 `TransactionScenarios.cs:390`。
- 场景先通过 `BeginNativeAction(card)` 建立 native action identity，再完成 `SpendResources` payment，确认 allowance reservation 存在；因此 payment 已建立到该 action 的 handoff 后才进入 race。
- `completionGate.Task` 作为未完成的 native completion task 传给真实 `ObserveNativeActionCompletionForPatch`；编辑后立即断言 observer 尚未完成，保证 completion cleanup 尚未越过 gate。
- 使用两个并发 task 同时启动真实 `CancelNativeActionForPatch(action)` 与 `completionGate.SetResult(true)`，随后 `Task.WhenAll(cancel, release)` 并等待 completion observer；没有使用 `ProbeEndAction` 作为 native cancel/completion 证据。race block 内 `ProbeEndAction` 检查为 `False`。
- race 两个入口都完成后，再次直接调用 cancel 和 completion observer，随后断言 token/reservation 已清理且 allowance 仍可用。
- 创建同卡新 action/payment generation，经过真实 `PlayTransactionCard` 顺序消费 allowance，断言该 generation 成功消费。
- 在新 generation 的 `AfterCardPlayed` 成功之后，再次对旧 action 执行 completion/cancel 清理，断言 allowance 仍为已消费，证明旧 action 延迟清理不会回滚新 generation 的成功消费。

## 已确认（既有场景保留）

- `transaction.void_native_cancel_releases_unstarted_payment` 保留。
- `transaction.void_native_completion_releases_unstarted_payment` 保留。
- stale-only manual wrapper、owner side-turn orphan cleanup、`AfterRemoved` orphan cleanup、payment-before-power/late-power、tokenless manual wrapper、tail fault/成功 `AfterCardPlayed` 保留。
- 静态名称扫描确认本轮要求的八个场景全部存在；新增 race block 不含 `ProbeEndAction`。

## 进行中

- 无需继续编辑本轮限定的两个文件；等待主会话中央收割。

## 未知

- 按用户要求本轮未构建、未运行 probe、未部署、未启动游戏，因此不能把源码静态检查升级为编译或运行时 PASS。
- 新 race 使用 gate 与并发 task 在 probe 中覆盖 cancel/completion 交错，但真实 `PlayCardAction.ExecuteAction`、`GameAction.Cancel()`、Harmony 安装、action queue 调度、Godot 线程边界及 `AsyncLocal` 跨真实 continuation 仍需主会话后续中央验证。
- 既有 production transaction logic 未在本轮改动；AssemblyRef、manifest、部分 mod 启动矩阵和隔离实机证据均未在本轮执行。

## 本轮结论

- 第三轮 reviewer 指出的源码伪换行阻断已修复并通过静态 grep 与 `git diff --check`。
- 所要求的真正 in-flight execution-task cancel/completion race probe 已落盘，直接调用两个 production native cleanup 入口，未用 `ProbeEndAction` 冒充证据。
- 本轮交付仍是静态源码级 REWORK4 完成，动态验证明确留给主会话；不宣称构建、探针或实机通过。

## 备份记录

- 本轮限定的两个文件已由提交 `cfdfeea`（`Add native transaction cancel race probe`）提交；未把其他会话已有 staged 变更纳入该提交。
- 本报告随后仅追加本备份记录；仍未构建、未测试、未部署、未启动游戏。
