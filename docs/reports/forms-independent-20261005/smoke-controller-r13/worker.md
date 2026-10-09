# worker.md - smoke-controller-r13

## 已确认
- 读取 `worker.request.md`、同目录 `protocol.md` 与 `G:\omp works\AGENTS.md`；本轮唯一模型/路由/effort 为 `global:deepseek-v4.1-flash` / `wb2api` / `xhigh`。未换模型/路由/fallback，未再委派，未启动其它 harness，未构建/lint/测试/运行游戏/部署/git。
- 唯一代码写集：新建 `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r13.ps1`（原 `run-isolated-smoke.ps1` 未改，SHA256 仍为 `51ECBC4B1743AE9FDD1B229C559D33395D97FAC41DE5B598FCE2FBFDAA32D7DC`）。
- 新脚本 SHA256 `38E6157B8F2A279A13CC47F4DE2D90C0CAF415CD1F7179824E126843717572C0`，17761 字节，`[Management.Automation.PSParser]::Tokenize` 解析错误 0（静态语法检查，非运行验证）。
- 原有隔离保护全部保留：`Assert-Under` 路径/reparse 检查、独占 exe 检测（`ISOLATED_EXE_ALREADY_RUNNING`）、新 APPDATA/LOCALAPPDATA/TEMP/TMP/GseSavePath、`SPIRE1_FORM_SMOKE_REPORT`、`--headless`、`--audio-driver Dummy`、`--disable-crash-handler`、hidden、BelowNormal、窗口监测、stdout/stderr 排空、共享配置 hash 前后对比、`staged-mods-retained` 留存、退出码 `0/2` 安全门。未新增并行游戏进程路径。
- 任务一已实现：saveguard 仅当 `status` 严格等于 `completed`、`finalPassed`/`allChecksPassed`/`carrierPatchesHealthy` 为 JSON 布尔 `true`、`checks` 为恰好 4 项、name 恰好各出现一次且为 `guard-owner`/`form-stance-identity`/`vanilla-modifier-round-trip`/`unknown-modifier-deprecated`、每项 `status` 严格等于 `passed` 才判 Passed。已删除旧 `passed` 字段与 `status=passed` 放行分支（saveguard 判定不再引用 `passed` 字段）。
- 任务二已实现：新增 opt-in `bindingloss-terminal`/`bindingloss-shutdown`；各只有一场 `b1-terminal`/`b2-shutdown`，Mods 均为 `BaseLib/Watcher/Forms/FormsNativeSmoke`，`Spire1=$false`；参数分别为 `--forms-binding-loss-terminal` / `--forms-binding-loss-shutdown`。
- BindingLoss 文件名按已收敛路径格式 `forms-binding-loss-<scenario>.json`，实为 `forms-binding-loss-binding-loss-terminal.json` 与 `forms-binding-loss-binding-loss-shutdown.json`（与 r11 生产者 `WriteBindingLossJson` 一致）。
- BindingLoss 严格判定（消费 r11 生产者字段）：`status` 严格等于 `passed`、`testOnly`=true、`scenario` 严格等于对应 scenario、`formsBoundBefore`=true、`selectedBefore`=true、`bindingStateAfter` 严格等于 `unavailable`、`explicitRejection`=true、`noNativeMutation`=true、`regressionObserved`=false、`safetyPassed`=true、`unobservedFaults` 字段存在且为空数组；再结合原 final 报告 `status` 严格等于 `completed`、`finalEvidencePhase` 严格等于 `post-quit`、`quitDrainSettled`=true、`quitDrainOutcome` 严格等于 `settled`、`exitCode`=0。
- `status=baseline-regression-observed` 且 `regressionObserved=true` 时仅写 `RegressionObserved=true`（controller 结果与 RuntimeEvidence 均保留该字段），不把 `Passed` 置真；此类仍走安全门失败退出码 2。timeout/未知/未执行均按 fail closed 处理，不算拒绝成功。
- 类型防护（本轮用户强调项）：`Test-True`/`Test-False` 要求 `-is [bool]`，PowerShell 隐式字符串/数字到 bool 的转换被排除；`Test-ExactString` 要求 `-is [string]` 且 `Ordinal` 相等；`exitCode` 要求 `int`/`long`。因此 JSON 中的 `"true"`、`"false"`、`1`、`0` 或缺失/null 一律 fail closed。

## 进行中
- 无。代码与报告已交付；未按用户要求运行脚本或中央夹具。

## 未知
- 未运行脚本，故无实机证据；`run-isolated-smoke-r13.ps1` 的运行时行为、BindingLoss 实机 JSON 内容、以及 `controller-r13-fixtures/expectations.json` 夹具通过情况均待主会话集中执行确认。
- 已确认 `G:\omp works\.tmp\forms-independent-20261005\controller-r13-fixtures\` 夹具与 `expectations.json` 存在，但未运行（遵用户指令）。
- r11 收尾后的实机字段值未由本 worker 验证；仅依据只读源码核对生产者契约（`SaveGuardSmokeRunner.cs`、`BindingLossSmokeRunner.cs`、`FormNativeSmokeRunner.cs`）。

## 写集 / 哈希 / 调用参数 / 未知
- 写集：`G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r13.ps1`（新建）；`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\smoke-controller-r13\worker.md`（本报告）。
- 新脚本 SHA256：`38E6157B8F2A279A13CC47F4DE2D90C0CAF415CD1F7179824E126843717572C0`。
- 原脚本 SHA256：`51ECBC4B1743AE9FDD1B229C559D33395D97FAC41DE5B598FCE2FBFDAA32D7DC`（未改）。
- 调用参数（主会话运行用，非本 worker 执行）：`-SmokeMode saveguard`；`-SmokeMode bindingloss-terminal`；`-SmokeMode bindingloss-shutdown`；其余必填参数同原控制器 `-FormsPayload/-Spire1Payload/-SmokePayload/-RunRoot`。
- 未知：实机运行结果、夹具执行结果、r11 实机 JSON 与严格契约的一致性。