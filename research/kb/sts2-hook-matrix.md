# StS2 钩子实现矩阵（Hook Implementation Matrix, EA build）— sts2-spire1 知识库

## 本卷范围
StS2 全部战斗/战役钩子的**实现者清单**：`Hooks/Hook.cs` 的 71 个分发点里 62 个有覆写者。数据由 `scan-sts2-hooks.mjs` 扫描 `Models*/Powers/Relics/Cards/Monsters/Orbs/Enchantments/Potions` 生成（`override ... Task <Hook>` 签名匹配），机器可读版 `sts2-hook-matrix.json`（与本卷同目录提交）。
**架构事实（区别于 StS1 的最重要一点）**：分发是"遍历全部模型 + 调虚方法"（`runState.IterateHookListeners` / `IterateCombatHookListeners`），**没有 relics/powers 容器之分、没有插入顺序仲裁**——同钩子的先后由基类的**早/晚相位变体钩子**（`...Early` / `...Late`）显式表达。StS1 的"同容器按获得顺序"（triggers.md R15）在 StS2 不存在，移植时需把依赖顺序的 StS1 遗物/能力映射到对应变体。
**图例**：**高**=源码签名扫描直接可证。`Powers/` `Relics/` `Cards/` `Monsters/` 为目录前缀。

---

## 1. 早/晚相位变体清单（时序仲裁核心）

**M-A 存在的变体钩子（14 个）** — 出处 Hook.cs + 扫描。置信度：**高**
```
BeforeCombatStartLate            AfterCombatVictoryEarly
AfterSideTurnStartLate           AfterPlayerTurnStartLate
AfterCardDrawnEarly              AfterCardPlayedLate
AfterCardChangedPilesLate        AfterEnergyResetLate
AfterAutoPrePlayPhaseEnteredEarly / Late
AfterAutoPrePlayPhaseEntered (本体)  AfterAutoPostPlayPhaseEntered (本体)
```
实现者样例：`BeforeCombatStartLate` = PetrifiedToad；`AfterPlayerTurnStartLate` = BloodVial/FakeBloodVial；`AfterCardDrawnEarly` = HellraiserPower；`AfterCardPlayedLate` = MakeItSo/RightHandHand；`AfterEnergyResetLate` = BoundPhylactery；`AfterCombatVictoryEarly` = MeatOnTheBone（StS1 同名遗物挂"较早"位，与 StS1 onVictory 首段对应）。

## 2. 出牌族

**M-B BeforeCardPlayed（28）/ AfterCardPlayed（58）** — 出处扫描。置信度：**高**
```
BeforeCardPlayed: Cards/Stomp; Powers/Afterimage, Calamity, ChainsOfBinding,
  DanseMacabre, FreeAttack, FreePower, FreeSkill, Gravity, ImitationLearning,
  Juggling, Monologue, Oblivion, Rupture, SerpentForm, Sloth, SpiritOfAsh, Storm,
  Strangle, Subroutine, Surrounded, TheSealedThrone, Veilpiercer;
  Relics/ChemicalX, IntimidatingHelmet, MusicBox, PaelsEye, PenNib
AfterCardPlayed(58): Cards/BansheesCry, Pinpoint; Powers/Afterimage, BlackHole,
  Calamity, CurlUp, DevourLife, EchoForm, Enrage, Galvanic, Gravity, Haunt,
  ImitationLearning, MasterPlanner, Monologue, Oblivion, PaleBlueDot, Panache,
  Rage, Rupture, SerpentForm, Slow, Smoggy, Sneaky, Storm, Strangle, Subroutine,
  Tender, VitalSpark, VoidForm, WitheringPresence; Relics/ArtOfWar, BrilliantScarf,
  DaughterOfTheWind, GamePiece, HelicalDart, IronClub, IvoryTile, Kunai,
  Kusarigama, LetterOpener, LostWisp, MummifiedHand, MusicBox, Nunchaku,
  OrnamentalFan, PaelsLegion, PenNib, Permafrost, Pocketwatch, RainbowRing,
  RazorTooth, RippleBasin, Shuriken, TuningFork, UnsettlingLamp, Vambrace,
  VelvetChoker
```
仲裁：免费系（FreeAttack/FreePower/FreeSkill）挂 **Before** 阶段（改 ResourceInfo/费用判定先行）；计数遗物族（Kunai/Shuriken/LetterOpener/OrnamentalFan/Nunchaku/PenNib/VelvetChoker/Pocketwatch/ArtOfWar）在 **After** 阶段计数——与 StS1"计数在 UseCardAction 构造期"（triggers.md R14）时点不同，StS2 计数在效果执行完成后（含 playCount 循环每遍一次，C03）。

## 3. 回合族

**M-C AfterSideTurnStart（48）/ AfterPlayerTurnStart（20）** — 出处扫描。置信度：**高**
```
AfterSideTurnStart: Powers/BiasedCognition, Blur, Clarity, Coolant, Countdown,
  DemonForm, DrawCardsNextTurn, Feral, Furnace, Neurosurge, NoxiousFumes, Plating,
  Poison, PrepTime, Rampart, Reflect, ShadowStep, Slow, WraithForm;
  Relics/Akabeko, BigHat, BoomingConch, Bread, Brimstone, Candelabra, Chandelier,
  Crossbow, DiamondDiadem, DivineDestiny, FakeHappyFlower, FencingManual,
  HappyFlower, InfusedCore, Lantern, LetterOpener, OrangeDough, PaelsEye,
  PaelsFlesh, PaelsLegion, PaelsTears, PhylacteryUnbound, Pocketwatch,
  RunicCapacitor, Sai, SealOfGold, StoneCalendar, SymbioticVirus, VeryHotCocoa
AfterPlayerTurnStart: Powers/CrimsonMantle, Entropy, Hibernate, Inferno, Loop,
  RollingBoulder, SummonNextTurn, ToolsOfTheTrade, Tyranny;
  Relics/Bellows, BoneTea, ChoicesParadox, EmotionChip, FestivePopper, GamblingChip,
  MercuryHourglass, MrStruggles, RoyalPoison, ToastyMittens, VexingPuzzlebox
```
注意 **Poison/DemonForm 等 StS1 同名机制的钩子面不同**：StS1 毒挂 atStartOfTurn（power-lifecycle.md R01），StS2 毒挂 `AfterSideTurnStart`（侧开始而非个体开始）；BiasedCognition 在 StS2 是 Power（StS1 是遗物）。Stance 侧：`AfterSideTurnStart` 是**侧级**钩子（双方共），玩家个体另有 `AfterPlayerTurnStart`。

## 4. 抽牌/弃牌/消耗/洗牌族

**M-D** — 出处扫描。置信度：**高**
```
BeforeHandDraw(18): Cards/Bolas, ThrummingHatchet; Powers/CallOfTheVoid,
  CreativeAi, ForegoneConclusion, HelloWorld, InfiniteBlades, Nightmare,
  SentryMode, SpectrumShift; Relics/BlessedAntler, FuneraryMask, JeweledMask,
  NinjaScroll, Pendulum, PollinousCore, RadiantPearl, Toolbox
AfterCardDrawn(11): Cards/KinglyKick, KinglyPunch, Void; Powers/Automation,
  Cacophony, ChainsOfBinding, Confused, CorrosiveWave, Iteration, Pagestorm, Speedster
AfterCardDiscarded(2): Relics/Tingsha, ToughBandages
AfterCardExhausted(8): Cards/DrumOfBattle, Midnight; Powers/DarkEmbrace,
  FeelNoPain; Relics/BurningSticks, CharonsAshes, ForgottenSoul, JossPaper
AfterShuffle(3): Powers/Stratagem; Relics/BiiigHug, TheAbacus
AfterHandEmptied(1): Relics/UnceasingTop
```
**StS1 对照**：StS1 的 `triggerWhenDrawn` 在进手牌前（draw-exhaust.md R05）；StS2 `AfterCardDrawn` 在进手后（S14）；Void 在两侧都存在但挂点时点不同。

## 5. 伤害/死亡/资源族

**M-E** — 出处扫描。置信度：**高**
```
BeforeAttack(3): Powers/Gigantification, Hellraiser, Vigor
AfterAttack(7): Cards/Flatten; Powers/Gigantification, PainfulStabs, Skittish,
  Suck, Vigor; Relics/BoneFlute
BeforeDamageReceived(1): Powers/ThornsPower（Thorns 在 StS2 是 before-received 钩子）
AfterDamageGiven(8): Powers/Concoct, Envenom, Imbalanced, MonarchsGaze, PaperCuts,
  ReaperForm, SicEm, Underworld
AfterDamageReceived(21): Monsters/LagavulinMatriarch; Powers/Asleep, CurlUp,
  FlameBarrier, Flutter, HardenedShell, Inferno, PersonalHive, Plow, Reflect,
  Rupture, Shriek, Slippery, Slumber, TheGambit; Relics/BeatingRemnant,
  CentennialPuzzle, DemonTongue, EmotionChip, LavaLamp, SelfFormingClay
BeforeDeath(4): Monsters/Crusher, Rocket; Powers/Heist, Swipe
AfterCurrentHpChanged(5): Monsters/Crusher, Rocket; Powers/NecroMastery;
  Relics/MeatOnTheBone, RedSkull
AfterEnergyReset(9)/AfterEnergySpent(1): Powers/EnergyNextTurn, Genesis,
  LightningRod, Radiance, Spinner; Relics/ArtOfWar, FakeVenerableTeaSet,
  VenerableTeaSet / Powers/Orbit
AfterStarsGained/Spent(1/3): Powers/BlackHole / Powers/ChildOfTheStars;
  Relics/GalacticDust, MiniRegent
```

## 6. 战役/杂项族

**M-F** — 出处扫描。置信度：**高**
```
AfterRoomEntered(33): （进入房间触发的属性类遗物全集，名单见 JSON）
BeforeCombatStart(21): Relics/Anchor, BeltBuckle, BoundPhylactery, Byrdpip,
  DelicateFrond, FakeAnchor, FakeSneckoEye, FurCoat, Kusarigama, LetterOpener,
  MeatOnTheBone, PaelsFlesh, PaelsLegion, Pantograph, PhylacteryUnbound,
  SneckoEye, TeaOfDiscourtesy, UnsettlingLamp, Vambrace; Powers/Galvanic, VitalSpark
AfterCombatEnd(45): 计数归零/清理族（名单见 JSON）
AfterCombatVictory(5): Relics/BeltBuckle, BlackBlood, BurningBlood, SwordOfStone,
  WarHammer
AfterFlush(1): Relics/Bookmark（Flush 机制对应物，turn-machine 卷 T05）
AfterPotionUsed/Procured/Discarded: BeltBuckle, ReptileTrinket
AfterPowerAmountChanged(11): Powers/PossessSpeed, PossessStrength, Sandpit,
  Shroud, SleightOfFlesh, SwordSage, TemporaryDexterity, TemporaryFocus,
  TemporaryStrength, Vicious, VitalSpark
AfterRestSiteHeal(2): RegalPillow, StoneHumidifier; AfterItemPurchased: MawBank
AfterOrbChanneled/Evoked: Metronome / ThunderPower
AfterTakingExtraTurn(2): Powers/Ambergris; Relics/PaelsEye
```
无实现者的 9 个钩子（71-62）：多为预览/胜利细化点，清单见 JSON 键差。

## 7. 开放问题 / 低置信项

1. 同类多实现的**方法体内侧序**（早/晚变体之外，同变体内仍按 IterateHookListeners 的模型遍历序——该序的确定规则未取证）。置信度：**中**。
2. `AfterPowerAmountChanged` 只 11 个实现者——StS1 依赖 power 增删联动的逻辑移植面广，需专卷核对。
3. 扫描只覆盖 Models/Entities 命名空间；`Achievements` 等旁路实现者未扫。
