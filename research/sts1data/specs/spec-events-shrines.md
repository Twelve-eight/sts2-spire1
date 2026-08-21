# StS1 event spec - Shrines (shared - any act)

17 events. Every string below is the OFFICIAL StS1 English text extracted from desktop-1.0.jar; use it verbatim.
`NL` in StS1 text is a line break marker - DELETE it, StS2 does not use it. Keep sentences, drop the marker.
StS1 color codes (`#r`, `#g`, `#b`, `#y`, `#p`) are StS1 markup - DELETE them too; StS2 marks keywords with `*word*`.

---

## AccursedBlacksmith
- Class file: `mod/Spire1Code/Events/AccursedBlacksmith.cs`, `public class AccursedBlacksmith : Spire1Event`
- ID: `SPIRE1-ACCURSED_BLACKSMITH`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Ominous Forge`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "tinker_time";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5]
- StS1 APIs the event calls (tells you what it does):
  - `CardGroup.hasUpgradableCards`
  - `GenericEventDialog.setDialogOption`
  - `CardLibrary.getCopy`
  - `SoundMaster.play`
  - `AbstractImageEvent.update`
  - `AbstractCard.upgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `AbstractCard.makeStatEquivalentCopy`
  - `CardGroup.getUpgradableCards`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Forge] #gUpgrade #ga #gcard #gin #gyour #gdeck.`
1. `[Rummage] #gObtain #ga #gspecial #gRelic. #rBecome #rCursed #r- #rPain.`
2. `[Leave]`
3. `Choose a Card to Upgrade`
4. `[Locked] Requires: Upgradeable Cards`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You duck into a small hut. Inside, you find what appears to be a forge. The smithing tools are covered with dust, yet a fire roars inside the furnace. You feel on edge...`
1. `You decide to put the forge to use and... NL #y@CLANG@ #y@CLAAANG@ #y@CLANG!@ NL ...improve your arsenal!`
2. `You decide to see if you can find anything of use. After uncovering tarps, looking through boxes, and checking nooks and crannies, you find a dust covered #y~relic!~ `
3. `As you go through the finishing touches, the flames of the forge jump out at you, #r@burning@ #r@and@ #r@scarring@ your arms..`
4. `NL NL Taking the relic, you can't shake a sudden feeling of #r~sharp~ #r~pain~ as you exit the hut. Maybe you disturbed some sort of spirit?`
5. `There doesn't seem to be anything of use. You exit the way you came, the flames of the furnace casting #p~eerie~ #p~shadows~ on the walls inside the hut...`

---

## Bonfire
- Class file: `mod/Spire1Code/Events/Bonfire.cs`, `public class Bonfire : Spire1Event`
- ID: `SPIRE1-BONFIRE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Bonfire Spirits`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "luminous_choir";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7, 10]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `AbstractImageEvent.update`
  - `CardRarity.ordinal`
  - `CardGroup.removeCard`
  - `GenericEventDialog.updateDialogOption`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `CardGroup.size`
  - `GridCardSelectScreen.open`
  - `AbstractPlayer.hasRelic`
  - `AbstractDungeon.getCurrRoom`
  - `RelicLibrary.getRelic`
  - `AbstractRelic.makeCopy`
  - `AbstractRoom.spawnRelicAndObtain`
  - `AbstractPlayer.heal`
  - `AbstractPlayer.increaseMaxHp`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Leave]`
2. `[Offer] Receive a reward based on the offer.`
3. `Select a Card to Offer.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You happen upon a group of what looks like #ppurple #pfire #pspirits ~dancing~ around a large bonfire. NL `
1. `The spirits toss small bones and fragments into the fire, which ~brilliantly~ ~erupts~ each time. NL As you approach, the spirits all turn to you, expectantly...`
2. `You toss an offering into the bonfire. NL NL `
3. `However, the spirits aren't happy that you offered a #pCurse... The card fizzles a meek black smoke. You receive a... #p~something~ in return.`
4. `Nothing happens... NL The spirits seem to be ignoring you now. Disappointing...`
5. `The flames grow slightly brighter. NL The spirits continue dancing. You feel slightly warmer from their presence.. NL You #gheal #b5 HP.`
6. `The flames erupt, growing significantly stronger! NL The spirits dance around you excitedly, filling you with a ~sense~ ~of~ ~warmth.~ NL You are #ghealed to full HP.`
7. `The flames @burst,@ nearly knocking you off your feet, as the fire @doubles@ in strength. NL The spirits dance around you excitedly before ~merging~ ~into~ ~your~ ~form,~ filling you with warmth and strength. NL Your Max HP increases by #b10 and you are #ghealed to full HP.`

---

## Designer
- Class file: `mod/Spire1Code/Events/Designer.cs`, `public class Designer : Spire1Event`
- ID: `SPIRE1-DESIGNER`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Designer In-Spire`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "colorful_philosophers";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 20, 40, 50, 60, 75, 90, 110]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `Random.randomBoolean`
  - `AbstractImageEvent.update`
  - `OptionChosen.ordinal`
  - `SoundMaster.play`
  - `CardGroup.removeCard`
  - `AbstractCard.canUpgrade`
  - `Random.randomLong`
  - `AbstractCard.upgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `AbstractCard.makeStatEquivalentCopy`
  - `AbstractDungeon.transformCard`
  - `AbstractDungeon.getTransformedCard`
  - `CurrentScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.removeDialogOption`
  - `CardGroup.hasUpgradableCards`
  - `GenericEventDialog.updateDialogOption`
  - `CardGroup.getGroupWithoutBottledCards`
  - `CardGroup.size`
  - `AbstractPlayer.loseGold`
  - `CardGroup.getUpgradableCards`
  - `GridCardSelectScreen.open`
  - `CardGroup.getPurgeableCards`
  - `GenericEventDialog.loadImage`
  - `AbstractPlayer.damage`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Adjustments] #rLose #r`
2. `[Clean Up] #rLose #r`
3. `[Full Service] #rLose #r`
4. `[Punch] #rLose #r`
5. ` #rHP.`
6. ` #rGold. `
7. `#gUpgrade #g`
8. ` #grandom #gcards.`
9. `#gUpgrade #ga #gcard.`
10. `#gRemove #ga #gcard.`
11. `#gTransform #g`
12. ` #gcards.`
13. `#gRemove #ga #gcard #gand #gupgrade #ga #grandom #gcard.`
14. `[Leave]`
15. `Choose a Card to Upgrade`
16. `Choose 2 Cards to Transform`
17. `Choose a Card to Remove`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You discover a #g~colorful~ shop with the banner "IN-SPIRE" and walk in to see what's inside. NL "No, no way. Nope. Can't let you in!" NL NL A man with ridiculous clothing appears at the entrance to bar you.`
1. `"This will not do, no no. What is this style? @Disgusting!@ Are you #rbleeeeding? #p~Groooss.~ @Business??@ You a customer? Fine. ~Whaaatever."~ NL He lets out an exaggerated sigh and points at a list of services. NL NL The services seem fine, but you would rather punch this smug man in his smug face.`
2. `"Okay, bye bye now." NL NL ...should've punched him.`
3. `You punch him so hard your fist hurts. NL "My @FACE!!@ Now I'll have to-" NL NL He fainted. Who's #p~groooss~ and #rbleeeeding now?`

---

## Duplicator
- Class file: `mod/Spire1Code/Events/Duplicator.cs`, `public class Duplicator : Spire1Event`
- ID: `SPIRE1-DUPLICATOR`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Duplicator`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "amalgamator";`
- StS1 numeric constants found in the bytecode: [0, 1, 2]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `MusicMaster.playTempBGM`
  - `AbstractImageEvent.update`
  - `AbstractCard.makeStatEquivalentCopy`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `GridCardSelectScreen.open`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pray] #gDuplicate #ga #gcard #gin #gyour #gdeck.`
1. `[Leave]`
2. `Select a Card to Duplicate.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Before you lies a decorated altar to some ancient entity.`
1. `You kneel respectfully. A ghastly mirror image appears from the shrine and collides into you.`
2. `You ignore the shrine, confident in your choice.`

---

## FaceTrader
- Class file: `mod/Spire1Code/Events/FaceTrader.cs`, `public class FaceTrader : Spire1Event`
- ID: `SPIRE1-FACE_TRADER`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Face Trader`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "relic_trader";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 10, 15, 50, 75]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.gainGold`
  - `AbstractPlayer.damage`
  - `SoundMaster.play`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.clearAllDialogs`
  - `AbstractPlayer.hasRelic`
  - `Random.randomLong`
  - `RelicLibrary.getRelic`
  - `AbstractRelic.makeCopy`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Touch] #rLose #r`
1. ` #gGold.`
2. `[Trade] #g50%: #gGood #gFace. #r50%: #rBad #rFace.`
3. `[Leave]`
4. `[Continue]`
5. ` #rHP, #ggain #g`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You walk by an eerie statue holding several masks... NL Something behind you softly whispers, NL ~"Stop."~`
1. `You swerve around to face the statue which is now facing you! NL On closer inspection, it's not a statue but a statuesque, gaunt man. Is he even breathing? NL NL ~"Face.~ ~Let~ ~me~ ~touch?~ ~Maybe~ ~trade?"~`
2. `~"Compensation?~ ~Compensation."~ NL Mechanically, he cranes out a neat stack of #ygold and places it into your pouch. NL NL ~"What~ ~a~ ~nice~ ~face.~ ~Nice~ ~face."~ NL While he touches your face, you begin to feel your life drain out of it! NL During this, his mask falls off and shatters. Screaming, he quickly covers his face with all six arms dropping even more masks! Amidst all the screaming and shattering, you escape. NL NL His face was completely blank.`
3. `~"For~ ~me?~ @FOR@ @ME?@ ~Oh~ ~yes..~ ~Yes.~ ~Yes..~ ~mmm..."~ NL NL You see one of his arms flicker, and your face is in its hand! Your face has been swapped. NL NL ~"Nice~ ~face.~ ~Nice~ ~face."~`
4. `~"Stop.~ ~Stop.~ ~Stop.~ ~Stop.~ ~Stop."~ NL NL This was probably the right call.`

---

## FountainOfCurseRemoval
- Class file: `mod/Spire1Code/Events/FountainOfCurseRemoval.cs`, `public class FountainOfCurseRemoval : Spire1Event`
- ID: `SPIRE1-FOUNTAIN_OF_CURSE_REMOVAL`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Divine Fountain`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "wellspring";`
- StS1 numeric constants found in the bytecode: [0, 1, 2]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `MusicMaster.playTempBGM`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `CardGroup.removeCard`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Drink] #gRemove #gall #gCurses #gfrom #gyour #gdeck.`
1. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You come across #b~shimmering~ #b~water~ flowing endlessly from a fountain on a nearby wall.`
1. `As you drink the #b~water,~ you feel a #pdark #pgrasp loosen.`
2. `Unsure of the nature of this water, you continue on your way, parched.`

---

## GoldShrine
- Class file: `mod/Spire1Code/Events/GoldShrine.cs`, `public class GoldShrine : Spire1Event`
- ID: `SPIRE1-GOLD_SHRINE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Golden Shrine`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "sunken_treasury";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 15, 50, 100, 275]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CardLibrary.getCopy`
  - `MusicMaster.playTempBGM`
  - `AbstractImageEvent.update`
  - `RoomEventDialog.getSelectedOption`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.gainGold`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pray] #gGain #g`
1. ` #gGold.`
2. `[Desecrate] #gGain #g275 #gGold. #rBecome #rCursed #r- #rRegret.`
3. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Before you lies an elaborate shrine to an ancient spirit. `
1. `As your hand touches the shrine, #ygold rains from the ceiling ~showering~ ~you~ ~in~ ~riches.~ `
2. `Each time you strike the shrine, #ygold pours forth again and again! NL NL As you pocket the riches, something #rweighs #rheavily #ron #ryou.`
3. `You ignore the shrine.`

---

## GremlinMatchGame
- Class file: `mod/Spire1Code/Events/GremlinMatchGame.cs`, `public class GremlinMatchGame : Spire1Event`
- ID: `SPIRE1-GREMLIN_MATCH_GAME`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Match and Keep!`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "this_or_that";`
- StS1 numeric constants found in the bytecode: [0, 0.5, 0.7, 1, 1.25, 2, 3, 4, 5, 15, 80, 175, 210, 230, 290, 530, 640, 750, 780, 865, 1270, 1375, 2000]
- StS1 APIs the event calls (tells you what it does):
  - `Random.randomLong`
  - `GenericEventDialog.setDialogOption`
  - `AbstractDungeon.getCard`
  - `AbstractCard.makeCopy`
  - `AbstractDungeon.returnRandomCurse`
  - `AbstractDungeon.returnColorlessCard`
  - `AbstractPlayer.getStartCardForEvent`
  - `AbstractRelic.onPreviewObtainCard`
  - `AbstractCard.makeStatEquivalentCopy`
  - `AbstractImageEvent.update`
  - `CardGroup.update`
  - `GenericEventDialog.show`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.clearRemainingOptions`
  - `GenericEventDialog.getSelectedOption`
  - `CInputAction.isJustPressed`
  - `CInputAction.unpress`
  - `Hitbox.update`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.removeDialogOption`
  - `GenericEventDialog.hide`
  - `CardGroup.size`
  - `CardGroup.render`
  - `AbstractCard.render`
  - `FontHelper.renderSmartText`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Leave]`
2. `[Play]`
3. `Remaining Attempts: #y`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `" #bTwelve cards! Match them to keep them! #bFive tries, no do-overs. NL Are you ready? Let's start!"`
1. `You complete the gremlin's game and look up. NL He disappeared?`
2. `A gremlin is madly shuffling cards on a table. This monster seems to be a harmless one. You approach him out of curiosity.`

---

## GremlinWheelGame
- Class file: `mod/Spire1Code/Events/GremlinWheelGame.cs`, `public class GremlinWheelGame : Spire1Event`
- ID: `SPIRE1-GREMLIN_WHEEL_GAME`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Wheel of Change`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "endless_conveyor";`
- StS1 numeric constants found in the bytecode: [0, 0.1, 0.15, 0.25, 0.5, 1, 1.05, 1.25, 2, 3, 3.5, 4, 5, 6, 7, 8, 9, 10, 15, 32, 60, 64, 70, 100, 160, 180, 200, 256, 300, 330, 450, 500, 512, 770, 771, 1000, 1024, 1500]
- StS1 APIs the event calls (tells you what it does):
  - `ImageMaster.loadImage`
  - `GenericEventDialog.setDialogOption`
  - `Hitbox.move`
  - `AbstractImageEvent.update`
  - `MathHelper.cardLerpSnap`
  - `Hitbox.update`
  - `CInputAction.isJustPressed`
  - `SoundMaster.play`
  - `GenericEventDialog.clearAllDialogs`
  - `GenericEventDialog.show`
  - `GenericEventDialog.getSelectedOption`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.gainGold`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.hide`
  - `Random.random`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractRoom.addRelicToRewards`
  - `CombatRewardScreen.open`
  - `AbstractPlayer.heal`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `CardGroup.size`
  - `GridCardSelectScreen.open`
  - `RoomEventDialog.hide`
  - `AbstractPlayer.damage`
  - `CardGroup.removeCard`
  - `CInputAction.getKeyImg`

### Official option strings (StS1 order)
0. `[Play]`
1. `[Prize!] YAY!!!!`
2. `[Prize!] #gObtain #ga #gRelic.`
3. `[Prize!] #gHeal #gto #gfull #ghealth.`
4. `[Prize?] #rCurse #r- #rDecay.`
5. `[Prize!] #gRemove #ga #gcard #gfrom #gyour #gdeck.`
6. `[Prize?] #rLose #r`
7. ` #rHP.`
8. `[Leave]`
9. `Select a Card to Remove.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You come upon a dapper looking, cheery gremlin. NL "It's time to spin the wheel! Are you ~R~ ~E~ ~A~ ~D~ ~Y~ ~?~ Of course you are!"`
1. `"You win some #yGOLD! NL YAY!!!!"`
2. `"Ah, a #ggift! NL Enjoy!"`
3. `"Oooh, a free #gHeal for you!"`
4. `"Looks like you won a #pCurse! NL That's not good. NL Oh well! Better luck next time!"`
5. `"Ohh, the power of #r~darkness...~ NL Choose a card to remove from your deck!"`
6. `"Uh oh! NL You lose!" NL You spot him readying a shiv...`
7. `You slash at the crazy gremlin but he's simply too quick! NL He gets you a few times with a crude shiv. NL "The price has been paid!!" NL and with that, both the gremlin and its wheel disappear in a puff of smoke.`

---

## Lab
- Class file: `mod/Spire1Code/Events/Lab.cs`, `public class Lab : Spire1Event`
- ID: `SPIRE1-LAB`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Lab`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "potion_courier";`
- StS1 numeric constants found in the bytecode: [0, 1, 15]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.hide`
  - `AbstractDungeon.getCurrRoom`
  - `PotionHelper.getRandomPotion`
  - `CombatRewardScreen.open`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Search] #gFind #gsome #gPotions!`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You find yourself in a room filled with racks of test tubes, beakers, flasks, forceps, pinch clamps, stirring rods, tongs, goggles, funnels, pipets, cylinders, condensers, and even a rare spiral tube of glass. NL NL Why do you know the name of all these tools? It doesn't matter, you take a look around.`

---

## Nloth
- Class file: `mod/Spire1Code/Events/Nloth.cs`, `public class Nloth : Spire1Event`
- ID: `SPIRE1-NLOTH`  (auto-derived from the class name; do NOT set it manually)
- Title text: `N'loth`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "welcome_to_wongos";`
- StS1 numeric constants found in the bytecode: [0, 1, 2]
- StS1 APIs the event calls (tells you what it does):
  - `Random.randomLong`
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.hasRelic`
  - `AbstractEvent.logMetricRelicSwap`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `AbstractPlayer.loseRelic`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractEvent.logMetricIgnored`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Offer: `
1. `] #rLose #rthis #rrelic. #gObtain #ga #gspecial #grelic.`
2. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `An odd creature with a hunched back sprouting several tentacles is scrounging through a pile of trash and debris in front of you. As you approach, he shuffles towards you in a non-threatening manner. NL "N'loth hungry. Feed N'loth."`
1. `Holding the #yrelic out towards him, N’loth snatches it out of your hand with his tentacles, dislocates his jaw, and slurps down your offer in one quick gulp. NL He gives you a large, toothy grin as more tentacles appear from behind his cloak, these ones brandishing an impossibly neat looking box. He pushes it towards you until you take it.`
2. `You shake your head. N'loth hunches even further and sighs, then scuttles away.`

---

## NoteForYourself
- Class file: `mod/Spire1Code/Events/NoteForYourself.cs`, `public class NoteForYourself : Spire1Event`
- ID: `SPIRE1-NOTE_FOR_YOURSELF`  (auto-derived from the class name; do NOT set it manually)
- Title text: `A Note For Yourself`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "round_tea_party";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRelic.onObtainCard`
  - `CardGroup.addToTop`
  - `AbstractRelic.onMasterDeckChange`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractImageEvent.update`
  - `CardGroup.removeCard`
  - `Prefs.getString`
  - `CardLibrary.getCard`
  - `AbstractCard.makeCopy`
  - `Prefs.getInteger`
  - `AbstractCard.upgrade`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Take and Give] #gReceive `
2. ` #gand #gStore #ga #gCard.`
3. `[Ignore]`
4. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You spot a loose brick within a pillar that catches your eye.`
1. `You find a folded note and a #ycard inside. It reads, NL "The Heart awaits." NL NL This is your handwriting.`
2. `Choose a Card to Store.`
3. `What is going on?`

---

## PurificationShrine
- Class file: `mod/Spire1Code/Events/PurificationShrine.cs`, `public class PurificationShrine : Spire1Event`
- ID: `SPIRE1-PURIFICATION_SHRINE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Purifier`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "whispering_hollow";`
- StS1 numeric constants found in the bytecode: [0, 1, 2]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `MusicMaster.playTempBGM`
  - `AbstractImageEvent.update`
  - `SoundMaster.play`
  - `CardGroup.removeCard`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pray] #gRemove #ga #gcard #gfrom #gyour #gdeck.`
1. `[Leave]`
2. `Select a Card to Remove.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Before you lies an elaborate shrine to a forgotten spirit.`
1. `As you kneel in reverence, you feel a weight lifted off your shoulders.`
2. `You ignore the shrine.`

---

## Transmogrifier
- Class file: `mod/Spire1Code/Events/Transmogrifier.cs`, `public class Transmogrifier : Spire1Event`
- ID: `SPIRE1-TRANSMOGRIFIER`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Transmogrifier`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "morphic_grove";`
- StS1 numeric constants found in the bytecode: [0, 1, 2]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `MusicMaster.playTempBGM`
  - `AbstractImageEvent.update`
  - `CardGroup.removeCard`
  - `AbstractDungeon.transformCard`
  - `AbstractDungeon.getTransformedCard`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pray] #gTransform #ga #gcard.`
1. `[Leave]`
2. `Select a Card to Transform.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Before you lies an elaborate shrine to a forgotten spirit.`
1. `As the power of the shrine flows through you, your mind feels altered.`
2. `You ignore the shrine.`

---

## UpgradeShrine
- Class file: `mod/Spire1Code/Events/UpgradeShrine.cs`, `public class UpgradeShrine : Spire1Event`
- ID: `SPIRE1-UPGRADE_SHRINE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Upgrade Shrine`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "tinker_time";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3]
- StS1 APIs the event calls (tells you what it does):
  - `CardGroup.hasUpgradableCards`
  - `GenericEventDialog.setDialogOption`
  - `MusicMaster.playTempBGM`
  - `AbstractImageEvent.update`
  - `AbstractCard.upgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `AbstractCard.makeStatEquivalentCopy`
  - `CUR_SCREEN.ordinal`
  - `AbstractDungeon.getCurrRoom`
  - `GenericEventDialog.updateBodyText`
  - `CardGroup.getUpgradableCards`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pray] #gUpgrade #ga #gcard.`
1. `[Leave]`
2. `Select a Card to Upgrade.`
3. `[Locked] Requires: Upgradeable Cards`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Before you lies an elaborate shrine to a forgotten spirit.`
1. `The shrine's energy flows into you, making you stronger.`
2. `You ignore the shrine.`

---

## WeMeetAgain
- Class file: `mod/Spire1Code/Events/WeMeetAgain.cs`, `public class WeMeetAgain : Spire1Event`
- ID: `SPIRE1-WE_MEET_AGAIN`  (auto-derived from the class name; do NOT set it manually)
- Title text: `We Meet Again!`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "relic_trader";`
- StS1 numeric constants found in the bytecode: [0, 0.28, 1, 2, 3, 4, 5, 6, 7, 8, 9, 50, 150]
- StS1 APIs the event calls (tells you what it does):
  - `AbstractPlayer.getRandomPotion`
  - `FontHelper.colorString`
  - `GenericEventDialog.setDialogOption`
  - `AbstractCard.makeStatEquivalentCopy`
  - `Random.randomLong`
  - `Random.random`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.removePotion`
  - `AbstractPlayer.loseGold`
  - `CardGroup.removeCard`
  - `ScreenShake.shake`
  - `SoundMaster.play`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Give Potion] #rLose `
1. `[Locked] Requires: Potion.`
2. `[Give Gold] #rLose #r`
3. `[Locked] Requires: At least 50 Gold.`
4. `[Give Card] #rLose `
5. `[Locked] Requires: Card.`
6. `. #gObtain #ga #gRelic.`
7. `[Attack]`
8. `[Leave]`
9. ` #rGold`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `"We meet again!" NL A cheery disheveled fellow approaches you gleefully. You do not know this man. NL NL "It's me, #yRanwid! Have any goods for me today? The usual? A fella like me can't make it alone, you know?" NL You eye him suspiciously and consider your options...`
1. `"Exquisite! Was feeling parched." NL #b~Glup~ #b~glup~ #b~glup~ NL NL He downs the potion in one go and lets out a satisfied @burp.@`
2. `"Magnificent! This will be quite handy if I run into those #rmask #rwearing #rhoodlums again."`
3. `"Exemplary! I shall study this further in my chambers."`
4. `" @Aaaaagghh!!@ What a jerk you are sometimes!" NL He runs away.`
5. `  NL NL He rummages around his various pockets... NL "Here, look what I've got for you today! Take it take it!"`

---

## WomanInBlue
- Class file: `mod/Spire1Code/Events/WomanInBlue.cs`, `public class WomanInBlue : Spire1Event`
- ID: `SPIRE1-WOMAN_IN_BLUE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Woman in Blue`
- Act: leave `Acts` unoverridden (shared event, any act)
- Portrait: `protected override string ShippedPortrait => "the_future_of_potions";`
- StS1 numeric constants found in the bytecode: [0, 0.05, 1, 2, 3, 4, 5, 6, 15, 20, 30, 40]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CurScreen.ordinal`
  - `AbstractPlayer.loseGold`
  - `GenericEventDialog.updateBodyText`
  - `AbstractDungeon.getCurrRoom`
  - `PotionHelper.getRandomPotion`
  - `CombatRewardScreen.open`
  - `ScreenShake.shake`
  - `SoundMaster.play`
  - `AbstractPlayer.damage`
  - `GenericEventDialog.clearAllDialogs`
  - `AbstractEvent.logMetric`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Buy 1 Potion] #y`
1. `[Buy 2 Potions] #y`
2. `[Buy 3 Potions] #y`
3. ` #yGold.`
4. `[Leave]`
5. `[Leave] #rTake #r`
6. ` #rDamage.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `From the darkness, an arm pulls you into a small shop. As your eyes adjust, you see a pale woman in sharp clothes gesturing towards a wall of potions. NL "Buy a potion. Now!" she states.`
1. `"Good. Now leave." NL You exit the shop cautiously.`
2. `#r@WHAM@ NL Her gloved fist collides with your face, nearly knocking you off your feet. NL "Get out before I litter the floor with your guts." You take her word and exit with your guts still safely in your body.`

