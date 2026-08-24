# 伤害管线（Damage Pipeline）— StS1 战斗语义知识库

## 本卷范围
以 `AbstractCreature.damage(DamageInfo)` 为中心，覆盖：`DamageInfo`/`DamageType` 结构与语义、防守端结算全序（怪物与玩家两条路径）、进攻端乘区修改器链（卡牌侧 `applyPowers`/`calculateCardDamage` 与通用侧 `DamageInfo.applyPowers`）、多重打击快照语义、格挡获得/损失机制、LoseHP/Heal 分流。
**不含**：力量等状态施加合并（status-stacking.md）、抽牌/消耗/弃牌（draw-exhaust.md）、回合尾掉格挡在流水线中的位置（action-manager.md / turn-phase.md，Barricade 免除已录主代理上下文）。

**图例**：每条规则标注出处 `类名#方法` + 置信度（**高**=javap 字节码直接可证；**中**=字节码+调用链推断；**低**=仅 wiki/间接证据）。字节码摘录 ≤6 行。本 jar 版本 v2.x（含 Watcher），注意 `AbstractCreature.damage` 在本版为 **abstract**（`AbstractCreature#damage` 声明 `public abstract void damage(DamageInfo)`），实际逻辑在 `AbstractMonster#damage` 与 `AbstractPlayer#damage` 两份近似但不同的实现中——移植仲裁时不可当作单一函数处理。wiki 交叉校验本次尝试访问 fandom 失败（HTTP 403/404），全部结论以反编译字节码为准。

---

## 1. DamageInfo 与 DamageType

**R01 DamageInfo 字段与消费方式** — 出处 `DamageInfo`（javap -p）+ `AbstractCreature#decrementBlock`。置信度：**高**
字段全集：`owner: AbstractCreature`、`name: String`、`type: DamageType`、`base: int`、`output: int`、`isModified: boolean`。
- `base` 是原始值；各修改器链用 `(int)tmp != base` 判断是否置 `isModified=true`；
- 防守端 `damage(info)` 只读 **`info.output`**（`AbstractMonster#damage` 第 0-4 条指令 `getfield DamageInfo.output; istore_2`）；`applyPowers` 类方法负责把 `base` 加工进 `output`。
- 构造器 `DamageInfo(owner, n)` 默认 `type=NORMAL`（`di.txt` 偏移 2-6：`invokespecial <init>(owner, int, NORMAL)`）。

**R02 DamageType 全集 = {NORMAL, THORNS, HP_LOSS}，无其他值** — 出处 `DamageInfo$DamageType`（javap -p 仅三个常量）。置信度：**高**

| 类型 | 吃攻方 give 链(力量/虚弱/笔尖) | 吃守方 receive 链(易伤) | 可被格挡 | 计数器影响 |
|---|---|---|---|---|
| NORMAL | 是 | 是 | 是 | `damageReceivedThisTurn/Combat`++（玩家受击）；触发全部 onAttack/onAttacked/wasHPLost/onLoseHp 钩子 |
| THORNS | 否（所有 hook 均 `if_acmpeq NORMAL` 门控） | 否 | **是**（见 R10） | 不再触发 Thorns 反伤（ThornsPower 自环防护）；Lightning 电球伤害即此类型 |
| HP_LOSS | 否 | 否 | **否**（decrementBlock 直通） | 玩家侧 `GameActionManager.hpLossThisCombat += amount`（仅此类型累加）；仍走 onLoseHpLast/onLoseHp/wasHPLost |

证据：`StrengthPower#atDamageGive` 偏移 0-4 `aload_2; getstatic NORMAL; if_acmpeq`（跳过则原样返回）；`VulnerablePower#atDamageReceive` 同构门控；`ThornsPower#onAttacked` 偏移 0-17 对 THORNS/HP_LOSS 直接跳过；`AbstractPlayer#damage` 偏移 715-730 `if type==HP_LOSS → hpLossThisCombat += amount`。

---

## 2. 防守端结算序

**R03 怪物受伤完整顺序** — 出处 `AbstractMonster#damage`（am.txt 偏移 0-831）。置信度：**高**
```
① 若 info.output>0 且自身有 "IntangiblePlayer" → info.output=1   （偏移7-19）
② damageAmount = info.output；isDying||isEscaping → return；<0 → 0
③ 记录 hadBlock=(currentBlock!=0)
④ damageAmount = decrementBlock(info, damageAmount)   ← 格挡最先吸收（偏移70-76）
⑤ info.owner==player → player.relics 逐个 onAttackToChangeDamage(info,amt)→amt
⑥ info.owner!=null → owner.powers    逐个 onAttackToChangeDamage(info,amt)→amt
⑦ self.powers       逐个 onAttackedToChangeDamage(info,amt)→amt
⑧ info.owner==player → player.relics onAttack(info,amt,this)      [void]
⑨ self.powers        逐个 wasHPLost(info,amt)                     [void]
⑩ info.owner!=null → owner.powers onAttack(info,amt,this)         [void]
⑪ self.powers        逐个 onAttacked(info,amt)→amt                （可改最终值，如 Thorns 挂反射）
⑫ lastDamageTaken = min(damageAmount, currentHealth)
⑬ damageAmount>0 → 扣血 currentHealth-=amount（clamp≥0）+healthBarUpdatedEvent()
⑭ currentHealth≤0 → die()；若 areMonstersBasicallyDead() → cleanCardQueue+DeckPoof+hideCombatPanels；
   死亡时若仍有 block → loseBlock() 清空
```
要点：**格挡吸收发生在所有 change-damage 钩子之前**，⑤⑦⑪ 看到的是穿透格挡后的余量；Thorns 反伤（onAttacked）在扣血之前入队（addToTop）但不立即结算。

**R04 玩家受伤完整顺序（与怪物路径的差异点）** — 出处 `AbstractPlayer#damage`（ap.txt 偏移 0-1372）。置信度：**高**
前缀同 R03 ①②④（玩家版 Intangible 判定在 clamp 之后：`amount>1 && hasPower("IntangiblePlayer") → amount=1`）。钩子序列：
```
⑤ owner==self → relics.onAttackToChangeDamage → owner.powers.onAttackToChangeDamage
⑦ relics.onAttackedToChangeDamage → powers.onAttackedToChangeDamage
⑧ owner==self → relics.onAttack → owner.powers.onAttack
⑪ powers.onAttacked → relics.onAttacked          （relic 版本存在且排在 power 之后）
⑭ relics.onLoseHpLast(amount)→amount             （Tungsten Rod -1 挂点）
⑮ lastDamageTaken = min(amount, currentHealth)
⑯ amount>0 时：powers.onLoseHp(amount)→amount → relics.onLoseHp → wasHPLost(powers→relics)
   → owner.powers.onInflictDamage(info,amount,this)
   → 计数器（hpLossThisCombat[仅HP_LOSS]/damageReceivedThisTurn/damageReceivedThisCombat）
   → 扣血 → updateCardsOnDamage(): 手牌+弃牌堆+抽牌堆逐卡 tookDamage() → damagedThisCombat++
⑰ health<1 → Mark of the Bloom 检查 → FairyPotion 自动使用(return) → Lizard Tail(counter==-1) 触发(return)
   → 否则 isDead=true + DeathScreen + health=0 + loseBlock()
```
要点：玩家路径**没有 die() 调用**，死亡被药水/蜥蜴尾拦截后直接 return；`NO OWNER, DON'T TRIGGER POWERS` 日志分支证明 owner==null 时钩子层整体跳过但仍扣血。

**R05 Vulnerable ×1.5 的真实位置：不在 damage() 内，而在进攻端计算链** — 出处 `AbstractMonster#damage`/`AbstractPlayer#damage` 全文无 atDamageReceive/vulnerable 引用；`VulnerablePower#atDamageReceive` 偏移 66-70。置信度：**高**
```java
// VulnerablePower#atDamageReceive（仅 NORMAL 门控）
28: Odd Mushroom  → dmg * 1.25f
62: Paper Frog    → dmg * 1.75f
67: 默认           → dmg * 1.5f
```
即：卡牌伤害的易伤加成在 **出牌前计算 `this.damage` 时** 已并入（R06 第⑥层）；`DamageInfo.output` 到达 `damage()` 时已是含易伤终值。**向下取整发生在整条链尾一次性 `MathUtils.floor`**（R06），不是每个乘区后取整——`(base+力)*0.75*1.5` 全程浮点，最后 floor。HP_LOSS/THORNS 类型完全绕过该层。

---

## 3. 进攻端修改器链

**R06 卡牌链：relics → powers.give → stance → 目标 receive → finalGive → 目标 finalReceive → floor** — 出处 `AbstractCard#calculateCardDamage(AbstractMonster)`（card.txt 偏移 24-322）与 `AbstractCard#applyPowers()`（偏移 20-603）。置信度：**高**
单目标 `calculateCardDamage(m)` 完整顺序（每步均以 float 传递）：
```
tmp = baseDamage
① player.relics   r.atDamageModify(tmp, this)     ← WristBlade +4 挂点，位于一切 power 之前！
② player.powers   p.atDamageGive(tmp, damageTypeForTurn, this)
      StrengthPower.atDamageGive: NORMAL→ tmp + amount   （pw.txt 201-211 fadd）
      WeakPower.atDamageGive:     NORMAL→ tmp*0.75f（Paper Crane relic 时 *0.6f，pw.txt 444-464）
      PenNibPower.atDamageGive:   NORMAL→ tmp*2f      （pw.txt 1264-1272，v2.x 中笔尖在 give 层而非 final 层）
③ player.stance.atDamageGive(tmp, type, this)
      WrathStance: NORMAL→ ×2f；DivinityStance.atDamageGive: NORMAL→ ×3f
④ m.powers        p.atDamageReceive(tmp, type, this)   ← Vulnerable ×1.5 挂点（仅 calculateCardDamage 有）
⑤ player.powers   p.atDamageFinalGive(...)
⑥ m.powers        p.atDamageFinalReceive(...)          （IntangiblePower 即挂此层：>1 → 1）
⑦ tmp<0 → 0；this.damage = MathUtils.floor(tmp)
```
- `applyPowers()`（无目标预览）= 同链去掉 ④⑥ 两层目标侧；`isMultiDamage==true` 时对每个敌人独立跑一遍得到 `multiDamage[i]`，`damage = multiDamage[0]`。
- **关键序点**：relics 层在最前 ⇒ Wrist Blade 的 +4 发生在 Weak/Pen Nib 乘区**之前**（例：base5+腕刃4=9 → 弱×0.75=6.75 → floor 6，而非 (5×0.75)+4）。
- 每个钩子调用后都用 `baseDamage != (int)tmp` 刷新 `isDamageModified`（用于 UI 显示灰色数字）。
- `WristBlade#atDamageModify` 条件（pw.txt 1586-1603）：`costForTurn==0 || freeToPlayOnce || cost==-1(X费)` → +4，比 wiki 口径"费用0"更宽。

**R07 通用链（非卡牌来源）：DamageInfo.applyPowers(owner, target)** — 出处 `DamageInfo#applyPowers(AbstractCreature,AbstractCreature)`（di.txt 偏移 45-648）。置信度：**高**
```
output = base; isModified=false; tmp=(float)output
若 owner 为怪物：[Endless DeadlyEnemies blight ×effect]
① owner.powers atDamageGive(tmp,type)          （无 relic 层、无 card 参数重载版本）
② target.powers atDamageReceive(tmp,type)
③ target==player → player.stance.atDamageReceive(tmp,type)   （Wrath 受伤×2 挂点）
④ owner.powers atDamageFinalGive → ⑤ target.powers atDamageFinalReceive
floor → output（<0→0）
若 owner 为玩家：①②③ 顺序变为 owner.powers.give → player.stance.atDamageGive → target.powers.receive → final 两层
```
变体 `applyEnemyPowersOnly(target)`：只跑 target 的 receive+finalReceive 两层（用于伤害源固定、只随目标变化的场合）。
**已证调用者**：`AbstractMonster#applyPowers`（am.txt 偏移 68-74：对 `this.damage` 列表中每个 DamageInfo 调 `DamageInfo.applyPowers(this, player)`，BackAttack 时再 `output*=1.5f`）。其余调用面未穷举（见开放问题）。

**R08 敌方攻击数值管线（意图数字与实伤同源）** — 出处 `AbstractMonster#applyPowers`（am.txt 3708-3772）+ `AbstractMonster#calculateDamage(int)`（私有，am.txt 3591-3706，仅供 intent 显示）。置信度：**高**
实伤：怪物 takeTurn 用 move 里预先 applyPowers 过的 `DamageInfo.output`。意图显示：`calculateDamage(base)` 私有方法按 怪物powers.give(NORMAL) → player.powers.receive → player.stance.receive → BackAttack×1.5 → finalGive → finalReceive → floor 计算 `intentDmg`，与 R07 同构。

---

## 4. 多重打击快照语义

**R09 结论：vanilla 多段打击 = 单次快照（single snapshot per card play），DamageAction 不重算** — 出处 `DamageAction#update`（act.txt 偏移 104-292）、`Pummel#use`、`TwinStrike#use`、`SkewerAction#update`、`WhirlwindAction#update`。置信度：**高**
- `DamageAction#update`：tickDuration 结束后直接 `target.damage(this.info)`（act.txt 偏移 237-245），**全程无 setValues/无 applyPowers 重调**；仅 shouldCancelAction（source isDying 等）与 THORNS 类型早退检查。
- `Pummel#use`：循环 magicNumber 次，每次 `addToBot(new PummelDamageAction(m, new DamageInfo(p, this.damage, damageTypeForTurn)))` —— 每个 action 一个**新** DamageInfo 实例但都从同一个 `this.damage` 读值 → 每击数值恒等。
- `TwinStrike#use`：两个 DamageAction 各配新 DamageInfo，同样取自同一 `this.damage`。
- `Skewer#use` → `SkewerAction(p,m,this.damage,...)`：update 中 `for i<energy: addToBot(new DamageAction(m, new DamageInfo(p, this.damage /*捕获值*/, type)))`，能量一次扣除。
- `Whirlwind#use` → `WhirlwindAction(player, multiDamage[], ...)`：update 中每击 `addToBot(new DamageAllEnemiesAction(p, this.multiDamage /*共享数组引用*/, type, NONE, true))` —— X 费多击共享同一个 int[] 快照。
- 推论（移植仲裁要点）：出牌瞬间后发生的力量增减（如 Flex 到期、战斗中触发的力量变化）**不影响**已在队列中的后续击；`this.damage` 的取值时刻 = 最后一次 `applyPowers/calculateCardDamage`（悬停瞄准期间每帧刷新）到点击出牌之间。

**R10 唯一逐击重算例外：AttackDamageRandomEnemyAction** — 出处 `AttackDamageRandomEnemyAction#update`（act.txt 偏移 1474-1500+）。置信度：**高**
每次执行先随机选目标，然后 `card.calculateCardDamage(target)` 重算后再建 DamageAction —— 随机目标的多次攻击卡若改用此 action 则逐击快照（每次可能不同目标、不同易伤状态）。与之相对 `DamageRandomEnemyAction#update` 只选随机目标、复用构造时传入的同一 `info`（不重算）。选择哪个 action 由各卡 use() 决定。
另注：`DamageAllEnemiesAction#update` 有 `firstFrame` VFX 预扫 + 玩家 powers `onDamageAllEnemies(int[])` 钩子（可原地改数组，act.txt 偏移 269-309），随后对未死未逃怪物逐个 `m.damage(new DamageInfo(source, damage[i], type))`（偏移 488-525）；`utilizeBaseDamage=true` 变体才走 `DamageInfo.createDamageMatrix(base)` 现算。

---

## 5. 格挡侧

**R11 卡牌格挡修饰链与 Dex/Frail 挂点** — 出处 `AbstractCard#applyPowersToBlock`（card.txt 偏移 8799-8859）+ `FrailPower#modifyBlock` + `DexterityPower#modifyBlock`。置信度：**高**
```
tmp = baseBlock
① player.powers 循环 modifyBlock(tmp, this)：DexterityPower→tmp+=amount（结果<0则0）；FrailPower→tmp*0.75f
② player.powers 循环 modifyBlockLast(tmp)
clamp≥0 → this.block = MathUtils.floor(tmp)
```
注意：Dex/Frail 只作用于**卡牌产生的 block**（baseBlock 经 applyPowersToBlock）；非卡牌来源直接 `GainBlockAction(rawAmount)` → `addBlock(amount)`，构造器原样存值（act.txt 359-390 无任何 modify 调用），**不吃 Dex/Frail**。`modifyBlock` 是单循环顺序应用 ⇒ 同时有 Dex 与 Frail 时结果依赖 power 列表顺序（先加后乘 vs 先乘后加不等）——移植时建议按「获得顺序」建模或明确仲裁。

**R12 addBlock：三层钩子 + floor + 999 上限** — 出处 `AbstractCreature#addBlock`（ac.txt 偏移 1386-1522）。置信度：**高**
```
tmp=(float)amount
① isPlayer → player.relics 循环 onPlayerGainedBlock(F)I→tmp
② tmp>0 → 自身 powers 循环 onGainedBlock(tmp)
③ 全场每个怪物的 powers 循环 onPlayerGainedBlock(tmp)→tmp     （怪物侧反应钩子）
currentBlock += MathUtils.floor(tmp)；cap：>999 → 999
成就：≥99 IMPERVIOUS；==999 BARRICADED
```

**R13 格挡吸收 decrementBlock：溢出穿透、THORNS 可被格挡** — 出处 `AbstractCreature#decrementBlock(DamageInfo,int)`（ac.txt 偏移 445-574）。置信度：**高**
```
type==HP_LOSS            → 原值返回（唯一不可格挡类型；THORNS 会被正常吸收）
currentBlock<=0          → 原值返回
amount >  block          → 余量=amount-block；loseBlock()(清空)+brokeBlock()；返回余量
amount == block          → 返回0；loseBlock()+brokeBlock()
amount <  block          → loseBlock(amount)；返回0
```
返回值作为余量继续走 R03/R04 的钩子链与扣血。**THORNS 类型伤害可被格挡**（decrementBlock 只排除 HP_LOSS）——与部分社区资料说法相反，以字节码为准。
手动掉格挡：`LoseBlockAction#update` → `target.loseBlock(amount)`（act.txt 616-638）；`loseBlock(int,boolean)` 只是减法+clamp+碎盾特效（ac.txt 1524-1600）。

---

## 6. LoseHP / Heal 分流

**R14 LoseHPAction：走 damage() 但 HP_LOSS 直通** — 出处 `LoseHPAction#update`（act.txt 偏移 481-531）。置信度：**高**
`target.damage(new DamageInfo(source, amount, HP_LOSS))`。效果：不碰格挡（R13）、不吃易伤/虚弱/力量/笔尖（R02 门控）、仍触发玩家的 onLoseHpLast（钨杆 -1 生效于 HP_LOSS）、计入 hpLossThisCombat。HealAction 完全独立：`HealAction#update` → `target.heal(amount)`（act.txt 583-598）。

**R15 heal() 路径：onPlayerHeal → onHeal → 加血封顶** — 出处 `AbstractCreature#heal(int,boolean)`（ac.txt 偏移 1245-1376）。置信度：**高**
isDying 早退（死者不可治疗）→ isPlayer 时 relics 循环 `onPlayerHeal(amount)→amount` → powers 循环 `onHeal(amount)→amount` → `currentHealth+=amount`（clamp maxHealth）→ 脱离 bloodied 状态时 relics.onNotBloodied → HealEffect。不走 damage() 的任何钩子。

**R16 Thorns 反伤形态与时机（汇总）** — 出处 `ThornsPower#onAttacked`（pw.txt 1356-1394）。置信度：**高**
门控：type∈{THORNS,HP_LOSS} 或 owner==null 或 owner==self → 不触发。触发：`flash(); addToTop(new DamageAction(info.owner, new DamageInfo(this.owner, amount, THORNS), SLASH_HORIZONTAL, muteSfx=true)); return damageAmount不变`。位置=R03⑪/R04⑪（扣血前、lastDamageTaken 前）。`addToTop` ⇒ 反伤排到当前动作之后立刻执行的队首；THORNS 类型保证对方身上的 Thorns 不再连锁（自环防护）且不吃双方乘区、但可被其格挡吸收。同类用法：Lightning 电球被动/激发均以 `new DamageInfo(..., THORNS)` 造伤（orbs/Lightning.class 常量池），故电球伤害不吃力量/易伤、可被格挡。

**R17 玩家受击的手牌联动与计数器** — 出处 `AbstractPlayer#damage` 偏移 749-789 + `AbstractPlayer#updateCardsOnDamage`（ap.txt 4950-5001）。置信度：**高**
amount>0 且 phase==COMBAT 时：手牌/弃牌堆/抽牌堆全部卡片 `tookDamage()`（供如 Rupture 类「受伤时」卡牌钩子的统一通知点），随后 `damagedThisCombat++`。全局计数器 `GameActionManager.damageReceivedThisTurn/damageReceivedThisCombat` 在每次扣血前无条件累加（含被格挡后溢出量？——否：累加的是经格挡后的 `amount`，因为累加点在 decrementBlock 之后）。

---

## 开放问题 / 低置信项
1. **atDamageFinalGive 的 vanilla 使用者**：标准卡池/powers 中未找到实现 `atDamageFinalGive` 的类（IntangiblePower 只实现 finalReceive）；该层存在但可能仅有 mod/特殊内容使用。未做全 jar 扫描。置信度：**中**。
2. **PenNibPower 的层级归属**：本版字节码为 `atDamageGive`(×2) 而旧 mod 文档常写作 final 层；其自身移除走 `onUseCard` → `addToBot(RemoveSpecificPowerAction)`，意味着消耗笔尖的是「本次出牌」（本次伤害已按旧快照计算完毕），下一次攻击不再翻倍。跨版本行为差异需在移植仲裁时单独确认。置信度：**高**（本版行为）/ **低**（历史版本对比）。
3. **WristBlade 条件口径**：字节码含 `freeToPlayOnce || cost==-1`（X费）分支，宽于 wiki 文本"cost 0"。以字节码为准；wiki 校验本次网络失败未能完成。置信度：**高**（行为）/ wiki 佐证缺失。
4. **THORNS 可被格挡**与部分社区攻略矛盾；未找到官方文档佐证（wiki 403）。字节码明确：`decrementBlock` 仅排除 HP_LOSS。置信度：**高**。
5. **DamageInfo.applyPowers 的完整 caller 清单**：已证 `AbstractMonster#applyPowers`；其余（如某些 unique action/orb 变体）未穷举，若移植涉及非卡牌伤害源建议按 R07 语义统一处理。置信度：**中**。
6. **Intangible 双实现差异**：怪物侧在方法入口直接改 `info.output=1`（且怪物版 `output>0` 即生效，玩家版要求 `>1`），power 版 `atDamageFinalReceive` 要求 `>1` 才压到 1——三处阈值/时机微差，0 伤与 1 伤边界行为建议移植时逐一测试。置信度：**高**（字节码事实），整合行为待运行时验证。
