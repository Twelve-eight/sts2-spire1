# Silent RARE cards (19) — StS1 vanilla

Every number below was extracted from the shipped StS1 jar (`desktop-1.0.jar`) bytecode. Use these EXACT values. The `StS2 name collision` field decides the localization title prefix.

## AThousandCuts  (class name = `AThousandCuts`, loc key `SPIRE1-ATHOUSAND_CUTS`)
- StS1 id: `A Thousand Cuts`, official name: `A Thousand Cuts`
- type=POWER, rarity=RARE, cost=2, target=SELF, base values: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Whenever you play a card, deal !M! damage to ALL enemies.`
- StS2 name collision: no -> plain title "A Thousand Cuts"
- IMPL: Power cost 2: whenever you play a card, deal 1 damage (2 upgraded) to ALL enemies. StS2 has NO AThousandCutsPower (verified NOT FOUND; PaperCutsPower is a different max-HP effect). Write Spire1Code/Powers/AThousandCutsPower.cs : CustomPowerModel (Buff, Counter) using the AfterCardPlayed hook signature copied from the decompiled AfterimagePower.cs, dealing Amount unpowered damage to CombatState.HittableEnemies.

## Adrenaline  (class name = `Adrenaline`, loc key `SPIRE1-ADRENALINE`)
- StS1 id: `Adrenaline`, official name: `Adrenaline`
- type=SKILL, rarity=RARE, cost=0, target=SELF, base values: none, StS1 flags: exhaust
- upgrade deltas: none
- official StS1 description: `Gain [G]. NL Draw 2 cards. NL Exhaust.`
- official upgraded description: `Gain [G] [G]. NL Draw 2 cards. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Adrenaline"
- IMPL: Skill cost 0, Exhaust: gain 1 Energy (2 upgraded) and draw 2 cards. Vars: EnergyVar(1), CardsVar(2).

## AfterImage  (class name = `AfterImage`, loc key `SPIRE1-AFTER_IMAGE`)
- StS1 id: `After Image`, official name: `After Image`
- type=POWER, rarity=RARE, cost=1, target=SELF, base values: none, StS1 flags: none
- upgrade deltas: flag:isInnate=true
- official StS1 description: `Whenever you play a card, gain 1 Block.`
- official upgraded description: `Innate. NL Whenever you play a card, gain 1 Block.`
- StS2 name collision: no -> plain title "After Image"
- IMPL: Power cost 1: whenever you play a card, gain 1 Block; upgraded also Innate. REUSE the game power AfterimagePower (exact class spelling 'AfterimagePower'): PowerVar<AfterimagePower>(1) + ApplySelf; OnUpgrade AddKeyword(CardKeyword.Innate).

## Alchemize  (class name = `Alchemize`, loc key `SPIRE1-ALCHEMIZE`)
- StS1 id: `Venomology`, official name: `Alchemize`
- type=SKILL, rarity=RARE, cost=1, target=SELF, base values: none, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=0
- official StS1 description: `Obtain a random potion. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Alchemize"
- IMPL: Skill cost 1 (0 upgraded), Exhaust: obtain a RANDOM potion. Search for a potion-granting command (e.g. PotionCmd / player potion slots) in the decompiled sources. If no API to grant a random potion exists, FLAG as infeasible.

## BulletTime  (class name = `BulletTime`, loc key `SPIRE1-BULLET_TIME`)
- StS1 id: `Bullet Time`, official name: `Bullet Time`
- type=SKILL, rarity=RARE, cost=3, target=NONE, base values: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=2
- official StS1 description: `You cannot draw additional cards this turn. Reduce the cost of all cards in your hand to 0 this turn.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Bullet Time"
- IMPL: Skill cost 3 (2 upgraded): you cannot draw additional cards this turn AND all cards in your hand cost 0 this turn. NoDrawPower EXISTS (apply 1 to self). For the cost part, set a this-turn cost override on every card in hand (verify the per-card this-turn cost API, e.g. EnergyCost.AddThisCombat / a this-turn variant). If the cost-to-0 part is impossible, FLAG the whole card rather than shipping only NoDraw.

## Burst  (class name = `Burst`, loc key `SPIRE1-BURST`)
- StS1 id: `Burst`, official name: `Burst`
- type=SKILL, rarity=RARE, cost=1, target=SELF, base values: MagicNumber=1, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `This turn, your next Skill is played twice.`
- official upgraded description: `This turn, your next !M! Skills are played twice.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Burst"
- IMPL: Skill cost 1: this turn, your next Skill is played twice (2 Skills upgraded). Needs a power that replays the next Skill. Search for an existing StS2 double-play power; if none, FLAG as infeasible (do not fake it).

## CorpseExplosion  (class name = `CorpseExplosion`, loc key `SPIRE1-CORPSE_EXPLOSION`)
- StS1 id: `Corpse Explosion`, official name: `Corpse Explosion`
- type=SKILL, rarity=RARE, cost=2, target=ENEMY, base values: MagicNumber=6, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=3
- official StS1 description: `Apply !M! Poison. NL When the enemy dies, deal damage equal to its Max HP to ALL enemies.`
- official upgraded description: `deprecated`
- StS2 name collision: no -> plain title "Corpse Explosion"
- IMPL: Skill cost 2, target enemy: apply 6 Poison (9 upgraded); when that enemy dies, deal damage equal to its MAX HP to ALL enemies. Write Spire1Code/Powers/CorpseExplosionPower.cs : CustomPowerModel (Debuff, Single) that on its owner's death deals owner MaxHp damage to all other hittable enemies (verify the exact death hook name in the decompiled PowerModel.cs). Apply PoisonPower(6) as well.

## DieDieDie  (class name = `DieDieDie`, loc key `SPIRE1-DIE_DIE_DIE`)
- StS1 id: `Die Die Die`, official name: `Die Die Die`
- type=ATTACK, rarity=RARE, cost=1, target=ALL_ENEMY, base values: Damage=13, StS1 flags: exhaust, isMultiDamage
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Deal !D! damage to ALL enemies. NL Exhaust.`
- StS2 name collision: no -> plain title "Die Die Die"
- IMPL: Attack cost 1: deal 13 damage (+4) to ALL enemies, Exhaust. TargetType.AllEnemies.

## Doppelganger  (class name = `Doppelganger`, loc key `SPIRE1-DOPPELGANGER`)
- StS1 id: `Doppelganger`, official name: `Doppelganger`
- type=SKILL, rarity=RARE, cost=-1, target=SELF, base values: none, StS1 flags: exhaust
- upgrade deltas: none
- official StS1 description: `Next turn, draw X cards and gain X [G]. NL Exhaust.`
- official upgraded description: `Next turn, draw X+1 cards and gain X+1 [G]. NL Exhaust.`
- StS2 name collision: no -> plain title "Doppelganger"
- IMPL: Skill, X-COST, Exhaust: next turn draw X cards and gain X Energy (X+1 upgraded). Combine DrawCardsNextTurnPower + EnergyNextTurnPower with amount = ResolveEnergyXValue() (+1 when upgraded). Copy the X-cost pattern from Cards/Whirlwind.cs.

## Envenom  (class name = `Envenom`, loc key `SPIRE1-ENVENOM`)
- StS1 id: `Envenom`, official name: `Envenom`
- type=POWER, rarity=RARE, cost=2, target=SELF, base values: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=1
- official StS1 description: `Whenever an Attack deals unblocked damage, apply 1 Poison.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Envenom"
- IMPL: Power cost 2 (1 upgraded): whenever an Attack deals UNBLOCKED damage, apply 1 Poison. Write Spire1Code/Powers/EnvenomPower.cs : CustomPowerModel (Buff, Counter). The mod already reads unblocked damage in a card via AttackCommand.Results -> DamageResult.UnblockedDamage; for a power, find the post-attack hook in the decompiled PowerModel.cs. FLAG if no such hook exists.

## GlassKnife  (class name = `GlassKnife`, loc key `SPIRE1-GLASS_KNIFE`)
- StS1 id: `Glass Knife`, official name: `Glass Knife`
- type=ATTACK, rarity=RARE, cost=1, target=ENEMY, base values: Damage=8, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Deal !D! damage twice. Decrease the damage of this card by 2 this combat.`
- StS2 name collision: no -> plain title "Glass Knife"
- IMPL: Attack cost 1: deal 8 damage (+4) TWICE; permanently (this combat) decrease this card's damage by 2 each time it is played. hitCount: 2 then DynamicVars.Damage must be reduced by 2 for the rest of the combat (verify the exact combat-scoped value-change API; the mod's Rampage.cs does a per-combat damage increase — copy that mechanism inverted).

## GrandFinale  (class name = `GrandFinale`, loc key `SPIRE1-GRAND_FINALE`)
- StS1 id: `Grand Finale`, official name: `Grand Finale`
- type=ATTACK, rarity=RARE, cost=0, target=ALL_ENEMY, base values: Damage=50, StS1 flags: isMultiDamage
- upgrade deltas: upgradeDamage=10
- official StS1 description: `Can only be played if there are no cards in your draw pile. NL Deal !D! damage to ALL enemies.`
- official upgraded description: `My draw pile NL must be #rEmpty.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Grand Finale"
- IMPL: Attack cost 0: can ONLY be played if your draw pile is empty; deal 50 damage (+10) to ALL enemies. Playability: `protected override bool IsPlayable => PileType.Draw.GetPile(Owner).Cards.Count == 0;` (the mod's Clash.cs shows the IsPlayable override shape).

## Malaise  (class name = `Malaise`, loc key `SPIRE1-MALAISE`)
- StS1 id: `Malaise`, official name: `Malaise`
- type=SKILL, rarity=RARE, cost=-1, target=ENEMY, base values: none, StS1 flags: exhaust
- upgrade deltas: none
- official StS1 description: `Enemy loses X Strength. Apply X Weak. NL Exhaust.`
- official upgraded description: `Enemy loses X+1 Strength. Apply X+1 Weak. NL Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Malaise"
- IMPL: Skill, X-COST, Exhaust, target enemy: the enemy loses X Strength and gains X Weak (X+1 upgraded). Strength loss = apply negative StrengthPower amount (verify that a negative apply is allowed; the mod's PiercingWail slice uses the same mechanism — coordinate with the other worker via hub if needed).

## Nightmare  (class name = `Nightmare`, loc key `SPIRE1-NIGHTMARE`)
- StS1 id: `Night Terror`, official name: `Nightmare`
- type=SKILL, rarity=RARE, cost=3, target=NONE, base values: MagicNumber=3, StS1 flags: exhaust
- upgrade deltas: upgradeBaseCost=2
- official StS1 description: `Choose a card. NL Next turn, add !M! copies of that card into your hand. Exhaust.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Nightmare"
- IMPL: Skill cost 3 (2 upgraded), Exhaust: choose a card; next turn add 3 copies of it into your hand. Needs card selection + a delayed next-turn effect (custom power holding the chosen CardModel and adding clones at turn start via CreateCloneForPlayer + CardPileCmd.AddGeneratedCardToCombat). Implement if the turn-start hook and clone API are available; otherwise FLAG.

## PhantasmalKiller  (class name = `PhantasmalKiller`, loc key `SPIRE1-PHANTASMAL_KILLER`)
- StS1 id: `Phantasmal Killer`, official name: `Phantasmal Killer`
- type=SKILL, rarity=RARE, cost=1, target=SELF, base values: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official StS1 description: `Next turn, your Attacks deal double damage.`
- StS2 name collision: no -> plain title "Phantasmal Killer"
- IMPL: Power cost 1 (0 upgraded): next turn your Attacks deal DOUBLE damage. REUSE the game power DoubleDamagePower (verified exists, Buff/Counter, modifies damage): apply 1 stack to self. Read the decompiled DoubleDamagePower.cs to confirm it is a next-turn-scoped effect; if it is permanent-while-stacked, still apply 1 and describe it exactly as implemented, and FLAG the difference.

## StormOfSteel  (class name = `StormOfSteel`, loc key `SPIRE1-STORM_OF_STEEL`)
- StS1 id: `Storm of Steel`, official name: `Storm of Steel`
- type=SKILL, rarity=RARE, cost=1, target=NONE, base values: none, StS1 flags: none
- upgrade deltas: none
- official StS1 description: `Discard your hand. NL Add 1 *Shiv into your hand for each card discarded.`
- official upgraded description: `Discard your hand. NL Add 1 *Shiv+ into your hand for each card discarded.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Storm of Steel"
- IMPL: Skill cost 1: discard your hand, then add 1 Shiv into your hand for EACH card discarded (upgraded adds Shiv+ — upgraded Shivs). Count hand first, discard the whole hand (CardCmd.Discard with a materialised list), then Shiv.CreateInHand(Owner, count, Owner.Creature.CombatState, Owner); when upgraded, upgrade each created Shiv (CardCmd.Upgrade or card.Upgrade — verify).

## ToolsOfTheTrade  (class name = `ToolsOfTheTrade`, loc key `SPIRE1-TOOLS_OF_THE_TRADE`)
- StS1 id: `Tools of the Trade`, official name: `Tools of the Trade`
- type=POWER, rarity=RARE, cost=1, target=SELF, base values: none, StS1 flags: none
- upgrade deltas: upgradeBaseCost=0
- official StS1 description: `At the start of your turn, draw 1 card and discard 1 card.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Tools of the Trade"
- IMPL: Power cost 1 (0 upgraded): at the start of your turn, draw 1 card and discard 1 card. Write Spire1Code/Powers/ToolsOfTheTradePower.cs : CustomPowerModel (Buff, Counter) using the AfterSideTurnStart hook; draw Amount then let the player discard Amount via CardSelectorPrefs(DiscardSelectionPrompt, Amount).

## Unload  (class name = `Unload`, loc key `SPIRE1-UNLOAD`)
- StS1 id: `Unload`, official name: `Unload`
- type=ATTACK, rarity=RARE, cost=1, target=ENEMY, base values: Damage=14, StS1 flags: none
- upgrade deltas: upgradeDamage=4
- official StS1 description: `Deal !D! damage. NL Discard all non-Attack cards in your hand.`
- StS2 name collision: no -> plain title "Unload"
- IMPL: Attack cost 1: deal 14 damage (+4), then discard ALL non-Attack cards in your hand. Filter PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type != CardType.Attack).ToList() and CardCmd.Discard the list.

## WraithForm  (class name = `WraithForm`, loc key `SPIRE1-WRAITH_FORM`)
- StS1 id: `Wraith Form v2`, official name: `Wraith Form`
- type=POWER, rarity=RARE, cost=3, target=SELF, base values: MagicNumber=2, StS1 flags: none
- upgrade deltas: upgradeMagicNumber=1
- official StS1 description: `Gain !M! Intangible. NL At the end of your turn, lose 1 Dexterity.`
- StS2 name collision: YES -> localization title MUST be "StS1 - Wraith Form"
- IMPL: Power cost 3: gain 2 Intangible (3 upgraded); at the end of your turn, lose 1 Dexterity. IntangiblePower and DexterityPower both EXIST. The recurring Dexterity loss needs a custom power (Spire1Code/Powers/WraithFormPower.cs, Buff/Counter) with an end-of-turn hook applying -1 Dexterity. Apply IntangiblePower(2) directly from the card.
