# StS1 战斗时序 KB 第二卷 — 抽牌堆 / 弃牌堆 / 消耗区机制（draw-exhaust）

**本卷范围**：抽牌流程（DrawCardAction/FastDrawCardAction 逐帧逻辑）、洗牌（EmptyDeckShuffleAction/ShuffleAction 及 onShuffle 触发器）、"抽到时"结算（triggerWhenDrawn 全量清单）、消耗区进入时机（moveToExhaustPile 通知链、各 Exhaust 动作、UseCardAction 的 exhaust/purge 路径、Necronomicurse 特例）、回合尾弃牌阶段内部机制（DiscardAtEndOfTurnAction、Retain/Ethereal 判定序、手动弃牌差异）、以及用户示例问题的确定性裁决。行动作管理器的调度框架（getNextAction 五级优先级、cardQueue 哨兵等）归第一卷 `action-manager.md` / `turn-phase.md`，本文只引用不展开；回合首 DrawCardAction 的入队位置与回合尾弃牌在流水线中的位置可直接引用 `action-manager.md` R19 与 `turn-phase.md` R16/R17 的既有结论。

**图例**
- 出处格式：`类名#方法`，附 javap 字节码偏移摘录（≤6 行）。全部出处可用 `javap -c -p <类>` 对 desktop-1.0.jar 复核。
- 置信度：**高** = 字节码直接可证；**中** = 字节码 + 调用链推断（注明推断环节）；**低** = 仅 wiki 或间接证据。
- 版本基准：desktop-1.0.jar v2.x（2022-12-20 构建，含 Watcher）。
- 术语：`addToTop`=actions 列表 index 0 插入（下一个执行）；`addToBot/addToBottom`=append；手牌/牌堆的 `CardGroup.addToTop` 是 **ArrayList 末尾追加**（"顶"=列表尾），见 P2。

---

## 0. 前置事实（本文推理依赖）

| 编号 | 规则 | 出处 | 置信度 |
|---|---|---|---|
| P1 | GameActionManager 单活动 action 模型：每次只 update 队首 action，其 `isDone` 后才轮到下一个；执行期间被 `addToTop/addToBot` 插入的新 action **不会抢占当前 action** | 共享规范 §getNextAction①；`GameActionManager#getNextAction`；另见 `action-manager.md` | 高 |
| P2 | CardGroup 的 `group` 是 ArrayList，**索引末尾 = 牌堆顶**：`getTopCard()` 读末元素、`removeTopCard()` 删末元素、`addToTop(c)`=`group.add(c)`（尾部）、`addToBottom(c)`=`add(0,c)`（头部） | `CardGroup#addToTop/addToBottom/getTopCard/removeTopCard` | 高 |
| P3 | 随机源分离：洗牌用 `AbstractDungeon.shuffleRng`（`Collections.shuffle(list, new java.util.Random(shuffleRng.randomLong()))`），随机选卡用 `AbstractDungeon.cardRandomRng`（DiscardAction 随机弃、ExhaustAction 随机消耗、Strange Spoon 判定等） | `CardGroup#shuffle(Random)`；`DiscardAction#update`@159 | 高 |
| P4 | Soul/SoulGroup 只是视觉动画层：`Soul.discard(card,visible)` 在调用瞬间就把卡加入 `discardPile.addToTop(card)`；`Soul.shuffle(card,invisible)` 同样立即 `drawPile.addToTop(card)`。逻辑归属在动作调用时即完成 | `Soul#discard(AbstractCard,boolean)`@19-24、`Soul#shuffle`@20-25 | 高 |
| P5 | tickDuration：`duration -= Gdx.graphics.getDeltaTime()`，<0 时 `isDone=true`；多帧动作靠它收尾 | `AbstractGameAction#tickDuration`（共享规范） | 高 |

```bytecode
// P2: CardGroup#addToTop —— 名为 Top 实为尾部追加
0: aload_0; getfield group
4: aload_1
5: invokevirtual ArrayList.add(Object)   // 追加到末尾
```

---

## 1. 抽牌流程

### 1.1 战斗开局牌堆初始化（背景）

`CardGroup#initializeDeck(masterDeck)`：clear → 把主牌组拷贝为临时 DRAW_PILE 组并用 `shuffleRng` 洗一次 → 遍历洗后的临时组：普通卡逐张 `addToTop`（即按遍历序压栈）；**Innate 卡与瓶装卡（inBottleFlame/Lightning/Tornado）单独收集**，最后再逐张 `addToTop`——因此它们位于牌堆顶，且**最后遍历到的那张在最顶上**。若 Innate+瓶製数 > `masterHandSize`，把超出部分以 `addToTurnStart(new DrawCardAction(player, 超出数))` 挂入 preTurnActions 补抽。
出处：`CardGroup#initializeDeck`（偏移 0-236）。置信度：**高**。

### 1.2 DrawCardAction 构造器族

| 构造器 | 语义 |
|---|---|
| `(AbstractCreature, int, boolean)` | 主构造器；第三参=true 时向 `topLevelEffects` 加 `PlayerTurnEffect`（回合开局抽牌视觉）；`actionType=DRAW`；`duration=FAST_MODE?ACTION_DUR_XFAST:ACTION_DUR_FASTER`；字段初始 `shuffleCheck=false, clearDrawHistory=true, followUpAction=null` |
| `(int)` | =`(null, amount, false)` |
| `(int, boolean)` | =`(int)` 再覆写 `clearDrawHistory=第二参`（分段抽牌保持 drawnCards 连续） |
| `(int, AbstractGameAction[, boolean])` | 设置 `followUpAction`（抽完后续接的动作） |

出处：`DrawCardAction#<init>*`。置信度：**高**。

### 1.3 DrawCardAction#update() 完整逐帧逻辑

```
update():                                    // 每帧调用，直到 isDone
 1. 若 clearDrawHistory: 置 false 并 drawnCards.clear()      [@0-18]
 2. 若玩家有 "No Draw" power: power.flash(); endActionWithFollowUp(); return   [@18-44]
    （直接结束，不进入抽牌循环）
 3. 若 amount <= 0: endActionWithFollowUp(); return          [@45-56]
 4. dpSize = player.drawPile.size(); discardSize = player.discardPile.size();
    若 SoulGroup.isActive(): 直接 return（不 tick duration，下一帧原样重试——等灵魂动画就绪）[@57-83]
 5. 若 dpSize + discardSize == 0（双堆全空）: endActionWithFollowUp(); return  [@84-94]
 6. 若 hand.size() == 10: createHandIsFullDialog(); endActionWithFollowUp(); return  [@95-119]
 7. 【一次性中段检查】若 !shuffleCheck:                        [@120-255]
    a.【手牌上限钳制】若 amount + hand.size() > 10：
         overflow = 10 - (amount + hand.size())
         this.amount += overflow        // 即把 amount 改写为 10 - hand.size()
         createHandIsFullDialog()
       （未开始的余量被直接削减，不会溢出销毁任何卡）
    b.【抽空洗牌重排】若 amount > dpSize（抽牌堆不够）：
         need = amount - dpSize
         addToTop( new DrawCardAction(need, this.followUpAction, /*clearDrawHistory=*/false) )
         addToTop( new EmptyDeckShuffleAction() )
         若 dpSize != 0: addToTop( new DrawCardAction(dpSize, /*clearDrawHistory=*/false) )
         this.amount = 0; isDone = true; return
       // 三连 addToTop 的插入序 → 执行序固定为：
       //   [补抽现有堆顶 dpSize 张] → [EmptyDeckShuffleAction] → [续抽 need 张]
       // followUpAction 移交给"续抽"子动作携带，仍在整链最后执行；
       // 两段子抽都带 clearDrawHistory=false，静态 drawnCards 跨洗牌边界持续累积
    c. 否则 shuffleCheck = true（后续帧不再进 7 分支）
 8. duration -= Gdx.graphics.getDeltaTime()                   [@256-272]
 9. 若 amount != 0 && duration < 0（单帧至多一张）:             [@273-417]
      重置 duration（FAST_MODE?XFAST:FASTER）; amount--
      若 drawPile 非空:
          drawnCards.add(drawPile.getTopCard())
          player.draw()          ← triggerWhenDrawn 在这里触发（见 1.4）
          hand.refreshHandLayout()
      否则（中途空堆，理论不可达）: logger.warn("...empty drawpile mid-DrawAction"); endActionWithFollowUp(); return
      若 amount == 0: endActionWithFollowUp()
      // endActionWithFollowUp: isDone=true; 若 followUpAction!=null 则 addToTop(followUpAction)
```
出处：`DrawCardAction#update`（偏移 0-418，上文行号即偏移），`DrawCardAction#endActionWithFollowUp`。置信度：**高**。

关键结论：
- **单帧至多抽 1 张**（第 9 步每帧一次 amount--），N 张跨 N 个动作帧完成；
- **空堆→EmptyDeckShuffleAction 的入队时机**是"发现 amount > dpSize 的那一帧"（7b），三个子动作一次性全部插入且顺序固定（先抽残余→洗→续抽）；
- 手牌上限 10 有两道闸：抽批开始前手已满 10 → 整批终止（步骤 6）；否则余量钳制到 `10 - hand.size()`（7a）。溢出不销毁卡；
- 分段子动作带 `clearDrawHistory=false`，供 followUpAction 消费完整 drawnCards。

### 1.4 单张卡的抽入序列（triggerWhenDrawn 调用点）

`player.draw()`（无参）：手满 10 → 仅弹 HandIsFull 对话框并返回；否则播放音效后调 `draw(1)`，随后 `onCardDrawOrDiscard()`。

`draw(int n)` 每张卡严格按以下顺序：
```
1. c = drawPile.getTopCard()；设置入场坐标/scale（纯视觉）
2. c.triggerWhenDrawn()                    ← 同步直调，早于进手牌   [@65]
3. hand.addToHand(c)                       ← group.add：追加到手牌列表末尾 [@73]
4. drawPile.removeTopCard()                ← 此时才从抽牌堆移除       [@80]
5. 遍历 player.powers: power.onCardDraw(c)                            [@84-122]
6. 遍历 player.relics:  relic.onCardDraw(c)                           [@123-162]
```
出处：`AbstractPlayer#draw(int)`（偏移同上）、`AbstractPlayer#draw()`、`AbstractPlayer#onCardDrawOrDiscard`（powers.onDrawOrDiscard → relics.onDrawOrDiscard）。置信度：**高**。

要点：`triggerWhenDrawn` 在卡**尚未进入手牌、尚未离开抽牌堆**时同步触发（此刻它同时挂在 drawPile.group 与即将进 hand 的过渡态——依赖"卡在哪一堆"的钩子读到的是旧状态）。它内部 `addToBot/addToTop` 的动作受 P1 约束：**要等整个 DrawCardAction 结束后才执行**。

### 1.5 FastDrawCardAction 差异

- 构造器：`actionType=DRAW`，`duration` 恒为 `ACTION_DUR_XFAST`（更快）。参数 3=true 时向 `effectList`（注意不是 topLevelEffects）加 PlayerTurnEffect；参数 3=false 时在**构造期**检查 "No Draw"，命中则直接 `isDone=true, duration=0, actionType=WAIT` 自我终结。
- update() 与 DrawCardAction 同构的部分：SoulGroup.isActive 让路、上限钳制（无"手满 10 即停"分支）、不够时同样三连 addToTop(FastDrawCardAction(player,dpSize) / EmptyDeckShuffle / FastDrawCardAction(player,need))、单帧一张、每张后 refreshHandLayout。
- 缺失的能力：**没有 drawnCards 记录、没有 followUpAction、update 中不再复查 "No Draw"**。
出处：`FastDrawCardAction#<init>`、`FastDrawCardAction#update`。置信度：**高**。

### 1.6 抽牌规则条目

**R01** DrawCardAction 每个动作帧最多抽 1 张；一批 N 张抽牌 = N 个连续动作帧，其间其他 action 一律等待。出处：`DrawCardAction#update`@273-321 + P1。置信度高。

**R02** "牌堆顶"=CardGroup.group 列表末尾；`addToTop` 是尾部 append、`addToBottom` 是头部 insert(0)。出处：`CardGroup#addToTop/addToBottom/removeTopCard`。置信度高。

**R03** 抽牌堆+弃牌堆双空时 DrawCardAction 无事结束；只有抽牌堆空而弃牌堆非空才触发洗牌续抽。出处：`DrawCardAction#update`@84-94,188-250。置信度高。

**R04** 洗牌续抽的三段式插入顺序固定：[补抽现有 dpSize 张] → [EmptyDeckShuffleAction] → [续抽 need 张]，followUpAction 附着于最后一段。出处：`DrawCardAction#update`@188-250。置信度高。

**R05** 每张卡抽入的内部顺序固定：视觉 → `triggerWhenDrawn()`（同步直调）→ `hand.addToHand` → 离开抽牌堆 → 全体 powers.onCardDraw → 全体 relics.onCardDraw。出处：`AbstractPlayer#draw(int)`@17-162。置信度高。

**R06** "No Draw" power 使 DrawCardAction 直接闪光结束且不进入抽牌循环；FastDrawCardAction 只在构造参数 3==false 时做同一检查且 update 不复查。出处：`DrawCardAction#update`@18-44；`FastDrawCardAction#<init>`@30-78。置信度高。

**R07** 手牌达 10 上限时：批前已满 10 → DrawCardAction 终止本批；未满时余量钳制为 `10 - hand.size()`。溢出不会销毁卡。出处：`DrawCardAction#update`@95-179。置信度高。

---

## 2. 洗牌

### 2.1 EmptyDeckShuffleAction（弃牌堆 → 抽牌堆）

- **构造器**（在 `new` 那一刻执行，即 DrawCardAction 第 7b 步插队的那一帧内）：首次显示 SHUFFLE_TIP 教程后，**立即遍历 `player.relics` 逐个调用 `relic.onShuffle()`**。因此 relic 的 onShuffle 早于物理洗牌发生（洗牌在其后的 update 里）。出处：`EmptyDeckShuffleAction#<init>`@90-126。置信度：**高**。
- **update() 第一帧**：`discardPile.shuffle(AbstractDungeon.shuffleRng)` 先随机化弃牌堆内序。出处：`EmptyDeckShuffleAction#update`@0-21。置信度：**高**。
- **update() 后续帧**（vfx 阶段）：每帧从弃牌堆取一张：`iterator.remove()` + `souls.shuffle(card, count<11)`（前 10 张可见飞行动画，之后 invisible=true）。由 P4，每张卡在被处理的当帧即加入抽牌堆顶。搬完 → `isDone=true`。出处：`EmptyDeckShuffleAction#update`@24-124；`SoulGroup#shuffle`；`Soul#shuffle`@20-25。置信度：**高**。
- 本动作**不通知任何 power**（power 无 onShuffle 钩子，R09），也**不触发卡牌侧钩子**。

### 2.2 ShuffleAction（原地洗某个牌堆）

- 字段 `group`（目标 CardGroup）+ `triggerRelics`（boolean）。update()：若 `triggerRelics` 则遍历 relics 调 `onShuffle()`，随后 `group.shuffle()`（内部同样以 `shuffleRng.randomLong()` 为种子），立即 isDone——单帧动作。
- 适用场景：效果性"洗牌你的抽牌堆"，与弃牌堆回填无关。
出处：`ShuffleAction#update`（javap 可复核：relics 迭代 → `AbstractRelic#onShuffle` → `group#shuffle()` → isDone）。置信度：**高**。

### 2.3 洗牌随机性与触发器规则

**R08** 所有洗牌最终走 `Collections.shuffle(group, new java.util.Random(rng.randomLong()))`；弃牌堆回填与 initializeDeck 用 `shuffleRng`。出处：`CardGroup#shuffle()/shuffle(Random)`。置信度高。

**R09** onShuffle 只有 relic 钩子，power 没有对应方法：`AbstractPower` 方法面不含 onShuffle（含 onCardDraw/onUseCard/onAfterUseCard/onExhaust）；`AbstractRelic` 含 `onShuffle()`。出处：`javap -p AbstractPower / AbstractRelic`。置信度高。
- 两个触发点区别：EmptyDeckShuffleAction 在**构造期**（=DrawCardAction 决定洗牌那一帧）触发 relic.onShuffle；ShuffleAction 在**自身 update 执行时**、洗牌语句之前触发。二者都在物理洗牌之前。（构造期时机推断链：new 发生在 `DrawCardAction#update`@212-220 → Java 构造器即时执行。）置信度高。

**R10** EmptyDeckShuffleAction 把弃牌堆逐张搬回需多帧（前 10 张可见动画），但每张卡的逻辑归属（进抽牌堆顶）在处理它的当帧立即生效（P4）。排其后的"续抽"子动作开始时全部卡已就位。出处：`EmptyDeckShuffleAction#update`@24-114 + `Soul#shuffle`。置信度高。

---

## 3. "抽到时"结算（triggerWhenDrawn 全量清单）

基类 `AbstractCard#triggerWhenDrawn()` 为空实现（`Code: 0: return`）。对 jar 内 `com/megacrit/cardcrawl/cards/**` 全部 453 个 class 做 `javap -p` 声明扫描，**声明 triggerWhenDrawn 的类共 6 个：5 个有效覆盖 + 1 个弃用透传**：

| 类 | 效果（字节码语义） | 内部动作的入队位置 | 置信度 |
|---|---|---|---|
| `status/VoidCard` | 失去 1 点能量 | `addToBot(LoseEnergyAction(1))` → 动作队尾，整批抽完后结算 | 高 |
| `curses/Doubt` | 仅 `addToBot(SetDontTriggerAction(this,false))`：把 dontTriggerOnUseCard 复位 false（`SetDontTriggerAction#update` 就是 `card.dontTriggerOnUseCard=trigger`）。**Weak 不在抽到时施加**——其 `triggerOnEndOfTurnForPlayingCard` 置 `dontTriggerOnUseCard=true` 并把 `new CardQueueItem(this, autoplay=true)` append 到 cardQueue 末尾，Weak 由回合末自动打出时经 use() 施加 | addToBot（动作队尾）+ cardQueue 尾 | 高 |
| `green/EndlessAgony` | 复制一张 `makeStatEquivalentCopy()` 进手 | `addToTop(MakeTempCardInHandAction(copy))` → 插队首（仍晚于当前 DrawCardAction 完成，P1） | 高 |
| `purple/DeusExMachina` | 获得 magicNumber 张 Miracle 并消耗自身。连续两次 `addToTop`：先 MakeTempCardInHandAction(Miracle×n)、后 ExhaustSpecificCardAction(this,hand)。**第二次 addToTop 反转到最前 → 实际执行序 = 先自耗、后生成 Miracle** | 双 addToTop（逆序执行） | 高 |
| `green/Eviscerate` | 纯同步：先 `super.triggerWhenDrawn()` 再 `setCostForTurn(cost - GameActionManager.totalDiscardedThisTurn)`，不入队任何 action | 无动作 | 高 |
| `deprecated/DEPRECATEDStepAndStrike` | 仅 `super.triggerWhenDrawn()`（空操作） | 无 | 高 |

出处（逐类 `javap -c -p`）：`VoidCard#triggerWhenDrawn`、`Doubt#triggerWhenDrawn/use/triggerOnEndOfTurnForPlayingCard`、`EndlessAgony#triggerWhenDrawn`、`DeusExMachina#triggerWhenDrawn`、`Eviscerate#triggerWhenDrawn`、`DEPRECATEDStepAndStrike#triggerWhenDrawn`、`SetDontTriggerAction#update`、`AbstractCard#triggerWhenDrawn`。

**对照反例（纠正 wiki 口传旧认知）**：

**R24** `red/Havoc` **不覆写 triggerWhenDrawn**（javap 方法面无此方法）。它是可打出的 cost-1 牌：`use()` 里 `addToBot(new PlayTopCardAction(monsters.getRandomMonster(null,true,cardRandomRng), true))`——"打出该牌时"才打抽牌堆顶并消耗之。"Havoc 抽到时自动打出"是 wiki 口传/旧版印象，**低置信，勿用于移植仲裁**。出处：`javap -c -p cards/red/Havoc.class`（方法面仅 use/upgrade/makeCopy）。置信度高（反证本身）。

**R25** `colorless/Mayhem` 是 Power 牌，效果在 `MayhemPower#atStartOfTurn`（`addToBot(匿名 action)×amount`，回合开始随机打出手牌中的卡），与抽牌无关；`green/HeelHook` 也无 triggerWhenDrawn，条件抽牌在其 `use()` 经 `HeelHookAction` 完成。出处：`MayhemPower#atStartOfTurn`；`HeelHook` javap。置信度高。

**R11** triggerWhenDrawn 的唯一调用点是 `AbstractPlayer#draw(int)`@65，每张卡恰一次，先于进手牌与离堆。出处同 1.4。置信度高。

**R12** triggerWhenDrawn 内部入队的动作（无论 addToTop/addToBot）都在**当前这批 DrawCardAction 完全结束后**才执行；一批抽牌抽到多张覆盖卡时，各自入队，相对顺序由"入队位置+入队时刻"决定（同用 addToTop 时后抽者反而排在更前）。出处：P1 + 各覆盖类。置信度高。

**R13** MakeTempCardInHandAction 生成的卡经 `ShowCardAndAddToHandEffect` 进入手牌（`makeStatEquivalentCopy`，除非 sameUUID）；超过 10 张的部分转投弃牌堆（`ShowCardAndAddToDiscardEffect`）；持有 Master Reality power 时非 curse/status 新卡 upgrade。出处：`MakeTempCardInHandAction#update/addToHand/addToDiscard/makeNewCard`。置信度高。

---

## 4. 消耗区进入时机

### 4.1 唯一汇聚点：CardGroup#moveToExhaustPile

所有正常消耗路径最终落到这个方法（接收者必须是卡当前所在的 CardGroup，因为它负责把卡从该组移除）。**固定通知链**：

```
1. 遍历 player.relics : relic.onExhaust(c)        [@0-37]
2. 遍历 player.powers : power.onExhaust(c)        [@37-74]
3. c.triggerOnExhaust()   （卡自身的钩子）          [@74-77]
4. resetCardBeforeMoving(c)：若 c 是 hoveredCard 则 releaseCard；
   actionManager.removeFromQueue(c)；unhover/untip/stopGlowing；从本组 group 移除 [@78-82]
5. AbstractDungeon.effectList.add(new ExhaustCardEffect(c))  （纯视觉）[@83-97]
6. exhaustPile.addToTop(c)  → 消耗堆"顶"(列表尾)            [@98-107]
7. player.onCardDrawOrDiscard()                              [@108-113]
```
出处：`CardGroup#moveToExhaustPile`（偏移同上）、`CardGroup#resetCardBeforeMoving`。置信度：**高**。
注：StS1 没有 `CardGroup.exhaustCard` 这样的方法名；等价物即 `moveToExhaustPile`。

### 4.2 ExhaustSpecificCardAction（定点消耗指定卡）

`update()` 首帧（duration 仍等于起始值时）：目标卡确实在给定 `group` 里 → `group.moveToExhaustPile(targetCard)` + `checkForPactAchievement`，并置 `targetCard.exhaustOnUseOnce=false、freeToPlayOnce=false`；然后 tickDuration 收尾。**目标卡不在指定组里时什么都不发生（不消耗、不触发任何钩子）**。
出处：`ExhaustSpecificCardAction#update`@0-63。置信度：**高**。

### 4.3 ExhaustAction（从手牌选/随机消耗 N 张）

update() 首帧分支：
1. 手牌空 → isDone。
2. `!anyNumber && hand.size() <= amount` → **同步 for 循环把整只手逐张 `hand.moveToExhaustPile(getTopCard())`**（次数为进入时的手牌数），checkForPactAchievement 后 return——此分支不置 isDone、也不 tickDuration，靠下一帧"手牌已空"的第 1 分支收尾（一帧延迟怪癖）。
3. `isRandom` → 循环 amount 次 `hand.moveToExhaustPile(hand.getRandomCard(cardRandomRng))`。
4. 否则打开 `HandCardSelectScreen.open(TEXT[0], amount, anyNumber, canPickZero)` 进入选择 UI；后续帧在 `wereCardsRetrieved==false` 时把 `selectedCards` 逐张 `hand.moveToExhaustPile`。
出处：`ExhaustAction#update`（偏移 0-291）。置信度：**高**。

### 4.4 UseCardAction 的出牌后去向（exhaust 与 purge 路径）

构造器（玩家 `useCard` 出牌、action 入队那一刻同步执行）：置 `exhaustCard = card.exhaustOnUseOnce || card.exhaust`；随后若 `!card.dontTriggerOnUseCard` 依次同步触发：玩家 powers.onUseCard(card,this) → relics.onUseCard → **hand/discardPile/drawPile 三堆逐卡 triggerOnCardPlayed(card)** → 全部怪物的 powers.onUseCard。`actionType`=EXHAUST 或 USE。
出处：`UseCardAction#<init>`。置信度：**高**。

update() 首帧（duration==0.15 时；物理移动全部发生在这一帧内，之后仅 tickDuration 收尾）：
1. 玩家 powers.onAfterUseCard → 怪物 powers.onAfterUseCard（受 dontTriggerOnUseCard 门控）。
2. `freeToPlayOnce=false; isInAutoplay=false`。
3. **purgeOnUse 路径**：`addToTop(ShowCardAndPoofAction(card)); isDone=true; cardInUse=null; return` ——**完全绕过 moveToExhaustPile**：不进消耗堆、不触发任何 relic/power 的 onExhaust、也不触发卡自身 triggerOnExhaust。卡只是消失（视觉 poof）。
4. POWER 牌路径：addToTop(ShowCardAction)+WaitAction(0.1/0.7)，然后 `hand.empower(card)`（resetCardBeforeMoving + souls.empower——POWER 牌使用后**既不进弃牌堆也不进消耗堆**，直接移出战局），cardInUse=null，结束。
5. 普通/攻击/技能牌去向判定：
   - `discardInstead=false`；若将消耗且持有 Strange Spoon 且非 POWER → 50% (`cardRandomRng.randomBoolean`) 改判弃牌，改判时 `Strange Spoon.flash()`。
   - 将消耗且未被改判 → `hand.moveToExhaustPile(card)` + checkForPactAchievement（**标准出牌消耗路径**）。
   - 否则按优先级：`reboundCard` → moveToDeck(card,false)（抽牌堆顶）；`shuffleBackIntoDrawPile` → moveToDeck(card,true)；`returnToHand` → moveToHand(card)+onCardDrawOrDiscard；默认 → moveToDiscardPile(card)。
6. 收尾：`exhaustOnUseOnce=false; dontTriggerOnUseCard=false`（复位），`addToBot(new HandCheckAction())`，tickDuration。
出处：`UseCardAction#update`（偏移 0-527，关键段 160-520）。置信度：**高**。

**purgeOnUse 由谁设置**：cards/** 全量扫描无任何基础牌写死 `purgeOnUse=true`；唯一运行时赋值点是 `GameActionManager#queueExtraCard(card,monster)`（为自动打出复制的实例置位，使被代打的牌用后消失）。
出处：grep `putfield purgeOnUse` 全 jar 类 → 仅 `AbstractCard`（默认 false）与 `GameActionManager#queueExtraCard`@264。置信度：**高**。

### 4.5 Necronomicurse 特例

`cards/curses/Necronomicurse`：
- 构造器**未设置** isEthereal/selfRetain（尽管文案常记成"Ethereal"，代码里无该标志）。
- `triggerOnExhaust()`：若持有 Necronomicon relic 则 flash，然后 `addToBot(new MakeTempCardInHandAction(makeCopy()))` —— 被消耗时经 4.1 第 3 步触发，**新副本经动作队列进入手牌**（排在触发它的消耗动作之后）。
- `onRemoveFromMasterDeck()`：Necronomicon flash + `effectsQueue.add(NecronomicurseEffect)`（战役层删除阻止的表现部分；删除拦截本身在战役/事件层，不在战斗时序内，本卷不裁）。
出处：`Necronomicurse#<init>/triggerOnExhaust/onRemoveFromMasterDeck`。置信度：**高**（代码行为）/ **低**（与 wiki 文案措辞出入，未逐字核对 wiki）。

### 4.6 消耗规则条目

**R14** 一次消耗的钩子顺序全局唯一：全体 relics.onExhaust → 全体 powers.onExhaust → 该卡 triggerOnExhaust → 移出原区 → 进消耗堆顶 → onCardDrawOrDiscard。出处：`CardGroup#moveToExhaustPile`。置信度高。

**R15** purgeOnUse 是"假消耗"：绕过全部 onExhaust/triggerOnExhaust 钩子且不进消耗堆；运行时唯一赋值来源是 GameActionManager#queueExtraCard。出处：`UseCardAction#update`@160-197；`GameActionManager#queueExtraCard`。置信度高。

**R16** POWER 牌使用后经 `empower` 移出战局（不弃不耗），正常出牌流程中"消耗堆里的 POWER"不存在。出处：`UseCardAction#update`@198-304 + `CardGroup#empower`。置信度高。

**R17** Strange Spoon 把"出牌消耗"50% 变成"出牌弃牌"；只影响 UseCardAction 路径，不影响 ExhaustAction/ExhaustSpecificCardAction 等外部消耗。出处：`UseCardAction#update`@305-375。置信度高。

**R18** ExhaustAction 的"消耗整只手"分支在同一帧内同步清空手牌，但自身要到下一帧才 isDone（缺 isDone/tick 的路径靠空手守卫兜底）。出处：`ExhaustAction#update`@25-128。置信度高。

---

## 5. 回合尾弃牌阶段内部机制（DiscardAtEndOfTurnAction）

入队位置一句话（流水线归属 turn-phase.md，详见其 R16/R17）：`AbstractRoom#endTurn()` 依次 `player.applyEndOfTurnTriggers()` → `addToBottom(ClearCardQueueAction)` → `addToBottom(DiscardAtEndOfTurnAction)` → 对 drawPile/discardPile 全卡 `resetAttributes()`。出处：`AbstractRoom#endTurn`@0-110。置信度：**高**。

### 5.1 DiscardAtEndOfTurnAction#update() 首帧逐步

```
 1. 【Retain 先行】遍历 hand（迭代中删除）：凡 retain || selfRetain 的卡
    → limbo.addToTop(c)（append 到 limbo 尾）并从 hand 移除。        [@11-75]
 2. addToTop( new RestoreRetainedCardsAction(player.limbo) )         [@76-92]
    —— 此刻它在新增批次的最底，稍后被后续 addToTop 压到更下。
 3. 若 无 Runic Pyramid relic 且 无 Equilibrium power：
    n = hand.size()（定格）
    循环 n 次: addToTop( new DiscardAction(player, null,
                             hand.size()/*每次取当时值*/, true/*isRandom*/, true/*endTurn*/) )  [@93-163]
    每个 DiscardAction 的 amount 都是"入队当时的手牌数"。执行时第一个运行的
    DiscardAction 因 hand.size() <= amount 命中"全弃"分支，一次性把剩余手牌全部
    moveToDiscardPile；其余 n-1 个成为空转 no-op。（字节码如此，见 5.3 与开放问题 1。）
 4. 克隆当前 hand.group → Collections.shuffle(List)（无参 Random，非 shuffleRng）→
    遍历乱序克隆，逐张【同步直调】 c.triggerOnEndOfPlayerTurn()。      [@164-214]
 5. isDone = true                                                     [@215-218]
```
出处：`DiscardAtEndOfTurnAction#update`（偏移同上）。置信度：**高**。

### 5.2 Ethereal vs Retain：Retain 先判，Ethereal 后行

- `AbstractCard#triggerOnEndOfPlayerTurn()` 基类实现就是 Ethereal 处理：`if (isEthereal) addToTop(new ExhaustSpecificCardAction(this, hand))`。出处：`AbstractCard#triggerOnEndOfPlayerTurn`@0-25。置信度：**高**。
- 结合 5.1：Retain/selfRetain 卡在第 1 步已离开 hand（进 limbo），第 4 步的乱序克隆里没有它们 → 其基类 Ethereal 逻辑不会被调；即便自定义覆盖被调到，ExhaustSpecificCardAction 的 `group.contains` 守卫也令其空转。**结论：Retain 胜过 Ethereal；Ethereal 消耗发生在保留判定之后。**
- 执行序细节：第 4 步里 Ethereal 卡 addToTop 的 ExhaustSpecificCardAction 位于 DiscardAction 之上 → **Ethereal 消耗先于弃牌执行**；多张 Ethereal 之间相互顺序 = 乱序克隆遍历的**逆序**（连续 addToTop 反转）。出处：P1 + 5.1 步骤 2/4 叠加分析。置信度：**高**（步骤事实高；"逆序"为 addToTop 语义直接推论）。

### 5.3 回合尾弃牌 vs 手动弃牌（DiscardAction 两副面孔）

`DiscardAction(target, source, amount, isRandom, endTurn)`。update() 首帧：
1. 怪物全灭（areMonstersBasicallyDead）→ 直接 isDone（战斗结束时挂起的弃牌失效）。
2. `hand.size() <= amount` → 同步循环整只手 `moveToDiscardPile`；每张后：`if (!endTurn) c.triggerOnManualDiscard()`；无条件 `incrementDiscard(endTurn)`。
3. `isRandom` → 循环 amount 次：随机取一张（cardRandomRng）`moveToDiscardPile` + **无论 endTurn 都调 `c.triggerOnManualDiscard()`** + `incrementDiscard(endTurn)`。
4. 非 random 且 amount>=0 且手牌多于 amount → 打开 HandCardSelectScreen（手动选弃，amount<0 时 99 张 anyNumber+canPickZero）；确认后逐张 moveToDiscardPile + triggerOnManualDiscard + incrementDiscard(endTurn)。
出处：`DiscardAction#update`。置信度：**高**。

`GameActionManager.incrementDiscard(boolean)`：`totalDiscardedThisTurn++`；**仅当 `!turnHasEnded && 参数==false` 时**才调 `player.updateCardsOnDiscard()` 并遍历 relics 调 `relic.onManualDiscard()`。由于 DiscardAction 传的是 `endTurn`：
- 手动/效果弃牌（endTurn=false）：卡侧 triggerOnManualDiscard（random 分支恒调；全弃分支仅在 endTurn=false 时调）+ relic.onManualDiscard（Tingsha 类）+ updateCardsOnDiscard 全部生效；
- 回合尾弃牌（endTurn=true）：relic.onManualDiscard 与 updateCardsOnDiscard 被 incrementDiscard 门控跳过 → **Tingsha 不吃回合尾弃牌**；卡侧 triggerOnManualDiscard 仅在全弃分支被 endTurn 抑制、random 分支却照调（字节码层面不对称，如实记录）。
出处：`GameActionManager#incrementDiscard(boolean)`（偏移 0-63）；`DiscardAction#update`。置信度：**高**。

### 5.4 RetainCardsAction / RestoreRetainedCardsAction 配合

- `RetainCardsAction(p, amount)`（效果让玩家保留手牌用）：打开 HandCardSelectScreen 选 amount 张（canPickZero=true）；取回时对每张：`if (!isEthereal) retain = true` ——**Ethereal 卡不能借此获得 retain**；选中卡 `hand.addToTop` 放回手牌。
- `RestoreRetainedCardsAction(limbo)`：立即 isDone；遍历 limbo（保持入队顺序）：`retain||selfRetain` 者 → `c.onRetained()` → `hand.addToTop(c)` → `retain=false`（**selfRetain 不清除**，天然永久保留）→ 从 limbo 移除；最后 refreshHandLayout。
- 时序配合（5.1 的队列结果）：`[DA×n] → Restore`，弃牌全部完成后保留卡才回手；Restore 与 DiscardAtEndOfTurnAction 同批执行，因此**新回合开始抽牌前手牌已包含保留卡**。
出处：`RetainCardsAction#update`、`RestoreRetainedCardsAction#update`。置信度：**高**。

### 5.5 弃牌阶段规则条目

**R19** 回合尾弃牌阶段的判定顺序固定：①retain/selfRetain 卡先撤入 limbo；②Ethereal 消耗（基类 triggerOnEndOfPlayerTurn 经 addToTop 插队）先于弃牌动作执行；③剩余手牌被第一个 DiscardAction 一次性清空（Runic Pyramid/Equilibrium 存在时跳过③）。出处：`DiscardAtEndOfTurnAction#update` + `AbstractCard#triggerOnEndOfPlayerTurn`。置信度高。

**R20** Retain 优于 Ethereal：双重保险（limbo 先行撤离 + ExhaustSpecificCardAction 的 contains 守卫）。出处：同 R19 + `ExhaustSpecificCardAction#update`@12-23。置信度高。

**R21** 手牌各卡的 triggerOnEndOfPlayerTurn 调用顺序**每回合随机**（乱序克隆驱动，用的是无参 java.util.Random 而非 shuffleRng）。出处：`DiscardAtEndOfTurnAction#update`@164-214。置信度高。

**R22** relic.onManualDiscard（Tingsha 类）只在非回合尾弃牌时触发；门控点在 GameActionManager#incrementDiscard 而非 DiscardAction 本身。出处：`GameActionManager#incrementDiscard`@8-63。置信度高。

**R23** RestoreRetainedCardsAction 清 `retain` 但不清 `selfRetain`；恢复顺序 = 撤入 limbo 的顺序（原手牌遍历序），逐张加回手牌"顶"（列表尾）。出处：`RestoreRetainedCardsAction#update`。置信度高。

---

## 6. 用户问题裁决

> 问题：战斗开局首回合初始抽牌、手牌中某张牌被消耗（exhaust 触发链）、某张刚抽到的牌自动产生效果三者同时涉及时，确定先后顺序是什么？

**前提澄清**（指派原文举例"Havoc 类 triggerWhenDrawn"，字节码反证，见 §3 R24）：desktop-1.0.jar v2.x 中 **Havoc 没有 triggerWhenDrawn**——它是"打出时"经 use()→PlayTopCardAction 打抽牌堆顶。真正定义"抽到时触发"的是本 jar 实际覆盖 triggerWhenDrawn 的 **5 个类**：VoidCard、Doubt、EndlessAgony、DeusExMachina、Eviscerate（另有弃用透传 DEPRECATEDStepAndStrike）。以下裁决以这 5 个真实类为准；Havoc/Mayhem 作为对照反例排除在外。

**总裁决（唯一确定答案）**：三者不存在交错执行，顺序完全由 GameActionManager 单活动 action 串行模型（P1，另见 `action-manager.md`）决定：

1. **初始抽牌是一个原子动作块。** 回合切换序列（共享规范⑤；`action-manager.md` R19 / `turn-phase.md` R16/R17 已载）把 `DrawCardAction(null, gameHandSize, true)` 入队；它占住队首直到 amount 归零才 isDone。期间任何已排队 action、以及抽牌过程中 triggerWhenDrawn 新入队的 action，都**必须等整批抽完**。
2. **每张被抽卡的内部顺序固定**（R05）：视觉 → `triggerWhenDrawn()` 同步直调 → 进手牌 → 离开抽牌堆 → powers.onCardDraw → relics.onCardDraw。triggerWhenDrawn 发生在该卡尚未进手、尚未离堆的瞬间。
3. **抽牌全部完成后**才轮到下一个 action：
   - 消耗手牌某卡的 ExhaustSpecificCardAction/ExhaustAction 排在 DrawCardAction 之后 → 此刻执行，钩子链严格按 R14（relics.onExhaust → powers.onExhaust → 该卡 triggerOnExhaust → 移除 → 进消耗堆顶）；
   - 排在 DrawCardAction 之前 → 先完整执行完毕，抽牌随后开始（如 start-of-combat 效果先行消耗的情形）。
4. **"刚抽到的牌自动产生的效果"永远排在整批抽牌之后、按各自入队位置执行**：
   - VoidCard：`addToBot(LoseEnergyAction(1))` → 队尾，在本批抽牌结束后、且排在所有 addToTop 动作之后；
   - EndlessAgony：`addToTop(MakeTempCardInHandAction)` → 抽牌一结束立即执行（同批出现时先于 Void 的 LoseEnergy）；
   - DeusExMachina：双 addToTop 反转 → **先自耗**（走 R14 全链，含全体 relics/powers onExhaust）**后生成 Miracle**；
   - Eviscerate：纯同步改费，无队列事件；
   - Doubt：抽到时只复位 dontTriggerOnUseCard；Weak 要等**回合末**经 cardQueue 自动打出（use() 内 ApplyPowerAction）才施加——不属于抽牌时刻的事件链。

**逐步事件链示范**（确定性排序；设首回合 gameHandSize=5，抽牌堆自顶向下次序为 A、B、EndlessAgony、Void、C；此前已有入队的 ExhaustSpecificCardAction(E)，E 在手牌中，且排在 DrawCardAction 之前）：

| 步 | 事件 | 出处 |
|---|---|---|
| 1 | ExhaustSpecificCardAction(E)：E 仍在手 → `hand.moveToExhaustPile(E)`：全体 relics.onExhaust(E) → 全体 powers.onExhaust(E) → E.triggerOnExhaust() → E 移出手牌、进消耗堆顶 | `ExhaustSpecificCardAction#update`; `CardGroup#moveToExhaustPile` |
| 2 | DrawCardAction 开始，第 1 帧：shuffleCheck 通过，抽 A：A.triggerWhenDrawn()（空）→ A 进手 → 离堆 → powers/relics.onCardDraw(A) | `AbstractPlayer#draw(int)` |
| 3 | 第 2 帧：抽 B，同上 | 同上 |
| 4 | 第 3 帧：抽 EndlessAgony：其 triggerWhenDrawn **此刻同步执行** `addToTop(MakeTempCardInHandAction(copy))`——动作入队但不打断抽牌；EndlessAgony 本体继续进手/离堆/onCardDraw | `EndlessAgony#triggerWhenDrawn`; P1 |
| 5 | 第 4 帧：抽 Void：`addToBot(LoseEnergyAction(1))` 只入队 | `VoidCard#triggerWhenDrawn` |
| 6 | 第 5 帧：抽 C，同构 | — |
| 7 | amount==0 → endActionWithFollowUp：isDone | `DrawCardAction#update`@407-417 |
| 8 | 队首变为 MakeTempCardInHandAction：EndlessAgony 副本进手 | P1 + R12 |
| 9 | 队首变为 LoseEnergyAction：失去 1 点能量 | P1 |

若把 ExhaustSpecificCardAction(E) 挪到 DrawCardAction 之后（第 1 步与第 2-7 步互换），唯一变化是 E 的消耗发生在 C 进手之后、MakeTemp/LoseEnergy 之前或之后取决于其原始入队位置——**无论哪种摆放，答案都唯一且可由上述规则机械推出**；不存在"消耗链打断抽牌"或"抽到时效果抢先于整批抽牌"的可能。

---

## 7. 开放问题 / 低置信项

1. **DiscardAction 全弃分支的冗余循环**：DiscardAtEndOfTurnAction 给每个 DiscardAction 传 `amount=当时手牌数`，导致首个 action 清空全手、其余空转——字节码确凿（高置信），但属设计意图还是重构残留无法从二进制判断；移植时建议语义化为一枚"清空剩余手牌"动作并保留 endTurn 门控。
2. **triggerOnManualDiscard 的分支不对称**：DiscardAction random 分支无视 endTurn 恒触发卡侧钩子，全弃/选择分支受 endTurn 门控。是否影响实卡行为预期需全卡扫描覆盖该钩子的类（本卷未逐一核对）。
3. **ExhaustAllCards 类效果**：jar 中无 `ExhaustAllCardsAction` 类名；"消耗全部手牌"均由 ExhaustAction(amount=99/anyNumber 变体) 实现。若有遗漏专用 action 名，低置信待查。
4. **Necronomicurse 文案 vs 代码**：代码无 isEthereal 标志；wiki 页面描述（未逐字核对，低置信）可能标注 Ethereal。移植以代码为准，并在本地化文本中复核关键词注入方式。
5. **Soul 动画极端竞态**：SoulGroup.isActive() 让路守卫存在于 DrawCardAction/FastDrawCardAction，但 moveToDiscardPile/moveToExhaustPile 直调路径无对应守卫；理论上动画堆积不影响逻辑仅影响表现帧。静态字节码推断，中置信，未动态验证。
6. **queueExtraCard 调用方全集**（哪些效果会把牌标记 purgeOnUse 后自动打出）未穷尽枚举，属自动打出专题，建议由触发器卷（MechKB.TTrig）覆盖。
