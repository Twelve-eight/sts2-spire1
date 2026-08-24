# Turn Phase — 回合流水线完整时序

> 本卷范围：从战斗开局到回合循环的完整事件顺序：开局初始化 → 玩家回合（能量/抽牌/出牌/弃牌）→ 敌方回合 → 新回合序列。
> 动作队列机制本身见 `action-manager.md`；抽牌/洗牌/消耗区细节见 `draw-exhaust.md`；伤害数值见 `damage-pipeline.md`；状态叠加见 `status-stacking.md`；钩子清单见 `triggers.md`。
> 置信度：**高** = 字节码直接可证；**中** = 字节码+调用链推断；**低** = 仅 wiki。

## 0. 一图流（玩家视角的一个完整回合）

```mermaid
flowchart TD
    A[战斗进入 COMBAT 房间] --> B[waitTimer 0.1s 到期: 开局初始化块]
    B --> C["GainEnergyAndEnableControlsAction(3)
    → 能量到手 + onEnergyRecharge + 关闭 turnHasEnded 闩"]
    C --> D[DrawCardAction(player, gameHandSize=5)]
    D --> E[EnableEndTurnButton]
    E --> F[玩家出牌循环: cardQueue/actions 交替]
    F --> G[点击结束回合]
    G --> H[NewQueueCardAction 追加 null 哨兵到 cardQueue]
    H --> I[哨兵触发 callEndOfTurnActions:
    遗物→前置power→宝珠排队→手牌回合尾触发→姿态]
    I --> J[cardQueue 中自动结算的诅咒/状态牌]
    J --> K[队列全空 → room.endTurn:
    powers.atEndOfTurn → 清队列 → DiscardAtEndOfTurn
    → EndTurnAction(turnHasEnded=true) → MonsterStartTurn]
    K --> L[怪物逐个 takeTurn]
    L --> M[新回合块: 怪 atEndOfTurn/atEndOfRound →
    玩家 start-of-turn 钩子梯 → turn++ → 掉格挡
    → 抽牌入队 → PostDraw 钩子 → 按钮亮起]
    M --> F
```

---

## 1. 战斗开局（第一回合特殊点）

入口：`MonsterRoom#onPlayerEntry` 只做 BGM/怪物组懒加载并置 `waitTimer=0.1f`；真正的初始化在 **首个 COMBAT 帧的 `AbstractRoom#update`**（RoomPhase.COMBAT 分支，waitTimer 归零后执行一次）：

- **R01【开局直调钩子先于初始抽牌到达】** 开局块内同步直调顺序：
  1. `actionManager.turnHasEnded = true`（防重入闩，见 R03）
  2. BattleStartEffect（视效）
  3. `addToBottom(GainEnergyAndEnableControlsAction(player.energy.energyMaster))`
  4. `player.applyStartOfCombatPreDrawLogic()` → 遗物 `atBattleStartPreDraw()`（**同步直调**）
  5. `addToBottom(DrawCardAction(player, gameHandSize))` ← 初始手牌（2 参构造，无 PlayerTurnEffect 视效）
  6. `addToBottom(EnableEndTurnButtonAction)`
  7. showCombatPanels
  8. `player.applyStartOfCombatLogic()` → 遗物/blight `atBattleStart()`
  9. 日替 mod 检查（Careless / ControlledChaos）
  10. `skipMonsterTurn = false`
  11. `applyStartOfTurnRelics()`（姿态 `atStartOfTurn` → 遗物 `atTurnStart` → blight）
  12. `applyStartOfTurnPostDrawRelics()` → `atTurnStartPostDraw`
  13. `applyStartOfTurnCards()` → 抽牌堆/手牌/弃牌堆各卡的 `atTurnStart`
  14. `applyStartOfTurnPowers()` → power `atStartOfTurn`（AbstractCreature 版）
  15. `applyStartOfTurnOrbs()`
  16. `useNextCombatActions()`（冲刷跨战斗缓冲）

  出处：`AbstractRoom#update`（COMBAT 分支 offset 219–385）。置信度高。
- **R02【首回合能量只发一次】** 每场战斗基础能量 = `EnergyManager.energyMaster`（角色基础值，通常 3），由开局队列第一个动作发放。**新回合块不补发能量——未用完的能量跨回合保留**。`EnergyPanel#addEnergy` 上限 999，≥9 解锁 ADRENALINE 成就。⇒ 移植常见误区："每回合重置为 3" 是错的。置信度高。
- **R03【turnHasEnded 闩】** 开局把 `turnHasEnded=true` 后，若不解除，动作排空时会误触"新回合块"造成双重抽牌。实际由 **`GainEnergyAndEnableControlsAction#update` 的最后一行 `actionManager.turnHasEnded=false`** 解除（发完能量才放行新回合检测）。这是理解开局为何恰好抽 5 张的关键字节码事实。置信度高。
- **R04【PostDraw 命名陷阱】** 无论开局还是后续新回合块，`atTurnStartPostDraw`（遗物）/`atStartOfTurnPostDraw`（power）都是**紧跟 DrawCardAction 入队之后同步直调**——此时抽牌动作还在队列里未执行，卡尚未进手牌。语义是"排在抽牌之后入队"，不是"抽完之后"。依赖'已抽到手'状态的逻辑必须挂别的钩子。出处：`AbstractRoom#update` offset 355 与 `GameActionManager#getNextAction` offset 2205–2214。置信度高。
- **R05【怪物开局不动】** 开局不 rollMove、不行动；`monsterAttacksQueued` 构造时即为 true，直到第一次结束回合才被 `AbstractRoom$1#update` 复位为 false 并 `queueMonsters()`。怪物的首次意图 roll 在各自 `usePreBattleAction/useUniversalPreBattleAction`（`MonsterGroup#init` 后经 `preBattlePrep` 路径调用，跳过读档场景 `loading_post_combat`）。置信度高。
- **R06【结束回合点击链】** 点击按钮 → `EndTurnButton#disable(true)`：`addToBottom(NewQueueCardAction())` + `player.endTurnQueued=true` + releaseCard。`NewQueueCardAction` 在现有动作流尾部执行，向 `cardQueue` 追加 null 哨兵。当 `cardQueue.isEmpty() && !hasControl` 时 `endTurnQueued` 才转为 `isEndingTurn=true`，随后帧内 `AbstractRoom#update` 调 `room.endTurn()`（字节码：apc.txt offset 994–1012，字面检查 cardQueue；因 hasControl 仅在全部容器排空时才为 false，与检查 actions 等价）。⇒ 从点击到真正弃牌之间，还夹着整个"回合尾钩子 + 自动结算牌"窗口（见 §2）。置信度高。
---

## 2. 回合尾（点击结束回合之后）

- **R07【callEndOfTurnActions 固定序】** 哨兵触发 `GameActionManager#callEndOfTurnActions`：
  1. `room.applyEndOfTurnRelics()` → 遗物 `onPlayerEndTurn()`（后 blight 同名）
  2. `room.applyEndOfTurnPreCardPowers()` → power `atEndOfTurnPreEndTurnCards(true)`
  3. `addToBottom(TriggerEndOfTurnOrbsAction)` —— 注意是**入队**非直调；因 actions 优先级高于 cardQueue（action-manager.md R02），它会在下一帧抢先于 cardQueue 里刚生成的自动结算牌执行：遍历宝珠 `orb.onEndOfTurn()`，有 Cables 遗物再额外触发一次
  4. 遍历手牌直调 `card.triggerOnEndOfTurnForPlayingCard()` —— Regret/Shame/Decay/Doubt/Burn 等在此把自身以 `dontTriggerOnUseCard=true` 追加进 cardQueue
  5. `stance.onEndOfTurn()`

  置信度高。
- **R08【诅咒/状态牌自动结算窗口】** 手牌回合尾触发生成的 cardQueue 项在 `actions` 排空后逐张按正常出牌流水结算（但免 onUseCard 钩子、免能量）。它们**先于**弃牌阶段执行——Regret 按"弃牌前手牌数"计伤即由此保证。置信度高。
- **R09【真正的弃牌阶段位置】** 自动结算牌全部出清、队列完全静止后：`isEndingTurn → AbstractRoom#endTurn()`：
  1. `player.applyEndOfTurnTriggers()` → power `atEndOfTurn(true)`（**注意与 R07.2 是两个不同钩子**；玩家的 PreEndTurnCards 已在 R07.2 发过，此处不再发）
  2. `addToBottom(ClearCardQueueAction)`（清残留 limbo 项并 `cardQueue.clear()` 兜底）
  3. `addToBottom(DiscardAtEndOfTurnAction)` ← Retain/Ethereal 判定与弃牌发生在这里（内部机制见 draw-exhaust.md）
  4. 三堆+悬停卡全部 `resetAttributes()`
  5. `addToBottom(AbstractRoom$1)` = { EndTurnAction → Wait(1.2s) → MonsterStartTurnAction }
  
  置信度高。
- **R10【敌方回合准备】** `EndTurnAction#update` → `GameActionManager#endTurn()`（仅三件事：`resetControllerValues()`、`turnHasEnded=true`、记录 `playerHpLastTurn`）+ EnemyTurnEffect 视效。随后 `MonsterStartTurnAction` → `MonsterGroup#applyPreTurnLogic`：对每只存活怪依次 **掉格挡（除非该怪有 Barricade power）→ `monster.applyStartOfTurnPowers()`（power.atStartOfTurn）**。置信度高。
- **R11【玩家与怪物掉格挡时机不对称】** 玩家格挡在新回合块内（自己下回合开始前）扣除；怪物格挡在敌方回合开始前扣除。两者都吃 Barricade 类豁免（Calipers 对玩家保 15 点，见 R13）。置信度高。
- **R12【怪物逐个行动】** `getNextAction` 尾部：`monsterAttacksQueued false→true` 时 `queueMonsters()`（存活或 halfDead 者入队）；此后每帧队头一只：intent≠NONE 则先 `ShowMoveNameAction`+`IntentFlashAction` 入队（下一帧才轮到 takeTurn！），然后 `m.takeTurn(); m.applyTurnPowers()`（power.duringTurn），出队；队列清空后垫 Wait(1.5s)。置信度高。
- **R13【新回合块（敌方回合结束后）】** `GameActionManager#getNextAction` 末段（offset 1983–2228），条件 `turnHasEnded && !areMonstersBasicallyDead`：
  1. `!skipMonsterTurn` → `MonsterGroup#applyEndOfTurnPowers`：存活怪各 `applyEndOfTurnTriggers()`（power `atEndOfTurnPreEndTurnCards(false)` + `atEndOfTurn(false)`）；然后**玩家 powers 全部 `atEndOfRound()`**；再怪物 powers `atEndOfRound()`
  2. 计数复位：`cardsPlayedThisTurn=0`、`orbsChanneledThisTurn.clear`、日替 mod、`totalDiscardedThisTurn=0`、`damageReceivedThisTurn=0`、`cardsPlayedThisTurn` 列表 clear
  3. 玩家 start-of-turn 钩子梯（顺序同 R01.11–15，全部同步直调）
  4. `turn++`
  5. 玩家掉格挡：Barricade power 或 Blur power 免除；Calipers 遗物 `loseBlock(15)`，否则 `loseBlock()` 清零
  6. `!isBattleOver` → `addToBottom(DrawCardAction(null, gameHandSize, true))`（3 参构造带 PlayerTurnEffect 回合横幅视效）→ 直调 `applyStartOfTurnPostDrawRelics()/PostDrawPowers()`（见 R04 陷阱）→ `addToBottom(EnableEndTurnButtonAction)`

  置信度高。
- **R14【回合计数基准】** `turn` 静态字段：战斗开始 `clear()` 置 1；每个新回合块 ++。因此**敌人行动发生在计数器已 ++ 之后**；"第 N 回合开始"判定应以新回合块完成后的值为准。`skipMonsterTurn`（TimeWarp 等额外回合机制用）在新回合块末尾复位 false。置信度高。
- **R15【提前结束回合路径】** `callEndTurnEarlySequence()`（观者 PressEndTurnButtonAction / TimeWarpPower 调用）：强制把 cardQueue 中所有 `autoplayCard==true` 的项立即转成 UseCardAction 结算，清空 limbo（视效 poof）、releaseCard、禁用结束按钮。用于跳过剩余出牌窗口的特殊机制。置信度高。

---

## 3. 出牌窗口内的微时序

- **R16【单张牌完整链】** 手牌点出（`AbstractPlayer#playCard` 把 CardQueueItem 追加进 cardQueue）→ 队头处理（canUse 门禁，见 action-manager.md R07）→ `player.useCard(card, m, energyOnUse)`：
  1. ATTACK 类型播放快攻动画
  2. **`card.calculateCardDamage(m)`** ← 伤害快照在此时点计算一次（详见 damage-pipeline.md）
  3. X 费牌（cost==-1）能量不足时钳制 `energyOnUse = EnergyPanel.totalCount`
  4. `card.use(player, m)` ← 卡牌效果此刻把自己的一串 Action addToBottom/addToTop
  5. `addToBottom(new UseCardAction(card, m))` —— 其**构造器同步**触发：玩家 powers.onUseCard → 玩家 relics.onUseCard → 手牌 triggerOnCardPlayed → 弃牌堆 triggerOnCardPlayed → 抽牌堆 triggerOnCardPlayed → 全部怪物 powers.onUseCard
  6. `!dontTriggerOnUseCard` → `hand.triggerOnOtherCardPlayed(card)`
  7. 从手牌 removeCard，置 cardInUse
  8. 扣能：`costForTurn>0 && !freeToPlay && !isInAutoplay && !(Corruption&&SKILL)` → `energy.use(costForTurn)` —— **扣能在效果入队之后**
  
  出处：`AbstractPlayer#useCard` 全文。置信度高。
- **R17【UseCardAction 双阶段】** 该动作 duration=0.15f：第一帧 update 直调玩家/怪物 powers 的 `onAfterUseCard(card, action)`；末帧按 purgeOnUse → 直接 ShowCardAndPoof（牌凭空消失，不入任何堆）；否则 POWER 牌走 hand.empower 移除展示，普通牌按 rebound/shuffleBackIntoDrawPile/returnToHand 优先级分流，默认 `moveToDiscardPile`；exhaust 属性牌走 `moveToExhaustPile`（Strange Spoon 50% 改道弃牌堆，POWER 牌不受勺子影响）。⇒ "打出消耗牌"的消耗发生在其效果动作全部入队之后的收尾帧。置信度高。
- **R18【能量不足的视觉反馈】** `hand.canUseAnyCard()==false` 且未排队结束时，结束按钮发光提示（`useCard` 尾部）。纯 UI，不影响时序。置信度高。

---

## 4. 开放问题 / 低置信项

- `AbstractPlayer#applyPreCombatLogic`（遗物 `atPreBattle` 的分发器）在本 build **没有任何外部调用者**（全量类名引用扫描确认）。实现它的遗物（Lantern、Cracked Core、Nuclear Battery、Snecko Eye 等）实际生效必然经由其他钩子（多数同时覆写了 atBattleStart 族）——移植时不要给 atPreBattle 留调度点，但需逐遗物核对真实挂点（归 triggers.md 详查）。
- `EnergyManager.prep()` 与 `SlaversCollar.beforeEnergyPrep()` 出现在 `preBattlePrep` 内（apc.txt offset ~5200），说明能量面板每战重置发生在 preBattlePrep 阶段；与 R02 不冲突（energyMaster 是上限模板）。
- Room$1 匿名动作里 Wait(1.2s) 仅作演出间隔，移植可压缩，但注意它把 EndTurnAction 与 MonsterStartTurnAction 分隔成两帧以上。
