# 姿态形态长回合边界烟测监督审查报告 - 2026-10-03

## 监督门禁与边界

- 已用当前 Codex 原生设施 `wait_threads` 等待 worker `01a0ff7a-94be-7790-93c1-713150880ea3`. 等待期间其 turn 状态由 `inProgress` 变为 `completed`(durationMs=907952), 收到 turnCompleted 后**才**开始审查; 等待前未做任何独立静态检查, 未并行冒充监督.
- 已读取: 本审查请求, worker 请求, worker 报告 `turn-boundary-smoke-worker-20261003.md`, 以及实际 `git diff -- mod/Spire1Code/Run/FormNativeSmokeRunner.cs`(1159 增 / 59 删, 文件 2478->3578 行).
- 本审查只读; 唯一写入为本报告. 未构建, 未测试, 未部署, 未启动游戏, 未修改产品代码, 未再委派.
- worker 自述路由: Codex 原生设施 / `global:deepseek-v4.1-flash` / reasoning `xhigh`, 未启动 omp/codex exec. 该路由为自述, 本报告未独立核验 session 元数据.

## 已确认

### 检查面 1 - parser 入口兼容与既有三场景行为(通过)

- 新增 `--form-native-smoke-turns`: `FormNativeSmokeRunner.cs:96-101`.
- `=/:` 值形式新增 `turns`: `FormNativeSmokeRunner.cs:107-112`.
- `--form-native-smoke` 全量展开仍只加入 calm/wrath/divinity, 不含 turns: `FormNativeSmokeRunner.cs:121-127`.
- 非法值仍走 `invalid:` 并在 `RunAsync` 拒绝(exitCode 2): `FormNativeSmokeRunner.cs:114-118`, `248-266`.
- 逐行比对 `RunEffectVerificationAsync` 旧(HEAD)与新实现, 唯一差异是新增 `cardEntry` 实参 `"WATCHER_STRIKE_P"`(`FormNativeSmokeRunner.cs:2633-2641`); 断言/数值/失败文本完全一致. 既有 calm/wrath/divinity 效果验证语义未被改写.

### 检查面 2 - turns 场景真实链路(通过)

- 真实 `NGame.Instance.StartNewSingleplayerRun(..., ModelDb.Modifier<FormStanceModifier>() ..., GameMode.Custom, ...)`: `FormNativeSmokeRunner.cs:1956-1972`.
- 真实 `RunManager.EnterRoomDebug`: `FormNativeSmokeRunner.cs:1992-2005`.
- 真实 `PlayCardAction` + `runManager.ActionQueueSynchronizer.RequestEnqueue`: `FormNativeSmokeRunner.cs:2852-2867`.
- 真实 `PlayerCmd.EndTurn(player, canBackOut:false)`: `FormNativeSmokeRunner.cs:2292-2304`; 该重载为引擎 public API(`.tmp/dllsrc/MegaCrit.Sts2.Core.Commands/PlayerCmd.cs:279`), AutoSlay 生产路径同用 `canBackOut:false`(`.tmp/dllsrc/MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms/CombatRoomHandler.cs:104`).
- 全文 grep 无 `PowerCmd.Apply` 注入形态, 无 `StanceCmd` 直调, 无伪造 hook. 唯一 `PowerCmd.Remove` 用于移除测试目标 ArtifactPower(`FormNativeSmokeRunner.cs:2029-2038`, `1737-1742`), 与既有 wrath fixture 同款; 未触及生产逻辑.
- 真实 Watcher 卡均按精确 `Id.Entry` 从 `ModelDb.AllCards` 查找: `WATCHER_VIGILANCE`/`WATCHER_ERUPTION_P`/`WATCHER_BLASHPHEMY`/`WATCHER_STRIKE_P`(`FormNativeSmokeRunner.cs:1426-1432, 1559-1565, 2461-2467, 2490-2496, 2531-2537`).

### 检查面 3 - Calm / Wrath 证据(通过, 仅静态)

- Calm: 真实 `WATCHER_VIGILANCE` 入场→formGate→首张真实 `WATCHER_STRIKE_P`→真实结束回合→下一 Play 阶段第二张 Strike; 记录 firstEnergy/secondEnergy/turnNumberBefore/After, 通过条件 `firstFree && allowanceRefreshed && secondFree`: `FormNativeSmokeRunner.cs:2461-2588`. before/after 与 failure gate 明确.
- "下一回合" 判据严格: 探针要求 `TurnNumber >= before+1` 且 `Phase == Play`(`FormNativeSmokeRunner.cs:2322-2327`); 引擎 `RunAutoPrePlayPhase` 在 `AfterPlayerTurnStart` 之后才置 `Phase = Play`(`.tmp/dllsrc/MegaCrit.Sts2.Core.Combat/CombatManager.cs:865-868`), 故 Calm 的刷新证据确实晚于回合开始 hook, 成立.
- Wrath: 记录 roundBeforeEntry/strengthRound1→真实结束回合→roundAfterBoundary/strengthRound2, 通过条件 `roundAfterBoundary > roundBeforeEntry && strengthRound2 > strengthRound1`: `FormNativeSmokeRunner.cs:1553-1623`. before/after 与 failure gate 明确.

### 检查面 4 - 主线程/超时/清理门(通过)

- 总 deadline 由 `EndTurnAndAwaitNextPlayAsync` 的 `CombatTimeoutSeconds` 与 `WaitForStateWithTimeoutAsync` 的 `deadline` 承担: `FormNativeSmokeRunner.cs:2306-2332`, `2975-3089`.
- 主线程 gate 保留: `InvokeOnMainThreadWithTimeoutAsync` + `BoundedMainThreadGateTimeout`: `FormNativeSmokeRunner.cs:3275-3296`, `3252-3256`.
- detached drain: 超时任务进入 `DetachedOperation` 并由 `ObserveDetachedTask` 观察: `FormNativeSmokeRunner.cs:3024-3033/3057-3066`, `3426-3452`.
- cleanup gate 与 unobserved-fault gate: turns 场景 `finally` 内的 `DrainDetachedOperationsAsync`/`cleanupAllowed`/`Cleanup` 结构与既有 `RunScenarioAsync` 一致(`FormNativeSmokeRunner.cs:2170-2273` vs 旧 `821-943`), 场景级 `ApplyUnobservedFaultGate` 保留(`2272`).

### 检查面 6 - 写集与脚本文本(通过, 附观察)

- 新增行仅 ASCII: 对 `git diff` 全部 `+` 行做码点扫描, 无非 ASCII 字符.
- 本 worker 的 diff 只落在白名单产品文件 `FormNativeSmokeRunner.cs`, 与请求一致.

## 已确认缺陷(REWORK)

### P1 - Divinity 入口把敌方目标传给 `TargetType.None` 的 Blasphemy, 引擎会取消该 action

- 现场: `RunDivinityTurnBoundaryAsync` 先取 `entryTarget = FindFirstHittableEnemy(player, preferNoArtifact:true)`(`FormNativeSmokeRunner.cs:1416-1419`), 再把 `entryTarget` 作为 target 传给 `WATCHER_BLASHPHEMY`(`FormNativeSmokeRunner.cs:1426-1434`).
- 事实: 真实 `WatcherMod.WatcherBlasphemy` 构造函数为 `base(1, CardType 2, CardRarity 4, (TargetType)0)`(`.tmp/watchermod/WatcherMod/WatcherBlasphemy.cs:21`), `(TargetType)0 == TargetType.None`(`.tmp/dllsrc/MegaCrit.Sts2.Core.Entities.Cards/TargetType.cs:5`).
- 事实: `CardModel.IsValidTarget(non-null)` 在 TargetType 既非 AnyEnemy 也非 AnyAlly 时返回 `false`(`.tmp/dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:1762-1784`).
- 事实: `PlayCardAction.ExecuteAction` 在 `!_card.IsValidTarget(target)` 时调用 `Cancel()` 并直接返回, 不进入 `OnPlayWrapper`(`.tmp/dllsrc/MegaCrit.Sts2.Core.GameActions/PlayCardAction.cs:85-89`); `Cancel()` 置 `State=Canceled` 并取消 completion(`.tmp/dllsrc/.../GameAction.cs:203-209`).
- 后果: `effectRun.Passed=false` → 提前 `return`(`FormNativeSmokeRunner.cs:1436-1440`), 此时 divinity 结果字典从未设置 `status` 键.
- 交叉印证: 旧 calm/wrath/divinity 单场景对 TargetType.None 的 divinity 传 `null`(`FormNativeSmokeRunner.cs:559-567` 的 `canonicalCard.TargetType == TargetType.AnyEnemy ? ... : null`); 历史实机 JSON `native-form-smoke-r23/r25/.../form-native-smoke-divinity.json` 为 `status=passed`, 其 `before.target=null` 且 `cardPlay.state=Finished`. 说明运行时不传目标才有效; 新 turns 路径传敌方目标与运行时契约冲突.

### P2 - "blocked/partial" 诚实阻塞路径实际不可达, 且与报告声明不符

- `divinityBlocked` 仅在 `divinity["status"] == "blocked"` 时为真(`FormNativeSmokeRunner.cs:2071-2073`), 而 `status="blocked"` 只在 turn-boundary 失败分支设置(`FormNativeSmokeRunner.cs:1491-1495`). 该分支必须先通过 entry(`entryRun.Passed=true`)。
- 由 P1, entry 在存在存活敌人时必然 Cancel → 走 `1436-1440` 提前返回, `status` 缺失 → `divinityBlocked=false` → 外层落到 `else` 置 `status="failed"`(`FormNativeSmokeRunner.cs:2087-2092`). 若敌人已死则 `entryTarget==null`(`1420-1424`)同样 `status` 缺失.
- 后果: turns 场景预期为 `failed`(exitCode 1), 而不是 worker 报告与注释声称的 `partial`; `FormNativeSmokeRunner.cs:415-417` 的 "仅 turns 接受 partial" 分支成为死代码. 这与请求文件 "无法覆盖要标 Unknown, 不得伪造 PASS" 的意图冲突, 且 worker 报告 "Divinity 阻塞路径" 的实现说明不成立.

## 进行中

- 无(静态检查面已全部完成).

## 未知

- 编译与实机结果不在本静态监督范围; worker 明确未构建未运行, 本报告也未运行.
- 真实引擎是否会触发额外 player choice / 敌人能否存活多回合, 静态不可判定(worker 已列未知).
- `Spire1RunContent.cs`(10:10:19)与 `Spire1LargeCapsuleGatePatch.cs`(10:09:41)的 mtime 落在本次时段内, 但其 diff 内容为 C14 r12 大胶囊/发牌门, 与本任务无关, 无法归因本 worker; 提请协调者注意并发写者, 不作为本 worker 越界证据.
- `form-native-smoke-turns.json` 的旧文件清理由外部 harness 负责, 不在本产品文件内; 本文件每次结束都会重写同名文件, 但若进程在写出前中断, 旧文件残留风险需由 harness 清理保证.

## 最终结论

- **REWORK(仅限静态监督)**.
- 阻断原因: P1/P2 - Divinity 入口对 `TargetType.None` 的 `WATCHER_BLASHPHEMY` 传入敌方目标, 引擎必取消该 action, 使 Divinity 子场景既不能通过, 也走不到设计的 blocked/partial 路径, 最终把 turns 判为 failed. 需改为按真实目标类型传 `null`(与既有 divinity 单场景一致), 并在 entry 失败等提前返回路径补齐 `status`, 使其与诚实阻塞语义一致.
- Calm/Wrath 证据链、parser 兼容、真实 API 链路、线程/超时/清理门、ASCII 与写集在静态范围内未发现问题.
- 本结论不代表构建或实机通过; 中央构建与隔离运行仍需在修复后单独执行.


---

# Rework 复审批次 - 2026-10-03 (第二轮监督审查)

## 监督门禁与边界

- 已用当前 Codex 原生设施 `wait_threads` 确认 worker `01a0ff7a-94be-7790-93c1-713150880ea3` 的 **rework turn** `01a0ff98-6935-7b92-9590-e6366e2fd0bc` 状态为 `completed`(durationMs=237703), 与首轮 turn 不同; 确认 completed 后才开始审查.
- 已读取: worker 报告 `turn-boundary-smoke-worker-20261003.md` 的 "Rework 批次" 与 "Rework 完成记录" 两节, 以及实际 `git diff -- mod/Spire1Code/Run/FormNativeSmokeRunner.cs`(1253 增 / 116 删, 文件 3578 -> 3615 行).
- 本审查只读; 唯一写入为本报告. 未构建, 未测试, 未部署, 未启动游戏, 未修改产品代码, 未再委派.
- 说明: `git diff` 的 116 行删除中约 115 行是首轮大插入造成的 hunk 边界重排, 非语义删除; 已用"按方法体逐行比对 HEAD"单独核实(见检查面 R4).
- worker 自述路由: Codex 原生设施 / `global:deepseek-v4.1-flash` / reasoning `xhigh`, 未启动 omp/codex exec. 该路由为自述, 本报告未独立核验 session 元数据.

## 已确认 (rework 焦点逐项)

### R1 - Blasphemy 现在确实传 null(通过)

- `RunDivinityTurnBoundaryAsync` 新增事实注释: `WATCHER_BLASHPHEMY` 是 `TargetType.None`, 传敌目标会让 `PlayCardAction.IsValidTarget` 返回 false 并 Cancel: `FormNativeSmokeRunner.cs:1417-1420`.
- `entryTarget` 查询描述改为 "find divinity observation target", 仅用于快照观测: `FormNativeSmokeRunner.cs:1421-1424`.
- 真实 `WATCHER_BLASHPHEMY` 的 `PlayEffectCardAsync` 目标实参改为 **`null`**: `FormNativeSmokeRunner.cs:1432-1440`(第 1435 行).
- 与引擎事实一致: `WatcherBlasphemy` 构造 `(TargetType)0 = None`(`.tmp/watchermod/WatcherMod/WatcherBlasphemy.cs:21`); `CardModel.IsValidTarget(null)` 对 None 返回 true(`.tmp/dllsrc/.../CardModel.cs:1762-1771`). 首轮 P1 的必取消缺陷已消除.

### R2 - Divinity 全部返回路径 status 完整(通过)

- 结果字典初始化即 `status=failed`: `FormNativeSmokeRunner.cs:1408-1414`.
- 6 条返回路径逐条核对, 全部写 `status`:
  1. entryTarget 缺失 -> `status=failed`(`1427`), `return result`(`1429`).
  2. entryRun 失败 -> `status=failed`(`1444`), `return`(`1446`).
  3. formGate 失败 -> `status=failed`(`1456`), `return`(`1458`).
  4. 回合边界 blocked(玩家死亡/战斗结束/超时)-> `status=blocked`(`1500`), `return`(`1503`).
  5. 正常清理判定 -> `status=passed|failed`(`1525`), `return`(`1534`).
  6. catch 异常 -> `status=failed`(`1539`), `return`(`1541`).
- 方法体内 `return result;` 仅上述 6 处(1401-1543 全文核对), 无漏网提前返回. 首轮 P2 的 "status 缺失" 已修复.
- blocked/partial 可达性: 引擎在 `AfterPlayerTurnStart`(其中 `EndTurnDeathPower` 造成 99999 伤害)之后才置 `Phase = Play`(`.tmp/dllsrc/MegaCrit.Sts2.Core.Combat/CombatManager.cs:865-868`); 下一回合探针要求 `TurnNumber>=before+1 && Phase==Play`, 故玩家死亡时探针返回 `player-dead`, `boundary.Passed=false` -> blocked -> 外层 partial. 该路径在静态链路上已可达(与首轮 "不可达" 不同).

### R3 - 外层无证据字典直接索引(通过)

- 新增安全读取 helper: `TryReadEvidenceFlag`(`FormNativeSmokeRunner.cs:1763-1772`), `ReadEvidenceString`(`1774-1786`), 均为 `TryGetValue` + 类型检查, 缺键降级不抛.
- 外层 `RunTurnBoundaryScenarioAsync` 全部改用 helper: wrath `2085`, calm `2094`/`2134`, divinity `2109-2111`/`2121`/`2127`; 无 `divinity[...]`/`calm[...]`/`wrath[...]` 直接索引(计数为 0).
- 无 "强转后直接索引" 残留(对 `as Dictionary<string, object?>?)[` 与 `(calm|wrath|divinity)Evidence[` 扫描均为 0). `calmEntry` 亦以 `TryGetValue` + 模式匹配读取(`2096-2102`). 字典缺键不会再抛 `KeyNotFoundException` 吞掉已通过的 Calm/Wrath 证据.

### R4 - 原三场景入口语义保持(通过)

- `RunScenarioAsync`(原 calm/wrath/divinity 单场景入口实现)与 HEAD 逐行比对: 均为 580 行, trim 后完全一致, 语义未改.
- `RunEffectVerificationAsync` 与 HEAD 逐行比对: 仅新增 `cardEntry` 实参一行, 断言/数值/失败文本一致.
- parser: `--form-native-smoke` 仍置 all 后只展开 calm/wrath/divinity(`FormNativeSmokeRunner.cs:121-127`); `turns` 仅由 `--form-native-smoke-turns`(`96-101`)或 `=/:` 值形式(`107-112`)触发; 非法值仍 `invalid:` 拒绝(`114-117`).
- `acceptableStatus` 仅对 `turns` 额外接受 `partial`, 其它场景语义不变(`411-421`).

### R5 - 主线程 gate 与真实 API 链保持(通过)

- 主线程 gate 与辅助设施仍在位: `InvokeOnMainThreadWithTimeoutAsync`(62 处引用)/`BoundedMainThreadGateTimeout`/`WaitForStateWithTimeoutAsync`/`WaitForConditionWithTimeoutAsync`/`DrainDetachedOperationsAsync`(8 处)/`ObserveDetachedTask`/`ApplyUnobservedFaultGate` 各定义唯一且被 turns 复用.
- cleanup gate 与 unobserved-fault gate 结构保留: `RunTurnBoundaryScenarioAsync` 的 `finally`(`2145-2273`)与 `RunScenarioAsync` 同构; `ApplyUnobservedFaultGate` 场景级(`2272`)保留.
- 真实 API 链保留: 真实 `StartNewSingleplayerRun`(`1992`)/`EnterRoomDebug`(`2028`)/`PlayEffectCardAsync` + `ActionQueueSynchronizer.RequestEnqueue`(`2900`)/`PlayerCmd.EndTurn(player, canBackOut:false)`(`2337`).
- 全文无 `PowerCmd.Apply` / `StanceCmd`(计数均为 0), 未引入形态注入或伪造 hook.

### R6 - 结构/文本卫生(通过)

- 新增行仅 ASCII(对 diff 全部 `+` 行做码点扫描, 无 >127).
- 括号平衡: `{}` 390/390, `()` 1196/1196, `[]` 556/556; 文件末正常闭合.

## 进行中

- 无(本轮焦点检查已全部完成).

## 未知 / 未覆盖

- 编译结论未验证; worker 与本审查都未构建. 实机结论需中央 Release 构建 + 隔离运行确认.
- Blasphemy 传 `null` 后, `EndTurnDeathPower` 击杀与 Divinity 清理 hook 的真实执行先后顺序、以及 blocked 快照能否读到已清理状态, 仍需实机确认(静态只知道两者都在 `AfterPlayerTurnStart` 阶段).
- Calm 跨回合免费额度刷新、Wrath round1->round2 力量增量、turns `partial` 的 exitCode 语义, 均待实机.
- 并发写者观察: `Spire1PowersGatePatch.cs` mtime(10:34:55)晚于本 worker rework turn 结束(等待时 turn 已 idle), 不归因本 worker; 本次 rework 时段(约 10:28-10:32)内白名单外文件无改动.

## 最终结论

- **PASS(仅限静态监督)**.
- 首轮两个阻断缺陷均已修复并有行号证据: P1 - Blasphemy 传 `null`(`1435`)且事实注释到位(`1417-1420`); P2 - Divinity 6/6 返回路径 status 完整(`1412/1427/1444/1456/1500/1525/1539`), 外层改用 `TryReadEvidenceFlag`/`ReadEvidenceString` 无直接索引(`1763`/`1774`/`2085`/`2094`/`2109`).
- 原三场景入口语义、主线程 gate、bounded drain、cleanup gate、unobserved-fault gate、真实 API 链在静态范围内保持; ASCII 与括号平衡通过.
- 本结论不代表构建或实机通过; 建议按计划执行中央构建与隔离运行以关闭剩余未知项.
