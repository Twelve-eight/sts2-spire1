# Defect UNCOMMON cards, part B (13) — StS1 vanilla

All numbers were extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(DefectCardPool))]`.

## FTL  (class name = `FTL`, loc key `SPIRE1-FTL`)
- StS1 id `FTL`, official name `FTL`
- type=ATTACK, rarity=UNCOMMON, cost=0, target=ENEMY, base: Damage=5, MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeDamage=1, upgradeMagicNumber=1
- official description: `Deal !D! damage. If you have played less than !M! cards this turn, draw 1 card.`
- StS2 name collision: no -> plain title "FTL"
- IMPL: Attack cost 0: 5 damage (+1); if you have played FEWER THAN 3 cards this turn (4 upgraded), draw 1 card. Count with CombatManager.Instance.History.CardPlaysFinished filtered by HappenedThisTurn and owner (mod precedent Cards/Finisher.cs). The threshold must be a DynamicVar.

## ForceField  (class name = `ForceField`, loc key `SPIRE1-FORCE_FIELD`)
- StS1 id `Force Field`, official name `Force Field`
- type=SKILL, rarity=UNCOMMON, cost=4, target=SELF, base: Block=12, StS1 flags: none
- upgrade deltas: upgradeBlock=4
- official description: `Costs 1 less [B] for each Power card played this combat. NL Gain !B! Block.`
- StS2 name collision: no -> plain title "Force Field"
- IMPL: Skill base cost 4: 'Costs 1 less Energy for each POWER card played this combat.' Gain 12 Block (+4). Track power plays via the combat history (CardPlayFinishedEntry with Card.Type == CardType.Power) and reduce the cost with EnergyCost.AddThisCombat(-1, reduceOnly: true) as they happen; guard clones like the mod's Cards/MasterfulStab.cs does with IsClone.

## Fusion  (class name = `Fusion`, loc key `SPIRE1-FUSION`)
- StS1 id `Fusion`, official name `Fusion`
- type=SKILL, rarity=UNCOMMON, cost=2, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeBaseCost=1
- official description: `Channel !M! Plasma.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Fusion"
- IMPL: Skill cost 2 (upgrade REDUCES cost to 1: StS1 upgradeBaseCost=1): Channel 1 Plasma (`OrbCmd.Channel<PlasmaOrb>`).

## GeneticAlgorithm  (class name = `GeneticAlgorithm`, loc key `SPIRE1-GENETIC_ALGORITHM`)
- StS1 id `Genetic Algorithm`, official name `Genetic Algorithm`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=2, Block=2, StS1 flags: exhaust
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !B! Block. Permanently increase this card's Block by !M!. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Genetic Algorithm"
- IMPL: Skill cost 1, Exhaust: gain 2 Block and PERMANENTLY (for the whole run, StS1 semantics) increase this card's Block by 2 (3 upgraded). If a run-persistent per-card value change is not supported by the engine, implement the combat-scoped version and FLAG the deviation explicitly; do not silently claim permanence.

## Glacier  (class name = `Glacier`, loc key `SPIRE1-GLACIER`)
- StS1 id `Glacier`, official name `Glacier`
- type=SKILL, rarity=UNCOMMON, cost=2, target=SELF, base: MagicNumber=2, Block=7, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL Channel !M! Frost.`
- official upgraded description: `Gain !B! Block. NL Channel !M! Frost.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Glacier"
- IMPL: Skill cost 2: gain 7 Block (+3) and Channel 2 Frost (count NOT upgraded).

## Heatsinks  (class name = `Heatsinks`, loc key `SPIRE1-HEATSINKS`)
- StS1 id `Heatsinks`, official name `Heatsinks`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Whenever you play a Power card, draw !M! card.`
- official upgraded description: `Whenever you play a Power card, draw !M! cards.`
- StS2 name collision: no -> plain title "Heatsinks"
- IMPL: Power cost 1: whenever you play a POWER card, draw 1 card (2 upgraded). Custom power Spire1Code/Powers/HeatsinksPower.cs (Buff/Counter) with the AfterCardPlayed hook filtered to CardType.Power plays by its owner.

## HelloWorld  (class name = `HelloWorld`, loc key `SPIRE1-HELLO_WORLD`)
- StS1 id `Hello World`, official name `Hello World`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: none, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official description: `At the start of your turn, add a random Common card into your hand.`
- official upgraded description: `Innate. NL At the start of your turn, add a random Common card into your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Hello World"
- IMPL: Power cost 1: at the start of your turn, add a random COMMON card into your hand; upgrade adds Innate. Random common generation: use the same CardFactory API the mod's Cards/Distraction.cs uses, filtered to CardRarity.Common. Custom power Spire1Code/Powers/HelloWorldPower.cs with AfterSideTurnStart.

## LockOn  (class name = `LockOn`, loc key `SPIRE1-LOCK_ON`)
- StS1 id `Lockon`, official name `Bullseye`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=8, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeDamage=3, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL Apply !M! Lock-On.`
- StS2 name collision: no -> plain title "Bullseye"
- IMPL: Attack cost 1: 8 damage (+3) + apply 2 Lock-On (+1). StS2 has NO Lock-On power (verified NOT FOUND). Lock-On in StS1 makes the enemy take 50% more ORB damage. Search the decompiled sources for any damage-taken/orb-damage modifier hook on PowerModel; if such a hook exists, implement Spire1Code/Powers/LockOnPower.cs faithfully (Debuff/Counter, +50% orb damage taken, decrements each turn). If no such hook exists, FLAG the card and do not ship it.

## Loop  (class name = `Loop`, loc key `SPIRE1-LOOP`)
- StS1 id `Loop`, official name `Loop`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `At the start of your turn, trigger the passive ability of your next Orb.`
- official upgraded description: `At the start of your turn, trigger the passive ability of your next Orb !M! times.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Loop"
- IMPL: Power cost 1: at the start of your turn, trigger the passive of your NEXT orb 1 time (2 upgraded). Custom power with AfterSideTurnStart calling OrbCmd.Passive on OrbQueue.Orbs[0] Amount times.

## Melter  (class name = `Melter`, loc key `SPIRE1-MELTER`)
- StS1 id `Melter`, official name `Melter`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=10, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official description: `Remove all Block from the enemy. NL Deal !D! damage.`
- StS2 name collision: no -> plain title "Melter"
- IMPL: Attack cost 1: remove ALL Block from the enemy, then deal 10 damage (+4). Find the exact block-removal command in the decompiled CreatureCmd (e.g. a LoseBlock/SetBlock API) and use it; do not simulate it with damage.

## Overclock  (class name = `Overclock`, loc key `SPIRE1-OVERCLOCK`)
- StS1 id `Steam Power`, official name `Overclock`
- type=SKILL, rarity=UNCOMMON, cost=0, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Draw !M! cards. NL Add a *Burn into your discard pile.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Overclock"
- IMPL: Skill cost 0: draw 2 cards (3 upgraded), then add a Burn into your discard pile. REUSE the mod's existing `Spire1.Spire1Code.Cards.Burn` status card via CardPileCmd.AddToCombatAndPreview<Burn>(Owner.Creature, PileType.Discard, 1, Owner) (mod precedent Cards/Immolate.cs).

## Recycle  (class name = `Recycle`, loc key `SPIRE1-RECYCLE`)
- StS1 id `Recycle`, official name `Recycle`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official description: `Exhaust a card. NL Gain [B] equal to its cost.`
- StS2 name collision: no -> plain title "Recycle"
- IMPL: Skill cost 1 (0 upgraded): Exhaust a chosen card from hand and gain Energy equal to ITS cost. Select with CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1), read the selected card's current energy cost value (verify the exact property), CardCmd.Exhaust it, then PlayerCmd.GainEnergy that amount. X-cost cards count as 0 unless the game's own Recycle says otherwise.

## ReinforcedBody  (class name = `ReinforcedBody`, loc key `SPIRE1-REINFORCED_BODY`)
- StS1 id `Reinforced Body`, official name `Reinforced Body`
- type=SKILL, rarity=UNCOMMON, cost=-1, target=SELF, base: Block=7, StS1 flags: none
- upgrade deltas: upgradeBlock=2
- official description: `Gain !B! Block X times.`
- StS2 name collision: no -> plain title "Reinforced Body"
- IMPL: Skill, X-COST: gain 7 Block (+2) X TIMES. Base cost -1 + HasEnergyCostX + ResolveEnergyXValue(); loop CommonActions.CardBlock X times (mod precedent Cards/Whirlwind.cs).
