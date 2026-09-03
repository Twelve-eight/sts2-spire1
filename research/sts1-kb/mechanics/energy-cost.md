# 费用与能量管线（Energy & Cost Pipeline）— StS1 战斗语义知识库

## 本卷范围
能量三变量模型、开局/每回合的发放与重置真实位置（**含对 turn-phase.md R02 的重要勘误**）、出牌门禁 hasEnoughEnergy 全门序、费用变量生命周期（cost/costForTurn/resetAttributes）、Confusion/Madness 等费用变异、freeToPlay/X 费豁免。
依赖引用：出牌扣能时序 → `turn-phase.md` R16 步骤⑧；X 费多段快照 → `damage-pipeline.md` R09。

**图例**：出处 `类名#方法` + javap 偏移；置信度 **高**=字节码直接可证 / **中**=字节码+推断（注明）/ **低**=仅 wiki。基准 jar：desktop-1.0.jar v2.x。

---

## 1. 能量模型与每回合真实重置点

**R01 三变量模型** — 出处 `EnergyManager`（`energyMaster`/`energy` 字段）+ `EnergyPanel.totalCount`（静态 int）。置信度：**高**
`energyMaster` = 角色模板（Ironclad 3 等，改自角色卡）；`EnergyPanel.totalCount` = **唯一生效的余量**（`useEnergy`/`addEnergy` 都直接操作它，钳 [0,999]，≥9 触发 ADRENALINE 成就）。`EnergyManager.energy` 只是模板副本（prep 时刷新）。

**R02 战斗开局：prep 归零 + 首个动作发放** — 出处 `EnergyManager#prep`（`energy=energyMaster; totalCount=0`）、`AbstractPlayer#preBattlePrep`（offset 207-210 调 prep）、`GainEnergyAndEnableControlsAction#update` offset 10-157。置信度：**高**
开局动作序：`player.gainEnergy(energyMaster)`（additive，从 0 起加）→ `updateEnergyGain` → 手牌逐卡 `triggerOnGainEnergy(n,false)` → 全 relic `onEnergyRecharge()` → 全 power `onEnergyRecharge()` → **`actionManager.turnHasEnded=false`**（R03 闩的解除点，turn-phase.md R03 一致）。

**R03 【勘误 turn-phase.md R02】每回合能量确实重置，重置点在视效类构造器里** — 出处 `PlayerTurnEffect#<init>` offset 213-219（`player.energy.recharge()`）+ `EnergyManager#recharge` offset 0-118 + `DrawCardAction` 3 参构造（true 时 new PlayerTurnEffect 入 topLevelEffects）。置信度：**高**
```
recharge():
  if (hasRelic("Ice Cream")):                 // 保留余额
      if (totalCount > 0) flash + addToTop(RelicAboveCreatureAction)
      addEnergy(energy)                        // 叠加，不清余额
  elif (hasPower("Conserve")):                // 日替 mod
      if (totalCount > 0) addToTop(ReducePowerAction(Conserve,1))
      addEnergy(energy)
  else:
      setEnergy(energy)                        // ★ 硬重置为模板值，余额清零
  updateEnergyGain(energy)
```
`PlayerTurnEffect` 构造器在 recharge 之后依次同步直调：全 relic `onEnergyRecharge()` → 全 power `onEnergyRecharge()`（Energized"下回合+N 能量"在此发放并自移除，`EnergizedPower#onEnergyRecharge` offset 0-35）→ TURN_EFFECT 音效 → `MonsterGroup.showIntent()`（意图数字刷新点）。
**触发时刻**：新回合块内 `DrawCardAction(null, gameHandSize, true)` **入队构造的瞬间**（`getNextAction` offset 2186-2199）——即 start-of-turn 钩子梯（offset 2065-2089）**之后**、PostDraw 钩子（offset 2205-2214）**之前**。开局第一回合不走此路径（开局用 2 参 DrawCardAction 无横幅，能量由 R02 的专用动作发放）。
**勘误正文**：turn-phase.md R02 原文"新回合块不补发能量——未用完的能量跨回合保留"错误；vanilla 每回合开始把余额硬重置为模板值，**仅 Ice Cream（或日替 Conserve）保留余额**（保留方式也是"叠加"而非"不清零"）。R02 其余内容（energyMaster 模板、999 上限、开局只发一次的专用动作）不受影响。
仲裁推论：`atStartOfTurn` power 钩子读到的能量是**上一回合的余额**（重置发生在钩子梯之后）；`atStartOfTurnPostDraw` 系读到的是**本回合模板值**。

**R04 atEnergyGain 是视效收尾钩子** — 出处 `PlayerTurnEffect#update` 尾段 offset 265-301（isDone 时全 power `atEnergyGain()`）。置信度：**高**
与 R03 的 onEnergyRecharge 是两个不同钩子；能量数值变化前（onEnergyRecharge）与横幅动画结束后（atEnergyGain）各通知一次。

---

## 2. 出牌门禁（canUse / hasEnoughEnergy）

**R05 hasEnoughEnergy 七道门序** — 出处 `AbstractCard#hasEnoughEnergy`（方法头 offset 0-273）。置信度：**高**
```
① actionManager.turnHasEnded == true → false（TEXT[9]）
② 任一 power canPlayCard(this)==false → false（TEXT[13]）
③ Entangled power 且本卡 ATTACK → false（TEXT[10]）
④ 任一 relic canPlay(this)==false → false
⑤ 任一 blight canPlay(this)==false → false
⑥ 手牌中任一卡 canPlay(this)==false → false（手牌侧否决钩子）
⑦ EnergyPanel.totalCount < costForTurn 时：
     freeToPlay() || isInAutoplay → true（免费放行）
     否则 cantUseMessage=TEXT[11]（"没有足够能量"），false
   totalCount >= costForTurn → true
```
`freeToPlay()`（同文件）：`freeToPlayOnce || costForTurn<=0 || (X费且无能量需求) || COMBAT 房间外` 语义集合（以字节码为准，含免费标记与房间态）。

**R06 canUse 的类型门** — 出处 `AbstractCard#canUse` offset 0-82。置信度：**高**
STATUS 且 `costForTurn < -1`（即 -2 标记）→ 除持 Medical Kit 外不可打出；CURSE 同理（Blue Candle）；其余 → `cardPlayable(target) && hasEnoughEnergy()`。注意判定读的是 **costForTurn** 而非 cost——临时费用可把不可打出的牌救回来（Medical Kit 情形下 status 的 costForTurn 被 action 置正）。

---

## 3. 费用变量生命周期

**R07 三变量与复原点** — 出处 `AbstractCard` 字段（`cost`/`costForTurn`/`isCostModified(ForTurn)`）+ `#resetAttributes`（offset 0-57：`costForTurn=cost; isCostModifiedForTurn=false`，另有 block/damage/magicNumber/damageTypeForTurn 从 base 复原）。置信度：**高**
`resetAttributes` 的调用点 = 结束回合时三堆+悬停卡全量（turn-phase.md R09 步骤4）。⇒ **costForTurn 的一切临时修改只在当前回合有效**；改 `cost` 本身（Confusion）则跨回合残留（R08）。

**R08 Confusion 改的是 cost 本体** — 出处 `ConfusionPower#onCardDraw` offset 0-46。置信度：**高**
抽到的每张 `cost>=0` 的卡：`cost = costForTurn = random(0,2)`（cardRandomRng，与原值不同才写），`isCostModified=true`，并清 `freeToPlayOnce=false`。⇒ 混乱状态的牌**基础费被永久改写**（洗回牌堆再抽会再次重骰；本回合没打出的牌保留乱值到回合尾 reset 复原为已乱的新 cost）。

**R09 Madness（疯狂）** — 出处 `MadnessAction#update`（前半 offset 0-72：扫描手牌，`costForTurn>0` 或 `cost>0` 者为候选；后半选一置零）。置信度：**中**（前半高置信；后半选卡/置零分支未逐字节展开）
语义 = 本战斗内将手牌一张有费卡费用降为 0（卡牌描述与动作结构一致；置零落在 cost/costForTurn 的哪一侧未终验，移植时对拍 MadnessAction 字节码）。

**R10 freeToPlayOnce 与 isInAutoplay 的消费点** — 出处 `UseCardAction#update` 首帧（复位两者，triggers.md §2.4）、`hasEnoughEnergy` R05⑦、`AbstractPlayer#useCard`（autoplay 时置 freeToPlayOnce，turn-phase.md R16 步骤3）。置信度：**高**
`freeToPlayOnce` 是一次性豁免（"下一张免费"类效果），`isInAutoplay`（回合尾自动打出）豁免能量但照走扣能门以外的全部流程。

---

## 4. 仲裁案例表

| 场景 | 结局 | 依据 |
|---|---|---|
| 上回合剩 2 能量，无冰淇淋，新回合开始 | 余额清零，重置为 3（setEnergy）；atStartOfTurn 钩子若读能量读到旧值 2 | R03 推论 |
| 同上 + 冰淇淋 | 3+2=5（addEnergy 叠加），遗物闪亮 | R03 |
| Energized(1) 上回合挂上 | 本回合 recharge 时 powers.onEnergyRecharge 发放 +1 并自移除 | R03 |
| Entangled 回合打攻击牌 | hasEnoughEnergy ③ 拒绝（先于能量检查） | R05 |
| 混乱状态牌洗回再抽 | 再骰 0-2；留在手里则保持 | R08/R07 |
| X 费牌在 0 能量时（被 Burst 排队） | useCard 把 energyOnUse 钳为当前能量（可为 0），照常结算 | turn-phase.md R16③ |
| 手牌中某卡 canPlay 否决（Heel Hook 类条件卡） | 本卡 hasEnoughEnergy 直接 false | R05⑥ |

---

## 5. 开放问题 / 低置信项

1. `freeToPlay()` 的完整分支（含非战斗房间语义）未逐字节展开，仅确认其为豁免集合入口。置信度：**中**。
2. MadnessAction 后半的选卡与置零落点未终验（R09）。置信度：**中**。
3. `triggerOnGainEnergy(card, boolean)` 第二参语义（开局 false / 其他场景 true?）未穷举调用方。置信度：**低**。
4. 两个 onEnergyRecharge 之外的能量事件（atEnergyGain 的其他调用点，如部分遗物）未穷举。置信度：**低**。
