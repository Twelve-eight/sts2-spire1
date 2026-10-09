# Runtime finalization r29 worker

## 已确认

- P0 绝对路径 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:150` 至 `:166`：pre-quit 报告写入前现在先设置 `exitCode` / `quitStatus=pending` / `quitDrainSettled=false` / `finalEvidencePhase=pre-quit` / `status=pending|failed`，随后才写盘；pre-quit 写失败同步把盘上字段改为 `exitCode=1`。契约来自 request 第 1 条。未实机验证：失败注入后的首份盘上 JSON。
- P0 绝对路径 `RuntimeSafetySmokeRunner.cs:186` 至 `:251`：final fault gate 现在以 `faultsAtStart=0` 捕获订阅开始后的全部 raw/typed fault；scenario 阶段字段保留到 `scenarioUnobservedFaults` / `scenarioUnobservedFaultEvidence` / `scenarioExpectedRejections` / `scenarioUnexpectedUnobservedFaults`。final 调用 shared `ApplyUnobservedFaultGate`，按 reference identity 分区 expected/unexpected。最终 `passed` / `status` / `exitCode` 在 fault gate 之后统一收敛；late fault、quit/drain 失败、pre/post-quit 写失败均落为 `passed=false` / `status=failed` / `exitCode=1`。失败路径在当前 TEST 进程写盘后执行 `Environment.Exit(1)`；成功路径不硬退。未实机验证：真实 late fault 与真实退出码。
- P0 绝对路径 `RuntimeSafetySmokeRunner.cs:122`、`:253` 至 `:261`、`:348`、`:362`、`:375`、`:401`、`:789` 至 `:793`：expected root 列表现由 selected 场景把实际 settled command 抛出的 root 传回 final；记录函数只保留通过 `BindingLossFaultMentionsFormsRestart` 的 root，不再展开 inner leaf 作为独立批准集合。shared gate 仍执行单 root 身份叶子匹配。未实机验证：三条真实命令的异常树身份。
- `RUNTIME-SAFETY.md` 已更新 Drain/cleanup/fault 收尾说明与诚实边界，未改正常 check schema、命令数、action 数或既有持久化字段名；新增 scenario 快照字段仅为保留原始证据。
- 冻结改动 hash：`RuntimeSafetySmokeRunner.cs` = `48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C`，1385 行；`RUNTIME-SAFETY.md` = `2869D9A3F1147312E050C0A07F72EE51E1FCD3F373376FA4E6EC36F1E252D854`。基线 RuntimeSafetySmokeRunner.cs = `F416D053F38022F08046FEBC753A65A7E5BD6E2B82AD9AA97E37611244046919`。
- 改动路径仅：`G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs`、`G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RUNTIME-SAFETY.md`、本报告。未改 production 或 shared `FormNativeSmokeRunner.cs` bytes。

## 进行中

- 无；收尾静态实现已完成，未继续扩展。

## 未知

- 未构建/未 lint/未测试/未运行/未部署/未启动游戏；无真实编译结果、真实 JSON、真实 late fault、真实退出码、真实 Harmony 计数证据。
- 未验证 `Environment.Exit(1)` 在当前 Godot 退出时序中的实机效果；若该调用不可用，本文件无法进一步保证真实非零退出。
- 未验收边界：UI、读档、长战斗、多人、真热替换、性能，以及目标二进制对新 patch / Cubex encounter / 命令签名的兼容性。

## 结论

CODE_COMPLETE (静态实现完成; 实机证据仍未知, 未伪称通过)