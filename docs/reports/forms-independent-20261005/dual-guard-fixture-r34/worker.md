## 已确认
- 输入 hash 与请求一致：RuntimeSafetySmokeRunner.cs `007C2A515D49EF460B20C5AB5FAAD2C9F3DB9FFB4BAD79142C9125D0CF134DFC`；r31 脚本 `25E049A0E228861736F6CEA2569A49926AF71B6A485666CEED69D23574EF14EE`。
- 真实负例路径存在：`G:\omp works\.tmp\forms-independent-20261005\native-r30-core-r32\r1-runtime-safety\forms-runtime-safety.json`；JSON 为 failed/exit1，ownerCountsBefore.Forms=14，Shutdown 后 Forms=0，failure 为 guard/owner evidence 不匹配。
- 已读取 r33 只读请求；本轮不读/不写 r33 生产文件，guard 契约由请求文本与 runtime 测试载体独立落实。
- 失败根因：`RuntimeSafetySmokeRunner.cs:341` 在 Shutdown 前硬断言 `ownersBefore.Forms == 0 && ownersBefore.Bridge == 0`；真实采样顺序为 Bind 后采样，因此该断言先失败，后续 3 条真实 command 未执行。
- 当前测试载体只采样 Remove prefix：`RuntimeSafetySmokeRunner.cs:1123-1173` 仅查 `PowerCmd.Remove(PowerModel)`；`RUNTIME-SAFETY.md` 仍写安全 owner 前后均为 1，未覆盖 r33 双 prefix 契约。
## 进行中
- 将 runtime C# 载体扩展为双 guard 采样：保留 Remove 证明，新增 `playGuardBefore/playGuardAfterReinit/playGuardAfterShutdown`，并在 before 后真实重复 `Forms.MainFile.Initialize()`。
- 将 before owner gate 改为 Forms/Bridge 非负且 selected Bound 时 >0；afterShutdown 严格 Forms=0/Bridge=0；Safety 总数按 r33 契约为 2。
- 复制 r31 脚本为 r34，仅改 runtime parser；新增 final 失败字段出现即拒绝。
## 未知
- 未构建/测试/实机验证；r33 新 guard 的实际签名与安装行为尚需 hub 在集成后验证。

## 已确认 (追加)
- C# 载体已落盘第一阶段：`RuntimeSafetySmokeRunner.cs` 两个 runtime 场景都在 guardBefore 后新增 `playGuardBefore`，再真实重复 `Forms.FormsCode.MainFile.Initialize()`，随后新增 `playGuardAfterReinit`；Shutdown 后新增 `playGuardAfterShutdown`。
- before owner gate 改为：Forms/Bridge 非负且 selected 场景真实 Bound 时 Forms>0/Bridge>0；afterShutdown 严格 Forms=0/Bridge=0；Safety 总数按 r33 契约为 2。
- 引擎权威签名已核对：`G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs:62` 为 `protected override async Task ExecuteAction()`；`FormStanceSafetyGuard.cs:71-84` 使用 `PlayCardAction.ExecuteAction` 与 `PlayPrefix`。

## 已确认 (追加2)
- C# 双 guard 采样已完整落盘：`RuntimeSafetyPlayGuardEvidence` 字段为 owner, playMethodFound, playPatchedBySafetyOwner, prefixCount, prefixOwner, prefixPatchType, prefixMethodName, playMethodIdentity, targetDeclaringType, targetMethodName, targetReturnType, parameterCount；全部 typed。
- `RuntimeSafetyPlayGuardSnapshot.MatchesProof` 精确检查 PlayCardAction / ExecuteAction / Task / 0 参数 / owner / type / method / prefix=1 / identity 非空。
- `RUNTIME-SAFETY.md` 已改为双 prefix 契约，并记录真实负例与未验证边界；未称实机通过。

## 已确认 (追加3)
- r34 脚本已从 r31 基线复制并仅改 runtime parser：新增 `Test-RuntimePlayGuard` / `Test-RuntimePlayGuardSameIdentity`，逐字段校验 owner / playMethodFound / playPatchedBySafetyOwner / prefixCount / prefixOwner / prefixPatchType / prefixMethodName / playMethodIdentity / targetDeclaringType / targetMethodName / targetReturnType / parameterCount。
- parser 已加入 final 失败字段拒绝：`cleanupSkipReason` / `detachedDrainFailure` / `quitDrainFailure` / `reportWriteFailure` / `postQuitReportWriteFailure` 任一出现即失败；`cleanupFailure` 保持原拒绝。
- parser owner gate 已改：before 只要求 Forms/Bridge 非负且 `ownerCountsBeforeHealthy=true`，Safety 必须为 2；afterShutdown 严格 Forms=0 / Bridge=0 / Safety=2；Remove 与 Play 的 identity 都跨 before / afterReinit / afterShutdown 稳定。
## 进行中
- 复核脚本未误改 5 旧模式与 launch/隔离/日志；随后给出最终 hash 与字段行号。
## 未知
- 未运行脚本；PowerShell parser 仅做静态文本核对，未执行语法解析或实机验证。

## 已确认 (追加4)
- 与 r31 基线逐行比较：r31 746 行，r34 786 行；相同前缀 390 行，相同后缀 166 行；改动集中在脚本第 391-620 行的 runtime parser，5 个旧模式分支、launch/隔离/日志尾部均未改。
- r34 新增 Play guard parser 在脚本 `run-isolated-smoke-r34.ps1:391-414`；final 失败字段拒绝在 582-584；双 guard/owner 门禁在 602-620。
- 改动面未触及 `$testArg`、cases、mod 复制、settings、进程启动、shared snapshot、`Test-Evidence` 的非 runtime 分支与尾部 Move-Item。
## 进行中
- 计算三份最终 hash，核对 C# 文件的字段行号，并整理未测边界。
## 未知
- 未运行脚本；PowerShell parser 仅静态核对，未做 AST/语法执行或实机验证。

## 已确认 (追加5)
- 最终 hash：
  - `RuntimeSafetySmokeRunner.cs` = `231FBD20874B3398AA3AB89065DD8169402B6A0EE51743C3428F16AFAE32CD35`，96796 bytes。
  - `RUNTIME-SAFETY.md` = `8A3D2F1EBEE5230AC6F82EBEDCD2C12A6FFF8FE9F56999F7E55E1C651E5B4CA7`，7367 bytes。
  - `run-isolated-smoke-r34.ps1` = `6919A07627194DC62C7076AA26E829D2A818DDC61E1D18938CE57BB8D7835940`，67600 bytes。
- PowerShell AST 解析：`PS_PARSE_ERRORS=0`；C# 花括号/圆括号计数平衡 (148/148, 867/867)。
- 与 r31 基线逐行对比：r31 746 行，r34 789 行；相同前缀 390 行、后缀 166 行；脚本改动仅位于 runtime parser 区段 (391-620 附近)，5 个旧模式、launch、隔离、日志与尾部保留逻辑未变。
- C# 最终字段行号：playGuardBefore/AfterReinit/AfterShutdown 位于 277/294/354 与 460/477/552；owner 健康字段与 gate 位于 360-375 与 557-572；Play 采样位于 1245-1310；Play snapshot 位于 1474-1515。
- 项目文本检查：`G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs` 输出 `agent text accepted`，退出码 0。
## 进行中
- 无。
## 未知
- 未执行 build/lint/test/game/Git；C# 未编译，PowerShell 未运行，双 guard 实机稳定性与 r33 生产签名仍需 hub 验证。

## 已确认 (最终)
- Evidence shape 已补全到 `RUNTIME-SAFETY.md`；最终 `RUNTIME-SAFETY.md` hash 修正为 `A72EB63A5FC717AFB83E11B4383310C04BDF0B19D25F3ED8BCFAECB2A61BC62D` (7703 bytes)。
- 最终报告 `worker.md` hash = `2BB798FA6D0233DAF8E0A0E43A2A0B9677F3726A2EF4471BDC8DD1C242C648AA` (追加本段前快照)。
- 复跑文本检查通过：`agent text accepted`，退出码 0；r34 PowerShell AST 解析错误数为 0。
## 进行中
- 无；三文件白名单均已完成，未留下 stub。
## 未知
- 未执行 build/lint/test/game/Git。尚缺实机证据：真实编译、两 runtime 场景 JSON、重复 Initialize/Shutdown 后 Remove+Play identity 稳定性、r33 生产 DLL 的两 patch 原子安装、全量 no-mutation/fault/drain/exit 门禁。

## 已确认 (最终2)
- 补充实机可测的“不叠加”证明：C# 在两个场景重复 `Initialize()` 后新增 `ownerCountsAfterReinit` 采样，要求 Forms/Bridge 计数与 before 相等且 Safety 仍为 2；parser 在第 617-620 行验证同字段。
- 最终 hash (以本段写入前为准)：
  - `RuntimeSafetySmokeRunner.cs` = `45C4E599F66AFDB3044D8BBFCBB52B5EDE4615E7990E5EB109BA073AF8FEB946`，98088 bytes。
  - `RUNTIME-SAFETY.md` = `A72EB63A5FC717AFB83E11B4383310C04BDF0B19D25F3ED8BCFAECB2A61BC62D`，7703 bytes。
  - `run-isolated-smoke-r34.ps1` = `DC61A5C7F79A190E6132362387597925B2F39F7343CC3CD9F8690FA2243B04A3`，68221 bytes。
- 最终静态门禁：PowerShell AST 错误 0；C# 花括号 148/148；文本检查 `agent text accepted`，退出码 0。
## 进行中
- 无；唯一代码白名单三文件完成，无 stub。
## 未知
- 未 build/lint/test/game/Git。尚缺实机证据：真实编译、两 runtime 场景 JSON、重复 Initialize 后 owner=2 不叠、Shutdown 后 Remove+Play identity 稳定、r33 生产 DLL 两 patch 原子安装、全量 no-mutation/fault/drain/exit 门禁。
