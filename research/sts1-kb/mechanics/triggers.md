# triggers.md — StS1 触发器（Hook）调用点与时序知识

## 本卷范围
本文件从**触发器视角**记录 StS1（desktop-1.0.jar，v2.x 含观者）中 power / relic / card / stance / blight 各类钩子方法的：完整清单、精确调用点、相互顺序、同步直调 vs 队列化的区别。为 StS2 移植仲裁提供「谁在什么时刻收到什么通知」的确定性依据。
- 出牌主循环 / ActionManager 队列语义细节 → 见 `action-manager.md`、`turn-phase.md`（主代理），本文只引用其结论。
- 抽牌堆/弃牌堆/消耗区堆内移动细节 → `draw-exhaust.md`。
- 伤害数值管线（加减伤数学）不展开，只记录围绕它的**钩子通知序**。

图例：出处格式 `类名#方法`；字节码偏移如 `[offset 622]` 指 `javap -c -p` 输出中的指令偏移。置信度：**高**=javap 字节码直接可证；**中**=字节码+调用链推断（注明推断环节）；**低**=仅 wiki/间接证据。所有类均可在 jar 中以 javap 复核。

---

## 1. 五个基类的钩子方法清单（javap -p 全量）

### 1.1 AbstractPower（powers/AbstractPower.class）
| 钩子 | 签名 | 触发时机归属 |
|---|---|---|
| atDamageGive/atDamageReceive(+Final) | `(float, DamageType[, AbstractCard])→float` | 伤害计算期（他人卷） |
| modifyBlock / modifyBlockLast | `(float[, AbstractCard])→float` | 格挡计算期 |
| atStartOfTurn / atStartOfTurnPostDraw | `()void` | 新回合序列（turn-phase.md） |
| duringTurn | `()void` | 每帧轮询 |
| atEndOfTurn(boolean) | `(boolean)void` | 回合尾（见 §6.4） |
| atEndOfTurnPreEndTurnCards(boolean) | `(boolean)void` | 回合尾、手牌结算前 |
| atEndOfRound | `()void` | —（未在本文取证） |
| onHeal / onPlayerGainedBlock | `(int)int` 等 | 治疗/格挡事件 |
| **onAttackToChangeDamage** | `(DamageInfo,int)→int` | 攻击方，伤害落地前改值 |
| **onAttackedToChangeDamage** | `(DamageInfo,int)→int` | 受击方，伤害落地前改值 |
| **onAttack** | `(DamageInfo,int,AbstractCreature)void` | 攻击方通知 |
| **onAttacked** | `(DamageInfo,int)→int` | 受击方通知（荆棘类反伤在此 addToBot） |
| **onInflictDamage** | `(DamageInfo,int,AbstractCreature)void` | 攻击方，实际掉血前 |
| **wasHPLost** | `(DamageInfo,int)void` | 守方，HP 将扣减时 |
| **onLoseHp** | `(int)→int` | 守方 HP 损失 |
| **onDeath** | `()void` | 持有者死亡时（die 内同步遍历） |
| onVictory | `()void` | 战斗胜利（endBattle 链） |
| **onPlayCard** | `(AbstractCard,AbstractMonster)void` | getNextAction cardQueue 分支同步直调 |
| **onUseCard** | `(AbstractCard,UseCardAction)void` | UseCardAction 构造函数同步直调 |
| **onAfterUseCard** | `(AbstractCard,UseCardAction)void` | UseCardAction.update 首帧 |
| **onAfterCardPlayed** | `(AbstractCard)void` | 手牌组 triggerOnOtherCardPlayed 尾部 |
| canPlayCard | `(AbstractCard)→boolean` | 出牌合法性 |
| onCardDraw / onDrawOrDiscard | `(AbstractCard)void` / `()void` | 抽弃事件 |
| **onExhaust** | `(AbstractCard)void` | moveToExhaustPile 中央通知 |
| onEvokeOrb / onChannel | `(AbstractOrb)void` | 球事件 |
| onChangeStance | `(AbstractStance,AbstractStance)void` | 姿态切换 |
| onGainedBlock / onGainCharge / atEnergyGain / onEnergyRecharge | — | 能量/格挡/充能事件 |
| onScry | `()void` | 洗牌窥视 |
| onDamageAllEnemies | `(int[])void` | AOE 伤害动作 |
| onSpecificTrigger / triggerMarks / onApplyPower / onInitialApplication / onRemove | — | 杂项触发（部分见开放问题） |

出处：`javap -p com.megacrit.cardcrawl.powers.AbstractPower`（本卷 .tmp 快照）。置信度：高。

### 1.2 AbstractRelic（relics/AbstractRelic.class）
| 钩子 | 签名 | 备注 |
|---|---|---|
| atPreBattle / atBattleStart / atBattleStartPreDraw | `()void` | 战斗开场三段 |
| onSpawnMonster | `(AbstractMonster)void` | 战斗开场 |
| atTurnStart / atTurnStartPostDraw | `()void` | 新回合两段 |
| **onPlayerEndTurn** | `()void` | room.applyEndOfTurnRelics 调用 |
| **onPlayCard** | `(AbstractCard,AbstractMonster)void` | getNextAction 同步链 |
| **onUseCard** | `(AbstractCard,UseCardAction)void` | UseCardAction 构造期（计数遗物主入口） |
| **onExhaust** | `(AbstractCard)void` | moveToExhaustPile |
| onManualDiscard | `()void` | 弃牌事件（无参版） |
| **onShuffle** | `()void` | EmptyDeckShuffleAction/ShuffleAction 调用 |
| onCardDraw / onDrawOrDiscard | `(AbstractCard)` / `()void` | 抽弃 |
| **onMonsterDeath** | `(AbstractMonster)void` | 怪物 die(true) 内 |
| **onVictory** | `()void` | endBattle 链第一段 |
| onTrigger / onTrigger(AbstractCreature) / checkTrigger | — | 条件触发（Lizard Tail 等） |
| onAttack / onAttacked(ToChangeDamage) / onAttackToChangeDamage | 同 power 版 | 受击反伤族 |
| onLoseHp(int) / onLoseHpLast(int) / wasHPLost(int) | — | 掉血族 |
| onBloodied / onNotBloodied | `()void` | 半血状态沿 |
| onBlockBroken / onPlayerGainBlock(float)/GainedBlock(int) / onPlayerHeal | — | 格挡/治疗 |
| onGainGold / onLoseGold / onSpendGold | `()void` | 金币 |
| onObtainCard / onPreviewObtainCard / atDamageModify | — | 卡牌获取/伤害显示 |
| onEquip / onUnequip / onEnterRoom / justEnteredRoom / onRest / onRitual / onSmith / onMeditate / onUsePotion / onChestOpen(After) / onMasterDeckChange / onRefreshHand / onEnergyRecharge / onChangeStance / onEvokeOrb | — | 战斗外与杂项 |

出处：`javap -p com.megacrit.cardcrawl.relics.AbstractRelic`。置信度：高。

### 1.3 AbstractCard（cards/AbstractCard.class）— trigger/on 族
| 钩子 | 签名 | 调用者（本文已取证处加粗） |
|---|---|---|
| use | `(AbstractPlayer,AbstractMonster)void` abstract | **AbstractPlayer#useCard [4279]** 同步直调 |
| canUse / canPlay / hasEnoughEnergy / cardPlayable | 合法性 | — |
| **triggerWhenDrawn** | `()void` | 抽牌流程（draw-exhaust.md 卷取证） |
| **triggerOnEndOfTurnForPlayingCard** | `()void` | GameActionManager#callEndOfTurnActions [56] 同步 |
| **triggerOnEndOfPlayerTurn** | `()void` | DiscardAtEndOfTurnAction（draw-exhaust.md 卷范围） |
| **triggerOnOtherCardPlayed** | `(AbstractCard)void` | CardGroup#triggerOnOtherCardPlayed（手牌组，跳过自身） |
| **triggerOnCardPlayed** | `(AbstractCard)void` | UseCardAction `<init>`（手+弃+抽 三堆全量） |
| **triggerOnManualDiscard** | `()void` | DiscardAction / DiscardSpecificCardAction / GamblingChipAction / ScrapeFollowUpAction |
| **triggerOnExhaust** | `()void` | CardGroup#moveToExhaustPile [75]（被消耗卡自身） |
| triggerAtStartOfTurn / atTurnStart / atTurnStartPreDraw | `()void` | 新回合序列 |
| triggerOnGainEnergy / triggerOnScry / triggerWhenCopied / triggerOnGlowCheck / triggerExhaustedCardsOnStanceChange | — | 对应事件 |
| onPlayCard | `(AbstractCard,AbstractMonster)void` | getNextAction 同步链（手/弃/抽三堆逐卡） |
| onChoseThisOption / onRetained / onMoveToDiscard / onRemoveFromMasterDeck / tookDamage | — | 对应事件 |

出处：`javap -p com.megacrit.cardcrawl.cards.AbstractCard`。置信度：高。

### 1.4 AbstractMonster（monsters/AbstractMonster.class）
| 方法 | 说明 |
|---|---|
| damage(DamageInfo) | 受击入口，内含受击钩子序（§7） |
| takeTurn() abstract | 怪物回合（turn-phase.md 范围） |
| die() / die(boolean triggerRelics) | 死亡处理（§5） |
| updateDeathAnimation() private | 死亡动画计时→isDead→胜利判定（§5） |
| escape()/escapeNext()/updateEscapeAnimation() | 逃跑路径（escape 也触发 endBattle） |
| usePreBattleAction/useUniversalPreBattleAction | 战斗开场 |
| heal(int)、deathReact()、changeState(String) | 杂项 |
| 字段 halfDead/isDying/isEscaping/isDead/escaped | 死亡状态机四元组（§5.3） |

出处：`javap -p com.megacrit.cardcrawl.monsters.AbstractMonster`。置信度：高。

### 1.5 AbstractStance（stances/AbstractStance.class）
| 钩子 | 调用点 |
|---|---|
| onPlayCard(AbstractCard) | GameActionManager#getNextAction cardQueue 分支 [723]（玩家出每张牌时同步） |
| onEndOfTurn() | GameActionManager#callEndOfTurnActions [68] |
| atStartOfTurn() | 存在于签名；调用点未定位（见开放问题） |
| onEnterStance/onExitStance | 姿态切换动作 |
| atDamageGive/atDamageReceive | 伤害计算期 |

出处：`javap -p com.megacrit.cardcrawl.stances.AbstractStance`。置信度：高。

---

## 2. 出牌流水：从点击到 UseCardAction 完成

### 2.1 主链总览（一帧内同步段 + 后续队列段）
getNextAction 处理 cardQueue 头部时的**同步直调序列**（全部在同一帧内、按代码顺序执行；主代理已在 action-manager.md 验证，此处给出我复核的偏移号）：

```
GameActionManager#getNextAction（cardQueue 分支）:
 ① player.powers      → p.onPlayCard(card, monster)          [offset 622]
 ② 每个 monster m     → m.powers 全部 p.onPlayCard(card,monster)[659]
 ③ player.relics      → r.onPlayCard(card, monster)          [697]
 ④ player.stance      → stance.onPlayCard(card)              [723]
 ⑤ player.blights     → b.onPlayCard(card, monster)          [786]
 ⑥ hand.group 逐卡    → c.onPlayCard(card, monster)          [855]
 ⑦ discardPile 逐卡   → c.onPlayCard(card, monster)          [924]
 ⑧ drawPile 逐卡      → c.onPlayCard(card, monster)          [993]
 ⑨ cardsPlayedThisTurn/combat 登记
 ⑩ player.useCard(card, monster, energyOnUse)                [1588]
```
- fizzle 特例：目标已死且 target==ENEMY → 牌淡出（fadingOut+ExhaustCardEffect），跳过 ⑥ 之后全部步骤（含 useCard）。主代理验证 + 本文复核 `[1482-1540]`。置信度：高。

### 2.2 AbstractPlayer#useCard 内部（AbstractPlayer.jc:4242-4346）
按字节码顺序：
1. ATTACK → useFastAttackAnimation（纯表现）。
2. `card.calculateCardDamage(monster)`（伤害显示重算）。
3. X 费修正（cost==-1 且能量不足 → energyOnUse=当前能量；isInAutoplay → freeToPlayOnce=true）。
4. **`card.use(player, monster)` [4279] —— 同步直调**。卡内 `addToBottom/addToTop` 此刻直接操作 actionManager.actions。
5. **`new UseCardAction(card, target)` 构造 [4281-4285]** —— 构造函数本身触发一轮同步通知链（§3）；随后 `actionManager.addToBottom(该实例)` [4286]。
6. 若 `!card.dontTriggerOnUseCard` → `hand.triggerOnOtherCardPlayed(card)` [4293]。
7. `hand.removeCard(card)`; `cardInUse=card` [4297-4300]。注意：被出的牌到这一步才离开手牌组——第 5 步构造期的手牌循环里它仍在场。
8. 能量扣除：costForTurn>0 且非免费且非 autoplay 且非(Corruption+SKILL) → `energy.use(costForTurn)` [4334]。

置信度：高。

### 2.3 UseCardAction `<init>`（UseCardAction.jc:15-185）—— 构造期同步通知
门控：`!targetCard.dontTriggerOnUseCard` 时依次：
```
 1. player.powers       → p.onUseCard(card, this)         [58-102]
 2. player.relics       → r.onUseCard(card, this)         [105-149]
 3. player.hand.group   → c.triggerOnCardPlayed(card)     [152-201]（含被出的牌自己！）
 4. discardPile.group   → c.triggerOnCardPlayed(card)     [201-250]
 5. drawPile.group      → c.triggerOnCardPlayed(card)     [250-299]
 6. 每个怪物的 powers   → p.onUseCard(card, this)         [299-384]
 7. actionType = exhaustCard ? EXHAUSE : USE                    [384-408]
```
置信度：高。**关键推论**：`onUseCard`/`triggerOnCardPlayed` 不在 UseCardAction 执行时才发生，而是在入队那一瞬间（useCard 第 5 步）同步发生。

### 2.4 UseCardAction#update（UseCardAction.jc:195-419）—— 出队后首帧
`duration==0.15`（首帧）分支：
```
 1. player.powers       → p.onAfterUseCard(card, this)    [10-61]
 2. 所有怪物 powers     → p.onAfterUseCard(card, this)    [61-144]
 3. card.freeToPlayOnce=false; isInAutoplay=false               [144-163]
 4. purgeOnUse → addToTop(ShowCardAndPoofAction); isDone=true;
    cardInUse=null; return —— 牌不进任何堆                      [164-197]
 5. CardType.POWER → addToTop(ShowCardAction[+Wait]);
    hand.empower(card); isDone=true; return —— 牌也不进弃牌堆   [198-304]
 6. 其余：Strange Spoon 豁免掷骰 →
      rebound/shuffleBackIntoDrawPile → moveToDeck
      returnToHand → moveToHand
      默认 → hand.moveToDiscardPile(card)
      exhaustCard（且未被勺子救下）→ hand.moveToExhaustPile(card) [477-493]
 7. exhaustOnUseOnce=false; dontTriggerOnUseCard=false;
    addToBot(new HandCheckAction)                               [496-520]
```
置信度：高。

---

## 3. 「出牌相关」power 钩子是四个不同时刻，不是同一个

| 钩子 | 时刻 | 容器 | 门控 |
|---|---|---|---|
| onPlayCard | getNextAction 同步链（效果生效**前**） | 玩家powers+怪物powers+relics+stance+blights+三堆卡的 onPlayCard | 无 dontTrigger 门控 |
| onUseCard | useCard 内构造 UseCardAction 的瞬间 | 玩家 powers、玩家 relics、怪物 powers | !dontTriggerOnUseCard |
| triggerOnCardPlayed | 同上紧随（三堆全部卡，含被出的牌） | hand/discard/draw | !dontTriggerOnUseCard |
| onAfterCardPlayed | 同上再紧随（仅手牌组，跳过被出的牌） | hand 卡 + 玩家 powers | !dontTriggerOnUseCard |
| onAfterUseCard | UseCardAction.update 首帧（效果动作已全部执行完） | 玩家 powers、怪物 powers | !dontTriggerOnUseCard |

- relic 没有 onAfterUseCard/onAfterCardPlayed（javap 清单可证）。
- orb 在上述任何链中都不出现（两个调用点均无 orb 循环）——球没有"出牌"钩子。置信度：高（对这两个调用点而言）。

---

## 4. 计数型遗物取证（≥3 例）

共同模式：**计数递增发生在 `onUseCard`（或 onShuffle）——即 UseCardAction 构造期、卡效果动作刚入队之后**；满额效果用 `addToBot` 追加，因此排在卡自身 use() 动作之后、UseCardAction.update（onAfterUseCard/消耗移动）之前。

### 4.1 Nunchaku（双截棍）
`Nunchaku#onUseCard`（Nunchaku.jc:54-89）：ATTACK 牌 → `counter++`；`counter%10==0` → counter=0、flash、`addToBot(RelicAboveCreatureAction)`、**`addToBot(GainEnergyAction(1))`**。置信度：高。

### 4.2 Pen Nib（笔尖）
`PenNib#onUseCard`（PenNib.jc:28-86）：ATTACK 牌 → counter++；==10 → 归零、flash、pulse=false；==9 → beginPulse（预提示）；满额时 `addToBot(RelicAboveCreatureAction)` + **`addToBot(ApplyPowerAction(PenNibPower))`**。另有 `atBattleStart`：若进场时 counter 已为 9 直接补发 PenNibPower。置信度：高。

### 4.3 Ink Bottle（墨水瓶）
`InkBottle#onUseCard`（InkBottle.jc:28-84）：任意牌 → counter++；==10 → 归零、flash、`addToBot(RelicAboveCreatureAction)` + **`addToBot(DrawCardAction(1))`**；==9 → beginPulse。atBattleStart 同款脉冲逻辑。置信度：高。

### 4.4 Sundial（日晷）—— 注意：计的是洗牌不是出牌
`Sundial#onShuffle`（Sundial.jc:60-90）：每次洗牌 counter++；==3 → 归零、flash、`addToBot(RelicAboveCreatureAction)` + **`addToBot(GainEnergyAction(2))`**。
调用点：`EmptyDeckShuffleAction` [offset 120] 与 `ShuffleAction` [offset 37] 都遍历 `player.relics` 直调 `r.onShuffle()`（EmptyDeckShuffleAction.jc/ShuffleAction.jc 取证）。
> 注：常见 wiki 表格把 Sundial 描述为"每 3 张牌"，字节码证明是**每 3 次洗牌**（DESCRIPTIONS[0]+"3"+DESCRIPTIONS[1]，onShuffle 钩子）。移植时以钩子为准。置信度：高（字节码）；wiki 待校正。

---

## 5. 死亡链与胜利链（怪物侧）

### 5.1 单体死亡（同一动作帧内同步完成）
`DamageAction#update` → `target.damage(info)` [245] → `AbstractMonster#damage` 尾部 `currentHealth<=0 → die()` [2227]：

```
AbstractMonster#die(boolean triggerRelics=true)   (AbstractMonster.jc:3435-3530)
 0. isDying 已真 → 整个方法跳过（幂等）
 1. isDying = true
 2. currentHealth<=0 && triggerRelics → 本体 powers 逐个 p.onDeath()   [3448-3461]
 3. triggerRelics → player.relics 逐个 r.onMonsterDeath(this)          [3462-3478]
 4. areMonstersBasicallyDead() → EndTurnButton.disable();
    limbo 全部 ExhaustCardEffect + limbo.clear()                       [3479-3507]
 5. hp 负值钳 0; deathTimer += 1.8s（fast 模式 1.0）; 统计+1            [3508-3529]
```
置信度：高。

### 5.2 战斗胜利（跨帧、延迟约 1.8 秒后）
```
AbstractMonster#updateDeathAnimation（每帧）  (AbstractMonster.jc:3251-3301)
 deathTimer<=0 → isDead=true;
 MonsterGroup.areMonstersDead() && !room.isBattleOver && !room.cannotLose
   → room.endBattle()                                            [3286-3295]

AbstractRoom#endBattle  (AbstractRoom.jc:998-1021)
 isBattleOver=true → player.onVictory() → endBattleTimer=0.25s

AbstractPlayer#onVictory  (AbstractPlayer.jc:6660-6710)
 relics.onVictory() 循环 → blights.onVictory() 循环 → powers.onVictory() 循环

AbstractRoom#update（isBattleOver && actions 空 && timer<=0 时） (AbstractRoom.jc:556-1018)
 phase=COMPLETE → VICTORY 音效
 → 按房间类型 addGoldToRewards(...)：Boss=100±5(asc13+ ×0.75)；Elite=treasureRng 25-35；
    普通 MonsterRoom=treasureRng 10-20（逃跑则跳过）             [586-1015]
 → dropReward() + addPotionToRewards()                          [1060-1065]
 → CombatRewardScreen.open()                                    [1093-1132]
```
**gold 不是怪物死亡瞬间掉落**：战斗内不存在 per-monster 金币掉落钩子；金币在胜利奖励阶段一次性生成进 rewards 列表。指派假设"damage→die→InstantKillAction→gold"需修正为本节链条。置信度：高。

### 5.3 halfDead 与死亡状态机
- `areMonstersBasicallyDead()`（MonsterGroup.jc:164-187）：全员 `isDying||isEscaping` 才算"基本死光"。**halfDead 怪不算** → 战斗继续。
- `queueMonsters()`（MonsterGroup.jc:253-281）：`!isDeadOrEscaped || halfDead` 的怪仍会排回合。
- `GameActionManager#getNextAction` monsterQueue 分支 [1803-1814]：`isDeadOrEscaped && !halfDead` 才跳过其回合。
- `DamageAction#update` [126-141]：攻击方 `info.owner.isDying || info.owner.halfDead` 且非 THORNS → 攻击作废。
- 写入方：AwakenedOne、Darkling（复活型怪；字段定义于 AbstractMonster）。置信度：高（读取方）/中（写入方清单来自字符串扫描+类职责推断）。

### 5.4 InstantKillAction 是特例，不在常规死亡链上
`InstantKillAction#update`（InstantKillAction.jc:15-37）：`hp=0` → `healthBarUpdatedEvent` → `damage(new DamageInfo(null,0,HP_LOSS))` → 走常规 damage→die 链。全项目唯一使用者：JudgementAction（字符串扫描）。常规击杀不经它。置信度：高。

### 5.5 玩家死亡救援链（AbstractPlayer#damage 尾部，AbstractPlayer.jc:4758-4830）
扣血并钳位后 `currentHealth==0`（字节码条件为 `<1`，[4758-4761]）时依序：
1. 有 "Mark of the Bloom" → 直接进入死亡分支；
2. 有 FairyPotion → 自动使用（flash、currentHealth=0、potion.use、destroyPotion）、**return（不死）**；
3. 有 Lizard Tail 且 counter==-1 → currentHealth=0、`getRelic("Lizard Tail").onTrigger()`、**return（不死）**；
4. 否则 `isDead=true` + new DeathScreen（正式死亡；player 无 monsters 式 die() 链）。
置信度：高。

---

## 6. 弃牌与回合尾的触发器视角

### 6.1 triggerOnManualDiscard 只属于"手动弃牌"
调用者全集（字符串扫描 + 方法体验证）：
- `DiscardAction#update`：三条路径均为 `moveToDiscardPile(c)` → `triggerOnManualDiscard()` → `incrementDiscard(endTurn)`。不对称点：整手弃尽路径有 `if(!endTurn)` 门控 [97-106]，随机路径与选择屏幕回收路径无条件触发 [174-178, 336-340]。
- `DiscardSpecificCardAction#update`：moveToDiscardPile → incrementDiscard(false) → triggerOnManualDiscard（DiscardSpecificCardAction.jc [50-61]）。
- `GamblingChipAction#update`：同上模式（GamblingChipAction.jc:158-166）。
- ScrapeFollowUpAction（同类，扫描命中）。
**`DiscardAtEndOfTurnAction` 不出现在调用者集合**——回合尾弃牌不触发 triggerOnManualDiscard。置信度：高。

### 6.2 回合尾的三条链（分工，勿混叠）
| 链 | 入口 | 内容 |
|---|---|---|
| A. 按键链 | EndTurnButton → `AbstractRoom#endTurn` (AbstractRoom.jc:922-996) | player.applyEndOfTurnTriggers()【creature powers：非玩家先 atEndOfTurnPreEndTurnCards(false)，全体 atEndOfTurn(isPlayer)，AbstractCreature.jc:1764-1788】→ addToBottom(**ClearCardQueueAction**) → addToBottom(**DiscardAtEndOfTurnAction**) → 三堆卡 resetAttributes → 匿名 Action(AbstractRoom$1) |
| B. 哨兵链 | cardQueue null 哨兵 → `GameActionManager#callEndOfTurnActions` (1313-1342) | room.applyEndOfTurnRelics【relics.onPlayerEndTurn + blights.onPlayerEndTurn，AbstractRoom.jc:1395-1425】→ room.applyEndOfTurnPreCardPowers【powers.atEndOfTurnPreEndTurnCards(true)】→ addToBottom(TriggerEndOfTurnOrbsAction) → **手牌逐张 c.triggerOnEndOfTurnForPlayingCard()（同步直调）** → stance.onEndOfTurn() |
| C. 怪物链 | MonsterGroup 回合收尾循环 (MonsterGroup.jc:~900-950) | 非 dying/escaping 的怪 powers.atEndOfTurn(false) 等 |

置信度：高（A/B 为本文字节码取证，与主代理 turn-phase.md 结论一致）。

### 6.3 onShuffle 触发器
`EmptyDeckShuffleAction` 与 `ShuffleAction` 在完成洗牌后遍历 `player.relics` 直调 `r.onShuffle()`；实现者：Abacus、Melange、Sundial。置信度：高（调用点）/高（实现者名单来自常量池扫描）。

---

## 7. 受击/反伤前置钩子的精确顺序

### 7.1 AbstractMonster#damage（被打的怪）hook 序（AbstractMonster.jc:1927-2297）
```
 Intangible 钳制 → early-return(isDying||isEscaping) → 负数钳 0
 decrementBlock(info, amount)                                   [1973]
 1. info.owner==player → player.relics.onAttackToChangeDamage   [1989]
 2. info.owner!=null   → owner.powers.onAttackToChangeDamage    [2010]
 3. this.powers        → onAttackedToChangeDamage               [2027]
 4. info.owner==player → player.relics.onAttack(info,amt,this)  [2049]
 5. this.powers        → wasHPLost(info, amount)                [2065]（扣血前）
 6. info.owner!=null   → owner.powers.onAttack(info,amt,this)   [2086]
 7. this.powers        → onAttacked(info, amount)→int           [2102]（荆棘/炎壁在此 addToBot 反伤）
 lastDamageTaken=min(amount,hp) → hp-=amount → 钳0 → healthBarUpdatedEvent
 8. hp<=0 → die()                                               [2227]
 9. areMonstersBasicallyDead → cleanCardQueue + DeckPoof + hideCombatPanels [2230-2263]
```
注：怪没有 relics，故守方 relic 族钩子在怪物侧不存在。

### 7.2 AbstractPlayer#damage（被打的玩家）hook 序（AbstractPlayer.jc:4348-4830）
```
 负数钳0 → IntangiblePlayer 钳1 → decrementBlock                        [4360-4377]
 1. info.owner==this(自残) → this.relics.onAttackToChangeDamage         [4396]
 2. info.owner!=null       → owner.powers.onAttackToChangeDamage        [4417]
 3. this.relics            → onAttackedToChangeDamage                   [4434]
 4. this.powers            → onAttackedToChangeDamage                   [4451]
 5. info.owner==this       → this.relics.onAttack(info,amt,this)        [4473]
 6. info.owner!=null       → owner.powers.onAttack(info,amt,this)       [4494]
 7. this.powers            → onAttacked(info,amt)→int                   [4510]
 8. this.relics            → onAttacked(info,amt)→int                   [4527]
 9. this.relics            → onLoseHpLast(int)→int                      [4547]
    lastDamageTaken=min(amount,hp)
10. amount>0 时:
    this.powers → onLoseHp(int)→int                                     [4571]
    this.relics → onLoseHp(int)                                         [4587]
    this.powers → wasHPLost(info,amount)                                [4605]
    this.relics → wasHPLost(int)                                        [4618]
11. info.owner!=null → owner.powers.onInflictDamage(info,amt,this)      [4639]
12. hp-=amount → 钳0 → 血条/受伤计数(damageReceivedThisTurn/Combat)      [4648-4668]
13. 半血沿：首次 ≤maxHealth/2 → relics.onBloodied()                      [4736-4756]
14. hp<=1 → §5.5 救援链（FairyPotion/LizardTail.onTrigger/DeathScreen）
```
要点（移植仲裁用）：**攻方改伤钩子先于守方改伤钩子；守方内部 relics 先于 powers（改伤阶段）但 powers 先于 relics（onAttacked 及以后）；wasHPLost 在扣血之前收到的是将扣除的数值**。Thorns 类反伤动作统一 `addToBot`，落在当前动作之后。置信度：高。

---

## 8. 同容器多触发的叠加顺序

**结论：powers/relics/blights/orbs/手牌等容器均为 ArrayList，全部通知循环用 `iterator()` 正向遍历 ⇒ 同容器内严格按元素加入容器的顺序（=获得/施加顺序）逐个通知。**
证据（同一模式反复出现）：
- getNextAction onPlayCard 链：`player.powers.iterator()` [617-622]、`relics.iterator()` [647-686]。
- UseCardAction ctor：powers/relics/hand/discard/draw 五连 iterator 循环（§2.3 行号）。
- CardGroup.moveToExhaustPile：relics→powers 两个 iterator 循环。
- AbstractRoom.applyEndOfTurnRelics / applyStartOfTurn* 系列：iterator 正向。
边界：
1. 该结论只约束**同一种容器内**的相对顺序；powers 与 relics 之间由调用点硬编码先后（如 exhaust=relics 先、onUseCard=powers 先），不可互换。
2. AbstractPower 实现 Comparable（priority 字段）用于 UI 排序展示，**不影响通知遍历顺序**（遍历的是 powers ArrayList 本身）。
3. 怪物列表同理（areMonstersBasicallyDead/queueMonsters 按 monsters ArrayList 顺序）。
置信度：高。

---

## 9. 同步直调 vs 队列化对照表（移植仲裁模型）

| 类别 | 机制 | 例证 |
|---|---|---|
| **同步直调** | getNextAction onPlayCard 全链（①-⑧） | GameActionManager#getNextAction |
| 同步直调 | useCard 内 card.use() | AbstractPlayer#useCard [4279]（use 内部产生的动作才入队） |
| 同步直调（构造期） | UseCardAction ctor 的 onUseCard/triggerOnCardPlayed 链 | UseCardAction `<init>` |
| 同步直调 | useCard 内 hand.triggerOnOtherCardPlayed + powers.onAfterCardPlayed | AbstractPlayer#useCard [4293] |
| 同步直调 | callEndOfTurnActions 手牌逐张 triggerOnEndOfTurnForPlayingCard + stance.onEndOfTurn | GameActionManager [1329-1341] |
| 同步直调 | moveToExhaustPile 的 relics/powers/自身通知 | CardGroup |
| 同步直调 | damage() 内全部受击钩子 | AbstractMonster/AbstractPlayer#damage |
| 同步直调 | die() 内 powers.onDeath/relics.onMonsterDeath | AbstractMonster#die |
| **队列化（addToBot/addToTop）** | 卡 use() 内部的效果动作 | Sentinel.use → addToBot(GainBlockAction) |
| 队列化 | 钩子回调里产生的后续动作（Nunchaku 能量、FeelNoPain 格挡、Sentinel 能量、Thorns 反伤） | 各实现 |
| 队列化 | UseCardAction 自身（最后入队） | useCard [4286] |
| 队列化 | purgeOnUse 的 ShowCardAndPoofAction 用 **addToTop** | UseCardAction.update [182] |
| 队列化特例 | Sentinel.triggerOnExhaust 用 **addToTop**(GainEnergyAction)——消耗通知虽同步，效果插队头 | Sentinel.jc:40-58 |

**单张牌的完整时间轴**（供仲裁）：
```
帧 N（cardQueue 头）: onPlayCard 链 → calculateCardDamage → card.use()[其动作入队]
                     → UseCardAction ctor[onUseCard/triggerOnCardPlayed 链; 钩子的动作入队]
                     → triggerOnOtherCardPlayed/onAfterCardPlayed → removeCard → 扣能量
帧 N+k（前面动作清完后）: use()的动作 逐个执行
帧 N+m: onUseCard 钩子入队的动作（Nunchaku 抽1等）
帧 N+n: UseCardAction.update → onAfterUseCard 链 → 弃/耗/回移动（exhaust 路径此刻触发
        moveToExhaustPile 通知链）→ HandCheckAction
```

---

## 10. 编号规则

- **R01** 出牌时 `onPlayCard` 通知顺序固定为：玩家powers → 逐怪(m.powers) → 玩家relics → stance → blights → 手牌逐卡 → 弃牌堆逐卡 → 抽牌堆逐卡，全部同步直调且发生在卡效果之前。出处 `GameActionManager#getNextAction` [622-993]。置信度高。
- **R02** `card.use()` 由 `AbstractPlayer#useCard` 同步直调；卡内 addToBottom 的动作排在任何 onUseCard 钩子动作之前执行。出处 `AbstractPlayer#useCard` [4279][4286]。置信度高。
- **R03** `onUseCard`(powers/relics) 与 `triggerOnCardPlayed`(手/弃/抽三堆全卡) 发生在 UseCardAction **构造期**（入队瞬间），非执行期；受 `dontTriggerOnUseCard` 门控；此时被出的牌尚未移出手牌组，因此它自己也收到 triggerOnCardPlayed。出处 `UseCardAction#<init>` [58-299]、`AbstractPlayer#useCard` [4281-4297]。置信度高。
- **R04** `onAfterUseCard` 仅在 UseCardAction.update 首帧、即卡的全部效果动作与 onUseCard 钩子动作执行完之后触发；随后才进行 purge/POWER 短路判断或 弃/耗/回 移动。出处 `UseCardAction#update` [10-144][164-197]。置信度高。
- **R05** purgeOnUse（如仪式匕首类）走独立短路：牌不进弃/耗堆，仅 ShowCardAndPoof（addToTop）+ cardInUse 清空；POWER 牌同样不进任何堆。出处 `UseCardAction#update` [164-197][198-304]。置信度高。
- **R06** `triggerOnManualDiscard` 仅由手动弃牌动作（DiscardAction/DiscardSpecificCardAction/GamblingChipAction/ScrapeFollowUpAction）触发，且回合尾弃牌（DiscardAtEndOfTurnAction）**不**触发；DiscardAction 整手弃尽路径在 endTurn=true 时不触发该钩子（其余路径无条件）。出处 `DiscardAction#update` [97-106][174-178][336-340]、字符串扫描全集。置信度高（行为）；中（endTurn=true 的实际使用场景未穷举）。
- **R07** 消耗中央通知链固定于 `CardGroup.moveToExhaustPile`：玩家 relics.onExhaust → 玩家 powers.onExhaust → 被消耗卡自身 triggerOnExhaust → 视觉/入堆(addToTop)。手牌其他卡不会收到任何"别人被消耗"的通知。出处 `CardGroup#moveToExhaustPile` [3676-3726]。置信度高。
- **R08** Necronomicurse 的"消耗后回到手牌"实为 `triggerOnExhaust` 中 `addToBot(MakeTempCardInHandAction(makeCopy()))`——生成新拷贝入手牌，原卡仍正常进消耗堆；另有 onRemoveFromMasterDeck 兜底。出处 `Necronomicurse#triggerOnExhaust`。置信度高。
- **R09** 怪物死亡同步段顺序：本体 powers.onDeath → 玩家 relics.onMonsterDeath → (若全场 basically dead) 关闭结束按钮+limbo 清空；胜利判定延迟至死亡动画结束（≈1.8s）由 updateDeathAnimation 触发 room.endBattle。出处 `AbstractMonster#die(boolean)`、`AbstractMonster#updateDeathAnimation` [3251-3295]。置信度高。
- **R10** 胜利通知顺序：endBattle → isBattleOver=true → player.onVictory【relics.onVictory → blights → powers.onVictory】→ 0.25s 后 room.update 进入 COMPLETE 并生成金币/奖励；金币不存在逐怪掉落。出处 `AbstractRoom#endBattle` [1000-1021]、`AbstractPlayer#onVictory` [6660-6710]、`AbstractRoom#update` [556-1132]。置信度高。
- **R11** InstantKillAction 不在常规死亡链上（仅 Judgement 使用）；其机制为置 hp=0 后走标准 damage(HP_LOSS)。出处 `InstantKillAction#update`、字符串扫描。置信度高。
- **R12** halfDead 语义：不计入 areMonstersBasicallyDead、仍参与回合排队、持有者作为攻击方时攻击作废（DamageAction fizzle）。出处 `MonsterGroup#areMonstersBasicallyDead`/`#queueMonsters`、`DamageAction#update` [126-141]、`GameActionManager#getNextAction` [1803-1814]。置信度高。
- **R13** 受击钩子序（两侧一致的部分）：攻方改伤(relics→powers) → 守方改伤(relics→powers) → onAttack(攻方) → onAttacked(守方) → 掉血族(onLoseHpLast/onLoseHp/wasHPLost) → onInflictDamage(攻方)；玩家侧守方在 onAttacked 之后按 powers→relics 排列，怪物侧无 relic。出处 §7 两表。置信度高。
- **R14** 计数型遗物计数点在 onUseCard（或 Sundial 的 onShuffle）构造期同步递增，满额效果动作 addToBot 追加，因而执行序位于"卡效果之后、UseCardAction.update 之前"。出处 §4 四例。置信度高。
- **R15** 同容器（powers/relics/orbs/blights/牌堆）多订阅者按获得顺序正序通知（ArrayList.iterator 正向遍历）；跨容器顺序由各调用点硬编码，互不相同。出处 §8。置信度高。
- **R16** 回合尾存在两条独立链：按键链（applyEndOfTurnTriggers→ClearCardQueue→DiscardAtEndOfTurnAction）与哨兵链（callEndOfTurnActions：relics.onPlayerEndTurn→atEndOfTurnPreEndTurnCards(true)→球触发入队→手牌 triggerOnEndOfTurnForPlayingCard 同步→stance.onEndOfTurn）；两者触发时刻与内容不同，移植时不得合并。出处 `AbstractRoom#endTurn` [924-996]、`GameActionManager#callEndOfTurnActions` [1313-1342]。置信度高。
- **R17** 玩家致死保护判定序：Mark of the Bloom 在场则跳过一切救援直接进入死亡分支；否则 Fairy Potion 自动使用（免死）；否则 Lizard Tail counter 未用时 onTrigger（免死）。出处 `AbstractPlayer#damage` [4758-4819]。置信度高。
- **R18** shuffle 触发器：EmptyDeckShuffleAction/ShuffleAction 完成洗牌后遍历 relics 直调 onShuffle（Abacus/Melange/Sundial 订阅）。出处 两 action 字节码。置信度高。

---

## 11. 开放问题 / 低置信项
1. **stance.atStartOfTurn 的调用点**未定位（签名存在但本文扫描的调用点集中区未发现）；可能在 getNextAction 新回合序列或姿态切换动作内。置信度低。
2. `AbstractPower.onSpecificTrigger`、`checkTrigger`/`onTrigger(Creature)`（ChampionsBelt/MeatOnTheBone/LizardTail 族）的完整调用点矩阵未逐一取证；仅证实 LizardTail 由玩家 damage 致死分支直调、ApplyPowerAction 含 onTrigger 相关引用。置信度低-中。
3. `DiscardAction` 的 `endTurn=true` 构造在生产代码中的使用者未穷举（该参数改变整手弃牌路径是否发 triggerOnManualDiscard，行为差异已字节码确证）。置信度中。
4. AwakenedOne/Darkling 设置 halfDead 的具体时机（进入二阶段/复活瞬间）未读其方法体，仅有字符串级证据。置信度中。
5. Sundial 的 wiki 文本与字节码行为不一致（见 §4.4），建议以字节码为准并回写勘误；其余计数遗物未逐一比对 wiki。置信度高（字节码侧）。
6. orb 是否存在其他出牌期钩子（如 Orb.onPlayCard）未系统排查；已确证 getNextAction cardQueue 链与 UseCardAction 构造链均无 orb 循环。置信度中。
7. `AbstractRoom$1` 匿名动作（endTurn 尾部入队）的确切职责（疑似怪物回合启动器）属 turn-phase.md 范围，本文未展开。
