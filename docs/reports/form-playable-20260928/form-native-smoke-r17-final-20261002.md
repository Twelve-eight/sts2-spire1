# 姿态形态 P0 最终真实冒烟记录

- 日期: 2026-10-02
- 范围: 当前最终源码、Release 构建产物、隔离真实游戏进程中的三种 Watcher 姿态卡链路。
- 用户边界: 前台用户正在玩 CS；本轮没有激活窗口、键鼠操作或可见游戏窗口。
- 测试安装: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-isolated-20260930\game`。
- 禁止写入的 Steam 安装: `G:\steam\steamapps\common\Slay the Spire 2\`。

## 已确认

### 1. 当前源码静态审查

- 实现者报告: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\remaining-smoke-contract-fix-worker-20261001.md`。
- 独立最终审查: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\generic-nested-task-fix-final-review-20261001.md`。
- 监督审查: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\generic-nested-task-fix-supervisor-20261001.md`。
- 两份最终审查均为 `PASS（仅限静态审查）`，并明确未把静态结果扩展为构建或实机通过。
- 审查覆盖 `Task<Task>`、`Task<Task<T>>`、主线程 gate、总 deadline、frame terminal failure、pre-quit evidence、cleanup gate、failure latch 和禁止同步阻塞旁路。
- 本批审查实际使用用户允许的 `6.1sol` / `agentrouter` 路由；没有使用其它模型作为本批交付证据。

### 2. Release 构建

中央构建命令及日志:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-build-generic-fix-20261001.log`

结果: `58 warnings, 0 errors`。

最终构建产物哈希:

- `Spire1.dll`: `7418C03435ECF22A4669037D4684E5BFD05665DC2BAF517DE8B2EF56255AE000`
- `Spire1.pck`: `C8F718AB73F3C054FC3434F1CE0277E6C7E7E76DBF96A23104CF940231EC63BD`
- `Spire1.pdb`: `425384C464B8027FABC21B3442853F155F73CA5C9C0FAF0BC65A9FB8BBA8ED59`

构建输出和测试副本 B 中的 `Spire1.dll`、`Spire1.pck`、`Spire1.pdb` 哈希一致。

### 3. 正确 staging 和隔离

- staging 证据: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\staging-latest.json`。
- 隔离副本实际加载三个完整顶层 mod 目录: `BaseLib`、`Watcher`、`Spire1`。
- 每个目录均有顶层 manifest；没有 `BaseLib\BaseLib\BaseLib.json` 或 `Watcher\Watcher\Watcher.json` 这类嵌套 manifest；`Spire1.json` 存在。
- 旧运行的 `Loaded 2 mods (4 total)` 证据与旧场景 JSON 不再采信，本次运行使用新 staging。

### 4. 真实隔离游戏三形态链路

运行脚本:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r17-ftue-20261001\run-native-form-smoke.ps1`

本次真实运行时间:

- 开始: `2026-10-02T04:55:35.0674498+08:00`
- 结束: `2026-10-02T04:56:25.1038242+08:00`
- 进程退出码: `0`
- `timedOut=false`
- 49 次窗口句柄采样均为零
- stdout/stderr 均已排空
- `sharedConfigSha256Unchanged=true`
- `cleanupCompleted=true`
- 运行后隔离 `mods` 目录已清理，未发现残留 Slay the Spire 2 进程

stdout 确认:

- `Loaded 3 mods (3 total)`
- `Spire1 Forms: Watcher bridge bound; custom-run modifier available`
- `Player 1 playing card WATCHER_VIGILANCE`
- `Player 1 playing card WATCHER_ERUPTION_P`
- `Player 1 playing card WATCHER_BLASPHEMY`

三份场景证据均为本次运行的新文件，写入时间分别约为本地 `04:56:10`、`04:56:17`、`04:56:24`:

- `form-native-smoke-calm.json`: `status=passed`，`cardPlay.status=completed`，原生姿态 `Calm`，carrier 为 `VoidSerpentStancePower`，effects 为 `VoidFormEffectPower` 和 `SerpentFormPower`，`unobservedFaults=[]`。
- `form-native-smoke-wrath.json`: `status=passed`，`cardPlay.status=completed`，原生姿态 `Wrath`，carrier 为 `DemonReaperStancePower`，effects 为 `DemonFormPower` 和 `ReaperFormEffectPower`，`unobservedFaults=[]`。
- `form-native-smoke-divinity.json`: `status=passed`，`cardPlay.status=completed`，原生姿态 `Divinity`，carrier 为 `EchoCelestialStancePower`，effects 为 `EchoFormEffectPower` 和 `CelestialFormPower`，`unobservedFaults=[]`。

每份场景的 `formGateAfter.passed=true`、`cardPlay.exception=null`、`cardPlay.failure=null`、`cardPlay.failureLatched=false`。`formGateBefore.passed=false` 是出牌前无姿态基线，不是场景失败。

最终证据:

- `form-native-smoke-final.json`: `status=completed`、`exitCode=0`、`quitStatus=executed-main-thread`、`quitDrainSettled=true`、`quitDrainOutcome=settled`、`finalEvidencePhase=post-quit`。
- `run-final.json`: `exitCode=0`、`sharedConfigSha256Unchanged=true`、`cleanupCompleted=true`。

## 已知噪声和未扩大的结论

stderr 仍有测试环境噪声，不能写成“零错误”:

- Sentry crashpad 缺失。
- Dummy renderer/Godot 退出时 RID、ObjectDB 和资源泄漏提示。
- headless 资源缓存提示，包括姿态 power 图标；源码和 PCK 中对应图片文件存在。

本次通过证明的是：在真实隔离游戏主线程中，Watcher 三张真实姿态卡可以完成出牌，并生成对应的原生姿态 carrier 与两项形态 effect。它不扩大为以下结论:

- 未验证可见窗口中的图标、tooltip、动画和布局。
- 未验证整场战斗中的每回合数值、力量账本、重复出牌、自动出牌、伤害来源和 Reaper/Doom 交互。
- 未验证存档、重连、多人同步和长时间战斗。
- 未修改或验证 Steam 安装；未修改共享 `mod_configs`。

## 进行中

- 本次 P0 三形态真实出牌闭环无待处理的构建或 staging 动作。
- 后续若继续提升到完整可玩验收，应按上面的未验证边界补做可见 UI、长战斗、存档和多人测试，而不是把本记录的 headless smoke 当成全部验收。

## 未知

- 真实玩家在可见客户端中观察到的姿态图标和 tooltip 外观。
- 完整平衡行为与所有边界交互。
- 存档重载、重连和多人同步语义。

### 效果事务审查边界

- 旧 `effects-review.md` 中的 F1/F3 结论来自较早快照，不能直接套到当前源码；当前源码已经有 `SpendResources` 返回 Task 的包装、逐卡 token 和 `PowerModel._amount` 写入后捕获。
- 本轮新增的两个独立静态审查代理都只落盘到中途：已确认当前支付期卡身份守卫和真实 Task 包装的首条事实，但没有形成最终 PASS/REWORK。它们的未完成报告不作为通过证据。
- 因此本记录关闭的是 P0 的真实三张姿态卡出牌和形态 carrier/effect 生成，不关闭完整效果事务边界。支付失败后的 pending 清理、复杂嵌套重入、力量事务故障注入、完整数值平衡仍保留为后续审查项。
