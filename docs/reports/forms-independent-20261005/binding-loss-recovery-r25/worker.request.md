# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-recovery-r25\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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

---

## 主会话收割方法

```powershell
Get-ChildItem -LiteralPath '<报告目录>' -File | Sort-Object LastWriteTime |
  Select-Object Name,Length,LastWriteTime
```

- 子代理超时/中断/额度耗尽时: 先读报告文件, 把 `已确认` 当作可用证据, `进行中` 当作半成品复核, 不得当作无产出.
- 收到第一批落盘结果后即向用户汇报一次, 后续增量补充.
## 固定边界
本轮思考层级xhigh. 只准当前Codex原生设施, 不再委派, 不启动codex exec/omp/其它harness/模型/fallback. 禁peer thread/list/read/wait等工具, 不读凭据配置或改路由. 不构建/lint/测试/部署/游戏/git, 不写C:/Steam/共享配置/canonical/Workshop. 主hub集中验证.
旧会话部分后续阶段实际误用ovoapi:6.1sol/max, 已关闭且这些阶段的实现/监督不作交付. 当前候选仅是参考, 不得继承旧报告PASS或自己用请求文字宣称实际模型. 以当前权威引擎契约独立完整重审受影响差异并完成新DS产物, 然后同批监督/中央重建/正反例/实机重新建立证据. 首条结论立即落盘, 每完成一面落盘, 10分钟内有界; 无法准确实现的标缺口而非stub. 不因已有候选可编译就跳过审修.
## r25任务
只准代码写集:
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md
不改RuntimeSafety/工程/生产/控制器. Git HEAD 642753f是已推送生产与早期fixture基线. 读这四文件对HEAD差异, 当前typed异常/action/final/lifecycle阶段须独立重新确认, 不继承错误模型宣称. 已知待修 P1 mixed aggregate吞无关siblings和P2 Tranquility实际出牌历史错读Strike. 现候选中途被关闭可能仅完成identity一面, 原报告binding-loss-faults-r19/phase4-worker.md自述不是完成门禁.
1. 全异常分支fail closed: 批准action root每个leaf须明确Forms unavailable+restart, 不能一个Forms文本或ReferenceEquals(root,root)吞无关sibling; observed每个branch按真实reference identity覆盖批准叶子, 纯wrapper批准, 同文本不同对象/混合无关/空aggregate拒绝. 批准仍需恰两个真实action的BeforeExecuted实际发生且task settled/fault已观察排空, timeout/pending/取消无故障不算拒绝. raw事件/对象/sequence全保留, ordinary场景不放宽. 若一个observed组合跨两个合法action, 分类与proof唯一归属须保守拒绝而非造归属.
2. 每个实际nextStrike/stanceChangeProbe输出actualCardHistory对象, 同一真实CardModel对象before/after按ReferenceEquals从真实CardPlayStarted/CardPlayFinished历史读取. 不使用固定Strike reader证明Tranquility; 所有重放实例也保留真实匹配累计count. JSON合同固定:
actualCardHistory.card = run.card友好入口(分别WATCHER_STRIKE_P与WatcherTranquility)
actualCardHistory.cardTypeFullName = 实际CardModel.GetType().FullName
actualCardHistory.cardInstanceIdentity = 同一个真实对象的非空日志身份, 不按卡名证明同一对象
actualCardHistory.observedByCardReference = true, 仅真实reference筛选读取成功才置true
actualCardHistory.beforeStartedCount/afterStartedCount/beforeFinishedCount/afterFinishedCount = 非负整数
both denied时四counts前后相等, 任一started或finished增量都算副作用失败. 探针probeActualCardPlay从其自身finished delta读, 缺失历史未知必须失败. Strike既有watcherStrikeHistory字段不改义. actualCardHistory进入BindingLossRunEvidence, 便于控制器集中核验.
3. 保持至少2能量失效前fixture, raw marker determinate/nonconflict, 所有支付双向/HP/block/power/marker不变判据, 两实际牌明确拒绝. 不通过新增TryBind触发Terminal, 只能生产Always pump消费duplicate通知; 不解pause/不改生产保护.
4. lifecycle完整重审: stable PowerCmd.Remove(PowerModel?)的真实Harmony精确owner Forms.FormStanceSafety且一个RemovePrefix, 重复init身份/数量不变, Shutdown两次后仍一prefix, 两旧owner整数0, 同simple name Watcher重复收敛Terminal. 不直接call prefix证明安装, 不引Spire1/Watcher AssemblyRef.
5. final订阅留到post-quit drain, raw/expected/unexpected持久化, approved roots真实身份传到final, 全失败不被覆盖. README中文说明候选审修和故意驻留guard不是可热卸载, 不声明实机已验收.
权威API只查当前 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc 与 .tmp\watchermod. 已有混合fault机械负例 G:\omp works\.tmp\forms-independent-20261005\r19-phase3-mixed-fault-repro.json . 新实际history schema已与同批控制器r24约定, 不读取其它在写的文件, 不互改.
同批监督已由主hub安排, worker本阶段现在可开始. 完成后列实际修改文件/hash, CODE_COMPLETE或PARTIAL并停写等hub门禁. 不自行轮询监督.

