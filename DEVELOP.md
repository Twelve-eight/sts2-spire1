# DEVELOP.md — sts2-spire1 (Slay the Spire 1 ↔ Slay the Spire 2 interop)

> Vision (**PIVOTED** session 14, 2026-08-23 — supersedes the self-contained-sandbox vision): Spire1 is the **complementary character/card/relic layer** for vanilla StS1 content on StS2 (v0.111.x public-beta, Godot/C#/.NET9) via **BaseLib**. Bring vanilla StS1 **characters, cards, relics, powers, potions, events** into StS2, designed to run **inside the community act stack** — Acts from the Past (acts 1–3), Act 4 Heart (The Ending), Act Toggler, MP Rebalance — instead of maintaining our own dungeon presentation. Our own CustomActModel dungeon (former M2/M3) stays in-tree as **fallback only**, not a polish target. Any mix of StS1/StS2 characters, solo or co-op; all mod content runtime-toggleable.
>
> Authoritative design + contracts. Chronological log → `DEVLOG.md`. Shared conventions → `../AGENTS.md`. Deep API → research artifacts (§10).

## 0. Status (conclusion-first)
- **DIRECTION PIVOT (session 14, user decision): complementary layer.** The ecosystem covers the dungeon stack better than we can present it: AFTP (acts 1–3, real StS1 art/music/animation, 479 ratings), Act 4 Heart (The Ending, three-key gate, MP-compatible), Darkglade's Act Toggler (main+beta), Kziz3988's MP Rebalance. All four subscribed and downloaded locally. We stop investing in our acts' presentation; effort concentrates on the class/card/relic layer + interop correctness. Analysis: `DEVLOG.md` §13/§13.1.
- **Unlocked by AFTP decompilation (session 13)**: `N'loth's Gift` viable via Harmony on `CardRarityOdds.RollWithoutChangingFutureOdds`; `FaceTrader` unblocked (implement the five face relics); `Madness` has a reference impl. See §9.
- Target: **StS2 v0.111.0** at `G:\steam\steamapps\common\Slay the Spire 2`. Framework **BaseLib**, and BaseLib is the mod's **only** dependency (NuGet `Alchyr.Sts2.BaseLib`; build-time-only helpers `Alchyr.Sts2.ModAnalyzers`, `Krafs.Publicizer`, `BSchneppe.StS2.PckPacker`; templates `Alchyr.Sts2.Templates`).
- **We ship ecosystem-compat patches (session 15, user directive — AFTP is frozen):** anything our verification/interop infra needs from third-party mods goes INTO Spire1's release dll, not upstream. Current carrier: `AutoSlayModdedScreenHandlersPatch` (teaches engine AutoSlay to drive AFTP minigame overlays; gated on `--autoslay` so normal play is untouched). Upstream asks we still filed: AFTP issue #10 (API-stability + ProceedButton gating); MegaCrit draft at `.tmp/issues/megacrit-autoslay-extensibility.md`; SpeedX contact is a USER action item.
- **Toolchain PROVEN**: .NET 9 SDK 9.0.317 + Godot.NET.Sdk/4.5.1 + BaseLib restore + `dotnet build` → copies dll+json into `mods/<Mod>/`. Caches on G (C: <1GB free). Scaffolded `mod/` (`dotnet new alchyrsts2charmod --name Spire1`), id/prefix `Spire1`/`SPIRE1-`. Build green except STS001 (needs complete localization).
- Now: **M4 content is essentially complete. M2 monsters is the next milestone and is NO LONGER BLOCKED** (session 5 recon — see `DEVLOG.md` §5.1). All four characters, 305 card classes, 33 relics, 49 powers and 53 event classes build 0 errors and are deployed; every event branch that was blocked on a missing relic or card is wired.
- **Correction to earlier sessions: custom monsters were never blocked on a visuals decision.** BaseLib ships `CustomMonsterModel`/`CustomEncounterModel`/`CustomActModel`/`CustomOrbModel`/`CustomPetModel` and a real non-placeholder `CustomCharacterModel`, all confirmed present in the **shipped v3.3.5 binary**. Two cheap visual routes: point `CustomMonsterModel.CustomVisualPath` at one of the **121 shipped StS2 monster scenes** (the same trick `PlaceholderCharacterModel.cs:12` already uses for our characters, since `SceneHelper.GetScenePath` resolves against the base game's `res://`), or build an `NCreatureVisuals` from a single `Texture2D` via `NCreatureVisualsFactory`. So `Colosseum`, `MaskedBandits`, `MysteriousSphere`, `Mushrooms [Stomp]`, `DeadAdventurer`'s elite, `MindBloom [I am War]` and `SpireHeart` are all implementable now, with encounter tables already extracted in §7d.
- Remaining true gaps: the five StS1 face relics plus `Madness` (**data now fully extracted and validated** in `research/sts1data/face-relics-and-madness.json`, zero blockers, five bytecode-verified corrections recorded in `DEVLOG.md` §5.2); `NlothsGift` (**may be implementable after all** — `CardRarityOdds.Roll` is a public patchable instance method, verdict in `research/BaseLib-unused-surface.md`, `DEVLOG.md` §5.4); and `Girya`'s rest-site option, which BaseLib itself flags incomplete.
- **Relic art needs no second library.** `RelicModel.IconBaseName`, `PackedIconPath`, `PackedIconOutlinePath` and `BigIconPath` are all `protected virtual`/`public virtual` (`.tmp/dllsrc/.../RelicModel.cs:128-140`), so a relic can borrow a shipped StS2 relic's atlas entry by overriding `IconBaseName`, or point at art in our own `Spire1.pck`. This is the same donor trick the characters and monsters use. The earlier note that this needed RitsuLib's `ExternalAssetOverrideRegistry` is withdrawn.
- **Single dependency, deliberately.** `mods/BaseLib/BaseLib.json` declares `dependencies: []`, and our `Spire1.json` declares exactly `[{"id": "BaseLib", "min_version": "3.4.5"}]`. RitsuLib and JmcModLib were both surveyed in full (`docs/`) and **rejected** — a second runtime dependency costs every player an extra install for benefits we can reach through BaseLib alone. Earlier text in this file claiming "runtime deps BaseLib+RitsuLib" was wrong.
- **Version skew RESOLVED (session 6).** We compile against NuGet `Alchyr.Sts2.BaseLib` **3.4.5** and the game now loads **3.4.5** as well; the three runtime files were verified byte-identical (md5) to the NuGet package's `Content/`+`lib/net9.0/` payload, and 3.3.5 is retained at `mods/BaseLib-3.3.5-backup/`. Before this, compile-time 3.4.5 against runtime 3.3.5 meant any source-only API compiled cleanly and would have thrown at load. The whole 3.4.5 surface is now legal; `docs/BaseLib-API.md` §9's skew table is history, not a constraint.

## 1. Milestones
| M | Content | Verify |
|---|---|---|
| M0 pipeline (DONE) | toolchain + scaffold + build → dll in mods | build exit 0 |
| **M1 Ironclad slice** | `Spire1Ironclad` "StS1 - Ironclad" (80 HP, 3 energy, deck 5 Strike/4 Defend/1 Bash, Burning Blood) + loc + red pool + global content toggle; then full Ironclad card pool + relics | in select; run starts; cards playable |
| M2 monsters (**DONE — fallback**) | StS1 monsters+encounters for ALL acts incl. The Ending landed (session 12); kept compiling in-tree, NOT a presentation target | encounters spawn; 0 load errors |
| ~~M3 own-dungeon selector~~ (**SUPERSEDED**) | replaced by the ecosystem act stack; own acts/dungeon-selector code retained as fallback only | n/a |
| M4 (**DONE**) | 4 characters, 305 cards, 33 relics, 49 powers, potions, 53 events — deployed | build 0 errors; in-game smoke |
| **P1 interop verification** | dual-install smoke: Spire1 + AFTP + Act 4 Heart + Toggler (±MP Rebalance); shared-shrine cross-pollution; Harmony patch-collision audit; AutoSlay run in mixed stack | `--autoslay` exit 0 with stack enabled; no duplicated/mispooled events |
| **P2 gap closure** | N'loth's Gift (`RollWithoutChangingFutureOdds` prefix); FaceTrader + five face relics (`EventRelicPool`, uniform roll over unowned); Madness | each verified in-game |
| **P3 layer UX** | character-select visibility/gating polish vs ecosystem stack; decide if a light dungeon-picker UX atop ecosystem acts is worth building | smoke |

**VANILLA ONLY (hard):** unmodded StS1 as shipped by MegaCrit. Exclude all StS1 mods. Source = `desktop-1.0.jar` + fandom/wiki.gg vanilla; exact numbers in `agent://Sts1DataScout` (confirmed 100% vanilla: 75 red cards, 38 colorless, 5 status, 14 curses, temp/option cards; Act-1 monsters/encounters). Never invent unconfirmed values.

## 2. Core feature design
### 2a. Characters (additive, labeled)
- One class per StS1 character (`Spire1Ironclad`, later `Spire1Silent/Defect/Watcher`), NEW characters shown beside StS2's roster. Do NOT replace/hide StS2 characters. Display name **"StS1 - <Character>"** (eng) / **"一代-<角色>"** (zhs), e.g. `StS1 - Ironclad` / `一代-铁甲战士`.
- Characters are decoupled from dungeon: any StS1/StS2 character can enter any dungeon (M3).

### 2b. Dungeon/act-set selection at character select (M3) — **SUPERSEDED by ecosystem (fallback only)**
- Primary path: players choose StS1 acts via the ecosystem stack (AFTP pools / Act Toggler config); Spire1 contributes characters/cards/relics usable in ANY dungeon. Own-act selection below is kept for fallback activation only.
- Implementation: StS1 acts as `CustomActModel`s; a char-select control (BaseLib `CustomCharacterSelectEntry` and/or Harmony patch on the select/run-setup screen) selects the act sequence; `RunState`/act-progression patched to run 4 acts when StS1 dungeon chosen. [research act-sequence + co-op run setup via sts2.xml/BaseLib before M3]
- Co-op ("组队"): StS2 supports multiplayer; characters (mixed StS1/StS2) join a run into the selected dungeon. [verify multiplayer run-setup hooks]

### 2c. Runtime content gating (settings)
`Spire1Config : SimpleModConfig` (Settings→Mod Settings), all default ON:
| Toggle | Gates | Mechanism |
|---|---|---|
| `EnableSts1Content` (master) | all | short-circuit |
| `EnableSts1Characters` | StS1 chars in select | character visibility |
| `EnableSts1Cards` | StS1 colorless cards in shared pool | shared-card-pool filter |
| `EnableSts1Relics` | StS1 relics in shared pools | shared-relic/potion-pool filter |
| `EnableSts1Dungeon` | StS1 dungeon option + StS1 encounters | act-select option + `IsValidForAct` |
- Read at run/act/pool generation (StS2 granularity) → applies next run/act, not mid-combat. Lets a run use none of the mod's content while installed.

## 3. Architecture (BaseLib content model)
- **Registration automatic** (ctors + `[Pool]`); `Initialize()` only: Harmony patch-all, `ModConfigRegistry.Register(ModId, new Spire1Config())`, extra loc tables, character sort order. IDs = `SPIRE1-<CLASS>`.
- **Cards**: `Spire1Card : CustomCardModel(cost, CardType, CardRarity, TargetType)` (`[Pool(Spire1CardPool)]`); override `CanonicalVars` (DamageVar/BlockVar/PowerVar<T>/calculated), `CanonicalKeywords`, `CanonicalTags`, `async OnPlay` (`CommonActions.CardAttack/CardBlock/Apply<T>/Draw`), `OnUpgrade`.
- **Character**: `Spire1Ironclad : PlaceholderCharacterModel` (`PlaceholderID="ironclad"`). Override `StartingHp`, `StartingDeck`, `StartingRelics`, `CardPool/RelicPool/PotionPool`, `NameColor`. Energy default 3.
- **Relics**: `Spire1Relic : CustomRelicModel` (`[Pool(Spire1RelicPool)]`).
- **Powers**: `CustomPowerModel`, `Type` (Buff/Debuff), `StackType`. No pool. (StS2 power class names via sts2.xml.)
- **Monsters**: `CustomMonsterModel` + `GenerateMoveStateMachine()` via `MoveBuilder`/`MonsterActions`.
- **Encounters**: `CustomEncounterModel(RoomType)`, `IsValidForAct(act)` gate (config-aware), `GenerateMonsters()`, `AllPossibleMonsters`; `IsWeak` for early pool.
- **Acts**: `CustomActModel(actNumber)` — StS1 acts (M3).
- **Localization**: JSON `Spire1/localization/{eng,zhs}/<table>.json`, keys `SPIRE1-<NAME>.<entry>`. Fmt `!D!`/`!B!`/`*gold*`. Char names carry the "StS1 -"/"一代-" prefix.
- **Card color**: `Spire1CardPool.H/S/V` over `card_frame_red`.

## 4. Repo layout
```
G:/omp works/sts2-spire1/
├─ DEVELOP.md  DEVLOG.md  NuGet.config
├─ mod/    Spire1.csproj  Spire1.json  Directory.Build.props  Sts2PathDiscovery.props  project.godot  export_presets.cfg
│  ├─ Spire1Code/ (Character/ Cards/ Relics/ Powers/ Potions/ Monsters/ Encounters/ Acts/ Config/ Extensions/ MainFile.cs)
│  └─ Spire1/     (images/  localization/{eng,zhs}/*.json)
├─ research/  (BaseLib-StS2, ModTemplate-StS2)  — reference only
└─ .nuget/ .tools/ .dotnethome/ .tmp/  (G-local caches, gitignored)
```

## 5. Contracts (STABLE — all workers)
- Namespaces: root `Spire1`; code under `Spire1.Spire1Code.{Cards,Relics,Powers,Potions,Character,Monsters,Encounters,Acts,Config}`. Class name = identity (drives ID + loc key), StS1-descriptive PascalCase.
- Every card extends `Spire1Card`; every relic `Spire1Relic`; pools exist; `[Pool]` inherited; no manual registration.
- Every content class MUST add its localization entry to matching `localization/eng/*.json` (STS001 fails build). Card: `SPIRE1-<CLASS>` → `{ "title": "...", "description": "Deal !D! damage." }`.
- Effects use commands only (`CommonActions`/`*Cmd`); never mutate state directly.
- VANILLA StS1 numbers only (from `agent://Sts1DataScout`); flag unconfirmed; never invent.
- No writes to C:. No non-{zh,en,fr,de,ru} text anywhere (use eng + optional zhs).

## 6. Parallel execution (workers + reviewers)
After M1 proves the pipeline, fan out independent slices. **Each code-writing subagent is paired with a `reviewer` subagent** (user directive) reviewing its code before I integrate/build. Workers WRITE code only, SKIP build/lint (main builds centrally to avoid mid-flight breakage). **Cost**: prefer the cheapest suitable agent type — `sonic` for strictly mechanical slices (localization JSON, stat-only cards), `task` for logic-heavy cards/monsters; the underlying model isn't hand-selectable via tooling, so agent-type choice is the cost lever. Slices: Ironclad cards by rarity (Basic+Common / Uncommon / Rare / colorless); relics by tier; Act-1 monsters+encounters; acts+dungeon selector (M3). Shared powers (Vulnerable/Weak/Strength wrappers) + base classes defined ONCE by main pre-fan-out. Contract = §2/§5 + `agent://BaseLibApiScout` + `agent://Sts1DataScout`.

## 7. Findings / decisions
- StS2 already contains `StrikeIronclad`/`DefendIronclad`/`BurningBlood` (build resolved, no CS0246) — reuse `ModelDb.Relic<BurningBlood>()` for starter relic; implement vanilla-faithful custom cards for full control.
- `PlaceholderCharacterModel(PlaceholderID="ironclad")` → working visuals, no art for M1.
- Content gating + dungeon choice: monster set follows the selected dungeon; global toggles in settings for full enable/disable.

### 7a. LEAN-CODE RULE (user directive, overrides earlier "implement everything ourselves")
- If a card's mechanic AND numbers are identical between StS1 and the shipped StS2 card of the same name, DO NOT define our own class. Make the character able to obtain the SHIPPED card instead.
- Only write our own class when a field really differs (cost, base value, upgrade delta, keyword, target, rarity or behaviour). State the differing field in the class doc comment.
- Same rule for events: port an StS1 "?" event only if StS2 lacks it; a same-name-but-different event keeps the `StS1 - ` label.
- Measured baseline (jar bytecode vs decompiled StS2, `research/sts1data/`): of 97 same-name cards, 76 match numerically. Traps that match numerically but differ mechanically and therefore still need our own class: `Expertise` (StS1 draws up to 6 in hand, StS2 var is 2), `Claw` (StS1 +2 on upgrade, StS2 +1), `BiasedCognition` (StS1 4 Focus, StS2 5), `CalculatedGamble` (StS2 adds Retain), `Equilibrium`/`MachineLearning` (different keywords), `Chill`/`Darkness`/`Defragment`/`Fusion`/`GeneticAlgorithm`/`Glacier`/`HelloWorld` (verified differing by the Defect writers).

### 7b. Watcher subsystems are FEASIBLE (2026-08-19 probes, supersedes the earlier "stances absent" flag)
- Stances: `AbstractModel.ModifyDamageMultiplicative` exists and is dispatched for every hook listener (`Hook.ModifyDamageInternal`). Shipped proof: `DoubleDamagePower` returns 2m on the dealing side, `ColossusPower` returns 0.5m on the receiving side. So Wrath (deal x2 / receive x2), Divinity (deal x3) are implementable as `CustomPowerModel`s.
- Calm exit bonus: `PowerModel.AfterRemoved(Creature oldOwner)` + `PlayerCmd.GainEnergy` → 2 Energy on leaving Calm. Divinity self-expiry: `AfterSideTurnStart`/`AfterPlayerTurnStart` + `PowerCmd.Remove(this)`.
- Stance-change observers (Mental Fortress, Rushdown, Flurry of Blows): `Creature.PowerApplied` / `Creature.PowerRemoved` events, or the power's own `AfterApplied`/`AfterRemoved`.
- Scry: **BaseLib already ships it** — `BaseLib.Commands.ScryCmd.Execute(PlayerChoiceContext, Player, int)`, `ScryVar` for the displayed number, and `IModifyScryAmount` / `IAfterScryed` hooks (Nirvana, Weave). Do not write a custom scry.
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
  - **Special/conditional (from `EventHelper.getEvent`)**: Accursed Blacksmith, Bonfire Elementals, Fountain of Cleansing, Designer, Duplicator, Lab, FaceTrader, NoteForYourself, WeMeetAgain, The Woman in Blue, N'loth, Knowing Skull, The Joust, The Mausoleum — these are gated by run conditions, not by a plain act list.
  - Only two events read floor/act directly: `MindBloom` (`floorNum` vs 50) and every A15+ variant (`ascensionLevel` vs 15). Everything else is gated purely by pool membership.
- StS2 equivalent gating: override `IsAllowed(IRunState)` on the event and read `runState.CurrentActIndex` / `runState.TotalFloor` (shipped precedents: `BrainLeech.cs:37-40` uses `< 2`, `DollRoom.cs:79-82` uses `== 1`, `PunchOff.cs:42-45` uses `TotalFloor >= 6`). Act-scoped custom events set `CustomEventModel.Acts`; leaving `Acts` empty registers a SHARED event (that is the correct home for the six shrines).
- Uniqueness is automatic: `RoomSet.EnsureNextEventIsValid` skips events whose `ModelId` is already in `runState.VisitedEventIds`, so no one-time flag is needed.
- StS2 ships NO event with an StS1 name (its events are the Overgrowth/Hive/Glory/Underdocks sets), so every StS1 event is a genuine addition; none should be skipped as a duplicate.

### 7d. StS1 Act encounter tables (authoritative, for M2 monsters)
- **Act 1 weak**: Cultist, Jaw Worm, 2 Louse, Small Slimes. **Act 1 strong**: Blue Slaver, Gremlin Gang, Looter, Large Slime, Lots of Slimes, Exordium Thugs, Exordium Wildlife, Red Slaver, 3 Louse, 2 Fungi Beasts. **Act 1 elites**: Gremlin Nob, Lagavulin, 3 Sentries. **Act 1 bosses**: The Guardian, Hexaghost, Slime Boss.
- **Act 2 weak**: Spheric Guardian, Chosen, Shell Parasite, 3 Byrds, 2 Thieves. **Act 2 strong**: Chosen and Byrds, Sentry and Sphere, Snake Plant, Snecko, Centurion and Healer, Cultist and Chosen, 3 Cultists, Shelled Parasite and Fungi. **Act 2 elites**: Gremlin Leader, Slavers, Book of Stabbing. **Act 2 bosses**: Automaton, Collector, Champ.
- **Act 3 weak**: 3 Darklings, Orb Walker, 3 Shapes. **Act 3 strong**: Spire Growth, Transient, 4 Shapes, Maw, Sphere and 2 Shapes, Jaw Worm Horde, 3 Darklings, Writhing Mass. **Act 3 elites**: Giant Head, Nemesis, Reptomancer. **Act 3 bosses**: Awakened One, Time Eater, Donu and Deca.
- Weak encounters are the first floors of an act, then strong ones; each act also has an exclusion list preventing an immediate repeat (captured in the extraction output). Encounter gating in StS2 is `CustomEncounterModel.IsValidForAct(act)` plus `IsWeak` for the early pool.

## 8. Verification
Clean build; smoke-test IN-GAME (character loads, run starts, cards resolve, dungeon selection works, encounters spawn, toggles work). Deliverable = playable mod. Record smoke runs in `DEVLOG.md`.

## 9. Open gaps
Most of the original list is now closed — resolved in session 5 and documented in `docs/`. Resolve anything new via `docs/BaseLib-API.md` first, then `sts2.xml` grep, then the decompiled tree at `.tmp/dllsrc/`.
- **CLOSED**: `CardKeyword` members; power class names; command builders (`DamageCmd`/`PowerCmd`/`CreatureCmd`/`CardPileCmd`); per-character energy override; `MonsterModel` stat/name/art API + move-state selection (see `docs/BaseLib-API.md` §2 `CustomMonsterModel` and `research/BaseLib-unused-surface.md` §2).
- **RESOLVED BY PIVOT (session 14)**: M3 dungeon-selector hook question is moot for the primary path (ecosystem supplies acts); revisit only if the fallback activates. Character-select visibility + shared-pool filter hooks stay relevant to the layer (P3).
- **UNBLOCKED by AFTP decompilation (session 13, verify against shipped v1.0.5 dll before writing)**: `N'loth's Gift` — Prefix on `CardRarityOdds.RollWithoutChangingFutureOdds(CardRarityOddsType, ref float offset)` rewriting `offset = baseRareOdds*3 - baseRareOdds` when owned (no pity-state mutation; optional Dup-transpiler captures the roll for Flash). `FaceTrader` — implement `CultistHeadpiece`/`FaceOfCleric`/`GremlinVisage`/`NlothsHungryFace`/`SsserpentHead` as `CustomRelicModel`s pooled in `EventRelicPool`, event rolls uniformly over unowned faces. `Madness` — AFTP `Cards.Madness` is the working reference.
- `Girya`'s rest-site option — STILL OPEN; BaseLib itself flags incomplete.

## 10. References
**Library interface docs (`docs/`) — read these before writing code against a library:**
- `docs/BaseLib-API.md` — the framework we build on. Content base classes, hooks, localization, visuals, patches, and the v3.4.5-source vs v3.3.5-shipped availability table.
- `docs/RitsuLib-API.md` — third-party framework, 1325 public types, MIT. 92-namespace index plus exact signatures for content registration, non-Spine animation, free-play, act-enter forcing, lobby staging, asset overrides.
- `docs/JmcModLib-API.md` — third-party utility library (settings UI, reflection, logging, secrets, persistence, compat shims). No content abstractions.

**Research verdicts (`research/`) — what to adopt and why:**
- `research/BaseLib-unused-surface.md` — capabilities we are not using, with the `N'loth's Gift` and `Girya` verdicts.
- `research/RitsuLib-api.md` — per-gap adopt/skip analysis, per-consumer usage, dependency risk.
- `research/JmcModLib-api.md` — SKIP, with the measurement that proves it.
- `research/sts1data/` — extracted vanilla StS1 data (cards, relics, events, `face-relics-and-madness.json`).
- **AFTP-family reference binaries (NOT in repo)**: `G:\steam\steamapps\workshop\content\2868840\{3746969593,3747537811,3785039319,3787796638}` — decompile with `%USERPROFILE%\.dotnet\tools\ilspycmd.exe`; AFTP source at github.com/Cany0udance/ActsFromThePast (reuse permitted).

**Sources:** `research/BaseLib-StS2/` (BaseLib source, tag v3.4.5), `research/ModTemplate-StS2/`, `.tmp/dllsrc/` (decompiled StS2 engine), `sts2.xml` (game API doc, `data_sts2_windows_x86_64/sts2.xml`), `.tmp/ritsu/` + `.tmp/jmc/` (library dumps and source). Wiki: alchyr.github.io/BaseLib-Wiki. `agent://Sts1DataScout` — StS1 Ironclad + Act-1 vanilla data.
