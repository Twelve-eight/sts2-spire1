## 已确认
- [P0] `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs:251` 使用未限定 `Environment.Exit(1)`；构建日志 `G:\omp works\.tmp\forms-independent-20261005\native-smoke-build-r29-all.log:9` 记录 CS0104，Godot.Environment 与 System.Environment 歧义。
- 输入候选 SHA256 与请求一致：`48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C`。
- 已仅把失败路径第 251 行限定为 `System.Environment.Exit(1);`；文件内 `Environment` 其余引用原为 `System.Environment`（第 70、1217 行）或字符串（第 50、1221 行），无新增 exit/kill。
- 新 SHA256：`007C2A515D49EF460B20C5AB5FAAD2C9F3DB9FFB4BAD79142C9125D0CF134DFC`；diff 仅为第 251 行插入 `System.` 前缀，其余字节未动。
- 静态收尾 delta 已核对：`finalEvidencePhase` 先写 pre-quit（153）、后写 post-quit（185）；顶层持有 `finalExpectedRejectionRoots`（122）并传 selected（133）及 final gate（204-209）；final gate 从订阅起 `faultsAtStart=0`（207），gate 输出 raw/typed/expected/unexpected 完整字段（FormNativeSmokeRunner.cs:1295-1298）；final gate 后统一 `failed/passed/exitCode`（221-224）；必要失败当前 TEST 进程非零退出（250-251）。
- 命令：`Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs' -Algorithm SHA256`。
## 进行中
- 无。
## 未知
- 未执行 build/lint/test/game；实机行为未验证。
