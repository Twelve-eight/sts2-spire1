# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 监督审查员, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\supervisor.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 门禁
worker请求 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\worker.request.md . 精确id稍后由hub告知. 当前只落盘等待, hub真实wait该worker completed和gate-notice前不审代码. 没有wait工具直接初次回复等待, hub send_input后审. reasoning xhigh, 不再委派/其它harness/模型/fallback, 不构建/测试/运行/git.

## 监督维度
门禁后按请求检查4文件hash/精确diff. typed fault对象与action执行故障identity严格关联而非字符串吞异常; Event先于task完成的关联正确; expected故障有raw证据且已实际执行/settled/明确Forms重启拒绝; 其它fault/timeout/pending/cancelled仍失败. 两卡无真实副作用判据及原始marker门禁不松动. Lifecycle engine目标owner/prefix数量1且重复Initialize/Terminal/Shutdown前后稳定, 两旧owner0, 不能direct-call冒充真实安装. main thread/异步排空/内存限界, 普通三姿态与turns fault门禁不降低. 每完成一面立刻写报告, 最终SUPERVISION_PASS或NEEDS_REWORK, 不声称实机通过.