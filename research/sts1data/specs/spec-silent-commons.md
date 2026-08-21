# Silent COMMON cards (9) — StS1 vanilla

Every number below was extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. The `StS2 name collision` field decides the localization title prefix.

## Bane  (class name = `Bane`, loc key `SPIRE1-BANE`)
- StS1 id: `Bane`, official name: `Bane`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base values: Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official StS1 description: `Deal !D! damage. NL If the enemy has Poison, deal !D! damage again.`
- StS2 name collision: no -> plain title "Bane"
- IMPL: Attack. After the normal CardAttack, if the target has PoisonPower, execute a SECOND identical CardAttack. Query with play.Target!.HasPower<PoisonPower>() (verify exact API against an existing mod card).

## BladeDance  (class name = `BladeDance`, loc key `SPIRE1-BLADE_DANCE`)
- StS1 id: `Blade Dance`, official name: `Blade Dance`
- type=SKILL, rarity=COMMON, cost=1, target=NONE, base values: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Add !M! *Shivs into your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Blade Dance"
- IMPL: Skill. Add 3 Shivs to hand (4 upgraded). REUSE the game class MegaCrit.Sts2.Core.Models.Cards.Shiv via `await Shiv.CreateInHand(Owner, DynamicVars.Cards.IntValue, Owner.Creature.CombatState, Owner);`. Var: CardsVar(3), upgrade Cards +1. Do NOT write your own Shiv card.

## CloakAndDagger  (class name = `CloakAndDagger`, loc key `SPIRE1-CLOAK_AND_DAGGER`)
- StS1 id: `Cloak And Dagger`, official name: `Cloak and Dagger`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base values: Block=6, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Gain !B! Block. NL Add !M! *Shiv into your hand.`
- official upgraded description: `Gain !B! Block. NL Add !M! *Shivs into your hand.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Cloak and Dagger"
- IMPL: Skill. Gain 6 Block AND add 1 Shiv (2 upgraded; Block stays 6). Vars: BlockVar(6), CardsVar(1); OnUpgrade upgrades ONLY Cards by 1. Shiv via Shiv.CreateInHand.

## DodgeAndRoll  (class name = `DodgeAndRoll`, loc key `SPIRE1-DODGE_AND_ROLL`)
- StS1 id: `Dodge and Roll`, official name: `Dodge and Roll`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base values: Block=4, StS1 flags: none
- upgrade deltas: upgradeBlock=2
- official StS1 description: `Gain !B! Block. NL Next turn, gain !B! Block.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Dodge and Roll"
- IMPL: Skill. Gain 4 Block now, and gain 4 Block again at the start of next turn (6/6 upgraded). FIRST search the decompiled powers folder for an existing next-turn-block power (e.g. NextTurnBlockPower). If none exists, write mod power Spire1Code/Powers/NextTurnBlockPower.cs : CustomPowerModel (Buff, Counter) that on AfterSideTurnStart grants Amount Block to its owner and then removes itself (verify the exact power-removal API in the decompiled sources).

## FlyingKnee  (class name = `FlyingKnee`, loc key `SPIRE1-FLYING_KNEE`)
- StS1 id: `Flying Knee`, official name: `Flying Knee`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base values: Damage=8, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official StS1 description: `Deal !D! damage. NL Next turn, gain [G].`
- StS2 name collision: no -> plain title "Flying Knee"
- IMPL: Attack 8 (+3). Then next turn gain 1 Energy: `await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);`. Vars: DamageVar(8), EnergyVar(1).

## Outmaneuver  (class name = `Outmaneuver`, loc key `SPIRE1-OUTMANEUVER`)
- StS1 id: `Outmaneuver`, official name: `Outmaneuver`
- type=SKILL, rarity=COMMON, cost=1, target=NONE, base values: none, StS1 flags: none
- upgrade deltas: none
- official StS1 description: `Next turn, NL gain [G] [G].`
- official upgraded description: `Next turn, NL gain [G] [G] [G].`
- StS2 name collision: YES -> localization title MUST be "StS1 - Outmaneuver"
- IMPL: Skill, no damage/block. Next turn gain 2 Energy (3 upgraded) via EnergyNextTurnPower (same call as FlyingKnee). Var: EnergyVar(2), upgrade Energy +1.

## PiercingWail  (class name = `PiercingWail`, loc key `SPIRE1-PIERCING_WAIL`)
- StS1 id: `PiercingWail`, official name: `Piercing Wail`
- type=SKILL, rarity=COMMON, cost=1, target=ALL_ENEMY, base values: MagicNumber=6, StS1 flags: exhaust
- upgrade deltas: upgradeMagicNumber=2
- official StS1 description: `ALL enemies lose !M! Strength this turn. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Piercing Wail"
- IMPL: Skill, ALL enemies lose 6 Strength THIS TURN ONLY (8 upgraded), Exhaust. Implement with the same BaseLib temporary-power approach the mod already uses in Cards/Flex.cs (CustomTemporaryPowerModelWrapper<TCard, StrengthPower>) but with a NEGATIVE amount applied to every enemy in Owner.Creature.CombatState.HittableEnemies. Read Flex.cs first. If a negative temporary strength cannot be expressed, FLAG it instead of faking a permanent Strength loss.

## SneakyStrike  (class name = `SneakyStrike`, loc key `SPIRE1-SNEAKY_STRIKE`)
- StS1 id: `Underhanded Strike`, official name: `Sneaky Strike`
- type=ATTACK, rarity=COMMON, cost=2, target=ENEMY, base values: Damage=12, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Deal !D! damage. NL If you have discarded a card this turn, NL gain [G] [G].`
- StS2 name collision: no -> plain title "Sneaky Strike"
- IMPL: Attack 12 (+4). If you have discarded a card this turn, gain 2 Energy immediately (`await PlayerCmd.GainEnergy(2, Owner)`). Discard-count query pattern (verify in decompiled sources): CombatManager.Instance.History.Entries.OfType<CardDiscardedEntry>().Any(e => e.HappenedThisTurn(CombatState) && e.Actor == Owner.Creature). Vars: DamageVar(12), EnergyVar(2) (Energy is NOT upgraded).

## SuckerPunch  (class name = `SuckerPunch`, loc key `SPIRE1-SUCKER_PUNCH`)
- StS1 id: `Sucker Punch`, official name: `Sucker Punch`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base values: Damage=7, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=2, upgradeMagicNumber=1
- official StS1 description: `Deal !D! damage. NL Apply !M! Weak.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Sucker Punch"
- IMPL: Attack 7 (+2) + apply 1 Weak (+1). Same shape as the existing mod card Cards/Clothesline.cs.
