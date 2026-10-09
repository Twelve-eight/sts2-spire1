# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\phase4-worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 本阶段精确任务与边界
reasoning xhigh. 只使用用户指定global:deepseek-v4.1-flash/wb2api, 不许fallback. 不使用peer thread/list/read/wait工具, 不再委派, 不运行codex exec/omp/其它代理. 不构建/lint/测试/运行游戏/部署/git, 不写C:/Steam/共享配置/canonical/Workshop. 主会话负责验证. 最多10分钟有界, 每面完成立即落盘.
权威输入: 同目录supervisor.md最后phase3两发现, 中央复现 G:\omp works\.tmp\forms-independent-20261005\r19-phase3-mixed-fault-repro.json . phase3编译6 warnings/0 errors, 已排除未完成r21.
唯一代码写集只限:
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs
不改Lifecycle/README/RuntimeSafety/生产/控制器. 不新增跨文件API或降低原门禁.
1. P1 mixed AggregateException: FormNativeSmokeRunner.cs:1323-1335, 当前Any身份判断会吞无关siblings. observed异常树所有分支均须可批准; 任何无关leaf/branch必须unexpected. approved action root自身也必须逐leaf明确Forms unavailable+restart, 不可一个Forms文本或ReferenceEquals(root,root)批准含无关sibling的root. 纯同一对象leaf与只包装该leaf的真实Aggregate/wrapper可批准; 同文本不同引用仍拒绝; 混合双action外的无关异常拒绝; 空aggregate不能真空通过. 审清BindingLossFaultMentionsFormsRestart是否Any/ToString也会混入, 必要时同两文件窄修. 保持双真实action executed/settled与reference identity, 不以文本替代identity. 无root批准的正常场景仍严格零fault.
2. P2 probeActualCardPlay: BindingLossSmokeRunner.cs:430-431读Strike历史不代表Tranquility实际出牌. 增加或复用当前引擎CardPlayFinished的真实Tranquility实例/模型历史, 输出可复核before/after并将其参与无副作用判据. 与自动重放/其它卡区分; 不按action完成或卡名字符串凭空证明实际打出. Strike历史既有字段不改义. 原始marker, 支付双向, HP/block/power, 两个action及final drain门保持.
既有public JSON字段不改义, 新证据可补充. r23控制器按冻结phase3旧schema监督, 本阶段不读正在变化控制器或互改. 收尾列两源hash与实际修改路径, CODE_COMPLETE或明确缺口, 不声称实机通过.
同批实现者01a109cb-2810-7463-b8c0-eccd6b48ff4f, 同批监督01a109cb-4e3e-7fe1-82dc-41594288b07f. 实现现在可开始, 完成后停写等待hub原生门禁.


