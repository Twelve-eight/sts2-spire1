# r34 同批监督审查

## 已确认

### 0. 门禁与冻结身份
- 本批门禁有效：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dual-guard-fixture-r34\gate-notice.json` 记录 Worker `01a10a7d-236b-7d43-8b94-772aa19d4560`、NativeTool `multi_agent_v1.wait_agent`、Status `completed`、TimedOut `false`、WorkerResult `CODE_COMPLETE`；不继承 r32 门禁。
- 三份冻结 hash 与门禁完全一致：
  - `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs` SHA256 `45C4E599F66AFDB3044D8BBFCBB52B5EDE4615E7990E5EB109BA073AF8FEB946`，98088 bytes。
  - `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md` SHA256 `A72EB63A5FC717AFB83E11B4383310C04BDF0B19D25F3ED8BCFAECB2A61BC62D`，7703 bytes。
  - `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r34.ps1` SHA256 `DC61A5C7F79A190E6132362387597925B2F39F7343CC3CD9F8690FA2243B04A3`，68221 bytes。

### 1. 真实负例与采样顺序 (静态符合)
- 真实负例：`G:\omp works\.tmp\forms-independent-20261005\native-r30-core-r32\r1-runtime-safety\forms-runtime-safety.json` 为 failed/exit1；`ownerCountsBefore` Forms=14 / Bridge=14 / Safety=1，`ownerCountsAfterShutdown` 0/0/1，`guardPassed=false`。支持“Bind 后 before 采样不应硬断言两 old owner 为 0”。
- C# 采样顺序：selected `RuntimeSafetySmokeRunner.cs:276-278`、unselected `:468-470` 均为 `guardBefore -> playGuardBefore -> ownerCountsBefore`；随后 `:282-297` / `:474-489` 在主线程 gate 内真实反射调用 `Forms.FormsCode.MainFile.Initialize()`，再采 `playGuardAfterReinit -> ownerCountsAfterReinit`；Shutdown 后 `:355-357` / `:562-564` 采 `guardAfterShutdown -> playGuardAfterShutdown -> ownerCountsAfterShutdown`。
- 全局计数为真实 `Harmony.GetAllPatchedMethods()` + 每方法 `Harmony.GetPatchInfo`，统计 Prefix/Postfix/Transpiler：`RuntimeSafetySmokeRunner.cs:1330-1357`；异常写 `failure=-1`，不静默变 0。

### 2. 双 target 精确证明与两 runtime 模式 (静态符合)
- Remove 证据 `RuntimeSafetySmokeRunner.cs:1220-1260`；Play 证据 `:1263-1328`：反射取 `PlayCardAction.ExecuteAction` 0 参重载，`GetPatchInfo` 只统计 owner `Forms.FormStanceSafety` 的 prefix，并要求 count=1、patchType=`Forms.FormsCode.FormStanceSafetyGuard`、method=`PlayPrefix`。
- Snapshot `MatchesProof` 位于 `:1492-1528`，typed 读取全部 12 字段；C# 门禁 selected `:362-383`、unselected `:568-589` 同时要求 before 健康、afterReinit 不叠、Shutdown 后 old owner 0、Safety 2，且 `guardPassed` 参与最终 passed。
- 原 Remove 证据与三条真实 command、两条真实 action 路径仍在 `:388-431`、`:594-620`；原故障批准规则未被本 delta 改写。
- parser Play typed 校验在 `run-isolated-smoke-r34.ps1:391-414`，逐字段要求 owner、playMethodFound、playPatchedBySafetyOwner、prefixCount=1、prefixOwner、prefixPatchType、prefixMethodName、playMethodIdentity、targetDeclaringType、targetMethodName、targetReturnType、parameterCount=0，且 `failure` 缺失；身份跨阶段比较在 `:411-414`。
- parser final 失败字段拒绝在 `:582-584`：`cleanupSkipReason`、`detachedDrainFailure`、`quitDrainFailure`、`reportWriteFailure`、`postQuitReportWriteFailure` 任一存在即失败；`:581` 仍拒绝 `cleanupFailure`。

### 3. P1: parser 仍按旧契约把 before owner 计数判死 (NEEDS_REWORK)
- 优先级：P1。
- 绝对路径与准确行号：`G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r34.ps1:628-631`。
- 触发条件：任意真实 runtime 场景在 Bind 后采样 `ownerCountsBefore`。真实负例已证明此时 `Forms=14`、`Bridge=14`；新契约要求 before `Forms>0`、`Bridge>0`、`Safety==2`。
- 宣称或权威契约：本批 `supervisor.request.md` 要求 “before owner counts只要求 old Forms/Bridge 真实非负…afterShutdown才严格 Forms=0/Bridge=0”；r33 只读契约要求 Safety 总数 2、两 target 各 1。
- 当前控制流：`:613` 允许 before 非负，`:622` 要求 before Safety=2；但 `:628` 把 `ownerCountsBefore` 与 `ownerCountsAfterShutdown` 放进同一循环，`:629` 强制 before Forms=0、`:630` 强制 before Bridge=0、`:631` 强制 before Safety=1。`:631` 与 `:622` 直接矛盾，parser 对任何新契约场景必然抛错；即使修正 Safety，`:629-630` 仍会因真实 before=14 抛错。
- 可复现命令：`Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r34.ps1' | Select-Object -Skip 627 -First 5`
- 最小修复范围：删除 `:628-632` 旧循环，或拆分为 afterShutdown-only 严格校验；保留 `:613` before 非负、`:622` before Safety=2、`:623-625` afterShutdown 严格 0/0/2。
- 尚缺的实机证据：真实 runtime JSON、真实 `ownerCountsBefore/AfterReinit/AfterShutdown` 与最终退出码。
- 结论影响：当前 parser 会把正确的新契约场景判为失败，故本批静态结论为 `NEEDS_REWORK`，不判 `SUPERVISION_PASS`。

## 进行中

- 无。已命中 P1 且证据充分，按“有证据就停”停止扩大检查面。

## 未知

- 5 旧模式与隔离/launch/日志主体的逐行未改面：本会话未完成逐行复核；worker 报告称 r31/r34 前缀 390 行、后缀 166 行相同，但未取得中央独立复核。
- 未构建、lint、测试、运行游戏；worker 自报 AST/文本检查不替代中央验证。
- r33 生产 DLL 双 patch 原子安装、两 runtime 实机 JSON、真实退出码仍未验证。
