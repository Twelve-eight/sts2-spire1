# Ascension Modifiers A1-A20 (Ascension) - StS1 Combat Semantics Knowledge Base

## Scope of This Volume
The complete run-level modifier system: every gate reads the single static `AbstractDungeon.ascensionLevel` (int, persisted in SaveFile as `is_ascension_mode` + `ascension_level`). Official per-level text from `localization/eng/ui.json` key `AscensionModeDescriptions`. **Legend**: **High** = directly provable from bytecode / **Medium** = inferred (noted). Sources: `AbstractDungeon` / `AbstractPlayer` / `AbstractRoom` / `AbstractMonster` / dungeon ctors / monster ctors / event ctors / `ShopScreen` / `ProceedButton` javap offsets. Reference jar: desktop-1.0.jar v2.x. Dumps: `sts2-spire1/.tmp/audit/javap-asc/` (27 files) + `.tmp/audit/javap-asc2/` (93 files, incl. all events/shrines/shop/map).

Gate-level law (verified across all dumps): **normals gate at >=2 / >=7 / >=17, elites at >=3 / >=8 / >=18, bosses at >=4 / >=9 / >=19** for damage / HP / moveset respectively. Known exception: Snecko (city normal) HP gate is >=7 - consistent - but its HP values follow the normal tier while its GLARE ability gate is >=17 (normal tier). The tier assignment is per-monster by role, not by act.

---

## 1. Map and Economy (AbstractDungeon / AbstractRoom)

**A01 Elite room count +60%** - Source `AbstractDungeon` map-gen method, dump lines 1539-1586 (offsets 256-373). Confidence: **High**
```
base:    eliteRooms = round(n * eliteRoomChance)          (n = pathable nodes)
A1+:     eliteRooms = round(n * eliteRoomChance * 1.6f)
```
The "Elite Swarm" custom-mod check sits nearby and swaps the multiplier for x2.5f. Official text: "Elites spawn more often."

**A05 Post-boss heal 75%** - Source `AbstractDungeon#dungeonTransitionSetup` offsets 195-233 (dump lines 7198-7219). Confidence: **High**
```
A5+:  heal(round((maxHP - currentHP) * 0.75f), false)
else: heal(maxHP, false)
```
Applies at every act transition, not just after bosses (the method is the act-transition setup; the official text "Heal less after Boss battles" describes the player-visible effect).

**A06 Start damaged** - Source `dungeonTransitionSetup` offsets 298-323. Confidence: **High**
Run start only (`floorNum <= 1` and current dungeon `instanceof Exordium`): `currentHealth = round(maxHealth * 0.9f)`.

**A10 Start cursed** - Source `dungeonTransitionSetup` offsets 326-355. Confidence: **High**
`masterDeck.addToTop(new AscendersBane())` + `UnlockTracker.markCardAsSeen("AscendersBane")`. Same run-start-only gate as A06.

**A13 Boss gold x0.75** - Source `AbstractRoom` (endBattle branch), offsets 633-647 (dump lines 616-627). Confidence: **High**
```
MonsterRoomBoss only (loading_post_combat == false, not loading a save):
base = 100 + miscRng.random(-5, 5)          // 95-105
A13+: addGoldToRewards(round(base * 0.75f))
```
Normal and elite combat rooms are NOT affected. Daily runs bypass the roll (flat 100). The "Cursed Run" mod check follows immediately (adds a random curse). Official text: "Poor bosses."

**A14 Lower Max HP** - Source `dungeonTransitionSetup` offsets 278-295 + the four character overrides. Confidence: **High**
```
A14+: player.decreaseMaxHealth(player.getAscensionMaxHPLoss())
Ironclad 5 / TheSilent 4 / Defect 4 / Watcher 4   (iconst directly proven in each character class)
```
Run-start-only gate (same floorNum<=1 && Exordium condition as A06/A10).

## 2. Player-Model Gates (AbstractPlayer / dungeon ctors)

**A11 Fewer potion slots** - Source `AbstractPlayer` constructor, offsets 382-397. Confidence: **High**
`potionSlots -= 1`. Level number 11 confirmed by the gate constant (an earlier session note labeling this "A18" was wrong).

**A12 Upgraded cards appear less often** - Source `TheCity`/`TheBeyond`/`TheEnding` constructors, gate at ctor offset 59. Confidence: **High**
```
TheCity / TheBeyond:  cardUpgradedChance 0.25 -> 0.125f
TheEnding:             cardUpgradedChance 0.5  -> 0.25f
```
Field is `cardUpgradedChance` (not colorlessRareChance). Exordium has no gate (0.12f base, unchanged).

## 3. Monster Stat Gates (per-monster constructors)

The pattern (verified in every dump read): each monster ctor contains up to three `ascensionLevel` gates taking the next tier's stat block. Values below are `base -> gated`.

**A02 Normal enemies are deadlier** (gate >=2) - Confidence: **High**
| Monster | Field | base -> A2 |
|---|---|---|
| JawWorm | chompDmg | 11 -> 12 |
| Cultist | ritualAmount | 3 -> 4 |
| Snecko | biteDmg / tailDmg | 15/8 -> 18/10 |
| Champ (base branch) | slashDmg/executeDmg/slapDmg | 16/10/12 -> 18/10/14 (shared with A4, see A04) |

**A03 Elites are deadlier** (gate >=3) - Confidence: **High**
| Monster | Field | base -> A3 |
|---|---|---|
| GremlinNob | bashDmg / rushDmg | 6/14 -> 8/16 |
| Lagavulin | attackDmg | 18 -> 20 |
| GiantHead | startingDeathDmg | 30 -> 40 |

**A04 Bosses are deadlier** (gate >=4) - Confidence: **High**
| Monster | Field | base -> A4 |
|---|---|---|
| Champ | slashDmg/executeDmg/slapDmg | 16/10/12 -> 18/10/14 |
| TimeEater | reverbDmg / headSlamDmg | 7/26 -> 8/32 |
| Donu / Deca | beamDmg | 10 -> 12 |
| AwakenedOne | (not ctor damage) | gains StrengthPower 2 at battle start - Source `AwakenedOne#usePreBattleAction` offsets 159-191 |

**A07 Normal enemies are tougher** (HP gate; JawWorm/Cultist >=7) - Confidence: **High**
| Monster | setHp | base -> A7 |
|---|---|---|
| JawWorm | (40,44) | -> (42,46) |
| Cultist | (48,54) | -> (50,56) |
| Snecko | (114,120) | -> (120,125) |

**A08 Elites are tougher** (gate >=8) - Confidence: **High**
| Monster | setHp | base -> A8 |
|---|---|---|
| GremlinNob | (82,86) | -> (85,90) |
| Lagavulin | (109,111) | -> (112,115) |
| GiantHead | (500,500) | -> (520,520) |

**A09 Bosses are tougher** (gate >=9, single-value setHp) - Confidence: **High**
| Monster | setHp | base -> A9 |
|---|---|---|
| AwakenedOne | 300 | -> 320 (stage-2 rebirth HP also 320, dump L664-668) |
| Champ | 420 | -> 440 |
| TimeEater | 456 | -> 480 |
| Donu / Deca | 250 | -> 265 |


## 4. Moveset and Ability Gates (takeTurn / usePreBattleAction / getMove)
**A17 Normal enemies have more challenging movesets** (gate >=17) - Confidence: **High** (each branch read directly)
| Monster | Gate site | Change |
|---|---|---|
| JawWorm | ctor offsets 70-109 | bellowStr 3 -> 5, bellowBlock 6 -> 9 |
| Snecko | takeTurn offsets 326-364 | GLARE adds WeakPower 2 after Vulnerable 2 |

**A18 Elites have more challenging movesets** (gate >=18) - Confidence: **High** (each branch read directly)
| Monster | Gate site | Change |
|---|---|---|
| GremlinNob | takeTurn offsets 58-94 | BELLOW enrage AngerPower 2 -> 3 |
| GremlinNob | getMove offsets 21-197 | move selection reorders: rush (SKULL_BASH, move 2) becomes the default chain instead of the 33%-roll branch |
| Lagavulin | ctor offsets 108-125 | debuff -1 -> -2 |

**A19 Bosses have more challenging movesets** (gate >=19) - Confidence: **High** (each branch read directly)
| Monster | Gate site | Change |
|---|---|---|
| AwakenedOne | usePreBattleAction offsets 27-133 | RegenerateMonsterPower 10 -> 15, CuriosityPower 1 -> 2 |
| Champ | ctor offsets 139-185 | full gated block: slash 18 / execute 10 / slap 14 / strAmt 4 / forgeAmt 7 / blockAmt 20 (supersedes the A9-tier 18/10/14/3/6/18 block) |
| TimeEater | takeTurn offsets 270-308 | Half (move 3) adds FrailPower 1 after WeakPower 1 |
| TimeEater | takeTurn offsets 400-429 | adds Slimed x2 to the player's discard |
| TimeEater | takeTurn offsets 511-535 | self-heal branch gains GainBlockAction(headSlamDmg) = 40 block |
| Donu / Deca | usePreBattleAction offsets 0-59 | ArtifactPower 2 -> 3 at battle start |

Tier note: the A17/A18/A19 gates double as the "abilities" tier of the per-monster gate law (>=17/18/19); see Sec 3 for the damage/HP tiers.

## 5. Event Unfavorable Variants (A15, gate >=15)
**A15 Unfavorable Events** - All gates are constructor-local or buttonEffect-local; there is no central event switch. Confidence: **High** (each value read from its event's bytecode)
| Event | base -> A15 |
|---|---|
| Cleric (purify) | purifyCost 50 -> 75 |
| CursedTome | finalDmg 10 -> 15 |
| DeadAdventurer | encounterChance 25 -> 35 |
| Designer | adjust 40/60/90 + hpLoss 3 -> 50/75/110 + hpLoss 5 |
| ForgottenAltar | hpLoss 25% -> 35% maxHP (round) |
| Ghosts | hpLoss unchanged (ceil 50% maxHP, capped at maxHP-1) but the option text switches to the harsher variant (OPTIONS[3] vs OPTIONS[0]) |
| GoldenIdol | damage 25% -> 35% maxHP; maxHpLoss 8% -> 10% (floor 1) |
| GoldShrine | goldAmt 100 -> 50 |
| GoopPuddle | goldLoss random(20,50) -> random(35,75) (walk-through damage 11 and pool gold 75 unchanged) |
| GremlinMatchGame | pair set RARE/UNCOMMON/COMMON/colorless-UNCOMMON/starter/1-curse -> RARE/UNCOMMON/COMMON/starter/2-curses |
| GremlinWheelGame | hpLossPercent 0.1f -> 0.15f |
| Lab | potion rewards 2 -> 1 |
| MoaiHead | hpAmt round(12.5%) -> round(18%) maxHP |
| Nest | goldGain 99 -> 50 |
| ScrapOoze | per-hit dmg 3 -> 5 (relicObtainChance 25 unchanged) |
| ShiningLight | damage round(20%) -> round(30%) maxHP |
| Sssserpent | goldReward 175 -> 150 (Doubt curse unchanged) |
| TheLibrary | healAmt round(33%) -> round(20%) maxHP |
| TheMausoleum | curse chance 50% -> 100% (the second gate forces the `miscRng.randomBoolean()` outcome to true) |
| WindingHalls | hpAmt round(12.5%) -> round(18%); healAmt round(25%) -> round(20%); maxHPAmt 5% unchanged |
| WomanInBlue | an extra "pay HP" potion-buy variant appears (ceil 5% maxHP); buying 0 potions now deals ceil(5% maxHP) HP_LOSS damage |
| NoteForYourself | event removed entirely from specialOneTimeEventList - Source `AbstractDungeon#isNoteForYourselfAvailable` offsets 19-39, log string "Note For Yourself is disabled beyond Ascension 15+" |

## 6. Shop Price Gate (A16)

**A16 Shops are more costly** - Source `ShopScreen` constructor, offsets 547-562. Confidence: **High**
```
A16+: applyDiscount(1.1f, false)   // all wares +10% (cards/relics/potions)
```
Then the relic discounts stack multiplicatively afterwards: Courier x0.8, Membership Card x0.5 (both `applyDiscount(f, true)`), Smiling Mask sets `actualPurgeCost = 50`. Purge base cost 75 (`resetPurgeCost`), +10% at A16 -> 82 (rounded by the price setter). See loot-rewards.md L04-L06 for the base pricing formula `getPrice(rarity) x U(0.9,1.1)`.

## 7. Double Boss (A20)

**A20 (Max Level) Double Boss** - TheBeyond only. Confidence: **High** (three cooperating sites read directly)
```
1. initializeBoss (each dungeon ctor): fills bossList with the 3 act bosses, shuffled by
   monsterRng; if a new player has seen <1 boss, the seen-boss fast path can leave size 1,
   then the size==1 guard duplicates the entry (Exordium offsets 167-191) -> size 2.
   At A20 the veteran path keeps 3 entries.
2. AbstractDungeon ctor offset 126: setBoss(bossList.get(0)) -> bossKey = first boss
   (peek, no removal). MonsterRoomBoss#onPlayerEntry offsets 60-67: bossList.remove(0)
   (consume on entry). So entering fight 1 leaves 2 entries.
3. ProceedButton (post-combat-reward click), offsets 197-219:
   if (room instanceof MonsterRoomBoss && dungeon.id == "TheBeyond"
       && ascensionLevel >= 20 && bossList.size() == 2)
       -> goToDoubleBoss(): bossKey = bossList.get(0); new MonsterRoomBoss as nextRoom.
   (The normal path would call goToVictoryRoomOrTheDoor.)
4. Fight 2: onPlayerEntry remove(0) -> size 1 -> that boss's die() ->
   onFinalBossVictoryLogic sees size != 2 -> normal victory logic runs.
```
Skip-gate: `AbstractMonster#onFinalBossVictoryLogic` offsets 0-21: `ascensionLevel >= 20 && bossList.size() == 2 -> return` (skips the final-act availability check / achievements / stopClock). This method is called ONLY from the four act-3 bosses' `die()` overrides (AwakenedOne offset 172, TimeEater offset 42, Donu offset 42, Deca offset 42). Exordium/TheCity ProceedButton paths have no A20 branch (double boss is a third-act-only mechanic).

## 8. Persistence and Unlock

- SaveFile persists `is_ascension_mode` (boolean) + `ascension_level` (int); the save-path dungeon constructor restores `bossList` from `SaveFile.boss_list` and re-runs `setBoss(save.boss)` (AbstractDungeon offsets 43-54 of the save ctor). Confidence: **High**
- `ascensionCheck = UnlockTracker.isAscensionUnlocked(player)` (save ctor offset 8) gates the mode itself. Confidence: **High**

## 9. Arbitration Case Table

| Scenario | Outcome | Basis |
|---|---|---|
| Porting monster stats | Implement all three tiers per monster (>=2/3/4 damage, >=7/8/9 HP, >=17/18/19 abilities); Snecko keeps normal-tier gates despite being city content | A02-A09, Sec 3-4 |
| A20 in our StS2 port | Not portable as-is: StS2 has no ProceedButton/bossList queue; model as "act-3 boss fight repeats once, victory logic only after the second" | A20, Sec 7 |
| Event port fidelity | Each event needs its own A15 branch; there is no shared "unfavorable" helper - 22 events carry individual deltas | A15, Sec 5 |
| Shop port | A16 is a single +10% multiplier at screen construction, stacking with relic discounts | A16, Sec 6 |
| Ascension + custom mods | "Elite Swarm" x2.5f and "Cursed Run" interleave with A1/A13 code paths; both checks sit adjacent in the same methods | A01, A13 |

## 10. Open Questions / Low-Confidence Items

1. The remaining ~50 monster classes not dumped individually (only 11 + Donu/Deca/TimeEater read in full). The tier law (>=2/3/4, >=7/8/9, >=17/18/19) is expected to hold for all; spot-check any monster before porting its A-branches. Confidence: **Medium** (law) / **High** (all monsters actually read).
2. Champ's A2-vs-A4 overlap: the base branch (16/10/12) jumps to 18/10/14 at >=4; there is no separate >=2 branch for Champ (bosses use the >=4 tier). Listed under both A02 (for completeness) and A04 (authoritative).
3. AwakenedOne stage-2 secondary stats (SLASH/SOUL_STRIKE/ECHO/SLUDGE/TACKLE damage fields) were not individually gated in the ranges read; if any carry their own >=4/19 branches inside takeTurn, they were not extracted. Spot-check before porting its A-tier abilities.
4. WomanInBlue potion prices ( OPTIONS-driven ) were not value-extracted; only the A15 HP-variant behavior was.
