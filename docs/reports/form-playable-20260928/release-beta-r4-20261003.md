# Spire1 姿态形态 Beta r4 发布与验收报告 - 2026-10-03

## 结论

已生成朋友测试包:

- G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r4.zip
- ZIP 长度: 19382956 bytes
- ZIP SHA256: 31D77F8E3B70456D5B14359444CA97E0B31F5EF0B3682C0174756AEBF3C40B8B
- 校验 sidecar: G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r4.zip.sha256

该包是可供朋友测试的 Beta 副本,不是完整产品验收。源码发布身份必须按 HEAD + 工作树增量理解,不能只看 HEAD:

- 隔离源码树: G:\omp works\.tmp\spire1-beta-r4-clean
- HEAD: a6e46e53ae891e4faa7b640a64c1000a9e566c9a
- 工作树: dirty;存在未提交的源码和报告文件
- 该事实已写入 G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\evidence\release-manifest-final.json

## 源码证据

本次 Release 产物来自隔离源码树,核心增量包括:

- mod/Spire1Code/Cards/Spire1Card.cs
- mod/Spire1Code/Cards/Spire1DeckGrantGuardModifier.cs
- mod/Spire1Code/Patches/Sts1EventToggleFilterPatch.cs
- mod/Spire1Code/Run/FormNativeSmokeRunner.cs

新的 deck-grant listener 使用 owner-free mutable ModifierModel 延迟解析 canonical representative,避免把 canonical CardModel 当作 run-state listener 后触发 CanonicalModelException。三项发布门禁要求 DLL 不硬引用 Watcher、AutoAnthony、AutoAnthonyWatcher、DirectConnectIP、ActsFromThePast;本次门禁通过,manifest 只声明 BaseLib.

## 中央构建与结构门禁

- Release DLL: 883CD438435A651DF8BF0A1DE5D9C28467E5A7CF8B14B561015C436E70FFF519 (846336 bytes)
- PCK: 3AA0BD39173C491301BCF42F3FB5ADA25A4762794F0C86B119239824A8CFC545 (19666890 bytes)
- manifest: CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305 (548 bytes)
- PCK verifier: 1464 entries, 744 source files, 720 CTEX, 720 import, 24 JSON, MD5 verified
- Release gates: PASS; AssemblyRef 15, TypeDef 994, manifest consistency PASS, forbidden AssemblyRef PASS, forbidden TypeDef PASS
- 证据:
  - G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\evidence\release-manifest-final.json
  - G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\evidence\asset-manifest.json
  - G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\evidence\pck-structure.json
  - G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\evidence\release-gates.json

## 最终 payload 实机证据

运行目录: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-release-payload-r2-20261003

- 运行窗口: 10/03/2026 12:22:50 至 10/03/2026 12:23:32
- exitCode: 0
- timedOut: False
- logDrainCompleted: True
- scenarioFresh: True
- sharedConfigSha256Unchanged: True
- steamSettingsRestored: True
- cleanupCompleted: True
- 本次 staging 的 Spire1.dll SHA256: 883CD438435A651DF8BF0A1DE5D9C28467E5A7CF8B14B561015C436E70FFF519

三场景单场景 smoke 均 status=passed、formGateAfter.passed=true、effectVerification.passed=true、unobservedFaults=[]:

- Calm: WATCHER_VIGILANCE -> VoidSerpentStancePower;两次 WATCHER_STRIKE_P 分别免费和支付 1 能量,各造成 9 点总伤害。
- Wrath: WATCHER_ERUPTION_P -> DemonReaperStancePower;StrengthPower=1,DoomPower=7,目标伤害 7,能量 1 -> 0。
- Divinity: WATCHER_BLASPHEMY -> EchoCelestialStancePower;目标总伤害 12,echoHistoryFinishedDelta=2,两次 play count 均为 2。

日志确认 BaseLib、Watcher、Spire1 加载,Watcher bridge bound,三张入口卡和后续 Strike 出牌,以及 Steamworks: shutting down;本次日志没有 CanonicalModelException。

## 长回合证据边界

长回合 turns smoke 已在 listener-fix 测试批次完成: Wrath、Calm 通过;Divinity 进入形态并观察到死亡后的形态清理,随后因 EndTurnDeathPower 正常杀死玩家而进入 combat-ended,所以整体必须记为 partial/blocked,不能宣称完整长回合通过。该场景结果用于说明当前代码的诚实阻塞边界,不外推为最终精简 PCK 已完成完整长回合验收。

## 包内容与依赖

压缩包内严格只有:

- mods/Spire1/Spire1.dll
- mods/Spire1/Spire1.pck
- mods/Spire1/Spire1.json
- README-安装说明.txt

未包含 PDB、deps.json、pck.sha256、日志、config 或 Steam 文件。Spire1 版本 1.2.3,目标游戏 0.111.0,BaseLib 最低 3.4.5;Watcher、AutoAnthony、AutoAnthonyWatcher 均为可选运行时桥接,不是 manifest 硬前置。

## 未验证边界

可见 UI、视觉资源与动画、多人与同步、战中存档读档、重连、性能、完整平衡、完整长战斗以及朋友机器上的安装结果仍未验证。Headless 环境的 crashpad、Dummy renderer、Godot RID/resource leak 只作为环境噪声记录。

## 复现与恢复入口

- 最终发布清单: G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\evidence\release-manifest-final.json
- 最终 payload: G:\omp works\.tmp\spire1-beta-r4-release-evidence-20261003\payload\mods\Spire1
- 最终 payload 三场景实机证据: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-release-payload-r2-20261003
- 构建脚本: G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1
## 可选 Mod 交叉启动证据

为核对“可选运行时桥接不会变成 Spire1 的隐藏硬前置”,已用最终 Beta DLL/PCK 在同一隔离非 Steam 副本运行 9 组启动组合:

- 运行目录: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\partial-mod-matrix-r33-final-beta-r2-20261003
- 9/9 进程 exitCode=0,无超时,无窗口句柄,日志排空,共享 mod_configs 哈希不变,无嵌套 manifest,测试 mods 清理完成。
- BaseLib + Spire1: Spire1 initializer 成功,Watcher 缺失时 Forms bridge 按预期 fail-closed。
- BaseLib + AutoAnthony + Spire1: Spire1 initializer 成功,Watcher 仍可缺失。
- BaseLib + AutoAnthony + Watcher + Spire1: Spire1 与 Watcher initializer 成功,无硬引用失败。
- 完整挂载 AutoAnthonyWatcher: 5 个 mod manifest 中 5 个加载,Spire1 initializer 成功。
- 缺少 AutoAnthony 但挂载 AutoAnthonyWatcher: addon 自身被 ModLoader 拒绝,但 BaseLib、Watcher、Spire1 仍加载,Spire1 initializer 成功。

该矩阵只证明 loader/依赖边界,不替代 UI、存档、多人或形态战斗验收。详细 JSON 已并入最终发布清单的 OptionalBridgeCrossLaunch 字段。

## 源码备份

发布构建完成后,已将四个核心代码文件备份到 Git 分支并推送:

- 分支: codex/form-beta-r4-20261003
- commit: 0682f94864ce02e6296d9f4593c09dcc5782f0a7
- 远端: origin/codex/form-beta-r4-20261003

最终发布清单保留了构建时的 Source.Head=a6e46e53ae891e4faa7b640a64c1000a9e566c9a,并在 Source.BackupAfterBuild 记录了该备份提交;这样不会把构建前的 HEAD 错写成构建完整身份,也不会把未纳入备份的代理请求报告误认为产品源码。
