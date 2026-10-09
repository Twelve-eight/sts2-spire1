监督审查报告已给出 REWORK, 请在原工作树完成第二轮修复并追加最终报告. 报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-reviewer-20261002.md`.

必须闭合以下四项:
1. PowerAtStart 为空且 OnPlayWrapper 从未启动时, payment 成功 token 不能留在 `Tokens[card]`. 建立可证明的 owner 或 action 生命周期归属, 使 side-turn, AfterRemoved, action cancel 或 action completion 能按 card/token 精确回收. 不能只依靠 `BlockCard` 后的 `AdoptBlockedToken`.
2. OnPlayWrapper 与 payment 必须显式一对一 handoff. 不得再用 `Tokens[card]` 中唯一 completed token 猜测当前 wrapper. 推荐研究本地反编译的 `MegaCrit.Sts2.Core.GameActions.PlayCardAction` 并在手动原生路径建立 per-action state: `ExecuteAction` 中的 `SpendResources` token 传给后续 wrapper, `CancelAction` 和 action completion 可取消未 claim token. 没有显式 action handoff 的 wrapper 必须 fail-closed, 不得消费 allowance. AutoPlay 继续不消费.
3. payment 已成功但 action 未到达 wrapper 的路径必须有专属精确清理, 不能等待下一代同卡 wrapper 猜测回收.
4. 探针必须新增并真正构造两个反例: payment 完成后 wrapper 永不启动再做 side/removal cleanup; 只有一个 stale completed token 时执行无 payment manual wrapper, 断言不消费 allowance且后续同卡新 generation 不被旧 token 阻塞. 如果添加 action bridge, 同时覆盖 action cancel 或 completion cleanup.

保留现有 consumedCard, consumedToken, rollbackConsumed 代际保护, 保留 tokenless allowance 禁止, 保持生产顺序 `OnPlayWrapper prefix -> BeforeCardPlayed -> card body -> AfterCardPlayed`. 只修改以下白名单: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs`, `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs`, `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs`, 以及本报告. 不构建, 不测试, 不部署, 不启动游戏. 每完成一个检查面立即追加报告, 最终明确已确认, 进行中, 未知.