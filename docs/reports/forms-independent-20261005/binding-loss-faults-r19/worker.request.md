# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

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
## 代码写集与同批监督
报告如上, 另外只准改以下测试文件:
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\LifecycleSmokeRunner.cs
G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md
不改生产/控制器/其它文件; 不构建/lint/测试/部署/游戏/git, 不再委派/其它harness/模型/fallback. reasoning xhigh. 本批同批监督需hub实际wait completed门禁后再审. CODE_COMPLETE列最终文件hash和未验收边界. 先落盘再继续, 尽量10分钟内收敛.

## 已确认缺口
读 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-marker-r16\supervisor.md . 原始marker窄修已通过, 不再改回失效桥CurrentKind判姿态.
本机引擎 G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Helpers\TaskHelper.cs:21-42 与 MegaCrit.Sts2.Core.GameActions\GameAction.cs:125 表明实际action故障即使被等CompletionTask也会发UnobservedFault; 现在预期Forms重启拒绝将被全局fault gate强制失败. 不允许为了变绿按字符串吞掉所有Forms异常.

## 必须实现
1. 保留全部异步fault原始证据. binding-loss预期异常只有同时满足真实下一张牌action已实际执行且已完成/故障Task已观察并排空, Exception对象与该action实际故障root/wrapper(可展开AggregateException但必须reference identity)关联, 明确Forms unavailable+restart, 才可从unexpected unobservedFaults分出expectedRejections. TaskHelper event在actionTask变faulted之前发布, 所以允许延迟关联, 不在event到达时盲目删除. 保留索引/对象级关联, 不靠文本相同认作同一异常. timeout/pending/cancelled无故障/其它事件仍失败. 原始fault与expectedRejections持久化在scenario/final JSON, unexpected的unobservedFaults仍必须empty才通过. 普通三姿态/turns探针保持严格原语义, 不降低所有场景fault门禁.
2. 安全门仍须两张实际牌都显式拒绝, 无支付/伤害/HP/block/能量/marker变化, 原始marker证据明确, 失败不能被expected异常标签变安全. 失效前能量>=2, 失效后不补, Crescendo fixture不变. final退出/日志/quitDrain门禁保持.
3. Lifecycle新增真实Harmony元数据观测: 稳定引擎PowerCmd.Remove(PowerModel?)目标, owner固定Forms.FormStanceSafety, 精确1个prefix; Initialize重复前后prefix身份/数量不变, Terminal及MainFile.Shutdown两次后该guard仍存活且数量1, 两原owner仍清0. 不新增生产guard类型硬引用, 用Harmony owner/真实target metadata, 不直接调用prefix冒充安装. 测试只覆盖新生产guard字节, r5旧lifecycle已有证据不用重新通过此新增要求.
4. README更新typed fault关联和故意进程驻留guard区别, 不声称热卸载/完整读档/可见UI/长战斗已通过.
无法准确实现则报告阻塞, 不stub/猜API. 生产r18实现同批在另外4个生产文件, 与你完全不共享写集. 不等待其代码, 稳定owner和PowerCmd.Remove签名已定. 实机复测由hub中央做.