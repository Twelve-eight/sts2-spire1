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
