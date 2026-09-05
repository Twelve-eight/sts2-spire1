# Power 生命周期矩阵（Power Lifecycle Matrix）— StS1 战斗语义知识库

## 本卷范围
对 jar 内**全部 161 个 power 类**（含 watcher/、deprecated/ 子包，`javap -p` 全量扫描 + 签名级正则归类，工具 `../scan-hooks.mjs`）按"谁在哪个钩子里干什么"建档：回合时点钩子（start-of-turn / end-of-turn / end-of-round / PreEndTurnCards / duringTurn）、伤害/格挡修改钩子、出牌/消耗/抽牌事件钩子、叠层定制（stackPower/reducePower）、justApplied 家族。每个成员清单即数据本体。
调用点的时序细节不重复展开，引用：`turn-phase.md`（R13 新回合块）、`triggers.md`（§6.2 回合尾三条链）、`death-arbitration.md`/`defense-powers.md`（防御钩子语义）。扫描方法与子串污染陷阱见 `../../kb/research-methods.md` M7/M8。

**图例**：置信度 **高**=javap 签名扫描直接可证（成员清单）/ **中**=方法体语义（注明）。基准 jar：desktop-1.0.jar v2.x。基类 `AbstractPower` 的空实现不计入成员（下列清单已剔除基类）。`DEPRECATED*` = 废弃不可获得。

---

## 1. 回合时点钩子

**R01 atStartOfTurn（所有者自身回合开始）— 27 个覆写** — 出处 `scan-hooks.mjs powers` 签名扫描。置信度：**高**
```
BerserkPower, BiasPower, ChokePower, CreativeAIPower, DEPRECATEDDisciplinePower,
EchoPower, FlameBarrierPower, FlightPower, HelloPower, InfiniteBladesPower,
InvinciblePower, LoopPower, MagnetismPower, MayhemPower, NextTurnBlockPower,
NightmarePower, PanachePower, PhantasmalPower, PoisonPower, RechargingCorePower,
TimeMazePower, BattleHymnPower, EndTurnDeathPower, EnergyDownPower,
ForesightPower, WrathNextTurnPower, WinterPower
```
要点：**毒(Poison) 在持有者自己回合开始结算**——怪身上的毒在 `MonsterStartTurnAction → applyStartOfTurnPowers`（turn-phase.md R10）触发，先于该怪 takeTurn ⇒ 毒杀会吞掉怪的本回合行动；玩家身上的毒在新回合块钩子梯死亡仲裁可救（death-arbitration.md R02，毒是 HP_LOSS）。Demon Form 的力量增长挂 **atStartOfTurnPostDraw**（见 R02）——"打完开局牌才涨"是错的，它是"抽牌动作入队后"时机。

**R02 atStartOfTurnPostDraw（抽牌动作入队后同步直调）— 7 个** — 出处同 R01（调用点陷阱 turn-phase.md R04：卡尚未到手）。置信度：**高**
```
BrutalityPower, DemonFormPower, DEPRECATEDEmotionalTurmoilPower,
DrawCardNextTurnPower, NoxiousFumesPower, ToolsOfTheTradePower, DevotionPower
```

**R03 atEndOfTurnPreEndTurnCards（回合尾、诅咒/状态自动打出窗口之前）— 3 个** — 出处同 R01。置信度：**高**
```
MetallicizePower, PlatedArmorPower, LikeWaterPower
```
仲裁要点：这三个的"回合尾格挡/护甲"**先于** Regret/Decay/Doubt 自动结算入队（turn-phase.md R07 步骤②），即**金属化格挡能挡住悔恨的伤害**。它们只实现 PreEndTurnCards，不实现普通 atEndOfTurn（M9 复核确认）。

**R04 atEndOfTurn(boolean)（回合尾通用钩子，玩家=按键链 A、怪物=怪物链 C）— 28 个** — 出处同 R01。置信度：**高**
```
AmplifyPower, BurstPower, CombustPower, ConstrictedPower, DEPRECATEDDisciplinePower,
DoubleTapPower, EntanglePower, EquilibriumPower, GainStrengthPower, IntangiblePower,
LoseDexterityPower, LoseStrengthPower, MalleablePower, NoDrawPower, RagePower,
ReboundPower, RegenerateMonsterPower, RegenPower, RetainCardPower, RitualPower,
TheBombPower, CannotChangeStancePower, EstablishmentPower, LiveForeverPower,
NoSkillsPower, OmegaPower, StudyPower, WraithFormPower
```

**R05 atEndOfRound（新回合块第 1 步，玩家与怪物 powers 都触发）— 21 个** — 出处同 R01（调用点 death-arbitration.md R17）。置信度：**高**
```
AttackBurnPower, BlurPower, ConservePower, DoubleDamagePower, DrawReductionPower,
DuplicationPower, EquilibriumPower, FrailPower, GenericStrengthUpPower, GrowthPower,
IntangiblePlayerPower, LockOnPower, MalleablePower, NoBlockPower, RitualPower,
SkillBurnPower, SlowPower, VulnerablePower, VaultPower, WaveOfTheHandPower, WeakPower
```
**玩家的脆弱/虚弱/佝偻时长在这里递减**（敌方回合结束后）⇒ 玩家 debuff 的"剩余回合"按"敌方回合经历数"计。IntangiblePlayer 到期也在此（渎神仲裁的时序基础）。

**R06 justApplied 双条件家族（施加当回合不吃首跳）— 9 个** — 出处 `scan-hooks.mjs`（字段存在性）+ `status-stacking.md` 的双条件语义。置信度：**高**
```
AttackBurnPower, DoubleDamagePower, DrawReductionPower, FrailPower, IntangiblePower,
NoBlockPower, SkillBurnPower, VulnerablePower, WeakPower
```
全部同时属于 R05 的 atEndOfRound 家族。玩家侧 debuff（弱/脆/无格挡/技能烧等）即"施加当回合生效整一回合、下一新回合块开始递减"的实现载体。

**R07 duringTurn（每帧轮询）— 2 个** — 出处同 R01（调用点 turn-phase.md R12 `applyTurnPowers`）。置信度：**高**
```
ExplosivePower, FadingPower
```

**R08 onEnergyRecharge（能量发放点：开局 GainEnergyAndEnableControlsAction + 每回合 PlayerTurnEffect 构造器）— 4 个** — 出处同 R01 + energy-cost.md R02/R03。置信度：**高**
```
CollectPower, EnergizedBluePower, EnergizedPower, DevaPower
```
（Energized"下回合+N 能量"在此发放并自移除。）

---

## 2. 双钩子 power（每回合触发两次，职责不同）

**R09 三对双实现** — 出处 `javap -c -p` 方法体（M9 复核）。置信度：**高**

| Power | atEndOfTurn(boolean) | atEndOfRound |
|---|---|---|
| EquilibriumPower | `isPlayer` 门：手牌非虚无牌置 `retain=true`（本效果） | `amount<=1 → Remove`（到期自移除） |
| RitualPower | `isPlayer` 门：怪侧 flash + `ApplyPowerAction(Strength, amount)`（叠层合并） | `onPlayer && skipFirst` 分支：玩家侧（日替场景）给力量，首跳豁免 |
| MalleablePower | 非 player 门：`amount = basePower` 复位 | 同样复位（双保险） |

⇒ 移植时不可把它们当成"同钩子写两遍"，三个都是"效果钩子 + 生命周期钩子"各司其职。

---

## 3. 伤害/格挡修改钩子的使用者

**R10 攻方 give 层 5 个 + 守方 receive 层 2 个 + final 层 4 个** — 出处同 R01（语义详见 damage-pipeline.md R06/R07 与 defense-powers.md）。置信度：**高**
```
atDamageGive:        DoubleDamagePower(×2), PenNibPower(×2), StrengthPower(+),
                     VigorPower(+下一击), WeakPower(×0.75)
atDamageReceive:     SlowPower(×1.5×剩余回合), VulnerablePower(×1.5)
atDamageFinalGive:   （无 vanilla 覆写——damage-pipeline.md 开放问题 1 维持）
atDamageFinalReceive: FlightPower, ForcefieldPower, IntangiblePlayerPower, IntangiblePower
modifyBlock:         DexterityPower(+), FrailPower(×0.75)
onAttackedToChangeDamage: BufferPower(归零), InvinciblePower(预算钳制)
onAttacked(反伤/反应): AngryPower, CurlUpPower, FlameBarrierPower, FlightPower,
                     MalleablePower, ReactivePower, ShiftingPower, StaticDischargePower,
                     ThornsPower, BlockReturnPower + DEPRECATED 三件
wasHPLost:           PlatedArmorPower(掉甲), RupturePower(失力得力)
```
注意 `DoubleDamagePower` 在 give 层 ×2（非 final 层）⇒ 与 PenNib 同层乘算（先力后双倍），仲裁时按 R06 damage-pipeline 步骤②③序代入。

---

## 4. 出牌/事件钩子与叠层定制

**R11 onUseCard 23 个 / onAfterUseCard 5 个** — 出处同 R01（四时刻对照见 triggers.md §3）。置信度：**高**
```
onUseCard: AfterImage, Amplify, Anger, AttackBurn, Burst, Choke, Corruption,
           Curiosity, DoubleTap, Duplication, Echo, Heatsink, Hex, Panache,
           PenNib, Rage, SharpHide, SkillBurn, Storm, FreeAttack, Vigor + 废弃2
onAfterUseCard: BeatOfDeath, Rebound, Slow, TimeMaze, TimeWarp
```
TimeWarp（"你每打出 12 张牌→获得额外回合"）挂 onAfterUseCard——计数在**效果动作全部执行完后**才 +1（时序仲裁点：同一次出牌中途被 TimeWarp 触发的判定在 onAfterUseCard 帧）。

**R12 叠层/移除定制** — 出处同 R01。置信度：**高**
```
stackPower 定制 34 个（合并语义非加法者重点：PlatedArmor 上限3、Poison/Regen 常规加、
  Mantra、Winter、WraithForm、LikeWater、Panache、Focus、Strength、Dexterity…）
reducePower 定制 5 个: DexterityPower, DrawPower, FocusPower, GainStrengthPower,
  StrengthPower（受负值下限保护）
onRemove: DrawPower, DrawReductionPower, FlightPower, PlatedArmorPower
onInitialApplication: DrawReductionPower;  onSpecificTrigger: ArtifactPower
onExhaust: DarkEmbracePower, FeelNoPainPower
onCardDraw: ConfusionPower, CorruptionPower, EvolvePower, FireBreathingPower
onScry: NirvanaPower;  onChangeStance: MentalFortressPower, RushdownPower
```

---

## 5. 开放问题 / 低置信项

1. 各成员的**方法体细节**不在本卷（本卷只登记"谁在哪个钩子"）；需要语义时按类名 javap 定点读。
2. `priority` 字段值（UI 排序）未纳入扫描（对仲裁无影响，triggers.md §8 注 2 已证）。
3. deprecated 子包 15 个的清单已含但未逐一标废弃语义。
4. StS2 对应物：`PowerModel` 的钩子面与 `SkipNextDurationTick`（kb/sts2-combat-semantics.md S10/S11），StS2 power 全量扫描待做。
