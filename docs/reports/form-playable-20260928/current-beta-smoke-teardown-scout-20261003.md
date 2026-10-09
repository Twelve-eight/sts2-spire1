# current beta smoke teardown scout - 2026-10-03

## 已确认

### P0 - 外层 420 秒超时后,主进程未在 Kill 后的 10 秒内退出
- 证据文件:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-native-form-smoke.ps1:113-127`
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-final.json:19-25`
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-final.json:35`
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\central-rerun-current-beta-20261003.log:19-25`
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\central-rerun-current-beta-20261003.log:35`
- 触发条件: `--form-native-smoke` 启动后,主进程持续运行 420 秒;外层设置 `timedOut=true`,调用 `$proc.Kill()`,随后 `WaitForExit(10000)` 返回 false,抛出 `Isolated form smoke process did not exit after bounded wait`。
- 权威契约: 外层脚本要求主进程在 420 秒内自行退出;超时分支仍要求 Kill 后 10 秒内退出,否则本次运行不能产出有效 `exitCode`。
- 当前控制流: 外层 while 等待 -> 420 秒超时 -> `Kill()` -> `WaitForExit(10000)` 失败 -> catch 记录 `launchError` -> finally 再次尝试 Kill -> `run-final.json` 记录 `exitCode=null`、`logDrainCompleted=false`、`cleanupCompleted=false`。
- 可复现命令(只读证据复核):
  ```powershell
  Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-native-form-smoke.ps1' | Select-Object -Index 112,126
  Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-final.json'
  ```
- 最小修复范围: 尚未确定修复点;需要先区分 runner 未到达 final、final 写入未生效、Godot `Quit()` 未终止进程、主进程 Kill 后仍被原生 teardown 阻塞这四类原因。
- 尚缺的实机证据: 本次只读到外层结果与日志文件,未启动游戏;不能据此断言具体卡在 runner、engine、子进程还是原生 teardown。

### P0 - 当前 `run-final.json` 属于 2026-10-03 08:13-08:21 重跑,但同目录 scenario/final JSON 属于 2026-10-02 21:30-21:31 旧运行
- 证据文件:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-final.json:2-3` 记录本地时间 `2026-10-03T08:13:59` 至 `08:21:11`。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\form-native-smoke-calm.json` 的 `startedUtc` 为 `2026-10-02T21:30:47.3872776+00:00`, `completedUtc` 为 `2026-10-02T21:31:02+00:00`。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\form-native-smoke-wrath.json` 的 `startedUtc` 为 `2026-10-02T21:31:02.2171806+00:00`。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\form-native-smoke-divinity.json` 的 `startedUtc` 为 `2026-10-02T21:31:09.9529423+00:00`。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\form-native-smoke-final.json` 的 `status=completed`, `exitCode=0`, `quitStatus=executed-main-thread`, `quitDrainSettled=true`, `finalEvidencePhase=post-quit`。
- 触发条件: 同一 `$run` 目录被 2026-10-03 08:13 重跑复用,但新进程没有覆盖 `form-native-smoke-*.json`。
- 权威契约: `run-native-form-smoke.ps1:3` 固定 `$run` 目录;`FormNativeSmokeRunner.cs:2440-2468` 以固定文件名写入该目录;两份证据必须来自同一进程批次才能互相佐证。
- 当前控制流: 2026-10-03 08:13:59 启动新进程 -> 08:16:04 Godot 轮转旧日志 -> 08:21:11 外层超时并写新 `run-final.json`;同目录 runner JSON 仍停留在旧运行。
- 可复现命令(只读证据复核):
  ```powershell
  Get-Content -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\run-final.json'
  Get-ChildItem -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003' -Filter 'form-native-smoke-*.json' | Select-Object Name,CreationTimeUtc,LastWriteTimeUtc
  ```
- 最小修复范围: 重跑前隔离/清空旧 runner JSON,并在外层结束后校验 `form-native-smoke-final.json` 的 `startedUtc`/`completedUtc` 是否落在本次窗口内,否则不得把 scenario 结果用于本次实机结论。
- 尚缺的实机证据: 当前重跑进程在 08:16:04 之后的 runner 状态;新进程是否曾到达 `RunAndQuitAsync` 尚不能从 JSON 判定。

### P0 - 604 行含三形态、Steam shutdown、资源泄漏的日志是旧运行被轮转出的备份,不是当前 `51224C20` DLL 的执行证据
- 证据文件:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\appdata\SlayTheSpire2\logs\godot.log` 只有 38 行,最后一行是 `Registered config for mod BaseLib`;文件创建时间为 `2026-10-02T21:30:21.7233511Z`,最后写入时间为 `2026-10-03T00:20:01.7715546Z`。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\appdata\SlayTheSpire2\logs\godot2026-10-03T08.16.04.log` 共 604 行,创建与最后写入时间均为 `2026-10-03T00:16:04.2692576Z`;前 38 行与当前 `godot.log` 完全相同。
  - 轮转日志第 29 行加载 `BaseLib.dll`、第 55 行加载 `Watcher.dll`、第 290 行加载 `Spire1.dll`、第 382 行 `Watcher bridge bound`、第 504/507/508/540/544/576/581 行出现三形态卡牌、第 588 行 `Steamworks: shutting down...`、第 591-603 行资源泄漏。
  - 当前 `godot.log` 只到 BaseLib 配置注册,没有 `Loading assembly DLL ... Watcher.dll`、`... Spire1.dll`、`Watcher bridge bound` 或任何 `playing card` 行。
  - `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll` 的 SHA256 为 `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`,LastWriteTimeUtc 为 `2026-10-02T23:55:18Z`;隔离游戏目录 `native-isolated-20260930\game\mods\Spire1\Spire1.dll` 在 2026-10-03 00:13:13Z 被替换为同一 SHA。
- 触发条件: 当前 08:13 重跑在 08:16:04 触发 Godot 日志轮转;旧 `godot.log` 被改名为 `godot2026-10-03T08.16.04.log`,新 `godot.log` 只写入到 BaseLib 配置注册。
- 权威契约: `FormNativeSmokeRunner.cs:48-60` 只有在游戏完成 mod 初始化并调用 `TryStart` 后才启动 runner;当前 `godot.log` 尚未到达 Watcher/Spire1 加载,因此本次进程的 runner 尚未获得执行机会。
- 当前控制流: 新进程启动 -> 加载 BaseLib -> 注册 BaseLib 配置 -> 无后续日志;旧日志备份保留完整三形态与退出记录。旧 JSON 的 UTC `21:30:47`-`21:31:18` 与旧日志的三形态时间线一致;新 `51224C20` DLL 构建于 UTC `23:55:18`,不可能出现在旧运行中。
- 可复现命令(只读证据复核):
  ```powershell
  $run='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003'
  Get-Item -LiteralPath (Join-Path $run 'appdata\SlayTheSpire2\logs\godot.log'),(Join-Path $run 'appdata\SlayTheSpire2\logs\godot2026-10-03T08.16.04.log') | Select-Object FullName,Length,CreationTimeUtc,LastWriteTimeUtc
  Select-String -LiteralPath (Join-Path $run 'appdata\SlayTheSpire2\logs\godot.log') -Pattern 'Loading assembly DLL|Watcher bridge bound|playing card|Steamworks: shutting down|RIDs of type|ObjectDB instances leaked'
  ```
- 最小修复范围: 不能把轮转备份当作当前 DLL 的 smoke 证据;需为每次运行使用独立 `$run` 目录或先归档旧日志/JSON,并在验收时以本次 `godot.log` 与本次 `form-native-smoke-final.json` 的时间戳为准。
- 尚缺的实机证据: 当前 08:13 进程为何停在 BaseLib 配置注册、是否发生 BaseLib patch 死锁或原生锁等待;需要一次带独立日志目录的复跑才能确认。

### P0 - 当前隔离游戏目录在 08:13 运行前已被换成 `51224C20` DLL,但旧 `mod_configs\Spire1.cfg` 仍是 2026-10-02 21:30:31Z,本次进程未重新生成该配置
- 证据文件:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\mods\Spire1\Spire1.dll`: length 781312, LastWriteTimeUtc `2026-10-02T23:55:18.3201032Z`, SHA256 `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\staging-current.json` 的 `stagedFiles` 中 `Spire1.dll` 仍记录 length 759296、SHA256 `2FAA004BACF361C5EAF58494E6AF504AFD0215C5D19C498DB94CA1E7301E55CA`;该 staging 文件自身 LastWriteTimeUtc 为 `2026-10-02T21:29:54Z`,对应旧运行。
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\appdata\SlayTheSpire2\mod_configs\Spire1.cfg` 的 LastWriteTimeUtc 为 `2026-10-02T21:30:31Z`;同目录 `BaseLib.cfg` 为 `2026-10-02T21:30:25Z`。
- 触发条件: 08:13 重跑复用了旧 `$run` 目录与旧 staging 清单,但隔离游戏目录中的 `Spire1.dll` 已是 07:55 新构建;游戏本次在 BaseLib 配置注册后停住,未重新写 `Spire1.cfg`。
- 权威契约: staging 清单应描述本次实际部署的 DLL;本次 `staging-current.json` 与游戏目录实际 SHA 不一致,不能作为本次部署证据。
- 当前控制流: 新 DLL 复制到隔离游戏 `mods` -> 启动游戏 -> 加载 BaseLib -> 注册 BaseLib 配置 -> 未到达 Watcher/Spire1 加载,`Spire1.cfg` 保留旧运行内容。
- 可复现命令(只读证据复核):
  ```powershell
  $game='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game'
  Get-ChildItem -LiteralPath (Join-Path $game 'mods') -Force | Select-Object Name,LastWriteTimeUtc
  Get-FileHash -LiteralPath (Join-Path $game 'mods\Spire1\Spire1.dll') -Algorithm SHA256
  Get-Item -LiteralPath 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003\appdata\SlayTheSpire2\mod_configs\Spire1.cfg' | Select-Object LastWriteTimeUtc
  ```
- 最小修复范围: 每次重跑必须重建 staging 清单并与游戏目录实际 SHA 交叉校验;不能沿用旧 `staging-current.json` 证明新 DLL 已部署。
- 尚缺的实机证据: `51224C20` DLL 在本次进程内是否被加载;当前日志尚未到达 Spire1 加载行,因此无法证明。

### P1 - R17 成功路径对照: 成功运行约 50 秒内自行退出,scenario/final JSON 与 `godot.log` 同一批次写入
- 证据文件:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\run-final.json`: `timedOut=false`, `exitCode=0`, `logDrainCompleted=true`, `cleanupCompleted=true`, `launchError=null`, `pid=22516`。
  - 同目录 `form-native-smoke-calm.json`/`wrath.json`/`divinity.json`/`final.json` 最后写入时间为 `2026-10-01T20:56:10Z`/`20:56:17Z`/`20:56:24Z`/`20:56:24Z`。
  - 同目录 `appdata\SlayTheSpire2\logs\godot.log` 共 523 行,包含第 29/55/290 行加载三个 DLL、第 355 行 `Watcher bridge bound`、第 449/474/500 行三形态卡牌、第 507 行 `Steamworks: shutting down...`、第 510-522 行资源泄漏。
- 触发条件: R17 成功运行使用同一 `--quit-after 3600` 参数,但 runner 在约 50 秒内完成并主动调用 `SceneTree.Quit(0)`。
- 权威契约: 成功路径必须由 runner 写 scenario/final JSON 后再退出;`--quit-after 3600` 不是本次成功退出的触发点。
- 当前控制流: runner 完成三场景 -> 写 scenario JSON -> 写 `pre-quit` final -> `SceneTree.Quit(0)` -> 写 `post-quit` final -> 进程退出。
- 可复现命令(只读证据复核):
  ```powershell
  $r='G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001'
  Get-Content -LiteralPath (Join-Path $r 'run-final.json')
  Select-String -LiteralPath (Join-Path $r 'appdata\SlayTheSpire2\logs\godot.log') -Pattern 'Watcher bridge bound|playing card|Steamworks: shutting down|RIDs of type'
  ```
- 最小修复范围: 对照 R17 的成功顺序排查本次为何在 BaseLib 配置注册后停滞;优先检查本次运行是否使用了独立 `$run`/日志目录以及新 DLL 的加载路径。
- 尚缺的实机证据: 本次进程停滞的调用栈;需要在独立日志目录下复跑并采集进程转储/更详细日志。

## 进行中

- 无。只读审查范围内的文件证据已核对完毕,不再继续扩展扫描。

## 未知

- 当前 08:13 进程卡在 BaseLib 配置注册后的具体调用栈,现有文件没有托管栈或原生栈。
- `Kill()` 后 10 秒仍未退出的具体阻塞层,现有文件只能证明未退出,不能区分原生 teardown、驱动/Steam 子进程或不可中断等待。
- `godot.log` 38 行之后是否还有未刷盘的日志,以及当前进程最终如何退出,现有文件无法证明。
- `51224C20` DLL 在本次进程内是否被实际加载;当前日志尚未到达 Spire1 加载行。