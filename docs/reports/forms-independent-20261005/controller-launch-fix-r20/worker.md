# controller-launch-fix-r20 增量报告

## 已确认

1. 优先级: P0 控制器启动阻断。
   - 源文件: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r13.ps1`
   - 准确行号: 154, 164, 173, 174。
   - 触发条件: 进入第 154 行初始化 `$error=$null` 时，PowerShell 自动变量 `$Error` 为只读/常量，大小写不敏感，故控制器在任何游戏启动前抛出 `Cannot overwrite variable Error because it is read-only or constant.`。
   - 权威证据: `G:\omp works\.tmp\forms-independent-20261005\native-r5-bindingloss-terminal-r16\controller-failure.json` 记录 `Status=controller-prelaunch-failed`, `GameLaunched=false`, `Failure=Cannot overwrite variable Error because it is read-only or constant.`, `ExitCode=1`。
   - 当前控制流: r13 第 154 行先对保留变量赋值；异常发生在创建/启动进程前，故游戏未启动，没有 Forms runtime 证据。
   - 最小修复范围: 复制 r13 为新 r20，仅把四个精确的 `$error` 变量读写点改为 `$launchError`；不得改动第 2 行 `$ErrorActionPreference`、`catch` 中的 `$_.Exception`、JSON 字段 `LaunchError`。
   - 尚缺的实机证据: r20 未运行，未做烟测，不能声称修复已通过实机验证。

2. 优先级: P0 窄修完成，CODE_COMPLETE。
   - 目标文件: `G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r20.ps1`
   - 源文件 SHA256: `38e6157b8f2a279a13cc47f4de2d90c0caf415cd1f7179824e126843717572c0`
   - 目标文件 SHA256: `afbe52076154176890f6291574b63cfef315d76729f20706ab8c176ac3f83ea2`
   - 字节数: 源 17761，目标 17785；均无 BOM。
   - 修改行号: 154, 164, 173, 174。
   - 修改点:
     - L154 `$error=$null` -> `$launchError=$null`
     - L164 `$error=$_.Exception.ToString()` -> `$launchError=$_.Exception.ToString()`
     - L173 `$null-eq$error` -> `$null-eq$launchError`
     - L174 `LaunchError=$error` -> `LaunchError=$launchError`
   - 静态等值校验: 对 r13 仅执行相同四处普通字符串替换后，与 r20 全文逐字符相等；仅 4 行差异。
   - 保留校验: `$ErrorActionPreference` 1 处，`.Exception` 4 处，`LaunchError=` 3 处，关键门禁字符串计数不变；`Parsed JSON bool`、`finalPassed`、`unobservedFaults`、`quitDrainSettled`、initializer、退出码、无窗口、共享 config、路径白名单相关文本未被修改；`baseline-regression-observed` 未被改为 `passed`。
   - 未运行: 未执行 r20，未构建、未 lint、未测试、未启动游戏、未部署、未 git 操作。

## 进行中

- 无。窄修与静态核验已完成。

## 未知

- r20 脚本执行后的实际启动、运行时证据、退出码和门禁结果均未验证。
- 同批监督者尚未进行 hub wait 后的复核；本报告不声称已通过复核。