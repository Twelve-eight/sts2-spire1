# controller-launch-fix-r20 监督审查报告

审查范围: 仅 r13 -> r20 的 4 行保留变量窄修, 不扩全控制器复审.
门禁: hub wait 对精确 worker `01a109c5-4859-7380-881e-1539f74abf42` 返回 completed / timedOut=false,
证据 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\controller-launch-fix-r20\gate-notice.json`.
本次审查未运行任何脚本/测试/游戏/git, 只做读取, 独立 hash 与窄 diff.

## 已确认

1. P0 独立 hash 一致 (独立复算, 非采信 worker 报告).
   - `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r13.ps1`
     SHA256 `38E6157B8F2A279A13CC47F4DE2D90C0CAF415CD1F7179824E126843717572C0`, 17761 bytes, 无 BOM.
   - `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r20.ps1`
     SHA256 `AFBE52076154176890F6291574B63CFEF315D76729F20706AB8C176AC3F83EA2`, 17785 bytes, 无 BOM.
   - 复现命令: `Get-FileHash -LiteralPath <r13>,<r20> -Algorithm SHA256`.
   - 与 gate-notice.json 记录的目标 hash 一致.

2. P0 窄 diff 精确为 4 行, 仅保留变量改名.
   - 逐行比较 r13/r20: 仅 L154, L164, L173, L174 不同, 其余 180 行逐字符相等.
   - L154 `$error=$null` -> `$launchError=$null`
   - L164 `catch{$error=$_.Exception.ToString()}` -> `catch{$launchError=$_.Exception.ToString()}`
   - L173 `$null-eq$error` -> `$null-eq$launchError`
   - L174 `LaunchError=$error` -> `LaunchError=$launchError`
   - 判据: 对 r13 全文按 `(?i)\$error\b` 替换为 `$launchError` 后, 与 r20 全文 `-ceq` 完全相等
     (normalized_equals=True); r20 中 `(?i)\$error\b` 匹配数为 0.
   - 触发条件与权威契约: `$Error` 为 PowerShell 自动只读变量且大小写不敏感, 原 r13 L154 在启动进程前即抛
     `Cannot overwrite variable Error because it is read-only or constant.`; 该断言由
     `G:\omp works\.tmp\forms-independent-20261005\native-r5-bindingloss-terminal-r16\controller-failure.json`
     (`Status=controller-prelaunch-failed`, `GameLaunched=false`, `ExitCode=1`) 支持.
   - 最小修复范围: 仅上述 4 个读写点, 未引入新变量语义分支.

3. P0 门禁与 JSON 契约未被改动 (计数逐项比对 r13 vs r20 不变).
   - `finalPassed` 2=2, `allChecksPassed` 2=2, `unobservedFaults` 4=4, `quitDrainSettled` 3=3,
     `expected` 10=10, `SharedConfig` 1=1, `RegressionObserved` 10=10, `safetyPassed` 2=2,
     `baseline-regression-observed` 1=1.
   - JSON 字段名 `LaunchError` 保留 (L174 `LaunchError=$launchError`); `catch` 内 `$_.Exception` 保留;
     L2 `$ErrorActionPreference` 保留且未被当作可写普通变量误改.
   - baseline 门禁未放宽: L91 仍要求 `status` 恰为 `baseline-regression-observed` 且 `regressionObserved` 为真,
     L92 仍对 `status` 非 `passed` 抛错, 故 baseline 不能计为安全 Passed.
   - 全文件自动/保留变量赋值扫描: 仅 L2 `$ErrorActionPreference`(偏好变量), 无 `$Error`/`$input`/`$args`/
     `$matches`/`$true`/`$false`/`$null` 等只读自动变量赋值.
   - r20 AST ParseErrors=0 (静态解析, 仅证明语法可解析, 不构成实机证据).

## 进行中

- 无. 窄修静态核验已收敛.

## 未知

- r20 未在实机运行: 无启动成功, Forms runtime, 退出码, 无窗口, quit drain, 共享 config 不变的实机证据;
  上述全部仅为静态门禁保真比对, 不得声称烟测通过.
- 未覆盖: 全控制器复审, 其它未改文件, 部署与产品代码行为.

## 结论

SUPERVISION_PASS (限窄修范围).

理由: 4 行变量窄修与独立 hash 均已核实, 精确等价于对 r13 做单一 token 改名, 无其它语义/门禁漂移,
未发现保留变量赋值残留. 该结论仅覆盖本次窄修; 实机启动与烟测仍属未验证, 不得据此宣称控制器已修复通过.

路由记录: worker 由用户指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`, 经当前 harness 原生子代理设施
(hub wait_agent) 执行; 本监督审查由主会话 (Codex harness) 直接完成, 未再委派, 未切换模型或 harness.