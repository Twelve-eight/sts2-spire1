# StS1 event spec - The City (Act 2 / Underdocks)

15 events. Every string below is the OFFICIAL StS1 English text extracted from desktop-1.0.jar; use it verbatim.
`NL` in StS1 text is a line break marker - DELETE it, StS2 does not use it. Keep sentences, drop the marker.
StS1 color codes (`#r`, `#g`, `#b`, `#y`, `#p`) are StS1 markup - DELETE them too; StS2 marks keywords with `*word*`.

---

## Addict
- Class file: `mod/Spire1Code/Events/Addict.cs`, `public class Addict : Spire1Event`
- ID: `SPIRE1-ADDICT`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Pleading Vagrant`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "ranwid_the_elder";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 85]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CardLibrary.getCopy`
  - `GenericEventDialog.updateBodyText`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractEvent.logMetricObtainRelicAtCost`
  - `AbstractPlayer.loseGold`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractEvent.logMetricObtainCardAndRelic`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Offer Gold] #y`
1. ` #yGold: #gObtain #ga #gRelic.`
2. `[Locked] Requires: `
3. ` Gold.`
4. `[Rob] #gObtain #ga #gRelic. #rBecome #rCursed #r- #rShame.`
5. `[Leave]`
6. ``
7. ``

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `While sneaking past a group of shrouded figures, one of them approaches you. NL "Got anything for me friend? Please... maybe some #yCoin?" NL "I just need somewhere to stay, I have treasures I can trade..." NL He seems delusional, but harmless.`
1. `"Oh yes, ~yes!~ Here here, a fair trade!"`
2. `You snatch the precious #yrelic from his clutches and walk away. NL From behind you hear, NL "Have you no shame? ~HAVE~ ~YOU~ ~NO~ ~SHAAAAAME?!"~ NL You have some #pshame.`

---

## BackToBasics
- Class file: `mod/Spire1Code/Events/BackToBasics.cs`, `public class BackToBasics : Spire1Event`
- ID: `SPIRE1-BACK_TO_BASICS`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Ancient Writing`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "tablet_of_truth";`
- StS1 numeric constants found in the bytecode: [0, 0.1, 0.2, 0.8, 0.9, 1, 2, 3]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `AbstractImageEvent.update`
  - `AbstractEvent.logMetricCardRemoval`
  - `CardGroup.removeCard`
  - `CUR_SCREEN.ordinal`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `CardGroup.size`
  - `GenericEventDialog.updateBodyText`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractCard.hasTag`
  - `AbstractCard.canUpgrade`
  - `AbstractCard.upgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `AbstractCard.makeStatEquivalentCopy`
  - `AbstractEvent.logMetricUpgradeCards`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Elegance] #gRemove #ga #gcard #gfrom #gyour #gdeck.`
1. `[Simplicity] #gUpgrade #gall #gStrikes #gand #gDefends.`
2. `Select a Card to Remove.`
3. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Scaling the city, you notice a wall covered in the writing of #y~Ancients.~ As you try to wrap your head around what the puzzling symbols and glyphs could mean, the writing begins to #b~glow.~ NL Suddenly, the message becomes clear...`
1. `The answer was elegance. NL Of course.`
2. `The truth is always simple.`

---

## Beggar
- Class file: `mod/Spire1Code/Events/Beggar.cs`, `public class Beggar : Spire1Event`
- ID: `SPIRE1-BEGGAR`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Old Beggar`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "zen_weaver";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 75]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `AbstractImageEvent.update`
  - `SoundMaster.play`
  - `CardGroup.removeCard`
  - `CurScreen.ordinal`
  - `GenericEventDialog.loadImage`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.loseGold`
  - `GenericEventDialog.clearAllDialogs`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.getGroupWithoutBottledCards`
  - `GridCardSelectScreen.open`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Offer Gold] #y`
1. ` #yGold: #gRemove #ga #gcard #gfrom #gyour #gdeck.`
2. `[Locked] Requires: `
3. ` Gold.`
4. `[Continue]`
5. `[Leave]`
6. `Select a Card to Remove.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `An old beggar cloaked in fur reaches his hands out towards you as you pass. "Spare some coin, child?"`
1. `The beggar looks to the floor as you pass. NL "You will never make a difference... You never do."`
2. `The beggar takes off its cloak to reveal that he is #bCleric! NL @"You@ @are@ @a@ @kind@ @soul.@ @Receive@ @my@ @purification!"@ he screams. NL You are unsure if he is grateful or mad.`
3. `@"I@ @hope@ @you@ @do@ @better@ @this@ @time,@ @friend!"@ he shouts. NL Wondering what was implied by this, you push forward.`

---

## Colosseum
- Class file: `mod/Spire1Code/Events/Colosseum.cs`, `public class Colosseum : Spire1Event`
- ID: `SPIRE1-COLOSSEUM`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Colosseum`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "trial";`
- StS1 numeric constants found in the bytecode: [0, 0.25, 1, 2, 3, 4, 100]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractDungeon.getCurrRoom`
  - `MonsterHelper.getEncounter`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractRoom.addRelicToRewards`
  - `AbstractRoom.addGoldToRewards`
  - `AbstractEvent.logMetric`
  - `AbstractDungeon.resetPlayer`
  - `AbstractPlayer.preBattlePrep`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Fight]`
2. `[COWARDICE] Escape.`
3. `[VICTORY] A powerful fight with many rewards.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `@Thwack!!!@ NL .. NL ...... NL ... NL ~You~ ~were~ ~knocked~ ~unconscious...~`
1. `~Groggy~ and with a @throbbing@ head, you awaken to find yourself thrown in the center of a massive stadium with an overflowing audience of #bSlavers, #pCultists, and other denizens of the City! NL `
2. ` NL An armored giant with a #ygolden #ycrown bellows at you from atop, NL @"WE@ @NOW@ @BEGIN@ @THE@ @`
3. `TH@ @COMBAT!!!!"@ NL A gate on the opposite side opens...`
4. `@"WELL@ @DONE,@ @WEAKLING!"@ NL The giant mock claps whilst he riles up the crowd with exaggerated gestures. NL #y~Gold~ ~and~ #y~confetti~ ~shower~ ~you!~ NL @"TIME@ @FOR@ @THE@ @REAL@ @CHALLENGE!!"@ NL NL The last battle left a small opening in the Colosseum's wall, you can easily escape through there while everyone is distracted. NL Do you stay and fight?`

---

## CursedTome
- Class file: `mod/Spire1Code/Events/CursedTome.cs`, `public class CursedTome : Spire1Event`
- ID: `SPIRE1-CURSED_TOME`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Cursed Tome`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "self_help_book";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7, 10, 15]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CurScreen.ordinal`
  - `GenericEventDialog.clearAllDialogs`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.damage`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractPlayer.hasRelic`
  - `RelicLibrary.getRelic`
  - `AbstractRelic.makeCopy`
  - `Random.random`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.addRelicToRewards`
  - `CombatRewardScreen.open`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Read]`
1. `[Continue] #rLose #r1 #rHP.`
2. `[Continue] #rLose #r2 #rHP.`
3. `[Continue] #rLose #r3 #rHP.`
4. `[Stop] #rLose #r3 #rHP.`
5. `[Take] #gObtain #gthe #gBook. #rLose #r`
6. ` #rHP.`
7. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `In an abandoned temple, you find a giant book, open, riddled with #p@cryptic@ #p@writings.@ NL NL As you try to interpret the elaborate script, it begins to #b~shift~ and #b~morph~ into writing you are familiar with.`
1. `Odd. The book seems to be about an #yAncient named #yNeow. NL NL This piques your interest, but you have a general feeling of #p~malaise.~`
2. `The Ancient of Resurrection, #yNeow, was exiled to the bottom of the Spire. NL NL You feel compelled to read more, but your body begins to #r~ache.~`
3. `Seeking vengeance, #yNeow blesses outsiders, using them for her own purposes. NL NL You are starting to feel very #r~weak~ #r~and~ #r~tired...~`
4. `Those resurrected by #yNeow remember only fragments of their past selves, cursed to fight for eternity. NL NL As you near the final page, your #r@old@ #r@wounds@ #r@begin@ #r@to@ #r@reopen!@`
5. `Upon finishing the tome, you decide to take it with you. With proof in hand, will you retain your memories?`
6. `You exit, feeling a #p~dark~ #p~energy~ #p~emanating~ from the book on the pedestal.`
7. `With incredible strain and willpower, you resist the trance of the tome and @SLAM@ it shut. NL You turn and exit the temple, feeling #b~drained...~`

---

## DrugDealer
- Class file: `mod/Spire1Code/Events/DrugDealer.cs`, `public class DrugDealer : Spire1Event`
- ID: `SPIRE1-DRUG_DEALER`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Augmenter`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "the_future_of_potions";`
- StS1 numeric constants found in the bytecode: [0, 0.25, 1, 2, 3, 4, 5, 6]
- StS1 APIs the event calls (tells you what it does):
  - `CardLibrary.getCopy`
  - `GenericEventDialog.setDialogOption`
  - `CardGroup.getPurgeableCards`
  - `CardGroup.size`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractPlayer.hasRelic`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `AbstractImageEvent.update`
  - `AbstractCard.untip`
  - `AbstractCard.unhover`
  - `CardGroup.removeCard`
  - `AbstractDungeon.transformCard`
  - `AbstractDungeon.getTransformedCard`
  - `AbstractCard.makeCopy`
  - `GridCardSelectScreen.open`
  - `DynamicBanner.hide`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Test J.A.X.] #gGet #gJAXXED.`
1. `[Become Test Subject] #gTransform #g2 #gcards.`
2. `[Ingest Mutagens] #gObtain #ga #gspecial #grelic.`
3. `[Leave]`
4. `[Locked] Requires: 2 or more cards in deck.`
5. `Select Cards to Transform.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `A man with an eyepatch and a devilish grin strides up to you. NL "Hey there, stranger. Interested in advancing science? I can make you stronger than any training or blessing. You're gonna need it if you're one of those heroes with a death wish." NL NL ~"Whad'ya~ ~say?"~`
1. `~"Excellent."~ NL The man hands over a dangerous looking syringe filled with a #b~glowing~ #b~liquid~ before skulking off into a shadowy alleyway.`
2. `~"Superb."~ NL The man injects you with three unknown substances and pulls out a notepad. As you begin to feel light-headed, he starts to frantically write down notes. NL Losing track of time completely, by the time you regain your senses, the shady character has disappeared.`
3. `~"Marvelous."~ NL You quaff the mysterious substance. Immediately, you are invigorated and feel your muscle fibers @twitch.@`

---

## ForgottenAltar
- Class file: `mod/Spire1Code/Events/ForgottenAltar.cs`, `public class ForgottenAltar : Spire1Event`
- ID: `SPIRE1-FORGOTTEN_ALTAR`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Forgotten Altar`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "abyssal_baths";`
- StS1 numeric constants found in the bytecode: [0, 0.25, 0.35, 1, 2, 3, 4, 5, 6, 15]
- StS1 APIs the event calls (tells you what it does):
  - `AbstractPlayer.hasRelic`
  - `GenericEventDialog.setDialogOption`
  - `CardLibrary.getCopy`
  - `SoundMaster.play`
  - `AbstractPlayer.increaseMaxHp`
  - `AbstractPlayer.damage`
  - `ScreenShake.shake`
  - `AbstractDungeon.getCurrRoom`
  - `RelicLibrary.getRelic`
  - `AbstractRelic.makeCopy`
  - `AbstractRoom.spawnRelicAndObtain`
  - `AbstractRelic.onUnequip`
  - `AbstractRelic.instantObtain`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Offer: Golden Idol] #gObtain #ga #gspecial #gRelic. #rLose #rGolden #rIdol.`
1. `[Locked] Requires: Golden Idol.`
2. `[Sacrifice] #gGain #g`
3. ` #gMax #gHP. #rLose #r`
4. ` #rHP.`
5. `[Locked] Requires: Humanity.`
6. `[Desecrate] #rBecome #rCursed #r- #rDecay.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `In front of you sits an altar to a forgotten god. NL Atop the altar sits an ornate female statue with arms outstretched. NL She calls out to you, demanding sacrifice.`
1. `As you gently set the idol onto the altar a #bcold wind swirls throughout the room. NL The arms of the statue begin to discolor and crumble. NL NL Your #ygolden #yidol begins to dull in color and begins bleeding from its eyes. NL The bleeding never ceases.`
2. `You stand on the altar and cut your wrists. NL As the #rblood spills out in sacrifice, the arms of the statue reach out and close around your eyes. NL Everything goes dark. NL You wake up a short time later feeling a new potential surging through you.`
3. `You lash out and smash the statue in front of you, breaking the magical hold the room had placed upon you. NL A dark wail echoes all around you, and you can feel the #p~cursed~ #p~magic~ seep into your bones.`

---

## Ghosts
- Class file: `mod/Spire1Code/Events/Ghosts.cs`, `public class Ghosts : Spire1Event`
- ID: `SPIRE1-GHOSTS`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Council of Ghosts`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "reflections";`
- StS1 numeric constants found in the bytecode: [0, 0.5, 1, 2, 3, 5, 15]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.decreaseMaxHealth`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Accept] #gReceive #g5 Apparition. #rLose #r`
1. ` #rMax #rHP.`
2. `[Refuse]`
3. `[Accept] #gReceive #g3 Apparition. #rLose #r`
4. ``
5. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you continue your ascent, #p~thick~ #p~black~ #p~smoke~ begins to billow out of the ground and walls around you, coalescing into three masked forms that start to speak. NL NL ~"Another~ ~puppet~ ~of~ #y~Neow~ ~I~ ~think."~ NL #r@"AGREED!@ #r@SHE@ #r@ALWAYS@ #r@MAKES@ #r@THE@ #r@FUNNEST@ #r@TOYS!"@ NL NL You notice an over-sized grin as the third addresses you. NL "Ignore the others... Would you like a taste of our #y~power?"~ `
1. ``
2. `#y"Excellent!" NL As the ghostly shape speaks, you notice its large mouth opening wider and wider. #p~Thick~ #p~black~ #p~smoke~ spews forth and envelops the room. You cannot see or breathe... NL NL Just before you lose consciousness, the sensation stops. NL NL Whatever those things were, they are gone now. You continue on, feeling rather #bhollow.`
3. `"How disappointing..." NL ~"You~ ~will~ ~join~ ~us~ ~sooner~ ~or~ ~later."~ NL #r@"HA@ #r@HA@ #r@HA@ #r@HAHAHA!"@ NL NL The shapes fade away, leaving only the unnerving laughter ringing in your ears.`
4. ``

---

## KnowingSkull
- Class file: `mod/Spire1Code/Events/KnowingSkull.cs`, `public class KnowingSkull : Spire1Event`
- ID: `SPIRE1-KNOWING_SKULL`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Knowing Skull`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "brain_leech";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7, 8, 90]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.clearAllDialogs`
  - `AbstractPlayer.damage`
  - `AbstractPlayer.hasRelic`
  - `AbstractPlayer.getRelic`
  - `AbstractRelic.flash`
  - `PotionHelper.getRandomPotion`
  - `AbstractPlayer.obtainPotion`
  - `AbstractPlayer.gainGold`
  - `AbstractDungeon.returnColorlessCard`
  - `AbstractCard.makeCopy`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. ` #rHP.`
2. `[Information?] #gReveal #gthe #gBoss. #rLose #r`
3. `[Success?] #gGet #ga #gColorless #gCard. #rLose #r`
4. `[A Pick Me Up?] #gGet #ga #gPotion. #rLose #r`
5. `[Riches?] #gGain #g`
6. ` #gGold. #rLose #r`
7. `[How do I leave?] #rLose #r`
8. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You find yourself in an old, decorated chamber. In the center of the room, a large skull sits atop an ornate pedestal. As you approach, the skull #g@bursts@ #g@into@ #g@flames@ and turns to face you.`
1. `"WHAT IS IT YOU SEEK? WHAT IS IT YOU OFFER?" NL In sync with its final words, the door behind you @slams@ @shut.@`
2. ` NL NL "ANYTHING ELSE?"`
3. ` `
4. `"DRINK UP!" NL You obtain a potion.`
5. `"PERHAPS THIS WILL HELP?" NL You obtain a card.`
6. `"YOU MORTALS NEVER CHANGE. IT IS DONE." NL #yGold rains down on you!`
7. `"BEHIND YOU, MORTAL." NL You peek behind the skull. Surely enough, there is a door.`

---

## MaskedBandits
- Class file: `mod/Spire1Code/Events/MaskedBandits.cs`, `public class MaskedBandits : Spire1Event`
- ID: `SPIRE1-MASKED_BANDITS`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Masked Bandits`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "punch_off";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 25, 30, 35]
- StS1 APIs the event calls (tells you what it does):
  - `RoomEventDialog.addDialogOption`
  - `AbstractDungeon.getCurrRoom`
  - `MonsterHelper.getEncounter`
  - `AbstractEvent.update`
  - `RoomEventDialog.getSelectedOption`
  - `CurScreen.ordinal`
  - `AbstractPlayer.loseGold`
  - `RoomEventDialog.updateBodyText`
  - `RoomEventDialog.updateDialogOption`
  - `RoomEventDialog.clearRemainingOptions`
  - `Random.random`
  - `AbstractRoom.addGoldToRewards`
  - `AbstractPlayer.hasRelic`
  - `AbstractRoom.addRelicToRewards`
  - `SoundMaster.play`
  - `MonsterGroup.getRandomMonster`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Pay] #rLose #rALL of your #yGold.`
1. `[Fight!]`
2. `[Continue]`
3. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Hehehe.. Thanks for the #ygold! NL Oh, I love #ygold. It's so nice. NL #y~shiny~ #y~shiny~ chits they are!`
1. `Hey #bBear, hey! NL This guy gave us all his #ygold! What a sucker, right? NL Get this, I just had to ask nicely. Who knew?! NL I certainly didn't! What a chump!`
2. `Gang, let's all have a laugh for this wondrous occasion! NL @Hahaah@ NL @Ho@ @HOH@ ~hoho!~ NL ~Hoh!~`
3. `Oh? You're still here? NL Did you overhear something? Didn't think so. NL #r@*snerk*@ ~...loser....~ @Hahaha@ ~haaah~`
4. `You encounter a group of bandits wearing large #rred #rmasks. NL "Hello, pay up to pass... a reasonable fee of @ALL@ your #ygold will do! Heh heh!"`

---

## Nest
- Class file: `mod/Spire1Code/Events/Nest.cs`, `public class Nest : Spire1Event`
- ID: `SPIRE1-NEST`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Nest`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "byrdonis_nest";`
- StS1 numeric constants found in the bytecode: [0, 0.3, 1, 2, 3, 4, 5, 6, 15, 50, 99]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `GenericEventDialog.updateBodyText`
  - `UnlockTracker.markCardAsSeen`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.gainGold`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractPlayer.damage`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Stay in Line] #gObtain Ritual Dagger. #rLose #r`
1. ` #rHP.`
2. `[Smash and Grab] #gObtain #g`
3. ` #gGold.`
4. `[Leave]`
5. `[Continue]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `A long line of #b~hooded~ #b~figures~ can be seen entering NL an #punassuming #pcathedral.`
1. `Naturally, you join the line and are quickly surrounded by #rCultists! NL They ignore you as they gleefully @chant@ and ~wave~ their weapons around. NL NL #r@"MURDER!!@ #r@MURDER@ #r@MURDER!!"@ NL #b~"CAW~ #b~CAW~ #b~CAAAAAWWW!"~ NL #r@"MURDER!@ #r@MURDER@ #r@MUURDER!!"@ NL #b~"CAAW~ #b~CAW~ #b~CAAAAAAWW!!"~ NL NL You eye a #yDonation #yBox...`
2. `You decide to stay in line (out of fear) to see what will happen. NL NL Eventually, you are face-to-face with the leader. A well-dressed cultist hands you an #yOrnate #yDagger. Like the others before you, you slash your forearm and let the blood drip into a misshapen bowl. NL NL The cultists @chant@ and @holler@ for you! NL NL #b~"CAAW~ #b~CAW~ #b~CAAAAAAWW!!"~ NL NL You chant, too. Why not?`
3. `They didn't even notice.`
4. ``
5. ``

---

## TheJoust
- Class file: `mod/Spire1Code/Events/TheJoust.cs`, `public class TheJoust : Spire1Event`
- ID: `SPIRE1-THE_JOUST`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Joust`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "battleworn_dummy";`
- StS1 numeric constants found in the bytecode: [0, 0.01, 0.25, 0.3, 1, 2, 3, 4, 5, 6, 7, 8, 50, 100, 250]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `AbstractImageEvent.update`
  - `ScreenShake.shake`
  - `SoundMaster.play`
  - `CUR_SCREEN.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.loseGold`
  - `GenericEventDialog.clearRemainingOptions`
  - `Random.randomBoolean`
  - `AbstractPlayer.gainGold`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Murderer] #yBet #y`
2. ` #yGold - #g70%: #gwin #g`
3. ` #gGold.`
4. `[Owner] #yBet #y`
5. ` #yGold - #g30%: #gwin #g`
6. `[Watch]`
7. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you make your way through the large buildings you come across a long narrow bridge and spot knights on either side, facing one another. You approach... NL @"Halt!"@ NL NL A knight forcefully gestures you to stop with its giant lance.`
1. `"Today is the day I must settle the score with the #rmurderer of my beloved pet, #yNoodles. Until then, you may not pass. NL NL Fellow witness, why don't you #bbet on who you think will emerge victorious?"`
2. `"I can't believe you're betting against #yNoodles!" NL NL Furious, he clamps down his helmet and rushes towards his nemesis.`
3. `"Give me strength, #yNoodles!" NL NL Clamping down his helmet, the knight charges forward.`
4. `NL #y@*CRASH!!!*@ #r@*KLAAAANG!*@ NL NL #g@*POW!*@`
5. `The nemesis was slain. NL NL `
6. `The owner died. NL NL `
7. `You #gwin the bet. Unsure what to think, you grab your winnings and leave.`
8. `You #rlost the bet, but at least you weren't gouged by a lance.`

---

## TheLibrary
- Class file: `mod/Spire1Code/Events/TheLibrary.cs`, `public class TheLibrary : Spire1Event`
- ID: `SPIRE1-THE_LIBRARY`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Library`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "waterlogged_scriptorium";`
- StS1 numeric constants found in the bytecode: [0, 0.2, 0.33, 1, 2, 3, 4, 15, 20]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `AbstractImageEvent.update`
  - `AbstractCard.makeCopy`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractDungeon.rollRarity`
  - `AbstractDungeon.getCard`
  - `CardGroup.contains`
  - `AbstractRelic.onPreviewObtainCard`
  - `CardGroup.addToBottom`
  - `UnlockTracker.markCardAsSeen`
  - `GridCardSelectScreen.open`
  - `AbstractPlayer.heal`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Read] #gChoose #g1 #gof #g20 #gcards #gto #gadd #gto #gyour #gdeck.`
1. `[Sleep] #gHeal #g`
2. ` #gHP.`
3. `[Leave]`
4. `Add a card to your deck.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You come across an ornate building which appears abandoned. NL A plaque that has been torn free from a wall is on the floor. It reads, #b"THE #bLIBRARY". NL Inside, you find countless rows of scrolls, manuscripts, and books. NL You pick one and cozy yourself into a chair for some quiet time.`
1. `Reading is for chumps. NL You doze off in a comfy chair instead. NL ~Zzz...~ ~zzz...~ ~..Zz....~ NL You wake up feeling refreshed.`
2. `The story is about an insect-controlling teenage girl who aspires to become a hero. The book is filled with creative uses of powers, combat strategies, and varying perspectives. NL Satisfying.`
3. `The story is about a man who journeyed beyond the stars and found himself stuck on a desolate foreign planet. Ingenuity, luck, perseverance, and humor to retain his sanity were his tools to return home. NL Fascinating.`
4. `The story takes place in a giant isolated building underground as the outside conditions have become unbearable. The novel is mired with conspiracies, propaganda, and injustice. You ponder if similar dynamics are at play within the Spire. NL Unsettling.`

---

## TheMausoleum
- Class file: `mod/Spire1Code/Events/TheMausoleum.cs`, `public class TheMausoleum : Spire1Event`
- ID: `SPIRE1-THE_MAUSOLEUM`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Mausoleum`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "grave_of_the_forgotten";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 15, 50, 100]
- StS1 APIs the event calls (tells you what it does):
  - `CardLibrary.getCopy`
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `AbstractImageEvent.update`
  - `CurScreen.ordinal`
  - `Random.randomBoolean`
  - `GenericEventDialog.updateBodyText`
  - `ScreenShake.rumble`
  - `AbstractDungeon.returnRandomRelicTier`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.clearAllDialogs`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Open Coffin] #gObtain #ga #gRelic. #r`
1. `%: #rBecome #rCursed #r- #rWrithe.`
2. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Venturing through a series of tombs, you are faced with a large sarcophagus ~studded~ ~with~ ~gems~ in the center of a circular room. NL You cannot make out the writing on the coffin, however, you do notice #p~black~ #p~fog~ seeping out from the sides.`
1. `You push open the coffin. As you do, #p~black~ #p~fog~ spews forth and covers the entire room! Inside, you find no body, only a #y~relic.~ You take it and move onwards, #r@coughing@ #r@violently.@`
2. `You push open the coffin. The fog dissipates harmlessly. Inside, you find the mortal remains of a decorated soldier grasping an old #yrelic. You pilfer it and move on.`
3. `You continue along your way, leaving the forgotten dead undisturbed.`

---

## Vampires
- Class file: `mod/Spire1Code/Events/Vampires.cs`, `public class Vampires : Spire1Event`
- ID: `SPIRE1-VAMPIRES`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Vampires(?)`
- Act: `public override ActModel[] Acts => Act2;`
- Portrait: `protected override string ShippedPortrait => "spirit_grafter";`
- StS1 numeric constants found in the bytecode: [0, 0.3, 1, 2, 3, 4, 5]
- StS1 APIs the event calls (tells you what it does):
  - `AbstractPlayer.getVampireText`
  - `AbstractPlayer.hasRelic`
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.decreaseMaxHealth`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractPlayer.loseRelic`
  - `CardGroup.removeCard`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Accept] #gRemove #gall Strikes. #gReceive #g5 Bites. #rLose #r`
1. ` #rMax #rHP.`
2. `[Refuse]`
3. `[Lose `
4. `] #gRemove #gall Strikes. #gReceive #g5 Bites.`
5. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Navigating an unlit street, you come across several hooded figures in the midst of some dark ritual. As you approach, they turn to you in eerie unison. The tallest among them bares fanged teeth and extends a long, pale hand towards you. NL ~"Join~ ~us~ ~brother,~ ~and~ ~feel~ ~the~ ~warmth~ ~of~ ~the~ ~Spire."~`
1. `Navigating an unlit street, you come across several hooded figures in the midst of some dark ritual. As you approach, they turn to you in eerie unison. The tallest among them bares fanged teeth and extends a long, pale hand towards you. NL ~"Join~ ~us~ ~sister,~ ~and~ ~feel~ ~the~ ~warmth~ ~of~ ~the~ ~Spire."~`
2. `The tall figure grabs your arm, pulls you forward, and sinks his fangs into your neck. You feel a @dark@ @force@ pour into your neck and @course@ through your body. NL ... NL You wake up some time later, alone. An intense ~hunger~ passes through your belly. You #rmust #rfeed...`
3. `You step back and raise your weapon in defiance. The tall figure sighs. "Very well." The entire group of hooded figures morph into a thick black fog that flows away from you. NL You are alone once more.`
4. `The pale figures gasp as you take out the #yBlood #yVial. NL ~"The~ ~master's~ ~blood...~ ~the~ ~master's~ ~blood!~ @THE@ @MASTER'S@ @BLOOD!"@ NL They all chant fervently as the tall one bows before you. "Drink from His blood, and become one with #y~Him."~ NL The chant growing louder, you consume the contents of the vial. Your vision immediately ~warps~ and fades to darkness. NL NL You wake up some time later, alone. An intense ~hunger~ passes through your belly. You #rmust #rfeed.`
5. `Navigating an unlit street, you come across several hooded figures in the midst of some dark ritual. As you approach, they turn to you in eerie unison. The tallest among them bares fanged teeth and extends a long, pale hand towards you. NL ~"Join~ ~us~ ~broken~ ~one,~ ~and~ ~feel~ ~the~ ~warmth~ ~of~ ~the~ ~Spire."~`

