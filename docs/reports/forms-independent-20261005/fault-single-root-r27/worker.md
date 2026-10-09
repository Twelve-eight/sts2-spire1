# r27 单 root 归属窄修报告

## 已确认
- P1 复现证据(隔离, 已真实编译): `G:\omp works\.tmp\forms-independent-20261005\r25-approved-roots-baseline-r27.json` 记录 `aggregate-spanning-two-actions`、`wrapper-spanning-two-actions`、`cross-leaves-no-single-root` 三项 Expected=false 而 Actual=true。
- 原冻结哈希: `51E79681D39A1A188F02545679A5E6D286BA7EAA9DD676C0C7A8DA3352DB5B99`, 与任务给定值一致。证据命令: `Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs' -Algorithm SHA256`。
- 监督报告确认缺陷位于 approvedRoots 并集匹配: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-recovery-r25\supervisor.md`。
- 窄修完成, 只改 `FaultMatchesApprovedExceptionIdentities` 与其必要注释。新 SHA256 = `5A00ACB99846CDD8DFEF8D690B4F200C5367A3C028ADD6C98CED3E55C5BAF0EA`; 文件长度 164478 -> 164732; 目标函数 44 行 -> 51 行; 字节增量 254。
- 新实现位于 `FormNativeSmokeRunner.cs:1319-1374`: observed 的全部 leaf 先收集 (`1331-1337`); 每个 approved actionRoot 各自收集 leaf 并构造独立 `HashSet` (`1339-1349`); 最终要求 observed 全部 leaf 完整包含于某一个单独集合 (`1351`)。不再跨 root 取并集, 也不按 partial identity 批准。
- 规则边界保持: `ReferenceEqualityComparer` 身份规则保留 (`1333,1343,1348`); 空 approvedRoots、空 observed Aggregate、空 approved root leaf、环均 fail-closed (`1332-1337,1343-1346`); 纯 wrapper 展开、同 root 多 leaf、同引用重复 leaf 仍允许 (`1353-1373`)。
- 最小差异证明: 以同哈希副本 `G:\omp works\.tmp\forms-independent-20261005\model-route-quarantine-1137\FormNativeSmokeRunner.cs` 为基线, 逐段比对结果: gate 函数体 identical=True; 目标函数之前 identical=True; 目标函数之后 identical=True; 唯一差异即目标函数与注释块。
- 未触碰项: `BindingLossFaultMentionsFormsRestart` 定义于 `BindingLossSmokeRunner.cs:1232`, 本文件仅在其 1288 行作为谓词引用; `ApplyUnobservedFaultGate` 1284-1317 的 raw/expected/unexpected 持久化与 final gate 语义逐字节未变; `FaultMatchesExceptionIdentity` 1325-1326 仍委托同一单 root 判定。
- 未改动生产代码、schema、其它 smoke runner、构建与部署; 未提交 Git。

## 进行中
- 无。

## 未知
- 实机/游戏运行证据不在本次允许范围, 未覆盖。
- 本窄修按任务要求跳过 build/lint/test; 新哈希下的真实编译与 14 场景复跑由主会话集中验证。
- 未在源码内新增 14 场景负例断言, 归属主会话隔离 helper 复验范围。