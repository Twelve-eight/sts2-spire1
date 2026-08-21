# Watcher COMMON cards that use stances (8) — StS1 vanilla

All numbers extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(WatcherCardPool))]`.

## Crescendo  (class `Crescendo`, loc key `SPIRE1-CRESCENDO`)
- StS1 id `Crescendo`, official name `Crescendo`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: none, StS1 flags: exhaust, selfRetain
- upgrade deltas: upgradeBaseCost=0
- official description: `Retain. NL Enter Wrath. NL Exhaust.`
- StS2 name collision: no -> plain title "Crescendo"
- IMPL: Skill cost 1 (0 upgraded), Retain + Exhaust: enter Wrath. `await StanceCmd.Enter<WrathPower>(choiceContext, Owner, this);`

## Tranquility  (class `Tranquility`, loc key `SPIRE1-TRANQUILITY`)
- StS1 id `ClearTheMind`, official name `Tranquility`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: none, StS1 flags: exhaust, selfRetain
- upgrade deltas: upgradeBaseCost=0
- official description: `Retain. NL Enter Calm. NL Exhaust.`
- StS2 name collision: no -> plain title "Tranquility"
- IMPL: Skill cost 1 (0 upgraded), Retain + Exhaust: enter Calm. `StanceCmd.Enter<CalmPower>`.

## EmptyBody  (class `EmptyBody`, loc key `SPIRE1-EMPTY_BODY`)
- StS1 id `EmptyBody`, official name `Empty Body`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=7, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL Exit your Stance.`
- StS2 name collision: no -> plain title "Empty Body"
- IMPL: Skill: gain 7 Block (+3) then EXIT your stance (`StanceCmd.Exit`). Exiting Calm must grant its 2 Energy, which StanceCmd already handles.

## EmptyFist  (class `EmptyFist`, loc key `SPIRE1-EMPTY_FIST`)
- StS1 id `EmptyFist`, official name `Empty Fist`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=9, StS1 flags: none
- upgrade deltas: upgradeDamage=5
- official description: `Deal !D! damage. NL Exit your Stance.`
- StS2 name collision: no -> plain title "Empty Fist"
- IMPL: Attack 9 (+5) then EXIT your stance (`StanceCmd.Exit`).

## Halt  (class `Halt`, loc key `SPIRE1-HALT`)
- StS1 id `Halt`, official name `Halt`
- type=SKILL, rarity=COMMON, cost=0, target=SELF, base: Block=3, MagicNumber=9, StS1 flags: none
- upgrade deltas: upgradeBlock=1, set:baseMagicNumber=4
- official description: `Gain !B! Block. NL If you are in Wrath, gain !M! additional Block.`
- StS2 name collision: no -> plain title "Halt"
- IMPL: Skill cost 0: gain 3 Block (+1); if you are in Wrath, gain 9 additional Block (upgraded: the bonus becomes 4 MORE, i.e. base bonus 9 -> upgraded 13; StS1 sets baseMagicNumber to 4 on upgrade as an ADDITIONAL amount, so verify against the vanilla text 'gain 14 additional Block' before choosing the number and state your reading in the report). Wrath check: `StanceCmd.IsIn<WrathPower>(Owner)`.

## Prostrate  (class `Prostrate`, loc key `SPIRE1-PROSTRATE`)
- StS1 id `Prostrate`, official name `Prostrate`
- type=SKILL, rarity=COMMON, cost=0, target=SELF, base: MagicNumber=2, Block=4, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Mantra. NL Gain !B! Block.`
- official upgraded description: `DEPRECATED`
- StS2 name collision: no -> plain title "Prostrate"
- IMPL: Skill cost 0: gain 2 Mantra (+1) and 4 Block. `await StanceCmd.GainMantra(choiceContext, Owner, DynamicVars.<mantraVar>.BaseValue, this);`

## FlurryOfBlows  (class `FlurryOfBlows`, loc key `SPIRE1-FLURRY_OF_BLOWS`)
- StS1 id `FlurryOfBlows`, official name `Flurry of Blows`
- type=ATTACK, rarity=COMMON, cost=0, target=ENEMY, base: Damage=4, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official description: `Deal !D! damage. NL Whenever you change Stances, return this from the discard pile to your hand.`
- StS2 name collision: no -> plain title "Flurry of Blows"
- IMPL: Attack cost 0, 4 damage (+2): whenever you CHANGE STANCES, return this card from the discard pile to your hand. Implement by making the CARD itself implement `IOnStanceChanged` (cards in every pile receive the dispatch) and, when it is in the discard pile, move itself to hand with CardPileCmd.Add(this, PileType.Hand, ...).

## CutThroughFate  (class `CutThroughFate`, loc key `SPIRE1-CUT_THROUGH_FATE`)
- StS1 id `CutThroughFate`, official name `Cut Through Fate`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=7, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeDamage=2, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL Scry !M!. NL Draw 1 card.`
- StS2 name collision: no -> plain title "Cut Through Fate"
- IMPL: Attack 7 (+2): Scry 2 (+1) then draw 1 card. Scry via BaseLib ScryCmd + ScryVar.
