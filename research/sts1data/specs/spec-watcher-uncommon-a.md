# Watcher UNCOMMON part A (17) — StS1 vanilla

All numbers extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(WatcherCardPool))]`.

## BattleHymn  (class `BattleHymn`, loc key `SPIRE1-BATTLE_HYMN`)
- StS1 id `BattleHymn`, official name `Battle Hymn`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official description: `At the start of each turn, add a *Smite into your hand.`
- official upgraded description: `Innate. NL At the start of each turn, add a *Smite into your hand.`
- StS2 name collision: no -> plain title "Battle Hymn"
- IMPL: Power cost 1: at the start of each turn add a Smite into your hand (upgrade adds Innate). Custom power `BattleHymnPower` with AfterSideTurnStart adding the mod token card Smite to hand.

## CarveReality  (class `CarveReality`, loc key `SPIRE1-CARVE_REALITY`)
- StS1 id `CarveReality`, official name `Carve Reality`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=6, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage. NL Add a *Smite into your hand.`
- StS2 name collision: no -> plain title "Carve Reality"
- IMPL: Attack 6 (+4): add a Smite into your hand.

## Collect  (class `Collect`, loc key `SPIRE1-COLLECT`)
- StS1 id `Collect`, official name `Collect`
- type=SKILL, rarity=UNCOMMON, cost=-1, target=SELF, base: none, StS1 flags: exhaust
- upgrade deltas: none
- official description: `Put a *Miracle+ into your hand at the start of your next X turns. NL Exhaust.`
- official upgraded description: `Put a *Miracle+ into your hand at the start of your next X+1 turns. NL Exhaust.`
- StS2 name collision: no -> plain title "Collect"
- IMPL: Skill, X-COST, Exhaust: at the start of each of your next X turns, put a Miracle+ (UPGRADED Miracle) into your hand. Custom power `CollectPower` (Counter) that at turn start adds one upgraded Miracle and decrements; apply with amount = ResolveEnergyXValue() (+1 upgraded, per StS1 upgrade).

## Conclude  (class `Conclude`, loc key `SPIRE1-CONCLUDE`)
- StS1 id `Conclude`, official name `Conclude`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ALL_ENEMY, base: Damage=12, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage to ALL enemies. NL End your turn.`
- StS2 name collision: no -> plain title "Conclude"
- IMPL: Attack cost 1: 12 damage (+4) to ALL enemies, then END YOUR TURN via `PlayerCmd.EndTurn(...)` (exact signature at .tmp/dllsrc/MegaCrit.Sts2.Core.Commands/PlayerCmd.cs:279).

## DeceiveReality  (class `DeceiveReality`, loc key `SPIRE1-DECEIVE_REALITY`)
- StS1 id `DeceiveReality`, official name `Deceive Reality`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: Block=4, StS1 flags: none
- upgrade deltas: upgradeBlock=3
- official description: `Gain !B! Block. NL Add a *Safety into NL your hand.`
- StS2 name collision: no -> plain title "Deceive Reality"
- IMPL: Skill: gain 4 Block (+3) and add a Safety into your hand.

## EmptyMind  (class `EmptyMind`, loc key `SPIRE1-EMPTY_MIND`)
- StS1 id `EmptyMind`, official name `Empty Mind`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Draw !M! cards. NL Exit your Stance.`
- official upgraded description: `Draw !M! cards. NL Exit your Stance.`
- StS2 name collision: no -> plain title "Empty Mind"
- IMPL: Skill: draw 2 cards (+1) then EXIT your stance.

## Fasting  (class `Fasting`, loc key `SPIRE1-FASTING`)
- StS1 id `Fasting2`, official name `Fasting`
- type=POWER, rarity=UNCOMMON, cost=2, target=SELF, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Strength. NL Gain !M! Dexterity. NL Gain 1 less [W] at the start of each turn.`
- StS2 name collision: no -> plain title "Fasting"
- IMPL: Power cost 2: gain 3 Strength (+1) and 3 Dexterity (+1), and gain 1 LESS Energy at the start of each turn. The energy penalty needs a custom power hook that reduces per-turn energy; search the decompiled powers for an energy-gain modifier (e.g. ModifyEnergyGain used by PlayerCmd.GainEnergy) and use it. If no such hook exists, FLAG the card.

## FearNoEvil  (class `FearNoEvil`, loc key `SPIRE1-FEAR_NO_EVIL`)
- StS1 id `FearNoEvil`, official name `Fear No Evil`
- type=ATTACK, rarity=UNCOMMON, cost=1, target=ENEMY, base: Damage=8, StS1 flags: none
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. NL If the enemy intends to Attack, enter Calm.`
- StS2 name collision: no -> plain title "Fear No Evil"
- IMPL: Attack 8 (+3): if the target INTENDS TO ATTACK, enter Calm. Intent check `play.Target?.Monster?.IntendsToAttack == true`.

## ForeignInfluence  (class `ForeignInfluence`, loc key `SPIRE1-FOREIGN_INFLUENCE`)
- StS1 id `ForeignInfluence`, official name `Foreign Influence`
- type=SKILL, rarity=UNCOMMON, cost=0, target=NONE, base: none, StS1 flags: exhaust
- upgrade deltas: none
- official description: `Choose 1 of 3 Attacks of any color to add into your hand. NL Exhaust.`
- official upgraded description: `Choose 1 of 3 Attacks of any color to add into your hand. NL It costs 0 this turn. NL Exhaust.`
- StS2 name collision: no -> plain title "Foreign Influence"
- IMPL: Skill cost 0, Exhaust: choose 1 of 3 Attacks OF ANY COLOR to add into your hand (upgraded: the chosen card costs 0 this turn — verify the vanilla upgrade text before implementing). Needs a 3-option card choice from all character pools; use the CardFactory generation the mod's Cards/Distraction.cs uses but across every card pool, then a selection. If a 'choose one of N generated cards' API does not exist, FLAG.

## Foresight  (class `Foresight`, loc key `SPIRE1-FORESIGHT`)
- StS1 id `Wireheading`, official name `Foresight`
- type=POWER, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `At the start of your turn, Scry !M!.`
- StS2 name collision: no -> plain title "Foresight"
- IMPL: Power cost 1: at the start of your turn, Scry 3 (+1). Custom power with AfterSideTurnStart calling BaseLib ScryCmd.

## Indignation  (class `Indignation`, loc key `SPIRE1-INDIGNATION`)
- StS1 id `Indignation`, official name `Indignation`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=2
- official description: `If you are in Wrath, apply !M! Vulnerable to ALL enemies, otherwise enter Wrath.`
- StS2 name collision: no -> plain title "Indignation"
- IMPL: Skill cost 1: if you are in Wrath, apply 3 Vulnerable (+2) to ALL enemies; otherwise ENTER Wrath.

## InnerPeace  (class `InnerPeace`, loc key `SPIRE1-INNER_PEACE`)
- StS1 id `InnerPeace`, official name `Inner Peace`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `If you are in Calm, draw !M! cards, otherwise enter Calm.`
- StS2 name collision: no -> plain title "Inner Peace"
- IMPL: Skill cost 1: if you are in Calm, draw 3 cards (+1); otherwise ENTER Calm.

## LikeWater  (class `LikeWater`, loc key `SPIRE1-LIKE_WATER`)
- StS1 id `LikeWater`, official name `Like Water`
- type=POWER, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=5, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=2
- official description: `At the end of your turn, if you are in Calm, gain !M! Block.`
- StS2 name collision: no -> plain title "Like Water"
- IMPL: Power cost 1: at the END of your turn, if you are in Calm, gain 5 Block (+2). Custom power using AfterSideTurnEnd + StanceCmd.IsIn<CalmPower>.

## Meditate  (class `Meditate`, loc key `SPIRE1-MEDITATE`)
- StS1 id `Meditate`, official name `Meditate`
- type=SKILL, rarity=UNCOMMON, cost=1, target=NONE, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Put a card from your discard pile into your hand and Retain it. NL Enter Calm. NL End your turn.`
- official upgraded description: `Put 2 cards from your discard pile into your hand and Retain them. NL Enter Calm. NL End your turn.`
- StS2 name collision: no -> plain title "Meditate"
- IMPL: Skill cost 1: put 1 card (+1) from your discard pile into your hand and give it Retain (`CardCmd.ApplySingleTurnRetain`), then ENTER Calm, then END YOUR TURN (`PlayerCmd.EndTurn`).

## MentalFortress  (class `MentalFortress`, loc key `SPIRE1-MENTAL_FORTRESS`)
- StS1 id `MentalFortress`, official name `Mental Fortress`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=4, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=2
- official description: `Whenever you change Stances, gain !M! Block.`
- StS2 name collision: no -> plain title "Mental Fortress"
- IMPL: Power cost 1: whenever you CHANGE STANCES, gain 4 Block (+2). Custom power implementing `IOnStanceChanged`.

## Nirvana  (class `Nirvana`, loc key `SPIRE1-NIRVANA`)
- StS1 id `Nirvana`, official name `Nirvana`
- type=POWER, rarity=UNCOMMON, cost=1, target=SELF, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Whenever you Scry, gain !M! Block.`
- StS2 name collision: no -> plain title "Nirvana"
- IMPL: Power cost 1: whenever you SCRY, gain 3 Block (+1). Implement via BaseLib's scry hook interface `IAfterScryed` (research/BaseLib-StS2/Hooks/IAfterScryed.cs) on a custom power.

## Perseverance  (class `Perseverance`, loc key `SPIRE1-PERSEVERANCE`)
- StS1 id `Perseverance`, official name `Perseverance`
- type=SKILL, rarity=UNCOMMON, cost=1, target=SELF, base: Block=5, MagicNumber=2, StS1 flags: selfRetain
- upgrade deltas: upgradeBlock=2, upgradeMagicNumber=1
- official description: `Retain. NL Gain !B! Block. NL When Retained, increase its Block by !M! this combat.`
- StS2 name collision: no -> plain title "Perseverance"
- IMPL: Skill cost 1, Retain: gain 5 Block (+2); when RETAINED, increase its Block by 2 (+1) for the rest of the combat. Retain observation: override the card's `AfterFlush` hook (retention is decided in CombatManager.FlushPlayerHand via CardModel.ShouldRetainThisTurn and Hook.AfterFlush delivers the retained-card list to every listener). Per-combat value growth: mod Cards/Rampage.cs pattern.
