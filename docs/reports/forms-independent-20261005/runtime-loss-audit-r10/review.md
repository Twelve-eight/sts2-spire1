# Forms 运行期桥丢失只读审查 (runtime-loss-audit-r10)

范围: 只读静态审查, 不改代码/不构建/不测试/不运行游戏/不执行 git.
唯一契约: 已选 Forms 局在 bridge 不可用时不得静默按普通姿态规则继续.
模型/路由: global:deepseek-v4.1-flash / wb2api / xhigh (当前 harness 原生只读审查员).
边界: 以下全部是源码控制流证据, 非实机复现; 未运行隔离游戏.

## 已确认

### R10-01 (P0) Bound 后进入 Terminal/Shutdown: 已选 Forms 局静默退回原生姿态规则, 无最外层阻止

触发条件 (静态):
1. 已选 Forms 局且 bridge 已 `Bound`;
2. 运行中 bridge 离开 `Bound`: `FormStanceWatcherBridge.TryBind` 在 `Bound` 且收到 Watcher `AssemblyLoad` 后 `BoundIdentityStillValid()` 为假 -> `EnterTerminalLocked` (`FormStanceWatcherBridge.cs:154-160`); 或 `MainFile.Shutdown()` -> `FormStanceWatcherBridge.Shutdown()` (`MainFile.cs:162-202`, `FormStanceWatcherBridge.cs:289-338`).
3. 之后原生 Watcher 的下一张变姿态牌 / 伤害费用回调执行.

权威契约:
- `DEVELOP-forms-independent-20261005.md` 接续增量契约: "本局已选模式而运行桥不可用, IsSelected/Enter/Exit 任一环节均须明确失败, 不静默吞掉切换或退回普通规则."
- `DEVELOP.md` 第3节: "`FormStanceModifier` 在选择新局和加载旧局时必须在桥不可用时显式失败".

当前控制流 (证据):
- `FormStanceMode.cs:27-28` `IsSelectedAndBound = IsSelected(player) && FormStanceWatcherBridge.IsAvailable`; 注释 (第22-25行) 明说 "any Shutdown/Terminal race return false so the caller can preserve native behavior instead of throwing".
- `FormStanceWatcherBridge.cs:84-97` `IsAvailable` 仅 `Bound` 且无 pending AssemblyLoad 时为 true; `Terminal`/`ShuttingDown` 均为 false.
- `FormStanceWatcherBridge.cs:658-688` `EnterTerminalLocked`: 置 `Terminal`, 清空 `_binding`, 并 `Unpatch` 本 owner 全部 Watcher patch (第676-686行).
- `FormStanceWatcherBridge.cs:289-338` `Shutdown`: 置 `ShuttingDown` 后同样 `Unpatch` 全部 patch 并清空 `_binding`.
- 于是下列回调全部因 `IsSelectedAndBound == false` 走原生 (或 patch 已被撤掉直接原生):
  - `FormStanceWatcherBridge.cs:889-895` `MarkerAppliedPostfix` -> 不再接管 Watcher `AfterApplied`.
  - `FormStanceWatcherBridge.cs:922-926` `MarkerRemovedPostfix`.
  - `FormStanceWatcherBridge.cs:953-959` `NeutralizeDamagePrefix`: `return true` -> 原生 `Wrath`/`Divinity` 的 2x/3x 伤害倍率重新生效.
  - `FormStanceWatcherBridge.cs:961-967` `KeepDivinityUntilNextTurnPrefix`.
  - `FormStanceWatcherBridge.cs:969-982` `StanceChangedPrefix/Postfix`.
  - `FormStanceWatcherBridge.cs:1078-1085` `GainDivinityEntryEnergy`: `!CallbacksAllowed || !IsSelected` -> 恢复原生 `PlayerCmd.GainEnergy(3)`.
  - `FormStanceWatcherBridge.cs:1122-1144` `RemoveEndTurnDivinity` / `NotifyEndTurnDivinity`.
- 原生侧权威 decompile 佐证:
  - `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\watcher-Wrath-current.cs:39-53` 原生 Wrath 对 owner 造成/承受攻击 2x.
  - `...\watcher-divinity-current.cs:28-39,49-59` 原生 Divinity 入场 +3 能量, 攻击 3x.
  - `...\watcher-Calm-current.cs:31-39` 原生 Calm 移除 +2 能量.
  - `...\watcher-WatcherCombatHelper-current.cs:537-550` `ChangeStance<T>` 原生加 marker.

结论: 已选 Forms 局在 Bound 后丢失 bridge 时, 原生 marker 与其原生倍率/能量重新生效, 而 Forms 侧 `WatcherFormStancePower` 及其效果 power 仍可能留在 owner 身上 (其生命周期回调不以 bridge 为门禁, 见 `WatcherFormStancePower.cs:70-96,150-175`), 两者叠加后按普通姿态规则继续, 且没有任何最外层 abort/例外阻止. 这直接违反上述契约.

最小修复范围 (仅建议, 未改代码):
- 在 `IsSelectedAndBound` 的 false 分支区分 "非 Forms 局" 与 "Forms 局但 bridge 丢失"; 对后者在最外层 native 回调 (至少 `NeutralizeDamagePrefix`, `GainDivinityEntryEnergy`, `MarkerApplied/RemovedPostfix`, `StanceChanged*`, `EndTurn*`) 保留 fail-closed 行为, 而不是 `return true`/放行原生.
- 在已选局的中场入口 (回合开始 / 战斗事件) 增加 bridge 可用性复核, 使战中丢失也能显式失败而非静默退回.
- 是否保留原生语义给非 Forms 局这一条必须保留 (缺 Watcher 不得阻塞 mod 启动); 修复只应作用于 "已选 Forms 局".

尚缺的实机证据: 已选 Forms 局在 Bound 后真实触发 Terminal/Shutdown 的隔离运行未做; 本项为源码控制流 + 本机 decompile 证据, 非实机复现.

### R10-02 (P1) 已选 Forms 局的 bridge 可用性校验只覆盖建局/读档/战斗开始, 战中丢失无覆盖

权威契约: 同上 "本局已选模式而运行桥不可用...均须明确失败".

当前控制流 (证据):
- `FormStanceModifier.cs:20` `AfterRunCreated` -> `RequireAvailable()`.
- `FormStanceModifier.cs:22` `AfterRunLoaded` -> `RequireAvailable()`.
- `FormStanceModifier.cs:24-28` `BeforeCombatStart` -> `RequireAvailable()`.
- `FormStanceMode.cs:31-43` `IsEnabled` -> `RequireAvailable`; `RequireAvailable` 仅检查 `FormStanceWatcherBridge.IsAvailable`.
- `WatcherFormStancePower.cs:61` `BeforeApplied` -> `IsEnabled` (仅 power 应用时).
- 战斗开始之后, 除 power 应用路径外没有周期性/事件级复核; `Shutdown`/`EnterTerminalLocked` 本身也不检查 "是否有已选 Forms 局正在进行", 直接撤 patch.

结论: 如果 bridge 在战斗中途丢失, 只有下一次 `FormStanceModifier.BeforeCombatStart` 或新的 `WatcherFormStancePower.BeforeApplied` 才会再次显式失败; 当前这场战斗内不会中止, 与 R10-01 叠加成静默退回. 静态证据充分, 但 "游戏是否会在该异常处中止战斗/回主菜单" 属未知 (见下).

最小修复范围: 在战斗回合边界增加一次已选局 bridge 复核, 或在 `Shutdown`/`EnterTerminalLocked` 检测到活动 Forms 局时发布不可继续状态并让当前战斗显式失败.

尚缺的实机证据: 未验证游戏对 `RequireAvailable` 抛出的 `InvalidOperationException` 的实际处理 (中止/吞掉/回菜单).

## 进行中

- 无. 已在 5 分钟内收敛, 未继续扩大范围.

## 未知

- 实机: Bound -> Terminal/Shutdown 后下一张 Watcher 牌的真实表现 (倍率/能量/UI).
- 实机: 非 ProcessExit 路径下 `MainFile.Shutdown()` 是否会被运行时调用; 若不会, R10-01 主要通过 `EnterTerminalLocked` (Watcher 二次 AssemblyLoad / 身份改变) 触发.
- 实机: `RequireAvailable` 抛出异常在 `AfterRunCreated` / `BeforeCombatStart` 的实际后果.
- 未覆盖: 本审查未验证 r5 S-03..S-07 各项, 未扩 UI/多人/性能.
