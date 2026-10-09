# persistent-safety-r18 worker 增量报告

范围: G:\omp works\Sts\sts2-forms\mod\FormsCode
模型/路由: global:deepseek-v4.1-flash / wb2api (用户本轮唯一指定)
边界: 本文件只记录只读研究与门禁状态; 门禁前不改任何产品代码, 不构建/lint/测试/部署/游戏/git, 不委派, 不启动其它 harness.

## 已确认

### W18-00 (门禁) 尚未收到 hub 的 r5 真实负例 BASELINE_CONFIRMED 通知, 现在不改代码
- 已在报告目录与 .tmp 全量搜索 `BASELINE_CONFIRMED`, 仅命中 worker.request.md 自身第 50 行, 无任何 hub 落盘通知.
- 证据命令: `Select-String -Path 'G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\*','G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\*\*' -Pattern 'BASELINE_CONFIRMED' -SimpleMatch`
- r16 真实基线尝试在启动前失败, 不是游戏内负例: `G:\omp works\.tmp\forms-independent-20261005\native-r5-bindingloss-terminal-r16\controller-failure.json` 记录 RecordedAt 2026-10-05T09:54:39, Status `controller-prelaunch-failed`, GameLaunched false, Failure `Cannot overwrite variable Error because it is read-only or constant.`, Scope `Controller error, no Forms runtime evidence`.
- r20 控制器修复请求已存在 (`...\controller-launch-fix-r20\worker.request.md`, 2026-10-05 09:54:39), 仍属启动脚本修复, 尚未产出 r5 真实负例.
- 结论: 满足 worker.request.md 的等待条件, 继续研究并可落盘, 但禁止改代码; 等 hub 给出 r5 真实基线复现落盘路径 + BASELINE_CONFIRMED.

### W18-01 当前写集未被本任务改动, 基线 hash 已记录
- FormStanceModifier.cs SHA256 DACA8DB261F69C6AC563F69F2D9F5301B3985C8EBAC95A29308237D978DABBC4
- FormStanceMode.cs SHA256 3C3445164E7BF3146118BB82FCF772EFAD35AB6992DF07E9C4F50649D0B5B918 (与 binding-loss-marker-r16 worker.md 记录的生产未改 hash 一致)
- MainFile.cs SHA256 112A5E94E35254140CDF5BF780A72102FF9743C621CE57B4835BC5A1B4FB3E75
- FormStanceSafetyGuard.cs 尚不存在 (Test-Path False).
- 证据命令: `Get-FileHash -Algorithm SHA256 -LiteralPath <上述三个 .cs>`

### W18-02 当前生产缺口: FormStanceModifier 只有建局/读档/开战三个 RequireAvailable, 无战中 fail-closed
- 证据: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceModifier.cs:20` AfterRunCreated, `:22` AfterRunLoaded, `:24-28` BeforeCombatStart; 全文无 ShouldPlay / BeforeDamageReceived / BeforePowerAmountChanged / BeforeSideTurnStart / BeforeSideTurnEndVeryEarly override.
- 证据: `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceMode.cs:27-28` `IsSelectedAndBound = IsSelected(player) && FormStanceWatcherBridge.IsAvailable`; 其文档 `:22-25` 明说 Shutdown/Terminal race 返回 false 以 preserve native behavior. 该注释与已定契约第 1 条冲突, 属需要改的必要注释/语义面.
- 结论: 桥 Bound 后 Terminal/Shutdown 撤 patch 时, 战中没有任何本局 modifier fail-closed 入口; 这是 r10/r15 已确认缺口的当前代码状态.

### W18-03 权威引擎签名与支付前时序 (供门禁后实现, 当前未改代码)
- 支付前: `PlayCardAction.cs:85` `_card.CanPlay(...)` 在 `:92` `SpendResources()` 之前; `CardModel.cs:1746` CanPlay 内 `Hook.ShouldPlay(combatState, this, out preventer, AutoPlayType.None)`; `Hook.cs:2370` `public static bool ShouldPlay(ICombatState combatState, CardModel card, out AbstractModel? preventer, AutoPlayType autoPlayType)`; `AbstractModel.cs:2347` `public virtual bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)`. 故 ShouldPlay 是支付前入口, BeforeCardPlayed 不是.
- 伤害落地前: `AbstractModel.cs:608` `public virtual Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)`; dispatch `Hook.cs:415-421`; 调用点 `CreatureCmd.cs:283` ModifyDamage 之后, `:287` DamageBlockInternal / `:293` LoseHpInternal 之前.
- power amount 变更前: `AbstractModel.cs:1057` `public virtual Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)`; dispatch `Hook.cs:1020-1027`; 调用点 `PowerCmd.cs:124` Apply / `:231` ModifyAmount, 在 ApplyInternal/SetAmount 之前.
- 回合边界: `AbstractModel.cs:1247` BeforeSideTurnStart (dispatch `Hook.cs:1156-1170`); `AbstractModel.cs:1354` BeforeSideTurnEndVeryEarly (dispatch `Hook.cs:1244-1274`, VeryEarly 在 `:1255`).
- PowerCmd.Remove 缺口: `PowerCmd.cs:291` `public static async Task Remove(PowerModel? power)`; `:293-298` 只 RemoveInternal + AfterRemoved, 不经 BeforePowerAmountChanged; 这是唯一需要独立 engine prefix 的路径.
- marker 权威 FullName: `FormStanceWatcherBridge.cs:700-702` RequireMarker("WatcherMod.Calm"/"WatcherMod.Wrath"/"WatcherMod.Divinity").

### W18-04 live 判据权威事实 (不使用 CombatState.IsLiveCombat)
- `CombatState.cs:614-617` `IsLiveCombat()` 无条件 `return true`, 不可作活性判据.
- `CombatManager.cs:167` `IsInProgress => _turnState?.IsInProgress ?? false`; `:176` `CurrentCombatId => _turnState?.Id`; `:203` `IsEnding` (仅 turnState != null 且 IsCombatEnding 才 true, 不能识别已拆除 CombatState); `:222` `IsOverOrEnding`; `:358` `IsCurrentLiveCombat(CombatId?)` 同时校验 IsInProgress 与 id.
- `PowerModel.cs:264` Owner; `:287` `CombatState => Owner.CombatState`; `Creature.cs:104` Player; `:125` `CombatState` (玩家出战斗时为 null).
- 结论: prefix 需同时满足 (a) `power.Owner.Player?.RunState?.Modifiers` 含 FormStanceModifier, (b) `power.Owner.CombatState != null` 且为当前 live 战斗 (current id/state 引用 + IsInProgress), (c) 非 IsEnding; teardown/普通局/非当前 combat/ending 必须放行.

### W18-05 MainFile 安装/健康门禁现状
- `MainFile.cs:76-91` 在 PhaseLock 内 new Harmony(ModId), 扫描 `typeof(MainFile).Assembly.GetTypes()` 全部带 HarmonyPatch 类型并 Patch; `:105` 失败时 `UnpatchAll(ModId)`; `:113-114` 置 _patchesInstalled/_patchesHealthy false; `:119-121` 全成功才置 true; `:172` bridge 绑定时检查 `MainFile.PatchesHealthy`.
- 结论: 新增 FormStanceSafetyGuard 若不排除出常规扫描, 会在 Shutdown 后被 MainFile.UnpatchAll(ModId) 撤掉或造成重复注册; 需要显式最早初始化 + 扫描排除 + 独立 HarmonyId/owner proof, 并使安装失败保持 PatchesHealthy=false/不可 Bound.

## 进行中

- 等待 hub 的 r5 真实基线复现落盘路径与 BASELINE_CONFIRMED 通知; 未开始任何代码修改.
- 设计草案 (未落代码): FormStanceModifier 增加 ShouldPlay / BeforeDamageReceived / BeforePowerAmountChanged / BeforeSideTurnStart / BeforeSideTurnEndVeryEarly 的 fail-closed override; 新增 FormStanceSafetyGuard 独立 engine prefix 覆盖 PowerCmd.Remove(PowerModel?); 统一错误消息含 Forms unavailable / restart game process / UnavailableReason; MainFile 显式最早初始化 guard 并排除常规扫描; FormStanceMode.IsSelectedAndBound 注释改为只对非 Forms 保留原生.

## 未知

- hub 的 r5 真实负例落盘路径与实际结果 (当前无 BASELINE_CONFIRMED).
- 真实游戏中 ShouldPlay throw / PowerCmd.Remove prefix throw / 回合边界 throw 的 UI 与队列表现.
- 战斗结束/房间拆除/quit 时姿态 marker 的真实清理路径.
- 跨进程读档/多人/真正热替换/性能未验证.
## 2026-10-05 10:19 门禁通过后的增量 (BASELINE_CONFIRMED)

### W18-06 (门禁) 真实 r5 Shutdown 负例已确认, 代码实现开始
- 证据: `G:\omp works\.tmp\forms-independent-20261005\r5-shutdown-baseline-confirmed.json` RecordedAt 2026-10-05T10:19:46, Status BASELINE_CONFIRMED, Scenario binding-loss-shutdown, ProductionDllSHA256 E5E61A9A..., SafetyPassed false, ObservedDamage 14, EnergyBefore 2, EnergyAfter 1, RawStanceAfterProbe Calm, Scope "Actual selected r5 run Shutdown regression".
- 说明: Strike 支付 2->1 并造成 14 伤害, Tranquility 随后变 Calm, 证明当前 r5 生产字节在 Shutdown 后静默恢复原生规则. Terminal 仍未达到 identity changed reason, 不算 terminal 复现成功.

### W18-07 已实现文件与精确 hash (未构建/未测试)
- FormStanceModifier.cs 8167F71AE9E602A599B4F9E5BE36FC0BA4F827AAF7872FE9C7C2A94D1F2E3F25 (3729 bytes): 新增 ShouldPlay (支付前), BeforeDamageReceived (HP/Block 前), BeforePowerAmountChanged (power amount 前), BeforeSideTurnStart / BeforeSideTurnEndVeryEarly (回合边界), 全部走 `ThrowIfSelectedFormsCombatUnavailable`; 保留 AfterRunCreated/AfterRunLoaded/BeforeCombatStart 的 RequireAvailable.
- FormStanceMode.cs 3A4832F3CE1F654A3EEC77072E07536A0C4D5CC30B1E67F0E057E0AD99EFD080 (5358 bytes): 新增 `ThrowIfSelectedFormsCombatUnavailable(ICombatState?)`, live 判据为 `CombatManager.IsInProgress && !IsEnding && !IsOverOrEnding && IsCurrentLiveCombat(CurrentCombatId) && ReferenceEquals(combatState, DebugOnlyGetState())`, 不使用 `CombatState.IsLiveCombat()`; 统一错误消息 `Forms unavailable: restart game process. UnavailableReason: ...`; KindOf 增加权威 `WatcherMod.Calm/Wrath/Divinity` FullName; IsSelectedAndBound 注释改为只对非 Forms 保留原生.
- MainFile.cs 1645DA4C48F7C88B61E13519AD234AE470903B06004E08CE170C7102BEEE5A1A (8334 bytes): Initialize 最早显式 `FormStanceSafetyGuard.EnsureInstalled()`; 常规扫描跳过 `typeof(FormStanceSafetyGuard)`; `PatchesHealthy` 追加 `FormStanceSafetyGuard.IsInstalled`; Shutdown 只撤 Forms owner, 安全 owner 驻留.
- FormStanceSafetyGuard.cs C4851CC3129C4334A088904FA70D0A976C18A649F836F92024126B5213CBEE84 (4508 bytes, 新文件): HarmonyId `Forms.FormStanceSafety`, 只引用 `PowerCmd.Remove(PowerModel?)` 与本项目类型; EnsureInstalled 幂等; ProofHoldsLocked 精确校验 `PowerCmd.Remove(PowerModel?)` 上 owner 为该 id 且 PatchMethod 为 RemovePrefix 的 prefix 数量 == 1; 安装或证明失败抛异常并使 MainFile 不发布 Bound; RemovePrefix 对 null/非姿态/非 Forms/非当前 live/ending/teardown 放行, 仅已选 Forms + 当前 live + 桥不可用抛标准异常.
- FormStanceBridgePump.cs 35EAABCAA7191CEE31F8AA7EB8960CC8B80A850F2B34FA72F3505FF6904BA1DC (1038 bytes): 新增 `_Ready` 设 `ProcessMode = Node.ProcessModeEnum.Always`, 使 pump 在 paused SceneTree (Combat FTUE/暂停) 仍消费 AssemblyLoad 通知; 不解除游戏暂停, 不改其它节点; 注释改为 Retryable or Bound.
- FormStanceWatcherBridge.cs 6F8425EA6A31A813B37DEFCD68DB7C80C76C7688E3CF7E6D503704B4E8DD69FE (59656 bytes): Shutdown 在发布 ShuttingDown 时写非空 `UnavailableReason = "Forms bridge shut down; restart the game process"`; AssemblyLoad 通知写 provisional `Watcher assembly load pending; restart the game process if identity verification fails`; Bound 身份复核通过时清回空串.
- FormStanceMainMenuRetryPatch.cs 463196D0475E878EC33C0DE35EF8FF621DA24C8EA2345294A77314507F989609 (894 bytes): 注释改为 "Retryable or Bound", 修复过期描述.
- Godot 权威 API 证据: `E:\Slay the Spire 2\data_sts2_windows_x86_64\GodotSharp.dll` 反射得到 `Godot.Node.ProcessMode` 类型 `Godot.Node+ProcessModeEnum`, 枚举含 `Inherit/Pausable/WhenPaused/Always/Disabled`; 故 Always 为真实 API.

### W18-08 静态自检
- 7 文件均 UTF-8 无 BOM, 无 CRLF; brace/paren 平衡: Modifier 8/8,18/18; Mode 12/12,52/52; MainFile 36/36,68/68; SafetyGuard 11/11,41/41; Pump 3/3,6/6; WatcherBridge 162/162,720/720; MenuPatch 2/2,8/8.
- 未构建/lint/测试/部署/游戏/git; 未再委派, 未启动其它 harness/模型/fallback.

## 进行中

- 等待 hub 中央编译与同批监督; 上述为源码/静态证据, 不是实机验证.
- Terminal 真实收敛仍未复现成功, pump ProcessMode.Always 的实机因果由 hub 验证; 本轮不声称 terminal 已修.

## 未知

- 真实游戏中 ShouldPlay throw / PowerCmd.Remove prefix throw / 回合边界 throw 的 UI 与队列表现.
- Terminal identity changed 真实路径是否经 ProcessMode.Always 消费并收敛, 未验证.
- 战斗结束/房间拆除/quit 时姿态 marker 的真实清理路径; 跨进程读档/多人/真正热替换/性能未验证.
