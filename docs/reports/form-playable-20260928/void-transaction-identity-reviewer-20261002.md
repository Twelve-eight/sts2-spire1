# Void transaction identity reviewer report - 2026-10-02

## 已确认

### 审查门禁与范围

- 已先等待实现者 `01a0fcca-d6b4-7a00-bb12-830b940c18e5` 完成初轮实现、最终报告和工作树变更,随后才开始本监督审查。
- 已停止对旧状态继续审核,并在 Nietzsche 完成本轮 rework 后从头读取当前源码、实现报告和窄探针。当前实现报告为 `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-20261002.md`。
- 本审查范围仅为:
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs`
  - `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs`
  - `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs`
- 本监督只写本报告,未修改产品代码,未构建,未运行测试或探针,未部署,未启动游戏,未写 Steam install、shared `mod_configs` 或 C:。

### 已静态闭合的局部检查面

- **已启动 wrapper 的 late-power block token 回收:** `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:57-85` 为 `PowerAtStart` 非空 token 建立 power bucket; `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:144-161` 在 `BlockCard` 真正接管非空 token 时调用 `AdoptBlockedToken`; `VoidFormPlayTransactionPatch.cs:275-296` 记录 `BlockedPower`; `:413-444` 在 `Remove` 中同时移除 `PowerAtStart` 和 `BlockedPower` bucket。由此,付款开始时没有 Void、之后进入 Void、wrapper 已启动并被当前 power block 的路径,当前已有可枚举回收归属。
- **power 生命周期对已登记 token 的回收:** `VoidFormEffectPower.cs:264-300` 先调用 `CancelForPower`,再清除 pending、reservation、blocked 字段;`VoidFormPlayTransactionPatch.cs:298-315` 对 power bucket 做快照并逐 token `Cancel`。`AfterRemoved` 和 owner 参与的 `BeforeSideTurnStart` 都能覆盖已登记的 token。owner 不在 `participants` 时不刷新 `consumed` allowance,保持 owner-turn 语义。
- **tokenless allowance 分支已关闭:** `VoidFormEffectPower.cs:213-240` 只有 `ClaimForBeforeCardPlayed` 返回非空真实 token 时才写入 `pendingFreePlay`/`pendingToken`;认领要求 active wrapper、手动出牌、首系列、`SpendCompleted`、`Reserved`、`PowerAtStart` 精确匹配和 reservation 精确匹配。没有 token 的 wrapper 不会直接建立 pending。
- **exact `CardPlay` 成功身份:** `VoidFormEffectPower.cs:243-259` 只在 `ReferenceEquals(data.pendingFreePlay, cardPlay)` 成立时清理 pending、释放 reservation、清 blocked 并写入 `consumedCard`、`consumedToken`、`consumed=true`。当前没有按卡名、卡索引或新建 `CardPlay` 对象伪造成功消费。
- **wrapper 成功但缺失 AfterCardPlayed 的尾部清理:** `VoidFormPlayTransactionPatch.cs:361-377` 的 `CompletePlay` 无条件按同一 `card + token` 调用 `ClearFailedPlay`,随后 `Remove(token)`。因此引擎在 `G:\omp works\Sts\sts2-spire1\research\_decomp\CardModel_full.cs:1888-1891,1935-1938,1963-1970` 因 owner 死亡或 combat 结束而提前返回、没有调用 `AfterCardPlayed` 时,未消费 pending 不会永久占住 reservation。
- **AfterCardPlayed 成功后的 wrapper tail fault 不回滚:** `VoidFormEffectPower.cs:243-259` 先完成真实消费;`VoidFormPlayTransactionPatch.cs:202-217,379-395` 的 observer 异常路径使用 `rollbackConsumed:false`,所以 callback 已成功后 wrapper 收尾异常不会重新发放 allowance。窄探针 `TransactionScenarios.cs:249-284` 已按该控制流设计检查点,但尚未运行。
- **单 token 的 payment/play 异常入口:** `VoidFormPlayTransactionPatch.cs:120-134` 只在真实 payment Task await 成功后标记 `SpendCompleted`;异常或取消进入 `Cancel`;同步 Harmony finalizer `:469-478` 也进入 `Cancel`。wrapper Task 异常进入 `AbortForPatch` 并在 `finally` 关闭 `CurrentPlay`;同步 finalizer `:505-515` 覆盖相同入口。对已经正确拥有的单 token,`Cancel`、`Remove` 和字段清理具备重复调用容忍性。
- **窄探针的已覆盖顺序:** `TransactionScenarios.cs:148-180` 覆盖 payment-before-power、power-before-payment-completion、wrapper start、owner-absent side start、wrapper release、next same-card generation,验证了 `BlockCard -> AdoptBlockedToken` 的 started-wrapper 回收形状。`TransactionScenarios.cs:396-418` 的成功 helper 保持生产 adapter 中 `OnPlayWrapper -> BeforeCardPlayed -> card body -> AfterCardPlayed` 的顺序。

### 当前仍然存在的 REWORK 阻断项

- **REWORK 1: `PowerAtStart == null` 且 wrapper 从未启动时仍有 orphan token。**
  - 证据: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs:57-85` 在 payment 开始时没有 Void power 时只登记 `Tokens[card]`,不登记 `PowerTokens`;新增归属只在 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs:144-161` 的 `BlockCard` 真正接管 token 时经 `AdoptBlockedToken` 发生。
  - 触发条件: payment 开始时没有 Void;payment 成功后 Void 进入;随后 action 被取消、卡离手、owner/combat 边界提前结束,或 `OnPlayWrapper` 根本没有启动。此时既没有 `PowerAtStart`,也没有 `BlockedPower`;`BeforeSideTurnStart`/`AfterRemoved` 通过 power bucket 无法枚举它。
  - 当前后果: token 留在 `Tokens[card]`,可能被同卡后续 wrapper 当成旧 generation 候选,也可能使后续同卡多代进入歧义。该漏洞正是本轮 rework 后重新复核仍未闭合的真实问题。
  - 最小修复范围: 为此类存活 payment token 建立可由后续 Void power 生命周期枚举的精确 owner/card 归属,或在已证实的 payment/action 取消边界提供等价的 token 精确回收;仅增加 `AdoptBlockedToken` 不能覆盖 wrapper 从未启动的分支。

- **REWORK 2: `OnPlayWrapper` 与 payment 仍不是严格的一对一显式 handoff。**
  - 证据: `VoidFormPlayTransactionPatch.cs:317-357` 仍以 `Tokens[card]` 为入口,选择唯一 `SpendCompleted` 且未启动 token;`CurrentPlay` 只在该 token 被猜中后由 `BeginPlay` `:155-168` 建立。代码没有从 payment invocation 或 action/play generation 将 token 显式传给 wrapper。
  - 触发条件: 同卡存在一个成功但尚未进入 wrapper 的旧 token,随后出现一个没有对应 `SpendResources` 的手动 wrapper。若旧 token 的 `PowerAtStart` 是当前 power 且 reservation 仍在,`ClaimForPlay` 会把这个唯一候选标记 `OnPlayStarted`;随后 `ClaimForBeforeCardPlayed` `:220-247` 的精确检查全部满足,无对应 payment 的 `CardPlay` 仍可消费 allowance。
  - 当前代码对两个已完成候选会 fail-closed,但对 stale token 是唯一候选时仍会猜测。`AsyncLocal<CurrentPlay>` 只能保存错误认领后的状态,不能修复入口绑定。
  - 最小修复范围: 建立不可歧义的 payment-to-wrapper/action-generation handoff;在没有唯一显式 handoff 时必须让 wrapper fail-closed,不能以同卡 bucket 的唯一候选代替当前 payment。

- **REWORK 3: payment 已成功但 action 没有到达 wrapper 的取消边界没有专属清理。**
  - 原生手动路径 `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:169902-169920` 先 await `SpendResources`,再调用 `OnPlayWrapper`;`PlayCardAction.CancelAction` `:169927-169945` 只移除排队卡和手牌取消状态,没有向当前事务 patch 传递 `SpendToken`。
  - 若 `PowerAtStart != null`,token 至少会留在 power bucket,但 reservation 可持续占住 power 直到 `BeforeSideTurnStart` 或 `AfterRemoved`;若 `PowerAtStart == null`,则升级为 REWORK 1 的 orphan。两者都没有在 payment/action cancellation 边界立即闭合。
  - 自动路径 `G:\omp works\Sts\sts2-spire1\research\_decomp\game\sts2.decompiled.cs:193070-193078` 也可能在前置逻辑后直接进入 `OnPlayWrapper(isAutoPlay:true)`,当前 token 仍只能在 wrapper 开始时认领或取消。
  - 最小修复范围: 建立 action-generation 的取消 handoff,或把成功但未 claim 的 token 归属到可在 action 生命周期结束时精确取消的 owner;不能等待下一代同卡 wrapper 猜测回收。

- **REWORK 4: 新增窄探针尚未覆盖上述两个关键反例。**
  - `TransactionScenarios.cs:148-180` 在 side start 前已经启动 wrapper,所以只覆盖 `BlockCard -> AdoptBlockedToken`。
  - `TransactionScenarios.cs:182-193` 的 no-token 场景要求 `Tokens[card]` 为空;不能覆盖 stale completed token 已存在时的 no-token wrapper。
  - `TransactionScenarios.cs:195-218` 用两个 completed token 验证歧义时拒绝猜测,但没有构造“一个旧 token 已完成且未启动,随后一个没有 payment 的 wrapper”这一唯一候选误认领场景。
  - 最小补充范围: 增加 payment 完成后不启动 wrapper、再执行 side/removal cleanup 的场景;增加 stale-only token 加 no-token manual wrapper 的场景;两者都要断言 token 不得消费新 allowance,且后续同卡 generation 不得被旧 token 阻塞。

### 最终判定

**REWORK**。以上 REWORK 1 至 REWORK 4 未闭合前,不能给 PASS,不能把当前静态局部通过扩展为总体通过。

## 进行中

- 当前监督审查已完成;没有继续对旧状态审查,也没有修改产品代码。
- 下一轮只应在 Nietzsche 完成上述 token owner/action handoff rework 并最终汇报后,重新读取当前三份源码和新增窄探针,从头复核本报告列出的两个主漏洞及其失败路径。

## 未知

- 未构建,因此 C# 编译、Harmony patch 绑定、实际 DLL 行为均未验证。
- 未运行探针,新增场景只有源码和静态控制流证据,没有运行结果。
- 未进行真实游戏的 payment/action cancel、owner death、combat ending、多人 side turn、power removal、同卡交错 wrapper 或 auto-play 失败验证。
- 当前文件没有显式主线程 dispatcher 或线程断言;`ObserveSpendCompletionAsync`、`ObservePlayCompletionAsync` 与 power 生命周期 hook 的实际调度交错仍未由本审查动态证明。局部 bucket `lock` 不能替代该运行时证据。
- 未知边界不改变当前判定: 即使运行时通常把标准 `PlayCardAction` 串行化,源码仍没有证明 payment token 与 wrapper 的显式一对一身份,也没有覆盖 payment 成功后 wrapper 永不启动的清理入口。

### 本轮禁止事项记录

- 未构建。
- 未运行测试或探针。
- 未部署。
- 未启动游戏。
- 未修改产品代码。
