# 姿态形态长回合边界烟测 worker 报告 - 2026-10-03

## 已确认

### 任务边界与当前入口静态证据

- 请求文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\turn-boundary-smoke-worker-request-20261003.md`
- 唯一产品写入白名单: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`
- 唯一报告写入白名单: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\turn-boundary-smoke-worker-20261003.md`
- worker 不构建、不部署、不启动游戏、不运行测试，不再委派子代理。
- 当前 runner 已有入口为 `--form-native-smoke` 三场景和 `--form-native-smoke-calm|wrath|divinity`；解析位置在 `FormNativeSmokeRunner.cs` 第 63-123 行。
- 现有三场景已复用 `StartNewSingleplayerRun`、`FormStanceModifier`、`EnterRoomDebug`、真实 `PlayCardAction`、真实 Watcher 卡；对应位置约为第 474-620 行。
- 现有场景证据字段包含 `before`、`after`、`cardPlay`、`formGateAfter`、`effectVerification`、`unobservedFaults`；运行级仍保留 main-thread gate、deadline、detached drain、cleanup gate 和 unobserved-fault gate。
- 现有 `Snapshot(player, target)` 已记录 `nativeWatcherStance`、carrier/effect 类型、`energy`、`turnNumber`、`combatRound`、power amounts、目标/敌人状态和 `WATCHER_STRIKE_P` 历史；位置第 1397-1440 行。
- 现有 `PlayEffectCardAsync` 已用真实 `WATCHER_STRIKE_P`、真实 `PlayCardAction` 和 `runManager.ActionQueueSynchronizer.RequestEnqueue`；位置第 1821-1971 行。

### 长回合边界契约静态证据

- DEVELOP 第 34 行: Calm 虚空免费额度进入后本回合一次，并在每个自己的回合开始重置一次。
- DEVELOP 第 51-52 行: Divinity 在下一个自己的回合开始退出；若在回合开始 hook 内进入，不能被同次 hook 列表立即清掉，必须用阶段或回合身份区分下一次开始。
- `VoidFormEffectPower.BeforeSideTurnStart` 在参与者包含 Owner 时清空 `consumed`，这是下一回合免费额度刷新的真实 hook。
- `WatcherFormStancePower.AfterPlayerTurnStart` 对 Divinity 只在 `player.PlayerCombatState.TurnNumber > EnteredTurnNumber` 时调用 `StanceCmd.Exit`，这是“下一回合退出、入场同回合不清”的真实实现。
- `FormStanceWatcherBridge.AfterMarkerRemoved` 在原生 marker 移除后移除绑定的 carrier；`WatcherFormStancePower.AfterRemoved` 按精确实例移除两个 effect。因此 Divinity 边界清理可观测为原生 stance 变空且三类形态 power 计数归零。
- `CombatManager` 的玩家回合结束是两阶段 ready signal，随后 `SwitchFromPlayerToEnemySide` -> enemy turn -> `SwitchSides` -> `IncrementTurnNumber`；因此真实驱动必须等待再次回到 `PlayerTurnPhase.Play`，不能只调用 `PlayerCmd.EndTurn` 后立即断言。
- `PlayerCmd.EndTurn(Player, bool, Func<Task>?)` 是 public；`EndPlayerTurnAction` 在队列内也调用它，但会传 `canBackOut:true`（可撤销）。最终实现改用真实 `PlayerCmd.EndTurn(player, canBackOut:false)`，仍等待下一次 `Play` 阶段和 `TurnNumber` 前进。
- Divinity 的原生 `WatcherMod.Divinity.AfterSideTurnEnd` 会被 bridge 前缀 `KeepDivinityUntilNextTurnPrefix` 取消，所以原生“回合结束即退”在 form 模式下停用，下一回合由 carrier 的 `AfterPlayerTurnStart` 退出。
- Calm 的免费额度刷新由 `BeforeSideTurnStart` 完成；下一次 Play 阶段再打第二张真实 Strike 可分别验证刷新后的 `EnergySpent=0` 与下一张付费行为。

### 实现前的未实现项（已被后续实现取代）

- 实现前尚未新增 `--form-native-smoke-turns` 或 `--form-native-smoke=turns` 显式入口。
- 实现前尚未实现长回合 Calm 的两次真实 Strike、Divinity 下一回合退出断言、Wrath round 1/round 2 Strength 增量断言。
- 这些条目记录编辑前的静态状态；当前实现见“已完成实现”章节。

## 进行中

- 无（实现已静态完成，等待主会话集中构建与实机运行）。

## 未知

- 真实引擎在当前确定性遭遇下，结束回合并等待下一 Play 阶段是否会触发额外 player choice、暂停或超时；必须由主会话实机运行确认。
- 当前确定性遭遇的敌人是否足以承受长回合场景的多张 Strike，以及 Wrath round 2 后目标是否仍存活；静态阶段未知。
- 如果敌人回合导致玩家死亡或战斗结束，turns 场景必须写 failure evidence，不能继续伪造成 passed。

## 路由与合规

- 本 worker 使用当前任务已注入的 Codex 原生设施；未启动 `omp`、`omp-zh` 或 `codex exec`，未委派子代理。
- 用户指定模型为 `global:deepseek-v4.1-flash`，reasoning `xhigh`。worker 不自行切换模型或 provider；最终实际路由由主会话按 harness 元数据记录。

## 已完成实现 (2026-10-03, 静态完成, 未构建)

改动文件只有一个: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`
(2478 行 -> 3578 行)。未修改其它产品代码, 项目文件, 发布包, 游戏目录, Steam 安装或共享 mod_configs。

### 入口兼容性

- 新增显式 `--form-native-smoke-turns` (第 96-101 行)。
- `--form-native-smoke=turns` / `--form-native-smoke:turns` 也接受 (第 110 行)。
- `--form-native-smoke` 仍然只展开 calm/wrath/divinity 三场景 (第 121-127 行未改), 原有 calm/wrath/divinity 单场景入口与行为完全保留。
- `turns` 走独立场景方法 `RunTurnBoundaryScenarioAsync` (第 1899 行起), 通过第 363-374 行的显式分派进入。

### turns 场景内容

场景内按 Wrath -> Calm -> Divinity 顺序执行, 原因写在 runner 注释里:

1. Wrath 先跑, 让 `DemonFormPower` 观察真实 round 1 -> round 2 力量增长。
2. Calm 随后在 round 2 进入, 在 round 3 边界验证免费额度刷新。
3. Divinity 最后进入, 验证下一个自己回合退出。

每个子场景都复用真实链路: `StartNewSingleplayerRun` + `FormStanceModifier` + `EnterRoomDebug` + 真实 `PlayCardAction` + 真实 Watcher 卡 (`WATCHER_VIGILANCE`/`WATCHER_ERUPTION_P`/`WATCHER_BLASPHEMY`/`WATCHER_STRIKE_P`)。没有使用 `PowerCmd.Apply` 直接注入形态或伪造 hook。

### Calm 证据 (第 2445 行起 `RunCalmTurnBoundaryAsync`)

- 真实 `WATCHER_VIGILANCE` 进入 Calm, 之后 `formGateAfterEntry` 必须 `passed=true`。
- 首回合真实 `WATCHER_STRIKE_P` 打一次; 记录 `firstEnergy` (before/after/spent)、`firstStrikeFree`。
- 真实结束玩家回合 (`EndTurnAndAwaitNextPlayAsync`, 第 2277 行), 等待再次回到 `PlayerTurnPhase.Play` 且 `TurnNumber` 前进。
- 下一回合再打第二张真实 `WATCHER_STRIKE_P`; 记录 `secondEnergy`、`secondStrikeFree`、`freeAllowanceRefreshed`。
- 通过条件: 第一次免费 (`EnergySpent=0` 且能量不变) 且回合号前进 且第二次仍免费。
- 每张牌都记录 `EffectCardRun` (before/after/action), 供 JSON 读取。

### Divinity 证据 (第 1401 行起 `RunDivinityTurnBoundaryAsync`)

- 真实 `WATCHER_BLASHPHEMY` 进入 Divinity; `formGateAfterEntry` 记录原生 `Divinity` stance + `EchoCelestialStancePower` carrier + 两个 effect。
- 真实结束当前玩家回合并等待下一次自己的回合边界。
- 能安全到达下一 Play 时: `afterBoundary` + `formGateAfterBoundary` 记录原生 stance, carrier, 两个 effect 均已清理, 通过条件为 native stance 变空且 carrier/effect 计数为 0。
- `entryStatePreserved` 记录“入场同回合没有被立即清掉”。

### Divinity 阻塞路径 (重要, 不假通过)

静态审查确认真实 `WATCHER_BLASHPHEMY` 还施加原生 `WatcherMod.EndTurnDeathPower`, 它在下一次自己回合开始时对玩家造成 99999 伤害并移除自身 (`.tmp\watchermod\WatcherMod\EndTurnDeathPower.cs` 第 27-35 行)。因此长回合 Divinity 可能根本到不了存活的下一 Play 阶段。

- `EndTurnAndAwaitNextPlayAsync` 改为三态: `next-play` / `combat-ended` / `player-dead`, 超时也返回证据而不是抛异常吞掉。
- 若下一回合不可达, Divinity 子场景写 `status=blocked` + `blockedReason` (区分 EndTurnDeath 击杀 / 战斗结束 / 超时), 并 best-effort 快照 `afterBoundary`、`nativeStanceAfterBoundary`、`carriersAfterBoundary`、`effectsAfterBoundary`。
- 此时整个 turns 场景写 `status=partial` (不是 `passed`, 也不是 `failed`), `failure` 记录具体阻塞。主场景循环仅对 `turns` 接受 `partial` 为可接受状态 (第 417-427 行), 其它场景语义不变。

### Wrath 证据 (第 1537 行起 `RunWrathTurnBoundaryAsync`)

- 真实 `WATCHER_ERUPTION_P` 进入 Wrath, `formGateAfterEntry` 必须 `passed=true`。
- 记录 round 1 `StrengthPower` (`strengthRound1`), 真实结束回合并等待下一 Play。
- 记录 round 2 `StrengthPower` (`strengthRound2`), 通过条件: `roundAfterBoundary > roundBeforeEntry` 且 `strengthRound2 > strengthRound1`。
- 不能安全驱动时按既有失败路径写 failure evidence, 不伪造 passed。

### 公共基础设施

- `PlayEffectCardAsync` 从 Strike 专用泛化为 `(cardEntry)` 参数 (第 2804 行起), 现有 `RunEffectVerificationAsync` 调用点补上 `"WATCHER_STRIKE_P"`, 原三场景行为不变。
- `Snapshot` 新增 `combatInProgress` 与 `playerDead` 字段 (第 1706-1707 行)。
- 新增 `ReadTurnNumber` / `ReadCombatRound` 读取 helper (第 1747 行起)。
- 新增 `WaitForStateWithTimeoutAsync` 三态轮询 helper (第 2975 行起), 复用现有 main-thread gate、deadline 和 detached drain 结构。
- 结束回合使用真实 `PlayerCmd.EndTurn(player, canBackOut: false)`。未用 `EndPlayerTurnAction` 的 `canBackOut:true` 路径, 避免可撤销回合导致 runner 停顿。
- 保留主线程边界、总 deadline、detached drain、cleanup gate 和 unobserved-fault gate; 所有新场景失败路径都写 failure evidence。

## 未知 / 未覆盖 (需主会话实机确认)

- 未构建: 本 worker 按任务约束不构建、不部署、不启动游戏、不运行测试。所有编译结论待主会话 Release 构建确认。
- 真实引擎是否会在 Calm/Wrath 回合结束等待中触发额外 player choice 或超时, 静态不可判定。
- 确定性测试敌人 (`CubexConstruct`) 初始 65 HP + 13 block; 多张 Strike 与 Demon round 2 后是否仍存活未验证, 可能需主会话调整场景顺序或目标。
- Divinity 的 `EndTurnDeathPower` 是否确实在 `AfterPlayerTurnStart` 早于 `WatcherFormStancePower.AfterPlayerTurnStart` 执行, 决定 `blocked` 时能否读到已清理状态; 静态顺序不可判定。
- turns 场景整体 `partial` 的 exitCode 语义 (可接受状态 -> exitCode 0) 是否为主会话期望; 若要求严格, 需改为把 Divinity 阻塞视为 failure 并单列。
- `--form-native-smoke-turns` 与现有 harness 脚本的参数传递未验证。

## 改动行范围 (以当前文件为准)

- 第 96-101 行: 新增 `--form-native-smoke-turns` 入口。
- 第 110 行: `=/:` 值形式接受 `turns`。
- 第 363-374 行: turns 场景分派。
- 第 417-427 行: 仅 turns 接受 `partial` 状态。
- 第 1250, 1288 行: `FindScenarioCard` / `GetScenarioFormExpectation` 的 turns 映射。
- 第 1401-1632 行: `RunDivinityTurnBoundaryAsync` 与 `RunWrathTurnBoundaryAsync`。
- 第 1634-1643 行: `TurnBoundaryEvidence` 记录。
- 第 1706-1707, 1747 行起: Snapshot 字段与读取 helper。
- 第 1899-2274 行: `RunTurnBoundaryScenarioAsync`。
- 第 2277-2443 行: `EndTurnAndAwaitNextPlayAsync` 与 TryRead helper。
- 第 2445-2735 行左右: `RunCalmTurnBoundaryAsync`。
- 第 2804-2956 行: 泛化后的 `PlayEffectCardAsync`。
- 第 2975 行起: `WaitForStateWithTimeoutAsync`。

## 路由与合规 (复核)

- worker 未启动 `omp`/`omp-zh`/`codex exec`, 未委派子代理。
- 用户指定模型 `global:deepseek-v4.1-flash`, reasoning `xhigh`; 实际路由由主会话按 harness 元数据记录。

## Rework 批次 2026-10-03 (监督审查 + 实机缺陷)

### 已确认 (静态证据, 本轮 rework 的第一批结论)

- 缺陷 1 成立: `RunDivinityTurnBoundaryAsync` 原第 1416-1434 行先 `FindFirstHittableEnemy`, 再把 `entryTarget` 传给真实 `WATCHER_BLASHPHEMY` 的 `PlayCardAction`。
- `WatcherBlasphemy` 构造函数为 `base(1, (CardType)2, (CardRarity)4, (TargetType)0)`, 即 `TargetType.None` (证据: `.tmp\watchermod\WatcherMod\WatcherBlasphemy.cs` 构造函数)。
- `MegaCrit.Sts2.Core.Entities.Cards.TargetType` 枚举第 0 项为 `None` (证据: `.tmp\dllsrc\MegaCrit.Sts2.Core.Entities.Cards\TargetType.cs`)。
- `PlayCardAction.ExecuteAction` 对 `card.IsValidTarget(target)` 为 false 时直接 `Cancel()` (证据: `.tmp\dllsrc\MegaCrit.Sts2.Core.GameActions\PlayCardAction.cs`)。
- 结论: 传非 null 目标给 `TargetType.None` 的 Blasphemy 会被 `IsValidTarget` 判否并取消 action, 与实现者报告一致。修复方向是 Blasphemy 传 `target: null`, 不伪造目标。
- 缺陷 2 成立: `RunDivinityTurnBoundaryAsync` 的三条提前返回路径 (entryTarget 缺失第 1420-1424 行、entryRun 失败第 1436-1440 行、formGate 失败第 1447-1451 行) 只写 `failure`, 未写 `status`。
- 外层 `RunTurnBoundaryScenarioAsync` 第 2072-2073 行用 `divinity["status"]?.ToString()` 直接索引; 字典缺键时抛 `KeyNotFoundException`, 会把已经成功的 Calm/Wrath 证据一起变成 runner 顶层异常。
- 修复方向: Divinity 所有返回路径统一写 `status` (失败/blocked/按真实证据), 外层改用 `TryGetValue` 安全读取。

### 本轮 rework 约束

- 只改 `mod\Spire1Code\Run\FormNativeSmokeRunner.cs` 与报告文件。
- 不构建、不部署、不启动游戏、不测试、不委派子代理。
- 保持原 calm/wrath/divinity 三场景入口语义不变; 保留真实 API 链、主线程 gate、bounded drain、cleanup gate。

### 进行中

- 正在应用缺陷 1 与缺陷 2 的代码修复。

### 未知

- 修复后的编译结论仍待主会话 Release 构建确认; 本 worker 不构建。
- Divinity 传 `null` 目标后 `endTurnDeath` 击杀路径与清理 hook 顺序仍待实机确认。

## Rework 完成记录 2026-10-03

### 已确认 (修复后静态复核, 未构建)

- 缺陷 1 已修复: `RunDivinityTurnBoundaryAsync` 现在对真实 `WATCHER_BLASHPHEMY` 传 `target: null` (第 1432-1440 行)。`entryTarget` 仍被查出并用于快照观测, 但不再作为卡牌目标传入。
- 代码注释已记录事实 (第 1417-1420 行): `WATCHER_BLASHPHEMY` 是 `TargetType.None`, 传敌目标会让 `PlayCardAction.IsValidTarget` 返回 false 并 `Cancel()`。
- 缺陷 2 已修复: `RunDivinityTurnBoundaryAsync` 初始化即为 `status=failed` (第 1412 行), 且全部 6 条返回路径都写 `status`:
  - entryTarget 缺失 -> `status=failed` (第 1427 行)。
  - entryRun 失败 -> `status=failed` (第 1444 行)。
  - formGate 失败 -> `status=failed` (第 1456 行)。
  - 回合边界 blocked (玩家死亡 / 战斗结束 / 超时) -> `status=blocked` (第 1499 行)。
  - 正常清理判定 -> `status=passed|failed` (第 1524 行)。
  - catch 异常 -> `status=failed` (第 1538 行)。
- 缺陷 2 外层已改为安全读取: 新增 `TryReadEvidenceFlag` (第 1763 行) 与 `ReadEvidenceString` (第 1774 行), 外层不再对证据字典做直接索引。
- 外层 `RunTurnBoundaryScenarioAsync` 的 wrath/calm/divinity 判定全部通过上述 helper 读取 (wrath 第 2085 行, calm 第 2096 行, divinity 第 2103 行附近), 证据字典缺键不会抛 `KeyNotFoundException`。
- 原 calm/wrath/divinity 三场景入口语义未改: `--form-native-smoke` 仍只展开三场景; turns 仅由 `--form-native-smoke-turns` 或 `--form-native-smoke=turns` 触发。
- 真实 API 链保留: `StartNewSingleplayerRun` / `FormStanceModifier` / `EnterRoomDebug` / 真实 `PlayCardAction` / 真实 `WATCHER_VIGILANCE`/`WATCHER_ERUPTION_P`/`WATCHER_BLASHPHEMY`/`WATCHER_STRIKE_P`。
- 保留主线程 gate (`InvokeOnMainThreadWithTimeoutAsync`)、bounded drain (`DrainDetachedOperationsAsync`)、cleanup gate 与 `ApplyUnobservedFaultGate`。
- 静止检查: 大括号/圆括号/方括号平衡 (`brace depth 0`), 无对 `divinity[...]`/`calm[...]`/`wrath[...]` 的直接索引 (计数均为 0)。

### 各卡目标类型核对 (本轮一并确认, 防止第二处隐性取消)

- `WATCHER_STRIKE_P`: `TargetType.AnyEnemy` -> 需传敌目标 (现有传法正确)。
- `WATCHER_ERUPTION_P`: `TargetType.AnyEnemy` -> 需传敌目标 (现有传法正确)。
- `WATCHER_VIGILANCE`: `TargetType.Self` -> 传 `null` (现有传法正确)。
- `WATCHER_BLASHPHEMY`: `TargetType.None` -> 传 `null` (本轮修复)。

### 未知 (本 worker 不构建/不跑, 需主会话确认)

- 编译结论未验证; rework 只做静态检查。
- Blasphemy 传 `null` 后, `EndTurnDeathPower` 击杀与 Divinity 清理 hook 的真实顺序仍未实机确认, `blocked` 快照能否读到已清理状态未知。
- 实机确认 Calm 免费额度跨回合刷新与 Wrath round1/round2 力量增量仍待主会话运行。
- turns `partial` 的 exitCode 语义 (可接受状态 -> exitCode 0) 是否为主会话期望仍未知。

### 本轮改动行范围 (rework 增量)

- 第 1412 行: Divinity 结果字典新增默认 `status=failed`。
- 第 1417-1420 行: Blasphemy TargetType.None 事实注释。
- 第 1421-1440 行: `entryTarget` 改为仅供观测; Blasphemy `PlayCardAction` 传 `null`。
- 第 1427, 1444, 1456, 1538 行: 三条提前返回与 catch 补 `status=failed`。
- 第 1499, 1524 行: blocked / 正常清理路径 `status` (原有, 复核确认)。
- 第 1763-1786 行: 新增 `TryReadEvidenceFlag` / `ReadEvidenceString` 安全读取 helper。
- 第 2079-2121 行左右: `RunTurnBoundaryScenarioAsync` 外层 wrath/calm/divinity 判定改为安全读取。

## Rework 批次 2026-10-03 (实机 r3 精确字符串缺陷)

### 已确认 (主会话实机 r3 证据)

- 运行目录: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r3`
- 证据 JSON: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-turns-current-20261003-r3\form-native-smoke-turns.json`
- 顶层 `scenario=turns`, `status=failed`。
- 顶层 `failure` 原文: `Spire1.Spire1Code.Run.FormNativeSmokeRunner+MainThreadGateException: Main thread gate failed for find WATCHER_BLASHPHEMY for divinity-entry-blasphemy at direct ---> System.InvalidOperationException: Real Watcher card was not found in ModelDb.AllCards: WATCHER_BLASHPHEMY`
- `divinity.status=failed`, `divinity.entry.Card="WATCHER_BLASHPHEMY"`, `divinity.entry.Action.status="not-started"`。
- Wrath 子场景 `status=passed` (round 1->2, `strengthRound1=1` -> `strengthRound2=3`), Calm 子场景 `status=passed` (turn 2->3, 两次 Strike `EnergySpent=[0]`, `firstStrikeFree=true`/`secondStrikeFree=true`)。
- 根因: runner 第 1437 行把真实卡 entry 写成 `WATCHER_BLASHPHEMY`; ModelDb 真实 entry 为 `WATCHER_BLASPHEMY` (同一 runner 第 1249 行 `FindScenarioCard` 已使用正确拼写)。`FindCardByEntry` 抛 `InvalidOperationException`, 被主线程 gate 包装后使整个 turns 场景失败。
- 同文件第 1417 行注释也含错误拼写, 一并修正。
- 该缺陷只影响 `WATCHER_BLASHPHEMY` 字符串; 卡的真实类型 `WatcherMod.WatcherBlasphemy` 与 `TargetType.None` 判断不受影响。

### 修复范围 (待应用)

- `mod\Spire1Code\Run\FormNativeSmokeRunner.cs`: 将注释与 `entryRun` 的卡 entry 由 `WATCHER_BLASHPHEMY` 改为 `WATCHER_BLASPHEMY`。
- 不改任何其它标识符、标签 (`divinity-entry-blasphemy` 标签保持原样) 或逻辑。

## 拼写修复完成记录 2026-10-03 (r3)

### 已应用

- 产品文件 `mod\Spire1Code\Run\FormNativeSmokeRunner.cs`: 将 2 处 `WATCHER_BLASHPHEMY` 全部替换为 `WATCHER_BLASPHEMY`。
- 复检: 产品文件中 `WATCHER_BLASHPHEMY` 计数为 0; `WATCHER_BLASPHEMY` 计数为 3。
- 全仓 `*.cs` / `*.json` 扫描确认再无 `WATCHER_BLASHPHEMY`, 三处 `WATCHER_BLASPHEMY` 全部位于该 runner。
- 未改任何其它标识符或逻辑; 标签 `divinity-entry-blasphemy` 故意保持小写原样, 它不是 ModelDb entry。

### 修复行范围

- 第 1417 行: 注释 `// Real WATCHER_BLASPHEMY is TargetType.None (WatcherBlasphemy ctor passes` 修正 (注释文案)。
- 第 1437 行: `PlayEffectCardAsync` 的真实卡 entry 参数由 `WATCHER_BLASHPHEMY` 修正为 `WATCHER_BLASPHEMY` (本缺陷根因行)。
- 第 1249 行: `FindScenarioCard` 原有 `WATCHER_BLASPHEMY` 未变; 修正后两处拼写一致。

### 未知 (本 worker 不构建/不跑)

- 拼写修复后的编译与实机结论未验证, 待主会话重新构建并运行 turns 场景。
- Divinity 传 `null` 目标后 `EndTurnDeathPower` 击杀与清理 hook 顺序仍未实机确认。
- Divinity 在真实运行中会走 `passed` 还是 `blocked` 仍未确认。
