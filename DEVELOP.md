# DEVELOP.md - sts2-spire1 (Slay the Spire 1 <-> Slay the Spire 2 interop)

> Vision (**PIVOTED** session 14, 2026-08-23 - supersedes the self-contained-sandbox vision): Spire1 is the **complementary character/card/relic layer** for vanilla StS1 content on StS2 (v0.111.x public-beta, Godot/C#/.NET9) via **BaseLib**. Bring vanilla StS1 **characters, cards, relics, powers, potions, events** into StS2, designed to run **inside the community act stack** - Acts from the Past (acts 1-3), Act 4 Heart (The Ending), Act Toggler, MP Rebalance - instead of maintaining our own dungeon presentation. Our own CustomActModel dungeon (former M2/M3) stays in-tree as **fallback only**, not a polish target. Any mix of StS1/StS2 characters, solo or co-op; all mod content runtime-toggleable.
>
> Authoritative design + contracts. Chronological log -> `DEVLOG.md`. Shared conventions -> `../AGENTS.md`. Deep API -> research artifacts (Sec 10).

## 0. Status (conclusion-first)
- **DIRECTION PIVOT (session 14, user decision): complementary layer.** The ecosystem covers the dungeon stack better than we can present it: AFTP (acts 1-3, real StS1 art/music/animation, 479 ratings), Act 4 Heart (The Ending, three-key gate, MP-compatible), Darkglade's Act Toggler (main+beta), Kziz3988's MP Rebalance. All four subscribed and downloaded locally. We stop investing in our acts' presentation; effort concentrates on the class/card/relic layer + interop correctness. Analysis: `DEVLOG.md` Sec 13/Sec 13.1.
- **Unlocked by AFTP decompilation (session 13)**: `N'loth's Gift` viable via Harmony on `CardRarityOdds.RollWithoutChangingFutureOdds`; `FaceTrader` unblocked (implement the five face relics); `Madness` has a reference impl. See Sec 9.
- Target: **StS2 v0.111.0** at `G:\steam\steamapps\common\Slay the Spire 2`. Framework **BaseLib**, and BaseLib is the mod's **only** dependency (NuGet `Alchyr.Sts2.BaseLib`; build-time-only helpers `Alchyr.Sts2.ModAnalyzers`, `Krafs.Publicizer`, `BSchneppe.StS2.PckPacker`; templates `Alchyr.Sts2.Templates`).
- **We ship ecosystem-compat patches (session 15, user directive - AFTP is frozen):** anything our verification/interop infra needs from third-party mods goes INTO Spire1's release dll, not upstream. Current carrier: `AutoSlayModdedScreenHandlersPatch` (teaches engine AutoSlay to drive AFTP minigame overlays; gated on `--autoslay` so normal play is untouched). Upstream asks we still filed: AFTP issue #10 (API-stability + ProceedButton gating); MegaCrit draft at `.tmp/issues/megacrit-autoslay-extensibility.md`; SpeedX contact is a USER action item.
- **Toolchain PROVEN**: .NET 9 SDK 9.0.317 + Godot.NET.Sdk/4.5.1 + BaseLib restore + `dotnet build` -> copies dll+json into `mods/<Mod>/`. Caches on G (C: <1GB free). Scaffolded `mod/` (`dotnet new alchyrsts2charmod --name Spire1`), id/prefix `Spire1`/`SPIRE1-`. Build green except STS001 (needs complete localization).
- Now: **M4 content is essentially complete. M2 monsters is the next milestone and is NO LONGER BLOCKED** (session 5 recon - see `DEVLOG.md` Sec 5.1). All four characters, 222 card classes (220 concrete + 2 abstract bases), 25 relic classes (24 concrete + 1 abstract base), 62 power classes (60 concrete + 2 abstract bases) and 7 event classes (6 concrete + 1 abstract base) build 0 errors and are deployed; every event branch that was blocked on a missing relic or card is wired. The event figure is the post-`3b0af84` (v1.1.0) set: that commit removed the 46 events AFTP already provides and kept the 6 unique to this mod, and only 5 of those 6 are reachable - `SpireHeart` is deliberately not auto-added and no trigger point is wired anywhere in this repository (see `mod/Spire1Code/Events/SpireHeart.cs`). (Counts corrected 2026-08-30 per critic #13 and freeze-review - then 306 cards, 25 relics after the 8 official-equivalent deletions of 2e405f8 - and re-measured from the tree 2026-09-22; the 2026-08-30 figures predate the AFTP de-duplication of `3b0af84`.)
- **Correction to earlier sessions: custom monsters were never blocked on a visuals decision.** BaseLib ships `CustomMonsterModel`/`CustomEncounterModel`/`CustomActModel`/`CustomOrbModel`/`CustomPetModel` and a real non-placeholder `CustomCharacterModel`, all confirmed present in the **shipped v3.3.5 binary**. Two cheap visual routes: point `CustomMonsterModel.CustomVisualPath` at one of the **121 shipped StS2 monster scenes** (the same trick `PlaceholderCharacterModel.cs:12` already uses for our characters, since `SceneHelper.GetScenePath` resolves against the base game's `res://`), or build an `NCreatureVisuals` from a single `Texture2D` via `NCreatureVisualsFactory`. So `Colosseum`, `MaskedBandits`, `MysteriousSphere`, `Mushrooms [Stomp]`, `DeadAdventurer`'s elite, `MindBloom [I am War]` and `SpireHeart` are all implementable now, with encounter tables already extracted in Sec 7d.
- Remaining true gaps: the five StS1 face relics plus `Madness` (**data now fully extracted and validated** in `research/sts1data/face-relics-and-madness.json`, zero blockers, five bytecode-verified corrections recorded in `DEVLOG.md` Sec 5.2); `NlothsGift` (**may be implementable after all** - `CardRarityOdds.Roll` is a public patchable instance method, verdict in `research/BaseLib-unused-surface.md`, `DEVLOG.md` Sec 5.4); and `Girya`'s rest-site option, which BaseLib itself flags incomplete.
- **Relic art needs no second library.** `RelicModel.IconBaseName`, `PackedIconPath`, `PackedIconOutlinePath` and `BigIconPath` are all `protected virtual`/`public virtual` (`.tmp/dllsrc/.../RelicModel.cs:128-140`), so a relic can borrow a shipped StS2 relic's atlas entry by overriding `IconBaseName`, or point at art in our own `Spire1.pck`. This is the same donor trick the characters and monsters use. The earlier note that this needed RitsuLib's `ExternalAssetOverrideRegistry` is withdrawn.
- **Single dependency, deliberately.** `mods/BaseLib/BaseLib.json` declares `dependencies: []`, and our `Spire1.json` declares exactly `[{"id": "BaseLib", "min_version": "3.4.5"}]`. RitsuLib and JmcModLib were both surveyed in full (internal reference docs, not distributed with this repo) and **rejected** - a second runtime dependency costs every player an extra install for benefits we can reach through BaseLib alone. Earlier text in this file claiming "runtime deps BaseLib+RitsuLib" was wrong.
- **Version skew RESOLVED (session 6).** We compile against NuGet `Alchyr.Sts2.BaseLib` **3.4.5** and the game now loads **3.4.5** as well; the three runtime files were verified byte-identical (md5) to the NuGet package's `Content/`+`lib/net9.0/` payload, and 3.3.5 is retained at `mods/BaseLib-3.3.5-backup/`. Before this, compile-time 3.4.5 against runtime 3.3.5 meant any source-only API compiled cleanly and would have thrown at load. The whole 3.4.5 surface is now legal; `docs/BaseLib-API.md` Sec 9's skew table is history, not a constraint.

## 1. Milestones
| M | Content | Verify |
|---|---|---|
| M0 pipeline (DONE) | toolchain + scaffold + build -> dll in mods | build exit 0 |
| **M1 Ironclad slice** | `Spire1Ironclad` "StS1 - Ironclad" (80 HP, 3 energy, deck 5 Strike/4 Defend/1 Bash, Burning Blood) + loc + red pool + global content toggle; then full Ironclad card pool + relics | in select; run starts; cards playable |
| M2 monsters (**DONE - fallback**) | StS1 monsters+encounters for ALL acts incl. The Ending landed (session 12); kept compiling in-tree, NOT a presentation target | encounters spawn; 0 load errors |
| ~~M3 own-dungeon selector~~ (**SUPERSEDED**) | replaced by the ecosystem act stack; own acts/dungeon-selector code retained as fallback only | n/a |
| M4 (**DONE**) | 4 characters, 222 card classes (220 concrete + 2 abstract bases), 25 relic classes (24 concrete + 1 abstract base), 62 power classes (60 concrete + 2 abstract bases), 3 potion classes (2 concrete + 1 abstract base), 7 event classes (6 concrete, 5 reachable) - deployed (counts corrected 2026-08-30, re-measured 2026-09-22) | build 0 errors; in-game smoke |
| **P1 interop verification** | dual-install smoke: Spire1 + AFTP + Act 4 Heart + Toggler (+/-MP Rebalance); shared-shrine cross-pollution; Harmony patch-collision audit; AutoSlay run in mixed stack | `--autoslay` exit 0 with stack enabled; no duplicated/mispooled events |
| **P2 gap closure** | N'loth's Gift (`RollWithoutChangingFutureOdds` prefix); FaceTrader + five face relics (`EventRelicPool`, uniform roll over unowned); Madness | each verified in-game |
| **P3 layer UX** | character-select visibility/gating polish vs ecosystem stack; decide if a light dungeon-picker UX atop ecosystem acts is worth building | smoke |

**VANILLA ONLY (hard):** unmodded StS1 as shipped by MegaCrit. Exclude all StS1 mods. Source = `desktop-1.0.jar` + fandom/wiki.gg vanilla; exact numbers in `agent://Sts1DataScout` (confirmed 100% vanilla: 75 red cards, 38 colorless, 5 status, 14 curses, temp/option cards; Act-1 monsters/encounters). Never invent unconfirmed values.

## 2. Core feature design
### 2a. Characters (additive, labeled)
- One class per StS1 character (`Spire1Ironclad`, later `Spire1Silent/Defect/Watcher`), NEW characters shown beside StS2's roster. Do NOT replace/hide StS2 characters. Display name **"StS1 - <Character>"** (eng) / **"一代-<角色>"** (zhs), e.g. `StS1 - Ironclad` / `一代-铁甲战士`.
- Characters are decoupled from dungeon: any StS1/StS2 character can enter any dungeon (M3).

### 2b. Dungeon/act-set selection at character select (M3) - **SUPERSEDED by ecosystem (fallback only)**
- Primary path: players choose StS1 acts via the ecosystem stack (AFTP pools / Act Toggler config); Spire1 contributes characters/cards/relics usable in ANY dungeon. Own-act selection below is kept for fallback activation only.
- Implementation: StS1 acts as `CustomActModel`s; a char-select control (BaseLib `CustomCharacterSelectEntry` and/or Harmony patch on the select/run-setup screen) selects the act sequence; `RunState`/act-progression patched to run 4 acts when StS1 dungeon chosen. [research act-sequence + co-op run setup via sts2.xml/BaseLib before M3]
- Co-op ("组队"): StS2 supports multiplayer; characters (mixed StS1/StS2) join a run into the selected dungeon. [verify multiplayer run-setup hooks]

### 2c. Runtime content gating (settings)
`Spire1Config : SimpleModConfig` (Settings->Mod Settings), all default ON:
| Toggle | Gates | Mechanism |
|---|---|---|
| `EnableSts1Content` (master) | all | short-circuit |
| `EnableSts1Characters` | StS1 chars in select | character visibility |
| `EnableSts1Cards` | StS1 colorless cards in shared pool | shared-card-pool filter |
| `EnableSts1Relics` | StS1 relics in shared pools | shared-relic/potion-pool filter |
| `EnableSts1Dungeon` | StS1 dungeon option + StS1 encounters | act-select option + `IsValidForAct` |
- Read at run/act/pool generation (StS2 granularity) -> applies next run/act, not mid-combat. Lets a run use none of the mod's content while installed.

## 3. Architecture (BaseLib content model)
- **Registration automatic** (ctors + `[Pool]`); `Initialize()` only: Harmony patch-all, `ModConfigRegistry.Register(ModId, new Spire1Config())`, extra loc tables, character sort order. IDs = `SPIRE1-<CLASS>`.
- **Cards**: `Spire1Card : CustomCardModel(cost, CardType, CardRarity, TargetType)` (`[Pool(Spire1CardPool)]`); override `CanonicalVars` (DamageVar/BlockVar/PowerVar<T>/calculated), `CanonicalKeywords`, `CanonicalTags`, `async OnPlay` (`CommonActions.CardAttack/CardBlock/Apply<T>/Draw`), `OnUpgrade`.
- **Character**: `Spire1Ironclad : PlaceholderCharacterModel` (`PlaceholderID="ironclad"`). Override `StartingHp`, `StartingDeck`, `StartingRelics`, `CardPool/RelicPool/PotionPool`, `NameColor`. Energy default 3.
- **Relics**: `Spire1Relic : CustomRelicModel` (`[Pool(Spire1RelicPool)]`).
- **Powers**: `CustomPowerModel`, `Type` (Buff/Debuff), `StackType`. No pool. (StS2 power class names via sts2.xml.)
- **Monsters**: `CustomMonsterModel` + `GenerateMoveStateMachine()` via `MoveBuilder`/`MonsterActions`.
- **Encounters**: `CustomEncounterModel(RoomType)`, `IsValidForAct(act)` gate (config-aware), `GenerateMonsters()`, `AllPossibleMonsters`; `IsWeak` for early pool.
- **Acts**: `CustomActModel(actNumber)` - StS1 acts (M3).
- **Localization**: JSON `Spire1/localization/{eng,zhs}/<table>.json`, keys `SPIRE1-<NAME>.<entry>`. Fmt `!D!`/`!B!`/`*gold*`. Char names carry the "StS1 -"/"一代-" prefix.
- **Card color**: `Spire1CardPool.H/S/V` over `card_frame_red`.

## 4. Repo layout
```
G:/omp works/Sts/sts2-spire1/
+- DEVELOP.md  DEVLOG.md  NuGet.config
+- mod/    Spire1.csproj  Spire1.json  Directory.Build.props  Sts2PathDiscovery.props  project.godot  export_presets.cfg
|  +- Spire1Code/ (Character/ Cards/ Relics/ Powers/ Potions/ Monsters/ Encounters/ Acts/ Config/ Extensions/ MainFile.cs)
|  +- Spire1/     (images/  localization/{eng,zhs}/*.json)
+- research/  (BaseLib-StS2, ModTemplate-StS2)  - reference only
+- .nuget/ .tools/ .dotnethome/ .tmp/  (G-local caches, gitignored)
```

## 5. Contracts (STABLE - all workers)
- **External communications MUST disclose agent authorship** (user directive, mirroring AFTP's own AI-disclosure standard): every upstream issue/comment/draft states that research and writing were done by the autonomous agent (`ox-alpha`) under Twelve-eight's direction, with evidence sources named (decompiled shipped binaries, live autoslay logs, local repro runs).
- Namespaces: root `Spire1`; code under `Spire1.Spire1Code.{Cards,Relics,Powers,Potions,Character,Monsters,Encounters,Acts,Config}`. Class name = identity (drives ID + loc key), StS1-descriptive PascalCase.
- Every card extends `Spire1Card`; every relic `Spire1Relic`; pools exist; `[Pool]` inherited; no manual registration.
- Every content class MUST add its localization entry to matching `localization/eng/*.json` (STS001 fails build). Card: `SPIRE1-<CLASS>` -> `{ "title": "...", "description": "Deal !D! damage." }`.
- Effects use commands only (`CommonActions`/`*Cmd`); never mutate state directly.
- VANILLA StS1 numbers only (from `agent://Sts1DataScout`); flag unconfirmed; never invent.
- No writes to C:. No non-{zh,en,fr,de,ru} text anywhere (use eng + optional zhs).

## 6. Parallel execution (workers + reviewers)
After M1 proves the pipeline, fan out independent slices. **Each code-writing subagent is paired with a `reviewer` subagent** (user directive) reviewing its code before I integrate/build. Workers WRITE code only, SKIP build/lint (main builds centrally to avoid mid-flight breakage). **Cost**: prefer the cheapest suitable agent type - `sonic` for strictly mechanical slices (localization JSON, stat-only cards), `task` for logic-heavy cards/monsters; the underlying model isn't hand-selectable via tooling, so agent-type choice is the cost lever. Slices: Ironclad cards by rarity (Basic+Common / Uncommon / Rare / colorless); relics by tier; Act-1 monsters+encounters; acts+dungeon selector (M3). Shared powers (Vulnerable/Weak/Strength wrappers) + base classes defined ONCE by main pre-fan-out. Contract = Sec 2/Sec 5 + `agent://BaseLibApiScout` + `agent://Sts1DataScout`.

## 7. Findings / decisions
- StS2 already contains `StrikeIronclad`/`DefendIronclad`/`BurningBlood` (build resolved, no CS0246) - reuse `ModelDb.Relic<BurningBlood>()` for starter relic; implement vanilla-faithful custom cards for full control.
- `PlaceholderCharacterModel(PlaceholderID="ironclad")` -> working visuals, no art for M1.
- Content gating + dungeon choice: monster set follows the selected dungeon; global toggles in settings for full enable/disable.

### 7a. LEAN-CODE RULE (user directive, overrides earlier "implement everything ourselves")
- If a card's mechanic AND numbers are identical between StS1 and the shipped StS2 card of the same name, DO NOT define our own class. Make the character able to obtain the SHIPPED card instead.
- Only write our own class when a field really differs (cost, base value, upgrade delta, keyword, target, rarity or behaviour). State the differing field in the class doc comment.
- Same rule for events: port an StS1 "?" event only if StS2 lacks it; a same-name-but-different event keeps the `StS1 - ` label.
- Measured baseline (jar bytecode vs decompiled StS2, `research/sts1data/`): of 97 same-name cards, 76 match numerically. Traps that match numerically but differ mechanically and therefore still need our own class: `Expertise` (StS1 draws up to 6 in hand, StS2 var is 2), `Claw` (StS1 +2 on upgrade, StS2 +1), `BiasedCognition` (StS1 4 Focus, StS2 5), `CalculatedGamble` (StS2 adds Retain), `Equilibrium`/`MachineLearning` (different keywords), `Chill`/`Darkness`/`Defragment`/`Fusion`/`GeneticAlgorithm`/`Glacier`/`HelloWorld` (verified differing by the Defect writers).

### 7b. Watcher subsystems are FEASIBLE (2026-08-19 probes, supersedes the earlier "stances absent" flag)
- Stances: `AbstractModel.ModifyDamageMultiplicative` exists and is dispatched for every hook listener (`Hook.ModifyDamageInternal`). Shipped proof: `DoubleDamagePower` returns 2m on the dealing side, `ColossusPower` returns 0.5m on the receiving side. So Wrath (deal x2 / receive x2), Divinity (deal x3) are implementable as `CustomPowerModel`s.
- Calm exit bonus: `PowerModel.AfterRemoved(Creature oldOwner)` + `PlayerCmd.GainEnergy` -> 2 Energy on leaving Calm. Divinity self-expiry: `AfterSideTurnStart`/`AfterPlayerTurnStart` + `PowerCmd.Remove(this)`.
- Stance-change observers (Mental Fortress, Rushdown, Flurry of Blows): `Creature.PowerApplied` / `Creature.PowerRemoved` events, or the power's own `AfterApplied`/`AfterRemoved`.
- Scry: **BaseLib already ships it** - `BaseLib.Commands.ScryCmd.Execute(PlayerChoiceContext, Player, int)`, `ScryVar` for the displayed number, and `IModifyScryAmount` / `IAfterScryed` hooks (Nirvana, Weave). Do not write a custom scry.
- Retain triggers (Perseverance, Sands of Time, Windmill Strike, Establishment): retention is decided in `CombatManager.FlushPlayerHand` via `CardModel.ShouldRetainThisTurn`, and `Hook.AfterFlush` delivers the retained-card list to every card and power listener.
- Mantra: no engine support; use a `CustomPowerModel` with `PowerStackType.Counter` plus a threshold check (`AfterPowerAmountChanged`) to enter Divinity at 10 and subtract 10.
- Meditate's "end your turn": `PlayerCmd.EndTurn(Player, bool, Func<Task>?)`.
- Token cards: StS2 ships 10 `CardRarity.Token` cards (Fuel, GiantRock, Luminesce, MinionDiveBomb, MinionSacrifice, MinionStrike, Shiv, Soul, SovereignBlade, SweepingGaze). `Shiv` matches StS1 exactly (reuse it). `Miracle` and `Insight` have no equivalent, so the mod must define those two itself.

### 7c. StS1 event + encounter ACT/FLOOR gating (authoritative, from `desktop-1.0.jar` bytecode)
Data files: `research/sts1data/events.json` (52 events: id, per-option official text, numeric constants, called APIs, gating evidence) and the per-act lists below. Never guess an event's act.
- Act membership comes from the dungeon classes' `initializeEventList` / `initializeShrineList`:
  - **Exordium (Act 1) events**: Big Fish, The Cleric, Dead Adventurer, Golden Idol, Golden Wing, World of Goop, Liars Game, Living Wall, Mushrooms, Scrap Ooze, Shining Light.
  - **The City (Act 2) events**: Addict, Back to Basics, Beggar, Colosseum, Cursed Tome, Drug Dealer, Forgotten Altar, Ghosts, Masked Bandits, Nest, The Library, The Mausoleum, Vampires.
  - **The Beyond (Act 3) events**: Falling, MindBloom, The Moai Head, Mysterious Sphere, SensoryStone, Tomb of Lord Red Mask, Winding Halls.
  - **Shrines available in every act**: Match and Keep!, Golden Shrine, Transmorgrifier, Purifier, Upgrade Shrine, Wheel of Change.
  - **Special/conditional (from `EventHelper.getEvent`)**: Accursed Blacksmith, Bonfire Elementals, Fountain of Cleansing, Designer, Duplicator, Lab, FaceTrader, NoteForYourself, WeMeetAgain, The Woman in Blue, N'loth, Knowing Skull, The Joust, The Mausoleum - these are gated by run conditions, not by a plain act list.
  - Only two events read floor/act directly: `MindBloom` (`floorNum` vs 50) and every A15+ variant (`ascensionLevel` vs 15). Everything else is gated purely by pool membership.
- StS2 equivalent gating: override `IsAllowed(IRunState)` on the event and read `runState.CurrentActIndex` / `runState.TotalFloor` (shipped precedents: `BrainLeech.cs:37-40` uses `< 2`, `DollRoom.cs:79-82` uses `== 1`, `PunchOff.cs:42-45` uses `TotalFloor >= 6`). Act-scoped custom events set `CustomEventModel.Acts`; leaving `Acts` empty registers a SHARED event (that is the correct home for the six shrines).
- Uniqueness is automatic: `RoomSet.EnsureNextEventIsValid` skips events whose `ModelId` is already in `runState.VisitedEventIds`, so no one-time flag is needed.
- StS2 ships NO event with an StS1 name (its events are the Overgrowth/Hive/Glory/Underdocks sets), so every StS1 event is a genuine addition; none should be skipped as a duplicate.

### 7d. StS1 Act encounter tables (authoritative, for M2 monsters)
- **Act 1 weak**: Cultist, Jaw Worm, 2 Louse, Small Slimes. **Act 1 strong**: Blue Slaver, Gremlin Gang, Looter, Large Slime, Lots of Slimes, Exordium Thugs, Exordium Wildlife, Red Slaver, 3 Louse, 2 Fungi Beasts. **Act 1 elites**: Gremlin Nob, Lagavulin, 3 Sentries. **Act 1 bosses**: The Guardian, Hexaghost, Slime Boss.
- **Act 2 weak**: Spheric Guardian, Chosen, Shell Parasite, 3 Byrds, 2 Thieves. **Act 2 strong**: Chosen and Byrds, Sentry and Sphere, Snake Plant, Snecko, Centurion and Healer, Cultist and Chosen, 3 Cultists, Shelled Parasite and Fungi. **Act 2 elites**: Gremlin Leader, Slavers, Book of Stabbing. **Act 2 bosses**: Automaton, Collector, Champ.
- **Act 3 weak**: 3 Darklings, Orb Walker, 3 Shapes. **Act 3 strong**: Spire Growth, Transient, 4 Shapes, Maw, Sphere and 2 Shapes, Jaw Worm Horde, 3 Darklings, Writhing Mass. **Act 3 elites**: Giant Head, Nemesis, Reptomancer. **Act 3 bosses**: Awakened One, Time Eater, Donu and Deca.
- Weak encounters are the first floors of an act, then strong ones; each act also has an exclusion list preventing an immediate repeat (captured in the extraction output). Encounter gating in StS2 is `CustomEncounterModel.IsValidForAct(act)` plus `IsWeak` for the early pool.

### 7e. Act-pool semantics with ActToggler2 + our fallback acts (session 15/16, verified against configs)
- **Selection chain**: with `Spire1.cfg UseSts1Dungeon=False` (default since the ecosystem pivot), our M2/M3 dungeon code stays dormant and the act pool is owned by ActToggler2. Toggler2's `GetWeightedAct` picks per slot from the configured `"Type:weight"` entries; an **empty/unset slot = uniform random over every registered act of that slot** - it does NOT distinguish source mods, so a blank config would also admit our fallback acts.
- **Our fallback acts never enter the pool**: they carry `ActNumber/Index = -2` and register only when `UseSts1Dungeon=True`; Toggler2's weighted pick ignores them under the current pinned config.
- **Current test environment pins all three slots to AFTP**: `ActToggler2.cfg = {"EnabledAct1":"ExordiumAct:1","EnabledAct2":"TheCityAct:1","EnabledAct3":"TheBeyondAct:1"}` (verified 2026-08-23). Deterministic AFTP three-act run, ~48 floors.
- **Floor budget**: engine AutoSlayer caps at `TotalFloor < 49` (vanilla-calibrated: BaseNumberOfRooms 15/14/13 ~ 48); our transpiler raises it to 120 so a fourth act fits. Any act set pushing past 120 would need the cap raised again.
- **Implication for release**: shipping Spire1 without pinning Toggler2 means users with Toggler2 installed get our fallback acts randomly mixed into act slots alongside AFTP/vanilla sets. That may be desirable (variety) or surprising; if we want deterministic behavior we either document a recommended Toggler2 config or ship a config preset.

### 7f. AutoAnthony bridge (session 25, 2026-09-01 - "让 StS1 角色吃随机卡池")
**缺口**：Auto-Anthonyology（工坊 3786611028，每局随机生成全卡池）的激活链
`ChaosCharacterMapping.From(CharacterModel)` 用引擎类型检查（`is Ironclad` 等）识别角色；
我们三个角色是 `PlaceholderCharacterModel` 子类 -> 永不被识别 -> `DeactivateRun()` 放行，
StS1 角色开局拿不到任何随机卡（2026-09-01 反编译 0.2.217 实锤，765 文件全量审计）。
**桥接**（`mod/Spire1Code/Interop/AutoAnthonyCompatBridge.cs` + `AutoAnthonyLoadHook.cs`）：
1. Postfix `ChaosCharacterMapping.From` 三个重载：Spire1 角色 -> 同名 GeneratedCharacter
   （Ironclad/Silent/Defect；存档按 `SPIRE1-IRONCLAD` 等 Entry、历史按 ModelId.Entry）。
   原 null->补映射；原非 null（引擎角色）不干涉。单人 `SeedBeforeSingleplayerPatch`、
   多人 `SeedBeforeMultiplayerPatch`、存档 `SeedBeforeLoadPatch` 三条激活链全部经此口。
2. Prefix 我们三角色的 `CardPool` getter：`IsRunActive && IsCharacterRunActive` 时返回
   `ModelDb.CardPool<ChaosXxxCardPool>()`（AutoAnthony 只 patch 五个引擎角色类，够不到我们的）。
3. Prefix 我们三角色的 `StartingDeck` getter：`ActiveReplaceStartingCards` 时返回
   `ChaosCardRegistry.Canonical(character, slot) x BasicCountFor`（与它的
   `CharacterPoolPatchRouting.ReplaceStartingDeck` 同构）。
**编译期引用**：`.tmp/interop-refs/AutoAnthony.dll`（工坊副本，gitignore）条件引用 + 
`SPIRE1_AUTOANTHONY` 符号--dll 缺席的构建机产出无桥接的 dll（空壳 `Apply` 返回 false），
运行时缺席则探测后静默跳过。**运行时解析**与 BaseLib 模式相同：同简单名程序集由
ModManager 的 AssemblyLoadContext 已加载实例满足。internal 类型（ChaosCharacterMapping）
经 `Type.GetType` 反射取方法再 Harmony patch。
**多人**：AutoAnthony 多人路径对桥接透明（同一 From 口），host 快照 authoritative、
双端装两 mod 即可；MP 一致性是它自身契约，不在本层职责内。
**版本耦合策略**：公开 API（ChaosRunDefinitions/ChaosCardRegistry/Chaos*CardPool）直接引用--
AutoAnthony 大改 API 时我们的构建当场失败，强制重新审计（比静默漂移好）。
**验证状态**：构建 0 错误 0 警告已部署；冒烟待用户下局（游戏进程被四人联机占用）。

## 8. Verification
Clean build; smoke-test IN-GAME (character loads, run starts, cards resolve, dungeon selection works, encounters spawn, toggles work). Deliverable = playable mod. Record smoke runs in `DEVLOG.md`.

## 9. Open gaps
Most of the original list is now closed - resolved in session 5 and documented in the internal engineering records (not distributed with this repo). Resolve anything new via `docs/BaseLib-API.md` first, then `sts2.xml` grep, then the decompiled tree at `.tmp/dllsrc/`.
- **CLOSED**: `CardKeyword` members; power class names; command builders (`DamageCmd`/`PowerCmd`/`CreatureCmd`/`CardPileCmd`); per-character energy override; `MonsterModel` stat/name/art API + move-state selection (see `docs/BaseLib-API.md` Sec 2 `CustomMonsterModel` and `research/BaseLib-unused-surface.md` Sec 2).
- **RESOLVED BY PIVOT (session 14)**: M3 dungeon-selector hook question is moot for the primary path (ecosystem supplies acts); revisit only if the fallback activates. Character-select visibility + shared-pool filter hooks stay relevant to the layer (P3).
- **UNBLOCKED by AFTP decompilation (session 13, verify against shipped v1.0.5 dll before writing)**: `N'loth's Gift` - Prefix on `CardRarityOdds.RollWithoutChangingFutureOdds(CardRarityOddsType, ref float offset)` rewriting `offset = baseRareOdds*3 - baseRareOdds` when owned (no pity-state mutation; optional Dup-transpiler captures the roll for Flash). `FaceTrader` - implement `CultistHeadpiece`/`FaceOfCleric`/`GremlinVisage`/`NlothsHungryFace`/`SsserpentHead` as `CustomRelicModel`s pooled in `EventRelicPool`, event rolls uniformly over unowned faces. `Madness` - AFTP `Cards.Madness` is the working reference.
- **Known deviations & dispositions (R-wave 2026-09-06, from the internal review `CODE-REVIEW-20260904.md`, not distributed with this repo)**:
  - R10 conditional-event spawn gating: the 13 "run-condition gated" StS1 events
    (AccursedBlacksmith, Bonfire, Designer, Duplicator, FaceTrader, FountainOfCurseRemoval,
    Lab, Nloth, NoteForYourself, WeMeetAgain, WomanInBlue, KnowingSkull, TheJoust) ship
    with empty `Acts`/fixed act membership and NO spawn-condition gating - in StS1 their
    appearance is conditioned on resources/possessions. **Disposition: deferred**, not
    rejected - implement per-event `IsAllowed(IRunState)` after the StS1-side conditions
    are javap-verified from the jar's `EventHelper.getEvent`. Cost is small but the
    condition truth table (13 events x exact thresholds) is a bytecode-mining session;
    tracked as the next P1 batch.
  - R15 `Burn` upgrade: StS1 Burn is upgradable (+2/+4 damage); our class keeps
    `MaxUpgradeLevel => 0` (StS2 status semantics). **Accepted deviation** - StS2 status
    cards are never offered for upgrade by the engine's upgrade flow; implementing a
    one-off exception costs a custom upgrade entry point for zero reachable gameplay
    (upgraded Burn only appears via StS1-specific effects that StS2 does not port).
  - R6/R8 coverage exemptions: card-layer gap (~30 colorless cards, DoubleTap/Exhume/
    Amplify/Electrodynamics/LockOn/SearingBlow) and the relic layer (29/180+) are
    **acknowledged open coverage debt**, not silently dropped - the R6 batch (nine
    engine twins) is in flight; the relic batch needs a scope decision (full port vs
    curated set) from the user before writing code. **The 4 "missing" curses are now
    adjudicated (2026-09-06, grant-source check)**: Normality is granted by MindBloom
    and Writhe by TheMausoleum/WindingHalls as shipped StS2 engine cards (Sec 7a reuse -
    no own class needed); CurseOfTheBell and Pride have **zero grant sources** in our
    ported event set (Bell belongs to an unported event, Pride to StS1's Ascension
    curse mechanic) - no reachable gameplay, therefore intentionally not implemented.
  - R14 MP hash-bypass split: `IgnoreMpModDifferences` (mod-list, default ON, mostly
    false positives) is now separate from `IgnoreMpHashMismatch` (ModelID hash, default
    OFF - a hash mismatch means at least one side's gameplay-mod binary drifted;
    forcing through is an explicit opt-in for cross-version play).

## 10. References
**Public library interface docs (`docs/`) - read `docs/BaseLib-API.md` before writing code against the framework:**
- `docs/BaseLib-API.md` - the framework we build on. Content base classes, hooks, localization, visuals, patches, and the v3.4.5-source vs v3.3.5-shipped availability table.

The full RitsuLib (1325 public types, MIT; content registration, non-Spine animation, free-play, act-enter forcing, lobby staging, asset overrides) and JmcModLib (settings UI, reflection, logging, secrets, persistence, compat shims) interface references were internal survey material and are not distributed with this repository; their adopt/skip verdicts remain in `research/RitsuLib-api.md` and `research/JmcModLib-api.md`.

**Research verdicts (`research/`) - what to adopt and why:**
- `research/BaseLib-unused-surface.md` - capabilities we are not using, with the `N'loth's Gift` and `Girya` verdicts.
- `research/RitsuLib-api.md` - per-gap adopt/skip analysis, per-consumer usage, dependency risk.
- `research/JmcModLib-api.md` - SKIP, with the measurement that proves it.
- `research/sts1data/` - extracted vanilla StS1 data (cards, relics, events, `face-relics-and-madness.json`).
- **AFTP-family reference binaries (NOT in repo)**: `G:\steam\steamapps\workshop\content\2868840\{3746969593,3747537811,3785039319,3787796638}` - decompile with `%USERPROFILE%\.dotnet\tools\ilspycmd.exe`; AFTP source at github.com/Cany0udance/ActsFromThePast (reuse permitted).

**Sources:** `research/BaseLib-StS2/` (BaseLib source, tag v3.4.5), `research/ModTemplate-StS2/`, `.tmp/dllsrc/` (decompiled StS2 engine), `sts2.xml` (game API doc, `data_sts2_windows_x86_64/sts2.xml`), `.tmp/ritsu/` + `.tmp/jmc/` (library dumps and source). Wiki: alchyr.github.io/BaseLib-Wiki. `agent://Sts1DataScout` - StS1 Ironclad + Act-1 vanilla data.

## 2026-09-23 事件移除与默认注入新契约

用户本轮要求, 原版字节码证据与隔离失败复现见内部记录 `DEVELOP-events-20260923.md`（不随仓库分发）. 本段不覆盖已有 staged/unstaged 设计记录. 事件默认关闭, 永恒卡牌遵循二代 IsRemovable, 不操作共享配置或 Steam 安装.


## 2026-09-28 姿态形态可玩化

本轮当前契约见内部文档 `DEVELOP-form-playable-20260928.md`（不随仓库分发）。六形态规格沿用 2026-09-26 用户确认值; 默认构建接入, 每局自定义模式入口与 Watcher 可选反射桥接按新契约实现. 旧摘要中的缺研究文件和缺姿态基类判断已由本轮文件检查纠正. 不发布 Workshop, 不写 Steam 或共享配置, 不操作前台窗口.

## 2026-10-02 C01 内容组开关与统一 gate 契约

本轮为 Spire1 建立"总开关 + 按内容组开关 + 单一 gate API"的稳定契约, 供 C02-C07 与后续审查使用. 本节是 2c 节旧开关表在内容组维度上的补充与覆盖; 不修改已有 staged/unstaged 设计记录, 不删除任何 held-back 门禁 (AFTP compat / Debug / Experimental / character.txt 分装).

### 已实现的开关 (Spire1Config)

| 配置键 | 属性 | 默认 | 语义 |
|---|---|---|---|
| `EnableSts1Content` | master | true | 关闭时全部组关闭 |
| `EnableSts1Characters` | 角色可见性 | true | 选人期决策, 不绑定每局快照 |
| `EnableSts1Cards` | 卡牌 | true | 共享无色池过滤 + 角色池过滤 + 每局快照 |
| `EnableSts1Relics` | 遗物 | true | 遗物池过滤 + IsAllowed + 每局快照 |
| `EnableSts1Powers` | 力量/效果 | true | 运行期效果应用入口 + 每局快照 |
| `EnableSts1Potions` | 药水 | true | 药水池过滤 + 每局快照 |
| `EnableSts1Events` | 事件 | false | 运行期事件池剥离 + 每局快照 |
| `RegisterContentNextRun` | 每局登记 | true | 新局创建时快照锁存; 旧存档缺快照按 true |

死开关处置: `ENABLE_STS1_DUNGEON` / `USE_STS1_DUNGEON` 的 eng/zhs 本地化键已删除 (代码中早于 2026-09-10 移除); `EnableSts1Characters` 此前没有消费者, 本轮接到 `CharacterGate.IroncladEnabled/SilentEnabled/DefectEnabled`, 即角色可见性 = 分装标记 AND 总开关 AND 角色组开关.

### 统一 gate API

- `Spire1Config.Spire1ContentGroup` 枚举: `Characters / Cards / Relics / Powers / Potions / Events / Monsters / Encounters / Acts / Scenes`.
- `Spire1Config.IsEnabled(Spire1ContentGroup)`: 单一查询入口; 未知组与当前无实体的组 (Monsters/Encounters/Acts/Scenes) 一律返回 false (fail closed).
- 现有计算属性 `CharactersEnabled / CardsEnabled / RelicsEnabled / PowersEnabled / PotionsEnabled / EventsEnabled` 是 API 的实现体; C02-C07 不得再自行组合 AND 链, 也不得修改 `Spire1Config.cs` (单一 gate 文件).
- `Monsters / Encounters / Acts / Scenes` 在当前产品树中没有实体 (`mod/Spire1Code` 无 Monsters/Encounters/Acts 目录, `mod/Spire1.json` 也未声明); 不创建空开关. 若未来恢复 M2/M3 内容, 必须接入 `IsEnabled` 而不是新增第二套门控.

### 注册入口与运行期入口的双落点

引擎在 `ModelDb.Init` 里对 `AllAbstractModelSubtypes` (含所有 mod 类型) 逐个 `Activator.CreateInstance`, 该时机晚于所有 mod initializer. 因此"关闭组 = 不实例化类"在引擎层不可达; 契约定义如下:

- 注册入口 (MainFile.Phase2 / 内容构造器 / `[Pool]`): 仍无条件注册, 保证旧存档的 ModelId 可解析; 注册本身不决定可见性.
- 运行期入口 (pool 查询 / 事件池 / 授予路径 / 效果应用): 必须读 `Spire1Config.IsEnabled(group)` 或对应计算属性; 目标方法/字段漂移时 fail closed, 不注册补丁、不放行内容、不把失败记成成功.
- 每局快照: `Run.Spire1RunContent.ContentActiveThisRun` 由 `RunState.CreateForNewRun` postfix 锁存、`FromSerializable` prefix 恢复、`RunManager.ToSave` postfix 写回 (Patches/Spire1ContentSnapshotPatch.cs). 运行中改设置不改变当前局; 进入/离开菜单与 load/save 不残留上一局状态.

### 兼容与边界

- 配置属性均为 static get/set, 由 BaseLib 自动进 cfg 与 Settings -> Mod Settings; 计算属性带 `[ConfigIgnore]`, 不序列化、不进 UI.
- eng/zhs `settings_ui.json` 必须为每个配置属性提供 `SPIRE1-<SLUG>.title` 与 `.hover.desc`; 两个文件键集合保持对称.
- MP: 每局快照随 `SerializableRun` 传输, 双端以房主值为准; 未新增任何同步机制, 也未改动 `mod_configs` 共享文件.
- 本节不覆盖 C02-C07 各自写集的实现细节; 各代理在自己的报告与 (若允许的) 文档段落里描述具体消费者.

## 2026-10-02 C03 运行期门控契约 (snapshot / shared pool / event filter)

本轮在 C01 的 `Spire1Config.IsEnabled(Spire1ContentGroup)` 单一 gate API 之上, 收紧 Spire1 的每局内容快照与两个运行期过滤器的生命周期和 fail-closed 语义。本节只描述已落盘的运行期契约, 不修改 C01 的开关表, 也不释放任何 held-back 门禁 (AFTP compat / Debug / Experimental / character.txt 分装)。

### 每局快照 (Spire1RunContent + Spire1ContentSnapshotPatch)

- 新局: `RunState.CreateForNewRun` 后缀把 `RegisterContentNextRun` 锁存进 `Spire1RunContent.ContentActiveThisRun`。
- 读档: `RunState.FromSerializable` 前缀从 `SerializableRun.Modifiers` 里的 `Spire1ContentSnapshotModifier.ContentRegistered` 恢复该局决定; 缺快照的旧存档回落 true (保持既有可见行为, 属于显式设计取舍, 不是静默降级)。后缀把该快照从活动 `RunState.Modifiers` 剥离, 使它不参与玩法修正显示/生效。
- 保存: `RunManager.ToSave` 后缀先按 id 移除旧快照再重建, 因此反复保存/同帧多次保存都是幂等, 且快照始终反映当前局闩锁。联机读档路径 `RunManager.CanonicalizeSave` 内部经 `RunState.FromSerializable` (其后缀剥离活动局快照) 后用 `runState.Modifiers` 重建 SerializableRun, 因此同样在后缀补写快照, 保证 canonicalize 后不丢失每局登记决定。
- 离开对局: `RunManager.CleanUp` 前缀记录清理前是否有活动对局, Finalizer 在清理后把闩锁复位为默认 true。这样主菜单/图鉴期间不会保留上一局的 enabled 状态; 每局决定已随存档持久化, 复位不改变任何存档语义, 异常路径同样复位 (幂等)。
- 关闭组语义: 设置关闭只影响"下一次新局"; 进行中的存档始终以它创建时的快照运行。运行中改设置不改变当前局的确定性池。

### 共享无色池 (Spire1SharedPoolGatePatch)

- gate: `Spire1Config.IsEnabled(Spire1ContentGroup.Cards)`。
- 目标: 仅从 `CardPoolModel.GetUnlockedCards` 结果中移除 `[Pool(typeof(ColorlessCardPool))]` 的 Spire1 卡; 角色专属池与官方卡不动 (角色池由 C02 的消费者负责)。
- fail-closed: 类型扫描失败时集合为空, 过滤器不伪造过滤, 原样返回原版结果并记录一次 Error 明确"过滤未生效"; 成功过滤首次记录一次 Info (数量)。日志只描述实际发生的过滤。

### 事件过滤 (Sts1EventToggleFilterPatch)

- gate: `Spire1Config.IsEnabled(Spire1ContentGroup.Events)`。
- 落点 1: `ActModel.GenerateRooms` 后缀 (进幕生成事件池后剥离 Spire1Event)。
- 落点 2: `ActModel.PullNextEvent` 前缀 (读档经 `ActModel.FromSave` 直接恢复 RoomSet 不重跑 GenerateRooms, 因此抽取前再过滤一次)。
- fail-closed: `_rooms` 字段解析失败时以 Error 明确"过滤未生效", 不改变原版行为; 过滤会使事件池变空时保留原池并记录 Error, 避免引擎 `RoomSet.NextEvent` 的 `events[eventsVisited % events.Count]` 除零路径。
- 日志: 记录 source (GenerateRooms/PullNextEvent)、实际移除数与剩余数; 不做"已过滤"的虚假声明。

### 未覆盖边界

- 本轮不构建、不启动游戏、不部署; 上述契约的实机验证 (新局/读档/返回菜单/事件抽取/无色池奖励) 由主会话集中执行。
- SpireHeart 仍是 autoAdd:false 的不可达内容, 本轮不改变其可达性。


## 2026-10-02 C14 LargeCapsule 旧档加载与 fail-closed 收紧

- `mod/Spire1Code/Patches/Spire1LargeCapsuleGatePatch.cs` 新增 `Player.FromSerializable(SerializablePlayer)` 的严格单目标作用域补丁，以及 `RelicModel.FromSerializable(SerializableRelic)` 的精确实例标记补丁。
- 普通旧档链虽然以 `silent:false` 进入 `AddRelicInternal`, 现在只有被反序列化方法实际返回的同一 `RelicModel` 引用才能消费放行令牌；FromSerializable 作用域内重入的全新 `RelicCmd.Obtain` 不再能借用宽泛标记。
- `AsyncLocal` 父作用域、锁保护令牌集合和 Harmony Finalizer 对称清理覆盖嵌套/异常退出；任一目标解析或安装失败都保持 fail-closed。
- 集中 Release 编译: `Spire1.csproj`, 0 errors / 61 warnings；静态文本检查通过。无正式 Spire1 PCK/digest，未部署、未启动游戏、未通过 Workshop VerifyOnly。
- 首轮 DeepSeek 路由记录: `global:deepseek-v4.1-flash` / `gateway/wb2api` / `max`。请求的监督路由为 `ovoapi:6.1sol` / `ovoapi` / `xhigh`, 但本轮没有收获其完成标记；不把集中复核冒充监督模型 PASS。


## 2026-10-02 C14 r4c silent:true 边界收紧

- 监督发现仅以 `silent:true` 放行会留下公共 `AddRelicInternal` 重入边界。本轮新增 `Player.SyncWithSerializedPlayer(SerializablePlayer)` 的严格 load-context patch，并让 `AddRelicInternal` 只接受 `RelicModel.FromSerializable` 产生的精确引用令牌。
- 因此普通旧档和多人同步都保持兼容；新的直接 `AddRelicInternal(..., silent:true)`、重入调用和目标漂移路径不再凭参数本身绕过 Cards 关闭时的 fail-closed guard。
- Release 编译再次通过: 0 errors / 61 warnings。真实 Harmony/旧档/多人运行时仍未验证，未部署到测试副本，未写 Steam 或共享 `mod_configs`。


## 2026-10-03 姿态形态 P0 实机闭环与可选依赖复核

当前 Release DLL 已完成中央构建、结构门禁、真实 Watcher 三形态卡路径和部分 mod 交叉启动复核。Calm、Wrath、Divinity 三个隔离 native smoke 场景均通过;Wrath 的 Reaper 证据为目标 `DoomPower=7`。最新依赖门禁确认发布 DLL 的 mod AssemblyRef 只有 `BaseLib`, Watcher/AutoAnthony/AutoAnthonyWatcher 等保持可选反射路径。

交叉挂载五场景也已完成: BaseLib+Spire1 无 Watcher 可初始化, BaseLib+Watcher 无 Spire1 可初始化, BaseLib-only 可启动, Spire1 无 BaseLib 按 manifest 显式拒绝,三 mod 全挂载时 bridge 正常绑定。所有 staging 平面无嵌套 manifest,共享 `mod_configs` 未变化,测试 mods 和 settings 已清理恢复。

证据与边界见内部记录 `DEVELOP-form-playable-20260928.md` 与 `reports/form-playable-20260928/`（均不随仓库分发）。本结论仍不覆盖可见 UI、视觉、长战斗、存档重载、重连、多人同步、性能和完整平衡。

## 2026-10-03 可选 Mod 交叉挂载契约收紧

- Spire1 的 `Spire1.json` 只声明 `BaseLib`; Watcher、AutoAnthony、AutoAnthonyWatcher、DirectConnectIP、ActsFromThePast 不得出现在发布 DLL 的 mod AssemblyRef 或 manifest 前置项中。
- 可选能力只允许通过运行时反射和程序集加载观察接入。缺少可选程序集时,Spire1 必须继续完成自身 initializer,并以 disabled/pending/fail-closed 记录能力状态;不得因可选程序集缺失导致 `FileNotFoundException`、`TypeLoadException` 或 ModLoader 级硬前置失败。
- AssemblyLoad 事件是晚加载的廉价唤醒源。只有 core/partial/registration/unpatch 状态确实需要没有新 AssemblyLoad 也能推进时,才创建周期 Timer 或 fallback thread。Godot/进程退出期间所有通知必须静默退出,不得访问已释放原生对象。
- 真实部分 Mod 交叉启动矩阵以内部报告 `reports/form-playable-20260928/partial-mod-launch-matrix-central-run-r29-20261003.md` 为准（不随仓库分发）;报告中的源码推理、隔离启动日志和实机行为必须分开表述。
## 2026-10-03 交叉挂载后的退出生命周期契约

- `AssemblyLoad` 订阅必须与 `ProcessExit` 共享串行 gate。入口快速检查不能替代 gate 内二次检查,否则 in-flight unsettled Apply 可能在 shutdown 标记后重新订阅。
- Retry timer/fallback thread 的失败 bookkeeping 不得在 AssemblyLoad gate 内调用会锁 RetryGate 的路径,避免 shutdown 时锁反转。所有退出期间回调必须 fail-closed。
- 最终 Release 与 r30 隔离矩阵证据以内部报告 `reports/form-playable-20260928/partial-mod-launch-matrix-central-run-r30-20261003.md` 为准（不随仓库分发）;此前 r29 是修复前的基线,不再作为最终产物证据。
## 2026-10-05 Workshop 推送前准备

本轮契约见内部文档 `WORKSHOP-PREPARATION-CONTRACT-20261005.md`（不随仓库分发）。修复 canonical精简 PCK接线和 digest producer,保留全量 fail-closed门禁,不发布 Workshop.


## 2026-11-14 v0.113 忠实度核对：共享卡复用首片

用户优先顺序8 Forms→5 Spire1，要求仔细核对一代机制与玩法体验，外观偏差单列。目标参考为v0.113.0/16ac0fe3，官方BaseLib3.4.8；旧文的v0.111/“完整”仅历史。当前安装一代jar身份未知，使用现存javap与已定位历史class，不能用旧完整库计数作本轮认证。

本轮先逐项检查SharedCardReuse的三角色清单；这是全项目核对的一部分，不覆盖全部卡/遗物/事件/多人。默认模式必须只复用一代字段与机制真正一致的原版卡；差异项优先接已有自有实现，不新增别名/兼容shim、不修改PureSts1Pools设置。

已确认首项CalculatedGamble：一代 `.tmp/sts1-jar-dump/cards/CalculatedGamble.txt::upgrade @25-27` 写exhaust=false；二代当前 `MegaCrit.Sts2.Core.Models.Cards/CalculatedGamble.cs:26-29` 加Retain并保Exhaust；自有 `mod/Spire1Code/Cards/CalculatedGamble.cs:18-20` 已正确移除Exhaust，但默认registry没有把它列为drift，导致自有实现只在pure分支可用。Main已用真实编译InjectTwin→真实ModHelper注册列表→实际选中类型OnUpgrade/引擎keyword mutator复现错误，同时Backflip shipped/Acrobatics drift-own两正控通过。最小修复是在FieldDriftTwins加入CalculatedGamble，沿已有映射注入自有类；不重写卡效果。构建与修后真实probe在DEVLOG记录，不能提前称游戏可玩/旧存档已迁移。

原版知识库持续由GPT更新并独立复核，版本/consumer/字节码offset边界见docs/knowledge/KNOWLEDGE-INDEX.md。其他人的项目确认问题查重后提详细issue，不继续无条件维护本地fork（覆盖旧文§0的无条件compat承诺）。游戏启动/存档仍须单独授权。

## 2026-11-14 v0.113 快照构造入口签名迁移

当前引擎 `RunState.CreateShared` 在 `GameMode gameMode` 后增加 `bool isCustom`（本轮源 `G:/tmp/sts2-113-20261113/game-src/MegaCrit.Sts2.Core.Runs/RunState.cs:335-377`）。本仓精确matcher仍九参数；Main真实编译DLL探针已观察resolver=null、Harmony class processor未装prefix、边界仍收到快照。证据 `G:/tmp/sts2-113-20261113/spire1-official/snapshot-probe/before.json`。这不是编译错误；读档前缀要求FilterInstalled，否则显式拒绝，不能以编译通过称兼容。

迁移合同：仅接受当前十参数的完整类型、名称、顺序与RunState返回/静态条件；精确检查新`isCustom`，不加九参数兼容分支、不放宽fail-closed。过滤仍只改modifiers，其他参数（含isCustom/currentActIndex/ascensionLevel）透传；无快照保持同一collection，混合输入保持其余modifier顺序和引用。

验收：真实v0.113程序集+正式NuGet3.4.8引用的Spire1工件运行matcher、Harmony安装与过滤前后边界；旧签名/新bool错类型或错名/额外参数/非静态/错返回必须拒绝。隔离CLR中仅替换OS与日志、在真实prefix之后截住构造体，不能冒称RunState完整构造、存档往返、游戏或多人已通过。Main统一构建/运行，结果后续DEVLOG记录。

## 2026-11-14 v0.113 共享卡注册与关键词/标签窄修复

本片只处理已回到历史字节码/当前源码的静态差异，不把三角色全量矩阵提前标为完成。`SharedCardReuse.ResolveOwnImplementation` 的类名匹配容忍大小写差异；保留 `AfterImage`、`CreativeAI`、`FTL` 类型名与存档身份，不增加逐名字alias。只从当前程序集的精确 `Spire1.Spire1Code.Cards` 命名空间、非嵌套、直接继承真实 `Spire1Card` 的类型建立一次 `OrdinalIgnoreCase` 字典；重复键显式抛 `AmbiguousMatchException`，缺失返回null。此前依赖 `Assembly.GetType(ignoreCase:true)` 自动拒绝歧义的保证已由Main真实 `.NET 9.0.19` 双case类型负例推翻（`spire1-official/case-probe/results.json`），不能继续依赖该API的首次匹配。

具名行为修复：自有 `Burst` 去除错误 Exhaust（历史 `cards/Burst.txt::<init>@0-46/upgrade@0-30` 不授予，当前官方 Burst 同样不授予）；自有 `FiendFire` 补 Exhaust（历史 `cards/FiendFire.txt::<init>@39-41`）；自有 `PommelStrike`/`TwinStrike` 补 Strike 标签（历史各构造 `@52-62`/`@38-48`；真实消费者 `v0.113 Models.Relics/StrikeDummy.cs:19-37`）。默认 `Tempest` 接已有自有实现，以恢复历史构造 `@43-45` 的 Exhaust；不修改 X 费捕获和生成算法。通用引擎队列、FTL抽牌/伤害顺序、FiendFire的消耗触发顺序仍是单独待修合同，不能由本片字段修正判为全机制等价。

验收使用正式 NuGet BaseLib3.4.8 生产工件：实际 resolver、`AddOwnImplementations`/`InjectTwin` 到原版 ModHelper 注册列表；原版 `GetResultLocationForCardPlay` 对 Burst/FiendFire/Tempest 的去向；原版 Tags getter 与 `PerfectedStrike` 编译后真实标签计数谓词。`StrikeDummy` 源码作为消费者明证，但其基类静态初始化触及 Godot 原生端，隔离探针不执行它。记录修前失败样本和无关正控、修后实际输出及工件SHA。使用隔离 CLR 与种子对象，不执行 Godot/完整出牌/存档/多人，不据此宣称游戏可玩。

## 2026-11-14 v0.113 费用寿命与生成层数窄修复

- `BulletTime` 能量降费应持续整个当前回合，不因同回合第一次打出而消失。历史 `actions/unique/ApplyBulletTimeAction.txt::update@33-35` 调 `AbstractCard.setCostForTurn(-9)`；当前 `CardEnergyCost.cs:197-225,333-346` 区分 `WhenPlayed` 与仅 `EndOfTurn`。自有卡局部使用 `EnergyCost.SetThisTurn(0)`，保留 X 费排除、负费保护和已有星费用扩展 `SetStarCostThisTurn(0)`，不修改原版全局 helper。默认共享映射必须指向该自有类，pure 路径保留旧类型身份。
- `CreativeAIPower` 每层独立抽取并添加一张能力卡；不能把所有层合成一次 distinct 请求，否则可能排除重复。历史 `powers/CreativeAIPower.txt::atStartOfTurn@0-38` 按 amount 循环；当前官方 `Models.Powers/CreativeAiPower.cs:19-34` 同样逐层 count=1。只修自有层数循环，保留参与者、非正层数和两个内容门控，以及逐次 await。当前候选池、回合阶段与一代 ActionQueue 的等价性仍是另项未完成工作，不将本片标为整卡 MATCH。
- 验收：正式 NuGet BaseLib3.4.8 工件的真实 `BulletTime.OnPlay`/费用清理、真实 `CreativeAIPower` 异步体与实际注册消费者；修前必须见费用第二次使用与三层生成失败，修后正控/反控齐备。隔离对象与候选/生成落点边界只作夹具，不声称实际生成 RNG、满手落点、NoDraw、完整回合派发、游戏、存档或多人已运行。

## 2026-11-14 v0.113 TrueGrit 随机消耗合同

- 一代 `.tmp/sts1-jar-dump/actions/common/ExhaustAction.txt::update@12-30,@38-128` 对空手牌直接结束、单张直接消耗，不抽 RNG；多张 `@129-181` 从 `cardRandomRng` 取一张。当前 v0.113 `Rng.cs:300-309` 的 `NextItem` 对单张仍调用 `NextInt(0,1)`，而官方 `TrueGrit.cs:39-43` 不旁路单张；自有旧版使用 `Guid.NewGuid` 排序，完全不消费可保存的战斗随机流。旧矩阵默认 MATCH 因遗漏单张后的随机流状态须限定撤回，不能只把 Guid 换成无条件 NextItem。
- 修复自有 TrueGrit：先等待原有格挡，再从当时手牌读取；零张无选择、单张直接取唯一对象、多张用 `Owner.RunState.Rng.CombatCardSelection.NextInt(0,count)` 后索引取牌，不复制列表/随机排序。按现有卡牌随机流映射，不承诺跨一二代同种子的逐项输出相同。
- 默认注册也接该自有实现，pure 保留同一原类型；保留 7→9 格挡及升级手选一张，不改变通用 RNG、通用消耗/格挡命令。目标字段以一代 `cards/TrueGrit.txt::<init>@27` 的 SELF 与当前官方 Self 对齐。选择分支只读引擎 `IsUpgraded`，删除无法随 `CardModel.DowngradeInternal:2336-2357` 清理的私有 `_choose`；真实升级→降级→重升级必须恢复随机/手选行为，不能仅重置格挡数值。
- 验证：正式依赖工件的真实 OnPlay/OnUpgrade、真实 Rng 序列化状态及实际默认/pure注册；空/单/多手牌、恢复相同随机状态、后续抽取一致、升级选择/空结果、格挡异步未完成前不得提前抽签。消耗/格挡/UI命令边界是夹具，完整引擎钩子、UI、跨进程存档与多人保持未验，不把局部命令序列当一代完整队列等价。

## 2026-11-14 v0.113 Claw 增强对象域合同

- 来源 `.tmp/sts1-jar-dump/actions/defect/GashAction.txt::update@0-205`：先增强当前card，再按Discard→Draw→Hand逐卡增强同类。它不扫描Exhaust/其它Play/主Deck，也没有去重；若当前对象已在上述列表，按实际逐次操作会再次增长。本片忠实保留这个条件行为，不采纳早期草稿“当前一次+所有对象去重”的额外保证，不伪称已证明真实队列中全部重复位置可达。
- 自有Claw旧 `Owner.PlayerCombatState.AllCards.OfType<Claw>()` 的真实consumer `PlayerCombatState.cs:73-85` 覆盖Hand/Draw/Discard/Exhaust/Play；须改为先this，再Discard/Draw/Hand，保留原伤害完成后的结算时点。逐牌索引，不构造数组/Concat/HashSet；只更新自有Claw及派生实例，不新增全局钩子。默认已映射该自有类，pure身份不变。
- 基伤3、升级+2、每次增加2保持；`_extraDamageFromClawPlays` 的累计与`AfterDowngraded`恢复保留。验证真实OnPlay的受影响/排除实例、当前牌暂离所有pile、攻击await前后手牌域变化、升级→真实降级后的累计、重复当前身份不擅自去重。攻击Execute为命令边界夹具，真实攻击/钩子/跨引擎队列及多人不由本片认证。

## 2026-11-14 v0.113 LegSweep 默认复用次序合同

一代 `.tmp/sts1-jar-dump/cards/LegSweep.txt::use@0-47` 先排Weak再排Block；当前官方 `Models.Cards/LegSweep.cs:31-37` 先await Block再Weak。自有 `Cards/LegSweep.cs:20-27` 已是先Weak后Block，保留Powers配置门，11→14Block/2→3Weak及2能量正确。因此只在SharedCardReuse的差异集合加入LegSweep，默认改用已有自有类，pure和类型身份不改，不重写能力/格挡引擎。

以真实默认/pure注册选中的构造/OnPlay/引擎升级降级证明调用轨迹：基础Weak2→Block11、升级Weak3→Block14、Powers关闭仍Block而不施加Weak；Weak尚未完成不提前Block，返回null（被拒的命令边界控制）仍继续Block。探针隔离动画、能力/格挡命令终点与配置，不冒称真正Artifact、Juggernaut杀敌、真实弱化/格挡钩子、UI或多人已通过。完整共享伤害/队列语义是另项合同。

## 2026-11-14 v0.113 Nightmare 战斗生成资格合同

历史 `cards/Nightmare.txt::<init>@0-48` 没有HEALING标签，基础AbstractCard标签初始为空（`cards/AbstractCard.txt::<init>@140-148`），正控Feed明确添加HEALING（`cards/Feed.txt::<init>@57-67`）；`AbstractDungeon.returnTrulyRandomCardInCombat@118-192` 对稀有池也只排该标签。当前官方 `Models.Cards/Nightmare.cs:21` 则明确CanBeGeneratedInCombat=false，真实消费者 `CardFactory.FilterForCombat:160-162` 会排除。自有Nightmare继承Spire1Card门控后的true，因此默认差异集合加入Nightmare，沿已有自有类型，不改全局原生Nightmare。

本片只恢复默认自有候选资格：Cards关闭或内容不可用熔断仍排除；Feed/Basic等其它排除仍保留。自有3→2能量、Rare/Skill/None、Exhaust、延迟复制门控/能力调用不改；历史None目标保留，不能为贴近当前shipped Self而改一代字段。以真实默认/pure注册、构造/升级/降级、真实FilterForCombat、Cards关闭与两熔断正反控制验证。候选准入不是实际随机命中、复制能力完整行为或存档迁移证明，后者继续单独验证。

## 2026-11-14 v0.113 Havoc 自有顶牌打出合同

历史 `cards/Havoc.txt::<init>@0-33/upgrade@12-16` 本体不消耗，1→0费；`PlayTopCardAction.update@11-87` 在抽堆空且弃堆非空时先洗牌，`@133-138` 在自动打出前设置目标牌一次性消耗。自有旧 `Cards/Havoc.cs:17-25` 错给本体Exhaust、只直接读取抽堆并在自动打出后手工Exhaust目标，未使用当前统一顶牌consumer。

最小修复复用 v0.113 `CardPileCmd.AutoPlayFromDrawPile(choiceContext, Owner, 1, CardPilePosition.Top, forceExhaust:true)`；删除自耗尽及手工后置消耗，不新建洗牌/出牌调度器。当前默认shipped路径已用该helper，不改registry；pure保留自有Havoc类型/None目标与既有图片和本地化。自有hover提示另以真实Exhaust提示表达目标牌效果，不用本体keyword误代。

验收实际正式DLL中的自有OnPlay和原引擎AutoPlayFromDrawPile/ShuffleIfNecessary：有顶牌、抽空弃有、双空、打出前forceExhaust、先移入Play再AutoPlay、Owner死亡/战斗结束、异步洗牌与打出等待、异常传播；真实卡keyword/result-location与升级→降级。仅洗牌最终搬运、牌堆Add、AutoPlay执行终点和原生环境隔离，不能冒称完整随机洗牌/目标选择/目标牌效果/消耗hook/游戏/多人已验。一代选敌在Havoc.use提前、当前自动选敌在CardCmd.AutoPlay的跨引擎随机与快照边界仍单列，不以本片顶牌修复宣称整卡全等。

## 2026-11-14 v0.113 Chaos 局部随机候选与预采样合同

历史 `cards/Chaos.txt::use@0-37` 在任何ChannelAction执行前选好全部1/2球；`orbs.AbstractOrb.txt::getRandomOrb@8-79` 的候选顺序为Dark/Frost/Lightning/Plasma，使用cardRandomRng。当前原生 `OrbModel.cs:27-34,169-172` 额外包含Glass，自有Chaos应局部恢复四球，不改原生全局候选。保留当前宿主 `RunRngSet.CombatOrbGeneration` 随机流映射，不承诺跨游戏同seed结果。

自有类基础1能量/Uncommon/Skill/Self，无Exhaust，升级1→2球；入口捕获IsUpgraded及本次rng，先抽取所需可变球，再顺序await原OrbCmd.Channel。第一次Channel的hook消费同流不能改变已选第二球；同类型重复必须是不同mutable实例。由原引擎处理容量、激发、Owner和生命周期，不复制全局球管线。默认差异集合与pure解析都接自有类。

正式BaseLib3.4.8的PrefixIdPatch/TypePrefix决定自有ModelId，须用真实DLL确认，不从反编译插值里的数字45猜分隔符。本地化从当前宿主PCK两语言CHAOS项具名读取并保原变量；自有小/大图显式引用宿主Chaos本身的atlas/packed路径，不打包零售图片、不用通用占位图。PCK目录与具名payload的MD5正控/篡改反控仅证明文件与引用元数据，实际ResourceLoader/UI仍需另验。

验收真实正式DLL：默认/pure类型、实际ID、四候选与原生Glass正控、真实RNG恢复和其它流不变、预采样面对Channel同流干扰、等待/异常/两个Owner、真实升级降级与重复球实例；本地化用真实LocString/DynamicVar formatter检查基础/升级。Channel终点和原生初始化夹具须在报告明示，不把命令级通过冒充满槽激发/真实hook/游戏/存档/多人通过。

## 2026-11-14 Darkness升级与GoldPlatedCables局部合同

来源为历史 `cards/Darkness.txt::use@0-36`、`actions/defect/DarkImpulseAction.txt::update@23-142` 和 `orbs.Dark.txt::onEndOfTurn@22-64`：基础只Channel1Dark；升级在Channel后对全部Dark依序各触发一次，然后持Cables且当前首球为Dark时再触发首球一次。额外首球必须位于全Dark遍历之后，不把序列A/B/A改成A/A/B；基础不触发。当前宿主 `GoldPlatedCables` 类型按具名consumer映射一代Cables，球本身与Focus等数值机制仍使用宿主，不承诺完整球引擎全等。

自有Darkness保既有默认/pure身份；只局部调用 `OrbCmd.Passive(...,countAffectedByHooks:false)`，在原遍历之后检查本Owner的 `GetRelic<GoldPlatedCables>()` 与本队列首Dark追加一次。不能把通用次数hook打开而扩大到其它二代倍率，不能把Loop也改成受Cables加成。用真OrbCmd.Passive/DarkOrb.Passive与独立事件序列/实际EvokeVal正反控，Channel和完整combat监听器环境隔离须明示。双语描述补升级条件句，用真实IfUpgraded formatter区分基础/升级/降级，沿现有两张画像，不扩资源替换。

## 2026-11-14 自有postdraw三段桥实施合同（实施前）

原Silent设计见 `reports/spire1-turnstart-design.md`，34项为NOT RUN。Main已亲读原 `SetupPlayerTurn` 与正式DLL MethodInfo/IL：一处基抽call（序号303）、一处原AfterPlayerTurnStart（349），之间原await GetResult335与取消检查342；状态机为该方法AsyncStateMachineAttribute给出的值类型。此为真实producer形状，不是桥挂载或运行成功。

仅替换这两个call，保留原取消点与全部host Draw/Hook调用；同方法Prefix/Postfix/Finalizer建立每invocation的AsyncLocal lease，caller退出恢复previous，原Task完成finally释放本lease。基抽前冻结自有NoxiousFumes、单数DrawCardNextTurn、Tools的amount/source及NF目标；基抽后先毒→固定额外抽/按当前类型移除DrawCard→Tools固定抽再固定弃，最后原全局Hook。source被基抽移除不取消已捕获批次；晚挂不补本轮。小去重记录按原CombatTurnState/PlayerCombatState/TurnNumber，只限制own，不能吞第二次host调用；不同Owner/额外回合分开。取消/异常保原传播，清理不覆盖原错误。

安装须注册前原子完成并校验精确Harmony成员；两call、scope或所需getter任一失配则使用现有内容熔断，不退回全局Draw或shipped假装修复。原生power不变，迁移Predator/Doppelganger/Discipline三处共享producer及NF/Tools自有消费者、默认/pure接线，删除旧重复触发通道。沿现有Powers/master/run-content门，关闭后不继续余下own，不增加其它全局配置机制。普通未选中路径不分配payload；选中但无三类时不复制powers/目标数组。

资源沿正式BaseLib自有模型接口与明确原生能力图标；必须真实ID/formatter验证，不制造通用占位资源。完整网络选择/存档与其它postdraw内容相对全序尚不认证；其余host NoDraw基抽例外明确保留，两个额外Draw仍false。实际验证需以真正producer IL的正控/缺站点/多站点/取消移位反控，以及同ctx挂起嵌套、不同Owner、旧实例移除/新挂/取消清理为准，不以编译成功代替。

## 2026-11-14 自有postdraw桥运行增量（局部PARTIAL）

正式DLL最终 `5a3821f0fca2d9ef2cebe3d59ba1e45c61009ac476be97963bc65abb6d94f182`；具名源冻结在 `G:/tmp/sts2-113-20261113/spire1-official/turnstart-probe/source-sync-terminal.json`，bridge源SHA `0a4b282d6a70b045984037499719fef1d4c484bbb61fcfda665a3c85b2057d69`。正式BaseLib3.4.8构建54warnings/0errors。各阶段证据见同目录 `stage-summary.json`，不可把不同DLL的结果相加称一次全绿。

独审反例带动的修复：选中run即建lease，live Powers只决定A时是否捕获；固定当前编译器完整await形状，检查恢复switch/awaiter字段与local/分支/异常边界，原取消点不动；原State的typed `IsCombatEnding` 与Owner active阻止后续own；每段await后发完成通知前再检查。Release是永久终态，捕获/释放同lease锁串行，Finish只用锁内取出的Batch；第三方early或late postfix抛同步异常也释放自身，之后原Task恢复只走host，不能重抢claim/建payload。成功移除source不等于释放lease，仍保留已捕获批次。

真实原始IL变异：旧工件10项中9项新控制流负例漏拒；修后 `install-reviewed.json` 21/0（含18个拒绝变异及实际安装/重入），绑定1cae工件。实际Setup/Draw/Hook主体验证正序、数量/目标快照、旧source移除、晚挂不补、满手/NoDraw、同ctx/两Owner与取消/异常；`behavior-reviewed.json` 43/1中的结束通知断言错误已由 `ending-reviewed-corrected.json` 2/0替代，保留原失败文件。中途ending四点由 `mid-before-corrected.json` 5/4对 `mid-fixed.json` 5/0验证，绑定b6a7工件；两个Tools旧计数包含结束前通知的观测错误另存，不移除记录。

最终5a38工件增量：`pending-final.json` 5/0（early/late postfix×A前/A后pending），`release-final.json` 6/0（beforeA/base/choice故障、取消、成功直接读Batch清空/Claim终态），`native-final.json` 2/0（原生AmountOnTurnStart正控），`normal-terminal-final.json` 11/0（终态改动相关正常路径）。真实三producer形成同一ownD4、default/pure注册、N/Tools直接OnPlay与双语能力/卡面formatter已有分工件证据；其中Predator伤害Execute、instanceApply、卡堆Add、选择/Discard、有限listener及原生平台为fixture，不签完整伤害/联网UI/重洗/实际加载。

资源：两新能力沿明确原生Packed与ResolvedBigIconPath；正式PCK目录有对应atlas/import元数据，beta-only反例走原生getter。只验证目录与Exists夹具，不签ResourceLoader实际载入。zhs Tools卡面已纠正为先抽后弃；两新能力双语表齐备，Tools英文显示实际层数。

原34项设计矩阵不是34项全部完成：真实网络选择/同ctx并发UI、存档恢复、完整Main初始化与游戏渲染未跑；无游戏/部署/存档/发布操作。最终独审 `reports/spire1-turnstart-implementation-review.md` 增量6及 `spire1-turnstart-implementation-review/terminal-ledger.tsv` 已收口：14冻结源身份全部匹配，17阶段逐份核对，F01–F06无剩余已证未修缺陷；整体PARTIAL / LIVE HOLD。审查按各工件分签，未把b6a7中途ending结果转授5a38；MainFile与SharedCardReuse后续MultiCast改动不在该冻结审查范围。

## 2026-11-14 MultiCast无球付款实施合同

沿 `reports/spire1-multicast-design.md` 唯一候选，只对自有精确MultiCast类型：SpendResources入口无球时保留入口raw CapturedXValue，跳过完整energy付款及0额hook，原private SpendStars通过typed delegate仍执行；有球原方法完整放行。OnPlay先验安装/判球，首次await前固定ResolveX+升级次数，非正次数不播动画；原生ChemicalX仅对同Owner的自有无球牌跳Flash，不改ModifyXValue或其它listener。两prefix+private delegate注册前原子证明，失配走既有不可用状态，默认/pure都选自有实现。

明确保留宿主差异：付款早于BeforeCardPlayed/OnPlay，期间球队列变化不承诺旧单action原子性；raw captured与实际Spent可不同，paid-auto的ResourceInfo沿host，X不遵从普通free cost modifier，legacy -1不造新协议，有球0额付款hook保留。不能把单卡修复宣称一代全部时序等价；不能付款后退款、伪造free标志、让无球顺便免星成本。当前C#工件 `31fa84bb7b02e075895baaba548d5ef090b61ebe305309f82f8df1d8985c4b55` 已完成下述具名实际控制，设计33项不直接等于全部已验。

### MultiCast行为增量（独审已收口，整体PARTIAL）

`G:/tmp/sts2-113-20261113/spire1-official/multicast-probe/stage-summary.json` 保21阶段及各自DLL身份：旧5a38付款24/13，对31fa84的25/0；旧d820第二Verify失败+首Unpatch故障1/1，对修后1/0，后者仍明确spend残留1、chemical0，不能称全撤。无球跳完整energy消费者而原private SpendStars保持正额/零额/挂起/取消/fault；有球0额hook、独立星、excess转换、原生与其它X牌、派生付款域分别有正控。

真实manual ExecuteAction、WhisperingEarring付费自动、普通CardCmd.AutoPlay、OnPlayWrapper/GeneratePlayCount/Replay、CreateDupe和FreeSkill扣层进入；能量/星事件、History、Hook与ChemicalX原体保持。首caller日志10/2后原Cancel触及Godot平台fatal，两个失败是观察未用的f.Context；修后取实际action.PlayerChoiceContext，仅重跑相关2项和星不足拒绝，4/0。Cancel终点隔离，不认证原native deferred completion。caller-extra8/0与ordering6/0分列；先付款后hook造/移球、ChemicalX先后顺序造成Flash与效果分离的DRIFT已实际观测，不称旧版队列全等。

8种安装故障各1/0：缺三个目标、private签名错、第二Patch抛、priority错、已装缺一、method被替换。均锁存失败、拒绝own效果、保foreign与native；method替换例只移精确own方法，非own sentinel残留明确记录。默认/pure真实Register均own，未跑完整Main或materialize pool。

双语MultiCast升级卡面原均为X，现用 `{IfUpgraded:show:X+1|X}`。正式SimpleLoc及真实GetDescriptionForPile/UpgradePreview的修前5/4→修后5/0；中间formatter5/4遗漏原IfUpgraded注入是夹具错误，保留且不归生产。构造property -1不等于consumer成本：原CardEnergyCost将CostsX的Canonical归0，真实API控制已证，不改构造器。文案从具名磁盘表读取，未打PCK。真实球副作用、图像加载、完整启动/UI/存档/网络继续HOLD；animation/EvokeNext及平台/堆迁移为明确终点夹具。

## 2026-11-14 Claw增长参数捕获增量合同

一代 `actions/defect/GashAction.txt::<init>@9-11` 保存amount，`cards/Claw.txt::use@74-87` 在排好DamageAction后、伤害实际执行前把magicNumber传入；update再读取晚期Discard/Draw/Hand。修前own在await攻击后才读Increase，主会话 `claw-probe/capture-before.json` 已复现：挂起期间Increase2→7导致source/incoming变10而非5，下次7→11后变21而非12。修后仅把增长参数读取移到构建攻击后、Execute之前；收件牌堆仍在攻击完成后读取，保持先this/Discard/Draw/Hand、无去重及累计账本。`capture-after.json` 在0817e836工件5/0；同异常不增长、成长副本真实clone/升级/降级分叉有界通过。旧clone失败是裸Run缺_allCards夹具前置。原SolReuseDefectResume独审已收口，当前Claw SHA28385336430640231d77381205debe6af68f9630c83997f2c641cfe98b045a04；量时点局部接受，不认证完整伤害/旧队列/存档/网络，不把本次5项扩大为旧14项或注册分支重跑。

## 2026-11-14 MultiCast真实Frost消费增量（有界独审闭合）

`multicast-probe/real-orb-first.json` 8/0仅绑定0817e8364024873d1ea0b8d90e56b99837afc8f28920cb97a8c792b5a4f0a18c，不把31fa84旧阶段转授新DLL。撤掉EvokeNext终点替身后进入真实OrbCmd.EvokeNext/private Evoke、FrostOrb.Evoke、GainBlock/Hook/History与RemoveInternal：基础3次block15/history3/activation3，升级+Chemical6次block30，最后一次才从队列移除，AfterOrbEvoked在RemoveInternal之前。无球不付能/不效，有球0能保球且保原0额energy hook；原生MultiCast有球同consumer作正控。另补动画挂起中真实升级仍按已固定3次/block15；raw5/当前2+excess/独立star1真实支付(2,7)、Captured2后仅两次block10。新两probe源冻结在 `multicast-real-orb-sources/`，source-real-orb.json绑定。仍隔离裸state/有限listeners、房间UI、声音/VFX、动画/计时；不签其它球全链、实机、网络或存档。

原Sol113DeliveryAudit已在 `reports/spire1-multicast-implementation-review.md:132-149` 独立核对两冻结源与JSON/log的8项；报告SHA3842ab90c62811b45888408b02550cecf4441b073c245b3f8ed1deb42e7f65dd。8项实际组成是安装1项、Frost矩阵5项、T04/Y05补证2项，不是8个完整玩法域；本增量PARTIAL接受，无新增已证待修项。旧21阶段和27/6映射不变，Hibernate/Focus及其它球种、生命周期失效/异常、完整Main/UI/PCK/存档/网络均未认证。

## 2026-11-15 伤害快照候选与FTL每次出牌控制流

仅scratch `G:/tmp/sts2-113-20261113/spire1-official/damage-snapshot-probe/`：显式单target数值捕获、SnapshotAttack和两精确IL传递保原伤害/格挡/历史/通知；普通同card raw继续lateAll，Vigor只在显式capture读取当时Amount、不改外命令锁。初31/1→Intangible夹具单项1/0、边界8/1→dead-target夹具单项1/0分签，原失败保留。原SolReuseIroncladResume独立接受有限子域，不代表来源/队列/Vigor按ID消费完成。

真实stale负控1/1暴露挂起后换战斗仍旧HP200→195、新history2；首修捕获CombatId并在两状态机initial及7/12个GetResult后校验，30/0有界通过且独审。随后捕获时新ID仍可能借给旧State，domain-before2/1；typed只读LiveTurnStateFor→State绑定修复后domain-fixed4/0，增量独审中。两个修复版本不跨签。Hook内部多listener在外GetResult前的副作用、自然致死收尾、完整安装失败门禁、Main/UI/存档/联网仍未认证，不称完整生命周期安全，生产DLL仍0817e836…a18c。

FTL队列报告 `reports/spire1-ftl-queue-design.md`、Main合同 `ftl-wrapper-contract.md`：Frame+四producer仅载荷层，不能在OnPlay/AfterCardPlayed/wrapper末任一点flush就恢复旧UseCard/真实结果堆/每次Replay及effect顺序。Evolve尾抽可能洗回已Discard的原FTL，同一Draw action的EA头插不得抢占，跨shuffle又要拆action；当前全局Hook FIFO也不是旧card-trigger先于powers时钟。下一生产实施必须提供受控per-play continuation、真实replay对象、draw packet与上下文transport；28项Q反例全部NOT RUN，不发布无buff窄路径或临时移堆补丁。


## 2026-11-15 Claw真实克隆与序列化消费者有界收口

正式Spire1工件仍`0817e8364024873d1ea0b8d90e56b99837afc8f28920cb97a8c792b5a4f0a18c`。新`spire1-official/claw-serialization-probe/`构建3nullable/0errors，positive31/0、真实DynamicVars alias7/1、删除真实Scythe Props32/2，exit0/1/1；19个保pass的reader实际字段反控均按指定原因拒绝。正例开始时间未采样，executions保null，不从文件时间倒填。

真实own两次OnPlay使源7/4；clone/dupe独立增长到9/6，源仍7/4；升级11/6再降级9/6。实际DTO/JSON/packet恢复为不带战斗增长的普通卡，升级own恢复5/0、降级3/0；原生Claw同样不保存增长，原生Scythe依靠Props保存18/5，删除Props后两路13/0。Serialize.Equals即使true也不证明Props或floor保真。AttackCommand.Execute仍为端点夹具，记录3/5/7等是请求数值，不是实际伤害。

`reports/spire1-claw-serialization-review.md`与`reports/spire1-claw-serialization-runtime-review.md`分别源码/运行独审，27身份与21文本来源及全部明细已限定闭合。packet只记录bit计数/长度/SHA，未保原buffer供独审重算；不认证完整战斗存档、网络传输、真实伤害或可玩性。当前局部PARTIAL，不据此给战斗中保存新承诺，也未改生产增长字段。

## 2026-11-15 Scrape候选终态虚报的真实反例与窄修

原费用集成20场景已独审但保留Discard终态未验。Main亲读CardCmd.DiscardAndDraw172-176的IsOverOrEnding正常早退后，新建独立`spire1-official/cost-consumer-ending-probe/`，Compile Link原单一CostConsumers/CostScope/CostHooks/Fixture，不重跑原20组。5场景修前5/2exit1：最后真实AfterCardDrawn令敌HP为0，原History/Hook均无弃牌但候选报告两张；首个AfterCardDiscarded后令HP为0，原只弃第一张但候选报告两张。live-two、真实弃后hook再移Exhaust、already-ending三正控均成立。原IsEnding/Draw/Discard/Add/History/Hook原体保留，HP通过实际SetCurrentHpInternal(0)注入，不冒称完整Kill或teardown。

窄修仅scratch `CostConsumers.cs`增加Combat引用及每次实际Discard调用前IsOverOrEnding检查，结束即停止后续跟进；不通过最终pile判断是否弃牌，避免错误漏掉真实弃牌后由hook移往Exhaust的对象。新5/0exit0、fatal=null，两次构建各3warnings/0errors。before/after JSON、log、源副本和probe DLL分别保留；旧20组与embedded receipt继续绑定旧冻结字节，当前主集成receipt已STALE，不能直接重跑旧工程写覆原结果。

原Sol113DeliveryAudit独审已闭：先固定独立oracle，再以一次静态命令核18具名文本164570B、全部10行actual/事件/History/牌堆，精确确认修前仅两ending失败、修后5/0；源delta仅Combat引用、注释与逐项guard。Main已读完整增量。新probe只有运行时源hash，没有build-embedded receipt或新patch inventory，不借旧20的更强身份元数据。当前整体PARTIAL，生产尚未接线，跨Owner/PCS/新combat、状态性stop-ending谓词、异步fault及完整队列不由这5例认证；费用大项不因此标done。
## 2026-11-15 — GoForTheEyes真实伤后判断与Weak消费者有界闭合

主会话已亲读原GoForTheEyes.OnPlay、PowerCmd Apply/Hook链、最终Program/完整gate及`reports/spire1-gofortheeyes-fidelity-review.md`的原始来源和逐case独审。只认证headless、有限listener、脚本MoveState与该真实命令链，不认证完整付款/OnPlayWrapper/队列/Weak后续倍率和回合tick、自然遭遇全覆盖、多人或实机。

实际命令：`G:/dotnet10/dotnet.exe build G:/tmp/sts2-113-20261113/spire1-official/gofortheeyes-fidelity-probe/GoForTheEyesFidelityProbe.csproj -c Release --no-restore --disable-build-servers -m:1 -nr:false -p:UseSharedCompilation=false`，`build-callsite-fixed.log`记录2warnings/0errors。`C:/Program Files/dotnet/dotnet.exe .../bin/Release/net9.0/GoForTheEyesFidelityProbe.dll positive`为12/0 exit0；`mutant-prehit`为13/1 exit1，仅attack-to-summon-after-damage失败，真实getter1次改回伤前true并多施Weak1/PowerReceivedEntry；反向变化和其它case不受该变异影响。精确argv/实际UTC见`callsite-fixed-executions.json`。

源码/程序集冻结：`G:/tmp/sts2-113-20261113/gofortheeyes-callsite-fixed-sources/`的5份源码及manifest、DLL；实际probe SHA256 `54861f0cbe8b3a43f11d88c1eedfc85ca961767af6c922ba2ace3c3e469b3236`，MVID `e11fd18b-535c-427f-b8f3-d71418cd0c06`。原生音频第一次0xC0000005保留；加NoAudio后12/8的旧`positive-audio-fixed.json`也保留（generic观察漏记8次请求，真实Weak未被替换）。最终只对原Go.OnPlay.MoveNext的唯一Apply<WeakPower> call插透明typed观测，6参数和原Task原样转发，严格唯一call/返回与参数签名；不再称OnPlay完全未插桩。NDebugAudioManager.Instance=null仍是明确平台展示隔离。

`node --max-old-space-size=128 .../check-results.mjs`实际exit0；五独立调用`--mutate-actual-weak`、`--mutate-extra-pass`、`--mutate-history-damage`、`--mutate-history-actor`、`--mutate-history-power`各exit1，均changed及指定semantic rejection，不以任意异常算通过，日志和UTC见`gate-callsite-fixed-executions.json`。原KbTransportReview逐行核actual Damage/HP/Actor/History/Weak对象关系及5控制，无重跑成功程序；Main接受该有界证据。审查范围PARTIAL，不改shared-twin/生产映射，不将DeathBlow、人工死留或取消场景说成旧版自然遭遇已证。当前安装一代jar/common-version仍UNKNOWN。
## 2026-11-15 — FTL draw/snapshot分段实际证据独审，不是完整U接线

Main统一工程`spire1-official/ftl-integration-probe`直接Compile Link原singleplay wrapper与共享snapshot kernel，不复制算法；20源240908B冻结`ftl-integration-main-first-sources`，probe SHA`76ba267db29f155aff77275621ebe5a37e41851af64891ddd6b0415f7fc7338e`。最初中央实跑draw normal17/0、head17/2、whole17/1、tails17/6；snapshot normal15/0与三runtime变异16/1（S02/S02/S01）分别保真实metadata/UTC/argv/loaded identity，build13个未赋值夹具warnings/0errors。旧原capture不可被后续源码覆盖身份。

FormsSerializationReview独立复算68条draw实际trace/牌堆/能量/HP/lease导出、20冻结源，逐失败归因并重建5个reader负控全JSON bytes/SHA；原日志与收据分别指定错误exit1。tails唯一Q18 unhandled发生在B-before-U/A后的A.Validate，是次生consumer异常而非setup，不能当原设计保留B空跑的must-fail。异常detail.basic.targetHp200为已替换旧Base.Target，不是实际BigDummy；精确detached因缺直接字段仍只[INFERENCE]，Main准备独立Q18最小新增观察，不重跑17例正矩阵。

SolReuseIroncladResume的SIR08独立逐case重建snapshot actual，不以Pass算机制：正常0个原oracle违例，三错序/晚重算各S02/S02/S01。Main亲读完整独立reader及26定点实际反控，执行`node --max-old-space-size=128 G:/tmp/sts2-113-20261113/spire1-ftl-snapshot-integration-review/read-actual-results.mjs --self-falsify`，exit0；`main-actual-read.json`为OBSERVABLE_ORACLE_CONSISTENT_PARTIAL，baseline.errors空、26/26检测到指定actual错误并保原Pass，stderr空。没有重跑CLR。

Main裁决仅接受这些已导出observable：未导出的稳定对象/异常/lease身份、同步modifier前presence独立运行输入、任意异步失效、Q18异常精确目标状态不补签。完整U-origin与真实返回/await合同、旧shuffle构造通知等仍待接，不把分段全部绿色升级成FTL可玩，不生产部署。两个独审均已读原证据/来源，有界PARTIAL写审查账本。
## 2026-11-15 — FlameBarrier真实消费者与独审有界闭合

Main最终history版本九源/receipt冻结在`G:/tmp/sts2-113-20261113/flamebarrier-main-history-sources/`。实际四模式normal23/0、self23/1仅F04、early-expiry23/2仅F14/F16、old-instance23/1仅F23 lifecycle；进程0/1/1/1，三mutant真实消费者变化而非改PASS。作者actual gate exit0含9定点负控；该log只整体PASS，不宣称9项各有独立运行收据。独立FormsFullPowerReview逐case复算四raw/日志/真实HP-Block-energy/intent/history引用见证和完整事件，独立reader exit0/problems=[]，未重跑原成功载体。

F23证明captured旧power移除后fresh同ID replacement6仍按ID删除，PowerReceived与Removed引用见证均同replacement；wrong-instance mutant留replacement且缺removed。F17同实例换PCS再apply在写前拒绝，Amount4与原Binding不变，旧intent仍拒绝；同ID新实例不与同实例复用混同。拒覆盖收据exit2、前后normal SHA等于当前`97fc3165064dfca6cbb3cc8e7f7388e278ce01531d23ec7fd80504fbbdb298d5`，拒绝stdout含Sentry行，不称无输出。首六CS8752、typed版20个空mapHistory异常和修后0/0build保留，非生产缺陷。

本条只完成“触发域与寿命候选及真实消费者独审”局部项：16平台prefix/有限附着power监听器、bare Run/PCS，完整旧队列、致死/逃离域、生产注册、资源渲染、mod共存、游戏/网络/存档仍PARTIAL。独审新发现旧RemoveSpecificPowerAction在ID查找前看dead/escaped、当前局部continuation无同类门；是否实际存在power仍附着且queue继续未证，记UNKNOWN，不把F23外推到该域。报告`reports/spire1-flamebarrier-fidelity-review.md`，复算摘要`spire1-flamebarrier-fidelity-review/main-history-review-observed.json`。总目标未完成。
## 2026-11-15 — WhiteNoise前置共享CostScope归属纯查询

原WhiteNoise作者/独审指出：同combat误传另一owner的CostScope会在Factory消耗RNG并创建卡后才被EnrollFresh拒绝。Main亲读唯一共享`spire1-official/cost-producers-probe/CostScope.cs`，LSP references返回无server后新增`IsBoundTo(Player owner, object combatIdentity)`，纯比较closed/owner/CombatIdentity/captured PCS；无card变更、不调用Hook/RNG，不能替代实际caller的combat lease及CostHooks安装门。旧源23966B/SHA`b371a0868c488a6e8b90b77af329f1035c1532820f172f3b54d1eeb87e02db7d`保在`cost-scope-owner-preflight-before.cs.txt`，新SHA`d323eb6d3ee0e04e14f8127680323cdab1fd29bd2c9d74ed3235c087af41a4c7`。

Main新窄`cost-scope-preflight-probe`实际Compile Link共享源与CostHooks，构建1既有nullable warning/0error；真实CLR exit0/stderr空。九行逐actual核：exact true、foreign-owner即使same PCS仍false、foreign-combat false、null owner/token false、replacementPCS false、恢复原PCS true、Dispose后false、查询不改owner PCS true。未安装费用hook、未生成卡或消耗RNG，不能把此单独证明当WhiteNoise W28完整消费者通过。原C待回源先调用位置、完整noRNG正反及独审；历史费用/Nightmare/其它载体embedded收据保持旧代次，不以新current source倒签。

## 2026-11-15 — 独立一代事件切分

六个一代事件及 JAX、MutagenicStrength 迁至独立 `../sts2-spire1-events` 工程；旧事件实现、事件组开关及对应本地化从 Spire1 生产树移除，旧素材与历史记录另行保留。旧文档中关于事件由本工程提供的叙述是历史记录，不代表此后的生产程序集。

新工程保留旧事件和奖励 ModelId，仅依赖 BaseLib 3.4.8；单独的下一局事件和结局设置由新工程管理。当前切分尚待独立审阅、0.113 构建、私有素材导入、PCK 路径与实际玩法验证；本条不表示已部署或实机通过。
