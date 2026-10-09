# 监督审查报告 - fault-single-root-r27

GATE_RECEIVED (native wait_agent completed, CODE_COMPLETE, worker 01a10a4c-2e1b-7723-bead-c370c0899e83, TimedOut=false)

- 状态: 冻结审计进行中 (bounded, 无构建/测试)
- 唯一审核源(冻结): G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs
- 基线隔离反例: G:\omp works\.tmp\forms-independent-20261005\r25-approved-roots-baseline-r27.json
- 最小窄修契约: 同目录 worker.request.md
- 模型/路由: global:deepseek-v4.1-flash; wb2api; reasoning xhigh; native Codex; no fallback

## 已确认

- [门禁-1] 门禁真实: gate-notice.json 记录 NativeTool=multi_agent_v1.wait_agent, Worker=01a10a4c-2e1b-7723-bead-c370c0899e83, Status=completed, TimedOut=false, WorkerResult=CODE_COMPLETE, Approval="实现完成门禁, 尚待静态监督/中央验证"。
- [门禁-2] 冻结 hash 匹配: 审核源 SHA256 实算 = 5A00ACB99846CDD8DFEF8D690B4F200C5367A3C028ADD6C98CED3E55C5BAF0EA, 与 gate-notice.json 声明的 SHA256 完全一致 (MATCH=True)。文件 3845 行, LastWriteTimeUtc 2026-10-05 04:23:07Z。
  - 复现命令: `(Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs' -Algorithm SHA256).Hash`
- [边界-1] 改动边界收敛到单一函数: 以契约声明的原冻结 hash 51E79681D39A1A188F02545679A5E6D286BA7EAA9DD676C0C7A8DA3352DB5B99 对应文件 G:\omp works\.tmp\forms-independent-20261005\model-route-quarantine-1137\FormNativeSmokeRunner.cs 为基准 diff, 全文件仅 2 个 hunk, 均位于 FaultMatchesApprovedExceptionIdentities 及紧邻 XML 注释内: @@ -1317,7 +1317,8 @@ (注释 1 改 2) 与 @@ -1327,20 +1328,27 @@ (函数体)。numstat 18 insertions / 10 deletions。其余生产代码零改动。
  - 复现命令: `git diff --no-index -- 'G:\omp works\.tmp\forms-independent-20261005\model-route-quarantine-1137\FormNativeSmokeRunner.cs' 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs'`
- [规则-1] 单 root 完整 leaf 归属已实现: 行 1339-1351 先对每个 actionRoot 单独 CollectLeaves 生成独立 approvedLeafSets 元素 (行 1348), 再以 `approvedLeafSets.Any(approvedLeaves => observedLeaves.All(approvedLeaves.Contains))` (行 1351) 判定; 是"存在单个 root 覆盖全部 observed leaf", 不再把多 root leaf 取并集 (旧码为单一 approvedLeaves HashSet 跨 root Add, 已删除)。
- [规则-2] 跨 root 并集拒绝成立: 因并集结构已移除, aggregate-spanning-two-actions / wrapper-spanning-two-actions / cross-leaves-no-single-root 三项预期 false 在控制流上被满足 (任一 root 均无法覆盖全部 observed leaf)。此判定为静态推理, 与 r25 基线 Actual=true 的缺陷方向相反。
- [规则-3] 身份规则保持: observed 与每个 root 的 CollectLeaves 均以 `new HashSet<Exception>(ReferenceEqualityComparer.Instance)` 作 path 集合 (行 1333/1343), approved 集合亦以 ReferenceEqualityComparer.Instance 构造 (行 1348); 无文本比较, 未引入 Equals/ToString 匹配。same-text-distinct-reference 仍因引用不同被拒。
- [规则-4] empty/cycle 拒绝保持: 行 1332-1337 对 actionRoots.Count==0 与 observed 收集失败/0 leaf 返回 false; 行 1343-1347 对任一 root 收集失败/0 leaf 返回 false。CollectLeaves 的 path.Add 失败即 cycle 返回 false, Aggregate 空 (Count>0 前置, 行 1361) 返回 false。与契约"空 Aggregate 与 cycle 均拒绝"一致。
- [契约-2] 无关 sibling 与同文不同引用拒绝: CollectLeaves 只沿 InnerException/Aggregate.InnerExceptions 递归 (行 1359-1365), 不吸收无关 sibling; 身份集合为引用比较, 文本不参与。same-action aggregate/wrapper 与 repeated-same-reference 允许路径未被破坏。
- [契约-3] 其余语义未动: ApplyUnobservedFaultGate (行 1277-1317) 与 BindingLossFaultMentionsFormsRestart 过滤器 (行 1287-1288) 逐字节未改 (diff 无 hunk); FaultMatchesExceptionIdentity (行 1325-1326) 未改, 仍委托单 root 重载。raw/expected/unexpected 持久化 (行 1295-1298) 与 final gate (行 1299-1316) 未改。未改 schema, 未抑制 fault, 未删除实际双 action。
- [边界-2] 其他文件未改: diff 仅命中 FormNativeSmokeRunner.cs; BindingLossSmokeRunner.cs / RuntimeSafetySmokeRunner.cs 等生产与测试文件不在本次写集内 (工作树中它们的改动早于本次窄修, mtime 11:50/12:05 < 本次 12:23:07)。

## 进行中

- 核对 r25-approved-roots-baseline-r27.json 14 项期望与实际语义逐项静态映射 (已完成推理, 待汇总)。

## 未知

- 实机证据: 本轮为纯静态审查, 不宣称任何实机验证; 修复后行为需中央真实编译/实机复现确认。
- 未运行构建/测试/隔离反例 (契约禁止), 因此"修复后 Actual 全 Match"未经本轮复现。
- 未核对 git 提交状态与远端同步 (本轮无 Git 写操作, 亦不属审查范围)。

## 结论

SUPERVISION_PASS (仅静态完整通过, 无 P1; 不宣称实机)。