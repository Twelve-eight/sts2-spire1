# Watcher COMMON cards without stance dependency (11) — StS1 vanilla

All numbers extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(WatcherCardPool))]`.

## BowlingBash  (class `BowlingBash`, loc key `SPIRE1-BOWLING_BASH`)
- StS1 id `BowlingBash`, official name `Bowling Bash`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage for each enemy in combat.`
- StS2 name collision: no -> plain title "Bowling Bash"
- IMPL: Attack 7 (+3): hits ONCE PER ENEMY in combat (hitCount = CombatState.HittableEnemies.Count), all hits on the chosen single target.

## Consecrate  (class `Consecrate`, loc key `SPIRE1-CONSECRATE`)
- StS1 id `Consecrate`, official name `Consecrate`
- type=ATTACK, rarity=COMMON, cost=0, target=ALL_ENEMY, base: Damage=5, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage to ALL enemies.`
- StS2 name collision: no -> plain title "Consecrate"
- IMPL: Attack 5 (+3) to ALL enemies (TargetType.AllEnemies).

## CrushJoints  (class `CrushJoints`, loc key `SPIRE1-CRUSH_JOINTS`)
- StS1 id `CrushJoints`, official name `Crush Joints`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=8, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=2, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL If the last card played this combat was a Skill, apply !M! Vulnerable.`
- StS2 name collision: no -> plain title "Crush Joints"
- IMPL: Attack 8 (+2): if the LAST card played this combat was a Skill, apply 1 Vulnerable (+1). Last-card check: CombatManager.Instance.History.CardPlaysFinished, take the most recent entry before this play, test CardPlay.Card.Type == CardType.Skill (see mod Cards/Finisher.cs for the history API).

## Evaluate  (class `Evaluate`, loc key `SPIRE1-EVALUATE`)
- StS1 id `Evaluate`, official name `Evaluate`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=6, StS1 flags: none
- upgrade deltas: upgradeBlock=4
- official description: `Gain !B! Block. NL Shuffle an *Insight into your draw pile.`
- official upgraded description: `Gain !B! Block. NL Shuffle an *Insight+ into your draw pile.`
- StS2 name collision: no -> plain title "Evaluate"
- IMPL: Skill: gain 6 Block (+4) then shuffle one Insight into your draw pile. Insight is a mod token card written by the token slice: CardPileCmd.AddToCombatAndPreview<Insight>(Owner.Creature, PileType.Draw, 1, Owner).

## FlyingSleeves  (class `FlyingSleeves`, loc key `SPIRE1-FLYING_SLEEVES`)
- StS1 id `FlyingSleeves`, official name `Flying Sleeves`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=4, StS1 flags: selfRetain
- upgrade deltas: upgradeDamage=2
- official description: `Retain. NL Deal !D! damage twice.`
- StS2 name collision: no -> plain title "Flying Sleeves"
- IMPL: Attack 4 (+2) hitting TWICE, with CardKeyword.Retain.

## FollowUp  (class `FollowUp`, loc key `SPIRE1-FOLLOW_UP`)
- StS1 id `FollowUp`, official name `Follow-Up`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=7, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage. NL If the last card played this combat was an Attack, gain [W].`
- StS2 name collision: no -> plain title "Follow-Up"
- IMPL: Attack 7 (+4): if the LAST card played this combat was an Attack, gain 1 Energy. Same history check as CrushJoints.

## JustLucky  (class `JustLucky`, loc key `SPIRE1-JUST_LUCKY`)
- StS1 id `JustLucky`, official name `Just Lucky`
- type=ATTACK, rarity=COMMON, cost=0, target=ENEMY, base: Damage=3, Block=2, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=1, upgradeBlock=1, upgradeMagicNumber=1
- official description: `Scry !M!. NL Gain !B! Block. NL Deal !D! damage.`
- StS2 name collision: no -> plain title "Just Lucky"
- IMPL: Skill-like Attack cost 0: Scry 1 (+1), then gain 2 Block (+1), then deal 3 damage (+1) — in that order. Scry via BaseLib: `await ScryCmd.Execute(choiceContext, Owner, DynamicVars.Scry().IntValue);` and declare the displayed value with BaseLib's `ScryVar` (see research/BaseLib-StS2/Cards/Variables/ScryVar.cs and Commands/ScryCmd.cs).

## PressurePoints  (class `PressurePoints`, loc key `SPIRE1-PRESSURE_POINTS`)
- StS1 id `PathToVictory`, official name `Pressure Points`
- type=SKILL, rarity=COMMON, cost=1, target=ENEMY, base: MagicNumber=8, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=3
- official description: `Apply !M! *Mark. NL ALL enemies lose HP equal to their *Mark.`
- StS2 name collision: no -> plain title "Pressure Points"
- IMPL: Skill cost 1, target enemy: apply 8 Mark (+3), then ALL enemies lose HP equal to their own Mark. StS2 has NO Mark power, so write `mod/Spire1Code/Powers/MarkPower.cs` (Debuff, Counter) as a plain counter; the card applies it to the target and then, for every hittable enemy with MarkPower, deals HP loss equal to that enemy's Mark amount via CreatureCmd.Damage(..., ValueProp.Unblockable | ValueProp.Unpowered, ...). Mark persists across turns in StS1 — do not expire it.

## Protect  (class `Protect`, loc key `SPIRE1-PROTECT`)
- StS1 id `Protect`, official name `Protect`
- type=SKILL, rarity=COMMON, cost=2, target=SELF, base: Block=12, StS1 flags: selfRetain
- upgrade deltas: upgradeBlock=4
- official description: `Retain. NL Gain !B! Block.`
- StS2 name collision: no -> plain title "Protect"
- IMPL: Skill cost 2: gain 12 Block (+4), CardKeyword.Retain.

## SashWhip  (class `SashWhip`, loc key `SPIRE1-SASH_WHIP`)
- StS1 id `SashWhip`, official name `Sash Whip`
- type=ATTACK, rarity=COMMON, cost=1, target=ENEMY, base: Damage=8, MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeDamage=2, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL If the last card played this combat was an Attack, apply !M! Weak.`
- StS2 name collision: no -> plain title "Sash Whip"
- IMPL: Attack 8 (+2): if the LAST card played this combat was an Attack, apply 1 Weak (+1). Same history check as CrushJoints.

## ThirdEye  (class `ThirdEye`, loc key `SPIRE1-THIRD_EYE`)
- StS1 id `ThirdEye`, official name `Third Eye`
- type=SKILL, rarity=COMMON, cost=1, target=SELF, base: Block=7, MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeBlock=2, upgradeMagicNumber=2
- official description: `Gain !B! Block. NL Scry !M!.`
- StS2 name collision: no -> plain title "Third Eye"
- IMPL: Skill: gain 7 Block (+2) then Scry 3 (+2). Scry via BaseLib ScryCmd + ScryVar.
