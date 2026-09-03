# 防御性 power 仲裁（Defense Powers Arbitration）— StS1 战斗语义知识库

## 本卷范围
伤害预防/削减机制的挂点层级与"逐源/逐回合"消费语义：无实体双实现（玩家/怪物）+ damage() 入口钳制、Buffer 逐源消费、Invincible 回合预算、钨杆平减、格挡保留三件套（Barricade/Blur/Calipers）。每条都回答"多段攻击吃几层、HP_LOSS/THORNS 吃不吃、0 伤/1 伤边界、与格挡的先后"。
交叉引用不重复展开：Thorns 反伤族 → `damage-pipeline.md` R16；Artifact 拦截 → `status-stacking.md`；免死拦截链 → `death-arbitration.md` §1。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 挂点层级总图（一条伤害从计算到落地的防御干预点）

**R01 防御干预点共五层，时刻不同** — 出处 `AbstractPlayer#damage` offset 0-45（入口）、offset 190-229（onAttackedToChangeDamage powers 段）、`AbstractCard#calculateCardDamage`（damage-pipeline.md R06）。置信度：**高**
```
① 计算链 atDamageFinalReceive —— 仅卡牌/applyPowers 伤害（LoseHPAction 等直构 DamageInfo 不经过）
② damage() 入口字段钳制（玩家：local amount>1→1，offset 22-38；怪物：info.output>0→1，offset 0-19）
③ decrementBlock 格挡吸收（HP_LOSS 直通）                       [39-45 / 怪物 70-76]
④ 守方 powers.onAttackedToChangeDamage（Buffer/Invincible 挂点） [怪物侧 before 玩家侧，见 triggers.md R13]
⑤ 拦截链 relics.onLoseHpLast（钨杆 -1）                          [死亡闸之前]
```
关键序点：**④ 的钩子吃的是穿透格挡后的余量**（③ 在 ④ 前）——30 格挡吃 40 伤，Buffer 看到的入参是 10 而非 40。

---

## 2. 无实体（Intangible）三处实现

**R02 玩家版入口钳制：`>1 → 1`，无类型门控，改局部变量** — 出处 `AbstractPlayer#damage` offset 16-38（death-arbitration.md R15 全引）。置信度：**高**
1 点伤害（amount==1）**不**被钳（`>1` 严格大于）⇒ 无实体期间每点 1 伤照常掉血。0 伤无意义。NORMAL/THORNS/HP_LOSS 一视同仁。

**R03 怪物版入口钳制：`>0 → 1`，直接改写 `info.output`** — 出处 `AbstractMonster#damage` offset 0-19。置信度：**高**
```
if (info.output > 0 && hasPower("IntangiblePlayer")) info.output = 1;
```
与玩家版两处微差：阈值 `>0`（1 伤也被压到 1，等价但实现不同）、写回 DamageInfo 字段（后读 info.output 的代码会看到 1）。

**R04 计算链钳制：双版各实现 `atDamageFinalReceive`（`>1 → 1`），仅服务于 calculateCardDamage/DamageInfo.applyPowers** — 出处 `IntangiblePlayerPower#atDamageFinalReceive` 与 `IntangiblePower#atDamageFinalReceive`（两者字节码同构）。置信度：**高**
挂点在 finalReceive 层 = Weak/Vulnerable 等乘区**之后**（damage-pipeline.md R06 步骤⑥）⇒ 先乘后钳，99999 也归 1。

**R05 持续期语义：玩家版 atEndOfRound、怪物版 atEndOfTurn(boolean)+justApplied** — 出处 `IntangiblePlayerPower#atEndOfRound` offset 0-57；`IntangiblePower#atEndOfTurn(boolean)` offset 0-70。置信度：**高**
- 玩家版：新回合块（玩家 powers 段）`amount==0 → addToBot(Remove)` 否则 `addToBot(Reduce 1)`——**当回合内获得的无实体当回合就有效**，下一新回合块开始时递减（death-arbitration.md R17）。
- 怪物版：`atEndOfTurn`：`justApplied==true → 置 false 并 return`（施加当回合不递减）；否则同 Remove/Reduce 分叉。⇒ 怪物在它自己回合开始前获得的无实体会多顶一个完整回合。
- `priority` 字段（玩家 75 / 怪物 75 / Invincible 99）仅 UI 排序，不影响通知遍历（triggers.md §8 注 2）。

---

## 3. Buffer（护盾缓冲，逐源消费）

**R06 Buffer = onAttackedToChangeDamage 无类型门控归零 + addToTop 递减** — 出处 `BufferPower#onAttackedToChangeDamage` offset 0-29 + `BufferPower#stackPower` offset 0-15。置信度：**高**
```
onAttackedToChangeDamage(info, amount):
  if (amount > 0) addToTop(new ReducePowerAction(owner, owner, "Buffer", 1));
  return 0;
```
- 门条件**只有 `amount > 0`**：不区分 DamageType ⇒ NORMAL/THORNS/HP_LOSS（含渎神 99999）全归零；0 伤不消费层数。
- 归零发生在格挡吸收**之后**（R01④）⇒ 有格挡时 Buffer 消耗在溢出上。
- `addToTop(Reduce)`：消费动作插队首，当前 damage() 所在动作结束后立即执行；amount 递减到 0 时由 ReducePowerAction 移除 power（death-arbitration.md R18 同一机制）。
- 多段攻击逐击消费（每击一次 onAttackedToChangeDamage，damage-pipeline.md R09 单快照不改变逐击落地次数）。
- 与玩家无实体同时存在：入口钳制先于钩子链 ⇒ 99999 先被钳 1，再被 Buffer 归零且**消费 1 层 Buffer**（两者串联而非二选一）。

**R07 Buffer 不吃时长递减、无回合钩子** — 出处 `javap -p BufferPower` 方法面（仅 stackPower/updateDescription/onAttackedToChangeDamage）。置信度：**高**
语义 = "接下来 N 个正伤害实例"，跨回合有效，无 justApplied 机制。

---

## 4. Invincible（无敌，回合预算，心的战斗机制）

**R08 预算钳制与回收** — 出处 `InvinciblePower#onAttackedToChangeDamage` offset 0-40 + `#atStartOfTurn` offset 0-12。置信度：**高**
```
onAttackedToChangeDamage(info, amount):
  if (amount > this.amount) amount = this.amount;   // 钳到剩余预算
  this.amount -= amount; if (<0) = 0; updateDescription();
  return amount;
atStartOfTurn(): this.amount = maxAmt;   // 持有者回合开始重置
```
- 无类型门控（同 Buffer）⇒ HP_LOSS/THORNS 也吃预算；门为"钳制"而非归零 ⇒ 预算内伤害照常通过。
- 预算按**实际通过量**（钳后）扣减：一次 60 伤、预算剩 30 ⇒ 造 30 伤、预算清零，超出的 30 已被丢弃（不延迟）。
- 多段攻击共享预算，先到先得。
- 重置挂 `atStartOfTurn` = **持有者（怪物）自己回合开始**（`MonsterStartTurnAction → applyStartOfTurnPowers`，turn-phase.md R10），即玩家一整轮打出的全部伤害共享一份预算。
- 与 Buffer/无实体同挂 ④ 层时的相对顺序 = powers 列表插入序（triggers.md R15），无 priority 干预。

---

## 5. 平减与格挡保留

**R09 钨杆：onLoseHpLast 平减 1，`>0` 门** — 出处 `TungstenRod#onLoseHpLast` offset 0-13（`amount>0 → flash; return amount-1`）。置信度：**高**
挂 ⑤ 层（relics.onLoseHpLast，调用点 `AbstractPlayer#damage` offset 466，在 HP 扣减与 `<1` 死亡闸之前）⇒ 对任何类型（含 HP_LOSS）生效；1 点伤害被减成 0（掉血量 `amount>0` 才扣，0 不掉）；对渎神 99999 → 99998（death-arbitration.md R20）。

**R10 格挡保留三件套是回合块中央门控，power 本体为空壳** — 出处 `GameActionManager#getNextAction` offset 2127-2174。置信度：**高**
```
if (!hasPower("Barricade") && !hasPower("Blur")):
    if (hasRelic("Calipers")) player.loseBlock(15);
    else                      player.loseBlock();      // 清零
```
玩家格挡掉落只发生在这里（新回合块、start-of-turn 钩子梯之后）；怪物格挡在 `MonsterGroup#applyPreTurnLogic`（turn-phase.md R10/R11）。Barricade/Blur power 类本体不含 retain 逻辑（`javap -p` 无受击相关方法）——**改这套语义必须改中央门控，不是改 power**。Blur 的 N 层数值语义在 power 自身 atStartOfTurn 递减（数量在 power.amount，中央门只查存在性 ⇒ Blur 层数≥1 即全保，逐层消费在 power 自己的钩子——本卷实测门控处仅 hasPower 存在性检查；BlurPower 方法面未逐字节取证，开放问题 3）。

---

## 6. 仲裁案例表（ vanilla 数值直推，全部可由 R01-R10 机械复算）

| 场景 | 结局 | 依据 |
|---|---|---|
| 无实体 1 层 + 敌 10 连击 ×3 伤 | 每击 3→1，共掉 10 血；回合结束层归零 | R02/R05/R07 对照 |
| 无实体 ≥2 层 + 渎神死 | 失血 1 存活 | death-arbitration.md R19 |
| 无实体 1 层 + 渎神死 | 死（层先到期） | death-arbitration.md R17-R18 |
| Buffer 1 层 + 敌 50 伤（自己 30 格挡） | 20 溢出→Buffer 归 0，消耗 1 层，0 掉血 | R06 + R01④ |
| Buffer 1 层 + 敌 0 伤攻击 | 层数不消耗（门 `>0`） | R06 |
| Buffer 1 层 + 渎神死 | 救（99999→0，耗 1 层） | R06 + death-arbitration.md R20 |
| 心 Invincible(30) + 敌 60 连击 + 你 20 格挡 | 首击 40→20 过、预算 10；次击 20→10 过、预算 0；后续全额 | R08（注意预算吃的是穿透格挡后余量） |
| 钨杆 + 1 点 HP_LOSS | 0 掉血，flash | R09 |
| Calipers + 无 Barricade/Blur | 掉格挡保留 15 | R10 |
| Thorns 反伤 + 敌有格挡 | 反伤被敌格挡吸收（THORNS 可格挡） | damage-pipeline.md R13/R16 |

---

## 7. 开放问题 / 低置信项

1. **BlurPower 逐层消费细节**（amount 递减点与 selfRetain 边界）未逐字节取证；中央门控侧已证只查存在性。置信度：**中**。
2. **怪物版无实体的 `info.output` 写回副作用**：写回后同一 DamageInfo 若再被读取（如 lastDamageTaken、Thorns 门控）读到 1——静态推断一致，运行时无反例。置信度：**中**。
3. **④ 层多 power 顺序**对"Buffer 在 Invincible 前/后"的数值差异（插入序决定谁先钳）——机制已证（R15 triggers），具体组合案例未穷举。置信度：**高**（规则）/ 未枚举（案例）。
4. IntangiblePlayerPower 的 `atDamageFinalReceive` 与入口钳制**双保险**在卡牌伤害路径上等效性（先钳后钳都是 1）为语义推论，无独立行为差异可测。置信度：**中**。
