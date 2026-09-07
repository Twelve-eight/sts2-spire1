# StS1 -> StS2 Porting Cookbook - sts2-spire1 Knowledge Base

## Scope of This Volume
The API mapping master table for porting StS1 content into StS2 (BaseLib). Grounded in this repo's own ported code (mod/Spire1Code, 300+ cards, 30+ powers, 60+ monsters) - every mapping cites a live example file. StS1 semantics anchors: ../sts1-kb/mechanics/ volumes. **Legend**: **High** = proven in our codebase / **Medium** = engine-readable but unused by us (noted).

## 1. Cards

**C01 use() -> OnPlay** - StS1 `AbstractCard#use(AbstractPlayer, List<AbstractMonster>)` -> StS2 `Spire1Card.OnPlay(PlayerChoiceContext, CardPlay)` (async). Example: Cards/Accuracy.cs L17-18. Confidence: **High**
No sync play path: every card body is `async Task`; awaits sequence = StS1 addToBottom order (the queue becomes the await chain). addToTop semantics = await BEFORE the rest of the body.

**C02 card declaration** - StS1 constructor (cost, type, rarity, target) -> C# primary-constructor base call `Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)` + `[Pool(typeof(...))]` attribute (Cards/Accuracy.cs L12-13). Confidence: **High**
[Pool] resolves through the base-class chain (Inherited=true); pool-audit.mjs replicates that resolution - missing attribute silently inherits the parent pool (GA incident).

**C03 magic numbers -> DynamicVars** - StS1 `magicNumber`/`baseMagicNumber` -> `CanonicalVars => [new PowerVar<T>(4)]`, read/write via `DynamicVars.Power<T>()`, upgrade via `.UpgradeValueBy(2m)` (Accuracy.cs L15/L20). Confidence: **High**
Var types: PowerVar<T>, BlockVar, DamageVar, MagicVar, CardsVar, HpLossVar - the audit parser's whitelist mirrors this set (audit-card-fidelity known artifact: multi-variable cards take magic from others[0]).

**C04 upgrade() -> OnUpgrade()** - StS1 `upgrade()` mutation block -> `protected override void OnUpgrade()` mutating DynamicVars (Accuracy.cs L20). Confidence: **High**

## 2. Powers

**C05 power hooks -> virtual methods** - StS1 `onAttacked`/`atStartOfTurn`/`onUseCard`... -> StS2 CustomPowerModel virtuals: `AfterDamageReceived`, turn-start family, play hooks. Example: Powers/AngryPower.cs L38 (StS1 onAttacked -> AfterDamageReceived + gate re-derivation). Confidence: **High**
Gate translation is per-hook: StS1 `damageAmount > 0 && type != HP_LOSS && != THORNS` becomes `props.IsPoweredAttack() && result.UnblockedDamage > 0` (AngryPower.cs doc comment L16-25 documents the derivation - required practice for every ported power).

**C06 applyPower -> PowerCmd.Apply** - StS1 `AbstractDungeon.actionManager.addToBottom(new ApplyPowerAction(...))` -> `await PowerCmd.Apply<T>(choiceContext, target, amount, source, cardSource)` (AngryPower.cs L46). Confidence: **High**

**C07 power registration/localization** - CustomPowerModel + `Localization => new PowerLoc(id, desc, tip)` with `#` marker + `{Amount}` interpolation (AngryPower.cs L32-36). Confidence: **High**

## 3. Monsters

**C08 monster class shape** - StS1 ctor (setHp/damage arrays/takeTurn/getMove) -> `Spire1Monster` subclass: `MinInitialHp/MaxInitialHp`, `GenerateMoveStateMachine()` (MoveState graph with FollowUpState edges), async move lambdas. Example: Monsters/Cultist.cs L24-72. Confidence: **High**
MoveState + FollowUpState models StS1 getMove's if/else history logic declaratively; firstMove flags become the initial-state choice.

**C09 ascension gates -> AscensionHelper** - StS1 `ascensionLevel >= N` tiers (mechanics/ascension.md A02-A19 gate law: normals 2/7/17, elites 3/8/18, bosses 4/9/19) -> `AscensionHelper.GetValueIfAscension(AscensionLevel.<Tier>, gated, base)` with tier enum names (DeadlyEnemies/ToughEnemies/...). Example: Cultist.cs L27-35 (A7 HP + A2 ritual). Confidence: **High**
Mapping table (StS1 level -> StS2 AscensionLevel): 2/7/17 normals, 3/8/18 elites, 4/9/19 bosses map to the Deadly/Tough/ChallengingMoves families; the enum member used in our code is the ported source of truth (grep AscensionHelper in mod/ for the roster).

**C10 animations** - StS1 `addToBottom(new AnimateSlowAttackAction(m))` etc. -> inline awaits: `await CreatureCmd.TriggerAnim(Creature, "Attack", 0.2f)`, `await Cmd.CustomScaledWait(...)`; donor-asset monsters need `SetupSkins` override (Cultist.cs L41-48, DampCultist donor note). Confidence: **High**

**C11 donor monsters** - `protected override string DonorId => "damp_cultist"` borrows a shipped monster's scene/anim rig (Cultist.cs L37). Donor must exist in engine assets; skin composite needs SetupSkins when the donor overrides it. Confidence: **High**

## 4. Damage / Block / Energy

**C12 damage action -> DamageCmd builder** - StS1 `new DamageAction(target, info, effect)` -> `await DamageCmd.Attack(dmg).FromMonster(this).WithAttackerAnim(...).WithHitFx(...).Execute(null)` (Cultist.cs L69-71). Confidence: **High**

**C13 relics** - StS1 relic hooks -> BaseLib Hook family (CustomRelicModel + Hook overrides). Roster: research/kb/sts2-hook-matrix.md (62/71 implementer table). Confidence: **High** (matrix) / individual hooks cited there.

## 5. Orbs / Stances / X-cost (engine-gap classes)

**C14 X-cost -> CapturedXValue** - StS1 X-cost (`cost == -1`, `energyOnUse`) -> StS2 `CapturedXValue` captured at play time. R8 relic-layer scope decision pending; card-side precedent in mod/Spire1Code (grep CapturedXValue). Confidence: **Medium** (engine mechanism; our card usage is the reference).

**C15 stances** - NO StS2 stance API (engine gap; pool-architecture StS2 diff table). Watcher stance behaviors must be modeled as powers or explicit state machines; multiplier arbitration per damage-pipeline R06 layering. Confidence: **High** (gap confirmed)

**C16 orbs** - StS2 has its own orbs/enchantments (sts2-orbs-enchantments.md O01-O08); StS1 Defect orbs map to that system with different slot semantics (Fission R12 case: RemoveSlots+AddSlots for clear-keep-slots, OrbCmd.EvokeNext x n for evoke-all). Confidence: **High**

## 6. Events / Rewards

**C17 event card grants -> CreateForReward** - StS1 `AbstractDungeon.effectsQueue.add(new ShowCardAndObtainEffect(...))` -> combat-side CreateCard + pile commands; reward-side CreateForReward. Confidence: **Medium** (pattern from Events/*.cs; not itemized here).

**C18 shop/relic pools** - stock membership via pool registration (C02); pricing is slot-local in StS2 (sts2-merchant.md M02-M04) - StS1 pricing formulas (loot-rewards L14) do NOT carry over. Confidence: **High**

## 7. Porting Checklist (condensed from incidents)

1. [Pool] attribute present? (pool-audit.mjs must stay 0 orphans)
2. Power gates re-derived with the StS2 predicate form and documented in the doc comment (AngryPower pattern C05)?
3. Ascension tiers via AscensionHelper with the right tier enum (C09)?
4. Localization via PowerLoc/MonsterLoc/SimpleLoc with markers (#, {Amount}, -x-+y+ swap) - no raw string interpolation drift?
5. Semantic review: cross-pool pick features get set-operation specs (G1); mechanical gates green (G3) before merge.
6. Fidelity audit: node tools/audit-card-fidelity.mjs --scope=all; monster-HP audit for stat ports.

## 8. Open Questions / Low-Confidence Items

1. C13/C14/C17 deserve per-hook appendices when next needed; this volume is the index + the load-bearing mappings.
2. Multiplayer constraint surface for ported cards (PlayerChoiceContext semantics) not itemized - see invariants I4/I12 for the incident record. Confidence: **Medium**.
