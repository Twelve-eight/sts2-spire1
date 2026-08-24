# ActionManager — 动作队列主循环与排序语义

> 本卷范围：`GameActionManager` 的帧驱动执行模型、五级队列优先级、插入/取消语义、ActionType 的真实用途。
> 出处标注：`类名#方法` 均可直接用 `javap -c -p` 对 desktop-1.0.jar（v2.x，含观者）复核。
> 置信度：**高** = 字节码直接可证；**中** = 字节码+调用链推断（注明环节）；**低** = 仅 wiki/间接证据。

## 0. 执行模型总览

游戏每帧调用 `AbstractRoom#update`（COMBAT 阶段）→ `GameActionManager#update`。管理器不是线程，是**单帧单步**的状态机：

```java
// GameActionManager$Phase 仅有两个值（javap -p GameActionManager$Phase）
enum Phase { WAITING_ON_USER, EXECUTING_ACTIONS }
```

- **R01【执行步进】** 每次 `GameActionManager#update`：WAITING_ON_USER 态调一次 `getNextAction()`；EXECUTING_ACTIONS 态若 `currentAction != null && !isDone` 则调该动作的 `update()` 一次。动作内部用 `duration -= Gdx.graphics.getDeltaTime()` 跨帧计时（`AbstractGameAction#tickDuration`：duration < 0 时置 `isDone=true`）。⇒ 移植时"一个动作占多帧、期间不并行任何其他动作"是硬约束。置信度高。

---

## 1. 五级取件优先级（getNextAction）

`GameActionManager#getNextAction`（私有）按以下固定顺序每帧取一件工作，**前一级非空时后级完全不推进**：

| 级 | 容器 | 取件方式 | 说明 |
|---|---|---|---|
| ① | `actions`（ArrayList） | `remove(0)` FIFO | 常规动作流 |
| ② | `preTurnActions` | `remove(0)` FIFO | 回合起始缓冲（见 R05） |
| ③ | `cardQueue`（CardQueueItem） | 只看头部，每帧处理一件 | 打牌请求；`card==null` 为回合尾哨兵 |
| ④ | `monsterQueue` | 头部逐只 | 敌方行动（见 turn-phase.md R08） |
| ⑤ | —（turnHasEnded 标志） | 新回合序列 | 见 turn-phase.md R10 |

- **R02【绝对优先级】** 上述顺序意味着：只要 `actions` 还有东西，`cardQueue` 里已排好的牌一张都不会结算。例：打出 Whirlwind 后其伤害动作在 `actions` 中逐段执行，此间新点的一张牌只是排队等待。字节码：getNextAction 开头三个 `isEmpty` 分支依次短路（offset 0→40→80）。置信度高。
- **R03【FIFO 与插队】** `addToBottom`= `actions.add(e)`（尾部）；`addToTop`= `actions.add(0, e)`（头部插队）。两者都先检查 `AbstractDungeon.getCurrRoom().phase == COMBAT`，非战斗房间静默丢弃。`AbstractGameAction#addToBot/addToTop` 只是转发到管理器同名方法。置信度高。
- **R04【跨战斗缓冲】** `addToNextCombat(e)` 把动作存入独立 `nextCombatActions` 列表；战斗开局初始化末尾由 `useNextCombatActions()` 一次性按序 `addToBottom` 并清空。用途：房间切换间隙预排的动作（如某些事件/遗物效果）。置信度高。
- **R05【preTurnActions】** `addToTurnStart(e)` = `preTurnActions.add(0, e)`——注意是**头插**：多次 addToTurnStart 后执行顺序为后加者先跑（LIFO），整体在 `actions` 清空后、`cardQueue` 之前以 FIFO 排空。本 build 内主要使用者是 `CardGroup`（洗回牌堆类操作）。置信度高。

---

## 2. cardQueue：打牌请求的处理细节

- **R06【逐帧处理头部】** cardQueue 分支每帧只处理队头一项：设置 `usingCard=true` → 读出 CardQueueItem → 结算或取消 → `remove(0)` → 返回。下一帧才轮到下一个。⇒ 多张自动打出的牌（Burst 复制、诅咒自动结算等）之间至少隔一帧，且其间允许 `actions` 插入的新动作先跑（回到 R02）。置信度高。
- **R07【canUse 门禁】** 队头牌先过 `card.canUse(player, monster)`；不通过且 `dontTriggerOnUseCard == false` → 取消路径：从 `player.limbo` 移除并播放 ExhaustCardEffect 视效，弹出 `cantUseMessage` ThoughtBubble（如能量不足时被 Burst 强行排队的牌）。`dontTriggerOnUseCard == true` 的强制牌跳过门禁。字节码 offset 302–343 与 1594–1698。置信度高。
- **R08【autoplay 补偿】** 若 canUse 失败但 `isInAutoplay==true`（回合尾强制自动打出的诅咒类）：处理完后会以 `dontTriggerOnUseCard=true` 重新 `addToBottom(new UseCardAction(card))` 兜底重入队（offset 1708–1739）。置信度高。
- **R09【目标死亡作废】** `target == ENEMY` 且 (`monster == null || monster.isDeadOrEscaped()`) → 牌作废：仅从 limbo 淡出 + ExhaustCardEffect 视效，`useCard` 完全不执行，也不触发 onPlayCard 系列（该分支在钩子调用之后、useCard 之前判断）。⇒ "指向已死敌人的复制牌不会造成伤害也不会触发打出钩子"。offset 1189–1540。置信度高。
- **R10【回合尾哨兵】** `CardQueueItem.card == null` 的项是**回合结束标记**：处理到它时调用私有 `callEndOfTurnActions()`（回合尾钩子链，见 turn-phase.md R07），然后移除。生产者：`NewQueueCardAction#update` / `QueueCardAction#update` 的无卡构造路径——`queueContainsEndTurnCard()` 保证不重复添加；点击结束回合按钮时 `EndTurnButton#disable(boolean)` 会 `addToBottom(new NewQueueCardAction())` 从而把哨兵追加到当前动作流的尾部。置信度高。
- **R11【Unceasing Top 抑制】** 当 cardQueue 只剩最后一项且该项 `isEndTurnAutoPlay==true` 时，若玩家有无尽之顶（Unceasing Top）则 `disableUntilTurnEnds()`，防止回合尾自动打牌无限触发抽牌循环。offset 167–215。置信度高。

---

## 3. ActionType 枚举：真实语义

- **R12【枚举清单】** `AbstractGameAction$ActionType` 共 18 个值：`BLOCK, POWER, CARD_MANIPULATION, DAMAGE, DEBUFF, DISCARD, DRAW, EXHAUST, HEAL, ENERGY, TEXT, USE, CLEAR_CARD_QUEUE, DIALOG, SPECIAL, WAIT, SHUFFLE, REDUCE_POWER`。置信度高（javap -constants）。
- **R13【不存在逐类型节拍】** 本 build 的 `getNextAction/update` **不按 ActionType 做"同类连发、异类停顿"的排序或节流**；`actionType` 字段在管理器中仅两处消费：
  1. `clearPostCombatActions()`：战斗结束时清扫 `actions`，保留 `HealAction / GainBlockAction / UseCardAction / actionType==DAMAGE` 四种，其余全部移除；
  2. `UseCardAction` 构造器：`exhaustCard ? EXHAUST : USE`（纯标记用途）。
  ⇒ 移植仲裁时不要引入 wiki 口口相传的"类型相位"模型；唯一真实的排序就是 R02 的容器优先级 + FIFO。（任务书假设的 `actionTypePhase` 枚举在本 jar 不存在。）置信度高（全量 getNextAction/update 反汇编无其他 actionType 比较）。
- **R14【WAIT 类型的特殊性】** `actionType==WAIT` 无专门分支（R13 已述）；`utility.WaitAction` 就是普通 duration 动作，用于节奏垫片（怪物队列清空后 Wait(1.5s)、结束回合后 Wait(1.2s) 等）。置信度高。

---

## 4. 取消与完成语义

- **R15【shouldCancelAction】** `AbstractGameAction#shouldCancelAction()` = `target == null || source.isDying || target.isDeadOrEscaped()`。时长型动作（tickDuration 派生类，如 DamageAction/GainBlockAction）惯例在 `update()` 开头检查它来中途放弃。⇒ "来源濒死时其后续伤害动作静默失效"。置信度高。
- **R16【完成即弃】** 动作没有"回滚"；`isDone=true` 后对象被丢弃（currentAction=null，previousAction 留引用）。队列中的动作对象在入队后仍可改字段（各 Action 构造器大量捕获闭包状态），但管理器不做防御性拷贝。置信度高。

---

## 5. 全局计数器与清理

- **R17【静态计数器族】** `GameActionManager` 持有跨回合统计：`totalDiscardedThisTurn, damageReceivedThisTurn/ThisCombat, hpLossThisCombat, playerHpLastTurn, energyGainedThisCombat, turn`。复位点：`clear()`（新战斗，turn=1）、新回合块内（per-turn 三项清零，见 turn-phase.md R10）。`updateEnergyGain(n)` 由 `GainEnergyAndEnableControlsAction` 调用以累计 energyGainedThisCombat。置信度高。
- **R18【手动弃牌通知】** `incrementDiscard(boolean evoFire)`：`totalDiscardedThisTurn++`；若 `!turnHasEnded && !evoFire` → 同步直调 `player.updateCardsOnDiscard()`（刷新手牌描述）+ 遍历遗物 `onManualDiscard()`。⇒ 回合结束后（turnHasEnded=true）的弃牌不再触发手动弃牌遗物。置信度高。
- **R19【queueExtraCard】** 静态工具：为 X 费牌生成 `makeSameInstanceOf()` 副本放入 limbo，随机屏幕定位，`calculateCardDamage(m)` 快照，置 `purgeOnUse=true`，以 `addCardQueueItem(item, true)` **插到 cardQueue index 1**（紧跟当前正在处理的项之后）。Necronomicon/Amplify/Burst/DoubleTap/Duplication/Echo 等复制器均经 `addCardQueueItem` 入队。置信度高。
- **R20【hasControl 门控输入】** 取件到 actions/preTurn 时置 `phase=EXECUTING_ACTIONS, hasControl=true`；队列彻底排空且非出牌态时置 `WAITING_ON_USER, hasControl=false`。`AbstractPlayer` 的 `endTurnQueued → isEndingTurn` 转换要求 `actions.isEmpty() && !hasControl`——这保证"结束回合请求只在动作流完全静止后生效"（完整链条见 turn-phase.md R06）。置信度高。

---

## 6. 开放问题 / 低置信项

- `previousAction`、`turnStartCurrentAction` 字段的读取方未逐一追踪（推测仅调试/UI 用），不影响排序结论。
- `ActionLogEntry`（actions 包根）未被管理器引用，疑似废弃。
- 契约假设的 "BEFORE/DEBUFF/DRAW_BLOCK phase 排序" 在本版本无对应实现；若 StS2 设计文档源自旧 mod 文档，需按本文档 R02/R13 校正。
