# 姿态管线（Stance Pipeline）— StS1 战斗语义知识库

## 本卷范围
观者姿态系统的换姿态仲裁：ChangeStanceAction 全序（含 CannotChangeStance 门、同姿态幂等、四类订阅者的同步通知序）、四姿态的进出钩子与自动退场、姿态回合钩子的调用点（补记 `stance.atStartOfTurn` 首位）、与渎神(EndTurnDeathPower)的时序交叉、姿态计数器与死代码注记。
交叉引用：姿态伤害倍率在计算链的位置 → `damage-pipeline.md` R06 步骤③ / R07 步骤③；`stance.onPlayCard/onEndOfTurn` 在回合流水中的位置 → `triggers.md` §2.1/R16；渎神死本体 → `death-arbitration.md` §3。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x。

---

## 1. ChangeStanceAction 全序

**R01 换姿态动作的完整字节码序** — 出处 `ChangeStanceAction#update` offset 11-328。置信度：**高**
```
① hasPower("CannotChangeStancePower") → isDone，return（换姿态被禁止）   [14-27]
② oldStance = player.stance
③ oldStance.ID == 目标 ID（同姿态）→ 跳到 ⑨（幂等：只刷新，不触发任何钩子）[35-46]
④ newStance = getStanceFromName(id)（字符串解析，懒构造）                 [56-64]
⑤ 同步直调：player.powers 逐个 onChangeStance(old, new)                  [77-108]
⑥ 同步直调：player.relics 逐个 onChangeStance(old, new)                  [118-148]
⑦ oldStance.onExitStance()（Calm 在此 +2 能量：addToBottom）              [149-152]
⑧ player.stance = newStance; newStance.onEnterStance()                    [153-169]
⑨ uniqueStancesThisCombat[新ID]++（HashMap 计数）                         [170-258]
⑩ player.switchedStance()（动画态标记）
⑪ 弃牌堆逐卡 triggerExhaustedCardsOnStanceChange(newStance)               [268-308]
⑫ player.onStanceChange(id)（基类空实现；Watcher 覆写 = 纯眼球动画）      [311-315]
⑬ AbstractDungeon.onModifyPower()（全局刷新手牌数值/宝珠/怪物意图）        [318]
```
仲裁要点：**⑤⑥ 的订阅者先于 ⑦ 的离场结算**——Mental Fortress/Rushdown/Violet Lotus 的效果动作排在 Calm 退场能量的**前面**入队。

**R02 vanilla 的 onChangeStance 实现者全集** — 出处 常量池全扫（2306 class）。置信度：**高**
`MentalFortressPower`（换姿态加格挡）、`RushdownPower`（换姿态抽牌）、`VioletLotus`（遗物：每回合首次换姿态 +1 能量）、`DEPRECATEDMasteryPower`（废弃）。四个实现者在同一次换姿态内的相对顺序 = 各自容器内的插入序（R01⑤⑥，参照 triggers.md R15）。

**R03 CannotChangeStancePower 本版本不可达** — 出处 全 jar 引用扫描：仅 `powers/watcher/CannotChangeStancePower.class`（定义）与 `ChangeStanceAction`（读取门）两处，**无任何卡牌/遗物/事件施加它**。置信度：**高**
门逻辑存在但内容物缺失（mod 面向钩子）；移植时可保留门、按需实现施加方。

---

## 2. 四姿态钩子清单

**R04 姿态进出钩子语义表** — 出处：`CalmStance#onExitStance` offset 0-20（`addToBottom(GainEnergyAction(2))`）；`DivinityStance#atStartOfTurn` offset 0-15（`addToBottom(ChangeStanceAction("Neutral"))`）；`WrathStance#onEnter/onExitStance`（仅停音效）；`NeutralStance`（无钩子）。置信度：**高**

| 姿态 | onEnterStance | onExitStance | atStartOfTurn | onEndOfTurn | 伤害面 |
|---|---|---|---|---|---|
| Wrath | 停音效 | 停音效 | — | — | 给伤 ×2（give 层）；受伤 ×2（DamageInfo.applyPowers 步骤③） |
| Calm | 停音效 | **+2 能量（addToBottom）** | — | — | — |
| Divinity | 停音效 | 停音效 | **自退 Neutral（addToBottom）** | — | 给伤 ×3（give 层） |
| Neutral | 空 | 空 | 空 | 空 | — |

- Calm 的能量在**离场**时发且走队列（addToBottom）⇒ 排在同动作链后续位置，非瞬发。
- Divinity 的自动退场是**回合开始钩子**（见 R05 调用点），不是持续一整回合的定时器。

**R05 `stance.atStartOfTurn` 调用点 = `applyStartOfTurnRelics` 首位** — 出处 `AbstractPlayer#applyStartOfTurnRelics` offset 0-7（方法第一条指令即 `stance.atStartOfTurn()`，随后 relics → blights）。置信度：**高**
**勘误**：`triggers.md` 开放问题 1（"stance.atStartOfTurn 调用点未定位"）就此关闭——它在**每回合新回合块**的 start-of-turn 遗物梯（turn-phase.md R13 步骤③、开局块 R01 步骤⑪）第一步同步直调，**先于** relic `atTurnStart`、先于 power `atStartOfTurn` 梯。

**R06 Divinity 自动退场 vs 渎神死亡的时序** — 出处 R05 + `death-arbitration.md` R12。置信度：**高**
新回合块内入队顺序：`applyStartOfTurnRelics`（Divinity.atStartOfTurn 在此排队 `ChangeStanceAction("Neutral")`）**先于** `applyStartOfTurnPowers`（EndTurnDeathPower 在此排队 VFX→LoseHP→Remove）。执行序：先退出 Divinity（失去 ×3 增伤），**再**结算渎神 99999 失血。⇒ "渎神回合开始时你已不在 Divinity"。

**R07 同姿态 ChangeStance 是幂等刷新** — 出处 R01③。置信度：**高**
在 Calm 里再 `ChangeStanceAction("Calm")`：不触发 onChangeStance 订阅者、不触发 Calm 退场能量、不计数；仅结尾 `onModifyPower()` 全局刷新。⇒ "刷 Calm 能量"类玩法不存在（退场能量只在真离场时发）。

---

## 3. 姿态与伤害/回合流水的挂点

**R08 姿态在伤害计算链中的层级** — 出处 `damage-pipeline.md` R06 步骤③ / R07 步骤③（relics→powers.give 之后、目标 receive 之前）。置信度：**高**（引用）
Wrath ×2 / Divinity ×3 挂 `player.stance.atDamageGive`（give 层后段）：与力量**乘算**（先 +力量 后 ×2），与 Weak（×0.75）、Pen Nib（give 层）按 ②③ 顺序乘算；受击倍率 Wrath ×2 挂 `DamageInfo.applyPowers` 步骤③（target==player 的 stance.atDamageReceive）——**只有非卡牌伤害源链**（如敌方攻击预演算）会走；玩家以 Wrath 受击的敌方攻击伤害在怪物 `applyPowers` 时已翻倍。

**R09 姿态钩子在回合流水中的全部四个挂点** — 出处 `triggers.md` §2.1（onPlayCard，getNextAction ④ 位）、`turn-phase.md` R07 步骤⑤（onEndOfTurn，哨兵链末位）、R05 本文（atStartOfTurn，遗物梯首位）、R01⑧（进出）。置信度：**高**（汇总）
汇总表：姿态无 `duringTurn`/`onExhaust`/`onDeath` 参与；`stance.onPlayCard` 在 relics 之后、blights 之前（出牌每张同步直调）。

---

## 4. 辅助动作与计数器

**R10 StanceCheckAction / NotStanceCheckAction** — 出处 两类 update（首帧 `player.stance.ID` 判定，匹配则执行 followUp 动作或跳过）。置信度：**中**（结构已证，逐卡使用者未穷举）
"检查是否处于/不处于某姿态再执行后续动作"的条件包装，供卡牌 use() 拼接。

**R11 uniqueStancesThisCombat 计数器在本版本无读取者** — 出处 全 jar 引用扫描：仅 `GameActionManager`（字段定义）与 `ChangeStanceAction`（++维护）两处，无任何卡牌/power 读取。置信度：**高**
推论：观者"本战斗用过 N 种姿态"类效果在本 build 无原生消费者（后续版本或 mod 内容的预留计数）。移植仲裁时按"维护但不用"处理，勿臆造语义。

**R12 player.onStanceChange 是角色覆写钩子，基类空** — 出处 `AbstractPlayer#onStanceChange`（`0: return`）+ `Watcher#onStanceChange`（按 ID 切 Spine 眼球动画，纯表现）。置信度：**高**

---

## 5. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| Calm → Wrath，带 Mental Fortress + Rushdown | 订阅序：Fortress 格挡动作 → Rushdown 抽牌动作 → Calm +2 能量（入队序即执行序） | R01⑤⑦/R02 |
| Wrath 中打出渎神，下回合开始 | 先退 Divinity（若无）→渎神 VFX→99999 失血；Divinity 增伤在死亡结算前已失效 | R06 + death-arbitration.md R12 |
| Divinity 态过自己回合开始 | 自动 ChangeStance("Neutral")，×3 增伤只活到本回合结束前的行动窗口 | R04/R05 |
| Calm 中被施加 ChangeStance("Calm") | 无能量、无订阅者触发 | R07 |
| Wrath 中受敌 10 伤（无其他增减） | 怪物攻击预演算时 ×2 = 20（applyPowers 步骤③），实伤 20 | R08 |
| Violet Lotus 首次换姿态 | +1 能量（addToBot，排在姿态订阅者效果之后） | R01/R02 |
| 同回合"进入又退出"Calm | 进时不发能量；退出那次发 +2（离场才结算） | R04 |

---

## 6. 开放问题 / 低置信项

1. `StanceCheckAction/NotStanceCheckAction` 的 vanilla 使用卡清单未穷举（Transcendence? Mental Fortress 卡牌侧?）。置信度：**中**。
2. `triggerExhaustedCardsOnStanceChange` 的弃牌堆卡实现者清单（应为 Vengeance 类"姿态切换消耗弃牌堆"效果）未取证。置信度：**低**。
3. Divinity 自退与"玩家在 Divinity 态回合开始前死亡"（敌方回合死）无交互——死亡即战斗结束，姿态钩子不再跑（turnHasEnded 门）。静态推断。置信度：**中**。
4. Blasphemy 的 EndTurnDeathPower 与 "FearNoEvil"（消除姿态类 debuff？）等观者功能卡的交叉未取证。置信度：**未定**。
