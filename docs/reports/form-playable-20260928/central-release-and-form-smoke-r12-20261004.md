# Spire1 r12 中央发布验证报告

- 验证日期: 2026-10-04 Asia/Shanghai
- 源码树: G:\omp works\Sts\sts2-spire1
- Release 输出: G:\omp works\.tmp\spire1-release-r12-20261004-central

## 构建与门禁

- Build-Spire1Release.ps1 Release 构建: 成功, 0 errors, 64 warnings.
- PCK 结构门禁: PASS, entries=1464, sourceFiles=744.
- AssemblyRef, manifest consistency, TypeDef: 全部 PASS.

### 当前 payload

- Spire1.dll: 900608 bytes, SHA256 E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB
- Spire1.json: 548 bytes, SHA256 CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305
- Spire1.pck: 19669354 bytes, SHA256 70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79

## 隔离真实游戏 smoke

- 执行脚本: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\run-native-form-smoke-r12-current-20261004-rerun.ps1
- 游戏副本: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game\SlayTheSpire2.exe
- exitCode=0, timedOut=False, nonzeroWindowHandleObserved=False, logDrainCompleted=True.
- scenarioFresh=True, sharedConfigSha256Unchanged=True, steamSettingsRestored=True, cleanupCompleted=True.

- calm: status=passed, effect=passed, failure=, damage=, energy=->, echoExtraPlay=.
- divinity: status=passed, effect=passed, failure=, damage=12, energy=5->4, echoExtraPlay=True.
- wrath: status=passed, effect=passed, failure=, damage=7, energy=1->0, echoExtraPlay=.

## 未验证边界

- 本次 smoke 覆盖当前 r12 Release DLL/PCK 的三条形态场景, 不等于完整新局, 旧存档, 关闭开关, 多人和所有 AutoAnthony 晚加载矩阵均已验证.
- VoidForm 探针另有独立中央运行日志, 本报告不把探针替代真实游戏.
