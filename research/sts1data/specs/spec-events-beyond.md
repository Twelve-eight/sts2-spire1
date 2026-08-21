# StS1 event spec - The Beyond (Act 3 / Hive)

9 events. Every string below is the OFFICIAL StS1 English text extracted from desktop-1.0.jar; use it verbatim.
`NL` in StS1 text is a line break marker - DELETE it, StS2 does not use it. Keep sentences, drop the marker.
StS1 color codes (`#r`, `#g`, `#b`, `#y`, `#p`) are StS1 markup - DELETE them too; StS2 marks keywords with `*word*`.

---

## Falling
- Class file: `mod/Spire1Code/Events/Falling.cs`, `public class Falling : Spire1Event`
- ID: `SPIRE1-FALLING`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Falling`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "slippery_bridge";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 6, 7, 8]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CardHelper.hasCardWithType`
  - `CardHelper.returnCardOfType`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.clearAllDialogs`
  - `FontHelper.colorString`
  - `AbstractCard.makeStatEquivalentCopy`
  - `CardGroup.removeCard`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Land] #rLose `
2. `[Locked] Requires: Skill Card`
3. `[Channel] #rLose `
4. `[Locked] Requires: Power Card`
5. `[Strike] #rLose `
6. `[Locked] Requires: Attack Card`
7. `[Leave]`
8. `[Land] `

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you head upwards hopping from one floating shape to another, you slip. NL NL You begin to fall.`
1. `While in free fall you consider your options: NL Land safely with your greatest techniques. NL Channel a Power to survive the fall. NL Strike at the wall to hang on to it.`
2. `You land with extreme grace before continuing on.`
3. `Harnessing and expending some of your raw power, you manage to land unhurt.`
4. `You are able to latch on to the wall, and manage to make a short hop onto another stable platform.`
5. `You seem to fall as slow as a feather, reaching the bottom without a scratch.`

---

## MindBloom
- Class file: `mod/Spire1Code/Events/MindBloom.cs`, `public class MindBloom : Spire1Event`
- ID: `SPIRE1-MIND_BLOOM`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Mind Bloom`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "aroma_of_chaos";`
- StS1 numeric constants found in the bytecode: [0, 0.1, 0.2, 0.3, 0.6, 0.8, 0.9, 1, 2, 3, 4, 13, 20, 25, 40, 50, 999]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `CardLibrary.getCopy`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `MusicMaster.playTempBgmInstantly`
  - `Random.randomLong`
  - `AbstractDungeon.getCurrRoom`
  - `MonsterHelper.getEncounter`
  - `AbstractRoom.addGoldToRewards`
  - `AbstractRoom.addRelicToRewards`
  - `AbstractCard.canUpgrade`
  - `AbstractCard.makeStatEquivalentCopy`
  - `AbstractCard.upgrade`
  - `AbstractPlayer.bottledCardUpgradeCheck`
  - `RelicLibrary.getRelic`
  - `AbstractRelic.makeCopy`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.gainGold`
  - `AbstractPlayer.heal`
  - `GenericEventDialog.clearRemainingOptions`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[I am War] #rFight #ra #rBoss #rfrom #rAct #r1. #gObtain #ga #gRare #gRelic.`
1. `[I am Rich] #gGain #g999 #gGold. #rCursed #r- #r2 #rNormality.`
2. `[I am Healthy] #gHeal #gto #gfull #gHP. #rCursed #r- #rDoubt.`
3. `[I am Awake] #gUpgrade #gall #gCards. #rYou #rcan #rno #rlonger #rheal.`
4. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `While walking and traversing through the chaos of the Spire, your thoughts suddenly begin to feel very... #p~real...~ NL NL Imaginings of #rmonsters and #yriches begin to manifest themselves into reality. NL The sensation is quickly fleeting. What do you do?`
1. `Can it really be this easy?`
2. `Everything makes sense now. NL The lack of memories, the ascent, the #yAncient #yOne. NL NL This is the way it always was. NL This is the way it always will be. NL All will be forgotten again soon...`

---

## MoaiHead
- Class file: `mod/Spire1Code/Events/MoaiHead.cs`, `public class MoaiHead : Spire1Event`
- ID: `SPIRE1-MOAI_HEAD`  (auto-derived from the class name; do NOT set it manually)
- Title text: `The Moai Head`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "sunken_statue";`
- StS1 numeric constants found in the bytecode: [0, 0.125, 0.18, 1, 2, 3, 4, 15, 333]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `AbstractPlayer.hasRelic`
  - `GenericEventDialog.updateBodyText`
  - `ScreenShake.shake`
  - `SoundMaster.play`
  - `AbstractPlayer.heal`
  - `GenericEventDialog.updateDialogOption`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractPlayer.loseRelic`
  - `AbstractPlayer.gainGold`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Jump Inside] #gHeal #gto #gfull #gHP. #rLose #r`
1. ` #rMax #rHP.`
2. `[Offer: Golden Idol] #gGain #g333 #gGold. #rLose #rGolden #rIdol.`
3. `[Locked] Requires: Golden Idol.`
4. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `You stumble across something that feels *very* out of place. Before you, an enormous stony head emerges from a large wall segment that does not shift and change like the rest of this area. NL The head's mouth is wide open, and it reveals large intimidating teeth stained red with blood. The surface of the statue is riddled with pictographs that seem to indicate people throwing themselves into the mouth of this head and being devoured. Why would anyone do that?`
1. `At first when you step up into the mouth of the statue, nothing happens. As you start to feel more than a little foolish, the huge molars slam down from above, crushing you whole. NL @Darkness.@ NL Sometime later from within the dark, you see a sliver of light, and hear what you now realize is the sound of stony teeth slowly rising upwards. NL NL NL You leave confused.`
2. `You jump back a little as the gigantic molars smash down on the idol, smashing it into dust. As the teeth start to rise up again, #ygold pours forth in a torrent from the opening, flooding you with riches.`
3. `You leave, wondering what could have been.`

---

## MysteriousSphere
- Class file: `mod/Spire1Code/Events/MysteriousSphere.cs`, `public class MysteriousSphere : Spire1Event`
- ID: `SPIRE1-MYSTERIOUS_SPHERE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Mysterious Sphere`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "crystal_sphere";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 45, 50, 55, 1120]
- StS1 APIs the event calls (tells you what it does):
  - `RoomEventDialog.addDialogOption`
  - `AbstractDungeon.getCurrRoom`
  - `MonsterHelper.getEncounter`
  - `AbstractEvent.update`
  - `RoomEventDialog.getSelectedOption`
  - `CurScreen.ordinal`
  - `RoomEventDialog.updateBodyText`
  - `RoomEventDialog.updateDialogOption`
  - `RoomEventDialog.clearRemainingOptions`
  - `Random.random`
  - `AbstractRoom.addGoldToRewards`
  - `AbstractDungeon.returnRandomScreenlessRelic`
  - `AbstractRoom.addRelicToRewards`
  - `ImageMaster.loadImage`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Open Sphere] #rFight. #gReward: #gRare #gRelic.`
1. `[Leave]`
2. `[Fight]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Jutting from the chaotic terrain around you, a bony sphere surrounds a mysterious glowing object within. NL While you are curious what lies inside, you notice some sentries keeping an eye on it.`
1. `As soon as you strike the sphere, the sentries spring to life around you!`
2. `No need to be greedy.`

---

## SecretPortal
- Class file: `mod/Spire1Code/Events/SecretPortal.cs`, `public class SecretPortal : Spire1Event`
- ID: `SPIRE1-SECRET_PORTAL`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Secret Portal`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "doors_of_light_and_dark";`
- StS1 numeric constants found in the bytecode: [-1, 0, 1, 2, 5, 15]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `ScreenShake.mildRumble`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractDungeon.getCurrRoom`
  - `MusicMaster.fadeOutTempBGM`
  - `AbstractDungeon.nextRoomTransitionStart`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Enter the Portal] IMMEDIATELY travel to the boss.`
1. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Before you is a sight that seems out of place in the alien landscape around you. Strangely placed into one of the living walls of the Beyond is an enclosed stone entrance filled with a #p~swirling~ #p~magical~ #p~portal.~ NL NL You aren't sure where it leads, but maybe it could speed your journey through the Spire.`
1. `Jumping through the portal, your sense of time and space is completely torn apart. NL NL As you reorient yourself to the new surroundings, you realize that right before you is a fearsome battle.`
2. `Careful and cautious seems the better approach for reaching the top of the Spire. Ignoring the portal you continue on.`

---

## SensoryStone
- Class file: `mod/Spire1Code/Events/SensoryStone.cs`, `public class SensoryStone : Spire1Event`
- ID: `SPIRE1-SENSORY_STONE`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Sensory Stone`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "sapphire_seed";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 5, 10]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `CurScreen.ordinal`
  - `GenericEventDialog.updateBodyText`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.damage`
  - `GenericEventDialog.clearRemainingOptions`
  - `Random.randomLong`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.addCardReward`
  - `CombatRewardScreen.open`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Recall] #gAdd #g1 #gColorless #gcard #gto #gyour #gdeck.`
1. `[Recall] #gAdd #g2 #gColorless #gcards #gto #gyour #gdeck. #rLose #r`
2. `[Recall] #gAdd #g3 #gColorless #gcards #gto #gyour #gdeck. #rLose #r`
3. ` #rHP.`
4. `[Leave]`
5. `[Interact]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `Navigating through the Beyond, you discover a #bglowing #btesseract @spinning@ and ~shifting~ gently in the air.`
1. `You touch it. NL NL A #r@sharp@ #r@pain@ flows through you, followed by vivid flashes of a distant memory. NL ...whose memories are these?`
2. `#r~FEAR.~ NL NL A demonic creature towers above you, wings spread wide as it howls with laughter. Dead bodies of a tribe surround you while the village is engulfed in terrible #p~dark~ #p~flames.~ NL NL The demon calls out, taunting you. NL NL " #r@YOU@ #r@REALLY@ #r@ARE@ #r@THE@ #r@STRONGEST@ #r@NOW!@ #r@Haha..@ #r@HEHE...@ #r@HAHAHAAAAH!!@ " NL NL This laughter echoes forever... `
3. `#g~TRIUMPH.~ NL NL The remains of a #p~ghostly~ #p~creature~ sink slowly into the mud before you, barely visible in the moonlight. You have proven yourself amongst your sisters. NL NL Standing victoriously, you wait in silence as the others ceremoniously place the #ycreature's #yskull atop your head. The ritual has concluded. NL NL You head towards the Spire...`
4. `#b~CONFUSION.~ NL NL #y[OBJECTIVE] #gBALANCE #gmust #gbe #gENFORCED NL #y[DEFINE] #gBALANCE NL #y[ERROR] #gBALANCE #gNOT #gFOUND NL #y[DEFINE] #gBALANCE NL #y[ERROR] #gBALANCE #gNOT #gFOUND NL #y[WARNING] #rLarge #robject #rapproaching NL NL ~"I...~ ~..am~ ~....Neow.."~`
5. `#p~SERENITY.~ NL NL Two primitive creatures fight over a carcass on the side of the road. You observe, devoid of emotion. NL #yWatch. #yRemember. #yLive. This is the Watcher's mission. NL NL Recently, one of your peers had stopped reporting on their assignment: a Spire of unknown origin. NL NL As the fight ends, you continue onward, unfazed by the bloody scene that took place.`

---

## SpireHeart
- Class file: `mod/Spire1Code/Events/SpireHeart.cs`, `public class SpireHeart : Spire1Event`
- ID: `SPIRE1-SPIRE_HEART`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Spire Heart`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "the_legends_were_true";`
- StS1 numeric constants found in the bytecode: [0, 0.05, 0.2, 0.8, 1, 1.5, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 40, 80, 1300]
- StS1 APIs the event calls (tells you what it does):
  - `AnimatedNpc.setTimeScale`
  - `AnimatedNpc.addListener`
  - `RoomEventDialog.clear`
  - `RoomEventDialog.addDialogOption`
  - `GameOverScreen.resetScoreChecks`
  - `GameOverScreen.calcScore`
  - `AbstractPlayer.getWinStreakKey`
  - `Settings.isStandardRun`
  - `AbstractPlayer.getLeaderboardWinStreakKey`
  - `Prefs.getInteger`
  - `Prefs.putInteger`
  - `DoorUnlockScreen.open`
  - `AbstractEvent.update`
  - `RoomEventDialog.getSelectedOption`
  - `MathHelper.slowColorLerpSnap`
  - `CUR_SCREEN.ordinal`
  - `AbstractPlayer.getSpireHeartText`
  - `RoomEventDialog.updateBodyText`
  - `RoomEventDialog.updateDialogOption`
  - `AbstractPlayer.getSlashAttackColor`
  - `AbstractPlayer.getSpireHeartSlashEffect`
  - `ScreenShake.shake`
  - `ScreenShake.rumble`
  - `RoomEventDialog.hide`
  - `AnimatedNpc.render`
  - `AbstractEvent.dispose`
  - `AnimatedNpc.dispose`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Continue]`
1. `[Attack] #b???`
2. `[Sleep]`
3. `[Approach Door]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `@tu-thump@ ... @tu-thump@ ... @tu-thump@ ... NL A deep pulsing dread can be felt throughout the room... NL Is this the #r~heart~ of the Spire? The source of this evil?`
1. `You deal #b`
2. ` damage! NL The heart #r@squirms@ and #r~bleeds~ ...but is ultimately still pounding. NL Are your mightiest attacks not enough?`
3. `You ask yourself, ~"Have~ ~I~ ~been~ ~here~ ~before?"~ NL You feel that you have dealt a total of #b`
4. ` damage to the heart.`
5. ` NL A total of #b`
6. ` damage has been dealt by all who have challenged it.`
7. ` NL The heart pulses louder and louder as your #y~consciousness~ #y~fades...~`
8. `NL You ready your blade...`
9. `NL You prepare your daggers...`
10. `NL You charge your core to its maximum...`
11. `You ask yourself, ~"Have~ ~I~ ~been~ ~here~ ~before?"~`
12. ` NL The heart pulses louder and louder as your #p~consciousness~ #p~begins~ #p~to~ #p~fade...~`
13. ` NL A sudden burst of #y@energy@ emanates from inside you, #b@jolting@ you awake.`
14. ` NL The heart #gretreats upwards! A large door is revealed in its place.`
15. `NL You prime your staff with divine energy...`

---

## TombRedMask
- Class file: `mod/Spire1Code/Events/TombRedMask.cs`, `public class TombRedMask : Spire1Event`
- ID: `SPIRE1-TOMB_RED_MASK`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Tomb of Lord Red Mask`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "mirror_mask3";`
- StS1 numeric constants found in the bytecode: [0, 1, 2, 3, 4, 222]
- StS1 APIs the event calls (tells you what it does):
  - `AbstractPlayer.hasRelic`
  - `GenericEventDialog.setDialogOption`
  - `CurScreen.ordinal`
  - `AbstractPlayer.gainGold`
  - `GenericEventDialog.updateBodyText`
  - `AbstractPlayer.loseGold`
  - `AbstractDungeon.getCurrRoom`
  - `AbstractRoom.spawnRelicAndObtain`
  - `GenericEventDialog.clearAllDialogs`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `[Don the Red Mask] #gGain #g222 #gGold.`
1. `[Locked] Requires: Red Mask.`
2. `[Offer: `
3. ` Gold] #rLose #rall #rGold. #gObtain #ga #gRelic.`
4. `[Leave]`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `A highly ornamented tomb can be seen on the other side of a floating path. Upon reaching the tomb, you notice a slot for #ygold coins with a scratched out inscription above it.`
1. `You don the mask and the tomb starts to ~flake~ ~away...~ a secret passage! NL NL The passage is lined with countless stolen goods and mounds of #ygold!`
2. `An opening appears in the tomb and out slides a small red mask with a note attached. "Take from others as I have taken from you!"`

---

## WindingHalls
- Class file: `mod/Spire1Code/Events/WindingHalls.cs`, `public class WindingHalls : Spire1Event`
- ID: `SPIRE1-WINDING_HALLS`  (auto-derived from the class name; do NOT set it manually)
- Title text: `Winding Halls`
- Act: `public override ActModel[] Acts => Act3;`
- Portrait: `protected override string ShippedPortrait => "jungle_maze_adventure";`
- StS1 numeric constants found in the bytecode: [0, 0.05, 0.125, 0.18, 0.2, 0.25, 1, 2, 3, 4, 5, 6, 7, 10, 15, 350]
- StS1 APIs the event calls (tells you what it does):
  - `GenericEventDialog.setDialogOption`
  - `SoundMaster.play`
  - `GenericEventDialog.updateBodyText`
  - `CardLibrary.getCopy`
  - `GenericEventDialog.updateDialogOption`
  - `AbstractPlayer.damage`
  - `GenericEventDialog.clearRemainingOptions`
  - `AbstractPlayer.heal`
  - `AbstractPlayer.decreaseMaxHealth`
  - `ScreenShake.shake`
  - `LocalizedStrings.getEventString`

### Official option strings (StS1 order)
0. `...`
1. `[Embrace Madness] #gReceive #g2 Madness. #rLose #r`
2. ` #rHP.`
3. `[Focus] #rBecome #rCursed #r- #rWrithe. #gHeal #g`
4. `[Leave]`
5. ` #gHP.`
6. `[Retrace Your Steps] #rLose #r`
7. ` #rMax #rHP.`

### Official description strings (StS1 order; index 0 is the INITIAL page)
0. `As you slowly make your way up the twisting pathways, you constantly find yourself losing your way as the walls and ground seem to inexplicably shift before your eyes. NL NL The constant #p~whispering~ #p~voices~ in the back of your head aren't helping things either.`
1. `Passing by a structure you are certain you have previously seen you start to question if you are going insane, or if the impossible geography of this place is starting to get to you. You need to change something, and soon. NL NL That's what the #p~voices~ say anyway, and why would they lie?`
2. `Something in you cracks. NL NL Only the truly mad can understand a place like this, so you give into the chattering voices and continue on with a #p~"new"~ perspective. NL Things do seem to make so much more sense now.`
3. `As you take a moment to stop and carefully observe the undulating landscape around you, the hint of a pattern starts to emerge from within the randomness. Whenever the demented noises begin to interrupt your thoughts, you struggle through the mental pain and ignore it. NL NL Eventually you successfully map out a path forward, and continue on, now resistant to the nefarious nature of this alien place.`
4. `You spend what seems like an eternity lost in the maze. Slowly but surely, you are able to retrace your steps, reorient yourself, and make it out of the twisting passages. NL NL You feel #r~drained~ from the experience.`

