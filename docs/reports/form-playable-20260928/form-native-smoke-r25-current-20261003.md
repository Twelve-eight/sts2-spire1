# Spire1 Forms 当前 Beta 三形态真实隔离验收 - r25 - 2026-10-03

## 结论

当前 Beta DLL `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7` 已在非 Steam 隔离游戏副本中完成三条真实 Watcher 出牌链路。Calm、Wrath、Divinity 三场景均通过;真实游戏进程正常退出;外层 harness 的 staging 哈希、scenario 新鲜度、日志排空、共享配置不变、Steam settings 恢复和测试 mods 清理全部通过。

这关闭了“当前 r2 Beta DLL 是否能在真实运行时进入并执行三形态效果”的 P0 证据缺口,不等于完整产品验收。

## 已确认

### 1. 运行身份和隔离边界

- 运行目录: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\`
- 启动脚本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r25-current-20261003.ps1`
- 实际游戏副本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\`
- 不是 Steam 路径;启动前检查了目标进程,运行使用 `--headless`,Dummy audio,隐藏窗口和隔离的 `APPDATA`、`LOCALAPPDATA`、`TEMP`、`TMP`、`GseSavePath`.
- 测试挂载的 mod 为 `BaseLib`、`Watcher`、`Spire1`.
- staging 文件清单: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\staging-current.json`.
- staging 中实际部署的 `Spire1.dll` 长度 `781312`,SHA256 为 `51224C20...`,与 Beta r2 包完全一致。

### 2. 外层 harness 结果

`run-final.json`:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\run-final.json`

- 启动时间: `2026-10-03T08:56:11.6518119+08:00`.
- 结束时间: `2026-10-03T08:56:52.9697780+08:00`.
- `exitCode`: `0`.
- `timedOut`: `false`.
- `nonzeroWindowHandleObserved`: `false`.
- `logDrainCompleted`: `true`.
- `scenarioFresh`: `true`.
- `sharedConfigSha256Unchanged`: `true`.
- `steamSettingsRestored`: `true`.
- `cleanupCompleted`: `true`.
- `launchError`: `null`.

运行结束后目标 `game\mods` 不存在;目标 `data_sts2_windows_x86_64\steam_settings` 只恢复了原有顶层文件,没有残留嵌套 `steam_settings` 目录。

### 3. Calm 真实出牌链路

场景 JSON:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\form-native-smoke-calm.json`

- `status`: `passed`.
- 真实入口卡: `WATCHER_VIGILANCE`.
- 原生姿态: `Calm`.
- 形态 carrier: `Spire1.Spire1Code.Forms.VoidSerpentStancePower`.
- 形态效果: `VoidFormEffectPower`、`SerpentFormPower`.
- 后续真实手动 `WATCHER_STRIKE_P` 两次均完成;第一次免费,第二次支付一次能量。
- 两次观察到的总伤害均为 `9`.
- `unobservedFaults=[]`.

### 4. Wrath 真实出牌链路

场景 JSON:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\form-native-smoke-wrath.json`

- `status`: `passed`.
- 真实入口卡: `WATCHER_ERUPTION_P`.
- 原生姿态: `Wrath`.
- 形态 carrier: `DemonReaperStancePower`.
- 形态效果: `DemonFormPower`、`ReaperFormEffectPower`.
- 后续真实手动 `WATCHER_STRIKE_P` 完成;目标掉血 `7`,能量 `1 -> 0`.
- `StrengthPower=1`.
- 目标观察到 `DoomPower=7`,证据字段为 `doomEvidence=observed-exact`.
- `unobservedFaults=[]`.

### 5. Divinity 真实出牌链路

场景 JSON:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\form-native-smoke-divinity.json`

- `status`: `passed`.
- 真实入口卡: `WATCHER_BLASPHEMY`.
- 原生姿态: `Divinity`.
- 形态 carrier: `EchoCelestialStancePower`.
- 形态效果: `EchoFormEffectPower`、`CelestialFormPower`.
- 后续真实手动 `WATCHER_STRIKE_P` 的目标总伤害 `12`.
- Echo 完成历史增量 `2`;两次 play count 均为 `2`;额外打出证据为 `true`.
- `unobservedFaults=[]`.

### 6. 日志和退出

日志:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\appdata\SlayTheSpire2\logs\godot.log`

已确认日志包含:

- BaseLib 实际加载。
- Watcher 实际加载。
- 当前 `Spire1.dll` 被加载。
- `Watcher bridge bound`.
- 三张真实 Watcher 入口卡的出牌记录。
- `Steamworks shutdown succeeded`.

final JSON:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-current-20261003\form-native-smoke-final.json`

- `status=completed`.
- `exitCode=0`.
- `quitStatus=executed-main-thread`.
- `quitDrainSettled=true`.
- `quitDrainOutcome=settled`.

### 7. r24 harness 清理修正

r24 的游戏证据本身已经包含三形态 JSON,但外层收尾因把整目录复制到 `pre-steam-settings` 而出现嵌套目录,导致清理验证失败。随后已在目标绝对路径经过检查、确认无运行进程后:

- 从 r24 的 `pre-steam-settings\steam_settings\` 逐文件恢复顶层 Steam settings。
- 将 stale 嵌套目录移到隔离的清理留存目录:
  `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r24-cleanup-quarantine-20261003\steam_settings-nested`
- 确认顶层文件 SHA256 与 r24 备份一致。
- 确认共享 `mod_configs` 的 r24 before/after snapshot 相同。

这一步没有修改 Steam 安装,没有修改共享配置。

### 8. r25 首次 runner 失败与重跑

第一次误用 `powershell.exe` 启动时,脚本在 `ProcessStartInfo.ArgumentList` 初始化处停止,游戏尚未启动。临时挂载的测试 mods 和三份临时 Steam settings 被安全移入:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r25-abort-quarantine-20261003\`

随后恢复原 settings,移除临时挂载,再使用支持 `ArgumentList` 的 `pwsh.exe` 重跑;上面的 r25 通过证据只指第二次真实运行。

## 进行中

- 朋友可见 UI 安装验证仍未由本机 headless 证据替代。
- 可选 Mod 的当前 r2 字节交叉矩阵尚未重新跑完整 m1/m6/m7/m8/m9;已有 r30 矩阵使用的是旧 DLL 字节,不能外推到本次 r2。

## 未知

- 可见 UI、图像和动画的实际呈现。
- 长战斗、完整回合边界、战中存档/读档、重连、多人同步、性能和完整平衡。
- ProcessExit 与 in-flight Apply 的并发注入压力测试。
- AutoAnthony 自身的 `Expected 65 complete v111 Colorless cards, found 76` 版本自检噪声是否在朋友环境出现;它不是 Spire1 manifest 依赖。

## 运行证据边界

本报告可证明“当前 Beta r2 DLL 在隔离真实运行时能通过三条姿态形态出牌链路”。本报告不能证明完整发布质量、可见 UI、多人或长时稳定性。