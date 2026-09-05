# 回合控制：窥视/跳过/额外回合（Scry, Skip, Extra Turn）— StS1 战斗语义知识库

## 本卷范围
观者窥视（Scry）完整时序、"跳过敌方回合"（Vault/SkipEnemiesTurnAction → room.skipMonsterTurn 的全部消费者）、"提前结束回合"（PressEndTurnButtonAction/TimeWarp → callEndTurnEarlySequence）。三者的共同点：都是**改写回合流水线**的机制，消费者分散在多处，移植时极易漏。
依赖：回合流水线 → `turn-phase.md`；callEndTurnEarlySequence 本体 → 同卷 R15。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=推断（注明）。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 窥视（ScryAction）

**R01 ScryAction 完整序** — 出处 `actions/utility/ScryAction#update` offset 0-329。置信度：**高**
```
首帧：
① 全场基本死 → isDone
② player.powers 逐个 onScry()（同步直调；vanilla 实现者 = NirvanaPower，power-lifecycle.md R12）
③ 抽牌堆空 → isDone
④ 组 UNSPECIFIED 临时 CardGroup：
     amount != -1：取抽牌堆"顶" min(amount, dpSize) 张（列表尾部起倒序取）→ addToTop
     amount == -1：整堆正序 addToBottom
⑤ GridCardSelectScreen.open(组, amount, anyNumber=true, TEXT[0])（任意张数可 0）
确认帧（duration < startDuration 后）：
⑥ 选中卡逐张 drawPile.moveToDiscardPile(card)（弃牌堆"顶"=列表尾）
⑦ 清空选择
⑧ 弃牌堆全部卡逐张 triggerOnScry()（同步直调——对"任何窥视发生"作出反应的卡）
⑨ tickDuration
```
要点：未选中的牌**原地留在抽牌堆**（窥视不洗牌不重排）；选中的从"顶"进弃牌堆（后选的先入？逐张 addToTop of discardPile —— moveToDiscardPile 内部是 addToTop，P2 语义 ⇒ 按选择顺序压栈，最后选择的在弃牌堆最上）。UI 字符串 ID 是 "ReprogramAction"（vanilla 复制粘贴残留，M5 常量池陷阱的活例）。

---

## 2. 跳过敌方回合（skipMonsterTurn）

**R02 写入者全集（3 处）** — 出处 常量池扫描 + javap。置信度：**高**
```
SkipEnemiesTurnAction#update:  room.skipMonsterTurn = true; isDone（单帧直置）
SmokeBomb#use:                 （间接——玩家 isEscaping/room.smoked 走逃跑路径，potions-combat.md R03）
EndTurnButton 相关：无
```
vanilla 中 `SkipEnemiesTurnAction` 的唯一队列化调用者 = `cards/purple/Vault`（常量池扫描全量）。

**R03 skipMonsterTurn 的全部消费点（漏一个就移植错）** — 出处 常量池扫描 + 各点 javap。置信度：**高**
| 消费点 | 行为 | 出处 |
|---|---|---|
| `EndTurnAction#update` | skip → **不**加 EnemyTurnEffect 视效 | offset 9-25 |
| `GameActionManager#getNextAction` 新回合块 | skip → **整段 `MonsterGroup.applyEndOfTurnPowers()` 跳过** ⇒ 怪物 atEndOfTurn(false)、玩家 atEndOfRound、怪物 atEndOfRound 全部不跑（**玩家 debuff 这轮也不递减**！Vulnerable/Weak 等，power-lifecycle.md R05） | offset 2002-2017（death-arbitration.md R17 引） |
| `GameActionManager` 新回合块尾 | `skipMonsterTurn=false` 复位（一次性） | offset 2100-2104 |
| `AbstractRoom$1`（endTurn 尾匿名动作）/ AbstractRoom | 怪物回合启动侧的跳过判定 | turn-phase.md R05/R12 语境 |
⇒ Vault 的准确语义不是"怪物跳过回合"而是"怪物整回合不活动 + 怪物回合尾钩子整段不跑"；增益/减益时长仲裁受影响。

---

## 3. 提前结束 / 额外回合

**R04 PressEndTurnButtonAction = callEndTurnEarlySequence 的队列化包装** — 出处 `actions/watcher/PressEndTurnButtonAction#update` offset 0-11（单帧同步直调后 isDone）。置信度：**高**

**R05 TimeWarpPower（时间吞噬者）** — 出处 `powers/TimeWarpPower#onAfterUseCard` offset 0-96。置信度：**高**
`onAfterUseCard`：计数 ++ → ==12 归零 + 音效 → **同步直调 `GameActionManager.callEndTurnEarlySequence()`**（强制把 cardQueue 中 autoplay 项立即转 UseCardAction、清空 limbo、禁用结束按钮，turn-phase.md R15）→ 尾部 addToBot（吞噬者自益效果）。计数挂在 **onAfterUseCard**（UseCardAction 执行首帧）而非 onUseCard（构造期）⇒ "被复制的牌也算次数、且按结算完成顺序计数"。

**R06 callEndTurnEarlySequence 与正常结束回合是两条不同路径** — 出处 `GameActionManager#callEndTurnEarlySequence`（turn-phase.md R15）。置信度：**高**
正常路径 = 结束按钮 → NewQueueCardAction 追加 null 哨兵 → 哨兵链（R07 turn-phase）→ 弃牌阶段；提前路径 = 直调 callEndTurnEarlySequence，**不走哨兵链的 relics.onPlayerEndTurn/PreEndTurnCards/球触发/stance.onEndOfTurn**（该链属哨兵处理）。⇒ TimeWarp/Vault 触发的"回合提前结束"会少跑一整段回合尾钩子——移植仲裁必须分链处理。

---

## 4. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| 窥视时 Nirvana 在场 | onScry 同步直调（选牌界面打开**之前**） | R01② |
| 窥视把攻击牌选去弃牌堆 | 后选择的在弃牌堆更"顶"（下一洗牌先出） | R01⑥ + P2 |
| Vault 后本回合挂的 Vulnerable | 怪物 atEndOfRound 整段跳过 ⇒ 玩家侧 debuff 这轮**不递减** | R03 |
| Time Eater 满 12 张 | 立刻打断当前出牌流（callEndTurnEarly 同步） | R05 |
| 提前结束的回合尾 | 哨兵链（onPlayerEndTurn/球/姿态）不跑 | R06 |
| SmokeBomb vs Vault | 走逃跑态不同标志；skipMonsterTurn 只有 Vault 走 | R02 + potions-combat.md R03 |

---

## 5. 开放问题 / 低置信项

1. `AbstractRoom$1` 匿名动作对 skipMonsterTurn 的确切分支（turn-phase.md 开放问题遗留）未逐字节展开。置信度：**中**。
2. triggerOnScry 的卡牌实现者清单未穷举（钩子调用点已证）。置信度：**低**。
3. ScryAction 的 uiStrings ID "ReprogramAction" 是否影响其他逻辑——纯字符串复用，无。置信度：**高**（已核）。
