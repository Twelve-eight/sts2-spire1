你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api; reasoning xhigh; native Codex; no fallback`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\fault-single-root-r27\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 3 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.
## 实现写集与任务覆盖条款
本节仅替换模板的代码禁止项: 除报告外, 唯一代码可写路径为 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs. 不改任何其它文件. WRITE code only; SKIP build/lint/tests. 主会话集中构建验证. 禁止启动任何其它harness/模型或再委派, 禁止peer/chat工具. 不提交Git. 第一条结论立即写报告, 每完成一个面写一次. 最多8分钟完成这次窄修.

## 最小窄修契约
1. r25监督已确认P1, 已由hub使用真实编译helper隔离复现. 证据文件 G:\omp works\.tmp\forms-independent-20261005\r25-approved-roots-baseline-r27.json. aggregate-spanning-two-actions, wrapper-spanning-two-actions, cross-leaves-no-single-root 三项在期望false时实际true. 监督报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-recovery-r25\supervisor.md.
2. 只改 FaultMatchesApprovedExceptionIdentities 的跨root规则与必要注释. 每个observed异常的全部leaf必须完整包含于某一个单独approved actionRoot的leaf集合. 严禁把多个root的leaf取并集. 同一真实root的两Forms leaf允许; 跨两个合法action root的组合拒绝; 无关sibling, 同文不同引用, 空Aggregate和cycle均拒绝. 保持ReferenceEqualityComparer身份规则和完整路径检查. 不扩大root批准语义.
3. 保持 BindingLossFaultMentionsFormsRestart, FaultMatchesExceptionIdentity, ApplyUnobservedFaultGate的 raw/expected/unexpected 持久化及final gate语义. 不改schema, 不抑制fault, 不删除实际双action. 不改RuntimeSafetySmokeRunner.cs, BindingLossSmokeRunner.cs或任何生产代码.
4. 原冻结源码SHA256 51E79681D39A1A188F02545679A5E6D286BA7EAA9DD676C0C7A8DA3352DB5B99. 本次报告列出新hash与行号, 说明最小差异, 对不能实现的部分flag, 不写stub. 最终CODE_COMPLETE并列出全部改动路径.