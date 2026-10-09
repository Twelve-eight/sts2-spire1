# r36 同批监督审查

## 已确认

### 0. 门禁与身份
- 本批门禁有效：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-owner-recovery-r36\gate-notice.json` 记录 Worker `01a10a9c-9dfc-7a70-b7af-ecad72625caf`、NativeTool `multi_agent_v1.wait_agent`、Status `completed`、TimedOut `false`、WorkerResult `CODE_COMPLETE`、ScriptSHA256 `09115BA296E8C5008BEF4373D3B605DAED1BFAB02DCB3FF7ED67F7AE2BFF3375`；不继承 r34 门禁。
- 实测 hash 与门禁一致：
  - `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r36.ps1` SHA256 `09115BA296E8C5008BEF4373D3B605DAED1BFAB02DCB3FF7ED67F7AE2BFF3375`，67805 bytes。
  - r34 基线 `run-isolated-smoke-r34.ps1` SHA256 `DC61A5C7F79A190E6132362387597925B2F39F7343CC3CD9F8690FA2243B04A3`，68221 bytes。
  - 源 `RuntimeSafetySmokeRunner.cs` SHA256 `45C4E599F66AFDB3044D8BBFCBB52B5EDE4615E7990E5EB109BA073AF8FEB946`，98088 bytes；与 r34 冻结值一致，本批未改。

### 1. r36 相对 r34 的唯一 delta
- 独立字节级重建：以 r34 原始字节删除单一连续区间 `68221 - 67805 = 416` bytes 后，与 r36 逐字节相等 (`rebuiltEqualsR36=True`)。
- 删除区间解码内容为 r34 第 628-632 行旧 `before+afterShutdown` 共用循环，逐字：
```text
  foreach($pair in @(@('ownerCountsBefore',$ownersBefore),@('ownerCountsAfterShutdown',$ownersAfter))){
   if($pair[1]['Forms'] -ne 0){throw ($pair[0]+'.Forms must be exactly 0')}
   if($pair[1]['Forms.FormStanceMode.Watcher'] -ne 0){throw ($pair[0]+'.Forms.FormStanceMode.Watcher must be exactly 0')}
   if($pair[1]['Forms.FormStanceSafety'] -ne 1){throw ($pair[0]+'.Forms.FormStanceSafety must be exactly 1')}
  }
```
- 结论：r36 唯一 delta 与请求一致，无其它字节改动。

### 2. 保留面未放宽
- before old owner 非负仍保留：r36 `:613`。
- afterReinit 两 old owner 必须等于 before 且 Safety=2：r36 `:617-620`。
- before Safety=2：r36 `:622`。
- afterShutdown 严格 Forms=0 / Bridge=0 / Safety=2：r36 `:623-626`。
- Remove proof 与 identity 稳定仍保留：r36 `:603-605`。
- 三组 Play proof 各 prefix=1 且跨重复 Initialize / Shutdown identity 稳定：r36 `:606-610`。
- Play typed 校验与 Remove typed 校验仍在 `:391-414` 与 `:375-390`；`playGuardPassed` 仍参与最终 `guardPassed`（r36 `:627` 及 r34 对应 C# 门禁）。
- final 失败字段拒绝仍保留：r36 `:582-584` 对 `cleanupSkipReason`、`detachedDrainFailure`、`quitDrainFailure`、`reportWriteFailure`、`postQuitReportWriteFailure` 任一存在即失败；`:581` 仍拒绝 `cleanupFailure`。

### 3. 5 旧模式与隔离/launch/日志主体
- 与 r31 原已接受脚本逐行比较：第一处行差异为 r31/r36 第 391 行，改动全部落在 runtime parser 区段；r31 尾部 159 行（r31 `:588-746`）与 r36 尾部 159 行（r36 `:630-788`）逐行完全相同，`suffixLineDiffs=0`。
- 5 旧模式分支（effects/lifecycle/saveguard/bindingloss-terminal/bindingloss-shutdown）位于共同前缀，未改；mod 复制、settings、进程启动、隔离环境、shared snapshot、`Test-Evidence` 非 runtime 分支与尾部 `Move-Item` 均在共同前缀或共同后缀内，未改。
- r31 前 390 行与 r36 前 390 行逐行相同（`firstDiffLine=391`），隔离主体与 launch 主体未改。

## 进行中

- 无。已命中面均完成，未发现新 P1。

## 未知

- 未构建、lint、Parser、测试、运行游戏；未验证真实 runtime JSON、真实退出码、真实 Harmony 输出。
- 中央已运行的 64 旧解析 / 15 fault / 22 Play metadata 合成检查不能替代 r36 实机或本会话源码审核。

## 结论

SUPERVISION_PASS（仅静态）

- r36 相对 r34 的唯一字节级 delta 为删除旧 `628-632` 矛盾循环，未放宽 before 非负、afterReinit、afterShutdown、Safety=2、各 target=1 或 identity 稳定门禁。
- 5 旧模式与隔离/launch/日志主体相对 r31 未改。
- 不称实机通过；真实 runtime 证据仍须由 hub 集中采集。
