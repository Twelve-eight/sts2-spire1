# Partial mod launch matrix current r15 2026-10-04

## 已确认

- [P0] runner: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-partial-mod-matrix-r15-current-20261004.ps1.
- [P0] result: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r15-current-20261004\matrix-summary.json.
- [P0] staging source: G:\omp works\.tmp\spire1-release-r15-20261004-central\payload\mods\Spire1.
- [P0] current Spire1.dll hash=8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06, PCK hash=70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79.
- [P0] 9/9 场景 exitCode=0, timedOut=false, nonzeroWindowHandleObserved=false, logDrainCompleted=true, sharedConfigSha256Unchanged=true, steamSafe=true.
- [P0] m1 BaseLib plus Spire1: Spire1 initializer true, Watcher initializer false.
- [P0] m4 Spire1 without BaseLib: Loaded 0 mods, Spire1 initializer false, explicit dependency gate fail-closed.
- [P0] m6 BaseLib plus AutoAnthony plus Spire1: Spire1 initializer true, bridge state core=true, third-party=Pending, settled=false.
- [P0] m7 BaseLib plus AutoAnthony plus Watcher plus Spire1: bridge state core=true, third-party=LegacyBridge, settled=false.
- [P0] m8 with AutoAnthonyWatcher: bridge state core=true, third-party=OfficialAddon, settled=true.
- [P0] m9 without AutoAnthony but with AutoAnthonyWatcher: addon is rejected by dependency check, while BaseLib, Watcher and Spire1 still initialize.
- [P1] m6, m7 and m8 contain AutoAnthony log error Expected 65 complete v111 Colorless cards, found 76. The game process still exits 0 and Spire1 startup is unaffected. This is AutoAnthony resource-version evidence, not Spire1 failure.

## 进行中

- [P1] 本矩阵覆盖隔离 non-Steam headless loader startup and optional bridge combinations, not UI or full combat acceptance.

## 未知

- [P2] 未覆盖可见 UI,长战斗,存档重载,重连,多人同步,性能和完整平衡.

结论: 当前 r15 payload 的可选 Mod 交叉启动通过,没有发现隐藏硬前置或退出竞态.