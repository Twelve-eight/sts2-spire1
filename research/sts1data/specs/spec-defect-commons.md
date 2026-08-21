# Defect COMMON cards (18) — StS1 vanilla

All numbers were extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(DefectCardPool))]`.

## BallLightning  (class name = `BallLightning`, loc key `SPIRE1-BALL_LIGHTNING`)
- StS1 id `Ball Lightning`, official name `Ball Lightning`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: MagicNumber=1, Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. Channel !M! Lightning.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Ball Lightning"
- IMPL: Attack 7 (+3) then Channel 1 Lightning: `await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);` (see mod Cards/Zap.cs). Keep the CastAnim call out of attacks.

## Barrage  (class name = `Barrage`, loc key `SPIRE1-BARRAGE`)
- StS1 id `Barrage`, official name `Barrage`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=4, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official description: `Deal !D! damage for each Channeled Orb.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Barrage"
- IMPL: Attack 4 (+2) that hits ONCE PER CHANNELED ORB: hitCount = Owner.PlayerCombatState.OrbQueue.Orbs.Count. If zero orbs, no damage.

## BeamCell  (class name = `BeamCell`, loc key `SPIRE1-BEAM_CELL`)
- StS1 id `Beam Cell`, official name `Beam Cell`
- type=ATTACK, rarity=COMMON, cost=0, target=ENEMY, base: Damage=3, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=1, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL Apply !M! Vulnerable.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Beam Cell"
- IMPL: Attack 3 (+1) + apply 1 Vulnerable (+1). PowerVar<VulnerablePower>(1).

## Claw  (class name = `Claw`, loc key `SPIRE1-CLAW`)
- StS1 id `Gash`, official name `Claw`
- type=ATTACK, rarity=COMMON, cost=0, target=ENEMY, base: Damage=3, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official description: `Deal !D! damage. NL Increase the damage of ALL Claw cards by !M! this combat.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Claw"
- IMPL: Attack 3 damage, upgrade sets base damage to 5 (upgradeDamage=+2). Effect: 'Increase the damage of ALL Claw cards by 2 this combat'. Implement by iterating every combat pile (PileType.Draw/Hand/Discard/Exhaust/Play) for cards of this exact runtime type and raising their damage for the rest of the combat; the mod's Cards/Rampage.cs shows the per-combat damage-raise mechanism. The +2 step is fixed (StS1 magicNumber 2, not upgraded). Verify the game ships its own Claw (`.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Cards/Claw.cs`) and copy its cross-copy update approach if present.

## ColdSnap  (class name = `ColdSnap`, loc key `SPIRE1-COLD_SNAP`)
- StS1 id `Cold Snap`, official name `Cold Snap`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: MagicNumber=1, Damage=6, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. Channel !M! Frost.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Cold Snap"
- IMPL: Attack 6 (+3) then Channel 1 Frost (`OrbCmd.Channel<FrostOrb>`).

## CompileDriver  (class name = `CompileDriver`, loc key `SPIRE1-COMPILE_DRIVER`)
- StS1 id `Compile Driver`, official name `Compile Driver`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=7, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. NL Draw !M! card for each unique Orb you have.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Compile Driver"
- IMPL: Attack 7 (+3) then draw 1 card FOR EACH UNIQUE orb type currently channeled: count distinct runtime types in OrbQueue.Orbs and draw that many (CardPileCmd.Draw).

## ConserveBattery  (class name = `ConserveBattery`, loc key `SPIRE1-CONSERVE_BATTERY`)
- StS1 id `Conserve Battery`, official name `Charge Battery`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=7, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL Next turn, gain [B].`
- StS2 name collision: no -> plain title "Charge Battery"
- IMPL: Skill: gain 7 Block (+3) and next turn gain 1 Energy via PowerCmd.Apply<EnergyNextTurnPower>. Vars BlockVar(7), EnergyVar(1).

## Coolheaded  (class name = `Coolheaded`, loc key `SPIRE1-COOLHEADED`)
- StS1 id `Coolheaded`, official name `Coolheaded`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Channel 1 Frost. NL Draw !M! card.`
- official upgraded description: `Channel 1 Frost. NL Draw !M! cards.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Coolheaded"
- IMPL: Skill: Channel 1 Frost, then draw 1 card (2 upgraded). Var CardsVar(1) upgraded +1.

## GoForTheEyes  (class name = `GoForTheEyes`, loc key `SPIRE1-GO_FOR_THE_EYES`)
- StS1 id `Go for the Eyes`, official name `Go for the Eyes`
- type=ATTACK, rarity=COMMON, cost=0, target=ENEMY, base: Damage=3, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=1, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL If the enemy intends to attack, apply !M! Weak.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Go for the Eyes"
- IMPL: Attack 3 (+1); if the target intends to attack, apply 1 Weak (+1). Intent check: `play.Target?.Monster?.IntendsToAttack == true` (verify exact member).

## Hologram  (class name = `Hologram`, loc key `SPIRE1-HOLOGRAM`)
- StS1 id `Hologram`, official name `Hologram`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=3, StS1 flags: exhaust
- upgrade deltas: upgradeBlock=2, flag:exhaust=false
- official description: `Gain !B! Block. NL Put a card from your discard pile into your hand. NL Exhaust.`
- official upgraded description: `Gain !B! Block. NL Put a card from your discard pile into your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Hologram"
- IMPL: Skill: gain 3 Block (+2) then return ONE chosen card from the discard pile to hand. Exhaust in base form; the upgrade REMOVES Exhaust (mod precedent: Cards/CalculatedGamble.cs uses RemoveKeyword in OnUpgrade). Selection from discard: CardSelectorPrefs + CommonActions.SelectCards(..., PileType.Discard, null), then CardPileCmd.Add to hand.

## Leap  (class name = `Leap`, loc key `SPIRE1-LEAP`)
- StS1 id `Leap`, official name `Leap`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=9, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Leap"
- IMPL: Skill: gain 9 Block (+3). Nothing else.

## Rebound  (class name = `Rebound`, loc key `SPIRE1-REBOUND`)
- StS1 id `Rebound`, official name `Rebound`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=9, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. NL Put the next card you play this turn on top of your draw pile.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Rebound"
- IMPL: Attack 9 (+3): 'Put the NEXT card you play this turn on top of your draw pile.' Needs a one-shot power (Spire1Code/Powers/ReboundPower.cs, Buff/Counter) that on the next AfterCardPlayed by the owner moves that played card to PileType.Draw / CardPilePosition.Top and then removes itself; also expire at turn end. Copy hook bookkeeping from the decompiled AfterimagePower (BeforeCardPlayed/AfterCardPlayed pair) so Rebound itself is not the affected card.

## Recursion  (class name = `Recursion`, loc key `SPIRE1-RECURSION`)
- StS1 id `Redo`, official name `Recursion`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official description: `Evoke your next Orb. NL Channel the Orb that was just Evoked.`
- StS2 name collision: no -> plain title "Recursion"
- IMPL: Skill cost 1 (0 upgraded): Evoke your next Orb, then Channel the SAME orb type that was just evoked. Read the front orb from Owner.PlayerCombatState.OrbQueue.Orbs before evoking, call OrbCmd.EvokeNext, then re-channel via OrbCmd.Channel(choiceContext, <fresh mutable orb of that type>, Owner) (see OrbModel.ToMutable). Verify against the game's own Recursion if it ships one.

## Stack  (class name = `Stack`, loc key `SPIRE1-STACK`)
- StS1 id `Stack`, official name `Stack`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=0, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain Block equal to the number of cards in your discard pile.`
- official upgraded description: `Gain Block equal to the number of cards in your discard pile +3.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Stack"
- IMPL: Skill: gain Block equal to the number of cards in your DISCARD pile (+3 additional when upgraded). The displayed number must be dynamic. First check whether the engine has a calculated-block var analogous to CustomCardModel.MakeCalculatedDamage; if yes use it, otherwise use the game's own Stack implementation (`.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Cards/Stack.cs`) as the exact reference. Do not hardcode a static number.

## SteamBarrier  (class name = `SteamBarrier`, loc key `SPIRE1-STEAM_BARRIER`)
- StS1 id `Steam`, official name `Steam Barrier`
- type=SKILL, rarity=COMMON, cost=0, target=SELF, base: Block=6, StS1 flags: none
- upgrade deltas: upgradeBlock=2
- official description: `Gain !B! Block. Decrease this card's Block by 1 this combat.`
- StS2 name collision: no -> plain title "Steam Barrier"
- IMPL: Skill cost 0: gain 6 Block (+2), then permanently (this combat) reduce this card's Block by 1. Mirror of the mod's Cards/GlassKnife.cs / Rampage.cs per-combat value change, decreasing instead of increasing, and never below 0.

## Streamline  (class name = `Streamline`, loc key `SPIRE1-STREAMLINE`)
- StS1 id `Streamline`, official name `Streamline`
- type=ATTACK, rarity=COMMON, cost=2, target=ENEMY, base: Damage=15, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=5
- official description: `Deal !D! damage. NL Reduce this card's cost by !M! this combat.`
- StS2 name collision: no -> plain title "Streamline"
- IMPL: Attack 15 (+5): then reduce this card's cost by 1 for the rest of the combat: EnergyCost.AddThisCombat(-1, reduceOnly: true) (mod precedent Cards/BloodForBlood.cs).

## SweepingBeam  (class name = `SweepingBeam`, loc key `SPIRE1-SWEEPING_BEAM`)
- StS1 id `Sweeping Beam`, official name `Sweeping Beam`
- type=ATTACK, rarity=COMMON, cost=1, target=ALL_ENEMY, base: Damage=6, MagicNumber=1, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage to ALL enemies. NL Draw !M! card.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Sweeping Beam"
- IMPL: Attack 6 (+3) to ALL enemies (TargetType.AllEnemies) then draw 1 card.

## Turbo  (class name = `Turbo`, loc key `SPIRE1-TURBO`)
- StS1 id `Turbo`, official name `TURBO`
- type=SKILL, rarity=COMMON, cost=0, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain [B] [B]. NL Add a *Void into your discard pile.`
- official upgraded description: `Gain [B] [B] [B]. NL Add a *Void into your discard pile.`
- StS2 name collision: YES -> localization title MUST be "StS1 - TURBO"
- IMPL: Skill cost 0: gain 2 Energy (3 upgraded), then add one Void status card into your DISCARD pile. REUSE the mod's existing status card `Spire1.Spire1Code.Cards.Void` via `CardPileCmd.AddToCombatAndPreview<Void>(Owner.Creature, PileType.Discard, 1, Owner)` (mod precedent Cards/Immolate.cs). Var EnergyVar(2).
