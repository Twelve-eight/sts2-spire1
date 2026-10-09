你是 实现者, 范围: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormEffectPower.cs; G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\VoidFormPlayTransactionPatch.cs; G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\TransactionScenarios.cs.
用户本轮唯一指定模型 `6.1sol`, 路由 `agentrouter / ovoapi:6.1sol`. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\void-transaction-identity-worker-20261002.md` (产品代码可按范围编辑; 不构建/不运行测试/不部署/不启动游戏, 不写 Steam install,shared mod_configs 或 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 实现范围与验收契约

先读取当前文件与既有报告, 然后只在上述白名单内修复以下静态缺口, 不要发明 engine callback, 不要伪造 CardPlay:

1. `BeforeCardPlayed` 的 allowance 消费必须绑定当前 `OnPlayWrapper` 的同一 SpendToken. 用当前 async 调用上下文或等价的不可歧义 handoff 传递 token; 不要按同卡 bucket 最新项猜 token. 删除没有真实 payment token 的 tokenless allowance 消费分支. 未完成/取消/被移除 token 必须不能消费.
2. wrapper 正常返回但由于 owner death 或 combat ending 跳过 `AfterCardPlayed` 时, 清除该 play 的 pending/reservation/blocked 状态, 但不要把 allowance 标为 consumed. `AfterCardPlayed` 成功后即使 wrapper 收尾随后失败, 也不要回滚已经真实消费的 allowance; 失败清理必须按 token 与精确 CardPlay 身份隔离.
3. 同卡不同 generation 的 failure/cancel/cleanup 不能清理彼此状态; 保留异卡隔离与幂等性. 当前 power 的 blocked state 也必须在 payment 中途入场的反例结束时释放.
4. `BeforeSideTurnStart` 和 `AfterRemoved` 必须回收 power bucket 中所有孤儿 token; owner 不在 participants 时只清 transaction fields, 不提前刷新该 power 的 turn allowance.
5. 为上述关键边界增加窄探针场景, 尤其是: 同卡两个 token 的 wrapper 上下文不串, no-token manual wrapper 不消费, wrapper skip After cleanup, After success 后 tail fault 不回滚. 只写测试代码, 不运行.
6. 保持现有真实引擎顺序: `OnPlayWrapper prefix -> engine Hook.BeforeCardPlayed -> card body -> engine Hook.AfterCardPlayed`; 不将探针顺序放到 wrapper prefix 之前.

要求: 先分析并在报告写出首条证据, 再编辑. 完成一面就落盘. 不做构建,lint,测试或游戏操作. 如果某项无法在白名单内安全实现, 记录为 flag, 不留 stub.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

