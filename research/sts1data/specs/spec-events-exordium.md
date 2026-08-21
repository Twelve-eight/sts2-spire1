# StS1 event spec - Exordium (Act 1 / Overgrowth)

11 events. Every string below is the OFFICIAL StS1 English text extracted from desktop-1.0.jar; use it verbatim.
`NL` in StS1 text is a line break marker - DELETE it, StS2 does not use it. Keep sentences, drop the marker.
StS1 color codes (`#r`, `#g`, `#b`, `#y`, `#p`) are StS1 markup - DELETE them too; StS2 marks keywords with `*word*`.

---

## BigFish
- Class file: `mod/Spire1Code/Events/BigFish.cs`, `public class BigFish : Spire1Event`
- ID: `SPIRE1-BIG_FISH`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Big Fish`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "room_full_of_cheese";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CardLibrary.getCopy`
  - `CurScreen.ordinal`
  - `AbstractPlayer.heal`
  - `GenericEventDialog.updateBodyText`
  - `AbstractEvent.logMetricHeal`
  - `AbstractPlayer.increaseMaxHp`
  - `AbstractEvent.logMetricMaxHPGain`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractEvent.logMetricObtainCardAndRelic`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.clearAllDialogs`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Banana] #gHeal #g`
1. ` #gHP.`
2. `[Donut] #gMax #gHP #g+`
3. `.`
4. `[Box] #gObtain #ga #gRelic. #rBecome #rCursed #r- #rRegret.`
5. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you make your way down a long corridor you see a #ybanana, a #ydonut, and a #ybox ~floating~ about. No... upon closer inspection they are tied to strings coming from holes in the ceiling. There is a quiet @cackling@ from above as you approach the objects. NL What do you do?`
1. `You eat the #ybanana. It is nutritious and slightly #bmagical, healing you.`
2. `You eat the #ydonut. It really hits the spot! Your Max HP increases.`
3. ``
4. `You grab the box. Inside you find a #yrelic!`
5. ` NL However, you really craved the donut... NL You are filled with ~sadness,~ but mostly #rregret.`

---

## Cleric
- Class file: `mod/Spire1Code/Events/Cleric.cs`, `public class Cleric : Spire1Event`
- ID: `SPIRE1-CLERIC`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Cleric`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "tea_master";`
- StS1 numeric constants found in the bytecode: [0, 0.25, 1, 2, 3, 4, 5, 6, 7, 8, 15, 35, 50, 75]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `AbstractImageEvent.update`
  - `AbstractEvent.logMetricCardRemovalAtCost`
  - `CardGroup.removeCard`
  - `AbstractPlayer.loseGold`
  - `AbstractPlayer.heal`
  - `AbstractEvent.logMetricHealAtCost`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `CardGroup.size`
  - `GridCardSelectScreen.open`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Heal] #y35 #yGold: #gHeal #g`
1. `[Locked] Requires: `
2. ` Gold.`
3. `[Purify] #y`
4. ` #yGold: #gRemove #ga #gcard #gfrom #gyour #gdeck.`
5. `[Locked] Requires: 50 Gold.`
6. `[Leave]`
7. `Select a Card to Remove.`
8. ` #gHP.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `A strange blue humanoid with a golden helm(?) approaches you with a huge smile. NL @"Hello@ @friend!@ I am #bCleric! Are you interested in my services?!" the creature shouts, loudly.`
1. `A warm golden light envelops your body and dissipates. NL The creature grins. "Cleric best healer. @Have@ @a@ @good@ @day!"@`
2. `A cold blue flame envelops your body and dissipates. NL The creature grins. "Cleric talented. @Have@ @a@ @good@ @day!"@`
3. `You don't trust this #b"Cleric", so you leave.`

---

## DeadAdventurer
- Class file: `mod/Spire1Code/Events/DeadAdventurer.cs`, `public class DeadAdventurer : Spire1Event`
- ID: `SPIRE1-DEAD_ADVENTURER`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Dead Adventurer`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "field_of_man_sized_holes";`
- StS1 numeric constants found in the bytecode: [-1, 0, 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 25, 30, 35, 86.5, 99, 146, 173, 292, 800]
- StS1 APIs the event calls (tells you what it does):
  - `Random.randomLong`
  - `Random.random`
  - `ImageMaster.loadImage`
  - `RoomEventDialog.addDialogOption`
  - `AbstractEvent.update`
  - `AbstractDungeon.getCurrRoom`
  - `RoomEventDialog.getSelectedOption`
  - `CUR_SCREEN.ordinal`
  - `RoomEventDialog.updateBodyText`
  - `RoomEventDialog.updateDialogOption`
  - `RoomEventDialog.removeDialogOption`
  - `AbstractRoom.addGoldToRewards`
  - `MonsterHelper.getEncounter`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractRoom.addRelicToRewards`
  - `EffectHelper.gainGold`
  - `AbstractPlayer.gainGold`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractRoom.spawnRelicAndObtain`
  - `AbstractEvent.logMetricGainGoldAndRelic`
  - `AbstractEvent.logMetricGainGold`
  - `AbstractEvent.render`
  - `AbstractEvent.dispose`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Search] #gFind #gLoot. #r`
1. `[Leave]`
2. `[Fight]`
3. `[Continue] #gFind #gLoot. #r`
4. `%: #rmonster #rreturns.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `While searching the adventurer you are caught off guard!`
1. `You exit without a sound.`
2. `You come across a #rdead #radventurer on the floor. NL His #bpants have been stolen! Also, `
3. `the armor and face appear to be #r@scoured@ #r@by@ #r@flames.@ `
4. `it looks as though he's been #r@gouged@ #r@and@ #r@trampled@ by a horned beast. `
5. `he looks to have been #r@eviscerated@ #r@and@ #r@chopped@ by giant claws. `
6. `NL Though his #ypossessions #yare #ystill #yintact, you're in no mind to find out what happened here...`
7. `You found some #ygold! NL Continue searching?`
8. `Hmm, couldn't find anything... NL Continue searching?`
9. `You found a #yrelic! NL Continue searching?`
10. `Looks like you searched all his belongings without a hitch!`

---

## GoldenIdolEvent
- Class file: `mod/Spire1Code/Events/GoldenIdolEvent.cs`, `public class GoldenIdolEvent : Spire1Event`
- ID: `SPIRE1-GOLDEN_IDOL_EVENT`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Golden Idol`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "sunken_statue";`
- StS1 numeric constants found in the bytecode: [0, 0.08, 0.1, 0.25, 0.35, 1, 2, 3, 4, 5, 6, 15]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.hasRelic`
  - `RelicLibrary.getRelic`
  - `AbstractRelic.makeCopy`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `ScreenShake.mildRumble`
  - `CardLibrary.getCopy`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractEvent.logMetricIgnored`
  - `ScreenShake.shake`
  - `AbstractEvent.logMetricObtainCardAndRelic`
  - `AbstractPlayer.damage`
  - `AbstractEvent.logMetricObtainRelicAndDamage`
  - `AbstractPlayer.decreaseMaxHealth`
  - `AbstractEvent.logMetricObtainRelicAndLoseMaxHP`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Take] #gObtain #gGolden #gIdol. #rTrigger #ra #rtrap.`
1. `[Leave]`
2. `[Outrun] #rBecome #rCursed #r- #rInjury.`
3. `[Smash] #rTake #r`
4. ` #rDamage.`
5. `[Hide] #rLose #r`
6. ` #rMax #rHP.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You come across an inconspicuous pedestal with a #yshining #ygold #yidol sitting peacefully atop. It looks incredibly valuable. NL NL You sure don't see any traps nearby.`
1. `As you grab the Idol and stow it away, a giant boulder smashes through the ceiling into the ground next to you. NL You realize that the floor is slanted downwards as the boulder starts to roll towards you.`
2. `@RUUUUUUUUUUN!@ NL You barely leap into a side passageway as the boulder rushes by. Unfortunately it feels like you sprained something however.`
3. `You throw yourself at the boulder with everything you have. When the dust clears, you can make a safe way out.`
4. `@SQUISH!@ NL The boulder flattens you a little as it passes by, but otherwise you can get out of here.`
5. `If there was ever an obvious trap, this would be it. NL You decide not to interfere with objects placed upon pedestals.`

---

## GoldenWing
- Class file: `mod/Spire1Code/Events/GoldenWing.cs`, `public class GoldenWing : Spire1Event`
- ID: `SPIRE1-GOLDEN_WING`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Wing Statue`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "stone_of_all_time";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 50, 80]
- StS1 APIs the event calls (tells you what it does):
  - `CardHelper.hasCardWithXDamage`
  - `GenericEventDialog.setDialogOption`
  - `AbstractImageEvent.update`
  - `GenericEventDialog.getSelectedOption`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.damage`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.removeDialogOption`
  - `Random.random`
  - `AbstractPlayer.gainGold`
  - `AbstractEvent.logMetricGainGold`
  - `AbstractEvent.logMetricIgnored`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `GridCardSelectScreen.open`
  - `AbstractEvent.logMetricCardRemovalAndDamage`
  - `CardGroup.removeCard`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pray] #gRemove #ga #gcard #gfrom #gyour #gdeck. #rLose #r`
1. ` #rHP.`
2. `[Destroy] #gGain #g`
3. ` #g- #g`
4. ` #gGold.`
5. `[Locked] Requires: Card with `
6. ` or more damage.`
7. `[Leave]`
8. `[Continue]`
9. `Select a Card to Remove.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Among the stone and boulders, you notice an intricate large blue statue resembling a wing. NL You find #ygold spilling from its cracks. Maybe there is more inside...`
1. `Someone once told you of a cult that worshipped a giant bird. As you kneel in prayer, you begin to ~feel~ ... ~lightheaded~ . NL NL You wake up some time later, feeling strangely fleet of foot.`
2. `With all your might, you hack away at the statue. NL It soon @crumbles,@ revealing a #ypile #yof #ygold. You grab as much as you can and continue onwards.`
3. `The statue makes you feel ~uneasy.~ You walk past and continue onward.`

---

## GoopPuddle
- Class file: `mod/Spire1Code/Events/GoopPuddle.cs`, `public class GoopPuddle : Spire1Event`
- ID: `SPIRE1-GOOP_PUDDLE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `World of Goop`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "spiraling_whirlpool";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 11, 15, 20, 35, 50, 75]
- StS1 APIs the event calls (tells you what it does):
  - `Random.random`
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.clearAllDialogs`
  - `AbstractPlayer.damage`
  - `AbstractPlayer.gainGold`
  - `AbstractEvent.logMetricGainGoldAndDamage`
  - `AbstractPlayer.loseGold`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Gather Gold] #gGain #g`
1. ` #gGold. #rLose #r`
2. ` #rHP.`
3. `[Leave It] #rLose #r`
4. ` #rGold.`
5. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You fall into a puddle. NL @IT'S@ @MADE@ @OF@ #g@SLIME@ #g@GOOP!!@ NL Frantically, you claw yourself out over several minutes as you feel the goop starting to burn. NL You can feel goop in your ears, goop in your nose, goop everywhere. NL NL Climbing out, you notice that some of your #ygold is missing. Looking back to the puddle you see your missing coins combined with #ygold from unfortunate adventurers mixed together in the puddle.`
1. `Feeling the sting of the goop as the prolonged exposure starts to melt away at your skin, you manage to fish out the #ygold.`
2. `You decide that mess is not worth it.`

---

## LivingWall
- Class file: `mod/Spire1Code/Events/LivingWall.cs`, `public class LivingWall : Spire1Event`
- ID: `SPIRE1-LIVING_WALL`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Living Wall`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "morphic_grove";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CardGroup.hasUpgradableCards`
  - `SoundMaster.play`
  - `AbstractImageEvent.update`
  - `Choice.ordinal`
  - `AbstractEvent.logMetricCardRemoval`
  - `CardGroup.removeCard`
  - `AbstractDungeon.transformCard`
  - `AbstractDungeon.getTransformedCard`
  - `AbstractEvent.logMetricTransformCard`
  - `AbstractCard.upgrade`
  - `AbstractEvent.logMetricCardUpgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `CurScreen.ordinal`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `CardGroup.size`
  - `GridCardSelectScreen.open`
  - `CardGroup.getUpgradableCards`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.clearAllDialogs`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Forget] #gRemove #ga #gcard #gfrom #gyour #gdeck.`
1. `[Change] #gTransform #ga #gcard #gin #gyour #gdeck.`
2. `[Grow] #gUpgrade #ga #gcard #gin #gyour #gdeck.`
3. `Select a Card to Remove.`
4. `Choose a Card to Transform.`
5. `Choose a Card to Upgrade.`
6. `[Leave]`
7. `[Locked] Requires: Upgradeable Cards`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you come to a dead-end and begin to turn around, walls @slam@ @down@ from the ceiling, trapping you! NL NL Three faces materialize from the walls and speak. NL #b"Forget #bwhat #byou #bknow, #band #bI'll #blet #byou #bgo." NL #p"I #prequire #pchange #pto #psee #pa #pnew #pspace." NL #y"If #yyou #ywant #yto #ypass #yme, #ythen #yyou #ymust #ygrow."`
1. `Satisfied, the walls in front of you merge back into the ceiling, leaving a path forward.`

---

## Mushrooms
- Class file: `mod/Spire1Code/Events/Mushrooms.cs`, `public class Mushrooms : Spire1Event`
- ID: `SPIRE1-MUSHROOMS`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Mushrooms`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "hungry_for_mushrooms";`
- StS1 numeric constants found in the bytecode: [0, 0.25, 1, 2, 3, 4, 20, 25, 30, 1080]
- StS1 APIs the event calls (tells you what it does):
  - `ImageMaster.loadImage`
  - `RoomEventDialog.addDialogOption`
  - `CardLibrary.getCopy`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractEvent.update`
  - `RoomEventDialog.getSelectedOption`
  - `MonsterHelper.getEncounter`
  - `RoomEventDialog.updateBodyText`
  - `RoomEventDialog.updateDialogOption`
  - `RoomEventDialog.removeDialogOption`
  - `AbstractEvent.logMetric`
  - `Random.random`
  - `AbstractRoom.addGoldToRewards`
  - `AbstractPlayer.hasRelic`
  - `AbstractRoom.addRelicToRewards`
  - `AbstractEvent.logMetricObtainCardAndHeal`
  - `AbstractPlayer.heal`
  - `AbstractEvent.dispose`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Stomp] #rAnger #rthe #rMushrooms.`
1. `[Eat] #gHeal #g`
2. ` #gHP. #rBecome #rCursed #r- #rParasite.`
3. `[Fight]`
4. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You give in to the unnatural desire to eat. As you consume mushroom after mushroom, you feel yourself entering into a daze and pass out. As you awake, you feel very odd. NL You #gHeal #b25% of your HP, but you also get #rinfected.`
1. `#r@Ambushed!!@ NL Rodents infested by the mushrooms appear out of nowhere!`
2. `You enter a corridor full of #b~hypnotizing~ #b~colored~ #b~mushrooms.~ NL Due to your lack of specialization in mycology you are unable to identify the specimens. NL You want to escape, but feel oddly compelled to eat a #b~mushroom...~`

---

## ScrapOoze
- Class file: `mod/Spire1Code/Events/ScrapOoze.cs`, `public class ScrapOoze : Spire1Event`
- ID: `SPIRE1-SCRAP_OOZE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Scrap Ooze`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "trash_heap";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 10, 15, 25, 99]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `AbstractPlayer.damage`
  - `Random.random`
  - `GenericEventDialog.updateBodyText`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractEvent.logMetricObtainRelicAndDamage`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.removeDialogOption`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `AbstractEvent.logMetricTakeDamage`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Reach Inside] #rLose #r`
1. ` #rHP. #g`
2. `%: #gFind #ga #gRelic.`
3. `[Leave]`
4. `[Deeper] #rLose #r`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you walk into the room you hear a ~gurgling~ and the @grinding@ of metals. Before you is a slime-like creature that ate too much scrap for its own good. From the center of the creature you see glints of strange light, perhaps something magical? It looks like you can get some #ytreasure if you just reach inside its... opening. However, the acid and sharp objects may #rhurt.`
1. `#r@Ouch!@ NL All you find is corroded metal and a bit of #r@burning@ #r@pain.@ NL However, you're still convinced there's a #yrelic...`
2. `#gSuccess! NL After rummaging through the metal and burning acid, you finally grab hold of a #yrelic and yank it out. NL You pull your way out of the ooze #rdamaged but rewarded.`
3. `You decide to leave the area. NL The slime pays no attention, content with its meal.`

---

## ShiningLight
- Class file: `mod/Spire1Code/Events/ShiningLight.cs`, `public class ShiningLight : Spire1Event`
- ID: `SPIRE1-SHINING_LIGHT`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Shining Light`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "colossal_flower";`
- StS1 numeric constants found in the bytecode: [0, 0.2, 0.3, 1, 2, 3, 15, 190]
- StS1 APIs the event calls (tells you what it does):
  - `CardGroup.hasUpgradableCards`
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.removeDialogOption`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.damage`
  - `AbstractEvent.logMetricIgnored`
  - `AbstractCard.canUpgrade`
  - `Random.randomLong`
  - `AbstractCard.upgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `AbstractCard.makeStatEquivalentCopy`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Enter] #gUpgrade #g2 #grandom #gcards. #rLose #r`
1. ` #rHP.`
2. `[Leave]`
3. `[Locked] Requires: Upgradeable Cards`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You find a shimmering #ymass #yof #ylight encompassing the center of the room. NL NL Its ~warm~ ~glow~ and ~enchanting~ ~patterns~ invite you in.`
1. `As you walk through the light, you notice that the light is absorbed into you. NL It's #r@scorching@ #r@hot@ ! However, the pain quickly recedes. NL You feel #binvigorated, as though you received a well deserved slap.`
2. `You walk around it, wondering what could have been.`

---

## Sssserpent
- Class file: `mod/Spire1Code/Events/Sssserpent.cs`, `public class Sssserpent : Spire1Event`
- ID: `SPIRE1-SSSSERPENT`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Ssssserpent`
- Act: `public override ActModel[] Acts => Act1;`
- Portrait: `protected override string ShippedPortrait => "symbiote";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 15, 150, 175]
- StS1 APIs the event calls (tells you what it does):
  - `SoundMaster.play`
  - `CardLibrary.getCopy`
  - `GenericEventDialog.setDialogOption`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.removeDialogOption`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractEvent.logMetricGainGoldAndCard`
  - `AbstractEvent.logMetricIgnored`
  - `AbstractPlayer.gainGold`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Agree] #gGain #g`
1. ` #gGold. #rBecome #rCursed #r- #rDoubt.`
2. `[Disagree]`
3. `[Continue]`
4. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You walk into a room to find a large hole in the ground. As you approach the hole, an enormous serpent creature appears from within. NL NL ~"Ho~ ~hooo!~ ~Hello~ ~hello!~ ~what~ ~have~ ~we~ ~got~ ~here?~ Hello adventurer, I ask a simple question. NL The most fulfilling of lives is that in which you can #y~buy~ #y~anything!~ NL Do you agree?"`
1. `~"Yeeeeeeessssssssssessss!~ NL ~Thisss~ ~will~ ~all~ ~be~ ~worthhh~ ~it.~ NL ~..ssSSs.....~ ~ss...~ ~sssss....!"~`
2. `The serpent stares at you with a look of extreme disappointment.`
3. `The serpent rears its head and blasts a stream of #ygold upwards! NL It is amazing and terrifying simultaneously. NL You gather all the #ygold, thank the snake, and get going.`

