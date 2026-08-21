# Watcher RARE cards (19) — StS1 vanilla

All numbers extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. Pool attribute for every card: `[Pool(typeof(WatcherCardPool))]`.

## Alpha  (class `Alpha`, loc key `SPIRE1-ALPHA`)
- StS1 id `Alpha`, official name `Alpha`
- type=SKILL, rarity=RARE, cost=1, target=NONE, base: none, StS1 flags: exhaust
- upgrade deltas: flag:isInnate=true
- official description: `Shuffle a *Beta into your draw pile. NL Exhaust.`
- official upgraded description: `Innate. NL Shuffle a *Beta into your draw pile. NL Exhaust.`
- StS2 name collision: no -> plain title "Alpha"
- IMPL: Skill cost 1, Exhaust (upgrade adds Innate): shuffle a Beta into your draw pile.

## Blasphemy  (class `Blasphemy`, loc key `SPIRE1-BLASPHEMY`)
- StS1 id `Blasphemy`, official name `Blasphemy`
- type=SKILL, rarity=RARE, cost=1, target=SELF, base: none, StS1 flags: exhaust
- upgrade deltas: flag:selfRetain=true
- official description: `Enter Divinity. NL Die next turn. NL Exhaust.`
- official upgraded description: `Retain. NL Enter Divinity. NL Die next turn. NL Exhaust.`
- StS2 name collision: no -> plain title "Blasphemy"
- IMPL: Skill cost 1, Exhaust (upgrade adds Retain): enter Divinity, then DIE at the start of your next turn. Implement the death part with a custom power that at the owner's next turn start reduces the player to 0 HP through the normal damage/HP-loss command (never a direct field write). If lethal self-damage cannot be expressed safely, FLAG the card.

## Brilliance  (class `Brilliance`, loc key `SPIRE1-BRILLIANCE`)
- StS1 id `Brilliance`, official name `Brilliance`
- type=ATTACK, rarity=RARE, cost=1, target=ENEMY, base: Damage=12, MagicNumber=0, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official description: `Deal !D! damage. NL Deals additional damage equal to Mantra gained this combat.`
- StS2 name collision: no -> plain title "Brilliance"
- IMPL: Attack cost 1: 12 damage (+4) plus additional damage equal to the MANTRA GAINED THIS COMBAT. Needs a per-combat total of Mantra gained: have the stance infrastructure's MantraPower or StanceCmd track a combat-scoped total, or count it yourself from a custom power. Coordinate with the stance infrastructure via hub if the counter is missing rather than inventing a second source of truth.

## ConjureBlade  (class `ConjureBlade`, loc key `SPIRE1-CONJURE_BLADE`)
- StS1 id `ConjureBlade`, official name `Conjure Blade`
- type=SKILL, rarity=RARE, cost=-1, target=SELF, base: none, StS1 flags: exhaust
- upgrade deltas: none
- official description: `Shuffle an *Expunger into your draw pile. NL Exhaust.`
- official upgraded description: `Shuffle an *Expunger with X+1 into your draw pile. NL Exhaust.`
- StS2 name collision: no -> plain title "Conjure Blade"
- IMPL: Skill, X-COST, Exhaust: shuffle an Expunger into your draw pile whose hit count equals X. The token slice gives Expunger a settable repeat count; set it before adding the card.

## DeusExMachina  (class `DeusExMachina`, loc key `SPIRE1-DEUS_EX_MACHINA`)
- StS1 id `DeusExMachina`, official name `Deus Ex Machina`
- type=SKILL, rarity=RARE, cost=-2, target=SELF, base: MagicNumber=2, StS1 flags: exhaust
- upgrade deltas: upgradeMagicNumber=1
- official description: `Unplayable. NL When you draw this card, add !M! *Miracles to your hand and Exhaust.`
- StS2 name collision: no -> plain title "Deus Ex Machina"
- IMPL: Skill, UNPLAYABLE, Exhaust: when you DRAW this card, add 2 Miracles (+1) to your hand and exhaust it. Use the card-level AfterCardDrawn hook (mod precedent Cards/EndlessAgony.cs and Cards/Void.cs).

## DevaForm  (class `DevaForm`, loc key `SPIRE1-DEVA_FORM`)
- StS1 id `DevaForm`, official name `Deva Form`
- type=POWER, rarity=RARE, cost=3, target=SELF, base: MagicNumber=1, StS1 flags: isEthereal
- upgrade deltas: flag:isEthereal=false
- official description: `Ethereal. NL At the start of your turn, gain [W] NL and increase this gain by !M!.`
- official upgraded description: `At the start of your turn, gain [W] NL and increase this gain by !M!.`
- StS2 name collision: no -> plain title "Deva Form"
- IMPL: Power cost 3, Ethereal (upgrade removes Ethereal): at the start of your turn gain 1 Energy, and INCREASE that gain by 1 each turn. Custom power holding an escalating amount.

## Devotion  (class `Devotion`, loc key `SPIRE1-DEVOTION`)
- StS1 id `Devotion`, official name `Devotion`
- type=POWER, rarity=RARE, cost=1, target=NONE, base: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `At the start of your turn, gain !M! Mantra.`
- StS2 name collision: no -> plain title "Devotion"
- IMPL: Power cost 1: at the start of your turn, gain 2 Mantra (+1). Custom power calling StanceCmd.GainMantra.

## Discipline  (class `Discipline`, loc key `SPIRE1-DISCIPLINE`)
- StS1 id `Discipline`, official name `DEPRECATED Discipline`
- type=POWER, rarity=RARE, cost=2, target=SELF, base: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=1
- official description: `If you end your turn with unused [W] , draw that many additional cards next turn.`
- StS2 name collision: no -> plain title "DEPRECATED Discipline"
- IMPL: Power cost 2 (upgrade: cost 1): if you END your turn with unused Energy, draw that many additional cards next turn. Custom power reading the player's remaining energy at turn end and applying DrawCardsNextTurnPower with that amount.

## Establishment  (class `Establishment`, loc key `SPIRE1-ESTABLISHMENT`)
- StS1 id `Establishment`, official name `Establishment`
- type=POWER, rarity=RARE, cost=1, target=SELF, base: MagicNumber=1, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official description: `Whenever a card is Retained, reduce its cost by !M! this combat.`
- official upgraded description: `Innate. NL Whenever a card is Retained, reduce its cost by !M! this combat.`
- StS2 name collision: no -> plain title "Establishment"
- IMPL: Power cost 1 (upgrade adds Innate): whenever a card is RETAINED, reduce its cost by 1 for the rest of the combat. Custom power using the AfterFlush retained-card list.

## Judgement  (class `Judgement`, loc key `SPIRE1-JUDGEMENT`)
- StS1 id `Judgement`, official name `Judgment`
- type=SKILL, rarity=RARE, cost=1, target=ENEMY, base: MagicNumber=30, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=10
- official description: `If the enemy has !M! or less HP, set their NL HP to 0.`
- StS2 name collision: no -> plain title "Judgment"
- IMPL: Skill cost 1, target enemy: if the enemy has 30 HP or less (+10), set their HP to 0. Use the normal lethal-damage command against the target rather than writing HP directly; the threshold must be a DynamicVar.

## LessonLearned  (class `LessonLearned`, loc key `SPIRE1-LESSON_LEARNED`)
- StS1 id `LessonLearned`, official name `Lesson Learned`
- type=ATTACK, rarity=RARE, cost=2, target=ENEMY, base: Damage=10, StS1 flags: exhaust
- upgrade deltas: upgradeDamage=3
- official description: `Deal !D! damage. NL If Fatal, Upgrade a random card in your deck. NL Exhaust.`
- StS2 name collision: no -> plain title "Lesson Learned"
- IMPL: Attack cost 2, Exhaust: 10 damage (+3); if FATAL, permanently upgrade a random card in your DECK. Read the kill from the attack result (WasTargetKilled) and use the run-level deck upgrade API; the mod's GeneticAlgorithm shows the run-persistent pattern ([SavedProperty] + DeckVersion). FLAG if no deck-upgrade command exists.

## MasterReality  (class `MasterReality`, loc key `SPIRE1-MASTER_REALITY`)
- StS1 id `MasterReality`, official name `Master Reality`
- type=POWER, rarity=RARE, cost=1, target=SELF, base: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official description: `Whenever a card is created during combat, Upgrade it.`
- StS2 name collision: no -> plain title "Master Reality"
- IMPL: Power cost 1 (0 upgraded): whenever a card is CREATED during combat, upgrade it. Custom power using the card-entered-combat hook (mod precedent Cards/MasterfulStab.cs uses AfterCardEnteredCombat) plus CardCmd.Upgrade.

## Omniscience  (class `Omniscience`, loc key `SPIRE1-OMNISCIENCE`)
- StS1 id `Omniscience`, official name `Omniscience`
- type=SKILL, rarity=RARE, cost=4, target=NONE, base: MagicNumber=2, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=3
- official description: `Choose a card in your draw pile. NL Play the chosen card twice and exhaust it. NL Exhaust.`
- StS2 name collision: no -> plain title "Omniscience"
- IMPL: Skill cost 4 (3 upgraded), Exhaust: choose a card in your DRAW pile, play it TWICE, then exhaust it. Requires a play-a-card-from-a-pile API; check CardCmd.AutoPlay. FLAG if playing an arbitrary chosen card twice is not expressible.

## Ragnarok  (class `Ragnarok`, loc key `SPIRE1-RAGNAROK`)
- StS1 id `Ragnarok`, official name `Ragnarok`
- type=ATTACK, rarity=RARE, cost=3, target=ALL_ENEMY, base: Damage=5, MagicNumber=5, StS1 flags: none
- upgrade deltas: upgradeDamage=1, upgradeMagicNumber=1
- official description: `Deal !D! damage to a random enemy !M! times.`
- StS2 name collision: no -> plain title "Ragnarok"
- IMPL: Attack cost 3: deal 5 damage (+1) to a RANDOM enemy 5 times (+1 hit). Use the explicit random-target damage command (mod precedent Cards/RipAndTear.cs uses DamageCmd ... TargetingRandomOpponents).

## Scrawl  (class `Scrawl`, loc key `SPIRE1-SCRAWL`)
- StS1 id `Scrawl`, official name `Scrawl`
- type=SKILL, rarity=RARE, cost=1, target=NONE, base: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=0
- official description: `Draw cards until your hand is full. NL Exhaust.`
- StS2 name collision: YES but the shipped card differs -> our own class, title "StS1 - Scrawl"
- IMPL: Skill cost 1 (0 upgraded), Exhaust: draw cards until your hand is FULL. StS2 ships a Scrawl but it also has Retain, so our own class is required. Hand limit: find the max-hand-size constant/property in the decompiled sources and draw the difference.

## SpiritShield  (class `SpiritShield`, loc key `SPIRE1-SPIRIT_SHIELD`)
- StS1 id `SpiritShield`, official name `Spirit Shield`
- type=SKILL, rarity=RARE, cost=2, target=SELF, base: MagicNumber=3, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official description: `Gain !M! Block for each card in your hand.`
- StS2 name collision: no -> plain title "Spirit Shield"
- IMPL: Skill cost 2: gain 3 Block (+1) FOR EACH card in your hand. Compute at play time; the displayed per-card value must be a DynamicVar.

## Unraveling  (class `Unraveling`, loc key `SPIRE1-UNRAVELING`)
- StS1 id `Unraveling`, official name `Unraveling`
- type=SKILL, rarity=RARE, cost=2, target=NONE, base: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=1
- official description: `Play all of your cards from left to right. Targets are chosen randomly. NL Exhaust.`
- StS2 name collision: no -> plain title "Unraveling"
- IMPL: Skill cost 2, Exhaust: play ALL of your cards from left to right with random targets. Needs an auto-play API (CardCmd.AutoPlay) applied over a snapshot of the hand. FLAG if auto-play cannot choose random targets.

## Vault  (class `Vault`, loc key `SPIRE1-VAULT`)
- StS1 id `Vault`, official name `Vault`
- type=SKILL, rarity=RARE, cost=3, target=ALL, base: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=2
- official description: `Take an extra turn after this one. NL End your turn. NL Exhaust.`
- StS2 name collision: no -> plain title "Vault"
- IMPL: Skill cost 3 (2 upgraded), Exhaust: take an EXTRA turn after this one, then end your turn. Search for an extra-turn API; FLAG if absent (do not fake it by refreshing energy).

## Wish  (class `Wish`, loc key `SPIRE1-WISH`)
- StS1 id `Wish`, official name `Wish`
- type=SKILL, rarity=RARE, cost=3, target=NONE, base: Damage=3, MagicNumber=25, Block=6, StS1 flags: exhaust
- upgrade deltas: upgradeDamage=1, upgradeMagicNumber=5, upgradeBlock=2
- official description: `Choose one: NL Gain !B! Plated Armor, !D! Strength, or !M! Gold. NL Exhaust.`
- StS2 name collision: YES but the shipped card differs -> our own class, title "StS1 - Wish"
- IMPL: Skill cost 3, Exhaust: choose one of Gain 6 Plated Armor (+2), 3 Strength (+1), or 25 Gold (+5). StS2's Wish is a different Ancient-rarity card, so our own class is required. Plated Armor is the shipped `PlatingPower`. Use a 3-option choice; if a card cannot present a 3-way choice, FLAG.
