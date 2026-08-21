# Silent UNCOMMON cards, part B (16) — StS1 vanilla

Every number below was extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. The `StS2 name collision` field decides the localization title prefix.

## Finisher  (class name = `Finisher`, loc key `SPIRE1-FINISHER`)
- StS1 id: `Finisher`, official name: `Finisher`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base values: Damage=6, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official StS1 description: `Deal !D! damage for each Attack played this turn.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Finisher"
- IMPL: Attack cost 1: deal 6 damage (+2) for EACH Attack played this turn (the card hits that many times). Attack-count query (exact pattern found in the game's own Finisher.cs): CombatManager.Instance.History.CardPlaysFinished.Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Type == CardType.Attack && e.CardPlay.Player == Owner). Use hitCount: count (minimum 1 hit only if vanilla does so — vanilla deals damage once per Attack played, so if count is 0 the card deals no damage).

## Flechettes  (class name = `Flechettes`, loc key `SPIRE1-FLECHETTES`)
- StS1 id: `Flechettes`, official name: `Flechettes`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base values: Damage=4, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official StS1 description: `Deal !D! damage for each Skill in your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Flechettes"
- IMPL: Attack cost 1: deal 4 damage (+2) for each SKILL in your hand (count the skills in hand at play time, excluding this card since it is an Attack). hitCount = number of Skills in PileType.Hand.GetPile(Owner).Cards.

## Footwork  (class name = `Footwork`, loc key `SPIRE1-FOOTWORK`)
- StS1 id: `Footwork`, official name: `Footwork`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base values: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Gain !M! Dexterity.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Footwork"
- IMPL: Power cost 1: gain 2 Dexterity (3 upgraded) using the existing game DexterityPower: PowerVar<DexterityPower>(2) + CommonActions.ApplySelf<DexterityPower>(choiceContext, this).

## HeelHook  (class name = `HeelHook`, loc key `SPIRE1-HEEL_HOOK`)
- StS1 id: `Heel Hook`, official name: `Heel Hook`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base values: Damage=5, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official StS1 description: `Deal !D! damage. NL If the enemy has Weak, NL gain [G] and NL draw 1 card.`
- StS2 name collision: no -> plain title "Heel Hook"
- IMPL: Attack cost 1, 5 damage (+3): if the enemy has Weak, gain 1 Energy AND draw 1 card. Check play.Target!.HasPower<WeakPower>() after the attack; then PlayerCmd.GainEnergy(1, Owner) and CommonActions.Draw(this, choiceContext). Vars: DamageVar(5), EnergyVar(1), CardsVar(1).

## InfiniteBlades  (class name = `InfiniteBlades`, loc key `SPIRE1-INFINITE_BLADES`)
- StS1 id: `Infinite Blades`, official name: `Infinite Blades`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base values: none, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official StS1 description: `At the start of your turn, add a *Shiv into your hand.`
- official upgraded description: `Innate. NL At the start of your turn, add a *Shiv into your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Infinite Blades"
- IMPL: Power cost 1: at the start of your turn add a Shiv into your hand; upgraded ALSO becomes Innate. REUSE the game power InfiniteBladesPower (it already creates Amount StS2 Shivs at hand draw, numerically identical to StS1): PowerVar<InfiniteBladesPower>(1) + CommonActions.ApplySelf<InfiniteBladesPower>(choiceContext, this). OnUpgrade adds CardKeyword.Innate (mod pattern: AddKeyword(CardKeyword.Innate)).

## LegSweep  (class name = `LegSweep`, loc key `SPIRE1-LEG_SWEEP`)
- StS1 id: `Leg Sweep`, official name: `Leg Sweep`
- type=SKILL, rarity=UNCOMMON, cost=2, target=ENEMY, base values: Block=11, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1, upgradeBlock=3
- official StS1 description: `Apply !M! Weak. NL Gain !B! Block.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Leg Sweep"
- IMPL: Skill cost 2, target enemy: apply 2 Weak (3 upgraded) and gain 11 Block (14 upgraded).

## MasterfulStab  (class name = `MasterfulStab`, loc key `SPIRE1-MASTERFUL_STAB`)
- StS1 id: `Masterful Stab`, official name: `Masterful Stab`
- type=ATTACK, rarity=UNCOMMON, cost=0, target=ENEMY, base values: Damage=12, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Costs 1 additional [G] NL for each time you lose HP this combat. NL Deal !D! damage.`
- StS2 name collision: no -> plain title "Masterful Stab"
- IMPL: Attack base cost 0, 12 damage (+4), 'costs 1 additional Energy for each time you lose HP this combat'. Mod precedent for HP-triggered dynamic cost: Cards/BloodForBlood.cs uses AfterCurrentHpChanged(Creature, decimal delta) + EnergyCost.AddThisCombat(...). Increase the cost by 1 per HP-loss event on the owner this combat.

## NoxiousFumes  (class name = `NoxiousFumes`, loc key `SPIRE1-NOXIOUS_FUMES`)
- StS1 id: `Noxious Fumes`, official name: `Noxious Fumes`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base values: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `At the start of your turn, apply !M! Poison to ALL enemies.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Noxious Fumes"
- IMPL: Power cost 1: at the start of your turn apply 2 Poison (3 upgraded) to ALL enemies. REUSE the game power NoxiousFumesPower (verified: applies Amount Poison to all hittable enemies at turn start): PowerVar<NoxiousFumesPower>(2) + ApplySelf.

## Predator  (class name = `Predator`, loc key `SPIRE1-PREDATOR`)
- StS1 id: `Predator`, official name: `Predator`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base values: Damage=15, StS1 flags: none
- upgrade deltas: upgradeDamage=5
- official StS1 description: `Deal !D! damage. NL Next turn, draw 2 additional cards.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Predator"
- IMPL: Attack cost 2, 15 damage (+5): next turn draw 2 ADDITIONAL cards via the game power DrawCardsNextTurnPower — `await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);`. Vars: DamageVar(15), CardsVar(2) (Cards NOT upgraded).

## Reflex  (class name = `Reflex`, loc key `SPIRE1-REFLEX`)
- StS1 id: `Reflex`, official name: `Reflex`
- type=SKILL, rarity=UNCOMMON, cost=-2, target=NONE, base values: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Unplayable. NL If this card is discarded from your hand, draw !M! cards.`
- official upgraded description: `Unplayable. NL If this card is discarded from your hand, draw !M! cards.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Reflex"
- IMPL: Skill, UNPLAYABLE (cost -2 in StS1 means unplayable): 'If this card is discarded from your hand, draw 2 cards' (3 upgraded). Requires a discarded-from-hand hook on CardModel. Search the decompiled CardModel.cs and the CardKeyword.Sly semantics. If a per-card on-discard hook exists, use it; otherwise FLAG the card as infeasible rather than shipping a version that never triggers.

## RiddleWithHoles  (class name = `RiddleWithHoles`, loc key `SPIRE1-RIDDLE_WITH_HOLES`)
- StS1 id: `Riddle With Holes`, official name: `Riddle with Holes`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base values: Damage=3, StS1 flags: none
- upgrade deltas: upgradeDamage=1
- official StS1 description: `Deal !D! damage 5 times.`
- StS2 name collision: no -> plain title "Riddle with Holes"
- IMPL: Attack cost 2: deal 3 damage (+1) 5 TIMES — hitCount: 5.

## Setup  (class name = `Setup`, loc key `SPIRE1-SETUP`)
- StS1 id: `Setup`, official name: `Setup`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base values: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official StS1 description: `Put a card from your hand on top of your draw pile. NL It costs 0 until played.`
- StS2 name collision: no -> plain title "Setup"
- IMPL: Skill cost 1: put a card from your hand on TOP of your draw pile; it costs 0 until played. Needs card selection from hand + move to draw pile top (CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top)) + a persistent cost override on that card. Verify the cost-override API; if the 'costs 0 until played' part cannot be expressed, FLAG it (do not ship the move-only version silently).

## Skewer  (class name = `Skewer`, loc key `SPIRE1-SKEWER`)
- StS1 id: `Skewer`, official name: `Skewer`
- type=ATTACK, rarity=UNCOMMON, cost=-1, target=ENEMY, base values: Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official StS1 description: `Deal !D! damage X times.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Skewer"
- IMPL: Attack, X-COST: deal 7 damage (+3) X times. Base ctor cost -1 with `protected override bool HasEnergyCostX => true;` and use ResolveEnergyXValue() as hitCount. Copy the exact X-cost pattern from the mod's Cards/Whirlwind.cs.

## Tactician  (class name = `Tactician`, loc key `SPIRE1-TACTICIAN`)
- StS1 id: `Tactician`, official name: `Tactician`
- type=SKILL, rarity=UNCOMMON, cost=-2, target=NONE, base values: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Unplayable. NL If this card is discarded from your hand, gain [G].`
- official upgraded description: `Unplayable. NL If this card is discarded from your hand, gain [G] [G].`
- StS2 name collision: YES -> localization title MUST be "StS1 - Tactician"
- IMPL: Skill, UNPLAYABLE: 'If this card is discarded from your hand, gain 1 Energy' (2 upgraded). Same discard-hook requirement as Reflex — reuse whatever hook Reflex uses; FLAG if none exists.

## Terror  (class name = `Terror`, loc key `SPIRE1-TERROR`)
- StS1 id: `Terror`, official name: `Terror`
- type=SKILL, rarity=UNCOMMON, cost=1, target=ENEMY, base values: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=0
- official StS1 description: `Apply 99 Vulnerable. NL Exhaust.`
- StS2 name collision: no -> plain title "Terror"
- IMPL: Skill cost 1, Exhaust, target enemy: apply 99 Vulnerable (upgrade reduces cost to 0 via EnergyCost.UpgradeBy(-1)). Var: PowerVar<VulnerablePower>(99).

## WellLaidPlans  (class name = `WellLaidPlans`, loc key `SPIRE1-WELL_LAID_PLANS`)
- StS1 id: `Well Laid Plans`, official name: `Well-Laid Plans`
- type=POWER, rarity=UNCOMMON, cost=1, target=NONE, base values: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `At the end of your turn, Retain up to !M! card.`
- official upgraded description: `At the end of your turn, Retain up to !M! cards.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Well-Laid Plans"
- IMPL: Power cost 1: at the end of your turn, Retain up to 1 card (2 upgraded). CardKeyword.Retain EXISTS in StS2. Needs a custom power (Spire1Code/Powers/WellLaidPlansPower.cs) that at end of turn lets the player retain up to Amount cards, or applies Retain to up to Amount cards in hand. If no retain-application API exists, FLAG it.
