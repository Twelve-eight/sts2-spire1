# StS2 回合状态机（Combat Turn Machine, EA build）— sts2-spire1 知识库

## 本卷范围
`Combat/CombatManager.cs` 的回合主循环与回合尾两段式：PlayerTurnPhase 相位、EndTurnSignal、EndPlayerTurnPhaseOne/Two、Flush（StS2 的"回合尾清手"机制与 ShouldFlush/ShouldRetainThisTurn 钩子）、胜负检查插桩、CombatTurnState 生命周期。
来源：`research/engine-dllsrc/`（C#）。关联：`sts2-combat-semantics.md`（伤害/死亡）、`sts2-monster-ai.md`（StartTurn 的 A05/A07）、StS1 对照 `../sts1-kb/mechanics/turn-phase.md`。

**图例**：**高**=源码直接可证（引用 `文件#方法` + 反编译行号，重导出会漂移）。

---

## 1. 相位与回合循环骨架

**T01 PlayerTurnPhase 全集** — 出处 `Combat/PlayerTurnPhase.cs`。置信度：**高**
`None → Start（回合切换侧至抽牌可出牌间：清格挡/AfterSideTurnStart/球开局触发/能量重置）→ AutoPrePlay（自动出牌窗口，见 A07）→ Play（可操作）→ AutoPostPlay（结束回合后的自动出牌窗口）→ End`。
**T02 回合循环骨架** — 出处 `CombatManager.cs`：`RunTurnLoopAfter(499) → StartCombatInternal(576) → per-side 循环 { StartTurn(688) → 玩家侧：AwaitTurnEndAndSwitchSides(635) → EndPlayerTurnPhaseOne(1518) → PhaseTwo(1741) → SwitchFromPlayerToEnemySide(1854) → StartTurn(enemy) → ExecuteEnemyTurn(1405) → EndEnemyTurn(1076) → 切回 }`。置信度：**高**（方法级）；循环体内部细节以各节为准。
要点：每个回合循环持有**自己的 CombatTurnState 快照**（行 571 注释："turn loop 捕获后永不重读 _turnState"）——前一场战斗的循环残留不会污染下一场（联机/连战防串扰设计）。

**T03 EndTurnSignal（结束回合是信号，不是调用）** — 出处 `Combat/EndTurnSignal.cs` + `CombatManager.cs#AwaitTurnEndAndSwitchSides`（行 635-670）+ `SetReadyToEndTurn(935)`。置信度：**高**
```
EndTurnSignal = record { RunningAction: GameAction?, ScheduledTurnNumber, ScheduledPlayer, ActionDuringEnemyTurn? }
AwaitTurnEndAndSwitchSides: await endTurnSignalSource.Task
  → 若 RunningAction != null：await 其 CompletionTask（"Void Form 的出牌必须打完才准结束回合"）
  → AfterAllPlayersReadyToEndTurn(信号)
陈旧信号防护：ScheduledTurnNumber != 当前回合 → 报错并照跑（Sentry 上报一次）
联机就绪制：每玩家 SetReadyToEndTurn / UndoReadyToEndTurn / SetReadyToBeginEnemyTurn（ReadyLock 保护）
```

## 2. 回合尾两段式（Phase One / Phase Two）

**T04 Phase One（EndPlayerTurnPhaseOneInternal，行 1518-1614）** — 出处同。置信度：**高**
```
WaitForUnpause → playersEndingTurn（extra-turn 玩家优先）
① 逐玩家 Phase=AutoPostPlay → Hook.AfterAutoPostPlayPhaseEntered（可自动出牌！）
② 全部完成后 Phase=End
③ Hook.BeforeSideTurnEnd → CheckWinCondition
④ 逐玩家 DoTurnEnd(1602)：
     OrbQueue.BeforeTurnEnd                     ← 宝珠回合尾触发（先于一切手牌处理）
     手牌分拣：HasTurnEndInHandEffect 卡 → turnEndCards
               Ethereal 且 Hook.ShouldEtherealTrigger → 立即 Exhaust(causedByEthereal:true)
     DoTurnEndCards：回合尾卡逐张延迟入 Play 堆自动打出（错峰）
⑤ 复查胜利 → 逐玩家 Hook.BeforeFlush
```
**T05 Phase Two（EndPlayerTurnPhaseTwoInternal，行 1741-1800）+ Flush** — 出处同。置信度：**高**
```
逐玩家 FlushPlayerHand(1780)：
  flag = Hook.ShouldFlush(state, player)          ← ★ 全手保留闸（某 power/relic 让你不清手）
  逐手牌：!flag || card.ShouldRetainThisTurn → cardsToRetain；否则 cardsToFlush
  cardsToFlush → CardPileCmd.Add(Discard)
  Hook.AfterFlush(state, player, ctx, flushed, retained)
  player.PlayerCombatState.EndOfTurnCleanup()
→ Hook.AfterSideTurnEnd → checksum
```
**StS1 对照**（draw-exhaust.md §5）：StS1 弃牌阶段在**新回合块前的 endTurn 链**里做 Retain→Ethereal→弃牌三判定（DiscardAtEndOfTurnAction，判 retain 先于 Ethereal）；StS2 拆成两段——**Ethereal 在 Phase One 消耗（带 ShouldEtherealTrigger 门）、Flush 在 Phase Two（带 ShouldFlush 总闸 + 逐卡 ShouldRetainThisTurn）**，且"回合尾自动打出的牌"（turnEndCards）先于 flush。"弃牌不触发 onManualDiscard 类遗物"的 StS1 语义在 StS2 对应 AfterFlush 的 flushed/retained 分组。

## 3. 胜负与状态生命周期

**T06 胜负检查插桩** — 出处 `CheckWinCondition(1376/1390)` 调用点（SetupPlayerTurn 后、出牌循环内逐怪后、DoTurnEnd 前后、BeforeFlush 后等）。置信度：**高**
死亡本身不直接判负——`Kill` 里的 LoseCombat（sts2-combat-semantics.md S06）与循环内 CheckWinCondition 双保险；`PendingLossState/ProcessPendingLoss(1277)` 延迟败局结算（联机等待）。
**T07 CombatTurnState = 战斗作用域容器** — 出处 `Reset(1265 注释)`：所有战斗内状态（相位/信号源/ReadyLock/Checksum）挂在 turnState，`Reset(graceful)` 整体丢弃；仅 manager 级状态跨战斗存活。置信度：**高**
**T08 AfterAllPlayersReadyToEndTurn 的陈旧转换防护** — 出处 行 1440-1477 + `_staleTurnEndReported` 单次上报。置信度：**高**（前一段"迟到的结束回合信号照跑但告警"的设计实录）。

## 4. StS1 → StS2 回合仲裁速查

| 语义 | StS1（turn-phase.md） | StS2（本卷） | 仲裁建议 |
|---|---|---|---|
| 回合尾自动打牌 | 哨兵链手牌 triggerOnEndOfTurnForPlayingCard → cardQueue（R07/R08） | Phase One turnEndCards（HasTurnEndInHandEffect）错峰自动打出 | 时点不同：StS1 在弃牌阶段前、StS2 在 Ethereal 之后 Flush 之前 |
| 弃牌阶段 | DiscardAtEndOfTurnAction（Retain→Ethereal→弃） | Flush（ShouldFlush 总闸 + 逐卡 retain + AfterFlush 分组） | "Retain 优于 Ethereal"在 StS2 天然成立（Ethereal 在 Phase One 已耗，Flush 才轮到 retain 判定） |
| 回合尾宝珠 | 哨兵链 TriggerEndOfTurnOrbsAction 入队（R07③） | DoTurnEnd 最先 OrbQueue.BeforeTurnEnd | StS2 宝珠先于回合尾卡与 flush |
| 结束回合输入 | 按钮追加 null 哨兵入 cardQueue（R10） | EndTurnSignal（等待 RunningAction 完成） | "回合尾与出牌流竞争"在 StS2 由 await 显式串行化 |
| 能量重置 | PlayerTurnEffect 构造器（energy-cost.md R03） | SetupPlayerTurn / PlayerTurnPhase.Start 内（能量重置在文档枚举里，位置 A05③） | 两侧都在"钩子梯之后、可出牌之前" |
| 每帧动作队列 | 五级容器 FIFO | GameAction 执行器 + Pause/Unpause 协作（ActionExecutor） | 无容器优先级；次序=await 拓扑 |

## 5. 开放问题 / 低置信项

1. `EndEnemyTurn(1076)` 收尾（敌侧回合尾 power tick、切边）未逐行展开。
2. `SwitchFromPlayerToEnemySide(1854)` 的 extra-turn 清理细节（PlayersTakingExtraTurn.Clear 已见，其余未展开）。
3. `StartCombatInternal(576)` 开局初始化（能量/抽牌/遗物开场钩子的确切顺序）未逐行展开——A05 已覆盖玩家侧 StartTurn 部分。
4. ChecksumTracker 的插桩位置清单（联机一致性用）未系统化。
