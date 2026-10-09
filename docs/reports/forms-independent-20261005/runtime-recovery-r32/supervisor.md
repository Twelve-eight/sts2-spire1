# r32 同批监督审查

## 已确认

- 门禁有效：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-recovery-r32\gate-notice.json` 记录 Worker `01a10a7d-236b-7d43-8b94-772aa19d4560`、NativeTool `multi_agent_v1.wait_agent`、Status `completed`、TimedOut `false`、WorkerResult `CODE_COMPLETE`。
- 身份与 delta：当前 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs` SHA256 `007C2A515D49EF460B20C5AB5FAAD2C9F3DB9FFB4BAD79142C9125D0CF134DFC`，与 gate-notice 一致；相对 r29 冻结副本 `G:\omp works\.tmp\forms-independent-20261005\RuntimeSafetySmokeRunner-r29-frozen-for-r31.cs`（SHA256 `48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C`）仅有 1 行差异：当前 :251 为 `System.Environment.Exit(1);`，冻结副本对应行为 `Environment.Exit(1);`。无其它增删改。
- pre-quit phase 先置：`RuntimeSafetySmokeRunner.cs:150-154` 在首次写盘前设置 `exitCode`、`quitStatus=pending`、`quitDrainSettled=false`、`finalEvidencePhase=pre-quit`、`status=pending|failed`；`:156-165` 才调用 `WriteRuntimeSafetyJson`，写失败立即同步 `status=failed`、`passed=false`、`exitCode=1`。
- 最后 raw 从订阅起点完整采集：`:204-209` 最终 gate 使用 `faultsAtStart=0`，覆盖 `:113-114` 订阅后的全部 raw fault；`:187-196` 先把 scenario 阶段字段复制为 `scenarioUnobservedFaults`、`scenarioUnobservedFaultEvidence`、`scenarioExpectedRejections`、`scenarioUnexpectedUnobservedFaults`，再由 shared gate 写入最终字段。
- 真实 command root 与单 root 引用身份批准：selected 场景 `:254-257` 接收 `finalExpectedRejectionRoots`，`:348/:362/:375` 三条真实命令传入，`:789-792` 仅记录明确 Forms-restart root；`FormNativeSmokeRunner.cs:1284-1298` 写入 `unobservedFaults`、`unobservedFaultEvidence`、`expectedRejections`、`unexpectedUnobservedFaults`；`:1328-1351` 用 `ReferenceEqualityComparer` 按叶子集合匹配，且 `approvedLeafSets.Any(... observedLeaves.All(approvedLeaves.Contains))` 保证单 root 批准，不按文本合并 root。
- fault gate 后状态一致：`:204-209` 得到 `finalFaultGatePassed` 后，`:214-224` 以 `exitCode==0 && passed && finalFaultGatePassed && quitStatus.StartsWith("executed") && quitDrainSettled && preQuitWritten` 重算 `completed`，并同步写 `status`、`passed`、`exitCode`；`:230-238` post-quit 写失败再次同步 `status=failed`、`passed=false`、`exitCode=1`。不存在 gate 后 `failed` 与 `exitCode=0` 并存的静态路径。
- `System.Environment.Exit(1)` 边界：`:248-251` 仅在最终 `exitCode != 0` 时执行；成功路径 `exitCode == 0` 不硬退出。该调用是 .NET 进程退出 API，作用于当前 TEST 进程；未发现调用其它 harness、peer 或产品进程的路径。此变更补齐了 r26 收尾 P1 中 late fault / post-quit 写失败后无法改变已提交 Quit 退出码的问题。
- drain/cleanup/真实动作字段保留：`:401` 与 `:546` 仍调用 `RuntimeSafetyDrainAndCleanupAsync`；`:614-686` 保留 `cleanup`、`cleanupSkipReason`、`cleanupFailure`、`cleanupDrainSettled`、`detachedDrainFailure` 及 fault gate 字段；三条命令的 `command/before/evidence/after/changed/passed` 在 `:349/:363/:376` 与 finalize 路径保留，未被本 delta 删除。
- 文档诚实性：`G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md:3` 与 `:87-91` 明确本轮静态实现，未构建、lint、测试、运行、部署；`:81-83` 描述 fail-closed 与当前 TEST 进程 `Environment.Exit(1)` 边界，未声称实机通过。
- 只读旁证（非本会话执行）：现有 `G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r32-all-status.json` 记录 `ExitCode: 0`、`SourcesUnchanged: true`，且 `RuntimeSafetySmokeRunner.cs` Before/After 均为当前 SHA；这仅说明中央已有候选构建，不替代实机验证。

## 进行中

- 无。窄审核已完成，未发现 P1，未发现需要返工的 delta。

## 未知

- 未构建、lint、测试、运行游戏、部署或采集退出码；本会话未执行任何 runtime 命令。
- 无真实 JSON、真实 Harmony 输出、真实 settled 命令、真实 fault 队列、真实退出码或实机 UI 证据。
- 目标二进制对新增 patch、Cubex encounter 与命令签名的兼容性仍未由本审核验证。

## 结论

SUPERVISION_PASS（仅静态）

- 本结论只覆盖 r32 相对 r29 的收尾 delta 与 r26 已审静态面的保留情况。
- 不称实机通过；实机证据与最终退出码仍须由主 hub 集中采集。
