# Defect UNCOMMON cards, part C (11) — StS1 vanilla

All numbers were extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(DefectCardPool))]`.

## Reprogram  (class name = `Reprogram`, loc key `SPIRE1-REPROGRAM`)
- StS1 id `Reprogram`, official name `Reprogram`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Lose !M! Focus. NL Gain !M! Strength. NL Gain !M! Dexterity.`
- StS2 name collision: no -> plain title "Reprogram"
- IMPL: Skill cost 1, no target: lose 1 Focus and gain 1 Strength and 1 Dexterity (2/2/2 upgraded). Apply negative FocusPower and positive StrengthPower/DexterityPower with PowerCmd.Apply; all three amounts share the same StS1 magicNumber, so use one DynamicVar for the value or three vars with identical values.

## RipAndTear  (class name = `RipAndTear`, loc key `SPIRE1-RIP_AND_TEAR`)
- StS1 id `Rip and Tear`, official name `Rip and Tear`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ALL_ENEMY, base: Damage=7, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official description: `Deal !D! damage to a random enemy twice.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Rip and Tear"
- IMPL: Attack cost 1: deal 7 damage (+2) to a RANDOM enemy TWICE (StS1 magicNumber 2 hits). Pick a fresh random enemy per hit using the game RNG path used by the mod's Cards/BouncingFlask.cs.

## Scrape  (class name = `Scrape`, loc key `SPIRE1-SCRAPE`)
- StS1 id `Scrape`, official name `Scrape`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=7, MagicNumber=4, StS1 flags: none
- upgrade deltas: upgradeDamage=3, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL Draw !M! cards. NL Discard all cards drawn this way that do not cost 0.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Scrape"
- IMPL: Attack cost 1: 7 damage (+3), draw 4 cards (5 upgraded), then DISCARD every card drawn this way that does not cost 0. Capture the drawn cards from the draw call's return value, filter by current cost != 0, and discard them with the ENUMERABLE CardCmd.Discard overload (the single-card overload in a loop is wrong for multi-card discards).

## SelfRepair  (class name = `SelfRepair`, loc key `SPIRE1-SELF_REPAIR`)
- StS1 id `Self Repair`, official name `Self Repair`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=7, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=3
- official description: `At the end of combat, heal !M! HP.`
- StS2 name collision: no -> plain title "Self Repair"
- IMPL: Power cost 1: at the END OF COMBAT, heal 7 HP (10 upgraded). Look for an end-of-combat hook on PowerModel (the mod's Relics/BurningBlood.cs heals at end of combat and shows the pattern used for that timing). If a power cannot observe end of combat, implement it the way the game's own SelfRepair does, or FLAG.

## Skim  (class name = `Skim`, loc key `SPIRE1-SKIM`)
- StS1 id `Skim`, official name `Skim`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Draw !M! cards.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Skim"
- IMPL: Skill cost 1: draw 3 cards (4 upgraded).

## StaticDischarge  (class name = `StaticDischarge`, loc key `SPIRE1-STATIC_DISCHARGE`)
- StS1 id `Static Discharge`, official name `Static Discharge`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Whenever you receive unblocked attack damage, Channel !M! Lightning.`
- official upgraded description: `Whenever you receive unblocked attack damage, Channel !M! Lightning.`
- StS2 name collision: no -> plain title "Static Discharge"
- IMPL: Power cost 1: whenever you receive UNBLOCKED ATTACK damage, Channel 1 Lightning (2 upgraded). Custom power using the damage-taken hook (see the decompiled EnvenomPower for the AfterDamageGiven shape and find the receiving-side equivalent, e.g. AfterDamageReceived); require UnblockedDamage > 0 and an attack source.

## Storm  (class name = `Storm`, loc key `SPIRE1-STORM`)
- StS1 id `Storm`, official name `Storm`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official description: `Whenever you play a Power card, Channel 1 Lightning.`
- official upgraded description: `Innate. NL Whenever you play a Power card, Channel 1 Lightning.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Storm"
- IMPL: Power cost 1: whenever you play a POWER card, Channel 1 Lightning; upgrade adds Innate. Custom power with AfterCardPlayed filtered to CardType.Power. NOTE: the channel count is fixed at 1 in StS1 (magicNumber 1, upgrade only adds Innate).

## Sunder  (class name = `Sunder`, loc key `SPIRE1-SUNDER`)
- StS1 id `Sunder`, official name `Sunder`
- type=ATTACK, rarity=UNCOMMON, cost=3, target=ENEMY, base: Damage=24, StS1 flags: none
- upgrade deltas: upgradeDamage=8
- official description: `Deal !D! damage. NL If this kills an enemy, gain [B] [B] [B].`
- StS2 name collision: YES -> localization title MUST be "StS1 - Sunder"
- IMPL: Attack cost 3: 24 damage (+8); if this KILLS the enemy, gain 3 Energy. Read the kill result from the attack command result (the mod reads AttackCommand.Results -> DamageResult.WasTargetKilled).

## Tempest  (class name = `Tempest`, loc key `SPIRE1-TEMPEST`)
- StS1 id `Tempest`, official name `Tempest`
- type=SKILL, rarity=UNCOMMON, cost=-1, target=SELF, base: none, StS1 flags: exhaust
- upgrade deltas: none
- official description: `Channel X Lightning. NL Exhaust.`
- official upgraded description: `Channel X+1 Lightning. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Tempest"
- IMPL: Skill, X-COST, Exhaust: Channel X Lightning (X+1 upgraded). Base cost -1 + HasEnergyCostX + ResolveEnergyXValue(); loop OrbCmd.Channel<LightningOrb>.

## WhiteNoise  (class name = `WhiteNoise`, loc key `SPIRE1-WHITE_NOISE`)
- StS1 id `White Noise`, official name `White Noise`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=0
- official description: `Add a random Power card into your hand. NL It costs 0 this turn. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - White Noise"
- IMPL: Skill cost 1 (0 upgraded), Exhaust: add a random POWER card into your hand; it costs 0 this turn. Same generation + SetToFreeThisTurn pattern as the mod's Cards/Distraction.cs, filtered to CardType.Power.
