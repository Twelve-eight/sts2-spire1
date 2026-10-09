# Void transaction identity supervisor review, rework 4

- 审查类型：只读监督审查
- 实现 worker：`01a0fe27-b848-7d41-87d8-f26dbed9a467`
- 实现报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-rework4-20261002.md`
- 审查报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-reviewer-rework4-20261002.md`
- 前置条件：已通过 Codex 原生 wait_threads 等待 worker 最终回报与工作树变更；worker 已完成。
- 本轮限制：不构建、不测试、不部署、不启动游戏、不修改产品代码。

## 已确认（初始证据）

- worker 最终回报声称已修复 TransactionScenarios.cs 的 backslash-n 伪换行，并新增 native in-flight cancel/completion race 探针；尚待本审查逐项以源码和 diff 核对。
- 本报告已在进一步审查前落盘；后续按“已确认 / 进行中 / 未知”增量追加。

## 进行中

- 复核源码伪换行、native action gate/race 时序、双入口幂等清理与消费回滚边界。
- 复核既有探针保留情况、production dynamic target marker、硬依赖和本轮代码变更范围。

## 未知

- 尚未对本轮源码逐行完成审查。
- 尚未作最终 PASS/REWORK 判定。
## 已确认（伪换行与探针保留面）

- 独立读取 `tools/form-effects-probe/TransactionScenarios.cs` 后，完整字节扫描结果为：`literal_backslash_n_count=0`、`literal_backslash_r_count=0`，UTF-8 解码通过；第三轮指出的 `\n\n` 伪换行已不再存在。
- 场景清单仍包含本轮要求的既有覆盖：
  - `transaction.void_native_cancel_releases_unstarted_payment`
  - `transaction.void_native_completion_releases_unstarted_payment`
  - `transaction.void_stale_only_manual_wrapper_does_not_consume_and_next_generation_works`
  - `transaction.void_payment_without_wrapper_side_cleanup_reclaims_orphan`
  - `transaction.void_payment_without_wrapper_removal_cleanup_reclaims_orphan`
  - `transaction.void_wrapper_skip_after_cleans_pending_without_consuming`
  - `transaction.void_no_token_manual_wrapper_does_not_consume_allowance`
  - `transaction.void_after_success_tail_fault_keeps_consumed`
- native cancel/completion 场景直接调用 `CancelNativeActionForPatch` 与 `ObserveNativeActionCompletionForPatch`；未把 `ProbeEndAction` 当作 native callback。新增 race 场景同样直接调用两个 production bridge。

## 已确认（新增 probe 的静态结构）

- `transaction.void_native_inflight_cancel_completion_race_is_idempotent` 先通过 `BeginNativeAction(card)` 建立 action identity，再完成 payment，并在 `:412-415` 将未完成的 `completionGate.Task` 交给真实 `ObserveNativeActionCompletionForPatch`；`Check.False(completion.IsCompleted, ...)` 确认 observer 尚未越过 gate。
- race 结束后直接等待 observer，并重复调用 cancel/completion；随后建立同卡新 generation，走真实 `PlayTransactionCard`，最后用旧 action 的延迟 cleanup 检查成功的 `AfterCardPlayed` 消费没有被回滚。该控制流覆盖了“入口重复清理、下一代消费、旧代延迟清理”的静态断言。

## 已确认（production target、硬依赖与变更范围）

- production dynamic target marker 仍真实存在：`VoidFormPlayCardActionExecutePatch.TargetMethod()` 与 `VoidFormPlayCardActionCancelPatch.TargetMethod()` 均带 `[HarmonyTargetMethod]`；resolver 以程序集扫描加完整类型名 `MegaCrit.Sts2.Core.GameActions.PlayCardAction`，并按实例公开/非公开方法查找 `ExecuteAction` / `CancelAction`。
- transaction production 文件、`Spire1.csproj` 与 `Spire1.json` 的静态范围未发现 `Watcher`、`AutoAnthony`、`AutoAnthonyWatcher`、`DirectConnectIP` 或 `ActsFromThePast` 硬类型标识。`Spire1.csproj` 的程序集引用仅为 `0Harmony`、`sts2`，NuGet 的 BaseLib 为私有构建依赖；manifest 的运行时前置仍只有 `BaseLib >= 3.4.5`。
- rework4 worker 的提交范围只新增/修改 `tools/form-effects-probe/TransactionScenarios.cs` 与 worker 报告；`VoidFormPlayTransactionPatch.cs`、`VoidFormEffectPower.cs` 在 `0d0dcc5..cfdfeea` 范围内没有变更。本审查未修改任何产品代码。

## 阻断证据（REWORK）

### 1. 新 race 没有保证“先 cancel、后释放 gate”

`TransactionScenarios.cs:419-430` 同时启动两个 `Task.Run`：一个等待 `raceStart` 后执行 `CancelNativeActionForPatch(action)`，另一个等待同一 `raceStart` 后执行 `completionGate.SetResult(true)`，随后只等待 `Task.WhenAll(cancel, release)`。源码没有 `cancelStarted`/`cancelCompleted` 栅栏，也没有在释放 gate 前等待 cancel 完成。

因此该 probe 允许以下两种顺序：

1. `CancelNativeActionForPatch` 先执行，再释放 gate；
2. `completionGate.SetResult(true)` 先执行，completion observer 先完成清理，之后 cancel 才执行。

第二种顺序会使本 probe 不能证明用户要求的“保持 completion task in-flight，先 cancel，再释放 gate，让 completion observer 继续”。它只是一个未确定顺序的并发 race。即使在常见调度下 cancel task 看似先运行，也没有源码级保证，不能把偶然调度当监督通过证据。

### 2. 本阻断直接影响本轮验收目标

既有两个 native 场景只验证 `Task.CompletedTask` 的顺序入口；本轮新增 probe 的唯一新增价值应是覆盖 completion observer 仍挂在未完成 task 上时的 cancel-first 交错。由于 cancel-first 没有被强制，双入口幂等仍有静态意图，但本轮不能确认目标交错真的被执行。

## 未知

- 按用户要求，本审查未构建、未运行 probe、未部署、未启动游戏；因此没有把上述源码证据升级为编译、运行时、Harmony 实际安装或隔离实机通过。
- 当前 probe 传给 observer 的确实是一个未完成的 `TaskCompletionSource` task，但它仍是 probe 构造的 gate，不是实机中真实 `PlayCardAction.ExecuteAction` 产生的 task；真实 action queue、`GameAction.Cancel()` 与 continuation 竞态仍需中央动态验证。
- AssemblyRef 表、manifest 解析运行结果、部分 mod 启动矩阵及游戏内行为均未在本轮执行。

## 最终判定：REWORK

本轮静态审查通过了伪换行修复、既有场景保留、dynamic target marker、硬依赖与变更范围检查；但新增 in-flight probe 没有强制 `CancelNativeActionForPatch` 在 `completionGate.SetResult(true)` 之前完成，不能满足本轮明确的 cancel-first 验收条件。因此不是 PASS。

返工最低要求：保留未完成 completion task 和双入口幂等断言；改为源码上确定的 cancel-first 顺序（例如先启动并等待 cancel 入口完成，再释放 gate，再等待 completion observer），或增加明确的 cancel 完成栅栏后再释放 gate；随后由主会话中央构建、运行 probe、执行 AssemblyRef/manifest 门禁和隔离实机验证。

## 审查执行记录

- 已先使用 Codex 原生 `wait_threads` 等待 implementation worker `01a0fe27-b848-7d41-87d8-f26dbed9a467` 最终 turn 完成并进入 `idle`，之后才读取工作树并开始本审查。
- 本审查只读；未修改产品代码、未构建、未测试、未部署、未启动游戏。

## 第五轮返工与监督复审 - 2026-10-02

### 已确认

### 进行中

### 未知


# Void transaction identity supervisor review, rework 5

- 审查日期：2026-10-03
- 实现 worker：`01a0fe35-faae-7f02-8f73-292f22baa014`
- 实现报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-rework5-20261002.md`
- 审查报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-reviewer-rework5-20261002.md`
- 监督前置条件：已使用 Codex 原生 `wait_threads` 等待实现 worker 的最终 turn 完成并进入 `idle`，随后才读取 worker 报告、当前工作树和源码。
- 本轮限制：只读复核；不构建、不测试、不部署、不启动游戏、不修改产品代码。

## 已确认（首批落盘证据）

- worker 最终 turn 已完成；其报告声称只修改 probe `TransactionScenarios.cs` 与 worker 报告，并已提交 `f8be5c2`。
- 已读取 worker 报告和第四轮唯一监督报告；第四轮唯一阻断是 race probe 没有 cancel-first 完成栅栏。
- 当前工作树仍有大量其他会话既有改动；本轮不对其执行 reset、清理或覆盖。
- 当前 race probe 的目标源码正在进行逐行复核；最终结论尚未形成。

## 进行中

- 复核 cancel-first 栅栏、completionGate 释放顺序、observer 后续完成、双入口幂等、后续 generation、AfterCardPlayed allowance 不回滚。
- 复核 backslash-n/backslash-r 伪换行、production dynamic target marker、Watcher/AutoAnthony 等硬依赖面是否退化。

## 已确认（race 时序）

- `TransactionScenarios.cs:400` 使用 `TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)` 作为 completion gate。
- `:411-414` 先把尚未完成的 `completionGate.Task` 交给 `ObserveNativeActionCompletionForPatch`，并以 `Check.False(completion.IsCompleted, ...)` 确认 observer 尚未越过 gate。
- `:419` 同步调用 `CancelNativeActionForPatch(action)`；该 production 方法返回类型为 `void`，其清理流程在 `:591-604` 同步完成，因此调用返回构成明确的 cancel-first 完成栅栏。
- `:420` 仅在上述 cancel 调用返回后执行 `completionGate.SetResult(true)`；`:421` 随后 `await completion`，因此 observer 被释放后必须完成，异常也会使 probe 失败。
- 第四轮的无序并发 `Task.Run`、`raceStart` 和 `Task.WhenAll(cancel, release)` 已从本轮提交中删除。`git diff cfdfeea..f8be5c2` 仅显示该 probe 的时序改动与报告变更。

## 已确认（断言和边界）

- `:423-428` 在两个 native 入口均已执行后，再次调用 `CancelNativeActionForPatch` 和 `ObserveNativeActionCompletionForPatch(Task.CompletedTask, action)`，并用 `Check.Cost(..., true, ...)` 断言 token/reservation 清理是幂等的。
- `:431-437` 建立同卡新的 native action/payment generation，经过真实 `PlayTransactionCard` 后以 `Check.Cost(..., false, ...)` 断言下一代仍可消费 allowance。
- `:439-444` 对旧 action 再次执行 completion/cancel 清理，并以 `Check.Cost(..., false, ...)` 断言已经由 `AfterCardPlayed` 消费的 allowance 不被旧 action 的延迟清理回滚。
- race probe 直接调用 production 的 `CancelNativeActionForPatch` 与 `ObserveNativeActionCompletionForPatch`，未使用 `ProbeEndAction` 冒充 native callback。

## 已确认（伪换行、target marker、硬依赖）

- 对当前 `TransactionScenarios.cs` 进行 UTF-8 文本扫描：literal backslash-n 计数为 `0`，literal backslash-r 计数为 `0`；第四轮发现的伪换行问题未复发。
- `VoidFormPlayTransactionPatch.cs:811-813` 与 `:831-833` 的两个动态 target 仍真实带有 `[HarmonyTargetMethod]`，分别解析 `ExecuteAction` 与 `CancelAction`。
- 本轮提交范围不包含 `VoidFormPlayTransactionPatch.cs`、`VoidFormEffectPower.cs`、`Spire1.csproj` 或 `Spire1.json`；因此没有发现 production target marker 或依赖声明被本轮退化。
- 当前 transaction production 文件未发现 `Watcher`、`AutoAnthony`、`AutoAnthonyWatcher`、`DirectConnectIP` 或 `ActsFromThePast` 标识；`Spire1.csproj` 的实际程序集引用仍只有 `0Harmony`、`sts2`，NuGet 的 BaseLib 为私有构建依赖；`Spire1.json` 运行时依赖仍只有 `BaseLib >= 3.4.5`。csproj 中的 AutoAnthony 文字仅是说明性注释，不是 `<Reference>` 或类型引用。
- `git diff --check` 对本轮 probe 通过；本轮提交 `f8be5c2` 的目标变更没有 whitespace error。

## 未知与边界

- 按任务要求，本轮未构建、未运行 probe、未部署、未启动游戏；因此本报告只提供静态监督证据，不能宣称编译、运行时、Harmony 实际安装或隔离实机通过。
- `TaskCompletionSource` gate 验证的是 probe 中的确定性顺序；真实 action queue、Harmony patch 安装和游戏运行时线程/取消竞态仍须主会话中央动态验证。
- AssemblyRef、部分 mod 启动矩阵和隔离实机行为不属于本轮静态 reviewer 操作。

## 最终判定：PASS

本轮唯一阻断已闭合：race probe 现在明确保持 completion task in-flight，先同步完成 native cancel，再释放 `completionGate`，最后等待 completion observer；双入口幂等、后续 generation、AfterCardPlayed 不回滚、伪换行、dynamic target marker 和 hard dependency 面均未发现退化。该 PASS 仅代表第五轮只读静态监督审查通过；中央构建、probe 运行、AssemblyRef/manifest 门禁和隔离实机验证仍为后续必需步骤。

## 审查执行记录

- 已先使用 Codex 原生 `wait_threads` 等待实现 worker `01a0fe35-faae-7f02-8f73-292f22baa014` 最终 turn 完成并进入 `idle`，之后才读取 worker 报告、工作树和源码。
- 本审查未修改产品代码；未构建、未测试、未部署、未启动游戏。
