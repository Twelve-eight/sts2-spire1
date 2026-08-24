# 状态（Power）施加 / 叠加 / 移除 / 衰减机制 — StS1 战斗时序知识库第二卷

## 本卷范围
以 `common/ApplyPowerAction` 与 `powers/AbstractPower.stackPower` 为中心，覆盖：施加动作完整逐帧逻辑、Artifact 预检分支、同名合并 vs 新建实例、onApplyPower/onInitialApplication 钩子、移除语义（RemoveSpecificPowerAction / ReducePowerAction / RemoveDebuffsAction）、持续回合递减时机（本卷最关键仲裁点）、容器边界与遗留 utility 动作。
**不含**：力量/易伤对伤害数值的结算入口（归 damage-pipeline.md）；action 队列调度本身（归 action-manager.md / turn-phase.md）。

## 图例
- 出处格式：`类名#方法`，必要时附 javap 字节码行摘录（offset 为方法内字节码偏移）。
- 置信度：**高** = javap 字节码直接可证；**中** = 字节码+调用链推断（注明推断环节）；**低** = 仅 wiki 或间接证据。
- 版本：desktop-1.0.jar v2.x（含观者 Watcher）。反编译复核命令见 mech-context 工具链。

---

## 一、ApplyPowerAction 完整流程

### 1.1 构造期（入队前，update() 尚未运行）

构造链：全部重载最终进入 `(target, source, power, stackAmount, fast, attackEffect)` 六参构造 `ApplyPowerAction#<init>`：

- 时长：`Settings.FAST_MODE → 0.1f`；否则 `fast==true → ACTION_DUR_FASTER`；否则 `ACTION_DUR_FAST`。`duration = startingDuration`（update 的首帧 gate 用它判定）。置信度高。
- `setValues(target, source, stackAmount)` → 动作的 `amount` 字段 = **stackAmount 参数**。这个 amount 是后续合并时真正使用的值。置信度高。
- 特例 A（Snake Skull 蛇骨）：`AbstractDungeon.player.hasRelic("Snake Skull") && source.isPlayer && target!=source && power.ID=="Poison"` → relic.flash() 且 **`powerToApply.amount += 1; this.amount += 1`**（字节码 offset 62–132）。发生在构造期。置信度高。
- 特例 B（Corruption）：`power.ID=="Corruption"` → 手牌/抽牌堆/弃牌堆/消耗堆所有 `CardType.SKILL` 卡 `modifyCostForCombat(-9)`（offset 135–375）。置信度高。
- 若 `getMonsters().areMonstersBasicallyDead()` → `duration=0, startingDuration=0, isDone=true`（offset 388–409），入队后立即完成。置信度高。
- 重载语义：3 参版 `(t,s,power)` 传 `stackAmount = power.amount`；4 参版传 `fast=false`；5 参 `(…,int,AttackEffect)` 同样 `fast=false`（offset 见各委托构造）。置信度高。

### 1.2 update() 逐帧顺序（首帧 gate：`duration == startingDuration` 才执行主体，之后各帧仅 tickDuration）

按字节码偏移顺序（`ApplyPowerAction#update`，全文已核对）：

| # | 偏移 | 行为 |
|---|------|------|
| 1 | 0–22 | `target==null ∥ target.isDeadOrEscaped()` → `isDone=true; return`。**什么都不发生**：不触发钩子、不消耗 Artifact、不产生飘字。 |
| 2 | 35–67 | NoDraw 特判：`powerToApply instanceof NoDrawPower && target.hasPower(NoDraw.ID)` → `isDone; return`（NoDraw 永不叠加）。 |
| 3 | 68–124 | **onApplyPower 钩子**：`source != null` 时遍历 **source.powers**（注意：是来源者的 power 列表，不是目标、不是 relics），逐个调 `p.onApplyPower(powerToApply, target, source)`。这是施加前唯一允许第三方改写待施加 power 的入口。 |
| 4 | 124–205 | Champion Belt（冠军腰带）：玩家拥有 && source 是玩家 && target≠source && `ID=="Vulnerable"` && 目标无 Artifact → `belt.onTrigger(target)`（其内部给目标叠 Weak）。 |
| 5 | 205–235 | 二次死亡检查：`target instanceof AbstractMonster && isDeadOrEscaped()` → `duration=0; isDone; return`（等待动画期间目标死亡的补偿路径）。 |
| 6 | 236–321 | Ginger（姜）：玩家有 Ginger && target 是玩家 && `ID=="Weakened"`（即 Weak）→ 飘字 + tick + **return，完全不施加**。 |
| 7 | 321–406 | Turnip（芜菁）：同上，`ID=="Frail"` → 不施加。 |
| 8 | 406–501 | **Artifact 分支**（详见 §二）。未被挡则继续。 |
| 9 | 502–539 | 攻击闪光特效 `FlashAtkImgEffect` 入 effectList。 |
| 10 | 540–1135 | **合并循环**：遍历 `target.powers`，若 `p.ID == powerToApply.ID` **且 `p.ID != "Night Terror"`**：`p.stackPower(this.amount)`（用动作的 int 参数！不是 powerToApply.amount）→ `p.flash()` → 按 amount 符号与 type 出 buff/debuff 飘字（Strength/Dexterity 负值强制按 debuff 文本）→ `p.updateDescription()` → `found=true` → `AbstractDungeon.onModifyPower()`。传入的 `powerToApply` 实例被**丢弃**。 |
| 11 | 1138–1160 | 若 `powerToApply.type == DEBUFF` → `target.useFastShakeAnimation(0.5f)`。 |
| 12 | 1160–1481 | **新建路径**（循环未发现同 ID）：`target.powers.add(powerToApply)` → `Collections.sort(powers)` → **`powerToApply.onInitialApplication()`** → flash → 飘字（新建时若 `this.amount < 0` 且 ID∈{Strength,Dexterity,Focus} 显示 debuff 文本——注意飘字判断用的是动作 amount 而实例 amount 来自其构造器）→ `AbstractDungeon.onModifyPower()`。 |
| 13 | 1484–1552 | target 是玩家且 BUFF 数 ≥10 → 解锁 POWERFUL 成就。 |
| 14 | 1555 | `tickDuration()`。 |

### 1.3 amount≤0 与 clamp

- ApplyPowerAction **没有任何 amount≤0 拦截**：负值/0 照常走合并或新建（新建一个 amount≤0 的实例会真实挂上）。行为完全由各 power 自己的 `stackPower`/衰减钩子决定。置信度高（全方法无该分支）。
- **999 上限不在 ApplyPowerAction，也不在 AbstractPower 默认实现**；只存在于具体 power 的 `stackPower` override 内：`StrengthPower` ±999、`DexterityPower` ±999、`FocusPower` +999/−999、`GainStrengthPower` ±999、`EnergizedPower`/`EnergizedBluePower`/`CollectPower` 仅 +999 上限、`PlatedArmorPower`/`watcher.LikeWaterPower` 仅 +999 上限（各文件 `stackPower` 内 `sipush 999` 分支，方向已逐一核对）。置信度高。

---

## 二、仲裁点 A：Artifact 分支结论

字节码（`ApplyPowerAction#update` offset 406–501）：

```
406: aload_0 … target.hasPower("Artifact")
418: … powerToApply.type
425: if_acmpne 502      ; type != DEBUFF → 跳过 Artifact 分支
…
489: getPower("Artifact").flashWithoutSound()
498: getPower("Artifact").onSpecificTrigger()
501: return             ; 本次施加到此为止
```

确定性结论：
1. 只有 `PowerType.DEBUFF` 会撞 Artifact；**BUFF 类型完全无视 Artifact**（哪怕语义上有害）。置信度高。
2. 被挡时 debuff **完全不施加**：不进合并循环、不走新建、不触发 onApplyPower 之后的一切应用逻辑（onApplyPower 钩子在分支 3 已跑过，但那只是通知，不等于施加）。传入实例直接丢弃。置信度高。
3. 单层消耗由被挡方执行：`ArtifactPower#onSpecificTrigger` → `if (amount > 0) addToTop(new ReducePowerAction(owner, owner, "Artifact", 1)) else addToTop(new RemoveSpecificPowerAction(owner, owner, "Artifact"))`。即实际扣层是**排队的 ReducePowerAction**（addToTop，下一拍执行），而非当场扣。置信度高。
4. **多层连续到达逐个消耗**：每个被 Artifact 挡住的 ApplyPowerAction 各自触发一次 onSpecificTrigger → 各排队一个 ReducePowerAction(1)，FIFO 逐个执行、逐层扣除。剩 1 层时：第一个 debuff 使 ReducePowerAction 走"reduce≥amount → addToTop(RemoveSpecificPowerAction)"路径（`ReducePowerAction#update` offset 81–98），Artifact 移除完成后，第二个 debuff 到达时 `hasPower("Artifact")` 已为 false → **正常施加**。置信度高。

---

## 三、stackPower 家族语义

### 3.1 默认实现
`AbstractPower#stackPower(int)`：
```
0: getfield amount; iconst_m1; if_icmpne 39   ; amount == -1 → 视为不可叠加
33: logger.info(name + " does not stack"); return
39: fontScale = 8.0f
46: amount += stackAmount                      ; 无下限、无上限
```
- `amount == -1` 是"不可叠加"哨兵（如 Thorns 初始为 -1 的用法，见 `ThornsPower#stackPower` 复刻同款守卫）。置信度高。
- 默认叠加**可正可负、越界不裁剪**；`canGoNegative` 字段存在但默认 stackPower 不读它（渲染层字段）。置信度高。
- **不存在 `stackPower(int, boolean)` 变体**：`AbstractPower` 全部签名清单中仅有 `stackPower(int)`。置信度高。
- 默认 `AbstractPower#reducePower(int)`：`amount -= n` 后若 ≤0 则**钳到 0 但不移除**（不移出列表、不触发 onRemove）。移除必须显式走 RemoveSpecificPowerAction 或 ReducePowerAction。置信度高。

### 3.2 关键 override 分类（全 powers/** 共 35 个 stackPower override 已扫描）

| 类 | stackPower 行为 | 出处 | 置信度 |
|---|---|---|---|
| StrengthPower | `fontScale=8; amount+=n; amount==0 → addToTop(RemoveSpecificPowerAction "Strength")`; 玩家≥50 → JAXXED; **±999 双向钳制** | StrengthPower#stackPower offset 60–108 | 高 |
| DexterityPower | 同上模式（remove@0、±999 钳制、无成就阈值） | DexterityPower#stackPower offset 60–98 | 高 |
| FocusPower | remove@0、玩家≥25 → FOCUSED、±999 钳制 | FocusPower#stackPower offset 46–86 | 高 |
| VulnerablePower / WeakPower / FrailPower | **无 stackPower override** → 走默认 `+=`（无 remove@0、无钳制）；时长递减靠 atEndOfRound（§五） | 三文件均无该方法 | 高 |
| PoisonPower | `super.stackPower(n)`（纯累加，不 remove@0）；>98 且静女 → CATALYST 成就 | PoisonPower#stackPower offset 107–122 | 高 |
| RegenPower / ThornsPower 等 | 纯累加（Thorns 带 -1 哨兵复刻） | 各自 stackPower | 高 |
| Energized(+Blue)/Collect | 累加 + 仅 +999 上限 | 各自 stackPower | 高 |
| PlatedArmor/LikeWater/GainStrength | 累加 + 999 相关钳制（GainStrength 双向） | 各自 stackPower | 高 |
| MantraPower | remove@0（Mantra 归零即移除并唤神） | MantraPower#stackPower | 高 |
| IntangiblePower | **无 override**（走默认累加）；特例在衰减钩子，见 §五 R17 | 文件无该方法 | 高 |

**负值下限结论**：不存在 −99 这类通用下限。STR/DEX/FOC 允许任意负值直至 −999 钳制；其余 power 默认无任何下限。任务假设中的"-99"在字节码中无对应物。置信度高。

---

## 四、仲裁点 B：合并 vs 新建结论

`ApplyPowerAction#update` 合并循环 offset 572–606 与新建段 offset 1164–1193 给出唯一确定答案：

1. **判据**：`target.getPower(ID) != null` 等价的线性扫描（合并循环逐个比对 ID）。命中且非 "Night Terror" → 合并；未命中 → 新建。**"Night Terror" 是唯一例外**：即使同 ID 已存在也跳过合并、追加新实例（多实例共存）。置信度高。
2. **合并时**：`existing.stackPower(this.amount)` —— 只使用构造 ApplyPowerAction 时传入的 int 参数；`powerToApply` 对象整个丢弃，它的 amount、justApplied 等字段全部无效。陷阱：`new VulnerablePower(m, 5, false)` 配 4 参构造 `ApplyPowerAction(t,s,power,2)`，若目标已有 Vuln 则只 +2；若没有则新实例 amount=**5**（构造器的值），参数 2 被无视。两路不对称是移植高频错误点。置信度高。
3. **新建时序**（严格先后）：`powers.add(instance)` → `Collections.sort(powers)`（按 priority 排序，`AbstractPower#compareTo`）→ `instance.onInitialApplication()` → flash。即 **onInitialApplication 在实例已进列表之后触发**，钩子内可以查到自己。合并路径**不会**调 onInitialApplication。置信度高。
4. 合并路径每次都会 `AbstractDungeon.onModifyPower()`（刷新手牌 applyPowers / Focus 刷 orb 描述 / 全怪物 applyPowers，纯 UI/数值刷新，不改 amount，`AbstractDungeon#onModifyPower` offset 7377–7420）。置信度高。

---

## 五、仲裁点 C：持续回合（debuff duration）递减时机

### 5.1 调用图（全部 javap 直证）

```
玩家点结束回合:
  EndTurnButton#update → player.endTurnQueued=true
  AbstractPlayer#update(offset~2393) → 手牌/队列就绪时 endTurnQueued=false, isEndingTurn=true
  AbstractRoom#update(offset 478)    → player.isEndingTurn → this.endTurn()
      ├─ player.applyEndOfTurnTriggers()          ← 玩家 powers 的 atEndOfTurn(true)【同步直调】
      ├─ addToBottom(ClearCardQueueAction)
      ├─ addToBottom(DiscardAtEndOfTurnAction)
      └─ addToBottom(匿名 AbstractRoom$1 → EndTurnAction → GameActionManager.endTurn())
           GameActionManager#endTurn: resetControllerValues; turnHasEnded=true; playerHpLastTurn=hp

怪物行动完（getNextAction monsterQueue 分支, offset 1941–1945）:
  m.takeTurn(); m.applyTurnPowers();     ← applyTurnPowers 只调 duringTurn() 钩子，不递减时长
                                          （AbstractCreature#applyTurnPowers offset 1728–1744）

回合边界（getNextAction turnHasEnded 分支, offset 1984–2228，第一步）:
  MonsterGroup.applyEndOfTurnPowers():
    循环① 每个存活怪 m.applyEndOfTurnTriggers()      ← atEndOfTurnPreEndTurnCards(false)+atEndOfTurn(false)
                                                      （AbstractCreature#applyEndOfTurnTriggers offset 1764–1788）
    循环② player.powers 每个 p.atEndOfRound()          ← 【玩家侧时长递减发生地】
    循环③ 每个存活怪 powers 每个 p.atEndOfRound()      ← 【怪物侧时长递减发生地】
  然后：清 cardsPlayedThisTurn → startOfTurnRelics → PreDrawCards → cards →
        applyStartOfTurnPowers(atStartOfTurn) → orbs → turn++ → 掉格挡 → DrawCardAction → postDraw*
```

关键出处：`MonsterGroup#applyEndOfTurnPowers`（offset 896–964，三循环）、`GameActionManager#getNextAction`（offset 2014 调 applyEndOfTurnPowers）、`AbstractCreature#applyTurnPowers/#applyEndOfTurnTriggers`、`AbstractRoom#endTurn`（offset 922–996）。

### 5.2 递减本体

`VulnerablePower#atEndOfRound`（Weak/Frail 同构）：
```
0: getfield justApplied; ifeq 13
7: justApplied=false; return            ; 首轮跳过
13: getfield amount; ifne 44
20..41: addToBot(RemoveSpecificPowerAction(owner,owner,"Vulnerable"))   ; amount==0 → 移除
44..63: addToBot(ReducePowerAction(owner,owner,"Vulnerable",1))         ; 否则排一个 -1
```
- 递减不是当场 `-‑amount`，而是**排队 ReducePowerAction(1)**（addToBot）；真正扣数发生在该 action 的首帧，且当 reduce≥amount 时转为 RemoveSpecificPowerAction（触发 onRemove）。置信度高。
- `atEndOfRound` 在整场只有 MonsterGroup.applyEndOfTurnPowers 一个调用者（全语料 grep 证实），即**每回合恰一次、发生在"怪物全部行动完之后、新回合开始序列的第一步"**。玩家与怪物在同一函数内先后被处理（先玩家②后怪物③）。置信度高。

### 5.3 justApplied（施放当回合不减的证据）

`VulnerablePower#<init>(owner, amount, boolean)`：
```
4: justApplied=false                       ; 先置 false
42: actionManager.turnHasEnded; ifeq 60    ; 仅当处于"回合已结束窗口"
51: iload_3; ifeq 60                       ; 且布尔参数为 true
55: justApplied=true
67: isTurnBased=true
```
- 双条件缺一不可：**只有在 turnHasEnded==true 期间（敌方回合/回合尾）构造、且调用方明确传 true**，才跳过紧随而来的本轮边界递减。
- 调用方普查：monsters/** 中对玩家的 Weak/Frail/Vulnerable 构造共 46 处，第三参**全部传 true**（21/13/12，无一例外）；卡牌侧（BeamCell、Trip 等抽查）一律传 false。置信度高。
- 因此：**敌方在其回合给你上的 debuff 当轮不减**（justApplied 吃掉紧邻的那次 atEndOfRound）；**你在自己回合给怪上的 debuff 当轮结束时照减一次**（turnHasEnded=false → justApplied 保持 false）。
- Intangible 特例：`IntangiblePower#<init` 无条件 `justApplied=true`（offset 43–45）；且其衰减钩子是 `atEndOfTurn(boolean)`（玩家=结束回合同步触发，怪物=回合边界循环①），不是 atEndOfRound。置信度高。
- 同款私有 justApplied 副本还见于 DoubleDamage/AttackBurn/SkillBurn/DrawReduction/NoBlock（各自独立字段，setter 条件未逐一展开）。置信度中。

### 5.4 「施放当回合立即生效且不减」的精确表述

- 效果生效与否只取决于伤害/格挡管线读取时刻该 power 是否在场（归 damage-pipeline.md），施加本身在 ApplyPowerAction 首帧即时完成——**当轮立刻参与结算**。
- 是否"当轮递减"由 §5.3 规则决定，而非统一规则：己方回合上的 debuff 当轮边界就减；敌方回合上的 debuff 凭 justApplied 跳过首轮。两类合起来才是完整的 StS1 手感（Vuln2 打在自己回合 → 覆盖你接下来整整 2 个进攻轮）。

---

## 六、移除语义

### 6.1 RemoveSpecificPowerAction（唯一正规移除通道）
`RemoveSpecificPowerAction#update`（首帧 gate `duration==0.1f`）：
- `target.isDeadOrEscaped()` → 直接 done（**不触发 onRemove**）。置信度高。
- 定位：按 ID 字符串 `target.getPower(id)`，或按实例 `powers.contains(instance)`。
- 顺序：`PowerExpireTextEffect` 特效 → **`p.onRemove()` 先调** → `powers.remove(p)` → `AbstractDungeon.onModifyPower()` → 刷新全部 orb 描述。置信度高。

### 6.2 ReducePowerAction
`ReducePowerAction#update`（首帧）：
- 定位同上（ID 或实例）。
- `action.amount < p.amount` → `p.reducePower(actionAmount)` + updateDescription + onModifyPower（仅减少，不移除，**不触发 onRemove**）。
- `action.amount >= p.amount` → `addToTop(RemoveSpecificPowerAction(target, source, pInstance))`（转完整移除路径，onRemove 会触发）。即"减到≤0 自动移除"的精确形式是 **reduce 量 ≥ 当前量才转移除**；恰好减到 0 也走移除。置信度高。

### 6.3 RemoveDebuffsAction（净化范围）
`actions.unique.RemoveDebuffsAction#update`：遍历 `c.powers`，凡 `type == DEBUFF` → `addToTop(RemoveSpecificPowerAction(c, c, p.ID))`，随后自身立即 isDone。即**清单 = 目标身上全部 DEBUFF 类型 power**，逐个走完整移除路径（onRemove 触发）；BUFF 一律保留。瞬时排队型（无 duration gate）。使用者：relic `OrangePellets#onTrigger`（攻击+技能+能力同回合打出）、怪物 TimeEater、Champ。置信度高。

### 6.4 遗留/边缘 utility
- `RemoveAllPowersAction(c, debuffsOnly)`：debuffsOnly=false 时连 BUFF 全移，同样逐个转 RemoveSpecificPowerAction。**在本次扫描范围内（powers/monsters/actions(common|unique|utility)/cards/relics/potions 全量）无任何调用者**，属遗留死代码。置信度高（限于扫描范围）。
- `ReApplyPowersAction(card, monster)`：名不副实——update 只是 `card.calculateCardDamage(m)`（单卡伤害重算触发器），与 power 重挂无关。同样在扫描范围内无调用者。主代理任务描述中的"变身重挂"用途在本 jar 中**不成立**。置信度高（限于扫描范围）。

---

## 七、容器边界（同名不同实例）

- `powers` 是 `AbstractCreature` 的实例字段（`ArrayList<AbstractPower>`）；玩家与每只怪物各有独立列表。`AbstractCreature#getPower/hasPower` 都是**只扫描本容器**的线性查找（offset 2595–2641）。跨容器从不自动同步。置信度高。
- 同一 power ID 可同时存在于玩家和多个怪物身上，互为独立实例（各自的 amount、justApplied、isTurnBased 独立演化）。
- `addPower`（`AbstractCreature#addPower` offset 1644 起）自带一套同 ID 合并逻辑（遍历命中 → stackPower(power.amount) + updateDescription；未命中 → add + 玩家侧 POWERFUL 成就计数）。它是绕过 ApplyPowerAction 的直通口（战斗初始化等场景使用），**没有** Artifact/onApplyPower/onInitialApplication 流程——移植时不要把它与 ApplyPowerAction 路径混淆。置信度高。

---

## 八、编号规则汇总

- **R01** 施加目标的生死前置：`target==null || target.isDeadOrEscaped()` 时 ApplyPowerAction 完全 no-op（不触发任何钩子、不消耗 Artifact、不飘字）；等待动画期间目标死亡还有第二次拦截（monster instanceof 分支）。出处 `ApplyPowerAction#update` offset 0–22, 205–235。置信度高。
- **R02** onApplyPower 钩子只遍历 **source.powers**，目标方与 relics 不收到该通知；原版唯一实现者 SadisticPower（效果是追伤，不改 amount）。出处 `ApplyPowerAction#update` offset 68–124；`SadisticPower#onApplyPower`。置信度高。
- **R03** Artifact 仅拦 `type==DEBUFF`；拦截时 debuff 完全不施加，消耗动作由 `ArtifactPower#onSpecificTrigger` 排队（ReducePowerAction(1) addToTop / 归零转 RemoveSpecificPowerAction）。出处 `ApplyPowerAction#update` offset 406–501；`ArtifactPower#onSpecificTrigger` offset 0–53。置信度高。
- **R04** Artifact 多层消耗严格逐次：N 层 Artifact 连挡 N 个 debuff；第 N+1 个 debuff 正常施加。两个 ApplyPowerAction 相继到达时各自独立走一遍 R03 流程。出处同 R03 + `ReducePowerAction#update` offset 52–98。置信度高。
- **R05** Ginger/Turnip 在 Artifact 检查**之前**硬挡 Weakened/Frail（对玩家），完全不施加且**不消耗 Artifact 层**。出处 `ApplyPowerAction#update` offset 236–406。置信度高。
- **R06** 同 ID 合并用动作的 `amount` 参数调 `existing.stackPower(int)`，传入实例丢弃；"Night Terror" 永远新建多实例。出处 `ApplyPowerAction#update` offset 572–611。置信度高。
- **R07** 新建时序固定：add → sort(priority) → `onInitialApplication()` → flash；合并路径绝不调 onInitialApplication。出处 `ApplyPowerAction#update` offset 1164–1200。置信度高。
- **R08** 无通用 amount≤0 拦截、无通用 999 clamp；999 钳制只在 STR/DEX/FOC/GainStrength（±999）与 Energized(+Blue)/Collect/PlatedArmor/LikeWater（+999）的 stackPower 内。出处各 `*Power#stackPower`；`ApplyPowerAction#update` 全文无 sipush 999。置信度高。
- **R09** 默认 `stackPower`：amount==-1 哨兵=不可叠加；否则无条件累加（可为负）；默认 `reducePower` 下限钳 0 不移除。不存在 `stackPower(int,boolean)`。出处 `AbstractPower#stackPower/#reducePower` offset 326–378 及签名清单。置信度高。
- **R10** STR/DEX/FOC 允许负值至 −999 钳制，==0 时 addToTop 自我移除；Vulnerable/Weak/Frail 无 override，时长语义完全交给 atEndOfRound+ReducePowerAction。出处 §3.2 表。置信度高。
- **R11** 移除顺序：`onRemove()` 先于列表移除；目标死亡时 RemoveSpecificPowerAction no-op 且不触发 onRemove。出处 `RemoveSpecificPowerAction#update` offset 10–65, 146–158。置信度高。
- **R12** ReducePowerAction 仅当 reduce≥当前 amount 才转 RemoveSpecificPowerAction；否则只减不触钩。出处 `ReducePowerAction#update` offset 52–98。置信度高。
- **R13** 回合边界递减唯一发生地：getNextAction turnHasEnded 分支第一步 `MonsterGroup.applyEndOfTurnPowers` 的循环②（player.powers.atEndOfRound）与循环③（每怪 powers.atEndOfRound）；递减体 = atEndOfRound 内 addToBot(ReducePowerAction(1))。怪物行动后的 `applyTurnPowers` 只发 duringTurn 钩子、不减时长。出处 `GameActionManager#getNextAction` offset 1941–1945, 2014；`MonsterGroup#applyEndOfTurnPowers` offset 896–964；`VulnerablePower#atEndOfRound`。置信度高。
- **R14** 玩家侧 `atEndOfTurn(true)` 在按下结束回合同步触发（endTurnQueued→isEndingTurn→`AbstractRoom.endTurn()`），先于弃牌阶段与怪物行动；怪物侧 `atEndOfTurn(false)` 在回合边界循环①。两者与 atEndOfRound 是不同钩子、不同时机。出处 `AbstractRoom#endTurn` offset 922–996；`AbstractCreature#applyEndOfTurnTriggers` offset 1764–1788。置信度高。
- **R15** justApplied 双条件：`turnHasEnded==true` 期间构造 && 构造布尔参为 true → 跳过紧邻的首轮 atEndOfRound 递减。怪物→玩家施加减益一律传 true（46/46 例），卡牌→任何目标传 false。出处 `VulnerablePower#<init>` offset 17–71 + monsters/cards 语料普查。置信度高。
- **R16** 敌方回合给你的 debuff 当轮不减（R15）；你回合给怪的 debuff 当轮边界照减一次。Vulnerable 2 于自己回合施加 → 有效覆盖自己接下来 2 个进攻轮。出处 R13+R15 推理链（每步均有字节码）。置信度高。
- **R17** Intangible 特例：构造即 justApplied=true；衰减走 atEndOfTurn 而非 atEndOfRound。出处 `IntangiblePower#<init>` offset 43–45、`IntangiblePower#atEndOfTurn` offset 71–107。置信度高。
- **R18** Power 容器按生物隔离；`addPower` 直通口无 Artifact/onApplyPower/onInitialApplication 流程，勿与 ApplyPowerAction 混同。出处 `AbstractCreature#addPower/getPower/hasPower`。置信度高。
- **R19** RemoveDebuffsAction 清单 = 全部 DEBUFF 类型、逐个走 RemoveSpecificPowerAction（onRemove 触发）；OrangePellets/TimeEater/Champ 为现役使用者。出处 `RemoveDebuffsAction#update` offset 17–50；`OrangePellets#onTrigger`。置信度高。
- **R20** ReApplyPowersAction 实为单卡 calculateCardDamage 触发器；RemoveAllPowersAction(debuffsOnly) 为全量清除变体；二者在扫描范围内无调用者（遗留代码）。出处 `ReApplyPowersAction#update`；`RemoveAllPowersAction#update`；全语料 grep。置信度高（限扫描范围）。
- **R21** Snake Skull 的 Poison +1 发生在构造期：若随后被 Artifact 挡下，增量随实例一起丢弃、无副作用；若正常施加则并入（合并路径体现为 stackPower(amount+1)）。出处 `ApplyPowerAction#<init>` offset 62–132。置信度高。

---

## 开放问题 / 低置信项

1. **ReApplyPowersAction / RemoveAllPowersAction 的历史调用者**：本卷扫描覆盖 powers、monsters、actions/common|unique|utility、cards、relics、potions 全量；events、stances、UI、helpers 未扫。若需穷尽"是否彻底死代码"，需补扫这些包。置信度：结论限于扫描范围（已在 R20 标注）。
2. **DoubleDamage/AttackBurn/SkillBurn/DrawReduction/NoBlock 的私有 justApplied**：字段与"跳过首次递减"模式确认存在，但其 setter 的双条件（是否也带 turnHasEnded 门）未逐一反汇编。置信度中。
3. **canGoNegative 字段的消费方**：存在于 AbstractPower 但默认 stackPower 不读；推测为渲染层（负数显示样式）依据，未追踪 UI 代码。置信度低。
4. **Collections.sort 的稳定性对同 priority power 显示顺序的影响**：排序键为 priority（`AbstractPower#compareTo`），同 priority 的相对顺序属显示层细节，不影响本卷任何裁决。未深挖。
5. 怪物 HP/伤害的 999 相关上限属于 damage-pipeline.md 范围，本卷不裁决。
