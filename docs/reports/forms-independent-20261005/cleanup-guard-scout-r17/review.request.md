# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 只读审查员, 范围: G:\omp works\Sts\sts2-forms 与 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\cleanup-guard-scout-r17\review.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 精确范围
只读, 不构建/lint/测试/部署/游戏/git; 禁止再委派/其它harness/模型回退. reasoning xhigh.
先读 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\stable-guard-api-scout-r15\review.md . 不重复ShouldPlay签名研究.
只找三个未覆盖的安全边界, 5分钟内收敛增量报告:
1. 真实引擎战斗结束/房间拆除/quit时PowerCmd.Remove姿态marker的调用与可靠的IsEnding或等价flag, 如果守卫只拦live已选Forms局应如何精准辨别, 不把关闭窗口的teardown当恢复原生规则.
2. 持续model hooks识别本局modifiers的来源, 分辨run state存在但CombatState空/已终结与真实战中, 不使用全局static规则开关, 无Forms普通局不误伤. 给出实际属性与源码行号.
3. 独立process-lifetime safety owner的安装证明与Shutdown/ProcessExit最小策略是否会持有Watcher动态引用; 只引用引擎PowerCmd.Remove签名的风险, Harmony扫描重复注册隔离方法. 如果旧Lifecycle只验证两个原owner无残留, 哪个新观测必须补充以免把故意驻留guard称为未清理.
未知保留, 不推测UI表现, 最多3项; 首结论即落盘, 不写产品代码.