# cleanup-guard-scout-r17 review

范围: 只读源码审查 (不构建/不测试/不部署/不运行游戏/不 git). 唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`, 当前 harness 原生只读审查. 不重复 r15 的 ShouldPlay 签名研究; 仅补三个未覆盖安全边界. 最多 3 项.

## 已确认

### R17-01 (P0) `PowerCmd.Remove` 对 "live 已选 Forms 局" 的唯一精确判据是 `CombatManager.IsEnding`, 但 Remove 本身不区分 "战斗中移除" 与 "战斗结束/拆除清理"; prefix 若只看 power 类型会把关闭窗口的 teardown 当恢复原生规则

- 目标签名与实现: `G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs:291` `public static async Task Remove(PowerModel? power)`; 实现 `:293-298`: `power.RemoveInternal()` -> `Cmd.CustomScaledWait(0.2f, 0.4f)` -> `await power.AfterRemoved(power.Owner)`. 该路径不经 `Hook.BeforePowerAmountChanged`, 与 r15 结论一致.
- `IsEnding` 语义 (权威契约): `CombatManager.cs:203-213` 只在 `_turnState != null` 且 `IsCombatEnding(turnState)` 为真时返回 true; `IsCombatEnding` (`:417-435`) 要求 `turnState.IsInProgress`, 且 `PendingLoss != null` 或 "无存活的 primary enemy 且 `Hook.ShouldStopCombatFromEnding` 为 false". 注释 `:194-201` 明确: 战斗结束流程中 (敌人已死/等待败北处理) 为 true; 战斗不在进行中时为 false. 因此 **`IsEnding` 不能识别 "已拆掉 CombatState / 已退出战斗" 的 teardown**, 这类情况下 `IsEnding` 可能为 false (见 `IsOverOrEnding` 注释 `:216-231`: 边界点需要它而不是 `IsEnding`/`!IsInProgress`).
- 精确判别 live 已选 Forms 局需要同时满足: (a) `power.Owner.Player?.RunState?.Modifiers` 中存在本局 `FormStanceModifier` (本局已选); (b) `power.Owner.CombatState != null` 且该 state 仍是当前战斗; (c) `!CombatManager.Instance.IsEnding`; (d) 该 power 是姿态 marker (本仓类型/`Id.Entry`, 不需要 Watcher Assembly/MethodInfo/delegate). 条件 (b)(c) 的组合才能把 "真实战斗中" 与 "CombatState 已空/已终结" 分开; 仅凭 `IsEnding` 会把 teardown 当成 live, 仅凭 `CombatState != null` 会把正在结束的 live 战斗当 teardown.
- 本项未找到任何引擎在战斗结束/房间拆除/quit 时 "必须调用 `PowerCmd.Remove` 清掉姿态 marker" 的直接调用点: 上述精确调用面 grep 显示 `PowerCmd.Remove(` 的调用者均为 mod 自身逻辑或引擎具体 power/monster 行为 (如 `AsleepPower.cs:27,34,42`, `TemporaryStrengthPower.cs:152` 等), 没有 "CombatRoom/CombatManager teardown -> PowerCmd.Remove(marker)" 的专用路径. 房间拆除更可能走 `Creature.ClearPowers`/`RemoveInternal` 这类批量清理 (见 `Creature.cs:645-668` 注释 "ONLY PowerModel.RemoveInternal should be calling this", `:660` "This skips the AfterRemoved call for powers"), 而该路径 **不经过** `PowerCmd.Remove` prefix. 因此 `PowerCmd.Remove` prefix 若 throw, 只能覆盖 "显式 Remove" 而非 "teardown 批量清理"; 不能把 "关闭窗口/退出房间时 marker 被清理" 当作 prefix 漏拦.
- 最小修复范围: prefix 只对 `power` 是姿态 marker 且 `power.Owner.Player?.RunState?.Modifiers` 含 `FormStanceModifier` 且 `power.Owner.CombatState != null` 且 `!CombatManager.Instance.IsEnding` 时 throw; teardown/`CombatState == null`/`IsEnding == true` 时必须放行. 尚缺实机证据: 关闭窗口/退出房间/quit 时真实引擎走哪条清理路径, 以及该 prefix 在 Watcher 协程中的异常传播.

证据命令:
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Commands\PowerCmd.cs' -Pattern 'public static async Task Remove|RemoveInternal|AfterRemoved'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatManager.cs' -Pattern 'public bool IsEnding|public bool IsOverOrEnding|private bool IsCombatEnding'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Entities.Creatures\Creature.cs' -Pattern 'ClearPowers|RemoveInternal|AfterRemoved'`

### R17-02 (P0) 本局 modifier 的来源判据是 `player.RunState.Modifiers` 中的 `FormStanceModifier`; `CombatState.IsLiveCombat()` 是常量 true, 不能用来区分 "真实战中" 与 "RunState 存在但 CombatState 空/已终结"

- 本局选择判据 (源码): `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs:20-21` `IsSelected(Player? player) => player?.RunState?.Modifiers.Any(modifier => modifier is FormStanceModifier) == true`. 这是 run state 序列化成员, 不依赖 Watcher 程序集是否加载, 因此普通局 (无该 modifier) 恒为 false, 不会误伤.
- 失败语义: `FormStanceMode.IsEnabled` (`:24-30`) 先 `IsSelected`, 再 `RequireAvailable()` (`:32-36`); `RequireAvailable` 只在 `FormStanceWatcherBridge.IsAvailable` 为 false 时 throw. `FormStanceModifier.AfterRunCreated/AfterRunLoaded` (`FormStanceModifier.cs:14-16`) 在 run 建立/读档时调用 `RequireAvailable`, 所以已选 Forms 局的桥不可用是显式失败, 不是静默回退.
- 真实战中判据: `RunState.IterateHookListeners` 在 `childCombatState == null` 时才把 `Modifiers` 加入 listener 列表 (`RunState.cs:563-574`); 战斗内 `Hook.IterateCombatHookListeners` 在 `CombatManager.Instance.IsOverOrEnding && !IsStarting` 时直接 `yield break` (`Hook.cs:53-58`). 因此 "RunState 存在但 CombatState 空/已终结" 与 "真实战中" 的差别可由 `CombatManager.Instance.IsInProgress` / `IsCurrentLiveCombat(combatId)` 判定.
- 关键反例 (必须记录): `CombatState.IsLiveCombat()` 在本引擎中 **无条件 `return true`** (`CombatState.cs:614-617`), 不能用作 "真实战斗中" 判据; `CreatureCmd.cs:518,618` 也把它当 "非 null 即 live" 的弱判断. 另 `CombatManager.IsInProgress` 定义为 `_turnState?.IsInProgress ?? false` (`CombatManager.cs:167`), `Reset` 会 `_turnState = null` (`:1200`), `EndCombatInternal` 会 `turnState.IsInProgress = false` (`:1307`), `IsCurrentLiveCombat` 同时检查 `IsInProgress` 与 combat id (`:336-361`). 这三者才是可用的活性边界.
- 无 Forms 普通局: `player.RunState.Modifiers` 不含 `FormStanceModifier` 时 `IsSelected` 为 false; 不读取全局 static 规则开关, 不会误伤普通局. 若 fail-closed 入口只在 `IsSelected(player) && !IsAvailable` 时触发, 则普通局不会触发.
- 最小修复范围: 以 `player.RunState.Modifiers.Any(m => m is FormStanceModifier)` 为 "本局已选"; 以 `player.Creature.CombatState != null && CombatManager.Instance.IsInProgress` (或 `IsCurrentLiveCombat(combatId)`) 为 "真实战中"; 不使用 `CombatState.IsLiveCombat()` 作为活性证据. 尚缺实机证据: 读档后 `RunState.Modifiers` 的实际存活时点与 `CombatState` 空窗.

证据命令:
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceMode.cs' -Pattern 'IsSelected|IsEnabled|RequireAvailable'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatState.cs' -Pattern 'public bool IsLiveCombat'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\research\engine-dllsrc\MegaCrit.Sts2.Core.Combat\CombatManager.cs' -Pattern 'public bool IsInProgress|public bool IsCurrentLiveCombat|_turnState = null|turnState.IsInProgress = false'`

### R17-03 (P1) 现有 safety owner 的安装证明与 Shutdown/ProcessExit 最小策略: 它持有的是 Forms 自有 owner 与 bridge, 不持有 Watcher 动态引用; 但旧 Lifecycle 若只验证两个原 owner 无残留, 必须新增观测 "独立 guard 的 HarmonyId 仍存在"

- 安装证明 (Forms 自有 owner): `G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs:76-123` 在 `lock (PhaseLock)` 内只扫描 `typeof(MainFile).Assembly` (`:85`), 每个带 `HarmonyPatch` 的类型用 `harmony.CreateClassProcessor(type).Patch()` (`:91`), 失败即 `harmony.UnpatchAll(ModId)` (`:105`) 并置 `_patchesInstalled=false; _patchesHealthy=false` (`:113-114`). 成功才置 `_harmony` (`:119`) 与 `_patchesHealthy=true` (`:121`). 这是 "all-or-nothing" 的安装证明, 不涉及 Watcher 程序集.
- Shutdown 最小策略: `MainFile.cs:162-202` 先 `FormStanceWatcherBridge.Shutdown()` (`:169`, 先发布 `ShuttingDown` 再撤 bridge patch), 然后 `_harmony?.UnpatchAll(ModId)` (`:178`), 置 `_harmony=null` (`:185`), 最后退订 `AppDomain.CurrentDomain.ProcessExit -= OnProcessExit` (`:193`). `OnProcessExit` (`:204-213`) 只调用 `Shutdown` 并吞掉异常, 是进程退出 backstop.
- 是否持有 Watcher 动态引用: `FormStanceWatcherBridge.Shutdown` (`FormStanceWatcherBridge.cs:289-338`) 在 `lock (Gate)` 内先置 `_state = ShuttingDown` (`:298`), 递增 `_bindingGeneration` (`:299`), 停止 pump (`:303`), 逐个 `harmony.Unpatch(target, All, HarmonyId)` (`:310-320`) + `UnpatchAll(HarmonyId)` (`:321-328`), 然后 `_binding=null` (`:331`), `_boundAssembly=null` (`:332`), `_boundAssemblyIdentity=null` (`:333`), `BoundTargets=Array.Empty<string>()` (`:334`), `RemoveAssemblyLoadSubscription()` (`:335`), `Spire1StanceNotification.Reset()` (`:336`). `Reset` (`:1191-1201`) 清 `_resolved/_dispatch/_assembly/_identity`. 因此 Shutdown 后不持有 Watcher `Assembly`/`MethodInfo`/delegate; 但 `_harmony` 自身在 `Shutdown` 中已先 `_harmony=null` (`:309`) 再 unpatch, 这是可核对的引用释放顺序.
- 与独立 process-lifetime guard 的关系: 若新增一个只对 `PowerCmd.Remove(PowerModel?)` 的 engine Harmony prefix, 它必须使用与 bridge 不同的 `HarmonyId` (bridge 是 `Forms.FormStanceMode.Watcher`, `FormStanceWatcherBridge.cs:38`), 且不能被 bridge 的 `Shutdown` 触及. 这样 bridge Shutdown 撤掉 Watcher patch 后, guard 仍存活; 该 guard 只引用 engine 稳定签名与本项目 `FormStanceModifier` 类型, 不持有 Watcher 动态引用, 不随 Watcher 程序集卸载而失效.
- 旧 Lifecycle 验证缺口: 若现有 Lifecycle 只验证 "两个原 owner 无残留" (即 Forms owner 与 bridge owner 都无 patch), 必须补充一个新观测: 独立 guard 的 `HarmonyId` 在进程生命周期内仍存在, 且 `PowerCmd.Remove` 的 `Harmony.GetPatchInfo(target).Owners` 含该 guard id; 否则会把 "故意驻留的 guard" 误报为 "未清理", 或者反过来在 Shutdown 后完全丢失 fail-closed 入口. 该观测不需要持有 Watcher 引用, 只需要 engine 目标方法签名.
- 尚缺实机证据: 进程退出时 guard 是否被 Harmony 自动移除; `PowerCmd.Remove` prefix 在真实游戏内的异常去向; 以及 guard 的 `HarmonyId` 在 Lifecycle 检查中的实际可读性.

证据命令:
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs' -Pattern 'InstallOwnPatches|UnpatchAll|Shutdown|ProcessExit'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs' -Pattern 'HarmonyId|public static void Shutdown|_binding = null|RemoveAssemblyLoadSubscription'`
`Select-String -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\FormStanceWatcherBridge.cs' -Pattern 'HarmonyId|public static void Shutdown|Unpatch'`

## 进行中

- 无; 三项已收敛.

## 未知

- 未做任何实机/动态复现; 全部为源码控制流证据.
- 未验证战斗结束/房间拆除/quit 时姿态 marker 的真实清理路径.
- 未验证 `PowerCmd.Remove` prefix throw 在真实游戏中的异常去向与 UI 表现.
- 未验证进程退出时 Harmony patch 的实际卸载顺序.

