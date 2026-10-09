# Spire1 r8e 当前发布字节三形态隔离烟测 - 2026-10-04

## 已确认

- 运行脚本: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r8e-current-20261004.ps1`.
- 隔离游戏: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\SlayTheSpire2.exe`.
- payload: `G:\omp works\.tmp\spire1-release-r8e-20261004-central\payload\mods\Spire1`.
- 启动参数: `--headless --audio-driver Dummy --disable-crash-handler --no-header --max-fps 30 --quit-after 3600 --form-native-smoke`.
- 运行窗口: `2026-10-04 08:44:42 +08:00` 至 `2026-10-04 08:45:25 +08:00`.
- 外层证据: `run-final.json` 的 `exitCode=0`, `timedOut=false`, `nonzeroWindowHandleObserved=false`, `logDrainCompleted=true`, `scenarioFresh=true`, `cleanupCompleted=true`.
- 字节绑定: Spire1.dll SHA256 `267B659C593C0B3AA7C9A875262060C268770755BD7B45669F1EAB174D1DAA41`; Spire1.pck SHA256 `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79`.
- 环境安全: `sharedConfigSha256Unchanged=true`; `steamSettingsRestored=true`; 测试 `mods` 已清理; 未写 Steam 安装目录.

### Calm

- `form-native-smoke-calm.json`: `status=passed`.
- 形态桥接: `VoidSerpentStancePower`, `VoidFormEffectPower`, `SerpentFormPower`; native stance 为 Calm.
- `WATCHER_STRIKE_P` 两次完成; 第一次免费, 第二次支付一次能量; 每次敌方生命损失 9; `unobservedFaults=[]`.

### Wrath

- `form-native-smoke-wrath.json`: `status=passed`.
- 形态桥接: `DemonReaperStancePower`, `DemonFormPower`, `ReaperFormEffectPower`; native stance 为 Wrath.
- `WATCHER_ERUPTION_P` 后 `WATCHER_STRIKE_P` 完成; 敌方受伤 7, `StrengthPower=1`, `DoomPower=7`, 能量 `1 -> 0`; `unobservedFaults=[]`.

### Divinity

- `form-native-smoke-divinity.json`: `status=passed`.
- 形态桥接: `EchoCelestialStancePower`, `EchoFormEffectPower`, `CelestialFormPower`; native stance 为 Divinity.
- `WATCHER_BLASPHEMY` 后 `WATCHER_STRIKE_P` 总伤害 12; Echo 完成额外出牌, 两次 play count 都为 2; `unobservedFaults=[]`.

## 未知

- 这是当前 r8e 发布字节的真实隔离 headless smoke,不覆盖可见 UI/视觉资源/完整长回合/存档读写/重连/多人/性能/完整平衡.
- 不覆盖内容快照的 FromSerializable 异常,CanonicalizeSave 交错,CreateForTest 或 NTopBar 隔离故障注入.
- stderr 中 crashpad,Dummy renderer 和 Godot 资源回收噪声未作为业务失败;本报告不把它们隐藏为零噪声.

CENTRAL_SMOKE_PASS
