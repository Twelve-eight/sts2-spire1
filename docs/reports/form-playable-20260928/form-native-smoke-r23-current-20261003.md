
# 姿态形态 native smoke r23 实机报告 - 2026-10-03

## 范围

本报告只记录真实游戏进程在完全隔离环境中的 Watcher 原生卡路径。它证明三条入场和效果路径可运行,不外推为可见 UI、长战斗、存档、多人或视觉验收。

运行目录:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r23-current-20261003`

游戏目录:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game`

staging 只挂载 `BaseLib`、`Watcher`、`Spire1`,使用隔离 `APPDATA`、`LOCALAPPDATA`、`TEMP`、`GseSavePath`,退出后清理 staging。Steam 安装和共享 `mod_configs` 未写入。

## 构建与结构门禁

中央构建命令:

```powershell
dotnet build `
  'G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj' `
  -c Release `
  -p:Sts2Path='E:\Slay the Spire 2' `
  -p:CopyToModsFolderOnBuild=false
```

证据:

- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-build-current-20261003-r23.log`
- 构建结果: 0 errors, 61 warnings.
- 最新 DLL: `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`.
- 依赖门禁结果: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json`.
- `assemblyref-forbidden`: PASS, AssemblyRef 15 个, 禁止的 5 个 mod 程序集均未出现.
- `manifest-consistency`: PASS, 二进制 mod AssemblyRef 只有 `BaseLib`, manifest 也只声明 `BaseLib`.
- `typedef-forbidden`: PASS, TypeDef 964 个, 形态/实验/held-back/Debug 禁止类型均未进入发布 DLL.

## r22 失败与 r23 修正边界

r20/r21 的 Wrath 失败属于驱动目标选择错误: `WATCHER_ERUPTION_P` 的真实运行时 `TargetType` 是 `Any`,旧驱动没有提供敌方目标。r22 已修正目标传入,但确定性 `CUBEX_CONSTRUCT_NORMAL` 唯一敌人带 `ArtifactPower`,因此 `ReaperFormEffectPower` 的 Doom 被原生 Artifact 抵消,观测到 `targetDoom=0`。

r23 只在隔离 smoke 的 Wrath 目标上移除该 `ArtifactPower`,再执行真实卡牌和形态效果链。该修改位于 `FormNativeSmokeRunner.cs` 测试夹具,不改变生产卡牌或形态逻辑,也不写入测试副本的持久配置。

## r23 真实场景结果

三个场景均满足 `status=passed`, `cardPlay.status=completed`, `cardPlay.state=Finished`, `formGateAfter.passed=true`, `effectVerification.passed=true`, `unobservedFaults=[]`。

| 场景 | 入场卡 | 形态 carrier / effects | 关键效果证据 |
|---|---|---|---|
| Calm | `WATCHER_VIGILANCE` | `VoidSerpentStancePower` / `VoidFormEffectPower` + `SerpentFormPower` | 后续 `WATCHER_STRIKE_P` 第一次免费,第二次支付一次能量,两次各造成 9 点总伤害 |
| Wrath | `WATCHER_ERUPTION_P` | `DemonReaperStancePower` / `DemonFormPower` + `ReaperFormEffectPower` | `WATCHER_STRIKE_P` 目标伤害 7, `StrengthPower=1`, `DoomPower=7`, 能量从 1 降至 0 |
| Divinity | `WATCHER_BLASPHEMY` | `EchoCelestialStancePower` / `EchoFormEffectPower` + `CelestialFormPower` | `WATCHER_STRIKE_P` 总目标伤害 12, `echoHistoryFinishedDelta=2`, 两次 play count 均为 2,能量从 5 降至 4 |

`form-native-smoke-final.json` 的最终状态为 `status=completed`, `exitCode=0`, `quitStatus=executed-main-thread`, `quitDrainSettled=true`, `quitDrainOutcome=settled`。`run-final.json` 为 `exitCode=0`, `timedOut=false`, `nonzeroWindowHandleObserved=false`, `sharedConfigSha256Unchanged=true`, `cleanupCompleted=true`。

## 运行日志边界

stdout 确认真实加载 `BaseLib`、`Watcher`、`Spire1`,并出现 `Watcher bridge bound`。三个入场动作和后续 `WATCHER_STRIKE_P` 均为目标明确的真实 `PlayCardAction`。

stderr 只有已知 headless 环境噪声: Sentry crashpad 缺失、Dummy renderer 的空纹理初始化、Godot RID/资源泄漏和 ObjectDB 清理提示。没有把这些噪声写成形态逻辑失败,也没有把它们隐藏为“无错误”。

尚未验证: 可见 UI 和资源呈现、长战斗中所有回合边界、战中存档重载、断线重连、多人同步、性能和完整平衡。
