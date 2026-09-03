# 死亡与免死仲裁（Death & Lethality Arbitration）— StS1 战斗语义知识库

## 本卷范围
回答"玩家到底什么时候死、什么能救、谁先谁后"类问题：致死来源分类学（damage/LoseHP/InstantKill/Suicide/渎神）、`AbstractPlayer#damage` 尾部免死拦截链的逐步字节码、各免死/减伤手段的挂点层级、**渎神(Blasphemy) vs 无实体(Intangible) 旗舰仲裁**（含新回合块内的入队时序推导）、治疗侧封死（Mark of the Bloom）、怪物侧自杀与 cannotLose 简表。
依赖并引用：`damage-pipeline.md`（伤害全序 R03/R04、格挡吸收 R13、LoseHP 分流 R14、heal R15）、`triggers.md`（§5.5 玩家救援链、§7 受击钩子序）、`turn-phase.md`（R13 新回合块）。

**图例**：出处格式 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+调用链推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x（2022-12-20，含观者）。本卷偏移实测自 `javap -c -p`。

---

## 1. 玩家死亡判定总闸（AbstractPlayer#damage 尾部）

**R01 currentHealth<1 是唯一死亡闸** — 出处 `AbstractPlayer#damage` offset 952-957。置信度：**高**
扣血完成后（HP 减法与计数器在更早的偏移 648-668，见 damage-pipeline.md R04）执行 `if (currentHealth < 1)` 进入救援/死亡判定。**与伤害类型无关**（NORMAL/THORNS/HP_LOSS 都走这里），与是否被格挡无关（格挡吸收在入口）。

**R02 拦截链固定三段，Mark of the Bloom 短路一切** — 出处 offset 960-1101。置信度：**高**
```
if (currentHealth < 1):
  ① hasRelic("Mark of the Bloom") → 跳过全部救援，直达死亡分支      [961-967]
  ② hasPotion("FairyPotion") → 遍历 potions 找首个 FairyPotion:
       flash(); currentHealth=0; potion.use(this); destroyPotion(slot); return
       —— return = 不进入死亡分支                                   [970-1052]
  ③ hasRelic("Lizard Tail") && counter == -1:
       currentHealth=0; getRelic.onTrigger(); return（不死）          [1059-1101]
  ④ 否则死亡分支: isDead=true; new DeathScreen(=getMonsters());
       currentHealth=0; currentBlock>0 → loseBlock() + 碎盾视效       [1102-1191]
```
要点：
- **玩家没有 `die()` 方法**——死亡即 `isDead=true`+DeathScreen，不存在怪物式的 onDeath 钩子链（对照 triggers.md §5.1 怪物 die()）。
- 拦截点在**全部伤害钩子之后、方法 return 之前**：onLoseHp/wasHPLost/onInflictDamage 已发完、计数器已累加，救援只是把 currentHealth 从 0 拉回来。
- FairyPotion 检查用 `hasPotion` + 遍历取**首个**实例；多瓶妖精只消耗一瓶。

**R03 FairyPotion 自动使用是直调，绕过 canUse()** — 出处 `FairyPotion#canUse` offset 0-1（恒 `iconst_0; ireturn` = 手动不可用）+ `AbstractPlayer#damage` offset 1035-1046 直调 `potion.use(this)` + `FairyPotion#use` offset 0-44。置信度：**高**
`use()`：`healAmount = maxHealth × potency/100`（potency=30，`getPotency` offset 0-2）；`< 1 → 1`；`player.heal(healAmount, true)`；`destroyPotion(slot)`。即**回复 30% 最大生命（至少 1）**。自动使用不经 canUse 门禁（妖精药水本来也不允许手动喝）。

**R04 Lizard Tail 一次性语义** — 出处 `LizardTail#onTrigger` offset 0-48 + `#setCounter` offset 0-13。置信度：**高**
`onTrigger`：flash + addToTop(RelicAboveCreatureAction) → `heal(max(maxHealth/2, 1), true)`（回复 50% 最大生命）→ `setCounter(-2)`；counter==-2 时 `usedUp()`（遗迹变灰）。damage() 侧的门是 `counter == -1`（-1=未用状态；-2=已消耗），因此蜥蜴尾整场战斗只救一次。

**R05 heal(int, boolean) 的布尔参数是纯视觉开关** — 出处 `AbstractCreature#heal(int,boolean)` offset 223-278（`boolean==true && isPlayer` 才播 TopPanel.panelHealEffect + HealEffect）。置信度：**高**
relic onPlayerHeal 链（offset 43-90）与 power onHeal 链（90-128）**不受**该布尔影响。妖精/蜥蜴尾的回复照样吃 Mark of the Bloom 的 onPlayerHeal=0（R12）——但渎神场景下 MotB 根本走不到这（R02① 已短路）。isDying 早退（offset 35-42）+ Endless 模式 FullBelly blight 治疗减半（offset 0-34）为方法头两个守卫。

---

## 2. 玩家致死来源分类学

| 来源 | 实现 | 类型 | 可格挡 | 免死拦截是否可达 |
|---|---|---|---|---|
| 常规攻击/卡牌伤害 | `damage(DamageInfo)` | NORMAL/THORNS | NORMAL/THORNS 可 | 可（R01） |
| 效果失血 | `LoseHPAction` → `damage(HP_LOSS)` | HP_LOSS | 否（R13 of damage-pipeline） | 可 |
| 渎神下回合死 | `EndTurnDeathPower.atStartOfTurn` → `LoseHPAction(99999, HP_LOSS)` | HP_LOSS | 否 | 可（见 §4/§5） |
| 审判处决 | `InstantKillAction`（仅 JudgementAction 使用） | HP_LOSS（amount=0） | 否 | 可（理论，见 R07） |
| 怪物自杀/分裂消失 | `SuicideAction`（怪物侧） | —（直改血量+die） | — | 不适用（怪物无免死） |

**R06 LoseHPAction 是 HP_LOSS 的唯一队列化入口** — 出处 `LoseHPAction#update`（详见 damage-pipeline.md R14）。置信度：**高**

**R07 InstantKillAction：先置 0 再走 0 伤 HP_LOSS** — 出处 `InstantKillAction#update` offset 0-39。置信度：**高**
```
target.currentHealth = 0; healthBarUpdatedEvent();
target.damage(new DamageInfo(null, 0, HP_LOSS)); isDone=true
```
- 对**怪物**：currentHealth 已为 0 → 走 `AbstractMonster#damage` 的 `hp<=0 → die()` 分支（damage-pipeline.md R03 步骤⑭），0 伤不吃格挡/无实体/Buffer（它们的门都是 `amount>0` 或 `>1`）→ **处决无视一切减伤**。
- 对**玩家**（当前无调用者，理论语义）：currentHealth=0 → damage 走完 → `<1` 闸命中 → 妖精/蜥蜴尾**可以**救（0 伤不消耗 Buffer——`damage>0` 门）。
- 全 jar 唯一使用者：`JudgementAction`（常量池扫描，2306 class 全量）。`JudgementAction#update` offset 11-47：`target.currentHealth <= cutoff && target instanceof AbstractMonster → addToTop(InstantKillAction(target))`。

**R08 SuicideAction（怪物侧自杀）清零金币** — 出处 `SuicideAction#update` offset 9-40。置信度：**高**
首帧：`m.gold=0; m.currentHealth=0; m.die(relicTrigger); healthBarUpdatedEvent()`。金币清零意味着分裂型自杀不给击杀奖励。调用者（常量池扫描全量）：SlimeBoss、AcidSlime_L、SpikeSlime_L（分裂）、TheCollector、Reptomancer（召唤物退场）、BronzeAutomaton、`FadingPower`（消失诅咒怪）、`ExplosivePower`（爆炸亡语）。

**R09 玩家致死没有"绕过拦截"的原生路径** — 推论（置信度：**高**，基于 §1/§2 全量枚举）：战斗内玩家一切 HP 减少最终都汇入 `AbstractPlayer#damage`（R06/R07 的两个 action 也如此），因此除 MotB 短路外总有拦截机会。战役层（事件扣血等）不在本卷范围（开放问题 4）。

---

## 3. 渎神专题（Blasphemy / EndTurnDeathPower）

**R10 渎神实现：换姿态 + 上 power，两动作入队** — 出处 `Blasphemy#use` offset 0-33。置信度：**高**
```
use(): addToBot(new ChangeStanceAction("Divinity"));
       addToBot(new ApplyPowerAction(player, player, new EndTurnDeathPower(player)));
```
1 费 SKILL·RARE·自定目标；构造器置 `exhaust=true`（offset 33-35）；upgrade：`selfRetain=true` + 换 UPGRADE_DESCRIPTION（offset 7-27）——**升级不改死亡时序，只加保留**。`EndTurnDeathPower` 全 jar 仅 Blasphemy 引用（常量池扫描）。

**R11 EndTurnDeathPower：回合开始闪电 + 99999 失血 + 自移除** — 出处 `EndTurnDeathPower#atStartOfTurn` offset 0-84。置信度：**高**
```
atStartOfTurn():
  flash();
  addToBot(new VFXAction(new LightningEffect(owner.hb.cX, owner.hb.cY)));   // 视觉
  addToBot(new LoseHPAction(owner, owner, 99999));                          // ← 死亡本体
  addToBot(new RemoveSpecificPowerAction(owner, owner, "EndTurnDeath"));
```
字段：ID="EndTurnDeath"，`amount=-1`，type 默认 BUFF（**Artifact 不拦截它**，见 status-stacking.md 的 DEBUFF 门）。**"死亡"就是一个 99999 点 HP_LOSS**——这是整卷最重要的单一事实。

**R12 死亡时点：新回合块的 start-of-turn power 梯内入队，执行早于新回合抽牌** — 出处 `GameActionManager#getNextAction` offset 2083-2202 + `applyStartOfTurnPowers` 迭代 player.powers。置信度：**高**
新回合块顺序（偏移实录）：applyEndOfTurnPowers[2014] → 计数复位 → applyStartOfTurnRelics[2065] → PreDrawCards[2071] → Cards[2077] → **Powers[2083]（EndTurnDeathPower.atStartOfTurn 在此把三动作 addToBot）** → Orbs[2089] → turn++ → 玩家掉格挡 → **DrawCardAction 入队[2199]** → PostDraw 钩子 → EnableEndTurnButton。
addToBot 为 FIFO ⇒ 队列序 `[闪电VFX → LoseHP(99999) → Remove] → DrawCardAction`。**死在抽新牌之前**；若被救下（R02②③），抽牌照常发生。

**R13 多次渎神不叠加** — 出处 `ApplyPowerAction#update` offset 589-606（同 ID 且非 Night Terror → `existing.stackPower(this.amount)`，不新建实例）。置信度：**高**（合并分支）/ **中**（3 参构造的 stackAmount 语义：EndTurnDeathPower 未覆写 stackPower，合并后仅 amount 数值变化，触发行为仍是"每回合开始一次"）。下一回合开始时 Remove 已把 power 拿掉——若玩家被救，渎神死只发生一次。

**R14 战斗在下次回合开始前结束则渎神不触发** — 出处 `getNextAction` offset 1990-1996：新回合块以 `!areMonstersBasicallyDead()` 为门。渎神当回合打死全部怪物 ⇒ 直接胜利，`EndTurnDeathPower` 永不执行。置信度：**高**

---

## 4. 旗舰仲裁：渎神 vs 无实体（用户指定验收项）

**结论先行**：
- **1 层无实体：渎神胜**（照样死）；
- **≥2 层无实体：无实体胜**（99999 被钳为 1，失血 1 点存活）；
- 妖精药水 / 蜥蜴尾（无 MotB 时）：都能救；
- Buffer：每层挡一次，能挡渎神死。

**R15 无实体钳制点一：`AbstractPlayer#damage` 入口，无类型门控** — 出处 offset 0-38（本卷实测复核 damage-pipeline.md R04）。置信度：**高**
```
amount = info.output;                        [0-4]
if (amount < 0) amount = 0;                  [16-21]
if (amount > 1 && hasPower("IntangiblePlayer")) amount = 1;   [22-38]
decrementBlock(info, amount);                [39-45]
```
门条件只有 `>1`，**没有 DamageType 检查** ⇒ HP_LOSS（含渎神 99999）在入口同样被钳为 1。这是"无实体能碰渎神"的唯一通道。

**R16 无实体钳制点二：计算链 `atDamageFinalReceive` 不参与失血** — 出处 `IntangiblePlayerPower#atDamageFinalReceive`（`>1 → 1`）。置信度：**高**
该钩子只在 `calculateCardDamage`/`DamageInfo.applyPowers` 计算链被调（damage-pipeline.md R06/R07）；`LoseHPAction` 直接构造 DamageInfo、**不调 applyPowers** ⇒ 计算链钳制对渎神无效。若没有 R15 的入口钳制，无实体对 HP_LOSS 本无能为力。

**R17 无实体的持续期递减发生在 `atEndOfRound`，且比渎神先入队** — 出处 `IntangiblePlayerPower#atEndOfRound` offset 0-57 + `MonsterGroup#applyEndOfTurnPowers` offset 48-84 + `getNextAction` offset 2014 vs 2083。置信度：**高**
```
IntangiblePlayerPower.atEndOfRound():
  flash();
  if (amount == 0) addToBot(RemoveSpecificPowerAction);
  else             addToBot(ReducePowerAction(owner, owner, "IntangiblePlayer", 1));
```
`atEndOfRound` 的调用点在 `MonsterGroup.applyEndOfTurnPowers()` 内部（玩家 powers 段 offset 48-84），而该方法在新回合块**第一步**[2014] 执行；渎神的 `atStartOfTurn` 在**第五步**[2083] 执行。全部 addToBot ⇒ 无实体的 Reduce/Remove 在渎神的 LoseHP **之前**。

**R18 ReducePowerAction 把 1 层无实体直接移除** — 出处 `ReducePowerAction#update` offset 52-98。置信度：**高**
`if (reduceAmount >= power.amount) addToTop(new RemoveSpecificPowerAction(...)) else power.reducePower(1)`。
1 层（amount=1，Reduce(1)）：`1>=1` → addToTop(Remove) → power 被移除；2 层：`1>=2` 假 → reducePower(1) → 剩 1 层保留。

**R19 裁决（时序逐帧表）** — 综合 R15-R18。置信度：**高**
新回合块产生的 actions 队列（FIFO）：

| 无实体层数 | 队列内容（按入队序） | LoseHP 结算时 power 状态 | 结局 |
|---|---|---|---|
| 0 | [VFX, LoseHP 99999, Remove] | 无 | 死亡（或触发 R02 拦截） |
| 1（Apparition/Ghost 类，上一回合获得） | [**Reduce→addToTop(Remove)**, VFX, LoseHP 99999, Remove] | 已被移除 | **99999 全额 → 死亡** |
| ≥2 | [**Reduce(1)**, VFX, LoseHP 99999, Remove] | 剩 ≥1 层 | **入口钳 1 → 失血 1 → 存活** |

注 1：1 层场景的 `addToTop(Remove)` 是在 ReducePowerAction 执行帧插入队首，仍先于其后的 VFX/LoseHP。
注 2：≥2 层时本回合结束后的下一个 atEndOfRound 会把最后 1 层移除——钳制只挡本次。
注 3：若渎神与无实体**同回合**施加（先渎神后无实体，如渎神+鬼瓶药水）：power 列表顺序只影响 atStartOfTurn 钩子的遍历序，不影响 R17 的块级先後（atEndOfRound 永远在 atStartOfTurn 之前），裁决不变。

**R20 拦截手段对照矩阵** — 出处：各行见 R02/R03/R04/R18/R12 与 §5。置信度：**高**

| 手段 | 挂点 | 对渎神死(99999 HP_LOSS) | 备注 |
|---|---|---|---|
| 无实体 ×1 | damage() 入口钳 + atEndOfRound 到期 | **不救**（先到期后失血） | R17-R19 |
| 无实体 ×≥2 | 同上 | **救**（失血 1） | 钳制不消费层数 |
| Buffer ×N | powers.onAttackedToChangeDamage（无类型门控，`>0 → 返回0` + addToTop(Reduce 1)） | **救**（消费 1 层） | 见 defense-powers 卷 R05 |
| 妖精药水 | damage() 尾部拦截链第 2 位 | **救**（回 30% maxHP，消耗药水） | R03 |
| 蜥蜴尾 | 拦截链第 3 位（counter==-1） | **救**（回 50% maxHP，一次性） | R04 |
| Mark of the Bloom | 拦截链第 1 位短路 | **不救**（且 onPlayerHeal=0 封死救援治疗） | R02/R12 |
| 钨杆 TungstenRod | relics.onLoseHpLast（`>0 → -1`） | **不救**（99998 仍致死） | 可把 1 点失血降为 0 |
| 怪物 Invincible 预算 | powers.onAttackedToChangeDamage（钳到剩余预算） | 仅当玩家持有才有意义（原生无此场景）；机制上 `99999 → 预算余额` | defense-powers 卷 R08 |
| 格挡 / Barricade | decrementBlock | **无效**（HP_LOSS 直通） | damage-pipeline R13 |
| 易伤/虚弱/力量 | 计算链 | **无效**（LoseHPAction 不走 applyPowers） | R16 |
| Artifact | ApplyPowerAction 的 DEBUFF 门 | **无效**（EndTurnDeathPower 是 BUFF） | R11 |

---

## 5. 治疗侧封死与怪物侧简表

**R21 Mark of the Bloom 双重封死** — 出处 `MarkOfTheBloom#onPlayerHeal` offset 0-5（flash + `ireturn 0`）。置信度：**高**
位置在 heal() 的 relic 链首位（R05 offset 43-90）⇒ 一切治疗归零（含妖精/蜥蜴尾救援的后续治疗——但它们根本到不了这步，R02① 已先短路）。

**R22 cannotLose：压制"全场怪物死亡→胜利"判定** — 出处 `AbstractRoom.cannotLose` 读者 = `AbstractMonster#updateDeathAnimation`（triggers.md §5.2：`areMonstersDead && !isBattleOver && !cannotLose → endBattle`）。写入者 = `CannotLoseAction`/`CanLoseAction`（引用者常量池扫描：Darkling、AwakenedOne、TimeEater、CorruptHeart）。置信度：**高**（门与写入者名单）/ **中**（各怪物开/关 cannotLose 的具体时机未逐一取证，开放问题 5）。

---

## 6. 开放问题 / 低置信项

1. **多 Buff 同帧叠加的极端序**：渎神 + 鬼瓶（同回合上无实体）时 R19 注 3 已裁决；但"渎神回合内无实体被 Time Eater 重置回合"一类跨跳回场景未枚举。置信度：**中**。
2. **ReducePowerAction 的 powerInstance 构造变体**（4 参，按实例定位）与 ID 变体在"同名多实例"下的差异未取证（vanilla 同 ID 合并，理论无多实例）。置信度：**低**，不影响本卷结论。
3. **InstantKillAction 对玩家** 的运行时表现（0 伤 + currentHealth 预置 0）为静态推导，无调用者可测。置信度：**中**。
4. **战斗外（事件层）致死**是否经过同一拦截链未取证（事件多用独立 HP 操作）。归战役层卷。置信度：**未定**。
5. Darkling/AwakenedOne/TimeEater/CorruptHeart 各自 cannotLose 的开关时机与 halfDead 的写入点未读方法体（triggers.md 开放问题 4 同源）。置信度：**中**。
6. `EndTurnDeathPower` 合并分支 R13 中 3 参 ApplyPowerAction 的 stackAmount 具体值（-1）未从构造器字节码逐字取证（status-stacking.md 归档过全分支，此处引用其结论）。置信度：**中**。
