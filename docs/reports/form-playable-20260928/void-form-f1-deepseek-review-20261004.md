# VoidForm F1 只读审查 (2026-10-04)

审查员: 只读审查, 未修改产品代码/构建/部署/游戏/共享配置.
指定模型: `global:deepseek-v4.1-flash`, 路由 `gateway/wb2api`, 思考层级 `max`.
输入: `VoidFormEffectPower.cs` (271 行), `VoidFormPlayTransactionPatch.cs` (774 行), `TransactionScenarios.cs` (736 行).

## 已确认

### C1. 支付 Task 完成前不会放行 ClaimForPlay (审查目标 1)

- 证据: `VoidFormPlayTransactionPatch.cs:185-200` `ObserveSpendCompletionAsync` 只有在 `await result` 成功返回后才调用 `MarkSpendCompleted`; 异常/取消路径调用 `Cancel` 并重抛.
- 证据: `VoidFormPlayTransactionPatch.cs:518-532` `MarkSpendCompleted` 仅在 `!token.Cancelled && bucket.Items.Contains(token)` 时置 `SpendCompleted = true`.
- 证据: `VoidFormPlayTransactionPatch.cs:443-452` `ClaimForPlay` 要求 `actionToken.SpendCompleted`, 因此真实支付 Task 未完成时 token 不可能被认领.
- 证据: `VoidFormEffectPower.cs:213-241` `BeforeCardPlayed` 依赖 `ClaimForBeforeCardPlayed`, 后者在 `VoidFormPlayTransactionPatch.cs:303-313` 再次要求 `token.SpendCompleted`.
- 结论: 目标 1 源码层面 PASS (以 await 语义为准, 未做实机验证).

### C2. 每次支付独立 token, 按引用匹配, 延迟 cleanup 不误清新代 (审查目标 2)

- 证据: `BeginSpend` 每次调用 `new SpendToken` (`VoidFormPlayTransactionPatch.cs:97-104`), 分别加入 card/owner/power 三个 bucket (`:130-149`).
- 证据: 认领与清理全部用 `ReferenceEquals(token.Action, state.Action)` (`:308`), `ReferenceEquals(actionToken.Action, action)` (`:448`), `ReferenceEquals(data.reservedToken, token)` (`VoidFormEffectPower.cs:128-131`), 没有值相等或跨代复用.
- 证据: `Remove` 从所有 bucket 移除并将 `token.Action = null` (`:534-564`); 已移除 token 无法再命中 `Actions.TryGetValue` 路径下的 `state.Spend`.
- 证据: 探针 `TransactionScenarios.cs:294-342` (native cancel), `:344-388` (native completion), `:390-466` (cancel/completion race) 均包含旧 action 延迟清理后新代消费仍成立的断言 (`:328`, `:332`, `:375`, `:378`, `:437`, `:444`).
- 结论: 目标 2 源码层面 PASS.

### C3. 异常/取消/动作取消与完成/移除/回合清理的幂等性 (审查目标 3)

- 证据: `Cancel` 先置 `Cancelled`, 再 `ClearFailedPlay(rollbackConsumed: false)` 并 `Remove` (`:202-218`); 重复调用在 bucket 缺失时仍安全 (`:204-214`).
- 证据: native action completion 的 `finally` 同时执行 `Cancel` 与 `CloseAction` (`:622-643`), 重复观察不抛异常; 探针 `:315-317` 与 `:365-366` 连续双调用验证.
- 证据: `BeforeSideTurnStart` (`VoidFormEffectPower.cs:264-285`) 先 `CancelOrphanTokensForOwner` 再 `CancelForPower`, 且仅 `participants.Contains(Owner)` 时刷新 allowance, 注释与代码一致.
- 证据: `AfterRemoved` (`:288-302`) 清理全部字段并取消该 power 相关 token.
- 证据: `CancelOrphanTokensForOwner` 只回收 `PowerAtStart == null && BlockedPower == null && !OnPlayStarted` 的 token (`:428-431`), 避免误清已绑定代.
- 结论: 目标 3 源码层面 PASS; 幂等性证据来自源码结构与探针静态断言, 未做构建/实机.

## 进行中

- 审查目标 4: BeforeCardPlayed/AfterCardPlayed 的 CardPlay 对象身份与免费出牌消费一次.
- 审查目标 5: 探针 cancel-first in-flight 交错覆盖度与 production dynamic target marker.
- 历史报告仅作上下文, 未采信其 PASS.

## 未知

- 未做构建/编译验证 (按要求只读, 不构建).
- 未做实机/游戏内验证; 探针结论均为静态结构证据.
- `TransactionScenarios.cs` 中 cancel-first in-flight 场景是否真正覆盖 native 执行期交错, 待目标 5 逐行核对.
- production `ResolveNativePlayCardMethodForPatch` 在真实运行时的 target 解析是否命中, 未在运行时验证.

## 已确认 (第二批)

### C4. BeforeCardPlayed / AfterCardPlayed 的 CardPlay 对象身份 (审查目标 4)

- 引擎证据: `.tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1915-1926` 每轮 play loop 新建 `CardPlay cardPlay`, 随即 `await Hook.BeforeCardPlayed(combatState, cardPlay)`; `:1965` 在卡牌逻辑/附魔/affliction 之后 `await Hook.AfterCardPlayed(combatState, choiceContext, cardPlay)` — 同一个实例.
- 引擎证据: `.tmp/dllsrc/MegaCrit.Sts2.Core.Entities.Cards/CardPlay.cs:7-11` 明确 "Represents a single instance of a card being played... distinguishing between multiple plays of the same card during the same turn", 是引用身份类.
- 产品证据: `VoidFormEffectPower.cs:230-239` `BeforeCardPlayed` 只在 `cardPlay.IsFirstInSeries` 且 `ClaimForBeforeCardPlayed` 返回非 null 时写 `data.pendingFreePlay = cardPlay`; `:246` `AfterCardPlayed` 只在 `ReferenceEquals(data.pendingFreePlay, cardPlay)` 成立时消费.
- 进入牌不消费: `VoidFormEffectPower.cs:213-228` 要求 `!cardPlay.IsAutoPlay && cardPlay.IsFirstInSeries`; `VoidFormPlayTransactionPatch.cs:292-313` `ClaimForBeforeCardPlayed` 要求 `state.Action != null && state.Spend != null && token.SpendCompleted && token.OnPlayStarted && token.Reserved && ReferenceEquals(token.Action, state.Action) && ReferenceEquals(token.PowerAtStart, power) && power.IsReservationFor(...)`. 进入时没有真实 `SpendResources` 调用 -> 无 token -> `BeginPlay` 走 `:254-263` 分支写 `BlockCard`, 不写 pending. 故进入牌不消费.
- 真实免费出牌只消费一次: `ShouldSkip` (`:51-79`) 的 `data.pendingFreePlay != null` 与 `data.consumed` 双重门禁; `AfterCardPlayed` 成功路径设置 `consumed=true` 并清空 pending (`:249-258`); 重复 `AfterCardPlayed` 同一 `CardPlay` 因 pending 已为 null 而不会二次消费.
- 结论: 目标 4 源码层面 PASS.

### C5. 探针覆盖与 production dynamic target marker (审查目标 5)

- cancel-first in-flight 交错**真实存在**: `TransactionScenarios.cs:390-466`. 控制流: `:397` 建立 native action; `:404-407` 完成 payment 且 action 持有 token; `:411-413` 把**未完成**的 `completionGate.Task` 交给真实 `ObserveNativeActionCompletionForPatch`; `:414` `Check.False(completion.IsCompleted)` 证明 observer 仍被门住; `:419` 先同步执行 `CancelNativeActionForPatch` (即 cancel-first 栅栏); `:420` 之后才释放 gate; `:421` await completion. 随后 `:424-428` 重复双入口验证幂等; `:431-437` 新代消费; `:440-444` 旧 action 延迟清理不回滚 `AfterCardPlayed`.
- production dynamic target marker 存在: `VoidFormPlayTransactionPatch.cs:829-843` (Execute) 与 `:849-858` (Cancel) 均带 `[HarmonyTargetMethod]`; resolver `:718-733` 以 `AppDomain.CurrentDomain.GetAssemblies()` 扫描完整类型名 `MegaCrit.Sts2.Core.GameActions.PlayCardAction`, 再取实例 `Public|NonPublic` 的 `ExecuteAction`/`CancelAction`.
- 引擎契约匹配: `.tmp/dllsrc/MegaCrit.Sts2.Core.GameActions/PlayCardAction.cs:62` `protected override async Task ExecuteAction()`, `:110` `protected override void CancelAction()`; `:92` `await _card.SpendResources()`, `:103` `await _card.OnPlayWrapper(... isAutoPlay: false ...)` — 与 patch 假设的 action -> payment -> wrapper 顺序一致.
- 边界(不构成 PASS 证据): 探针的 `BeginNativeAction` 用 `ProbeNativeActionIdentity` 私有类型 (`:691-705`), 生产 `IsNativePlayCardAction` 在 `SPIRE1_FORM_MOD && !GODOT` 下接受该类型 (`:709-713`), 所以探针调用的是同一批生产清理函数, 但 `ProbeNativeActionIdentity` 不是真实 `PlayCardAction` 实例; 真实 Harmony 目标解析只在生产程序集中生效, 未在运行时验证.
- 结论: 目标 5 的探针静态结构证据成立; production dynamic target 的**运行时命中**属于未验证边界.

## 进行中

- 复核异常/取消/移除/回合清理与 `blockedCard` 交互是否存在可触发的 P1/P2 场景.
- 复核探针 orphan 场景 (`:233-262`, `:264-293`) 与生产 `BeginSpend` 注册行为的一致性.

## 未知

- 未构建/未编译, 未运行探针, 未实机验证.
- Harmony `[HarmonyTargetMethod]` 在真实运行时是否命中 `PlayCardAction` 未验证.
- 未验证 `TargetMethod()` 返回 null 时 Harmony 的注册行为 (注释声称 "prefix is harmless", 未实测).

## 已确认 (第三批: 目标 3 深挖与边界核对)

### C6. 回合清理/移除路径的幂等与代际隔离

- 证据: `VoidFormEffectPower.cs:264-285` `BeforeSideTurnStart` 先 `CancelOrphanTokensForOwner(Owner)` 再 `CancelForPower(this)`, 然后无条件清空 pending/reserved/blocked, 仅在 `participants.Contains(Owner)` 时清 consumed 并重置 allowance.
- 证据: `:288-302` `AfterRemoved` 同样清理并重置全部状态.
- 证据: `VoidFormPlayTransactionPatch.cs:394-411` `CancelForPower` 先快照 bucket 再逐个 `Cancel`; `Cancel` 在 `:204-214` 对已移除 token 幂等.
- 关键: `CancelForPower` 只取消该 power bucket 中**仍注册**的 token; 已成功消费的 token 在 `AfterCardPlayed` -> `CompletePlay` -> `Remove` 中已从 power bucket 移除 (`:552-563`), 因此 `BeforeSideTurnStart` 不会回滚已消费状态 — 与 `AfterCardPlayed` 设 `consumed=true` 的顺序一致.
- 结论: 目标 3 源码层面 PASS.

### C7. 支付 Task 完成前不会放行: 逐条复核

- `ClaimForPlay` (`:443-452`) 的 action handoff 分支要求 `actionToken.SpendCompleted`.
- `ClaimForBeforeCardPlayed` (`:302-313`) 要求 `token.SpendCompleted`.
- `BeginSpend` 的 fail-closed 分支 (`:115-124`) 在无 action 时不注册任何 bucket, 也不产生 reservation.
- `ObserveSpendCompletionAsync` (`:185-200`) 只在 `await result` 成功后置位; `catch` 走 `Cancel` + rethrow.
- 结论: 目标 1 PASS (源码层面).

### C8. 探针 orphan 场景与生产注册一致性

- 证据: `TransactionScenarios.cs:233-262` 场景在**无 action**下先 `SpendResources`; 由于 `BeginSpend` 的 `action == null` 分支 (`:115-124`) 提前返回, 该 token 从不进入任何 bucket; 随后 `StartSide` 的 orphan 回收路径实际未被该 token 触发, 但断言仍成立 (allowance 保持可用).
- 证据: `:264-293` 场景同理, 走 `AfterRemoved`.
- 判定: 探针断言结果正确, 但这两个场景的标签 ("reclaims orphan") 与生产 `BeginSpend` 的 fail-closed 行为叠加后, 并不能单独证明 `CancelOrphanTokensForOwner` 对**已注册 token** 的回收逻辑; 该回收逻辑的覆盖来自 `:183-231` (late power block token is reclaimed, 那里 token 在 power 存在后才注册) 与 `:294-388` native 场景.
- 这是探针覆盖度说明, 不是产品缺陷; 列为 P3 观察项.

## 发现

### P3-1. 探针 orphan 场景标签与 fail-closed 分支叠加, 覆盖证据弱于标签表述

- 路径: `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs:233-262` 与 `:264-293`.
- 触发条件: 无 action 时 `BeginSpend` 提前返回 (`VoidFormPlayTransactionPatch.cs:115-124`), token 未注册.
- 当前控制流: 场景仍断言 allowance 可用, 结果正确, 但没有真正驱动 `CancelOrphanTokensForOwner` 的 bucket 遍历分支.
- 最小修复范围: 仅在探针中把该场景改为先建立 action 或直接注册 token 后再走 side/removal 清理; 不改产品代码.
- 尚缺证据: 未运行探针, 未验证真实注册 token 的 orphan 回收在探针中的断言强度.

### P3-2. 探针 native 路径使用 ProbeNativeActionIdentity, 非真实 PlayCardAction 实例

- 路径: `VoidFormPlayTransactionPatch.cs:691-705` (`ProbeBeginNativeAction`), `:707-716` (`IsNativePlayCardAction` 的探针分支).
- 触发条件: 探针工程 (`SPIRE1_FORM_MOD && !GODOT`) 下 native 场景.
- 当前控制流: 探针调用的是同一批生产清理函数, 但 action identity 是私有占位类型; 真实 `PlayCardAction` 的 `ExecuteAction`/`CancelAction` 与 Harmony 目标解析未在探针内执行.
- 最小修复范围: 不改产品; 如要闭合, 需在实机/集成层验证 Harmony 命中与真实 action 生命周期.
- 尚缺证据: 实机 Harmony 注册日志, 真实 `PlayCardAction` 的 cancel-first 交错.

## 未知

- 未构建/未编译; 未运行探针; 未实机验证 (按请求只读, 未构建/未部署/未启动游戏).
- `[HarmonyTargetMethod]` 在真实运行时是否命中 `MegaCrit.Sts2.Core.GameActions.PlayCardAction.ExecuteAction/CancelAction` 未验证.
- 未验证 `TargetMethod()` 返回 null 时 Harmony 的注册行为 (注释声称 "harmless", 无实测).
- 未验证 `WhisperingEarring` 等其它 `SpendResources` 调用点 (`.tmp/dllsrc/.../WhisperingEarring.cs:79`) 与 VoidForm 事务的交互.

## 最终结论

SUPERVISION_PASS

- 审查目标 1-5 均有源码/探针静态结构证据支持, 未发现 P0/P1/P2 产品缺陷.
- 两项 P3 均为探针覆盖度/证据强度问题, 不阻断当前源码层面结论.
- 本 PASS 仅代表**只读静态审查**通过: 不含构建, 不含探针运行, 不含实机验证.
- 后续必需步骤 (本次未做, 按请求不构建不部署): 中央构建, 探针运行, Harmony 注册日志确认 `PlayCardAction` 动态目标命中, 隔离实机验证 cancel-first 与回合边界.

## 审查范围与证据清单

- 产品文件:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs` (271 行, 全文件已读).
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs` (774 行, 全文件已读).
- 探针: `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs` (736 行, 全文件已读), 交叉核对 `TransactionPatchAdapter.cs`, `ContractStubs.cs`, `ProbeSupport.cs`, `VoidScenarios.cs`, `FormEffectsProbe.csproj`.
- 引擎参考 (反编译, 仅作契约证据):
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1807-1820, 1858-2005`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.Entities.Cards/CardPlay.cs:1-74`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.Entities.Cards/CardEnergyCost.cs:94-141`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.GameActions/PlayCardAction.cs:62-128`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.GameActions/GameAction.cs:116-213`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.GameActions/ActionExecutor.cs:123-183`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.Hooks/Hook.cs:53-62, 1156-1170, 1583-1599`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.Combat/CombatState.cs:411-494`.
  - `.tmp/dllsrc/MegaCrit.Sts2.Core.Helpers.Models/CardCostHelper.cs:103-127`.
- 历史报告 (仅上下文, 未采信其 PASS): `final-forms-effects-review-independent-20261002.md`, `void-form-transaction-supervisor-20261002.md`, `void-transaction-identity-reviewer-rework5-20261002.md`.

## 未验证边界 (明确列出)

- 未执行: 构建, 探针运行, 游戏启动, 部署, Steam 写, shared mod_configs 写, C: 写.
- 未验证: Harmony `[HarmonyTargetMethod]` 在真实运行时的目标解析与注册成功.
- 未验证: 真实 `PlayCardAction` 的 cancel-first 交错 (探针用 ProbeNativeActionIdentity 构造, 见 P3-2).
- 未验证: `WhisperingEarring` 等其它 `SpendResources` 调用点与 Void 事务的交互.
- 未验证: 多人/额外回合实机语义; 探针仅静态结构覆盖.
