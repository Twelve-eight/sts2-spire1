# Void transaction identity reviewer report - rework3 - 2026-10-02

## 范围与门禁

- 已使用 Codex 原生 `wait_threads` 等待 implementation worker `01a0fe0a-e1b0-71e1-a840-29237191988c` 完成；其最终 turn 已报告 `completed/idle`，之后才开始本审查。
- 实现报告：`G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-rework3-20261002.md`。
- 本审查只读：不构建、不测试、不部署、不启动游戏、不修改产品代码。

## 已确认（第一条增量证据）

- 已读取 implementation worker 的第三轮最终报告。报告声称两个动态 target 已加 `[HarmonyTargetMethod]`，并新增了直接调用 `CancelNativeActionForPatch` 与 `ObserveNativeActionCompletionForPatch` 的两个窄探针；这些先记为“待源码复核”，不把 worker 自述当作通过证据。
- 本报告是唯一监督报告，后续每完成一个审查面即追加证据与结论。

## 进行中

- 从源码、Harmony 2.4.2 反编译证据和 probe 场景逐项复核：动态 target marker、target 解析、cancel/completion 清理、竞态幂等、生命周期反例、消费代际和硬依赖。

## 未知

- 尚未独立确认本轮产品代码的实际 marker、调用点、清理字段和 manifest/AssemblyRef 结果。

## 已确认（动态 target 与 target 解析审查）

- `G:\omp works\.tmp\harmony.cs:6235-6239` 定义了真实 `HarmonyLib.HarmonyTargetMethod`；`G:\omp works\.tmp\harmony.cs:8028-8057` 显示 `PatchClassProcessor.GetBulkMethods()` 在发现该 marker 后调用无参 `TargetMethod()` 并把非空返回值加入目标集合。
- `VoidFormPlayTransactionPatch.cs:808-821` 的 `VoidFormPlayCardActionExecutePatch` 使用 `[HarmonyTargetMethod]`，其 `TargetMethod()` 返回 `ResolveNativePlayCardMethodForPatch("ExecuteAction")`；`:828-837` 的 `VoidFormPlayCardActionCancelPatch` 同样使用 marker 并返回 `CancelAction` 目标。两个 patch class 的 marker 和 prefix/postfix 调用均真实存在，不是仅写在报告里的声明。
- `ResolveNativePlayCardMethodForPatch`（`:712-726`）按完整类型名 `MegaCrit.Sts2.Core.GameActions.PlayCardAction` 扫描已加载程序集，再以 `Instance | Public | NonPublic` 查找指定方法。
- 反编译原生证据 `research\_decomp\game\sts2.decompiled.cs:169835-169920,169927-169945` 显示 `PlayCardAction` 自己声明 `protected override async Task ExecuteAction()` 和 `protected override void CancelAction()`；名称、实例可见性和返回类型与桥接目标相符。静态目标解析面通过；实际 Harmony 安装仍不因静态证据升级为运行时通过。

## 进行中

- 正在复核 action cleanup 的精确 token/reservation 清理、`Cancel()` 不终止 execution task 的竞态，以及各个 transaction probe 的直接覆盖关系。

## 已确认（原生 action cleanup 与直接 probe 覆盖）

- `TransactionScenarios.cs:294-342` 直接调用 `VoidFormPlayTransaction.CancelNativeActionForPatch(action)` 两次，再直接调用 `ObserveNativeActionCompletionForPatch(Task.CompletedTask, action)`；没有用 `ProbeEndAction` 冒充 native cancel/completion。该场景在 cleanup 前检查 reservation 存在（`:310`），cleanup 后检查 token/reservation 被释放且下一代可消费（`:318-328`），再用旧 action 的延迟 cleanup 检查成功消费不被回滚（`:330-332`）。
- `TransactionScenarios.cs:344-388` 直接调用 `ObserveNativeActionCompletionForPatch` 两次，使用 `Task.CompletedTask` 覆盖正常 completion，不依赖 `OperationCanceledException`；之后同样检查下一代消费和旧 action 延迟 completion 不回滚（`:360-378`）。
- `TransactionScenarios.cs:625-632` 显示 native 场景的 `BeginNativeAction` 使用 `ProbeBeginNativeAction`，只有手动 lease 场景才使用 `ProbeEndAction`。因此“直接调用两个生产桥入口”的覆盖要求已满足，且没有把 helper lease 当作 native callback。
- `CancelNativeActionForPatch`（`VoidFormPlayTransactionPatch.cs:591-604`）按 `ActionState.Spend` 精确取消并关闭 action；`ObserveNativeActionCompletionForPatch`（`:606-638`）在正常、异常和取消 task 路径的 `finally` 再次精确取消并关闭。`Cancel`（`:196-212`）清 reservation/pending/blocked 对应 power 字段并调用 `Remove`；`Remove`（`:528-558`）清 card、owner、PowerAtStart、BlockedPower、action handoff 索引。静态清理链完整。
- 原生反编译 `GameAction.Cancel()`（`sts2.decompiled.cs:169606-169616`）确实只设置 canceled state、调用 `CancelAction()`，再 deferred cancel completion source，没有停止 `_executionTask`；实现因此采用 cancel 与 completion 双入口的幂等清理，探针也重复调用两边。这一竞态处理逻辑静态上自洽。

## 进行中

- 继续复核 payment 未启动 wrapper、stale-only、orphan side-turn、AfterRemoved、consumed generation、tokenless allowance 以及交叉 mod 硬依赖。

## 阻断证据（探针源码存在未转义文本）

- 独立读取 `TransactionScenarios.cs` 发现第 `388` 行包含字面量 `\n\n`：
  `});\n\n        suite.Add("transaction.void_no_token_manual_wrapper_does_not_consume_allowance", async () =>`
- `rg -n '\\\\n|\\\\r' TransactionScenarios.cs` 只命中该行。该内容不是行尾换行，而是 C# 源码中的反斜杠和字母 `n`，会使前一条语句与下一条 `suite.Add` 连在非法 token 中；在未构建限制下，这已经是静态源码阻断，不能等运行探针再判断。
- 因此当前不能给出 PASS；至少需要把该字面量替换为真实换行并重新进行中央构建/探针门禁。后续仍继续完成其余审查面，以区分已通过面与阻断项。

## 已确认（硬依赖与 manifest 面）

- `VoidFormPlayTransactionPatch.cs`、`VoidFormEffectPower.cs`、`TransactionScenarios.cs` 中没有 `Watcher`、`AutoAnthony`、`AutoAnthonyWatcher`、`DirectConnectIP` 或 `ActsFromThePast` 标识，也没有对应 typed `using`。
- `mod/Spire1.csproj:21-29,41-50` 的实际程序集引用为 `0Harmony`、`sts2`，NuGet 依赖为 BaseLib/分析器/打包器；没有 Watcher 或 AutoAnthony 的 `<Reference>`/`<ProjectReference>`。第 32-38 行只是说明旧引用已移除的注释，不能构成 AssemblyRef。
- `mod/Spire1.json` 的 `dependencies` 只有 `BaseLib >= 3.4.5`，没有 Watcher、AutoAnthony 或其他可选 mod 前置项。静态硬依赖面通过；AssemblyRef 仍需中央构建后的门禁确认，本审查未构建。

## 已确认（token 生命周期、反例与代际语义）

- `BeginSpend`（`VoidFormPlayTransactionPatch.cs:93-144`）把每个 payment 登记到 card、owner creature 和 `PowerAtStart` bucket；native action 的第一笔 payment 绑定到 `ActionState.Spend`，第二笔/不同 card 不覆盖已有 handoff。`Remove`（`:528-558`）同时移除这些索引并清 `ActionState.Spend` 与 `BlockedPower`。
- `CancelOrphanTokensForOwner`（`:407-428`）覆盖 `PowerAtStart == null`、未被 blocked power 接管且 wrapper 未启动的 owner-only token；`BeforeSideTurnStart`（`VoidFormEffectPower.cs:264-285`）和 `AfterRemoved`（`:288-302`）都先调用 owner orphan cleanup 与 power-bucket cleanup，再清 pending/reserved/blocked 字段。对应 probe 分别在 `TransactionScenarios.cs:233-262`、`:264-293`。
- stale-only wrapper 场景（`TransactionScenarios.cs:401-433`）不创建 action handoff，生产 `ClaimForPlay`（`VoidFormPlayTransactionPatch.cs:432-474`）拒绝猜测并取消已完成 stale token；随后显式新 generation 仍可消费。无 token manual wrapper（`:388-399`）和 auto-play（`:142-152`）均不应消费 manual allowance。
- `AfterCardPlayed`（`VoidFormEffectPower.cs:243-261`）只在精确 pending card/token 匹配时释放 reservation、清 blocked 并写入 `consumedCard/consumedToken`；所有 action cancel/completion/abort 清理均传 `rollbackConsumed:false`，旧 generation 的延迟清理不会回滚新 generation 已成功消费的 allowance。owner 缺席 side-turn 保留 consumed 字段（`:277-284`），owner 在场才刷新代际。
- tokenless allowance、stale-only、orphan side-turn、AfterRemoved、consumed generation 的源码控制流与对应场景均已覆盖；由于禁止构建/运行，以上仍是静态证据，不是动态 PASS。

## 未知与覆盖边界

- 两个 native 场景是直接调用生产 cleanup 入口，但使用 `Task.CompletedTask` 和顺序调用，未实际启动一个仍在运行的 `ExecuteAction` task 后再调用 `GameAction.Cancel()`，也未观察真实 action queue/`TaskHelper.RunSafely` continuation。它们证明了入口的重复调用幂等设计，不能证明真实跨调度 `AsyncLocal<ActionState>` 行为。
- 静态推演显示 cancel 后仍继续执行的原生 action 会沿 closed `ActionState` fail-closed：`GetActiveAction` 跳过 `Closed` state，后续无显式 handoff 的 wrapper 不会认领旧 allowance；但若取消后的 execution task 在 wrapper 之外又成功创建一个新、无 action owner 的 payment token，其最终回收仍依赖 owner/power 生命周期，未有实际 continuation 探针证据。

## 最终判定：REWORK

### 阻断项

1. `TransactionScenarios.cs:388` 有字面量 `\n\n`，不是两个真实换行。该 worker commit `0d0dcc5` 的 `git blame` 直接归属于本轮交付。此处会把两个 C# 语句拼成非法源码；在未构建限制下已足以判定本轮不能 PASS。

### 已通过的静态监督面

- Harmony 2.4.2 的真实 `HarmonyTargetMethod` marker 存在，两个 patch class 都使用；动态 resolver 与反编译的 `PlayCardAction.ExecuteAction`/`CancelAction` 签名相符。
- 两个生产 cleanup 入口已被 probe 场景直接调用，重复 cancel、completion、旧 action 延迟 cleanup 和下一 generation 消费均有源码断言；未用 `ProbeEndAction` 冒充 native callback。
- token/reservation 清理覆盖 card、owner、PowerAtStart、BlockedPower、action handoff；stale-only、owner-absent side-turn、AfterRemoved、consumed generation、tokenless allowance 的静态控制流闭合。
- transaction 文件无 Watcher/AutoAnthony/AFTP 等 typed hard reference；`Spire1.csproj`/`Spire1.json` 静态只保留 BaseLib 作为 mod 前置，其他 mod 走字符串/反射桥。

### 返工后的必要验收

- 先把第 388 行的字面量 `\n\n` 改为真实换行，重新运行 `git diff --check`。
- 中央构建 probe 与 Release DLL；重新执行 AssemblyRef/manifest 门禁。
- 运行两个 native action 场景；再补或明确记录一个真实“execution task 仍在运行时调用 cancel，之后完成/继续到 payment/wrapper”的窄探针，确认 `AsyncLocal<ActionState>` 和 fail-closed 清理在实际 continuation 下成立。
- 本审查本身未构建、未测试、未部署、未启动游戏，也未修改产品代码；因此没有把静态通过面写成动态 PASS。
