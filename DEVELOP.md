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
- **Single dependency, deliberately.** `mods/BaseLib/BaseLib.json` declares `dependencies: []`, and our `Spire1.json` declares exactly `[{"id": "BaseLib", "min_version": "3.4.5"}]`. RitsuLib and JmcModLib were both surveyed in full (`docs/`) and **rejected** - a second runtime dependency costs every player an extra install for benefits we can reach through BaseLib alone. Earlier text in this file claiming "runtime deps BaseLib+RitsuLib" was wrong.
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
Most of the original list is now closed - resolved in session 5 and documented in `docs/`. Resolve anything new via `docs/BaseLib-API.md` first, then `sts2.xml` grep, then the decompiled tree at `.tmp/dllsrc/`.
- **CLOSED**: `CardKeyword` members; power class names; command builders (`DamageCmd`/`PowerCmd`/`CreatureCmd`/`CardPileCmd`); per-character energy override; `MonsterModel` stat/name/art API + move-state selection (see `docs/BaseLib-API.md` Sec 2 `CustomMonsterModel` and `research/BaseLib-unused-surface.md` Sec 2).
- **RESOLVED BY PIVOT (session 14)**: M3 dungeon-selector hook question is moot for the primary path (ecosystem supplies acts); revisit only if the fallback activates. Character-select visibility + shared-pool filter hooks stay relevant to the layer (P3).
- **UNBLOCKED by AFTP decompilation (session 13, verify against shipped v1.0.5 dll before writing)**: `N'loth's Gift` - Prefix on `CardRarityOdds.RollWithoutChangingFutureOdds(CardRarityOddsType, ref float offset)` rewriting `offset = baseRareOdds*3 - baseRareOdds` when owned (no pity-state mutation; optional Dup-transpiler captures the roll for Flash). `FaceTrader` - implement `CultistHeadpiece`/`FaceOfCleric`/`GremlinVisage`/`NlothsHungryFace`/`SsserpentHead` as `CustomRelicModel`s pooled in `EventRelicPool`, event rolls uniformly over unowned faces. `Madness` - AFTP `Cards.Madness` is the working reference.
- **Known deviations & dispositions (R-wave 2026-09-06, from docs/CODE-REVIEW-20260904.md)**:
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
**Library interface docs (`docs/`) - read these before writing code against a library:**
- `docs/BaseLib-API.md` - the framework we build on. Content base classes, hooks, localization, visuals, patches, and the v3.4.5-source vs v3.3.5-shipped availability table.
- `docs/RitsuLib-API.md` - third-party framework, 1325 public types, MIT. 92-namespace index plus exact signatures for content registration, non-Spine animation, free-play, act-enter forcing, lobby staging, asset overrides.
- `docs/JmcModLib-API.md` - third-party utility library (settings UI, reflection, logging, secrets, persistence, compat shims). No content abstractions.

**Research verdicts (`research/`) - what to adopt and why:**
- `research/BaseLib-unused-surface.md` - capabilities we are not using, with the `N'loth's Gift` and `Girya` verdicts.
- `research/RitsuLib-api.md` - per-gap adopt/skip analysis, per-consumer usage, dependency risk.
- `research/JmcModLib-api.md` - SKIP, with the measurement that proves it.
- `research/sts1data/` - extracted vanilla StS1 data (cards, relics, events, `face-relics-and-madness.json`).
- **AFTP-family reference binaries (NOT in repo)**: `G:\steam\steamapps\workshop\content\2868840\{3746969593,3747537811,3785039319,3787796638}` - decompile with `%USERPROFILE%\.dotnet\tools\ilspycmd.exe`; AFTP source at github.com/Cany0udance/ActsFromThePast (reuse permitted).

**Sources:** `research/BaseLib-StS2/` (BaseLib source, tag v3.4.5), `research/ModTemplate-StS2/`, `.tmp/dllsrc/` (decompiled StS2 engine), `sts2.xml` (game API doc, `data_sts2_windows_x86_64/sts2.xml`), `.tmp/ritsu/` + `.tmp/jmc/` (library dumps and source). Wiki: alchyr.github.io/BaseLib-Wiki. `agent://Sts1DataScout` - StS1 Ironclad + Act-1 vanilla data.

## 2026-09-23 事件移除与默认注入新契约

用户本轮要求, 原版字节码证据与隔离失败复现见 G:\omp works\Sts\sts2-spire1\docs\DEVELOP-events-20260923.md. 本段不覆盖已有 staged/unstaged 设计记录. 事件默认关闭, 永恒卡牌遵循二代 IsRemovable, 不操作共享配置或 Steam 安装.


## 2026-09-28 姿态形态可玩化

本轮当前契约见 docs/DEVELOP-form-playable-20260928.md. 六形态规格沿用 2026-09-26 用户确认值; 默认构建接入, 每局自定义模式入口与 Watcher 可选反射桥接按新契约实现. 旧摘要中的缺研究文件和缺姿态基类判断已由本轮文件检查纠正. 不发布 Workshop, 不写 Steam 或共享配置, 不操作前台窗口.

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

证据与边界见 `docs/DEVELOP-form-playable-20260928.md` 及 `docs/reports/form-playable-20260928/`。本结论仍不覆盖可见 UI、视觉、长战斗、存档重载、重连、多人同步、性能和完整平衡。

## 2026-10-03 可选 Mod 交叉挂载契约收紧

- Spire1 的 `Spire1.json` 只声明 `BaseLib`; Watcher、AutoAnthony、AutoAnthonyWatcher、DirectConnectIP、ActsFromThePast 不得出现在发布 DLL 的 mod AssemblyRef 或 manifest 前置项中。
- 可选能力只允许通过运行时反射和程序集加载观察接入。缺少可选程序集时,Spire1 必须继续完成自身 initializer,并以 disabled/pending/fail-closed 记录能力状态;不得因可选程序集缺失导致 `FileNotFoundException`、`TypeLoadException` 或 ModLoader 级硬前置失败。
- AssemblyLoad 事件是晚加载的廉价唤醒源。只有 core/partial/registration/unpatch 状态确实需要没有新 AssemblyLoad 也能推进时,才创建周期 Timer 或 fallback thread。Godot/进程退出期间所有通知必须静默退出,不得访问已释放原生对象。
- 真实部分 Mod 交叉启动矩阵以 `docs/reports/form-playable-20260928/partial-mod-launch-matrix-central-run-r29-20261003.md` 为准;报告中的源码推理、隔离启动日志和实机行为必须分开表述。
## 2026-10-03 交叉挂载后的退出生命周期契约

- `AssemblyLoad` 订阅必须与 `ProcessExit` 共享串行 gate。入口快速检查不能替代 gate 内二次检查,否则 in-flight unsettled Apply 可能在 shutdown 标记后重新订阅。
- Retry timer/fallback thread 的失败 bookkeeping 不得在 AssemblyLoad gate 内调用会锁 RetryGate 的路径,避免 shutdown 时锁反转。所有退出期间回调必须 fail-closed。
- 最终 Release 与 r30 隔离矩阵证据以 `docs/reports/form-playable-20260928/partial-mod-launch-matrix-central-run-r30-20261003.md` 为准;此前 r29 是修复前的基线,不再作为最终产物证据。