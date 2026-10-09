# Void transaction identity worker report - 2026-10-02

## 已确认

### 请求与范围

- 已在原工作树完成 supervisor 指出的 orphan blocked token 生命周期返工。
- 本轮只修改以下白名单文件:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs`
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs`
  - `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-20261002.md`
- 未构建、未运行测试、未部署、未启动游戏,未写 Steam install、shared mod_configs 或 C:。

### 漏洞与修复

- 漏洞证据: 修复前 `VoidFormPlayTransactionPatch.cs:76-83` 只有 `PowerAtStart != null` 时才登记 `PowerTokens`; payment 开始时没有 Void power、后来被当前 power `BlockCard` 接管的 token 无法被 `CancelForPower` 枚举。
- `SpendToken` 在 `VoidFormPlayTransactionPatch.cs:22-34` 新增 `BlockedPower` 归属。
- `VoidFormEffectPower.cs:144-162` 的 `BlockCard` 只有在真正接管非空 token 时才调用 `AdoptBlockedToken(this, token)`。
- `VoidFormPlayTransactionPatch.cs:275-296` 的 `AdoptBlockedToken` 将 token 登记到当前 power bucket,避免重复登记,并在 blocked power 转移时先移除旧 blocked bucket 记录。
- `Cancel` 在 `VoidFormPlayTransactionPatch.cs:137-153` 同时按精确 card/token 清理 `PowerAtStart` 与 `BlockedPower` 的 power state。
- `Remove` 在 `VoidFormPlayTransactionPatch.cs:413-444` 同时从 `PowerAtStart` 与 `BlockedPower` 两个 power bucket 移除 token,随后清空 `BlockedPower`。
- 因此 `CancelForPower` 与 `AfterRemoved` 可以回收 `PowerAtStart == null`、但后来被当前 Void power block 的 token。

### consumed 与生命周期语义

- `BeforeSideTurnStart` 在 `VoidFormEffectPower.cs:264-284` 的静态顺序为 `CancelForPower -> 清 pending/reserved/blocked fields -> participants 判断`。
- owner 不在 `participants` 时,在 consumed 字段清理之前返回;保留 `consumed`、`consumedCard`、`consumedToken`,不提前刷新 allowance。
- owner 在 `participants` 时才清理 consumed generation 并刷新 allowance。
- `AfterRemoved` 在 `VoidFormEffectPower.cs:287-300` 先调用 `CancelForPower`,再清 transaction fields 与 consumed fields。
- 本轮未删除既有的 `consumedCard`、`consumedToken` 与 `rollbackConsumed` 代际逻辑;所有本轮 cleanup 仍显式使用 `rollbackConsumed: false`。

### 窄探针

- `TransactionScenarios.cs:148-180` 新增 `transaction.void_late_power_block_token_is_reclaimed`:
  - payment 在 Void power 存在前开始。
  - power 在 payment 完成前进入。
  - wrapper 启动并由新 power block token。
  - owner 缺席 side start 回收 token。
  - wrapper 收尾后,同卡下一 generation 必须能够唯一 handoff 并消费 allowance。
- 若 orphan token 仍残留在 `Tokens[card]`,下一 generation 会与新 token 形成歧义,最终 consumed 断言无法成立。
- `TransactionScenarios.cs:392-418` 的成功 helper 仍通过 `OnPlayWrapper` 进入生产 patch 适配器,并保持 `BeforeCardPlayed -> card body slot -> AfterCardPlayed` 顺序。

### 剩余检查面

- token 归属索引审查已确认:
  - `BeginSpend` 登记 `PowerAtStart` bucket。
  - `BlockCard` 登记 `BlockedPower` bucket。
  - `Cancel` 清两侧 power state。
  - `Remove` 清两侧 bucket 并清空 blocked identity。
  - `CancelForPower`、`BeforeSideTurnStart`、`AfterRemoved` 链路均已连接。
- 生命周期顺序审查已确认 owner 缺席不会刷新 consumed allowance。
- 探针顺序审查已确认 payment-before-power、side-start-before-wrapper-release、next-generation-after-cleanup 的顺序均已落盘。
- 已执行 `git diff --check`,未发现 whitespace error;仅有既有 LF/CRLF 行尾提示。

### 最终静态收口 (2026-10-02)

- **token 归属检查面**：静态核对 `BeginSpend` 在 `PowerAtStart` bucket 登记；`BlockCard` 在 token 非空时调用 `AdoptBlockedToken`；`Cancel`、`Remove` 同时覆盖 `PowerAtStart` 与 `BlockedPower`；`CancelForPower`、`BeforeSideTurnStart`、`AfterRemoved` 均通过 power bucket 回收。证据行：`VoidFormPlayTransactionPatch.cs:57-85, 137-153, 275-315, 413-444`; `VoidFormEffectPower.cs:144-162, 287-300`。
- **owner 缺席 side-turn 检查面**：`BeforeSideTurnStart` 先执行 `CancelForPower`，再清 `pending/reserved/blocked` 事务字段；`participants` 不含 owner 时立即返回，未清 `consumed/consumedCard/consumedToken`，因此没有提前刷新 allowance。证据行：`VoidFormEffectPower.cs:264-284`。
- **探针顺序检查面**：late-power orphan 场景确认顺序为 payment-before-power、power-before-payment-completion、wrapper handoff、owner-absent side start、wrapper release、next same-card generation；成功 helper 保持 `OnPlayWrapper prefix -> BeforeCardPlayed -> card body -> AfterCardPlayed` 的生产路径顺序。证据行：`TransactionScenarios.cs:148-180, 392-418`。
- **字段处置理由**：保留既有 `consumedCard`、`consumedToken` 与 `rollbackConsumed`，因为它们分别保留消费代际身份，并区分失败清理是否允许回滚真实消费；本轮所有 orphan、side-turn、wrapper abort cleanup 均传 `rollbackConsumed: false`，不得把已成功消费的 allowance 回滚。保留 `pendingToken`、`reservedToken`、`blockedToken` 与新增 `SpendToken.BlockedPower`，因为同卡不同 generation 以及付款后入场的 power 必须按 token 和 power 身份清理；没有删除任何现有字段或把 tokenless cleanup 重新引入。
- **静态收口结果**：已执行 `git diff --check`，没有 whitespace error；输出中的 LF/CRLF 转换提示属于既有行尾提示，不是本轮新增错误。未执行构建、测试、探针运行、部署或游戏启动。
## 进行中

- 本轮源码返工、窄探针增补、三面静态检查和报告整理均已完成。
- 没有遗留需要继续编辑的白名单项。

## 未知

- 未构建,因此 C# 编译、Harmony patch 绑定和实际 DLL 行为未验证。
- 未运行探针,新增场景只有源码和静态控制流证据,没有运行结果。
- 未进行真实游戏的 payment/OnPlayWrapper 并发、owner death、combat ending、多人 side turn 或 `AfterRemoved` 实机验证。
- 当前修复使用单个 `SpendToken.BlockedPower` 记录 blocked 接管者;现有 `PlayState` 也只有一个 `BlockedPower`。若未来引擎允许同一 token 同时被多个 power 实例独立 block,需要额外的多归属集合设计,本轮未覆盖该未经证实的调用形状。

## 第二轮返工报告 - 2026-10-03

### 已确认

### 进行中

### 未知

# Void transaction identity worker rework2 report - 2026-10-03

## 已确认

### 第一条落盘证据：请求、监督结论与原生路径

- 已读取第二轮返工请求、监督报告和当前三份白名单源码。监督报告明确将 REWORK 1 至 4 归因于：未启动 wrapper 的 payment token 归属不足、`OnPlayWrapper` 依赖同卡 bucket 猜测、payment 成功后 action 未到 wrapper 缺少专属清理、探针未构造两个 stale-token 反例。
- 本地反编译 `research\_decomp\game\sts2.decompiled.cs` 已定位原生 `PlayCardAction`：`PlayCardAction.ExecuteAction` 从 `SpendResources()` 返回后才调用 `CardModel.OnPlayWrapper(...)`；`CancelAction` 仅处理 action/card queue 取消，不向当前事务 patch 传递 payment token。该路径说明当前 Harmony 接口没有现成的 token 参数可供 wrapper 反向匹配，后续修复必须在白名单文件内建立显式、可回收的 action owner 或 fail-closed handoff，不能继续扩大同卡唯一候选猜测。
- 当前源码快照仍显示 `SpendToken` 仅有 `Card`、`PowerAtStart`、`BlockedPower` 和代际状态，`ClaimForPlay` 仍从 `Tokens[card]` 扫描唯一 completed token；因此 REWORK 1 至 REWORK 3 不能在未修改前确认闭合。
- 本轮严格遵守：未构建、未测试、未部署、未启动游戏；后续只修改请求白名单中的两个产品文件、一个探针文件和本报告。

## 进行中

- 正在基于原生 `PlayCardAction` 路径设计第二轮 token owner/action handoff 与 stale-token 反例探针。

## 未知

- 当前未确认新设计的 C# 编译、Harmony 绑定、探针运行或真实游戏行为；在本轮禁止事项解除前不会宣称通过。

## 已确认

### 第二条落盘证据：第二轮实现面已完成

- `SpendToken` 现在同时拥有精确 `ActionState` owner、card bucket、owner creature bucket、power bucket；`BeginSpend` 只有在当前原生 action 尚未绑定 payment 时才建立一对一 `action.Spend = token`，同一 action 的其它 card 或第二次 payment 不会覆盖该绑定。
- 新增 native action bridge：动态解析 `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction` 作为 action-context 入口，动态解析 `PlayCardAction.CancelAction` 作为成功 payment 但 wrapper 未启动时的专属清理入口。目标缺失时 prefix 对非原生对象无操作，生产消费路径 fail-closed，不再回退到 `Tokens[card]` 唯一候选猜测。
- `ClaimForPlay` 已取消同卡 bucket 猜测，只接受当前 `ActionState` 的同一 `SpendToken`，且要求 card、action identity、`SpendCompleted`、未取消、未启动 wrapper 全部匹配。没有显式 handoff 的手动 wrapper 会先取消 completed stale token，再走阻断路径，不消费 allowance。
- `PowerAtStart == null` 的 token 现在额外登记到 owner creature bucket。`BeforeSideTurnStart` 与 `AfterRemoved` 先按 owner 精确清理 `PowerAtStart == null`、尚未启动 wrapper、尚未被 blocked power 接管的 orphan token，再处理现有 power bucket；已启动并被当前 power block 的 token 仍由 `PowerTokens` 回收。
- `ClosePlayContext`、`CancelNativeActionForPatch`、`ProbeEndAction` 均不回滚已由 `AfterCardPlayed` 成功消费的 allowance；保留 `consumedCard`、`consumedToken`、`rollbackConsumed` 代际保护与 `OnPlayWrapper prefix -> BeforeCardPlayed -> card body -> AfterCardPlayed` 顺序。

### 探针新增面已落盘

- `transaction.void_payment_without_wrapper_side_cleanup_reclaims_orphan`：无 action、无 wrapper 地完成 payment，随后新进入的 Void power 触发 owner-absent side cleanup，再以新 action/payment/wrapper generation 验证旧 token 未阻塞且新 allowance 可消费。
- `transaction.void_stale_only_manual_wrapper_does_not_consume_and_next_generation_works`：构造单个 completed stale token，执行没有 payment handoff 的 manual wrapper，断言 allowance 未消费；随后新 action generation 成功消费，断言 stale token 未阻塞下一代。
- 既有 transaction 探针已改为显式 `ProbeBeginAction`/`ProbeEndAction` 包裹 payment-to-wrapper 路径；无 token、auto-play 和 stale 反例仍故意不创建 action handoff，以验证 fail-closed。

## 进行中

- 正在逐项做白名单源码静态核对：方法引用、action handoff、owner/power bucket 清理、探针顺序和报告证据行号。

## 未知

- 本轮仍未构建、未运行探针、未部署、未启动游戏；dynamic Harmony target 是否被当前 loader 按 `TargetMethod()` 正确解析尚未由运行证据确认。

## 已确认

### 第二轮最终收口与 REWORK 1 至 4 对照

> 第一条落盘证据记录的是修改前快照；以下结论记录的是本轮修改后的白名单工作树状态。

- **REWORK 1：已静态闭合，运行边界未知。**
  - `PowerAtStart == null` 的 payment token 在 `OwnerTokens[card.Owner.Creature]` 中登记，不再只有 `Tokens[card]`。
  - `VoidFormEffectPower.BeforeSideTurnStart` 和 `AfterRemoved(oldOwner)` 在清理现有 `PowerTokens` 前，按 owner 精确取消 `PowerAtStart == null && BlockedPower == null && !OnPlayStarted` 的 orphan token。
  - 已启动 wrapper 且被后进入的 Void power 接管的 token 仍通过 `BlockedPower -> PowerTokens` 回收；未启动 wrapper 的 token 由 owner 生命周期、action cancel 或 action completion 处理。
  - 未知边界：尚未运行实际 Harmony，不能证明 `PlayCardAction` target 已成功安装，也不能证明所有引擎调度都保留同一 `AsyncLocal` execution context。

- **REWORK 2：已静态闭合，运行边界未知。**
  - 动态 target 解析 `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction`；prefix 建立 `ActionState`，`BeginSpend` 将第一笔 payment 绑定到该 action，后续 `OnPlayWrapper` 只接受同一 action、同一 card、同一 token。
  - `ClaimForPlay` 已完全移除“同卡 bucket 中唯一 completed token”候选猜测；无显式 handoff 的手动 wrapper 不消费 allowance，并先清理 completed stale generations。
  - 同 action 的第二笔 payment、不同 card 的嵌套 payment、同卡嵌套 wrapper 均不覆盖或窃取原 action 的 token。
  - 本地反编译证据仍为：`PlayCardAction.ExecuteAction` 在 `SpendResources` 完成后直接调用 `OnPlayWrapper`；因此本轮 handoff 绑定在该 async action context，而不是另造 card 名称或 bucket 顺序规则。
  - 未知边界：dynamic Harmony 的空 target annotation 在当前游戏 loader 中的实际解析，以及 `AsyncLocal` 在 `TaskHelper.RunSafely`/action continuation 下的运行行为，均未实测。

- **REWORK 3：已静态闭合，运行边界未知。**
  - 动态 target 解析 `PlayCardAction.CancelAction`；prefix 对 action state 中尚未 claim 的 token 做精确 `Cancel(token)`，清理 reservation、owner bucket、power bucket 和 action handoff。
  - `ExecuteAction` postfix 观察 action Task completion；action 正常完成或抛错时，如果 token 仍存在则按同一 action state 精确取消，随后关闭 action context。
  - `BeforeSideTurnStart`/`AfterRemoved` 是 owner orphan 的独立清理边界，不依赖下一代同卡 wrapper 猜测；`AfterCardPlayed` 已成功消费的 allowance 不被 tail fault 或 cleanup 回滚。
  - 未知边界：尚未验证 cancellation 与 completion 在真实 action queue 中的先后竞态，也未验证 native action patch 与其他 mod Harmony patch 的组合顺序。

- **REWORK 4：源码层已新增完整构造，尚未运行。**
  - `transaction.void_payment_without_wrapper_side_cleanup_reclaims_orphan` 构造 payment success、无 action/wrapper、power 后进入、owner-absent side cleanup，再验证下一 generation 可消费。
  - `transaction.void_payment_without_wrapper_removal_cleanup_reclaims_orphan` 使用 `AfterRemoved` 作为唯一清理边界，再验证下一 generation 可消费。
  - `transaction.void_stale_only_manual_wrapper_does_not_consume_and_next_generation_works` 构造恰好一个 completed stale token，执行无 payment handoff 的 manual wrapper，断言 allowance 不消费，再用新 action generation 验证不被旧 token 阻塞。
  - 既有 payment/play 探针已改用 `ProbeBeginAction`/`ProbeEndAction` 复现显式 action handoff；auto-play、无 token wrapper 和 stale wrapper 仍不创建 handoff，以保持 fail-closed 反例。

### 白名单与禁止事项

- 本轮只修改了请求白名单中的：
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs`
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs`
  - `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-rework2-20261003.md`
- `git diff --check` 对上述文件无 whitespace error；仅报告既有 LF/CRLF 转换提示。
- 未构建、未测试、未部署、未启动游戏；未写 Steam 安装、shared `mod_configs` 或 C:。

## 进行中

- 本轮实现和静态检查已完成；没有继续修改白名单文件的待办项。
- 等待中央会话在允许的后续阶段执行构建、探针、隔离实机和最终 supervisor 复审；本 worker 不代替这些验证。

## 未知

- 未构建：C# 编译、nullable/反射 target 签名、Harmony patch 安装结果未确认。
- 未运行探针：新增场景目前只有源码控制流证据，没有 PASS/FAIL 运行计数。
- 未部署、未启动游戏：没有真实 payment/action cancel/action completion、owner death、combat ending、多人 side turn 或 power removal 证据。
- 未验证 `PlayCardAction.ExecuteAction`/`CancelAction` dynamic target 在当前 `0Harmony.dll` 与游戏 loader 下的实际安装；源码使用的 `HarmonyPatch(typeof(CardModel), null)` 是为了兼容 probe 的 metadata-only Harmony stub，实际 target 解析仍需后续构建/运行门禁确认。
- 未验证跨线程/跨调度 continuation 中 `AsyncLocal<ActionState>` 的保持；若真实 action queue 不保留该 execution context，生产路径应继续 fail-closed，不得恢复同卡 bucket 猜测。
- 因以上未知边界，本报告结论是“REWORK 1 至 4 已完成源码级闭合，但尚未获得动态 PASS”，不是“已通过游戏实测”。

## 第三条落盘证据：静态发现 probe 元数据桩缺口（2026-10-02）

### 已确认

- 当前第二轮产品补丁的两个动态 Harmony target 使用 `[HarmonyTargetMethod]`，这是实际 Harmony 2.4.2 `PatchClassProcessor.GetBulkMethods()` 识别 `TargetMethod()` 的必要辅助标记；本地 Harmony 反编译证据为 `G:\omp works\.tmp\harmony.cs:8028-8057`。
- `tools/form-effects-probe/ContractStubs.cs` 只提供 metadata-only `HarmonyPatch(Type, string)`、`HarmonyPrefix`、`HarmonyPostfix`、`HarmonyFinalizer` 等桩，没有 `HarmonyTargetMethodAttribute`。因此第二轮源码在禁止构建期间已静态暴露一个 probe 编译阻断：`VoidFormPlayTransactionPatch.cs` 的 `[HarmonyTargetMethod]` 在 probe 引用面没有定义。
- 该缺口不改变产品运行时的 dynamic target 设计，但必须在白名单产品文件内补一个 probe-only attribute shim，不能删除真实 Harmony 的 target 标记，也不能把生产路径退回同卡 bucket 猜测。

### 进行中

- 将在 `VoidFormPlayTransactionPatch.cs` 内加入仅在 `SPIRE1_FORM_MOD && !GODOT` 下生效的 `HarmonyLib.HarmonyTargetMethodAttribute` metadata-only shim。probe csproj 定义 `SPIRE1_FORM_MOD` 且不定义 `GODOT`；生产 `Spire1.csproj` 不定义 `SPIRE1_FORM_MOD` 并由 Godot SDK提供 `GODOT`，所以实际产品使用引用的 Harmony 2.4.2 attribute。

### 未知

- 仍不构建 probe 或产品，故 shim 与当前编译器/SDK 的实际编译结果待中央会话验证。

## 第四条落盘证据：action bridge 探针缺口（2026-10-02）

### 已确认

- 第二轮已加入 `PlayCardAction.ExecuteAction`/`CancelAction` 动态桥，但现有新增探针只覆盖了 probe action lease、side-turn、power removal 和 stale-only fail-closed，尚未直接调用 `CancelNativeActionForPatch` 或 `ObserveNativeActionCompletionForPatch`。
- 请求要求在加入 action bridge 时同时覆盖 action cancel 或 completion cleanup；因此仅有 owner/power cleanup 探针不足以闭合 REWORK 4。

### 进行中

- 在 `VoidFormPlayTransactionPatch.cs` 增加 probe-only native-action marker/helper，使探针可以调用生产 action bridge 的 `EnterNativeActionForPatch` 与 `CancelNativeActionForPatch`，不复制事务算法。
- 在 `TransactionScenarios.cs` 增加 payment success -> native action cancel -> allowance remains available 的场景；不启动 `OnPlayWrapper`，直接验证 action cancel 专属精确回收。

### 未知

- 真实游戏 `PlayCardAction.CancelAction` 的 Harmony 安装、action queue 先后顺序和跨 continuation 行为仍须中央会话在后续允许阶段验证。
