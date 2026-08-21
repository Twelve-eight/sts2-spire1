# Defect UNCOMMON cards, part A (13) — StS1 vanilla

All numbers were extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(DefectCardPool))]`.

## Aggregate  (class name = `Aggregate`, loc key `SPIRE1-AGGREGATE`)
- StS1 id `Aggregate`, official name `Aggregate`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=4, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=-1
- official description: `Gain [B] for every !M! cards in your draw pile.`
- StS2 name collision: no -> plain title "Aggregate"
- IMPL: Skill: gain 1 Energy for every 4 cards in your DRAW pile (every 3 when upgraded — StS1 magicNumber 4 with upgrade -1). Compute floor(drawPileCount / magic) and PlayerCmd.GainEnergy that much. The divisor must be a DynamicVar so the upgraded text is correct.

## AutoShields  (class name = `AutoShields`, loc key `SPIRE1-AUTO_SHIELDS`)
- StS1 id `Auto Shields`, official name `Auto-Shields`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: Block=11, StS1 flags: none
- upgrade deltas: upgradeBlock=4
- official description: `If you have no Block, gain !B! Block.`
- StS2 name collision: no -> plain title "Auto-Shields"
- IMPL: Skill: if you have NO Block, gain 11 Block (15 upgraded). Check Owner.Creature.Block == 0 before granting.

## Blizzard  (class name = `Blizzard`, loc key `SPIRE1-BLIZZARD`)
- StS1 id `Blizzard`, official name `Blizzard`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ALL_ENEMY, base: Damage=0, MagicNumber=2, StS1 flags: isMultiDamage
- upgrade deltas: upgradeMagicNumber=1
- official description: `Deal damage equal to !M! times the number of Frost Channeled this combat to ALL enemies.`
- StS2 name collision: no -> plain title "Blizzard"
- IMPL: Attack, ALL enemies: deal damage equal to 2x (3x upgraded) the number of FROST orbs channeled THIS COMBAT. Requires a combat-scoped count of Frost channels: check the combat history for an orb-channel entry type (search `.tmp/dllsrc/MegaCrit.Sts2.Core.Combat.History/`), or the game's own Blizzard if shipped. If no channel history exists, track it with a small custom power/hook, and if neither is possible FLAG the card.

## BootSequence  (class name = `BootSequence`, loc key `SPIRE1-BOOT_SEQUENCE`)
- StS1 id `BootSequence`, official name `Boot Sequence`
- type=SKILL, rarity=UNCOMMON, cost=0, target=SELF, base: Block=10, StS1 flags: isInnate, exhaust
- upgrade deltas: upgradeBlock=3
- official description: `Innate. NL Gain !B! Block. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Boot Sequence"
- IMPL: Skill cost 0: Innate + Exhaust, gain 10 Block (13 upgraded). CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust].

## Capacitor  (class name = `Capacitor`, loc key `SPIRE1-CAPACITOR`)
- StS1 id `Capacitor`, official name `Capacitor`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Orb slots.`
- official upgraded description: `Gain !M! Orb slots.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Capacitor"
- IMPL: Power cost 1: gain 2 Orb slots (3 upgraded) via `await OrbCmd.AddSlots(Owner, DynamicVars.<var>.IntValue);`. Use a plain DynamicVar for the slot count (not PowerVar) unless the game's own Capacitor does otherwise.

## Chaos  (class name = `Chaos`, loc key `SPIRE1-CHAOS`)
- StS1 id `Chaos`, official name `Chaos`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Channel !M! random Orb.`
- official upgraded description: `Channel !M! random Orbs.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Chaos"
- IMPL: Skill cost 1: Channel 1 random Orb (2 upgraded). Use OrbModel.GetRandomOrb(<combat rng>) then OrbCmd.Channel(choiceContext, orb, Owner). Use the game's RNG accessor (see mod Cards/AllOutAttack.cs for the exact Rng path), never System.Random.

## Chill  (class name = `Chill`, loc key `SPIRE1-CHILL`)
- StS1 id `Chill`, official name `Chill`
- type=SKILL, rarity=UNCOMMON, cost=0, target=SELF, base: MagicNumber=1, StS1 flags: exhaust
- upgrade deltas: flag:isInnate=true
- official description: `Channel !M! Frost for each enemy in combat. NL Exhaust.`
- official upgraded description: `Innate. NL Channel !M! Frost for each enemy in combat. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Chill"
- IMPL: Skill cost 0, Exhaust: Channel 1 Frost FOR EACH ENEMY in combat; upgrade adds Innate. Count Owner.Creature.CombatState.HittableEnemies.

## Consume  (class name = `Consume`, loc key `SPIRE1-CONSUME`)
- StS1 id `Consume`, official name `Consume`
- type=SKILL, rarity=UNCOMMON, cost=2, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Focus. NL Lose 1 Orb slot.`
- StS2 name collision: no -> plain title "Consume"
- IMPL: Skill cost 2: gain 2 Focus (3 upgraded) and LOSE 1 Orb slot. FocusPower for the gain (PowerVar<FocusPower>), `OrbCmd.RemoveSlots(Owner, 1)` for the slot loss.

## Darkness  (class name = `Darkness`, loc key `SPIRE1-DARKNESS`)
- StS1 id `Darkness`, official name `Darkness`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: none
- official description: `Channel !M! Dark.`
- official upgraded description: `Channel !M! Dark. NL Trigger the passive ability of all Dark orbs.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Darkness"
- IMPL: Skill cost 1: Channel 1 Dark. Upgraded ALSO triggers the passive of all Dark orbs: iterate OrbQueue.Orbs of type DarkOrb and call OrbCmd.Passive(choiceContext, orb, null) (verify the exact signature; OrbCmd.Passive exists).

## Defragment  (class name = `Defragment`, loc key `SPIRE1-DEFRAGMENT`)
- StS1 id `Defragment`, official name `Defragment`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Focus.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Defragment"
- IMPL: Power cost 1: gain 1 Focus (2 upgraded) — PowerVar<FocusPower>(1) + ApplySelf.

## DoomAndGloom  (class name = `DoomAndGloom`, loc key `SPIRE1-DOOM_AND_GLOOM`)
- StS1 id `Doom and Gloom`, official name `Doom and Gloom`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ALL_ENEMY, base: MagicNumber=1, Damage=10, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage to ALL enemies. NL Channel !M! Dark.`
- StS2 name collision: no -> plain title "Doom and Gloom"
- IMPL: Attack cost 2: 10 damage (+4) to ALL enemies, then Channel 1 Dark.

## DoubleEnergy  (class name = `DoubleEnergy`, loc key `SPIRE1-DOUBLE_ENERGY`)
- StS1 id `Double Energy`, official name `Double Energy`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=0
- official description: `Double your Energy. NL Exhaust.`
- official upgraded description: `Double your Energy.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Double Energy"
- IMPL: Skill cost 1 (0 upgraded), Exhaust: DOUBLE your current Energy. Read the player's current energy (verify the exact property on Player/PlayerCombatState) and PlayerCmd.GainEnergy(current).

## Equilibrium  (class name = `Equilibrium`, loc key `SPIRE1-EQUILIBRIUM`)
- StS1 id `Undo`, official name `Equilibrium`
- type=SKILL, rarity=UNCOMMON, cost=2, target=SELF, base: Block=13, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL Retain your hand this turn.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Equilibrium"
- IMPL: Skill cost 2: gain 13 Block (+3) and RETAIN YOUR HAND this turn. CardKeyword.Retain exists; for 'retain whole hand this turn' find the single-turn retain API used by the mod's WellLaidPlansPower (CardCmd.ApplySingleTurnRetain) and apply it to every card in hand.
