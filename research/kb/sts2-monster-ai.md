# StS2 怪物 AI 框架（Monster AI, EA build）— sts2-spire1 知识库

## 本卷范围
StS2 怪物行为框架的完整仲裁依据：MonsterMoveStateMachine 状态机（MoveState/ConditionalBranchState/MonsterState）、意图（Entities.Intents）、RollMove/PerformMove 时点、每回合快照（AmountOnTurnStart）、格挡清除（ShouldClearBlock preventer）、敌人回合执行循环、空手检查（CheckForEmptyHand 设计注记）。
来源：`research/engine-dllsrc/`（C#）。对照 StS1：`../sts1-kb/mechanics/monster-ai.md`。

---

## 1. 状态机模型

**A01 MonsterState 抽象 + MoveState 可执行节点** — 出处 `MonsterMoves.MonsterMoveStateMachine/MonsterState.cs` 与 `MoveState.cs` 全文。置信度：**高**
```
MonsterState: Id / ShouldAppearInLogs / CanTransitionAway / IsMove /
              GetNextState(owner, rng) / RegisterStates / OnEnterState / OnExitState
MoveState:    onPerform lambda(目标列表) + Intents 列表(展示用) + FollowUpState(Id/对象) +
              MustPerformOnceBeforeTransitioning（CanTransitionAway = 至少执行过一次）
              PerformMove: 置 _performedAtLeastOnce → await onPerform(targets)
              OnExitState: 复位 _performedAtLeastOnce
```
非 move 状态 = 过路节点（如 ConditionalBranchState，`ShouldAppearInLogs=false`、`IsMove=false`）——状态机 roll 时**循环推进直到落在 MoveState**（A02）。每个 MoveState 自带 Intents（SingleAttackIntent/MultiAttackIntent/DefendIntent/DebuffIntent/BuffIntent/HealIntent/StatusIntent/SummonIntent/EscapeIntent/SleepIntent/DeathBlowIntent/StunIntent/HiddenIntent/UnknownIntent/CardDebuffIntent——`MonsterMoves.Intents/` 全目录）供 UI 展示，**意图与实际执行的 onPerform lambda 解耦**（改显示不改变行为，反之亦然）。

**A02 RollMove 推进规则** — 出处 `MonsterMoveStateMachine.cs#RollMove/#FindNextMoveState`（行 34-80）。置信度：**高**
```
FindNextMoveState:
  当前 CanTransitionAway==false → 不动
  !_performedFirstMove && 当前是 MoveState → 不动   ★ 开局首个 move 固定 = 初始状态
  do { next = currentState.GetNextState(owner, rng);   // rng = RunRng.MonsterAi 独立流
       SetCurrentState(next 或 initialState)           // OnExit → OnEnter
     } while (!currentState.IsMove);                   // 穿过非 move 节点直到可执行
  StateLog 记录（ShouldAppearInLogs 者）
```
- **ConditionalBranchState.GetNextState = 按 AddState 顺序取第一个条件为真的分支**（确定性，不掷骰；骰子在分支条件内部调用 rng）。
- `MoveState.FollowUpState` 强制链（连招）；`MonsterModel.SetMoveImmediate(state, forceTransition)` 供外部（power/卡）强制改意图，受 `CanTransitionAway || force` 门（低血量转阶段类）。
- StS1 对照：StS1 是"getMove(roll) 手写模式匹配 + lastMove 族历史读取"；StS2 是显式 FSM + 条件分支节点，`MonsterModel.Rng`（行 101 起）= **每怪独立 Rng 流**（与 RunRng.MonsterAi 并用，A02）。

## 2. 回合时点

**A03 RollMove 的两个触发点** — 出处 `CombatManager.cs`：`AfterCreatureAdded(Creature, state)`（行 1126-1136：**敌人进场且当前是玩家侧** → 立即 RollMove = 初始意图）；回合循环（敌人回合前，与 StS1 对齐——本卷开放问题 1 留待逐帧核对回合循环内的 RollMove 位置）。置信度：**高**（进场即 roll）/ **中**（循环内 roll 的确切位置）。

**A04 敌人回合执行循环** — 出处 `CombatManager.cs#ExecuteEnemyTurn`（行 1405-1444）+ `Creature.cs#TakeTurn`（行 711-721）。置信度：**高**
```
actionDuringEnemyTurn（额外动作位，先跑）→
foreach 存活敌人（快照列表，ContainsCreature 复查）:
    nCreature.PerformIntent()（视觉层）
    await enemy.TakeTurn()  ⇒ !Monster.SpawnedThisTurn 才 PerformMove
                              PerformMove: wait 0.1-0.2 → IsPerformingMove=true →
                              move.PerformMove(targets=PlayerCreatures) →
                              MoveStateMachine.OnMovePerformed → History.MonsterPerformedMove →
                              死亡且应移除 → combatState.RemoveCreature → wait 0.1-0.4
    WaitForUnpause → CheckWinCondition（逐怪之间！）
EndEnemyTurn
```
`SpawnedThisTurn`（MonsterModel 行 248）⇒ **本回合召唤的敌人首次出现在场上不执行 move**（SetUpForCombat 置位，回合切换清零——由回合循环管理）。

**A05 每回合开始的双钩子 + 格挡清除** — 出处 `Creature.cs#BeforeTurnStart/#AfterTurnStart/#ClearBlock`（行 692-733）+ `CombatManager.cs#StartTurn`（行 688-864）。置信度：**高**
```
StartTurn(side):
  creaturesStartingTurn = 当前侧生物（或 extra-turn 玩家）
  ① 逐怪 BeforeTurnStart：power.AmountOnTurnStart = power.Amount   ★ 快照
  ② Hook.BeforeSideTurnStart
  ③ 玩家侧：Phase=Start、能量/抽牌 Setup（SetupPlayerTurn 异步、联机 pause context）
     敌方侧：敌回合横幅
  ④ 逐怪 AfterTurnStart：玩家首回合跳过 → ClearBlock():
        Hook.ShouldClearBlock(state, creature, out preventer) 为真 → Block=0
        为假 → Hook.AfterPreventingBlockClear（Barricade 类 = preventer 实现，非特判！）
  ⑤ 逐怪 Hook.AfterBlockCleared
  ⑥ Hook.AfterSideTurnStart；玩家侧再逐玩家 OrbQueue.AfterTurnStart
  ⑦ AutoPrePlay 阶段（AutoPrePlay → CheckForEmptyHand → AfterAutoPrePlayPhaseEntered → Phase=Play）
```
**A06 AmountOnTurnStart 快照的用途** — 出处 `Creature.cs#BeforeTurnStart`（行 692-698）。置信度：**中**（快照事实高；消费方=时长递减类钩子的推断，见 sts2-combat-semantics.md S11 的 SkipNextDurationTick——两机制并存：施加当回合用 skip 旗，跨回合对比用快照）。

**A07 空手检查的设计注记（Unceasing Top 教训）** — 出处 `CombatManager.cs#CheckForEmptyHand` 文档注释（行 1170-1200）。置信度：**高**
空手钩子**不在手牌数变化时检查**，而在：出牌完成后、药水使用后、AutoPrePlay 进入时——否则"手牌仅剩 Pommel Strike + 无尽之顶"会在效果抽牌前误抽一张。移植 StS1 侧对应物：`hand.isEmpty` 触发器务必挂在"动作完成后"。

## 3. StS1 → StS2 怪物仲裁对照

| 语义 | StS1（monster-ai.md） | StS2（本卷） | 仲裁建议 |
|---|---|---|---|
| move 选择 | getMove(roll) 手写 + lastMove 历史 | 显式 FSM + ConditionalBranch + FollowUpState | 移植 StS1 怪物时把手写模式翻译成小 FSM；反向移植用 GetNextState 内手写 |
| 首个意图 | usePreBattleAction 手动 roll | 进场即 RollMove（A03），首个 move 固定初始状态（A02） | StS1 的"进场 first roll"在 StS2 侧由 FSM 初始状态天然覆盖 |
| 意图刷新 | power 变化→onModifyPower→重算 | `SetMoveImmediate`→RefreshIntents + Intent 对象展示 | 快照时点不同，逐怪回归 |
| 本回合召唤不行动 | halfDead 机制（triggers.md R12） | SpawnedThisTurn（A04） | 语义等价但实现不同；halfDead 的"复活不结束战斗"另寻对应物 |
| 格挡清除 | 中央门控 Barricade/Blur/Calipers（defense-powers.md R10） | ShouldClearBlock preventer 钩子（A05） | StS1 三件套在 StS2 侧应实现为 preventer |
| 毒杀吞回合 | 毒在 owner 回合开始（R01 power-lifecycle） | poison tick 时点未在卷内终验（开放问题 2） | 待证 |

## 4. 开放问题 / 低置信项

1. 回合循环内（非进场）的 RollMove 调用点逐帧位置（A03 中置信项）。
2. StS2 毒/DoT 的结算时点与"杀死即吞回合"是否与 StS1 一致。置信度：**未定**。
3. `MonsterState.GetNextState` 在 MoveState 子类（非 FollowUp 的随机 move 选择，如 BestiaryMonsterMove 相关）的随机实现样例未枚举。
4. EndEnemyTurn 的收尾细节（回合尾 power tick 的调用链）归 CombatManager 卷。
