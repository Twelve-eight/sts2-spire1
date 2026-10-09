# 同批监督结论

- 状态: SUPERVISION_PASS
- 监督者 worker id: 01a1097a-c417-7423-82ce-19637bb99183
- 门禁: 主会话已报告 multi_agent_v1.wait_agent 对该精确 worker returned completed / timed_out=false；gate-notice.txt 与 coordination.md 内容一致 (落盘时点 2026-10-05T08:43:17.4664848+08:00)。
- 本轮仅只读核对；未构建/lint/测试/运行游戏/部署/git，未再委派，未写除本文件外任何路径。
- 结论边界: 本结论仅覆盖静态源码契约与只读证据，不含运行时实机证据。

## 已确认

1. 门禁成立。gate-notice.txt 与 coordination.md 均为同一门禁陈述 (SHA256 72CCF46A09BAB6C60EB32D1E02764F925B5C901A4AF01658B132963B195A814D)，声明精确 worker 01a1097a-c417-7423-82ce-19637bb99183 completed、timed_out=false，并给出新脚本 hash。

2. 写集与 hash 核对通过。
   - 新脚本 `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r13.ps1`：17761 字节，SHA256 `38E6157B8F2A279A13CC47F4DE2D90C0CAF415CD1F7179824E126843717572C0`，与 gate-notice/worker.md 一致。
   - 原脚本 `run-isolated-smoke.ps1` 未改：SHA256 `51ECBC4B1743AE9FDD1B229C559D33395D97FAC41DE5B598FCE2FBFDAA32D7DC`，与 worker.md 声明一致。
   - 目录内仅有 run-isolated-smoke.ps1 与 run-isolated-smoke-r13.ps1 两个 run-isolated-smoke* 文件；未见第三个派生脚本。

3. 严格 JSON 布尔类型判定成立 (新脚本 41-43 行)。
   - `Test-True` 要求 `$v -is [bool] -and $v`；`Test-False` 要求 `$v -is [bool] -and (-not $v)`；`Test-ExactString` 要求 `$v -is [string]` 且 Ordinal 相等。
   - 已实测 PowerShell 语义: `true`/`false` 为 [bool]；字符串 `"true"`/`"false"`、数字 `1`/`0`、`$null` 均非 [bool]。故 JSON 里 `"true"`、`1`、缺失/null 一律 fail closed。

4. saveguard 严格判定成立 (新脚本 44-75 行)。
   - 要求 `status` 严格 `completed` (51)；`finalPassed`/`allChecksPassed`/`carrierPatchesHealthy` 均为布尔 true (52-54)。
   - `checks` 必须存在非 null (55-56)，恰好 4 项 (57-58)，每项非 null，`name` 为 string 且属于四项集合、无重复 (61-68)，每项 `status` 严格 `passed` (69)，四项缺一即失败 (71)。
   - 旧 `status=passed` OR 分支与旧 `passed` 字段放行已删除：新脚本中 `(Property $j 'passed')` 出现次数为 0，saveguard 分支不再引用 `passed` 字段。

5. 四项 check 集合与生产者一致。
   - 生产者 `G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\SaveGuardSmokeRunner.cs` 依次以 name `guard-owner`(159)、`form-stance-identity`(163)、`vanilla-modifier-round-trip`(167)、`unknown-modifier-deprecated`(171) 调用 `RunCheckOnMainThreadAsync`，成功置 `status="passed"`(488)，最终 `status=completed`/`finalPassed`/`allChecksPassed`(115-116,174)、`carrierPatchesHealthy`(145)。控制器的四项名称集合与严格布尔契约与生产者逐项吻合。

6. 两个 loss 场景接线成立 (新脚本 31-32, 37-38 行)。
   - `bindingloss-terminal` → 唯一用例 `b1-terminal`，`bindingloss-shutdown` → 唯一用例 `b2-shutdown`；二者 Mods 均为 `BaseLib/Watcher/Forms/FormsNativeSmoke`，`Spire1=$false`，`Watcher=$true`。
   - `$testMod` 在非 saveguard 模式下取 `FormsNativeSmoke` (31)，故两场景挂载的是 FormsNativeSmoke。
   - 参数分别为 `--forms-binding-loss-terminal` / `--forms-binding-loss-shutdown` (32)，与生产者开关一致 (FormNativeSmokeRunner.cs 102,108)。

7. BindingLoss 文件名与严格判定成立 (新脚本 76-116 行)。
   - 文件名按 `forms-binding-loss-<scenario>.json` 拼接 (80)，scenario 常量为 `binding-loss-terminal`/`binding-loss-shutdown` (79)，与生产者 `WriteBindingLossJson` 的 `"forms-binding-loss-" + scenario + ".json"` (BindingLossSmokeRunner.cs 780) 一致。
   - 严格字段: `status`=passed (92)、`testOnly`=true (93)、`scenario` 精确匹配 (94)、`formsBoundBefore`=true (95)、`selectedBefore`=true (96)、`bindingStateAfter` 严格 `unavailable` (97)、`explicitRejection`=true (98)、`noNativeMutation`=true (99)、`regressionObserved`=false (100)、`safetyPassed`=true (101)、`unobservedFaults` 存在且为空数组 (102-105)。
   - 叠加原 final 报告: `status`=completed (106)、`finalEvidencePhase`=post-quit (107)、`quitDrainSettled`=true (108)、`quitDrainOutcome`=settled (109)、`exitCode` 为 int/long 且 0 (110-112)。
   - 生产者契约吻合: 成功路径 status="passed"、explicitRejection/noNativeMutation/safetyPassed=true、regressionObserved=false (BindingLossSmokeRunner.cs 360-381)；bindingStateAfter 在 bridge 不可用时写 "unavailable" (267,295-298)；unobservedFaults 在 finally 写入 (445)；final 报告 status=completed/quitDrainSettled/quitDrainOutcome=settled/finalEvidencePhase=post-quit (FormNativeSmokeRunner.cs 235-255)。timeout/未执行走 status="failed" (391)，不被接受。

8. 基线负例不冒充安全通过。
   - 新脚本 91 行: 仅当 `status` 严格 `baseline-regression-observed` 且 `regressionObserved` 为布尔 true 时置 `$ev.RegressionObserved=$true`；随后 92 行仍因 status≠passed 抛错，`Passed` 保持 false。
   - `Passed` 参与 173 行安全门与 184 行退出码判定，故此类走退出码 2。`RegressionObserved` 另记入 controller 结果 (172,174) 供主会话收割，未把 Passed 翻真。与生产者 `baseline-regression-observed`+`regressionObserved=true` (BindingLossSmokeRunner.cs 382-388) 兼容。

9. 原隔离边界保留。
   - 原启动循环逐行字节比对: 旧 56-94 行 vs 新 133-171 行，0 处差异 (Assert-Under、独占 exe 检测、新 APPDATA/LOCALAPPDATA/TEMP/TMP/GseSavePath、SPIRE1_FORM_SMOKE_REPORT、--headless/--audio-driver Dummy/--disable-crash-handler、hidden、BelowNormal、窗口监测、stdout/stderr 排空、共享配置前后 hash、staging 留存、Move-Item 到 staged-mods-retained、退出码 0/2 全部原样)。
   - 关键隔离标记在两版中计数完全一致 (各 1 次): RUN_EXISTS、ISOLATED_MODS_NOT_EMPTY、EXE_MISSING、SETTINGS_TEMPLATE_MISSING、ISOLATED_EXE_ALREADY_RUNNING、MOD_FILE_REPARSE、MODS_MOVE_REPARSE、REPARSE-DENY、PATH-DENY、staged-mods-retained、shared-before.json、shared-after.json、SPIRE1_FORM_SMOKE_REPORT、--headless、--audio-driver、--disable-crash-handler、BelowNormal、WindowStyle、OWN_PROCESS_EXIT_TIMEOUT、GseSavePath、staging.json；Assert-Under 两版均为 5 次。
   - 新增路径仅为 opt-in 模式/参数/用例与严格判定函数，未新增并行游戏进程分支。

10. effects/lifecycle 选择语义未改。
   - 新脚本 124 行 (effects) 与 127 行 (lifecycle) 的判定表达式与旧脚本 44/47 行逐字节相同 (比较结果 True)。
   - 34-35 行 effects/lifecycle 用例定义与旧脚本 34-35 行一致；未改二者选择语义。

11. 静态语法有效。以 PowerShell Parser::ParseFile 解析新脚本，ParseErrors=0 (仅静态解析，非运行验证)。

12. 未把 test DLL 放进生产包。脚本仅将 SmokePayload 复制进隔离目录 `native-client-20261005-01\mods` (138-139)，未触及生产包或 Steam 目录。

13. 只读夹具证据 (由主会话在门禁后落盘，非本监督者执行) 与静态判定自洽。
   - `controller-r13-fixture-results.json` (2026-10-05T08:44:19, ScriptSha256=38E6...72C0) 记录 16/16 匹配: positive-exact-four 通过；missing/string/false 三组 finalPassed/allChecksPassed/carrierPatchesHealthy 均 fail closed；duplicate-check-and-missing-other→"duplicate check name: guard-owner"；extra-check→"exactly 4 entries; got 5"；one-failed-check→"status must be exactly passed"；null-checks→"checks must be present and non-null"；old-loose-passed-status→"status must be exactly completed"；missing-report→"Missing report forms-save-guard-smoke.json"。逐项复核夹具 JSON 内容与 expectations.json，与控制器代码路径一致。

## 进行中

- 无。最终只读监督已完成。

## 未知

- 未运行脚本或游戏，因此 BindingLoss 实机 JSON 的真实取值、saveguard 实机报告、以及两场景实机退出码均未由本监督者观测；属主会话集中执行范畴。
- 本监督者未能独立确认夹具与 expectations.json 的作者归属 (目录时间 2026-10-05 08:38:39，早于新脚本 08:40:51)；worker.md 仅称"已确认存在但未运行"。若该夹具由 worker 创建，则超出其声明的"唯一代码可写"范围，但此点不影响新脚本本身的正确性判定。
- r11 生产者源码当前状态 (BindingLossSmokeRunner.cs 08:34:36、FormNativeSmokeRunner.cs 08:27:05) 晚于/早于 worker 窗口，本监督者按只读契约核对字段，未验证其编译产物。
