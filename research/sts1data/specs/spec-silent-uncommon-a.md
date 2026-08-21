# Silent UNCOMMON cards, part A (17) — StS1 vanilla

Every number below was extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. The `StS2 name collision` field decides the localization title prefix.

## Accuracy  (class name = `Accuracy`, loc key `SPIRE1-ACCURACY`)
- StS1 id: `Accuracy`, official name: `Accuracy`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base values: MagicNumber=4, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=2
- official StS1 description: `*Shivs deal !M! additional damage.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Accuracy"
- IMPL: Power. Shivs deal 4 additional damage (6 upgraded). Needs a custom power that increases the damage of played Shiv cards. Search the decompiled powers for an existing StS2 accuracy/shiv-damage power FIRST; StS2's Shiv has CardTag.Shiv, so a power hook that modifies card damage for cards with CardTag.Shiv is the target. If no damage-modifying power hook exists, FLAG this card as infeasible and do not ship a fake version.

## AllOutAttack  (class name = `AllOutAttack`, loc key `SPIRE1-ALL_OUT_ATTACK`)
- StS1 id: `All Out Attack`, official name: `All-Out Attack`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ALL_ENEMY, base values: Damage=10, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Deal !D! damage to ALL enemies. NL Discard 1 card at random.`
- StS2 name collision: no -> plain title "All-Out Attack"
- IMPL: Attack, 10 damage to ALL enemies (14 upgraded), then discard 1 card AT RANDOM from hand. Random discard: pick a random card from PileType.Hand.GetPile(Owner).Cards using the run/combat Rng available in the decompiled sources (verify; if only a deterministic API exists, use the combat Rng, never System.Random).

## Backstab  (class name = `Backstab`, loc key `SPIRE1-BACKSTAB`)
- StS1 id: `Backstab`, official name: `Backstab`
- type=ATTACK, rarity=UNCOMMON, cost=0, target=ENEMY, base values: Damage=11, StS1 flags: isInnate, exhaust
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Innate. NL Deal !D! damage. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Backstab"
- IMPL: Attack 11 (+4), cost 0, keywords Innate AND Exhaust (CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust]).

## Blur  (class name = `Blur`, loc key `SPIRE1-BLUR`)
- StS1 id: `Blur`, official name: `Blur`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base values: Block=5, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official StS1 description: `Gain !B! Block. NL Block is not removed at the start of your next turn.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Blur"
- IMPL: Skill, gain 5 Block (8 upgraded); Block is NOT removed at the start of your next turn. The mod already has BarricadePower (Ironclad Barricade, permanent). Blur is one-turn: write Spire1Code/Powers/BlurPower.cs : CustomPowerModel (Buff, Counter) that prevents block loss at turn start and then decrements/removes itself; read the mod's BarricadePower.cs to copy the exact block-retention hook.

## BouncingFlask  (class name = `BouncingFlask`, loc key `SPIRE1-BOUNCING_FLASK`)
- StS1 id: `Bouncing Flask`, official name: `Bouncing Flask`
- type=SKILL, rarity=UNCOMMON, cost=2, target=ALL_ENEMY, base values: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Apply 3 Poison to a random enemy !M! times.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Bouncing Flask"
- IMPL: Skill, cost 2. Apply 3 Poison to a RANDOM enemy, 3 times (4 times upgraded). Vars: PowerVar<PoisonPower>(3) for the fixed 3 Poison and a separate count var (RepeatVar) 3 -> 4 on upgrade. Loop count times, each time picking a random hittable enemy with the combat Rng.

## CalculatedGamble  (class name = `CalculatedGamble`, loc key `SPIRE1-CALCULATED_GAMBLE`)
- StS1 id: `Calculated Gamble`, official name: `Calculated Gamble`
- type=SKILL, rarity=UNCOMMON, cost=0, target=NONE, base values: none, StS1 flags: exhaust
- upgrade deltas: flag:exhaust=false
- official StS1 description: `Discard your hand, NL then draw that many cards. NL Exhaust.`
- official upgraded description: `Discard your hand, NL then draw that many cards.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Calculated Gamble"
- IMPL: Skill cost 0, Exhaust (upgrade REMOVES Exhaust: OnUpgrade must clear the keyword — verify how the mod removes a keyword; if keywords cannot be removed after construction, express Exhaust via an IsUpgraded-dependent CanonicalKeywords override). Effect: discard your whole hand, then draw that many cards. Whole-hand discard idiom: `await CardCmd.Discard(choiceContext, PileType.Hand.GetPile(Owner).Cards.ToList())` (materialise the list first), count the cards BEFORE discarding, then draw that many.

## Caltrops  (class name = `Caltrops`, loc key `SPIRE1-CALTROPS`)
- StS1 id: `Caltrops`, official name: `Caltrops`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base values: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=2
- official StS1 description: `Whenever you are attacked, deal !M! damage back.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Caltrops"
- IMPL: Power. Gain 3 Thorns (5 upgraded) using the existing game power: PowerVar<ThornsPower>(3) + CommonActions.ApplySelf<ThornsPower>(choiceContext, this).

## Catalyst  (class name = `Catalyst`, loc key `SPIRE1-CATALYST`)
- StS1 id: `Catalyst`, official name: `Catalyst`
- type=SKILL, rarity=UNCOMMON, cost=1, target=ENEMY, base values: none, StS1 flags: exhaust
- upgrade deltas: none
- official StS1 description: `Double the enemy's Poison. NL Exhaust.`
- official upgraded description: `Triple the enemy's Poison. NL Exhaust.`
- StS2 name collision: no -> plain title "Catalyst"
- IMPL: Skill cost 1, Exhaust, target enemy. Double the enemy's Poison (TRIPLE when upgraded). Read the target's PoisonPower amount (Creature.GetPowerAmount<PoisonPower>()) and apply amount x1 more (base) / x2 more (upgraded) so the total becomes double/triple. No damage/block vars; the multiplier must be a DynamicVar token so the upgraded description shows the right word if you use one, otherwise use two distinct localization strings via IsUpgraded.

## Choke  (class name = `Choke`, loc key `SPIRE1-CHOKE`)
- StS1 id: `Choke`, official name: `Choke`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base values: Damage=12, MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=2
- official StS1 description: `Deal !D! damage. NL Whenever you play a card this turn, the enemy loses !M! HP.`
- StS2 name collision: no -> plain title "Choke"
- IMPL: Attack 12 damage (never upgraded) + 'Whenever you play a card this turn, the enemy loses 3 HP' (5 upgraded). Needs a custom power on the ENEMY: Spire1Code/Powers/ChokePower.cs : CustomPowerModel (Debuff, Counter) with an AfterCardPlayed hook (see the decompiled AfterimagePower for the exact hook signature) that makes its owner lose Amount HP whenever the player plays a card, and expires at end of turn.

## Concentrate  (class name = `Concentrate`, loc key `SPIRE1-CONCENTRATE`)
- StS1 id: `Concentrate`, official name: `Concentrate`
- type=SKILL, rarity=UNCOMMON, cost=0, target=SELF, base values: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=-1
- official StS1 description: `Discard !M! cards. NL Gain [G] [G].`
- StS2 name collision: no -> plain title "Concentrate"
- IMPL: Skill cost 0. Discard 3 cards (2 upgraded — note the upgrade is MagicNumber -1), then gain 2 Energy. Use CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, count) + CardCmd.Discard, like the existing mod Cards/Prepared.cs. Vars: CardsVar(3) (upgrade -1) + EnergyVar(2).

## CripplingPoison  (class name = `CripplingPoison`, loc key `SPIRE1-CRIPPLING_POISON`)
- StS1 id: `Crippling Poison`, official name: `Crippling Cloud`
- type=SKILL, rarity=UNCOMMON, cost=2, target=ALL_ENEMY, base values: MagicNumber=4, StS1 flags: exhaust
- upgrade deltas: upgradeMagicNumber=3
- official StS1 description: `Apply !M! Poison and 2 Weak to ALL enemies. NL Exhaust.`
- StS2 name collision: no -> plain title "Crippling Cloud"
- IMPL: Skill cost 2, ALL enemies, Exhaust. Apply 4 Poison (7 upgraded) and 2 Weak (never upgraded) to ALL enemies. Vars: PowerVar<PoisonPower>(4), PowerVar<WeakPower>(2). AoE apply idiom: CommonActions.Apply<T>(choiceContext, this, play).

## Dash  (class name = `Dash`, loc key `SPIRE1-DASH`)
- StS1 id: `Dash`, official name: `Dash`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base values: Damage=10, Block=10, StS1 flags: none
- upgrade deltas: upgradeDamage=3, upgradeBlock=3
- official StS1 description: `Gain !B! Block. NL Deal !D! damage.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Dash"
- IMPL: Attack cost 2, gain 10 Block THEN deal 10 damage (13/13 upgraded). Vars: BlockVar(10), DamageVar(10).

## Distraction  (class name = `Distraction`, loc key `SPIRE1-DISTRACTION`)
- StS1 id: `Distraction`, official name: `Distraction`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base values: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=0
- official StS1 description: `Add a random Skill into your hand. NL It costs 0 this turn. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Distraction"
- IMPL: Skill cost 1, Exhaust: add a RANDOM Skill card into your hand; it costs 0 this turn. Requires an API to pick a random Skill from the character's pool plus a this-turn cost override. Search for both; if either is missing, FLAG the card as infeasible and DO NOT ship a partial version.

## EndlessAgony  (class name = `EndlessAgony`, loc key `SPIRE1-ENDLESS_AGONY`)
- StS1 id: `Endless Agony`, official name: `Endless Agony`
- type=ATTACK, rarity=UNCOMMON, cost=0, target=ENEMY, base values: Damage=4, StS1 flags: exhaust
- upgrade deltas: upgradeDamage=2
- official StS1 description: `Deal !D! damage. NL Whenever you draw this card, add a copy of it into your hand. NL Exhaust.`
- StS2 name collision: no -> plain title "Endless Agony"
- IMPL: Attack cost 0, 4 damage (+2), Exhaust, plus 'whenever you DRAW this card, add a copy of it into your hand'. Look for an on-drawn hook on CardModel (e.g. AfterDrawn / OnDrawn) in the decompiled CardModel.cs. If such a hook exists, add a clone via this.CreateCloneForPlayer(Owner) + CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, Owner). If no draw hook exists, FLAG it.

## EscapePlan  (class name = `EscapePlan`, loc key `SPIRE1-ESCAPE_PLAN`)
- StS1 id: `Escape Plan`, official name: `Escape Plan`
- type=SKILL, rarity=UNCOMMON, cost=0, target=SELF, base values: Block=3, StS1 flags: none
- upgrade deltas: upgradeBlock=2
- official StS1 description: `Draw 1 card. NL If you draw a Skill, gain !B! Block.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Escape Plan"
- IMPL: Skill cost 0: draw 1 card; if the drawn card is a Skill, gain 3 Block (5 upgraded). CommonActions.Draw returns the drawn cards in the decompiled CardPileCmd.Draw — capture the result and inspect card.Type == CardType.Skill. Vars: BlockVar(3), CardsVar(1).

## Eviscerate  (class name = `Eviscerate`, loc key `SPIRE1-EVISCERATE`)
- StS1 id: `Eviscerate`, official name: `Eviscerate`
- type=ATTACK, rarity=UNCOMMON, cost=3, target=ENEMY, base values: Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official StS1 description: `Costs 1 less [G] NL for each card discarded this turn. NL Deal !D! damage 3 times.`
- StS2 name collision: no -> plain title "Eviscerate"
- IMPL: Attack, base cost 3, deals 7 damage 3 TIMES (+2 per hit), and 'costs 1 less Energy for each card discarded this turn'. Multi-hit: CommonActions.CardAttack(this, play, hitCount: 3). Dynamic cost: the mod already does dynamic cost in Cards/BloodForBlood.cs via EnergyCost.AddThisCombat(-1, reduceOnly: true) — for Eviscerate the reduction must track discards THIS TURN, so hook the discard event (CardDiscardedEntry history or a discard hook) and reduce the cost by 1 per discard, resetting each turn. If a per-turn cost reset cannot be expressed, FLAG the deviation explicitly in your report.

## Expertise  (class name = `Expertise`, loc key `SPIRE1-EXPERTISE`)
- StS1 id: `Expertise`, official name: `Expertise`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base values: MagicNumber=6, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Draw cards until you have !M! in your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Expertise"
- IMPL: Skill cost 1: draw cards until you have 6 cards in hand (7 upgraded). Count PileType.Hand.GetPile(Owner).Cards.Count and draw the difference (never negative). Var: CardsVar(6).
