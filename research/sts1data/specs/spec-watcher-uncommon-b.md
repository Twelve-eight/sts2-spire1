# Watcher UNCOMMON part B (18) — StS1 vanilla

All numbers extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(WatcherCardPool))]`.

## Pray  (class `Pray`, loc key `SPIRE1-PRAY`)
- StS1 id `Pray`, official name `Pray`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Mantra. NL Shuffle an *Insight into your draw pile.`
- StS2 name collision: no -> plain title "Pray"
- IMPL: Skill cost 1: gain 3 Mantra (+1) and shuffle an Insight into your draw pile.

## ReachHeaven  (class `ReachHeaven`, loc key `SPIRE1-REACH_HEAVEN`)
- StS1 id `ReachHeaven`, official name `Reach Heaven`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base: Damage=10, StS1 flags: none
- upgrade deltas: upgradeDamage=5
- official description: `Deal !D! damage. NL Shuffle a NL *Through *Violence into your draw pile.`
- StS2 name collision: no -> plain title "Reach Heaven"
- IMPL: Attack cost 2: 10 damage (+5), then shuffle a ThroughViolence into your draw pile.

## Rushdown  (class `Rushdown`, loc key `SPIRE1-RUSHDOWN`)
- StS1 id `Adaptation`, official name `Rushdown`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official description: `Whenever you enter Wrath, draw !M! cards.`
- official upgraded description: `DEPRECATED`
- StS2 name collision: no -> plain title "Rushdown"
- IMPL: Power cost 1 (0 upgraded): whenever you ENTER WRATH, draw 2 cards. Custom power implementing `IOnStanceChanged`, reacting only when the NEW stance is WrathPower.

## Sanctity  (class `Sanctity`, loc key `SPIRE1-SANCTITY`)
- StS1 id `Sanctity`, official name `Sanctity`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: Block=6, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL If the last card played this combat was a Skill, draw !M! cards.`
- StS2 name collision: no -> plain title "Sanctity"
- IMPL: Skill cost 1: gain 6 Block (+3); if the LAST card played this combat was a Skill, draw 2 cards.

## SandsOfTime  (class `SandsOfTime`, loc key `SPIRE1-SANDS_OF_TIME`)
- StS1 id `SandsOfTime`, official name `Sands of Time`
- type=ATTACK, rarity=UNCOMMON, cost=4, target=ENEMY, base: Damage=20, StS1 flags: selfRetain
- upgrade deltas: upgradeDamage=6
- official description: `Retain. NL Deal !D! damage. NL When Retained, lower its cost by 1 this combat.`
- StS2 name collision: no -> plain title "Sands of Time"
- IMPL: Attack cost 4, Retain: 20 damage (+6); when RETAINED, lower its cost by 1 for the rest of the combat (EnergyCost.AddThisCombat(-1, reduceOnly: true)). Retain observation via the card's AfterFlush hook.

## SignatureMove  (class `SignatureMove`, loc key `SPIRE1-SIGNATURE_MOVE`)
- StS1 id `SignatureMove`, official name `Signature Move`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base: Damage=30, StS1 flags: none
- upgrade deltas: upgradeDamage=10
- official description: `Can only be played if this is the only Attack in your hand. NL Deal !D! damage.`
- StS2 name collision: no -> plain title "Signature Move"
- IMPL: Attack cost 2: 30 damage (+10); can ONLY be played if it is the only Attack in your hand: `protected override bool IsPlayable => CardPile.GetCards(Owner, PileType.Hand).Count(c => c.Type == CardType.Attack) <= 1;` (verify against mod Cards/Clash.cs).

## SimmeringFury  (class `SimmeringFury`, loc key `SPIRE1-SIMMERING_FURY`)
- StS1 id `Vengeance`, official name `Simmering Fury`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `At the start of your next turn, enter Wrath and draw !M! cards.`
- StS2 name collision: no -> plain title "Simmering Fury"
- IMPL: Skill cost 1: at the start of your NEXT turn, enter Wrath and draw 2 cards (+1). Custom power that fires once on the next turn start then removes itself.

## Study  (class `Study`, loc key `SPIRE1-STUDY`)
- StS1 id `Study`, official name `Study`
- type=POWER, rarity=UNCOMMON, cost=2, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeBaseCost=1
- official description: `At the end of your turn, shuffle an *Insight into your draw pile.`
- StS2 name collision: no -> plain title "Study"
- IMPL: Power cost 2 (upgrade: cost 1): at the END of your turn, shuffle an Insight into your draw pile. Custom power with AfterSideTurnEnd.

## Swivel  (class `Swivel`, loc key `SPIRE1-SWIVEL`)
- StS1 id `Swivel`, official name `Swivel`
- type=SKILL, rarity=UNCOMMON, cost=2, target=SELF, base: Block=8, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL The next Attack you play costs 0.`
- StS2 name collision: no -> plain title "Swivel"
- IMPL: Skill cost 2: gain 8 Block (+3); the NEXT Attack you play costs 0 this combat. Custom one-shot power that sets the next played Attack to free (SetToFreeThisTurn or a cost override) and then expires; verify which API makes a single card free and state it.

## TalkToTheHand  (class `TalkToTheHand`, loc key `SPIRE1-TALK_TO_THE_HAND`)
- StS1 id `TalkToTheHand`, official name `Talk to the Hand`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=5, MagicNumber=2, StS1 flags: exhaust
- upgrade deltas: upgradeDamage=2, upgradeMagicNumber=1
- official description: `Deal !D! damage. NL Whenever you attack this enemy, gain !M! Block. NL Exhaust.`
- StS2 name collision: no -> plain title "Talk to the Hand"
- IMPL: Attack cost 1, Exhaust: 5 damage (+2); whenever you attack THIS enemy, gain 2 Block (+1). Custom power on the ENEMY (Debuff or Buff on that creature) whose damage-received hook grants the ATTACKER 2 Block; copy the hook shape from mod Powers/StaticDischargePower.cs.

## Tantrum  (class `Tantrum`, loc key `SPIRE1-TANTRUM`)
- StS1 id `Tantrum`, official name `Tantrum`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=3, MagicNumber=3, StS1 flags: shuffleBackIntoDrawPile
- upgrade deltas: upgradeMagicNumber=1
- official description: `Deal !D! damage NL !M! times. NL Enter Wrath. NL Shuffle this card into your draw pile.`
- StS2 name collision: no -> plain title "Tantrum"
- IMPL: Attack cost 1: 3 damage (+0) THREE times (magic 3, +1 on upgrade = 4 hits), then ENTER Wrath, then shuffle this card into your draw pile (StS1 flag shuffleBackIntoDrawPile). Use the repeat count as a RepeatVar so the text updates on upgrade.

## Wallop  (class `Wallop`, loc key `SPIRE1-WALLOP`)
- StS1 id `Wallop`, official name `Wallop`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base: Damage=9, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. NL Gain Block equal to unblocked damage dealt.`
- StS2 name collision: no -> plain title "Wallop"
- IMPL: Attack cost 2: 9 damage (+3) and gain Block equal to the UNBLOCKED damage dealt. Read the attack result: AttackCommand.Results -> DamageResult.UnblockedDamage (mod precedent: Cards/Sunder.cs reads WasTargetKilled from the same structure).

## WaveOfTheHand  (class `WaveOfTheHand`, loc key `SPIRE1-WAVE_OF_THE_HAND`)
- StS1 id `WaveOfTheHand`, official name `Wave of the Hand`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Whenever you gain Block this turn, apply !M! Weak to ALL enemies.`
- StS2 name collision: no -> plain title "Wave of the Hand"
- IMPL: Skill cost 1: whenever you GAIN BLOCK this turn, apply 1 Weak (+1) to ALL enemies. Custom power with a block-gain hook (search the decompiled powers for an after-block-gained hook); expires at end of turn. FLAG if no block-gain hook exists.

## Weave  (class `Weave`, loc key `SPIRE1-WEAVE`)
- StS1 id `Weave`, official name `Weave`
- type=ATTACK, rarity=UNCOMMON, cost=0, target=ENEMY, base: Damage=4, StS1 flags: none
- upgrade deltas: upgradeDamage=2
- official description: `Deal !D! damage. NL Whenever you Scry, return this from the discard pile to your Hand.`
- StS2 name collision: no -> plain title "Weave"
- IMPL: Attack cost 0, 4 damage (+2): whenever you SCRY, return this from the discard pile to your hand. Card implements BaseLib's `IAfterScryed` and moves itself from discard to hand.

## WheelKick  (class `WheelKick`, loc key `SPIRE1-WHEEL_KICK`)
- StS1 id `WheelKick`, official name `Wheel Kick`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base: Damage=15, MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeDamage=5
- official description: `Deal !D! damage. NL Draw !M! cards.`
- StS2 name collision: no -> plain title "Wheel Kick"
- IMPL: Attack cost 2: 15 damage (+5) then draw 2 cards.

## WindmillStrike  (class `WindmillStrike`, loc key `SPIRE1-WINDMILL_STRIKE`)
- StS1 id `WindmillStrike`, official name `Windmill Strike`
- type=ATTACK, rarity=UNCOMMON, cost=2, target=ENEMY, base: Damage=7, MagicNumber=4, StS1 flags: selfRetain
- upgrade deltas: upgradeDamage=3, upgradeMagicNumber=1
- official description: `Retain. NL Deal !D! damage. NL When Retained, increase its damage by !M! this combat.`
- StS2 name collision: no -> plain title "Windmill Strike"
- IMPL: Attack cost 2, Retain: 7 damage (+3); when RETAINED, increase its damage by 4 (+1) for the rest of the combat. Retain observation via the card's AfterFlush hook; per-combat growth like Rampage.cs.

## Worship  (class `Worship`, loc key `SPIRE1-WORSHIP`)
- StS1 id `Worship`, official name `Worship`
- type=SKILL, rarity=UNCOMMON, cost=2, target=SELF, base: MagicNumber=5, StS1 flags: none
- upgrade deltas: flag:selfRetain=true
- official description: `Gain !M! Mantra.`
- official upgraded description: `Retain. NL Gain !M! Mantra.`
- StS2 name collision: no -> plain title "Worship"
- IMPL: Skill cost 2: gain 5 Mantra (upgrade ADDS Retain, amount unchanged).

## WreathOfFlame  (class `WreathOfFlame`, loc key `SPIRE1-WREATH_OF_FLAME`)
- StS1 id `WreathOfFlame`, official name `Wreath of Flame`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=5, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=3
- official description: `Your next Attack deals !M! additional damage.`
- StS2 name collision: no -> plain title "Wreath of Flame"
- IMPL: Skill cost 1: your NEXT Attack deals 5 additional damage (+3). Custom power (Counter) that adds its Amount to the next attack's damage via ModifyDamageAdditive and then removes itself.
