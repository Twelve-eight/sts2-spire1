# 宝珠管线（Orb Pipeline）— StS1 战斗语义知识库

## 本卷范围
缺陷家宝珠系统的完整结算语义：数据模型（orbs 列表 + EmptyOrbSlot 占位）、槽位增减、通道（channel，含满槽逐出三连入队）、四种珠的被动/激发时点、Focus 作用范围与刷新时机（**含"焦点变化是否影响已有珠"的字节码裁决**）、回合触发器与 Cables 双触发。回答"通道/激发/被动/焦点谁先谁后"类问题。
依赖：动作队列语义见 `action-manager.md`；回合块内 `applyStartOfTurnOrbs` 的调用位置见 `turn-phase.md` R13 步骤⑤；伤害类型见 `damage-pipeline.md`（THORNS 语义）。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 数据模型与槽位

**R01 orbs 是 ArrayList，空槽用 EmptyOrbSlot 占位；index 0 = 最左（最先被激发），末尾 = 最新** — 出处 `AbstractPlayer#evokeOrb` offset 10-21（orbs[0] instanceof EmptyOrbSlot 检查）、`#channelOrb` offset 73-111（正向找首个 EmptyOrbSlot）。置信度：**高**
列表长度 = maxOrbs（槽位数），非"实际珠数"；`filledOrbCount()`（offset 0-43）统计非 EmptyOrbSlot 元素。

**R02 槽位上限 10；加槽在列表尾部追加 EmptyOrbSlot** — 出处 `AbstractPlayer#increaseMaxOrbSlots(int,boolean)` offset 0-90（`maxOrbs==10 → ThoughtBubble + return`；否则 maxOrbs+=n 并循环 `orbs.add(new EmptyOrbSlot())`）+ `IncreaseMaxOrbAction#update`（循环 amount 次 1 槽版）。置信度：**高**

**R03 减槽删的是列表最右元素（真珠或空槽都不激发、静默消失）** — 出处 `AbstractPlayer#decreaseMaxOrbSlots` offset 0-95。置信度：**高**
`maxOrbs--`（钳 0）→ `orbs.remove(orbs.size()-1)` → 全员 `setSlot(i, maxOrbs)`。**不调 onEvoke、无补偿**——被删的真是充能珠就白丢（vanilla 调用者：Certain events/`DecreaseMaxOrbAction`，后者逐次调 1 槽版）。

---

## 2. 通道（channelOrb / ChannelAction）

**R04 channelOrb 完整序（有空槽路径）** — 出处 `AbstractPlayer#channelOrb` offset 0-309。置信度：**高**
```
① maxOrbs <= 0 → ThoughtBubble("...")，return                       [0-38]
② hasRelic("Dark Core") 且新珠非 Dark → 替换为 new Dark()            [46-70]
③ 正向扫描 orbs 找首个 EmptyOrbSlot：
   找到 index i：
     新珠继承该槽 cX/cY → orbs.set(i, orb) → orb.setSlot(i, maxOrbs)
     → orb.playChannelSFX()
     → player.powers 逐个 onChannel(orb)                            [185-221]
     → 登记 actionManager.orbsChanneledThisCombat / ThisTurn          [222-242]
     → 本回合 Plasma 计数 == 9 → 解锁 NEON 成就                      [244-303]
     → orb.applyFocus()（仅新珠此刻重算一次）                        [305]
   没找到（index==-1）：                                              [312-354]
     addToTop 三连（连用三次 addToTop ⇒ 执行序与书写序相反）：
       ChannelAction(orb) → EvokeOrbAction(1) → AnimateOrbAction
     执行序 = AnimateOrb → Evoke(逐出 orbs[0]) → Channel（此刻有空槽，
     走上面 ③ 的正常路径）⇒ "满槽通道 = 先逐出最左珠再通道"
```

**R05 ChannelAction 两种模式** — 出处 `ChannelAction#update` offset 0-99。置信度：**高**
- `autoEvoke==true`：直接调 `channelOrb(orb)`（满槽时由 R04 的重排队机制先激发）；
- `autoEvoke==false`：自己扫描 orbs 找 EmptyOrbSlot，**找到才**调 channelOrb；找不到则**什么都不发生**（不通道、不激发、无提示）——卡牌语义上这类卡应当先保证空槽（如 Capacitor/熔断效应），移植时勿给非 autoEvoke 卡补"自动逐出"。

---

## 3. 激发与移除家族

**R06 evokeOrb() = 激发最左珠并移除** — 出处 `AbstractPlayer#evokeOrb` offset 0-131。置信度：**高**
`orbs[0]` 为 EmptyOrbSlot 或列表空 → no-op；否则 `orbs[0].onEvoke()`（同步直调——各珠把效果 action addToTop/addToBot）→ 用相邻 swap 级联把列表左移一位 → 末尾放 new EmptyOrbSlot → 全员 `setSlot(i, maxOrbs)`。

**R07 激发家族语义对照** — 出处：`#evokeNewestOrb` offset 0-53；`#evokeWithoutLosingOrb` offset 0-37；`#removeNextOrb` offset 0-146；`EvokeOrbAction#update`；`EvokeAllOrbsAction#update`；`RemoveAllOrbsAction#update`；`EvokeWithoutRemovingOrbAction`（→ evokeWithoutLosingOrb）。置信度：**高**

| 方法/动作 | 激发对象 | 是否移除珠 | 备注 |
|---|---|---|---|
| `evokeOrb()` / `EvokeOrbAction(n)` | orbs[0]（最左） | 是（尾部补空槽） | n 次循环每次重读 orbs[0] |
| `evokeNewestOrb()` | orbs[last]（最新） | **否**（仅 onEvoke，珠原地保留） | Multi-Cast 打右珠反复激发的实现基础 |
| `evokeWithoutLosingOrb()` | orbs[0] | **否** | 珠保留，仅结算一次效果 |
| `removeNextOrb()` | orbs[0] | 是（换 EmptyOrbSlot） | **不调 onEvoke**，纯移除 |
| `EvokeAllOrbsAction` | — | — | 循环 N=执行时 `orbs.size()`（**含空槽**）次 `addToTop(EvokeOrbAction(1))`；空槽珠是 no-op |
| `RemoveAllOrbsAction` | — | — | `while (filledOrbCount() > 0) removeNextOrb()`：全部**移除不激发** |

**R08 onEvoke 内的动作入队方向：四原珠统一 addToTop** — 出处 Frost/Lightning/Dark/Plasma `#onEvoke`（各 offset 0-33/76/33/17）。置信度：**高**
激发效果插队首 ⇒ 在"引发激发的当前动作"之后立即结算，先于任何已排队的后续动作（如 Evoke All 的下一个 EvokeOrbAction——多次激发时第 1 珠的结算先于第 2 次激发）。

---

## 4. 四原珠被动/激发数值与时点

**R09 数值总表** — 出处：Frost offset 97-70（onEvoke/onEndOfTurn）、Lightning offset 92-124、Dark offset 83-63、Plasma offset 90-43。置信度：**高**

| 珠 | 被动（触发点） | 激发（onEvoke，addToTop） |
|---|---|---|
| Frost | `onEndOfTurn`：VFX + `addToBot(GainBlockAction(passiveAmount, true))` | `addToTop(GainBlockAction(evokeAmount))` |
| Lightning | `onEndOfTurn`：Electro power 在场 → `LightningOrbEvokeAction(DamageInfo(passiveAmount, THORNS), hitAll=true)`；否则 `LightningOrbPassiveAction(随机单敌)` | 同上但用 `evokeAmount`，Electro 门控同构 |
| Dark | `onEndOfTurn`：`evokeAmount += passiveAmount`（累积，不入队任何动作） | `addToTop(DarkOrbEvokeAction(DamageInfo(evokeAmount, THORNS), FIRE))`（打血量最低敌人，见 DarkOrbEvokeAction） |
| Plasma | `onStartOfTurn`：VFX + `addToBot(GainEnergyAction(passiveAmount))` | `addToTop(GainEnergyAction(evokeAmount))` |

要点：
- **球类伤害全部是 THORNS 类型**（Lightning/Dark 的 DamageInfo 显式第三参）⇒ 不吃力量/虚弱/易伤、可被格挡（damage-pipeline.md R02/R16；电球被动用 LightningOrbPassiveAction 同样 THORNS）。
- **被动触发在回合尾部统一入队**（见 R10），Dark 累积先于激发发生（先攒后炸）。
- Plasma 是唯一的 onStartOfTurn 珠（R10 的 Cables 双触发对 Plasma 同样生效：回合开始多回 1 能量）。

**R10 回合触发器：玩家回合尾与回合开始的珠触发都是"队列化动作"** — 出处 `TriggerEndOfTurnOrbsAction#update` offset 0-96；`AbstractPlayer#applyStartOfTurnOrbs` offset 0-82。置信度：**高**
- 回合尾：`addToBottom(TriggerEndOfTurnOrbsAction)` 在哨兵链（turn-phase.md R07 步骤③）入队——因 actions 优先级高于 cardQueue，它**抢先于回合尾自动结算牌**执行：遍历全部 orbs（含 EmptyOrbSlot，基类空实现）调 `onEndOfTurn()`，随后若持 Cables（GoldPlatedCables，ID 字符串 "Cables"）且 orbs[0] 非空 → **orbs[0].onEndOfTurn() 再来一次**。
- 回合开始：新回合块内同步直调 `applyStartOfTurnOrbs()`：遍历 orbs 调 `onStartOfTurn()`（Plasma 产电），Cables 同款 orbs[0] 追加。
- Cables 的双触发**只对 index 0**（最左），两处都有 EmptyOrbSlot 守卫。

---

## 5. Focus（焦点）

**R11 applyFocus 公式与 Plasma 豁免** — 出处 `AbstractOrb#applyFocus` offset 0-77。置信度：**高**
```
focus = player.getPower("Focus");
if (focus != null && ID != "Plasma"):
    passiveAmount = max(0, basePassiveAmount + focus.amount);
    evokeAmount   = max(0, baseEvokeAmount   + focus.amount);
else:
    passiveAmount = basePassiveAmount; evokeAmount = baseEvokeAmount;
```
**Dark 覆写：只重算 passiveAmount，绝不触碰 evokeAmount**（`Dark#applyFocus` offset 0-41）——被动部分与基类公式相同（`max(0, basePassive + focus.amount)`），但 evokeAmount 保持通道以来的累积值。⇒ 焦点变化/丢失不会重置暗珠已累积的炸伤；暗珠的被动增速随焦点走、累积存量不动。

**R12 焦点变化对已有珠的作用是**不对称**的：增加刷新、递减逐次刷新、完全移除不刷新** — 出处 `AbstractDungeon#onModifyPower` offset 0-60（`if (player.hasPower("Focus"))` 门）+ Frost/Lightning/Dark/Plasma/EmptyOrbSlot 各自 `updateDescription()` 首行 `applyFocus()` + `RemoveSpecificPowerAction#update`（`onRemove` → remove → onModifyPower）+ `ReducePowerAction#update` 两分支。置信度：**高**
```
onModifyPower(): hand.applyPowers();  if (hasPower("Focus")) { for orb: orb.updateDescription(); }  // 门!
```
- **获得/增加 Focus**：ApplyPowerAction 后 onModifyPower，门为真 → 全员珠 refresh（数值上调）。
- **Focus 递减但仍在**（ReducePowerAction 走 reduce 分支，offset 63-78 内有 onModifyPower）：每次递减后全员珠下调一档。
- **Focus 1→0 走 Remove 分支 / 直接 RemoveSpecificPowerAction**：onModifyPower 在**移除之后**调用，`hasPower("Focus")` 已为假 → **宝珠刷新循环被跳过**；且 Remove 分支（ReducePowerAction offset 81-98）本身不调 onModifyPower。⇒ **已有珠冻结在最后一次成功刷新的值（= base+1），不回落 base**；此后不再跟随任何 Focus 变化（`AbstractOrb.update()`/`setSlot` 均不重算， orbs.md R12 前置证据），直到该珠被激发移除或新珠通道（新珠走 R04 的 applyFocus 兜底）。
- 唯一的显式 applyFocus 调用点除 onModifyPower 链路外只有 channelOrb（R04）。
仲裁推论：Biased Cognition（+4 Focus，每回合 -1）到期过程 = 4→3→2→1 每回合全员下调，1→0 那一回合起已有珠**停在 base+1**；Reprogram 一类一次性移除 Focus 则已有珠**保留全部旧加成**。
**勘误记录**：本卷初版曾写"失去 Focus 珠立即回落 base"，与字节码不符，以本条为准。

**R13 焦点上限/下限** — 出处 R11 公式（`max(0, base + focus)`）+ `FocusPower#stackPower` offset 0-75。置信度：**高**
Focus power 负值合法（`base + 负值` 可被钳到 0）；FocusPower 在 `stackPower` 内 `amount == 0`（offset 16-41）/`reducePower` 内同类条件时 `addToTop(RemoveSpecificPowerAction)` 自移除（另有 25 层 FOCUSED 成就、999 钳制）。"无 Focus 时新通道的珠"走 else 分支 = base 值（R11）；**已有珠**的行为见 R12 的不对称结论。

---

## 6. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| 满槽时通道（Storm 闪电等 autoEvoke 或直调 channelOrb） | 先激发最左珠（其效果 addToTop 先结算），再通道新珠 | R04 |
| 满槽时非 autoEvoke 的 ChannelAction | 无事发生（静默失败） | R05 |
| Evoke All + Dark（暗珠在中间） | 逐个激发：每次 onEvoke 的 DarkOrbEvoke addToTop，在下一枚激发之前结算；暗珠打"当时血量最低"敌 | R06/R08 |
| Focus +4 打出时场上已有 2 Frost(2) | 两 Frost 立即变 passive 6（onModifyPower 门为真，全员刷新） | R12 |
| Biased Cognition 逐回合 -1 到期 | 4→1 每回合全员下调；1→0 起已有珠**冻结在 base+1**，不回落 base | R12 |
| Reprogram 一次移除 Focus | 已有珠保留全部旧加成（onModifyPower 门为假，无刷新） | R12 |
| Cables + 首位 Plasma | 回合开始 Plasma 触发两次：+2 能量 | R10 |
| 电球被动 vs 敌 5 格挡 | THORNS 可被格挡吸收（不吃力/虚/易伤） | R09 + damage-pipeline R13 |
| 减 1 槽（DecreaseMaxOrb）而最右是充能 Lightning | Lightning 消失，不激发不掉效果 | R03 |
| Multi-Cast(2) 打最新暗珠 | 暗珠不消失，连续两次 evokeNewestOrb 各结算当时 evokeAmount | R07 |

---

## 7. 开放问题 / 低置信项

1. **DarkOrbEvokeAction 的"最低血量"选敌逻辑**（含平局/全空场处理）未逐字节取证，仅有类职责与常量池佐证。置信度：**中**。
2. `EvokeWithoutRemovingOrbAction` 与 `IncreaseMaxOrbAction(amount>1)` 的循环细节同构已证，但 vanish 路径（Orb 界面动画竞态）未覆盖。置信度：**中**。
3. orbsChanneledThisTurn/Combat 两个列表的消费方（部分卡牌如 Storm? All For One? 成就外）未穷举。置信度：**低**。
4. EmptyOrbSlot.updateDescription 的 applyFocus 行为（继承基类公式但 ID 非 Plasma → 会算 base 值，无实际意义）——静态推断。置信度：**中**。
5. LoopPower/StormPower/WinterPower/StaticDischargePower 引用 orb updateDescription 的具体语境（多为通道触发器）未逐一展开，属触发器卷范围。置信度：**未定**。
