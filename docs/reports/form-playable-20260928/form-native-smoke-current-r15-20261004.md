# Form native smoke current r15 2026-10-04

## 已确认

- [P0] runner: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r15-current-20261004.ps1.
- [P0] result: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r15-current-20261004\run-final.json.
- [P0] 真实隔离 non-Steam headless game run: exitCode=0, timedOut=false, nonzeroWindowHandleObserved=false, logDrainCompleted=true, scenarioFresh=true.
- [P0] sharedConfigSha256Unchanged=true, steamSettingsRestored=true, cleanupCompleted=true, launchError=null.
- [P0] staging bytes: DLL sha256=8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06, PCK sha256=70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79.
- [P0] Calm: WATCHER_VIGILANCE selected native Calm, carrier=VoidSerpentStancePower, VoidFormEffectPower and SerpentFormPower present. Two WATCHER_STRIKE_P plays completed, each total damage 9, first manual play free and second paid once.
- [P0] Wrath: WATCHER_ERUPTION_P selected native Wrath, carrier=DemonReaperStancePower, DemonFormPower and ReaperFormEffectPower present. WATCHER_STRIKE_P dealt 7, StrengthPower=1, DoomPower=7, energy 1 -> 0.
- [P0] Divinity: WATCHER_BLASPHEMY selected native Divinity, carrier=EchoCelestialStancePower, EchoFormEffectPower and CelestialFormPower present. WATCHER_STRIKE_P total damage 12, echo play counts=[2,2], energy 5 -> 4.
- [P0] 三场景 status=passed, formGateAfter.passed=true, effectVerification.passed=true, unobservedFaults=[]; final quit drain settled.

## 进行中

- [P1] 证据是隔离 headless 真实游戏运行,不是用户 Steam 安装的 UI 验收.

## 未知

- [P2] 未验证可见 UI,视觉资源,完整长战斗,战中存档重载,重连,多人同步,性能和完整平衡.
- [P2] Divinity EndTurnDeathPower 的下一回合终局边界未被本 smoke 宣称为长战斗通过.

结论: 当前 r15 payload 的三形态原生首回合和效果闭环通过.