# binding-loss-repro-r11 worker 报告

## 已确认
- 已读取唯一请求文件：`G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-repro-r11\worker.request.md`。
- 本批只新增/修改测试载体，唯一报告为 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\binding-loss-repro-r11\worker.md`。
- 代码白名单：`tests\FormsNativeSmoke\FormNativeSmokeRunner.cs`、新文件 `tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`、`tests\FormsNativeSmoke\README.md`；不修改 MainFile/patch/csproj/其它载体/生产源码。
- 本轮约束：不构建、不 lint、不测试、不运行游戏、不部署、不 Git、不写 C:/Steam/共享配置/标准 Release/Workshop；不委派、不运行其它 harness。
- 已读取 `G:\omp works\AGENTS.md`，确认本批为“只写代码、跳过构建/lint/测试”，并由主会话集中验证。
- 已读取 r10 `review.md`：R10-01(P0) 静态确认 Bound 后 `EnterTerminalLocked`/`Shutdown` 会清空 `_binding` 并 `Unpatch`，随后原生 Wrath 2x、Divinity 3x/+3 能量、Calm +2 能量会静默恢复；尚缺实机复现。
- 现有 `LifecycleSmokeRunner.cs` 已提供可复用模式：显式 opt-in 命令行开关、真实主线程 `Callable.From(...).CallDeferred()` pump、有限超时、`Assembly.Load(byte[])` 同名副本、Harmony owner patch 计数、JSON 写入 G:、退出排空。该 runner 只做生命周期隔离，不覆盖真实入愤怒与下一张 Watcher 牌安全门。
- `FormNativeSmokePatch.cs:20-21` 的 NGame 启动补丁是现有两个 runner 的唯一接入点；白名单只允许修改 `FormNativeSmokeRunner.cs`，因此新场景必须通过现有 runner 的必要分支或 partial 声明接入。
- 独立化契约路径已定位：`G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md`（第 147 行要求已选模式而运行桥不可用时 IsSelected/Enter/Exit 任一环节明确失败，不静默退回普通规则）。

- 独立化契约接续增量已确认：先用 r5 生产字节 + 新增 test-only 载体复现；修复须有不依赖 Watcher 旧程序集 delegate 的已选局外层保护；安全门 = 明确带重启原因的拒绝且无原生副作用；非 Forms 局保持原生规则。
- 主 runner 真实引擎路径已定位（`FormNativeSmokeRunner.cs`）：
  - `NGame.Instance.StartNewSingleplayerRun(character, shouldSave:false, acts, new[]{ ModelDb.Modifier<FormStanceModifier>().ToMutable() }, FixedSeed, GameMode.Custom, 0)`（约 494-508 行）。
  - `runManager.EnterRoomDebug(encounter.RoomType, MapPointType.Unassigned, encounter.ToMutable(), showTransition:false)`（约 531-542 行），随后等待 `CombatManager.Instance.IsInProgress` 与 `PlayerTurnPhase.Play`。
  - 真实牌：`player.Creature.CombatState.CreateCard(canonicalCard, player)` -> `CardPileCmd.Add(card, PileType.Hand, skipVisuals:true)` -> `new PlayCardAction(card, target)` -> `runManager.ActionQueueSynchronizer.RequestEnqueue(action)` -> await `action.CompletionTask`（约 592-660 行）。
  - `FindScenarioCard` 支持 `WATCHER_ERUPTION_P`（愤怒）等真实 Watcher 牌（约 1243-1256 行）；`FindCardByEntry` 按 `ModelDb.AllCards` 的 `Id.Entry` 查找。
  - `Snapshot(player, target)`、`BuildFormGateEvidence(...)`、`WaitForConditionWithTimeoutAsync(...)`、`InvokeOnMainThreadWithTimeoutAsync(...)`、`DrainDetachedOperationsAsync(...)`、`TryWriteScenarioEvidence(...)` 均可复用，无需复制 3600 行 runner。
- 现有 runner 是 `internal static class FormNativeSmokeRunner`（非 partial）；`ParseRequest` 与 `RunAsync` 均为 private，因此新增场景只能通过白名单内 `FormNativeSmokeRunner.cs` 的必要分支接入，真实逻辑放新文件 `BindingLossSmokeRunner.cs`。

- 生产 bridge 公开面已确认（`G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs`）：
  - `public static bool IsAvailable`（84 行）、`public static string UnavailableReason`（99 行）、`public static IReadOnlyList<string> BoundTargets`（101 行）。
  - `public static bool TryBind()`（143 行）：Bound 且 identity 变化时 `EnterTerminalLocked("Watcher assembly identity changed after binding; hot reload is not supported, restart the process")`（154-160 行）。
  - `public static void Shutdown()`（289 行）：先发布 ShuttingDown，再停止 pump、撤 patch、清空 `_binding`/native 引用。
  - `public static FormStanceKind CurrentKind(Player player)`（835 行）；`Enter`（843 行）、`Exit`（863 行）在不可用时抛 `InvalidOperationException("Forms unavailable: " + UnavailableReason)`。
  - `FormStanceMode.IsSelectedAndBound = IsSelected(player) && FormStanceWatcherBridge.IsAvailable`（`FormStanceMode.cs:27-28`），确认丢失后会走 `false` 分支。
- 现有 Snapshot 可直接观测安全门所需字段：`nativeWatcherStance`、`formCarrierTypes`、`formEffectTypes`、`formModeSelected`、`formBridgeAvailable`、`energy`、`ownerPowerAmounts`、`target`、`enemies`、`watcherStrikeHistory`（`FormNativeSmokeRunner.cs:1677-1721`）。
- 权威 decompile 原生副作用面已确认：
  - `watcher-Wrath-current.cs:39-53`：dealer 为 owner 且攻击类 `ValueProp` 时返回 2x。
  - `watcher-divinity-current.cs:28-59`：入场 +3 能量，dealer 为 owner 时 3x。
  - `watcher-Calm-current.cs:31-39`：移除时若战斗进行中，`PlayerCmd.GainEnergy(2m, owner)`。
  - `watcher-WatcherCombatHelper-current.cs:537-550`：`ChangeStance<T>` 先 `RemoveAllStances`，再 `Apply<T>`，再 `OnStanceChanged`；`594-623` 为 stance change 后续（Rushdown 抽牌、MentalFortress 格挡、VioletLotus 能量等）。因此 stance mutation 可通过 `nativeWatcherStance`、owner power 集合与能量/历史快照交叉核对。
- `LifecycleSmokeRunner` 的 terminal 等待条件为 `!IsAvailable && UnavailableReason.Contains("identity changed")`，且通过 `Assembly.Load(byte[])` 从已加载 Watcher 的 `Location` 读取字节；该模式可复用，但本任务要求在其之前先真实入愤怒并证明 Forms carrier/effects 与 `IsAvailable`。

- r5 生产字节已独立核对：`G:\omp works\.tmp\forms-independent-20261005\forms-build-r5\Forms.dll`，116736 bytes，SHA256 `E5E61A9AEFD222E39ECE2FD8E148F18942D0AF713385FB7ABBFA2095C442E8F3`，与 `sts2-forms\DEVLOG.md:67` 一致。
- “下一张 Watcher strike”真实模型已确认：`G:\omp works\.tmp\watcher-decomp-20260928\WatcherMod\WatcherStrike_P.cs`，Entry 为 `WATCHER_STRIKE_P`，`DamageVar(6m, ValueProp.8)`，`OnPlay` 调用 `DamageCmd.Attack(...).FromCard(...).Targeting(...).Execute(choiceContext)`。该攻击类 ValueProp 正是 Wrath 2x / Divinity 3x 的触发面。
- 安全门所需判定可完全由真实快照推导：
  - `explicitRejection`：下一张 strike 的 action 未成功，且异常/失败信息包含重启理由（当前 r5 预计不会出现，须由真实运行决定）。
  - `noNativeMutation`：after 快照相对 before，敌人 HP/受伤值无新增、能量无原生 +2/+3、`nativeWatcherStance` 与 owner power 集合无变化。
  - `regressionObserved`：strike 实际完成并产生伤害，或发生 stance mutation/原生能量变化。
  - `safetyPassed = explicitRejection && noNativeMutation && !regressionObserved`；超时/未执行不算通过或复现。

- 真实入愤怒的可选路径已确认：
  - `WATCHER_CRESCENDO`（`WatcherCrescendo.cs`）：1 费，`OnPlay` 直接 `WatcherCombatHelper.EnterWrath(owner, this)`，无伤害。
  - `WATCHER_ERUPTION_P`（`WatcherEruption_P.cs`）：2 费，先造成 9 伤害，再 `EnterWrath`；现有 runner 用它作为 wrath 场景，但伤害会污染“丢失前/后”证据。
  - 因此本任务应先真实入愤怒并证明 carrier/effects，再取 before 快照，再对下一张 `WATCHER_STRIKE_P`（6 伤害，攻击 ValueProp）做安全门判定。
- `FormStanceMode.cs` 已完整确认：`IsSelectedAndBound` 在 bridge 丢失后返回 false（注释明确“preserve native behavior instead of throwing”）；`IsEnabled` 才会 `RequireAvailable()` 抛 `InvalidOperationException("Forms unavailable: " + UnavailableReason)`。因此 r5 预期行为是原生继续，而非显式拒绝；但最终结论必须来自真实运行。
- 现有 runner 的 `FindScenarioCard` 仅映射 calm/wrath/divinity/turns；新增场景需要在 `FormNativeSmokeRunner.cs` 内做最小分支（白名单允许），具体逻辑全部放新文件。

- 已确认 `PlayEffectCardAsync` 是可直接复用的真实出牌模式（约 2841-2960 行）：Snapshot -> FindCardByEntry -> CombatState.CreateCard -> CardPileCmd.Add -> new PlayCardAction -> RequestEnqueue -> await CompletionTask -> RecordActionEvidence -> after Snapshot。能量不足会真实失败，不伪造。
- `CardPlayHistoryEvidence` 与 `DescribeCardPlayHistory(player, "WATCHER_STRIKE_P")` 已直接提供 strike 的 FinishedCount/PlayIndex/PlayCount/EnergySpent/EnergyValue/IsAutoPlay，可作为“下一张 strike 是否真实执行”的硬证据；`HistoryFinishedDelta`/`HistoryDeltaValues` 可比较 before/after。
- `TotalEnemyHpLoss` / `TotalEnemyDamageTaken` / `TargetDamageTaken` 可直接量化原生伤害副作用；`ReadEnergy` 可直接量化原生能量副作用。
- 进入愤怒选用 `WATCHER_CRESCENDO`（1 费、无伤害、直接 `EnterWrath`），在取 before 快照前完成，避免 Eruption 的 9 点伤害污染后续伤害证据。
- 设计决策（待实现核对）：新增两个显式开关 `--forms-binding-loss-terminal` 与 `--forms-binding-loss-shutdown`，每次只允许一个场景、单场景独立进程；`BindingLossSmokeRunner` 复用主 runner 的真实 RunState/StartCombat/PlayCardAction/主线程/超时/快照/排空，不复制 3600 行；主 runner 仅增加必要解析与分派分支。
- 固定 JSON 字段：`testOnly`、`status`、`scenario`、`formsBoundBefore`、`selectedBefore`、`bindingStateAfter`、`unavailableReason`、`before`/`after` 快照、`action`/`exception` 证据、`explicitRejection`、`noNativeMutation`、`regressionObserved`、`safetyPassed`；退出码安全通过 0 / 失败 1。

- 已完成白名单内第一处改动：`FormNativeSmokeRunner.cs` 由 `internal static class` 改为 `internal static partial class`，新增显式开关 `--forms-binding-loss-terminal`、`--forms-binding-loss-shutdown` 及 `--forms-binding-loss=terminal|shutdown`，同一进程同时请求两个场景时判为 invalid，并在场景分派处新增 `BindingLossTerminalScenario or BindingLossShutdownScenario` 分支调用 `RunBindingLossScenarioAsync(...)`。
- 改动后文件 SHA256：`D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC`。
- 未改动 MainFile/patch/csproj/其它载体/生产源码；未构建、未测试、未运行、未 Git。

- 已完成新文件 `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs`（711 行，SHA256 `F1893158867F1ACF8228416C330637B507C08AE1350F28A3443A5BEFE2CFC741`）：
  - `partial` 扩展 `FormNativeSmokeRunner`，承载两个场景全部逻辑，复用主 runner 的 `Snapshot`、`InvokeOnMainThreadWithTimeoutAsync`、`AwaitOperationWithTimeoutAsync`、`WaitForConditionWithTimeoutAsync`、`RecordActionEvidenceAsync`、`TryCancelActionAsync`、`ReadActionRuntimeAsync`、`CreateActionResult`、`BuildActionFailure`、`AppendFailure`、`FindCardByEntry`、`FindFirstHittableEnemy`、`ReadCardPlayHistory`、`TotalEnemyDamageTaken`、`ReadEnergy`、`DrainDetachedOperationsAsync`、`DetachedOperation`、`FixedSeed`，未复制整套 runner。
  - 真实流程：`StartNewSingleplayerRun`（含 `FormStanceModifier`）-> `EnterRoomDebug` -> 等待 `CombatManager.IsInProgress` 与 `PlayerTurnPhase.Play` -> 等待 `IsAvailable` -> 真实 `WATCHER_ERUPTION_P` 入愤怒 -> 校验 `formModeSelected` / `nativeWatcherStance=Wrath` / `DemonReaperStancePower` / `DemonFormPower` / `ReaperFormEffectPower` -> 移除桥 -> before 快照 -> 真实 `WATCHER_STRIKE_P` -> after 快照与安全门判定。
  - terminal 场景：`Assembly.Load(byte[])` 读取已加载 Watcher 的 `Location` 字节，等待真实主线程 pump 使 `!IsAvailable && UnavailableReason.Contains("identity changed")`；shutdown 场景：真实调用 `Forms.FormsCode.MainFile.Shutdown()` 并等待 `!IsAvailable`。
  - 安全门：`explicitRejection = 未成功 && 含 restart/not supported/unavailable/hot reload 理由`；`regressionObserved = strike 历史已完成数增加 || 伤害>0 || nativeWatcherStance 变化 || 能量增加`；`safetyPassed = explicitRejection && !regression`。超时/未执行落入 `status=failed`，既不算通过也不算复现。
  - 故障 Task 排空：复用 `DrainDetachedOperationsAsync` 有界排空，并检查 `TaskHelper.UnobservedFault` 增量；未排空则判失败。生产即时死亡语义未改动。
  - 输出 `forms-binding-loss-<scenario>.json`，仅在 `SPIRE1_FORM_SMOKE_REPORT` 指向 G: 时写入。
- README 已更新（SHA256 `35AA54B8EF5E7028096C4CB55249ADA321B0E4156F4432346425BAD4013B47C9`）：新增两个开关、两种场景说明、JSON 固定字段、退出码语义与诚实边界。
- 主 runner SHA256 更新为 `D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC`。
- 静态自查：两个文件花括号/圆括号配平；新文件引用的主 runner 成员全部存在；using 覆盖 `Map`/`Models`/`Runs`/`Rooms` 等实际类型；无 TODO/NotImplementedException/NotSupported 占位。
- 未构建、未 lint、未测试、未运行游戏、未部署、未 Git；未改 MainFile/patch/csproj/生产源码。

- 静态复核发现任务还要求“至少一个变姿态行为”探针，现有实现只覆盖 strike。补做中：桥丢失后先打真实 `WATCHER_STRIKE_P`，再打真实 `WatcherMod.WatcherTranquility`（1 费、`EnterCalm`，从 Wrath 切 Calm），不编造 Entry 字符串（按 `ModelDb.AllCards` 类型全名查找）。安全门要求两个探针都“明确带重启理由的拒绝且无原生副作用”；探针未执行（例如能量不足）落入 failed，不算通过也不算复现。

- 变姿态探针已补做：桥丢失后先打真实 `WATCHER_STRIKE_P`，再按类型全名 `WatcherMod.WatcherTranquility` 查找并打真实 Tranquility（1 费、`EnterCalm`，从 Wrath 切 Calm）。两个探针都必须“明确带重启理由的拒绝且无原生副作用”才算安全通过；任一探针未执行/超时落入 failed，不算通过也不算复现。
- 能量问题已按用户指示用真实引擎命令设置显式测试 fixture：两个探针前各调用一次真实 `PlayerCmd.GainEnergy(decimal, Player)`，把能量补到至少 3，并通过 `WaitWithTimeoutAsync` 真正等待该 `Task`；未伪造支付、未跳过支付。fixture 证据字段 `energyFixtureMinimum=3` 与 `energyFixtureBeforeStrike` 写入 JSON。
- 入愤怒已从 Eruption 改为真实 1 费无伤害 `WatcherMod.WatcherCrescendo`（按类型全名从 `ModelDb.AllCards` 查找，不编造 Entry 字符串）；Crescendo 的 Entry 常量在本机权威 decompile 中不存在，属未确认项，故只用类型全名。

## 未知
- 新代码能否编译（本批明确不构建，由主会话集中验证）。
- 两个场景在 r5 生产字节下的真实运行结果：是否显式拒绝、是否出现原生副作用，均未运行，不得预先宣称。
- terminal 场景真实 pump 消费通知的帧数是否落在 30 秒有界等待内（未运行验证）。

## 未知
- terminal 场景 `Assembly.Load(byte[])` 后真实 pump 消费通知所需帧数（沿用 30 秒有界等待）。
- r5 产品当前是否提供“明确带重启理由的拒绝”；未运行前不宣称。
- 新代码能否编译（本批明确不构建，由主会话集中验证）。

## 未知
- terminal 场景 `Assembly.Load(byte[])` 后真实 pump 消费通知所需帧数（沿用 30 秒有界等待）。
- r5 产品当前是否提供“明确带重启理由的拒绝”；未运行前不宣称。
- 新代码能否编译（本批明确不构建，由主会话集中验证）。

## CODE_COMPLETE

### 交付文件与源码哈希 (SHA256)
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\BindingLossSmokeRunner.cs` = `A7BADDB66C49868B3005E0A9820ABE5BD8061D4A9D146709B2401ACD8D89A51B` (788 行)
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\FormNativeSmokeRunner.cs` = `D597EBE052BDB842FEEAF2426D6566BD0D901B47E5A3108D997408BA00185FFC`
- `G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\README.md` = `E8F4D199A3132761DD6A228EE2D888C975EA70C4E6CCA0F44B5C1B2816FA1683`
- 白名单外未改动: MainFile.cs / FormNativeSmokePatch.cs / FormsNativeSmoke.csproj / 生产源码 / 其它载体.

### 入口参数 (每个场景独立进程, 非请求状态不运行)
- terminal: `--forms-binding-loss-terminal` 或 `--forms-binding-loss=terminal`
- shutdown: `--forms-binding-loss-shutdown` 或 `--forms-binding-loss=shutdown`
- 同进程同时请求两者 -> `invalid:binding-loss-requires-one-scenario-per-process`
- 复用环境变量 `SPIRE1_FORM_SMOKE_REPORT` (必须是 G: 下目录), 输出 `forms-binding-loss-<scenario>.json`
- 退出码: 安全通过 0 / 失败 1; baseline-regression-observed 亦为 1.

### 场景语义 (两探针均可执行)
1. 真实 `StartNewSingleplayerRun` + `FormStanceModifier` -> `EnterRoomDebug` -> 等 `CombatManager.IsInProgress` 与 `PlayerTurnPhase.Play` -> 等 `FormStanceWatcherBridge.IsAvailable`.
2. 真实 1 费无伤害 `WatcherMod.WatcherCrescendo` (按类型全名从 `ModelDb.AllCards` 查找) 入 Wrath; 证明 `formModeSelected` / `nativeWatcherStance=Wrath` / `DemonReaperStancePower` / `DemonFormPower` / `ReaperFormEffectPower`.
3. 移除桥: terminal = `Assembly.Load(byte[])` 同名 Watcher 副本并等真实主线程 pump 进入 `identity changed`; shutdown = 真实 `Forms.FormsCode.MainFile.Shutdown()` 并等 `!IsAvailable`.
4. 显式测试 fixture: 两探针前各调用真实 `PlayerCmd.GainEnergy(decimal, Player)` 补足能量到 >= 3, 用 `WaitWithTimeoutAsync` 真正等待; 记录 `energyFixtureMinimum` / `energyFixtureBeforeStrike`; 未伪造支付.
5. 探针 A: 真实 `WATCHER_STRIKE_P` (6 伤害, 攻击 ValueProp, Wrath 2x 面); 探针 B: 真实 `WatcherMod.WatcherTranquility` (1 费, `EnterCalm`, 变姿态面).
6. 安全门: 两探针都必须 `explicitRejection` (未成功且含 restart/not supported/unavailable/hot reload 理由) 且 `noNativeMutation`; 任一出现 strike 完成/伤害/姿态变化/能量增加即 `regressionObserved=true`, 安全门失败. 超时或未执行 -> failed, 不算通过也不算复现.
7. 故障 Task 由 `DrainDetachedOperationsAsync` 有界排空并检查 `TaskHelper.UnobservedFault` 增量; 未排空判失败. 生产即时死亡语义未改动.

### JSON 固定字段
`testOnly`, `status`, `scenario`, `formsBoundBefore`, `selectedBefore`, `bindingStateAfter`, `unavailableReason`, `before`, `after`, `action`, `exception`, `explicitRejection`, `noNativeMutation`, `regressionObserved`, `safetyPassed`; 另有 `entry`, `nextStrike`, `stanceChangeProbe`, `strikeExplicitRejection`, `stanceProbeExplicitRejection`, `strikeExecuted`, `nativeDamageObserved`, `stanceMutated`, `stanceProbeStanceMutated`, `energyFixtureMinimum`, `energyFixtureBeforeStrike`, `unobservedFaults`.

### 静态自查 (非构建)
- 两文件花括号/圆括号配平 (新文件 65/65, 296 圆括号, 788 行).
- 新文件复用的主 runner 成员均已存在 (Snapshot / InvokeOnMainThreadWithTimeoutAsync / WaitWithTimeoutAsync / AwaitOperationWithTimeoutAsync / WaitForConditionWithTimeoutAsync / RecordActionEvidenceAsync / TryCancelActionAsync / ReadActionRuntimeAsync / CreateActionResult / BuildActionFailure / AppendFailure / FindCardByEntry / FindFirstHittableEnemy / ReadCardPlayHistory / TotalEnemyDamageTaken / ReadEnergy / DrainDetachedOperationsAsync / DetachedOperation / FixedSeed).
- 无 TODO / NotImplementedException / NotSupported 占位; 旧 `WATCHER_ERUPTION_P` 入愤怒与旧字段名已清除.

### 未验证边界 (必须由主会话集中验证)
- 未构建, 未 lint, 未测试, 未运行游戏, 未部署, 未 Git.
- 新代码能否编译未验证; 若 `PlayerCmd.GainEnergy` 返回类型或 `FindCardByTypeName` 查找面与假设不符, 属待修断点.
- 两个场景在 r5 生产字节 (SHA256 `E5E61A9AEFD222E39ECE2FD8E148F18942D0AF713385FB7ABBFA2095C442E8F3`) 下的真实结果未运行: 是否显式拒绝 / 是否出现原生副作用, 均未确认.
- `WatcherMod.WatcherCrescendo` 的 Entry 常量在本机权威 decompile 中不存在, 故只按类型全名查找; 若 ModelDb 中该类型不可用, 场景将如实失败而非伪造.
- terminal 场景真实 pump 消费通知所需帧数是否落在 30 秒有界等待内未运行验证.
- `PlayerCmd.GainEnergy` 仅从本机权威 decompile 使用点确认签名形状 `(decimal, Player) -> Task`, 未在测试载体编译中验证.

### 明确断点
- 代码交付完成; 下一步是主会话集中构建 `FormsNativeSmoke.csproj` (带 `-p:FormsDllPath=G:\omp works\.tmp\forms-independent-20261005\forms-build-r5\Forms.dll`), 再按独立进程分别跑两个开关并用 r5 字节记录 JSON.
- 若编译报错, 只需在新文件白名单内做 API 签名修正, 不扩面.