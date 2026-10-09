# phase4 窄修增量报告

范围: 仅 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs / G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs 与本唯一报告。
请求: G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-faults-r19\phase4-worker.request.md
请求指定 global:deepseek-v4.1-flash / wb2api / xhigh; 当前工具未提供实际模型/provider route 元数据，不冒充实测路由；未启动任何模型/回退/其它 harness 或再委派。
边界: 不构建/lint/测试/游戏/部署/git/peer，不改生产/Lifecycle/README/控制器，不写 C:/Steam/共享配置。

## 已确认

### P4-01 [P1] mixed AggregateException 身份匹配存在部分命中漏洞
- 文件: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1323-1335。当前 observed Aggregate 使用 Any，ReferenceEquals(root,root) 先于逐分支审核。
- 证据: G:\omp works\.tmp\forms-independent-20261005\r19-phase3-mixed-fault-repro.json。中央真实编译 helper 隔离调用的 mixed-approved-and-unrelated 预期 false、实际 true，MixedFaultBugConfirmed=true。本 writer 仅读取既有中央证据，未运行复现。
- 最小修复: approved action root 的所有叶子必须明确 Forms unavailable+restart；observed 的所有叶子必须按 reference identity 覆盖，空 aggregate 不可批准。同文本不同对象保持拒绝；保留 raw event 和 sequence。
- 尚缺实机: 修订后的中央隔离检查与 terminal/shutdown、cleanup/quit late-fault 证据。

### P4-02 [P2] Tranquility actualCardPlay 错读 Strike history
- 文件: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs:430-431。当前 probe 调用固定 watcherStrikeHistory reader；来源 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs:1943-1971。
- 最小修复: 在真实 probe card 创建后保留其对象并读取 CardPlaysFinished 中对应实例的 before/after 与 replay 证据，不改既有 Strike history 语义。
- 证据类型: 源码数据流与同目录 supervisor.md 已有结论；未运行游戏。

## 进行中

- 先落盘异常树全分支窄修，再落盘真实 Tranquility 实例 history。

## 未知

- 新字节的中央编译/隔离复现/实机/同批监督尚未验证。
## 已确认

### P4-03 [P1] 异常树窄修已落盘
- FormNativeSmokeRunner.cs 的 identity helper 改为逐 leaf ReferenceEqualityComparer 对象闭包，不再 Any 命中一项批准整树，也不直接 ReferenceEquals(root,root) 跳过分支。observed 所有 leaf 必须属于真实已批准 roots；多 action 已批准叶子的纯组合可分类，但任何外部 sibling 仍拒绝。空 Aggregate/cyclic branch fail closed。
- BindingLossFaultMentionsFormsRestart 改为每个 leaf 的 Message 独立证明 Forms unavailable+restart，不以 Aggregate.ToString() 混入 sibling 文本。根批准仍要求原 executed/settled+实际 sequence identity proof；final gate 再核每个候选 root 的所有 leaf。
- 原始 raw event/text/sequence 字段完整保留，没有同文本对象匹配或清除 unexpected。新增 helper 均为既有 partial 类私有实现，不新增公开/跨文件 API。
- 证据类型：两文件已落盘源码；本 writer 未构建、未调用被修 helper、未运行隔离测试或游戏。

## 进行中
- 修复真实 Tranquility card 实例前后 history 与 replay 区分证据。

## 未知
- 新字节中央隔离验收与实机结果仍待 hub。
