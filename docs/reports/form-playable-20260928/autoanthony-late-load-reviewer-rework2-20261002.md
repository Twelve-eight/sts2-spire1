# AutoAnthony late-load 第二轮监督审查 - 2026-10-02

## 已确认

### 审查前置条件

- 已通过 Codex 原生 wait_threads 等待 implementation worker `01a0fdc1-38ca-7532-896e-0db2060ff665` 完成最终回报；其状态已变为 idle/completed。
- worker 最终回报确认本轮只修改两个 Interop 源文件和其实现报告，并明确未构建、未测试、未部署、未启动游戏。
- 本监督报告是唯一审查落盘路径；本轮不修改产品代码。

### 第一批增量证据（先于本轮深入审查落盘）

- 审查时工作树快照：M  DEVELOP.md | M  DEVLOG.md | M  NuGet.config |  D astra-advice.md |  M dist/REBUILD-PENDING.md | M  docs/BaseLib-API.md | M  mod/Spire1.csproj | M  mod/Spire1.json | M  mod/Spire1/localization/eng/settings_ui.json | M  mod/Spire1/localization/zhs/settings_ui.json | M  mod/Spire1Code/Cards/Necronomicurse.cs | M  mod/Spire1Code/Cards/Spire1Card.cs | M  mod/Spire1Code/Cards/Spire1Curse.cs | M  mod/Spire1Code/Character/Defect.cs | M  mod/Spire1Code/Character/DefectCardPool.cs | M  mod/Spire1Code/Character/DefectPotionPool.cs | M  mod/Spire1Code/Character/DefectRelicPool.cs | M  mod/Spire1Code/Character/Ironclad.cs | M  mod/Spire1Code/Character/SharedCardReuse.cs | M  mod/Spire1Code/Character/Silent.cs | M  mod/Spire1Code/Character/SilentCardPool.cs | M  mod/Spire1Code/Character/SilentPotionPool.cs | M  mod/Spire1Code/Character/SilentRelicPool.cs | M  mod/Spire1Code/Character/Spire1CardPool.cs | M  mod/Spire1Code/Character/Spire1PotionPool.cs | M  mod/Spire1Code/Character/Spire1RelicPool.cs | M  mod/Spire1Code/Config/CharacterGate.cs | M  mod/Spire1Code/Config/Spire1Config.cs | M  mod/Spire1Code/Events/Addict.cs | M  mod/Spire1Code/Events/DrugDealer.cs | M  mod/Spire1Code/Events/SpireHeart.cs | A  mod/Spire1Code/Experimental/MenuPerformance/CharacterSelectBackgroundPause.cs | A  mod/Spire1Code/Experimental/MenuPerformance/ViolaPortraitLogCompat.cs | M  mod/Spire1Code/Extensions/StanceCmd.cs | M  mod/Spire1Code/Forms/DemonFormPower.cs | MM mod/Spire1Code/Forms/VoidFormEffectPower.cs | MM mod/Spire1Code/Forms/VoidFormPlayTransactionPatch.cs | MM mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs | MM mod/Spire1Code/Interop/AutoAnthonyLoadHook.cs | M  mod/Spire1Code/MainFile.cs | M  mod/Spire1Code/Patches/DebugCardInjectPatch.cs | M  mod/Spire1Code/Patches/DebugRelicInjectPatch.cs | A  mod/Spire1Code/Patches/Spire1ContentSnapshotPatch.cs | M  mod/Spire1Code/Patches/Spire1LargeCapsuleGatePatch.cs | A  mod/Spire1Code/Patches/Spire1PowersGatePatch.cs | M  mod/Spire1Code/Patches/Spire1SharedPoolGatePatch.cs | M  mod/Spire1Code/Patches/Sts1EventToggleFilterPatch.cs | M  mod/Spire1Code/Potions/Spire1Potion.cs | M  mod/Spire1Code/Powers/AngryPower.cs | M  mod/Spire1Code/Powers/BattleHymnPower.cs | M  mod/Spire1Code/Powers/BiasedCognitionPower.cs | M  mod/Spire1Code/Powers/CollectPower.cs | M  mod/Spire1Code/Powers/CuriosityPower.cs | M  mod/Spire1Code/Powers/DisciplinePower.cs | M  mod/Spire1Code/Powers/EnvenomPower.cs | M  mod/Spire1Code/Powers/PhantasmalKillerPower.cs | M  mod/Spire1Code/Powers/ShiftingPower.cs | M  mod/Spire1Code/Powers/SporeCloudPower.cs | M  mod/Spire1Code/Powers/StudyPower.cs | M  mod/Spire1Code/Powers/TimeWarpPower.cs | M  mod/Spire1Code/Powers/WaveOfTheHandPower.cs | M  mod/Spire1Code/Powers/WraithFormPower.cs | M  mod/Spire1Code/Relics/BurningBlood.cs | M  mod/Spire1Code/Relics/CrackedCore.cs | M  mod/Spire1Code/Relics/Girya.cs | M  mod/Spire1Code/Relics/Kunai.cs | M  mod/Spire1Code/Relics/MutagenicStrength.cs | M  mod/Spire1Code/Relics/Necronomicon.cs | M  mod/Spire1Code/Relics/RingOfTheSnake.cs | M  mod/Spire1Code/Relics/Shuriken.cs | M  mod/Spire1Code/Relics/Spire1Relic.cs | M  mod/Spire1Code/Run/FormNativeSmokeRunner.cs | A  mod/Spire1Code/Run/Spire1ContentSnapshotModifier.cs | A  mod/Spire1Code/Run/Spire1RunContent.cs | M  mod/Sts2PathDiscovery.props |  m research/BaseLib-StS2 |  m research/ModTemplate-StS2 |  M research/templates/content/content/CharacterModTemplate/CharMod.csproj |  M research/templates/content/content/CharacterModTemplate/Sts2PathDiscovery.props |  M research/templates/content/content/ContentModTemplate/ContentMod.csproj |  M research/templates/content/content/ContentModTemplate/Sts2PathDiscovery.props |  M research/templates/content/content/ModTemplate/ModTemplate.csproj |  M research/templates/content/content/ModTemplate/Sts2PathDiscovery.props | M  tools/build-gates/gate-config.json | M  tools/event-removal-probe/RemovalProbe.csproj | M  tools/form-effects-probe/TransactionScenarios.cs | M  tools/stS1-event-cards.js | M  tools/stS1-event-pool-usage.js | M  tools/stS1-monster-scan.js |  M workshop/content/Spire1/Spire1.dll |  M workshop/content/Spire1/Spire1.json |  M workshop/content/Spire1/Spire1.pck |  M workshop/content/Spire1/Spire1.pdb | M  workshop/workshop-push.ps1 | M  workshop/workshop_upload.vdf | ?? docs/DEVELOP-menu-performance-20260923.md | ?? docs/reports/form-playable-20260928/autoanthony-late-load-reviewer-20261002.md | ?? docs/reports/form-playable-20260928/autoanthony-late-load-reviewer-request-20261002.md | ?? docs/reports/form-playable-20260928/autoanthony-late-load-reviewer-rework2-20261002.md | ?? docs/reports/form-playable-20260928/autoanthony-late-load-worker-20261002.md | ?? docs/reports/form-playable-20260928/autoanthony-late-load-worker-request-20261002.md | ?? docs/reports/form-playable-20260928/autoanthony-late-load-worker-rework2-20261002.md | ?? docs/reports/form-playable-20260928/demon-form-ledger-supervisor-20261002.md | ?? docs/reports/form-playable-20260928/demon-form-ledger-worker-20261002.md | ?? docs/reports/form-playable-20260928/native-effect-smoke-implementation-20261002.md | ?? docs/reports/form-playable-20260928/native-effect-smoke-implementation-request-20261002.md | ?? docs/reports/form-playable-20260928/native-effect-smoke-runner-supervisor-20261002.md | ?? docs/reports/form-playable-20260928/native-effect-smoke-supervisor-20261002.md | ?? docs/reports/form-playable-20260928/native-effect-smoke-supervisor-request-20261002.md | ?? docs/reports/form-playable-20260928/partial-mod-dependency-audit-20261002.md | ?? docs/reports/form-playable-20260928/partial-mod-dependency-audit-request-20261002.md | ?? docs/reports/form-playable-20260928/partial-mod-launch-matrix-audit-20261002.md | ?? docs/reports/form-playable-20260928/partial-mod-launch-matrix-audit-request-20261002.md | ?? docs/reports/form-playable-20260928/powers-gate-fallback-supervisor-20261002.md | ?? docs/reports/form-playable-20260928/powers-gate-fallback-worker-20261002.md | ?? docs/reports/form-playable-20260928/recent-forms-code-independent-review-20261002.md | ?? docs/reports/form-playable-20260928/recent-forms-code-independent-review-request-20261002.md | ?? docs/reports/form-playable-20260928/recent-forms-code-post-void-review-20261002.md | ?? docs/reports/form-playable-20260928/recent-forms-code-post-void-review-request-20261002.md | ?? docs/reports/form-playable-20260928/void-form-transaction-supervisor-20261002.md | ?? docs/reports/form-playable-20260928/void-form-transaction-worker-20261002.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-reviewer-20261002.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-reviewer-request-20261002.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-reviewer-rework2-20261003.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-worker-20261002.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-worker-request-20261002.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-worker-rework2-20261003.md | ?? docs/reports/form-playable-20260928/void-transaction-identity-worker-rework2-request-20261003.md | ?? tools/content-latch-probe/ | ?? tools/event-removal-probe/bin/ | ?? tools/event-removal-probe/obj/ | ?? tools/form-effects-probe/bin/ | ?? tools/form-effects-probe/obj/ | ?? tools/menu-background-probe/ | ?? tools/viola-log-probe/
- Interop 源文件当前大小：mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs=44618; mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs=7210
- 实现报告存在且可读：True
- 下面的结论必须以当前源文件行号和静态证据为准；没有构建、测试或游戏运行证据。

## 进行中

- 正在从头复核上一轮 REWORK 的五个门禁：缺失 Watcher 类型、无 AssemblyLoad 的 Pending 重试、统一串行化与主线程边界、OfficialAddonPending 晚加载接管、以及无 AutoAnthony/Watcher/AutoAnthonyWatcher 硬引用。

## 未知

- 尚未判定 PASS 或 REWORK。

## 已确认

### 1. Watcher / WatcherCardPool 缺失不得 settled：PASS

- `AutoAnthonyCompatBridge.cs:535-543` 对 `WatcherMod.Watcher` 或 `WatcherMod.WatcherCardPool` 任一缺失统一执行 `ClearThirdPartyTransientState()`、设置 `Pending`、返回 `false`。
- `AutoAnthonyCompatBridge.cs:259-278` 将 optional 返回值与 core 状态共同计算 `settled`；因此该分支不会被 `Apply` 报告为 settled，也不会触发加载钩子摘除。
- `AutoAnthonyCompatBridge.cs:546-569` 将 `CardPool`、`AllCards`、`AllCardIds` 作为一个 3-patch capability；数量不完整时回滚并保持 `Pending`，没有 CardPool-only 降级。
- `AutoAnthonyLoadHook.cs:127-139` 对任何非 settled 结果保留 AssemblyLoad hook，并在主线程检查后建立 retry source。

### 2. Pending / 异常回滚后无 AssemblyLoad 重试：PASS

- `AutoAnthonyCompatBridge.cs:283-299` 的 `NeedsRetryWithoutAssemblyLoad` 覆盖 core 未完成、普通 `Pending`、`OfficialAddonPending`、旧 `Unsupported` 状态和三组 partial patch 列表。
- `AutoAnthonyCompatBridge.cs:451-479` 在第三方 capability 异常后无论回滚成功与否都保留可重试状态；回滚失败时记录 partial methods，回滚成功时仍设置 `Pending`。
- `AutoAnthonyLoadHook.cs:195-222` 仅在 Godot 主线程读取该状态并创建 1 秒唤醒 timer；`AutoAnthonyLoadHook.cs:236-245` 的 ThreadPool callback 只提交 `RequestApply`，不直接读取 capability、不直接 Apply。
- `AutoAnthonyLoadHook.cs:73-87` 对 `CallDeferred` 提交异常保留另一个 wake source，避免一次提交失败后丢失后续重试。

### 3. initializer / AssemblyLoad / retry 串行化及 Harmony 主线程边界：PASS

- 三个入口分别位于 `AutoAnthonyLoadHook.cs:28-35`、`:164-175`、`:236-245`，最终都汇入 `RequestApply`。
- `AutoAnthonyLoadHook.cs:46-104` 使用同一个 `ApplyGate` 合并 queued request，并由 `ExecuteDeferredApply` 在同一 gate 内清除队列标志和执行 apply；`_deferredApplyQueued` 的读写也受该 gate 保护。
- `AutoAnthonyLoadHook.cs:106-125` 是唯一实际调用 `AutoAnthonyCompatBridge.Apply` 的路径；先以 `NGame.IsMainThread()` fail-closed，再进入 Harmony patch/unpatch 和 capability 状态转换。
- `AutoAnthonyCompatBridge.Apply` 的源码调用点扫描结果仅有该处，没有旁路调用；`FormNativeSmokeRunner.cs:2075-2108` 提供的 `Callable.From(...).CallDeferred()` 主线程模式与本实现一致。
- `RetryGate` 只保护 `_harmony` 与 timer source；没有发现持有 `RetryGate` 后再等待 `ApplyGate` 的反向锁顺序，因此静态上未见明显锁反转。

### 4. OfficialAddonPending 晚加载接管：PASS

- `AutoAnthonyCompatBridge.cs:484-490` 每次 capability apply 先检测 `AutoAnthonyWatcher`，因此官方 addon 优先于旧 Watcher bridge。
- `AutoAnthonyCompatBridge.cs:581-606` 在官方 addon 出现后尝试移除旧/partial patch；任一 unpatch 失败则保留 method lists、设置 `OfficialAddonPending`、返回 `false`，旧回调通过 capability state 清空映射而 fail-closed。
- `AutoAnthonyCompatBridge.cs:293-298` 显式把 `OfficialAddonPending` 纳入无 AssemblyLoad 重试判定；`AutoAnthonyLoadHook.cs:127-139` 仅在真正 settled 后才摘除 AssemblyLoad hook 和 timer。
- `AutoAnthonyLoadHook.cs:247-250` 监听 `AutoAnthony`、`Watcher`、`AutoAnthonyWatcher` 三个程序集名；因此官方 addon 已加载但没有下一次 AssemblyLoad 时仍可由 timer 再次调用 `SetOfficialWatcherCapability`。

### 5. AutoAnthony / Watcher / AutoAnthonyWatcher 无硬引用：静态 PASS

- `AutoAnthonyCompatBridge.cs` 仅使用 assembly/type name 字符串、`AppDomain.GetAssemblies()`、`Assembly.GetType()` 和 `MethodInfo` 反射；`AutoAnthonyLoadHook.cs` 仅比较字符串程序集名。
- 源码扫描未发现 `using AutoAnthony`、`using Watcher`、第三方具体类型签名或 `typeof(AutoAnthony...)` / `typeof(Watcher...)`；`WatcherFormStancePower` 是 Spire1 自有类型，不属于 Watcher mod 类型。
- `Spire1.csproj:21-30,32-38,41-50` 仅保留 `0Harmony`、`sts2`、`BaseLib` 等现有依赖，未声明 AutoAnthony/Watcher/AutoAnthonyWatcher；`Spire1.json:10-15` 唯一 manifest dependency 为 `BaseLib >= 3.4.5`。
- `git diff --check` 对本轮两个 Interop 源文件通过。

## 未知

- 按用户要求未构建、未测试、未部署、未启动游戏；因此 Godot `Callable.CallDeferred()` 的实际启动时序、目标运行时 `NGame.IsMainThread()` 行为、Harmony 实际 patch/unpatch 结果均未实测。
- 当前“无硬引用”结论是源码、csproj 和 manifest 静态证据；最终 DLL 的 AssemblyRef 门禁仍需主会话在后续中央构建后执行，不能由本审查替代。
- 当前未对缺失类型、optional patch 异常回滚、官方 addon 无后续 AssemblyLoad 等场景运行探针；本报告只判断第二轮源码是否闭合上一轮 REWORK。

## 最终判定

**PASS（第二轮静态监督审查通过）。**

本 PASS 仅表示本轮指定的五项源码门禁已闭合，且没有发现需要返工的静态阻断项；不等同于构建、AssemblyRef、部分 mod 启动矩阵或真实游戏运行通过。后续仍必须由主会话中央构建、执行 AssemblyRef/manifest 门禁和隔离实机验证。
