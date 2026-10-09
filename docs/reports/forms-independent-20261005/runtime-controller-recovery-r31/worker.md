# runtime-controller-recovery-r31 worker

## 已确认

- [P0] 基线哈希与冻结源哈希已独立复算，和 request 一致。
  - `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r24.ps1` SHA256 `E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1`
  - `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r28.ps1` SHA256 `E476A110A08D127E0D467CBE056818D30A273ED41ABFF6118A6DAE42339E9EF6`
  - `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs` SHA256 `48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C`
  - 复现命令: `Get-FileHash -LiteralPath <path> -Algorithm SHA256`
  - 尚缺实机证据: 无, 这是文件字节哈希.

- [P1] r28 `Test-RuntimeFaults` 存在未关联 fault 通过窗口; r31 已修。
  - r28 路径: `run-isolated-smoke-r28.ps1:402-432`
  - 权威契约: `RuntimeSafetySmokeRunner.cs:1277-1317` 将 faults 分为 expected/unexpected, 任何 unexpected 失败; `RUNTIME-SAFETY.md` 要求 expected 仅来自实际 settled command 的 Forms-restart root.
  - 触发条件: raw 与 expected 数量相等但 sequence 集合不同, 或 expected 重复引用同一 raw.
  - r31 修复: `run-isolated-smoke-r31.ps1:402-436`; expected sequence 去重 + 反向要求 raw sequence 全在 expected 集合.
  - 尚缺实机证据: 无, 这是解析器静态缺陷.

- [P1] r28 未选局 action 仅依赖顶层 `passed`; r31 已改为 typed 证据。
  - r28 路径: `run-isolated-smoke-r28.ps1:483-506`
  - 权威 schema: `RuntimeSafetySmokeRunner.cs:1361-1381` 输出 `action`; `FormNativeSmokeRunner.cs:1018-1031,1153-1177` 定义 `state/status/completionTaskOutcome/cancelled/exception/completedUtc/cardPileAfter`.
  - r31 修复: `run-isolated-smoke-r31.ps1:486-524`; 校验 Finished/completed/cancelled=false/无 exception/无 failure/cardLeftHand/真实历史增量/空 carrier+effect.
  - 边界: 本批 source 的 `RuntimeSafetyPreparedCardRun.ToJson()` 未输出 `executionObserved/actionExecutedAndSettled`; r31 不检查不存在的字段.
  - 尚缺实机证据: 无, 这是解析器静态缺陷.

- [P1] 中央 observation 1: r28 对 Shutdown 前后 ownerCounts 同时要求 Forms/Watcher=0; 已核对 source 后确认 r28 原语义正确。
  - source 路径: `RuntimeSafetySmokeRunner.cs:272-275,329-345`; selected 场景先 `MainFile.Shutdown()` 再采 `ownerCountsAfterShutdown`, `guardPassed` 同时要求 before/after 两旧 owner=0.
  - 结论: 不能放宽 before; r31 保留严格双端 owner 断言, 无改动.
  - 尚缺实机证据: 无, 这是 source 顺序与断言的静态确认.

- [P1] 中央 observation 2: r28 的 3 command probe 已正确只在 selected 场景出现。
  - source 路径: `RuntimeSafetySmokeRunner.cs:347-385` selected 3 probes; `:479-527` unselected 只 prepare Strike/Tranquility.
  - 结论: r31 无 selected/unselected 错位; safety 分支校验 3 command, unselected 分支校验 2 action.
  - 尚缺实机证据: 无, 这是 source schema 的静态确认.

- [P1] 中央 observation 3: r28 final raw 非空误判已修。
  - source 路径: `RuntimeSafetySmokeRunner.cs:187-209,680` 将 scenario fault 快照保留后, final gate 从 faultsAtStart=0 重算全量 raw/expected.
  - r31 修复: `run-isolated-smoke-r31.ps1:591-597`; scenario 从 `scenarioUnobservedFaults/scenarioUnobservedFaultEvidence/scenarioExpectedRejections/scenarioUnexpectedUnobservedFaults` 读取并严格 typed 关联; final 允许 raw 非空, 且 scenario raw 必须逐 sequence 出现在 final raw.
  - 尚缺实机证据: 无, 这是 source 输出顺序与 parser 断言的静态冲突.

- [P1] 中央 observation 4: `Property` 空数组展开已修。
  - r28 路径: `run-isolated-smoke-r28.ps1:42,583-587`
  - 权威 schema: `RuntimeSafetySmokeRunner.cs:461-465` 写入 `string[]` carriers/effects.
  - r31 修复: `run-isolated-smoke-r31.ps1:606-610`; 使用 Evidence-Property 保形并逐字段 typed array/Count=0 校验.
  - 尚缺实机证据: 无, 这是 PowerShell 数组展开语义的静态确认.

## 进行中

- 无.

## 未知

- 未构建, 未 lint, 未测试, 未运行游戏, 未部署. 本批明确要求跳过.
- `removeMethodIdentity` 的精确字符串格式尚未用运行报告验证; r31 保留非空字符串与前后同一性校验.
- `productionIdentity.location` 在 r31 中收紧为实际隔离 staged `Forms.dll` 路径, 实机加载路径仍未验证.
- 下一批仅把 `Environment.Exit` 限定为 `System.Environment.Exit`; 本批 schema 字段不再变化.

## 最终冻结

- CODE_COMPLETE
- 新脚本: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r31.ps1`
- 最终 SHA256: `25E049A0E228861736F6CEA2569A49926AF71B6A485666CEED69D23574EF14EE`
- 字节数: 62324
- 旧 r24 SHA256 未变: `E3C93A5FDD15C1D59AE97A0D3091CD0D5E4160AAF5BD2F89599A596F7344B2C1`
- 旧 r28 SHA256 未变: `E476A110A08D127E0D467CBE056818D30A273ED41ABFF6118A6DAE42339E9EF6`
- 冻结源 SHA256 未变: `48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C`
- 新增/改动仅 runtime 解析面: `Test-RuntimeFaultFields`(402), `Test-RuntimeFaults`(432), `Test-RuntimeCardRun`(486), `Test-RuntimeSafetyEvidence`(535), scenario/final fault 分流(591-597), safety-only 3 command 分支(598-606), unselected 两 action 分支(607-646).
- 未实现项: 无.
- 未构建/未 lint/未测试/未运行游戏/未部署; 本批明确禁止.