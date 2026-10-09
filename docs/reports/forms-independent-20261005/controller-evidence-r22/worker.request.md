# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-evidence-r22\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 代码写集与任务
只新增 G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r22.ps1, 从已监督并实际启动过的run-isolated-smoke-r20.ps1复制. 原r20与所有生产/测试代码不改. 报告唯一worker.md. 不构建/lint/测试/启动游戏/deploy/git, 不调用子代理或peer状态工具, 不启动omp/codex exec/其它harness, 不更换模型或fallback. reasoning xhigh. 同批监督须hub真实wait完成才审. 10分钟范围, 一面写一次报告, 无法准确完成就flag而非stub.
当前r19测试载体已保留raw UnobservedFault, 并按Exception reference identity与实际settled action关联预期Forms重启拒绝. r20控制器仍要求raw unobservedFaults为空, 因而与新schema冲突. 只做窄解析门禁修复, 不改生产语义或放宽为字符串白名单.
读取当前测试权威schema:
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs
对应gate-notice在binding-loss-faults-r19/phase2-gate-notice.json, 同批静态监督进行中. 解析字段以源码为准, 标出缺失proof而不造字段或让测试强制通过.
1. BindingLoss解析器保留原入场/失效/双卡拒绝/无变化/无回归/安全/最终quit settled门禁. raw fault必须存在数组, 不要求为空; unexpectedUnobservedFaults必须非null空数组, unobservedFaultGatePassed必须bool true, expectedRejections和对应真实action关联证明按新schema逐项核对, 数量/sequence/raw对应不允许丢弃或重复. 缺证据/未执行/未settled/timeout/pending/cancelled仍失败. 不把异常文本包含Forms当独立凭据. 最终证据若没有分类字段, 不能假造, 如实报告需要测试载体补齐.
2. Lifecycle解析器除原不叠补丁/Terminal/两次Shutdown旧owner0外, 还检查safetyGuardBefore/safetyGuardAfterReinit/safetyGuardAfterShutdown的精确owner=Forms.FormStanceSafety, Remove目标与prefix声明身份, prefix恰1且身份相等, survivesShutdown true. 以精确schema为准. 不用无关总patch数替代真实目标证明.
3. 不动其余effects/saveguard/启动隔离/路径保护/无窗口/G:环境/日志排空/共享config只读hash/安全Move语义. 不再用PowerShell自动变量$error或$pid作普通变量.
中央hub将从脚本AST提取纯解析函数跑正反夹具; 你只写代码, 不运行. 最终CODE_COMPLETE, 精确文件SHA256和未知边界. 不声称实机验收.