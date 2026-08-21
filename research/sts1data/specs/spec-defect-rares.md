# Defect RARE cards (17) — StS1 vanilla

All numbers were extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(DefectCardPool))]`.

## AllForOne  (class name = `AllForOne`, loc key `SPIRE1-ALL_FOR_ONE`)
- StS1 id `All For One`, official name `All for One`
- type=ATTACK, rarity=RARE, cost=2, target=ENEMY, base: Damage=10, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage. NL Put all cost 0 cards from your discard pile into your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - All for One"
- IMPL: Attack cost 2: 10 damage (+4), then put ALL cost-0 cards from your discard pile into your hand. Filter PileType.Discard cards whose current cost is 0 and move them with CardPileCmd.Add to hand.

## Amplify  (class name = `Amplify`, loc key `SPIRE1-AMPLIFY`)
- StS1 id `Amplify`, official name `Amplify`
- type=SKILL, rarity=RARE, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `This turn, your next Power card is played twice.`
- official upgraded description: `This turn, your next !M! Power cards are played twice.`
- StS2 name collision: no -> plain title "Amplify"
- IMPL: Skill cost 1: this turn, your next POWER card is played twice (2 Power cards upgraded). Search for an existing StS2 play-twice power (the game ships Burst/Amplify-like effects; check `.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Powers/BurstPower.cs` and any AmplifyPower). Reuse the shipped power if one exists; otherwise FLAG.

## BiasedCognition  (class name = `BiasedCognition`, loc key `SPIRE1-BIASED_COGNITION`)
- StS1 id `Biased Cognition`, official name `Biased Cognition`
- type=POWER, rarity=RARE, cost=1, target=SELF, base: MagicNumber=4, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Focus. NL At the start of your turn, lose 1 Focus.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Biased Cognition"
- IMPL: Power cost 1: gain 4 Focus (5 upgraded); at the start of your turn, LOSE 1 Focus. Apply FocusPower(4) from the card, and add a custom power (Spire1Code/Powers/BiasedCognitionPower.cs) whose AfterSideTurnStart applies -1 FocusPower.

## Buffer  (class name = `Buffer`, loc key `SPIRE1-BUFFER`)
- StS1 id `Buffer`, official name `Buffer`
- type=POWER, rarity=RARE, cost=2, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Prevent the next time you would lose HP.`
- official upgraded description: `Prevent the next !M! times you would lose HP.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Buffer"
- IMPL: Power cost 2: prevent the next 1 (2 upgraded) time you would lose HP. REUSE the shipped BufferPower: PowerVar<BufferPower>(1) + ApplySelf.

## CoreSurge  (class name = `CoreSurge`, loc key `SPIRE1-CORE_SURGE`)
- StS1 id `Core Surge`, official name `Core Surge`
- type=ATTACK, rarity=RARE, cost=1, target=ENEMY, base: Damage=11, MagicNumber=1, StS1 flags: exhaust
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage. NL Gain !M! Artifact. NL Exhaust.`
- StS2 name collision: no -> plain title "Core Surge"
- IMPL: Attack cost 1, Exhaust: 11 damage (+4) then gain 1 Artifact (ArtifactPower, amount NOT upgraded).

## CreativeAI  (class name = `CreativeAI`, loc key `SPIRE1-CREATIVE_AI`)
- StS1 id `Creative AI`, official name `Creative AI`
- type=POWER, rarity=RARE, cost=3, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeBaseCost=2
- official description: `At the start of your turn, add a random Power card into your hand.`
- StS2 name collision: no -> plain title "Creative AI"
- IMPL: Power cost 3 (2 upgraded): at the start of your turn, add a random POWER card into your hand. Custom power with AfterSideTurnStart + the same random-power generation used by WhiteNoise; coordinate the shared generation helper with the other Defect writers via hub if needed (duplicate small helpers are acceptable, contradictory APIs are not).

## EchoForm  (class name = `EchoForm`, loc key `SPIRE1-ECHO_FORM`)
- StS1 id `Echo Form`, official name `Echo Form`
- type=POWER, rarity=RARE, cost=3, target=SELF, base: none, StS1 flags: isEthereal
- upgrade deltas: flag:isEthereal=false
- official description: `Ethereal. NL The first card you play each turn is played twice.`
- official upgraded description: `The first card you play each turn is played twice.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Echo Form"
- IMPL: Power cost 3, ETHEREAL (upgrade removes Ethereal): the FIRST card you play each turn is played twice. Search for a shipped EchoForm power in the decompiled sources and reuse it; if absent, a custom power must actually replay the card (verify a replay/AutoPlay API such as CardCmd.AutoPlay) or the card must be FLAGGED.

## Electrodynamics  (class name = `Electrodynamics`, loc key `SPIRE1-ELECTRODYNAMICS`)
- StS1 id `Electrodynamics`, official name `Electrodynamics`
- type=POWER, rarity=RARE, cost=2, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Lightning now hits ALL enemies. NL Channel !M! Lightning.`
- StS2 name collision: no -> plain title "Electrodynamics"
- IMPL: Power cost 2: Lightning orbs now hit ALL enemies, and Channel 2 Lightning (3 upgraded). The 'lightning hits all enemies' part needs an orb-behaviour modifier; check the shipped Electrodynamics/LightningOrb code for a supported flag. If the AoE conversion cannot be expressed, FLAG the card rather than shipping only the channels.

## Fission  (class name = `Fission`, loc key `SPIRE1-FISSION`)
- StS1 id `Fission`, official name `Fission`
- type=SKILL, rarity=RARE, cost=0, target=NONE, base: MagicNumber=1, StS1 flags: exhaust
- upgrade deltas: none
- official description: `Remove all your Orbs. Gain [B] and draw !M! card for each Orb removed. NL Exhaust.`
- official upgraded description: `Evoke all your Orbs. Gain [B] and draw !M! card for each Orb Evoked. NL Exhaust.`
- StS2 name collision: no -> plain title "Fission"
- IMPL: Skill cost 0, Exhaust: base version REMOVES all your orbs and gains 1 Energy + draws 1 card per orb removed; upgraded EVOKES all orbs instead and gives the same Energy/draw. Use OrbQueue.Remove / OrbCmd.EvokeNext accordingly; count first, then grant.

## Hyperbeam  (class name = `Hyperbeam`, loc key `SPIRE1-HYPERBEAM`)
- StS1 id `Hyperbeam`, official name `Hyperbeam`
- type=ATTACK, rarity=RARE, cost=2, target=ALL_ENEMY, base: Damage=26, MagicNumber=3, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=8
- official description: `Deal !D! damage to ALL enemies. NL Lose !M! Focus.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Hyperbeam"
- IMPL: Attack cost 2: 26 damage (+8) to ALL enemies, then LOSE 3 Focus (apply negative FocusPower, amount NOT upgraded).

## MachineLearning  (class name = `MachineLearning`, loc key `SPIRE1-MACHINE_LEARNING`)
- StS1 id `Machine Learning`, official name `Machine Learning`
- type=POWER, rarity=RARE, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official description: `At the start of your turn, draw !M! additional card.`
- official upgraded description: `Innate. NL At the start of your turn, draw !M! additional card.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Machine Learning"
- IMPL: Power cost 1: at the start of your turn draw 1 additional card; upgrade adds Innate. REUSE the shipped DrawCardsNextTurnPower ONLY if it is permanent; StS1 Machine Learning is permanent, so prefer a custom power (Spire1Code/Powers/MachineLearningPower.cs) that increases the draw every turn (see the decompiled DrawCardsNextTurnPower.ModifyHandDraw for the correct hook).

## MeteorStrike  (class name = `MeteorStrike`, loc key `SPIRE1-METEOR_STRIKE`)
- StS1 id `Meteor Strike`, official name `Meteor Strike`
- type=ATTACK, rarity=RARE, cost=5, target=ENEMY, base: Damage=24, MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeDamage=6
- official description: `Deal !D! damage. NL Channel !M! Plasma.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Meteor Strike"
- IMPL: Attack cost 5: 24 damage (+6) then Channel 3 Plasma (count NOT upgraded).

## MultiCast  (class name = `MultiCast`, loc key `SPIRE1-MULTI_CAST`)
- StS1 id `Multi-Cast`, official name `Multi-Cast`
- type=SKILL, rarity=RARE, cost=-1, target=NONE, base: none, StS1 flags: none
- upgrade deltas: none
- official description: `Evoke your next Orb X times.`
- official upgraded description: `Evoke your next Orb X+1 times.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Multi-Cast"
- IMPL: Skill, X-COST: Evoke your next Orb X times (X+1 upgraded). Use OrbCmd.EvokeNext with dequeue:false for all but the final evoke, mirroring the mod's Cards/Dualcast.cs.

## Rainbow  (class name = `Rainbow`, loc key `SPIRE1-RAINBOW`)
- StS1 id `Rainbow`, official name `Rainbow`
- type=SKILL, rarity=RARE, cost=2, target=SELF, base: none, StS1 flags: exhaust
- upgrade deltas: flag:exhaust=false
- official description: `Channel 1 Lightning. NL Channel 1 Frost. NL Channel 1 Dark. NL Exhaust.`
- official upgraded description: `Channel 1 Lightning. NL Channel 1 Frost. NL Channel 1 Dark.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Rainbow"
- IMPL: Skill cost 2, Exhaust (upgrade REMOVES Exhaust): Channel 1 Lightning, 1 Frost and 1 Dark, in that order.

## Reboot  (class name = `Reboot`, loc key `SPIRE1-REBOOT`)
- StS1 id `Reboot`, official name `Reboot`
- type=SKILL, rarity=RARE, cost=0, target=SELF, base: MagicNumber=4, StS1 flags: exhaust
- upgrade deltas: upgradeMagicNumber=2
- official description: `Shuffle ALL your cards into your draw pile. NL Draw !M! cards. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Reboot"
- IMPL: Skill cost 0, Exhaust: shuffle ALL your cards (hand + discard + draw) into your draw pile, then draw 4 cards (6 upgraded). Find the shuffle/refill API in the decompiled CardPileCmd; do not fake it by moving cards one at a time if a shuffle command exists.

## Seek  (class name = `Seek`, loc key `SPIRE1-SEEK`)
- StS1 id `Seek`, official name `Seek`
- type=SKILL, rarity=RARE, cost=0, target=NONE, base: MagicNumber=1, StS1 flags: exhaust
- upgrade deltas: upgradeMagicNumber=1
- official description: `Put !M! card from your draw pile into your hand. NL Exhaust.`
- official upgraded description: `Put !M! cards from your draw pile into your hand. NL Exhaust.`
- StS2 name collision: no -> plain title "Seek"
- IMPL: Skill cost 0, Exhaust: put 1 card (2 upgraded) from your DRAW pile into your hand, chosen by the player. Selection from PileType.Draw + CardPileCmd.Add to hand.

## ThunderStrike  (class name = `ThunderStrike`, loc key `SPIRE1-THUNDER_STRIKE`)
- StS1 id `Thunder Strike`, official name `Thunder Strike`
- type=ATTACK, rarity=RARE, cost=3, target=ALL_ENEMY, base: MagicNumber=0, Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official description: `Deal !D! damage to a random enemy for each Lightning Channeled this combat.`
- StS2 name collision: no -> plain title "Thunder Strike"
- IMPL: Attack cost 3: deal 7 damage (+2) to a RANDOM enemy FOR EACH Lightning channeled this combat. Same combat-scoped channel count problem as Blizzard: use an orb-channel history entry if one exists, otherwise a tracked counter, otherwise FLAG.
