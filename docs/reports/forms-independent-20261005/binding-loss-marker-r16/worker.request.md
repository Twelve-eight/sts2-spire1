# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-marker-r16\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 实现写集与同批监督门禁
上面的报告唯一可写路径指唯一报告; 本节额外授权只改以下两文件:
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md
不得改其它生产源码. 不构建/lint/测试/部署/游戏/git/新子代理; 不运行 codex/omp/其它模型或fallback. 当前模型reasoning xhigh.
你的同批监督由hub原生等待你completed后才审查最终字节. 完成时报告CODE_COMPLETE, 文件hash/行号/未验证边界.

## 精确任务
先读 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-hotfix-r14\supervisor.md 和当前两文件.
仅返修R14-01和R14-02. 桥失效后不能调用CurrentKind推断native stance; unknown不能与Wrath比较后当成变化. 用原始owner Power实例FullName/原生marker键集合判定姿态, 不依赖Watcher桥或持有其MethodInfo/Assembly/delegate. 先核对ownerPowerAmounts真实键格式和当前Watcher源码中三个marker的FullName. 如无法获取可靠原始证据, 显式判不可确定且不让safetyPassed为true; 不猜名字,不stub. before/after都用同一原始marker证据, 原失效前桥CurrentKind只作诊断, 未知必须独立呈现.
必须保留真实动作/Crescendo fixture, 失效前一次能量至少2, 失效后无补能; explicitRejection的Forms失效+重启双条件; 真实动作完成/fault/排空; regressionObserved与safetyPassed语义, 不给timeout/cancelled/pending伪通过.
README同步为失效前补到至少2然后桥失效再probe两张牌; 不再写失效后两probe各补3. 如引入诊断字段要同步文档.
最终只做窄修, 不调整生产r5 DLL或其它场景.
模板中U+3001标点在派发前被文本门禁拒绝; 请求的自行撰写标点已改为ASCII, 未改源码/证据/路径/hash.
