# 遗物触发矩阵（Relic Trigger Matrix）— StS1 战斗语义知识库

## 本卷范围
对 jar 内**全部 190 个遗物类**（`javap -p` 全量 + 签名级正则，工具 `../scan-hooks.mjs relics`）按钩子归类建档：战斗开场两段、回合两段、回合尾、出牌计数族、洗牌/弃牌/消耗、受击族、死亡/胜利、血量阈值、checkTrigger/onTrigger 特例、onEquip。成员清单即数据本体；计数遗物的通用模式已录 triggers.md §4（计数点在 onUseCard 构造期），此处不重复。
坑与局限：常量池"引用≠调用"（MarkOfPain 命中 EnergyManager 字符串但真实挂点是 atBattleStart）、继承字段不可见（counter 在基类）——见 `../../kb/research-methods.md` M4-M6。

**图例**：置信度 **高**=签名扫描直接可证。基准 jar：desktop-1.0.jar v2.x。基类 AbstractRelic 空实现已剔除；`Test1/3/4/5/6` 为官方 Beta 测试遗物。

---

## 1. 战斗开场与回合时点

**R01 atBattleStart（开局初始化块，先于初始抽牌）— 40 个** — 出处 `scan-hooks.mjs relics`。置信度：**高**
```
Akabeko, Anchor, BagOfMarbles, BagOfPreparation, BloodVial, BottledFlame,
BottledLightning, BottledTornado, BronzeScales, CaptainsWheel, ClockworkSouvenir,
CultistMask, DataDisk, DuVuDoll, FossilizedHelix, Girya, GremlinMask, HornCleat,
InkBottle, MarkOfPain, MutagenicStrength, NeowsLament, OddlySmoothStone, Pantograph,
PenNib, PhilosopherStone, Pocketwatch, PreservedInsect, RedMask, RedSkull, Sling,
SnakeRing, StoneCalendar, TeardropLocket, ThreadAndNeedle, TwistedFunnel, Vajra,
VelvetChoker, Test4, DEPRECATEDDodecahedron
```
**R02 atBattleStartPreDraw（开局"抽牌前"段）— 5 个** — 出处同 R01（调用点 turn-phase.md R01 步骤④）。置信度：**高**
```
GamblingChip, HolyWater, NinjaScroll, PureWater, Toolbox
```
**R03 atTurnStart（每回合开始，遗物梯）— 27 个** — 出处同 R01（stance.atStartOfTurn 之后，stances.md R05）。置信度：**高**
```
AncientTeaSet, ArtOfWar, Brimstone, CaptainsWheel, Damaru, EmotionChip, HappyFlower,
HornCleat, HoveringKite, IncenseBurner, Inserter, Kunai, Lantern, LetterOpener,
MercuryHourglass, Necronomicon, OrangePellets, Orichalcum, OrnamentalFan,
RingOfTheSerpent, RunicCapacitor, Shuriken, StoneCalendar, UnceasingTop,
VelvetChoker, Test6, DEPRECATEDDodecahedron
```
**R04 atTurnStartPostDraw（抽牌动作入队后）— 3 个** — 出处同 R01（turn-phase.md R04 命名陷阱适用）。置信度：**高**
```
GamblingChip, Pocketwatch, WarpedTongs
```
**R05 onPlayerEndTurn（回合尾哨兵链第一段）— 6 个** — 出处同 R01（turn-phase.md R07 步骤①）。置信度：**高**
```
CloakClasp, FrozenCore, NilrysCodex, Orichalcum, StoneCalendar, Test6
```
仲裁点：Orichalcum"回合结束格挡≥10 则保 5"在 onPlayerEndTurn——**先于**回合金币/回合尾自动结算牌；FrozenCore 同理（回合尾充能闪电球）。

---

## 2. 出牌/洗牌/弃牌/消耗事件

**R06 onUseCard（UseCardAction 构造期，计数遗物主入口）— 16 个** — 出处同 R01（模式详见 triggers.md §4）。置信度：**高**
```
ArtOfWar, BirdFacedUrn, BlueCandle, Duality, InkBottle, Kunai, LetterOpener,
MedicalKit, MummifiedHand, Necronomicon, Nunchaku, OrangePellets, OrnamentalFan,
PenNib, Shuriken, DEPRECATEDYin
```
**R07 onShuffle — 3 个**：`Abacus, Melange, Sundial`（Sundial 计洗牌非出牌，triggers.md R18 勘误）。
**R08 onManualDiscard — 3 个**：`HoveringKite, Tingsha, ToughBandages`（回合尾弃牌不触发，draw-exhaust.md R22）。置信度：**高**
**R09 onExhaust — 2 个**：`CharonsAshes, DeadBranch`（中央消耗通知链，draw-exhaust.md R14）。置信度：**高**
**R10 checkTrigger/onTrigger — Necronomicon（checkTrigger）、LizardTail/MeatOnTheBone（onTrigger）** — 出处同 R01。onTrigger 直调点：玩家 damage() 致死拦截链（death-arbitration.md R04）+ 各计数器逻辑。置信度：**高**

---

## 3. 受击 / 阈值 / 死亡 / 胜利

**R11 受击族** — 出处同 R01。置信度：**高**
```
onAttackToChangeDamage: Boot（守方改伤：Boot=第一次受击-4）
onAttacked:             Torii（1 伤及以下无效）, DEPRECATEDDodecahedron
wasHPLost:              CentennialPuzzle, EmotionChip, RunicCube, SelfFormingClay
onBloodied:             MeatOnTheBone, RedSkull（血量阈值沿，damage-pipeline.md R04 步骤⑬）
```
**R12 onMonsterDeath — 2 个**：`GremlinHorn, TheSpecimen`（怪物 die() 内同步直调，triggers.md §5.1）。
**R13 onVictory（战斗胜利链第一段）— 21 个** — 出处同 R01（triggers.md R10）。置信度：**高**
```
ArtOfWar, BlackBlood, BlackStar, BurningBlood, CaptainsWheel, CentennialPuzzle,
EmotionChip, FaceOfCleric, HornCleat, Kunai, LetterOpener, Orichalcum, OrnamentalFan,
Pocketwatch, RedSkull, Shuriken, SlaversCollar, StoneCalendar, VelvetChoker, Test6,
DEPRECATEDDodecahedron
```
**R14 onChangeStance — 1 个**：`VioletLotus`（stances.md R02）。
**R15 onEquip — 46 个**（获取时一次性效果/计数初始化；名单略，见 `../relics.json` 与扫描 JSON）。置信度：**高**

**R16 能量/规则位遗物的真实挂点勘误** — 出处 `javap -p` 定点复核。置信度：**高**
MarkOfPain（+1 能量/战）= **atBattleStart**；PhilosopherStone = atBattleStart + onSpawnMonster（逐怪给力量）；VelvetChoker 有 atTurnStart（计数复位）+ onVictory。**真正零战斗钩子**的规则位遗物：CoffeeDripper / RunicDome / Sozu / Ectoplasm / BustedCrown / FusionHammer（禁营地选项/隐藏意图/无药水/无金/牌组奖励-1/无能量回复——由引擎各消费点 `hasRelic` 查询建模）；CursedKey 的诅咒逻辑挂 `justEnteredRoom + onChestOpen`（非战斗钩子）。⇒ 移植时规则位 boss 遗物要按"引擎查询点"建模，不是钩子建模。

---

## 4. 开放问题 / 低置信项

1. onTrigger 的第三实现者（扫描计数 3 含基类 → LizardTail/MeatOnTheBone 之外无）：ChampionsBelt 等用 checkTrigger? 本扫描 checkTrigger 仅 Necronomicon——ChampionsBelt（Champion Belt? StS1 无此遗物）之类旧认知需以 `../relics.json` 为准。置信度：**中**。
2. onEquip 46 个名单未在本卷展开（数据在扫描 JSON）。
3. StS2 对应物（`Entities.Relics/` 的 Hook 接口族）待专卷扫描。
