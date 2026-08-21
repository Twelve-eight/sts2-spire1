# DEVLOG — sts2-spire1

Recovery anchor + working state. Design/contracts: `DEVELOP.md`. Shared conventions: `../AGENTS.md`. Project skill: `sts2-spire1`. Resumable with zero prior chat.

## GOAL
Cross-game sandbox: play vanilla **StS1** content on the **StS2** engine (v0.111.0, Godot/C#/.NET9) via **BaseLib**. Additive; cards/content that also exist in StS2 vanilla are labelled **"StS1 - X"** (eng) / "一代-X" (zh) as distinct entries. Runtime-toggleable; VANILLA fidelity. Never write C:. Only zh/en/fr/de/ru text.

## STATUS (2026-08-18, session 1)
- **BUILD GREEN: Ironclad character + 89 cards + 8 powers + 19 relics + 8 potions + content-config** → `Spire1.{dll,pck,json}` into `Slay the Spire 2/mods/Spire1/`.
- 89 cards = ~73 Ironclad + 8 status + 8 colorless + 8 curses. 19 relics (BurningBlood starter wired into character + commons/uncommon/rare/boss/shop). 8 potions (Spire1Potion pool). All reviewed by paired v4f reviewers (faithful; loc nits fixed).
- **StS1 - prefix**: 55 cards + all colliding relics/potions get "StS1 - " title (class-name collision with StS2 vanilla + Strike/Defend). StS1-unique keep plain names.
- **CONTENT LAYER COMPLETE.** Remaining = MONSTERS (M2) + DUNGEON (M3), both new subsystems.
- NOT in-game smoke-tested (DRM blocks direct launch; needs user visual — see LIMITATIONS).
- Delegation model: heavy v4f `task` workers (opencode) research APIs themselves (typedump + publicized game-source at `.tmp/dllsrc/`) + paired v4f reviewers, parallel. Coordinator (me) merges loc + central builds + routes fixes. **WINNING FORMULA: precise inline specs + minimal reads (2-9min success); heavy self-research+many-files in one worker → budget-fails before writing (reuse its history or split).**

## ENV / BUILD
Game `G:\steam\steamapps\common\Slay the Spire 2`. Project `G:/omp works/sts2-spire1/mod`. Export (C: full → caches on G):
`export NUGET_PACKAGES=".../.nuget/packages" DOTNET_CLI_HOME=".../.dotnethome" TEMP=".../.tmp" TMP=".../.tmp"` then `dotnet build Spire1.csproj -c Debug /p:Sts2Path="G:/steam/steamapps/common/Slay the Spire 2"`. PckPacker builds .pck. BaseLib enabled in `mods/`.
Loc pipeline: workers write `mod/_staging/*.json` (flat keys) → I JS/Bun-merge into `Spire1/localization/eng/cards.json` (NEVER jq) → build → `rm _staging/*`.

## API RESEARCH TOOLS
- typedump: `cd research/typedump && dotnet run -c Release -- "<sts2.dll>" [--members] <Filter>`. `--sigs` NOT supported.
- **Publicized assemblies** (exact signatures): `mod/.godot/mono/temp/obj/Debug/PublicizedAssemblies/{sts2,BaseLib}.dll`. Also `research/sigdump` (dnlib dumper).
- Ground against the game's OWN card/power/relic of the same name (decompile). ilspycmd BROKEN.

## API CHEATSHEET (compile-validated)
Card `mod/Spire1Code/Cards/X.cs`, ns `Spire1.Spire1Code.Cards`: `public class X() : Spire1Card(cost, CardType.Attack|Skill|Power|Status|Curse, CardRarity.Basic|Common|Uncommon|Rare|Status|Curse, TargetType.AnyEnemy|AllEnemies|RandomEnemy|None)`
- Vars: `new DamageVar(n,ValueProp.Move)`, `BlockVar`, `CardsVar(n)`, `HealVar(n)`, `EnergyVar(int)`, `MaxHpVar(n)`, `PowerVar<TPower>(n)`. Scaling: `..CustomCardModel.MakeCalculatedDamage(base,(card,target)=>bonus)` (CardAttack auto-uses). X-cost: `protected override bool HasEnergyCostX => true;` + `ResolveEnergyXValue()`.
- Keywords **PUBLIC**: `public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];` (.Ethereal/.Innate/.Unplayable/.Eternal). Upgrade-add: `AddKeyword(CardKeyword.Innate)`.
- OnPlay: attack `await CommonActions.CardAttack(this, play).Execute(choiceContext);` (multi-hit hitCount:n; AoE/random via TargetType); block `CardBlock(this, DynamicVars.Block, play)`; draw `CommonActions.Draw(this, choiceContext)`; power self `ApplySelf<T>(choiceContext, this)`, target `Apply<T>(choiceContext, play.Target!, this)`, AoE `Apply<T>(choiceContext, this, play)`; energy `await PlayerCmd.GainEnergy(N, Owner)`; heal `await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue)`; **lose HP** `await CreatureCmd.Damage(choiceContext, Owner.Creature, amt, ValueProp.Unblockable|ValueProp.Unpowered, this, play)`; maxHP `CreatureCmd.GainMaxHp(Owner.Creature, n)`.
- OnUpgrade: `DynamicVars.Damage/Block/Cards/Energy.UpgradeValueBy(n)`; `DynamicVars.Power<T>().UpgradeValueBy(n)` (`using BaseLib.Extensions`); cost `EnergyCost.UpgradeBy(-1)`.
- Instance type = `card.Type` (NOT .CardType; enum in ctor is CardType.X). Card `Owner` is a **Player** (Owner.Creature). Pile: `PileType.Hand.GetPile(Owner).Cards`. Getters: `Owner.Creature.Block` (int), `Owner.Creature.GetPowerAmount<StrengthPower>()`, `creature.HasPower<T>()`.
- SELECT (construct prefs first): `var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt|.UpgradeSelectionPrompt, 1); var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, filterOrNull)).FirstOrDefault();`
- CardCmd.Upgrade/Exhaust/AutoPlay; `CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top)`; gen `AddToCombatAndPreview<T>` / `AddGeneratedCardToCombat`; clone `this.CreateCloneForPlayer(Owner)`. Playability (Clash) `protected override bool IsPlayable => CardPile.GetCards(Owner, PileType.Hand).All(c=>c.Type==CardType.Attack);`. Lifesteal via `AttackCommand.Results→DamageResult.UnblockedDamage/WasTargetKilled`. Intent `play.Target?.Monster?.IntendsToAttack==true`. Dynamic cost `AfterCurrentHpChanged(Creature,decimal delta)` + `EnergyCost.AddThisCombat(-1, reduceOnly:true)`.

## CUSTOM POWER (`mod/Spire1Code/Powers/XPower.cs`)
`public class XPower : CustomPowerModel { public override PowerType Type => Buff|Debuff; public override PowerStackType StackType => Counter|Single; public override List<(string,string)>? Localization => new PowerLoc("Title","#desc tokens","#smart"); }`
Hooks: `AfterSideTurnEnd(PlayerChoiceContext, CombatSide, IEnumerable<Creature>)`; `AfterSideTurnStart(CombatSide, IReadOnlyList<Creature>, ICombatState)` (NO ctx); `AfterCardDrawn(PlayerChoiceContext, CardModel, bool)`; `AfterCardExhausted(PlayerChoiceContext, CardModel, bool)`. In a power, `Owner` IS a Creature, `Amount`=stacks, `Flash()`. AoE from power: `CreatureCmd.Damage(ctx, Owner.CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner)`. Power carrying a damage value: give it a `DamageVar` + `SetDamage(decimal)` (TheBomb pattern). Temp strength: BaseLib `CustomTemporaryPowerModelWrapper<TCard,StrengthPower>`. `PowerCmd.Apply<T>(ctx, Creature target, decimal, Creature applier, CardModel, bool silent=true)`. Auto-registered (ICustomPower). Do NOT extend Spire1Power base (forces PNG icon path); extend CustomPowerModel directly (icons default null).

## LOC / SimpleLoc (BaseLib) — enabled in MainFile.Initialize via `SimpleLoc.EnableSimpleLoc(ModId)`
- CARD loc (cards.json) IS auto-simplified: `!D!`→{Damage:diff()}, `!B!`,`!C!`(Cards),`!E!`(Energy),`!H!`(Heal),`!CD!`,`!CB!`; `!MaxHp!`; **power amount `!<PowerClassName>!`** (PowerVar<T> var name = typeof(T).Name, e.g. `!StrengthPower!`). `*word*`→gold. Keyword text (Exhaust./Unplayable./Ethereal.) AUTO-appends from CanonicalKeywords — do NOT also write it in desc (status cards currently double-write it; minor).
- **MODEL loc (PowerLoc/RelicLoc from code) is NOT simplified unless the string STARTS WITH '#'.** (PowerLocPolish in flight is prefixing power descs with '#'.)

## POOLS (registration = `[Pool(typeof(TPool))]` attribute on the class; inherited)
- Colorless: `[Pool(typeof(ColorlessCardPool))]` (base `Spire1CardPool` else). Curses: base `Spire1Curse` has `[Pool(typeof(CurseCardPool))]`, CardType.Curse/CardRarity.Curse/cost -1/MaxUpgradeLevel 0. Status: CardType.Status/CardRarity.Status, Unplayable; ModelDb instantiates all mod AbstractModels so statuses/curses generate without reward-pool rolls.

## CARD INVENTORY (89 built)
~73 Ironclad (all core attacks/skills/powers incl. scaling BodySlam/HeavyBlade/Rampage, lose-HP Bloodletting/Brutality/Combust, X-cost Whirlwind, on-kill Feed, dynamic-cost BloodForBlood, custom powers Metallicize/Berserk/Combust/Brutality/Evolve/FireBreathing, Flex via temp-strength wrapper). 8 status. 8 colorless (SwiftStrike/FlashOfSteel/Finesse/GoodInstincts/Trip/Blind/BandageUp/DarkShackles). 8 curses (Regret/Decay/Doubt/Shame/Injury/Clumsy/Pain/AscendersBane).

## NEXT (resume here)
1. **M2 monsters** — pattern known, BLOCKED ON VISUALS. `X : CustomMonsterModel` (or `MonsterModel`): `public override int MinInitialHp/MaxInitialHp`, `protected override MonsterMoveStateMachine GenerateMoveStateMachine()` (build moves via BaseLib `MoveBuilder(this,"ID").Attack(dmg,hits).Block(n).ApplyToSelf<T>(n).ApplyToPlayers<T>(n,strong).HealSelf(n).FollowingState("ID").Build()`), private `async Task XMove(IReadOnlyList<Creature>)`, `GenerateAnimator(MegaSprite)`. **Blocker: a custom monster needs a spine/scene visual (`CustomVisualPath` → `res://…tscn`); the mod ships none → won't render. Decide: reuse a shipped StS2 monster scene+skin (find exact res path + animator anim names) OR commission art. Code compiles without art but visuals fail at runtime; verify only via smoke.** Refs: `.tmp/dllsrc/…/Monsters/{LouseProgenitor,DampCultist}.cs` + `Powers/RitualPower.cs`; `research/BaseLib-StS2/Monsters/MoveBuilder.cs` + `Abstracts/CustomMonsterModel.cs`. Then `CustomEncounterModel` (IsValidForAct, gated by Spire1Config).
2. **M3** StS1 4-act dungeon (`CustomActModel`) + char-select dungeon selector + character↔dungeon decoupling (research act-sequence/char-select/co-op).
3. In-game smoke (USER, visual — DRM blocks automation).
4. Polish (all unblocked, low priority): MarkOfPain add-2-Wounds-on-pickup, Girya rest-site lift, status-card double-keyword loc, per-card/relic art, Combust tooltip "{Amount} times {Damage}" wording.

## LIMITATIONS
- In-game smoke blocked: exe direct-launch fails DRM (launch via Steam). Runtime load UNVERIFIED → user visual smoke. Compile + paired-reviewer is current validation.
- Card art: placeholder for all; per-card art later.
- Display-loc nits unverifiable w/o smoke: power/relic tooltips need '#' prefix (being fixed); status cards double-write keyword text.
- StS1-prefix by exact class-name overlap with StS2 (+Strike/Defend); display-name-only collisions not exhaustively checked.

## SMOKE (session 1) — IN-GAME VERIFIED via Steam ✓
Launch via Steam (appid **2868840**; direct exe fails DRM). Logs `%APPDATA%/SlayTheSpire2/logs/godot.log` (crash detail in `sentry/*/__sentry-event`, msgpack). Rebuild needs game killed (locks Spire1.dll): `powershell -c "Stop-Process -Name SlayTheSpire2 -Force"`.
**Result: mod loads (RUNNING MODDED, 22 mods) → main menu → StS1 Ironclad selectable + playable → combat + monster-kill + WIN work.** StS2 HAS its own Ironclad ⇒ StS1-prefix confirmed necessary.
Bugs found by user smoke + FIXED (rebuilt green + reloaded):
1. **Startup crash** — 4 relic loc entries (Girya/MagicFlower/MarkOfPain/Toolbox) were NESTED objects not flat string keys → game LocException (needs `Dictionary<string,string>`). **STS build analyzer does NOT catch non-string loc values — GUARD loc merges (all values must be strings).**
2. **Combat couldn't end (P0)** — `MakeCalculatedDamage` bonus lambda MUST be static; HeavyBlade/Rampage captured instance fields → "Multiplier calc must be static!" on reward-clone → reward crash. Fix: values in card DynamicVars + `static (card,target)=>card.DynamicVars[..]`.
3. **Calc-damage cards showed raw `{Damage:diff()}`** — they use `CalculatedDamage` var; loc token must be **`!CD!`** not `!D!` (HeavyBlade/Rampage).
4. **Upgrade shows stale number (base==upgraded desc)** — upgrade-varying values MUST be DynamicVar tokens, never hardcoded/private-field: HeavyBlade `!StrMult!`(3→5), Rampage `!RampInc!`(5→8), SwordBoomerang/Pummel `_hits`→`RepeatVar`+`!Repeat!`. (Audit: exactly these 4.)
5. **Gray energy icons game-wide** — energy icon = `sprite_fonts/<pool.EnergyColorName>_energy_icon.png`; custom `charui/*.png` were gray. Fix: Spire1CardPool+Spire1PotionPool `EnergyColorName => "ironclad"` (reuse StS2 sprite).
6. **Pandora's Box / Fasten didn't transform StS1 Defend** — `CardModel.IsBasicStrikeOrDefend` checks `CardTag.Strike`/`CardTag.Defend`(+Basic). Strike had its tag; Defend lacked `CardTag.Defend` → added. (Any Basic Strike/Defend variant needs its tag.)
STILL open (visual, non-fatal placeholders; need art or StS2-asset reuse): relic icons (`Spire1/images/relics/*.png` missing→gray), custom-power atlas sprites (spire1-*_power missing→default), per-card art. HeavyBlade Strength mult ×3/×5 assumes calc damage doesn't auto-add Strength (per BodySlam=Block) — verify the number.

## LOG
- S1: research → toolchain (.NET9/Godot.NET.Sdk/PckPacker, caches→G) → scaffold → validated ALL card patterns → provider→opencode(v4f) → MASS parallel v4f delegation (workers self-research APIs via `.tmp/dllsrc` decompile + typedump) + paired reviewers + central build/fix. Delivered: 89 cards + 8 powers + status/colorless/curse pools + SimpleLoc + StS1-prefix + **19 relics (Spire1Relic/pool, BurningBlood starter wired) + 8 potions (Spire1Potion pool)**. Monster subsystem researched (MoveBuilder/CustomMonsterModel/StS1 numbers) but blocked on visual assets. Docs: DEVELOP.md, DEVLOG.md, root AGENTS.md, sts2-spire1 skill.

## ===== HANDOFF → gpt-5.6-sol (chat compressed here; RESUME FROM THIS) =====
Read `skill://sts2-spire1` FIRST (full workflow/API/gotchas), then this. Build/smoke commands + all API patterns are in the skill §2-5 and DEVLOG "API CHEATSHEET". You are the coordinator: delegate code to `task` (deepseek) workers with PRECISE inline specs + minimal reads; you merge loc + central-build + smoke via Steam. Budget: user cap $40, keep $10 reserve — spend little on yourself, push writing to deepseek.

### EXACT STATE (2026-08-19)
- **Ironclad "StS1 - Ironclad": DONE + in-game verified** (89 cards, 8 powers, 19 relics, 8 potions; loads/plays/wins; all 7 session-1 smoke bugs fixed — see SMOKE section).
- **Silent "StS1 - Silent": foundation built, SELECTABLE + playable, but has a P0 (below).** Files: `Character/{Silent,SilentCardPool,SilentRelicPool,SilentPotionPool}.cs`, `Cards/{StrikeSilent,DefendSilent,Neutralize,Survivor}.cs`, `Relics/RingOfTheSnake.cs`. PlaceholderID="silent", 70 HP, deck 5 StrikeSilent/5 DefendSilent/1 Neutralize/1 Survivor, starter relic RingOfTheSnake. Loc routed: cards→cards.json, `SPIRE1-SILENT.*`→characters.json, Architect dialogue→ancients.json. Build GREEN, verified in-game (embarked a Silent run, won a fight).
- **Defect / Watcher: NOT built.** Research done (see pointers).

### P0 — FIX FIRST: Silent combat-end crash (empty reward pool)
After winning a fight as Silent, reward generation throws `InvalidOperationException: couldn't generate a valid rarity! Card pool: SPIRE1-DEFEND_SILENT, NEUTRALIZE, STRIKE_SILENT, SURVIVOR` (godot.log). Cause: **SilentCardPool has only Basic starter cards; reward gen needs Common/Uncommon/Rare.** RULE (applies to every character): a card pool MUST contain non-Basic cards or combat-end crashes. FIX = add Silent Common/Uncommon/Rare cards. Ready-to-spawn deepseek `task` (I was about to run it; spec verbatim):
  10 Silent Commons, files `Cards/<Class>.cs` with `[Pool(typeof(SilentCardPool))]` (`using Spire1.Spire1Code.Character; using BaseLib.Utils;`), loc→`_staging/silentcommon.json`, StS1- prefix titles colliding w/ StS2 (check `.tmp/dllsrc/.../Cards/`): Slice(0E,6dmg,+3), DaggerThrow(1E,9dmg+draw1+discard1,dmg+3), PoisonedStab(1E,6dmg+PowerVar<PoisonPower>(3) Apply to target,8/4), QuickSlash(1E,8dmg+draw1,dmg+4), DaggerSpray(1E AllEnemies,DamageVar(4)+RepeatVar(2),+2), Deflect(0E,4 Block,+3), DeadlyPoison(1E,PoisonPower(5) to target,+2), Backflip(1E,5 Block+draw2,Block+3), Acrobatics(1E,draw3+discard1,draw4), Prepared(0E,draw1+discard1,draw2/discard2). Poison=`CommonActions.Apply<PoisonPower>(choiceContext, play.Target!, this)`. discard=draw then `CardSelectorPrefs.DiscardSelectionPrompt`+SelectCards(Hand)+`CardCmd.Discard(choiceContext,picked)`. Then merge silentcommon.json→cards.json, build, smoke. (After that, continue Silent uncommon/rare + relics/potions per TODO.)

### NEW GOTCHAS this session (add to the character-scaffold checklist; each new character NEEDS all of):
1. `Character/<Name>.cs : PlaceholderCharacterModel`, PlaceholderID = an StS2 char with visuals ("silent"/"defect" exist; **"watcher" does NOT** — needs substitute skin or art).
2. `<Name>CardPool : CustomCardPoolModel` with `EnergyColorName => "<placeholderid>"` (reuse StS2 energy icon; do NOT use custom charui/*.png — they render gray), CardFrameMaterialPath, DeckEntryCardColor. Plus `<Name>RelicPool`/`<Name>PotionPool` (potion pool EnergyColorName too). Character `CardPool/RelicPool/PotionPool` point to these.
3. **ancients.json** MUST get 4 keys per char or STS001 build FAILS: `THE_ARCHITECT.talk.SPIRE1-<CHAR>.0-0r.char`, `.0-0r.next`, `.0-1r.ancient`, `.0-attack`.
4. **Character loc → `characters.json`** (keys `SPIRE1-<CHAR>.{title,titleObject,description,pronounSubject,pronounObject,pronounPossessive,possessiveAdjective,aromaPrinciple,goldMonologue,eventDeathPrevention,cardsModifierTitle,cardsModifierDescription,banter.alive.endTurnPing,banter.dead.endTurnPing}`) — NOT cards.json. Card loc → cards.json. Route staging by key prefix `SPIRE1-<CHAR>.`.
5. `[Pool(typeof(...))]` needs `using BaseLib.Utils;`. Per-card/-relic `[Pool]` (most-derived) overrides the base Spire1Card/Spire1Relic pool.
6. Card pool needs non-Basic cards (P0 above).

### RESEARCH DONE (reuse — don't re-research; saves budget):
- **Defect ORBS**: `read history://OrbsResearch` — verified channel/evoke (OrbCmd), Focus power, orb slot count (BaseOrbSlotCount), orb model classes (Lightning/Frost/Dark/Plasma). Defect char PlaceholderID="defect" (StS2 has Defect). Starter: Cracked Core (start w/ 1 Lightning orb), deck 4 Strike/4 Defend/1 Zap/1 Dualcast, 75 HP.
- **Watcher**: `read agent://WatcherProbe` (15.8KB feasibility) — verdict on stances (Calm/Wrath/Divinity) existence + substitute visuals. Likely stances ABSENT + no StS2 Watcher visual → heavily blocked; do foundation + feasible non-stance cards + FLAG. 72 HP, Pure Water, deck 4 Strike/4 Defend/1 Eruption/1 Vigilance.

### OPEN QUESTION (user-reported, unresolved): Ironclad cards show PLACEHOLDER art but Silent cards show real card art. Investigate why the two set up card art differently — compare `Spire1CardPool.cs` (Ironclad) vs `SilentCardPool.cs` + BaseLib `CustomCardModel`/`CustomCardPoolModel` art-path resolution (CustomArt/CustomFrame/image). Was mid-investigation. If Silent's path is better, apply to Ironclad.

### DEFERRED (post-characters): M2 monsters (MoveBuilder pattern known, blocked on spine visuals — reuse StS2 monster skins or art), M3 4-act dungeon (CustomActModel + char-select decoupling), per-card/relic art, custom-power icons. All flagged in NEXT/LIMITATIONS.


## ===== HANDOFF AFTER ARCHIVE (2026-08-19)

### Scope
- User explicitly stopped the Minecraft data-pack task. Current work is the Slay the Spire 2 mod under `G:/omp works/sts2-spire1/`.
- All project work must follow `G:/omp works/AGENTS.md`, this project's `DEVELOP.md`, and this `DEVLOG.md`.
- Allowed written languages are Chinese, English, French, German, and Russian only. Prefer ASCII punctuation. Never write Japanese, Korean, or other scripts.

### Confirmed current implementation
- Ironclad is complete and was verified in game through Steam. Keep the existing smoke record above as the source of truth.
- Silent foundation exists and was verified selectable and playable, but combat reward generation still crashes because `SilentCardPool` contains only Basic cards. This is the first implementation task after resume.
- Defect and Watcher foundation code was not yet present when this session started. Research is already recorded in `agent://OrbsResearch` and `agent://WatcherProbe`; do not repeat that research unless an API conflict appears.

### Edits made in this session
- `mod/Spire1Code/Cards/Eruption.cs`: `Eruption.OnUpgrade()` now calls `EnergyCost.UpgradeBy(-1)`. Vanilla Eruption+ costs 1 and keeps the same 9 damage. The previous `UpgradeValueBy(5)` behavior was incorrect and was removed.
- `mod/Spire1/localization/eng/cards.json`: merged card entries from `mod/_staging/defect-foundation.json` and `mod/_staging/watcher-foundation.json`. The router counted 16 card entries.
- Defect Defend description was normalized to `Gain !B! *Block*.` so Block uses the existing gold formatting convention.
- `mod/Spire1/localization/eng/characters.json`: merged character entries from the same staging files. The router counted 29 character entries.
- Watcher single `SPIRE1-WATCHER.pronouns` was expanded to the required keys: `pronounSubject=she`, `pronounObject=her`, `pronounPossessive=hers`, `possessiveAdjective=her`.
- Five staged relic entries were intentionally dropped from JSON. `CrackedCore.cs` and `PureWater.cs` already define localization through `RelicLoc`, so duplicate JSON entries are unnecessary. Their code loc remains authoritative.

### Files inspected and evidence
- `mod/Spire1Code/Relics/CrackedCore.cs`: `Localization` returns `new RelicLoc(...)` and uses `DefectRelicPool`.
- `mod/Spire1Code/Relics/PureWater.cs`: `Localization` returns `new RelicLoc(...)`, uses `EnergyVar(2)`, and grants energy on the first combat turn.
- `mod/Spire1Code/Character/Defect.cs`: PlaceholderID `defect`, 75 HP, 3 orb slots, starter deck 4 Strike, 4 Defend, Zap, Dualcast, and Cracked Core.
- `mod/Spire1Code/Cards/Dualcast.cs`: uses `OrbCmd.EvokeNext` twice when an orb exists and upgrades cost from 1 to 0.
- `mod/Spire1Code/Cards/Eruption.cs`: 2 cost, 9 damage, no stance API available in StS2 v0.111.0; description documents the omitted Wrath effect.

### Required localization routing
- Card keys go to `mod/Spire1/localization/eng/cards.json`.
- Character keys with prefix `SPIRE1-DEFECT.` or `SPIRE1-WATCHER.` go to `mod/Spire1/localization/eng/characters.json`.
- Relic localization defined by `RelicLoc` stays in C# and must not be duplicated in card or character JSON.
- All localization values must be flat strings. Nested objects cause a runtime `LocException` and are not caught by the STS001 analyzer.
- Use `!D!`, `!B!`, `!E!`, and `*gold*` according to the SimpleLoc rules already recorded above.

### Immediate resume sequence
1. Add Silent Common, Uncommon, and Rare cards before any new character work. This fixes the known reward-generation P0.
2. Route Silent card and character localization through staging files, then remove staging files only after the central build succeeds.
3. Run the central build with all caches on G. Required environment: `NUGET_PACKAGES`, `DOTNET_CLI_HOME`, `TEMP`, and `TMP` must point under `G:/omp works/sts2-spire1/`.
4. Rebuild and perform a Steam-launched smoke test. Direct executable launch is blocked by DRM.
5. After Silent reward generation is fixed, implement Defect foundation and its non-Basic card pool. Watcher remains constrained by the absence of a native StS2 Watcher visual and stance API; use the documented substitute and disclose omitted stance behavior rather than faking it.

### Verification state
- The Eruption source edit was applied successfully.
- The localization merge script completed and reported `16` card entries, `29` character entries, and `5` dropped relic entries.
- A central build and post-merge in-game smoke test have not yet been run after these latest Defect/Watcher localization edits. Do not claim them verified until they are run.
- Do not overwrite unrelated user changes. Do not write to C:.

## STATUS UPDATE — Silent reward pool (2026-08-19)
- Implemented the missing StS1 Silent reward pool: 9 Common, 17 Uncommon, and 19 Rare cards. Silent now has a non-Basic pool across all reward rarities; this removes the known empty-rarity crash path.
- Reused shipped StS2 APIs where available: `Shiv`, `AccuracyPower`, `NoxiousFumesPower`, `InfiniteBladesPower`, `AfterimagePower`, `EnergyNextTurnPower`, `DrawCardsNextTurnPower`, `DoubleDamagePower`, `BlockNextTurnPower`, and the verified discard/history APIs.
- Added custom powers for the Silent mechanics that are absent from the base game, including Blur, Choke, A Thousand Cuts, Corpse Explosion, Envenom, Tools of the Trade, and Wraith Form support.
- Review fixes applied: batch discard for `Concentrate`; `CardCmd.DiscardAndDraw` for `CalculatedGamble`; canonical `-2` costs for `Reflex` and `Tactician`; clone guard for `MasterfulStab`; `ChokePower` is instanced per applier, removes fully at the applier's turn end, and stores the amount per card play to avoid stacked-power overdamage.
- Localization: merged 122 staged Silent card keys into `mod/Spire1/localization/eng/cards.json`, including the analyzer-correct `SPIRE1-A_THOUSAND_CUTS.*` keys. All merged values were flat strings.
- Build verification: `dotnet build Spire1.csproj -c Debug /p:Sts2Path="G:/steam/steamapps/common/Slay the Spire 2"` passed with 0 errors after the final fixes. Existing nullable and async warnings remain.
- Steam runtime verification: launched with Steam app `2868840`; `godot.log` shows `Spire1.dll` and `Spire1.pck` loaded, `Spire1.MainFile` initialized, BaseLib gameplay mode enabled, and no Spire1 exception or localization error in the current log. The automated surface cannot select a character and complete a Silent combat reward interaction, so that exact reward UI path remains user-visual verification.
- Known non-fatal runtime limitation: the log reports missing custom relic image files under `res://Spire1/images/relics/`; relic behavior remains implemented but icons use fallback visuals.
- Final rare-slice review found and fixed one contract defect: `PhantasmalKiller` is a `CardType.Skill`, matching the vanilla StS1 data; it was incorrectly declared as `CardType.Power` before the final rebuild.
- The four merged Silent localization staging files were removed after the successful central build. The spec files remain as research artifacts.
- Follow-up rare-slice review fixes: `BulletTime`, `Nightmare` and `StormOfSteel` now use `TargetType.None` (they take no creature target), and `AThousandCutsPower` adopts the shipped `AfterimagePower` before/after bookkeeping so it records the amount at play start and never triggers on the card play that applied it. `EnvenomPower` was confirmed correct against the shipped predicate (dealer, powered attack, unblocked damage) and needs no card-source check.
- Rebuilt with 0 errors and relaunched through Steam after these fixes; `godot.log` again shows `Spire1` initialized with no mod exception, no `LocException`, and no reward-rarity error.

## STATUS UPDATE — Defect pool + LEAN-CODE refactor (2026-08-19)
### Lean-code rule (user directive)
- New rule, recorded in `DEVELOP.md` 7a: when a card's mechanic AND numbers match the StS2 card of the same name, we do NOT define a class; the shipped card is added to our pool instead.
- Mechanism (verified): `ModHelper.AddModelToPool(poolType, modelType)` accepts ANY `AbstractModel` type, and `ModHelper.ConcatModelsFromMods` resolves it through `ModelDb.GetById` and concatenates the canonical shipped instance into the pool. It MUST run before the first pool generation (`ModPoolContent.isFrozen` throws afterwards), so it is called from `MainFile.Initialize()`.
- Implemented in `mod/Spire1Code/Character/SharedCardReuse.cs`: 26 shipped StS2 cards are now obtainable by StS1 - Defect instead of being reimplemented.
- Deleted our duplicate classes: `Scrape`, `MachineLearning`, `MeteorStrike`, `Skim`, `WhiteNoise` (+ `MachineLearningPower`). `Skim`/`WhiteNoise` were kept by a writer only because StS1 says target NONE while StS2 says `Self`; that is the same thing (no creature target), so they are reuse cases.
- Automated diff (`research/sts1data/`): 97 same-name cards, 76 numerically identical. Traps that match numerically but differ mechanically keep our own class: `Expertise`, `Claw`, `BiasedCognition`, `CalculatedGamble`, `Equilibrium`, `Chill`, `Darkness`, `Defragment`, `Fusion`, `GeneticAlgorithm`, `Glacier`, `HelloWorld`, `Hyperbeam`, `Sunder`, `Storm`, `Tempest`, `RipAndTear`, `AllForOne`, `Barrage`, `Rebound`, `Stack`, `ConserveBattery`.

### Defect (blue) pool
- All 71 non-basic StS1 Defect cards are now covered: 45 own classes + 26 reused shipped cards.
- Custom powers added: `HeatsinksPower`, `HelloWorldPower`, `SelfRepairPower`, `StaticDischargePower`, `StormPower`, `BiasedCognitionPower`, `CreativeAIPower`, `ReboundPower` (shipped one reused where available).
- FLAGGED as not implementable on v0.111.0 (deliberately absent, never faked): `LockOn` (orb damage carries no per-target hook: `LightningOrb.cs:62`/`DarkOrb.cs:54` send plain `ValueProp.Unpowered` with a null card source, and `ModifyOrbValue` has the orb but no target), `Amplify` (shipped `BurstPower` only replays `CardType.Skill`), `Electrodynamics` (no orb-AoE modifier API).
- Localization: 112 Defect keys merged; 6 keys dropped for the deleted duplicates. Analyzer key rule learned: it splits EVERY case boundary, so `FTL` -> `SPIRE1-F_T_L`, `CreativeAI` -> `SPIRE1-CREATIVE_A_I`, `AThousandCuts` -> `SPIRE1-A_THOUSAND_CUTS`.
- Coordinator-fixed defects: missing usings in `StaticDischargePower`/`CreativeAIPower`/`ThunderStrike`, and `MultiCast` had base cost 0 instead of the X-cost `-1`.

### Bug fixed from user smoke
- `Piercing Wail` displayed "lose -6 Strength" (double negative). Cause: the card stored a negative `PowerVar` while the text said "lose". Fix: the card now stores a POSITIVE 6 (+2 on upgrade) and `PiercingWailPower` overrides BaseLib's `InvertInternalPowerAmount => true`, which applies -6 internally and flips the displayed `PowerType` to Debuff. Vanilla wording restored.
- Confirmed NOT a bug: `Blade Dance` gives 3 Shivs and 4 when upgraded (jar: magic 3, upgrade +1). Only `Storm of Steel+` produces upgraded Shivs in StS1.
- User confirmed the shop offers all three card types for both Ironclad and Silent, so no shop-category gap remains.

### Verification
- `dotnet build` 0 errors, deployed to `mods/Spire1/`. Defect in-game smoke is pending user testing.

### Deferred by user request (do AFTER the project fully builds)
1. Code security review of the whole mod (no auth surface, so scope is mod-level safety: reflection, file IO, save handling, Harmony patch scope).
2. Code efficiency pass (allocations in hot combat hooks, per-frame work, repeated LINQ over piles).

## STATUS UPDATE — Watcher stance subsystem is REAL (2026-08-19)
Supersedes every earlier note that said "no stance API exists, stances deliberately omitted".
- Built and compiled: `Powers/StancePower.cs` (abstract), `CalmPower`, `WrathPower`, `DivinityPower`, `MantraPower`, plus `Extensions/StanceCmd.cs` and `Extensions/IOnStanceChanged.cs`.
- Mechanism: `AbstractModel.ModifyDamageMultiplicative` is dispatched to every hook listener, so Wrath multiplies damage dealt AND received by 2 and Divinity multiplies damage dealt by 3, exactly like the shipped `DoubleDamagePower` (dealer side) and `ColossusPower` (target side).
- `StanceCmd` owns the rules: one stance at a time, entering the current stance is a no-op that fires no trigger, the old stance is removed first so Calm's 2-Energy exit bonus applies, entering Divinity grants 3 Energy, Divinity removes itself at the owner's next turn start, and `IOnStanceChanged` is dispatched exactly once to the player's powers plus the cards in all five combat piles.
- `StanceCmd.GainMantra` stacks `MantraPower` and, at 10, subtracts 10 and enters Divinity, preserving any remainder.
- Scry needs no work: BaseLib already ships `ScryCmd.Execute`, `ScryVar` and the `IAfterScryed` hook, so Watcher scry cards and the "whenever you Scry" triggers (Nirvana, Weave) use the framework directly.
- Token cards StS2 lacks are now implemented as `CardRarity.Token` cards with no pool attribute (so reward generation can never offer them): `Miracle`, `Insight`, `Smite`, `Safety`, `ThroughViolence`, `Beta`, `Omega` (+ `OmegaPower`), `Expunger` (settable hit count for Conjure Blade).
- Recurring build trap, now a contract item: `CardModel` lives in `MegaCrit.Sts2.Core.Models`, and worker-written powers keep omitting that using. Fixed centrally five times so far.
- Verification: `dotnet build` 0 errors with the infrastructure and all 8 token cards; deployed to `mods/Spire1/`.
- In flight: the 73 remaining Watcher cards (5 parallel slices, specs in `mod/_staging/spec-watcher-*.md`, all numbers from the jar).

## ===== HANDOFF / RESUME POINT (2026-08-19, context compressed here) =====
Read `DEVELOP.md` sections 7a/7b/7c/7d first, then this block. Everything below is current and verified.

### Build + deploy command (caches MUST stay on G:)
```
cd "G:/omp works/sts2-spire1/mod"
NUGET_PACKAGES=".../.nuget/packages" NUGET_HTTP_CACHE_PATH=".../.nuget/http-cache" DOTNET_CLI_HOME=".../.dotnethome" TEMP=".../.tmp" TMP=".../.tmp" \
dotnet build Spire1.csproj -c Debug /p:Sts2Path="G:/steam/steamapps/common/Slay the Spire 2"
```
Last run: **0 errors, deployed to `mods/Spire1/`**. The game LOCKS `Spire1.dll` while running, so kill `SlayTheSpire2.exe` before building. Launch for smoke: `powershell -NoProfile -Command "Start-Process 'G:\steam\steam.exe' -ArgumentList '-applaunch','2868840'"`; log at `C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/logs/godot.log`.

### Content state per character
| Character | Non-basic pool | Status |
|---|---|---|
| StS1 - Ironclad | complete | in-game verified earlier |
| StS1 - Silent | 9 Common + 17 Uncommon + 19 Rare | built, user-tested (shop offers all 3 card types) |
| StS1 - Defect | 71 of 71 (45 own classes + 26 reused shipped cards) | built + deployed, smoke pending |
| StS1 - Watcher | 19 of 73 + full stance/Mantra subsystem + 8 token cards | built + deployed; 54 cards IN FLIGHT |

### IN FLIGHT when context was compressed (3 subagents, restarted once because their provider died)
- `WatcherUncA2` -> `mod/_staging/spec-watcher-uncommon-a.md`, 17 cards (BattleHymn..Perseverance) + powers BattleHymnPower, CollectPower, ForesightPower, LikeWaterPower, MentalFortressPower, NirvanaPower -> `mod/_staging/watcher-uncommon-a-loc.json`.
- `WatcherUncB2` -> `spec-watcher-uncommon-b.md`, 18 cards (Pray..WreathOfFlame) + powers RushdownPower, SimmeringFuryPower, StudyPower, SwivelPower, TalkToTheHandPower, WaveOfTheHandPower, WreathOfFlamePower -> `watcher-uncommon-b-loc.json`.
- `WatcherRares2` -> `spec-watcher-rares.md`, 19 cards (Alpha..Wish) + powers BlasphemyPower, DevaFormPower, DevotionPower, DisciplinePower, EstablishmentPower, MasterRealityPower -> `watcher-rares-loc.json`.
If those files are absent on resume, re-dispatch the same three slices; the spec files hold every exact value so nothing needs re-researching.

### Coordinator checklist for each finished slice (this is the whole integration loop)
1. Merge the slice's `mod/_staging/<slice>-loc.json` into `mod/Spire1/localization/eng/cards.json`. Values MUST be flat strings (a nested object causes a runtime `LocException` the analyzer does NOT catch).
2. Delete keys for any card the writer reported as REUSE_SHIPPED, and delete our duplicate class if it exists.
3. Build centrally; fix the recurring mechanical errors listed below.
4. Delete the merged staging file (keep the `spec-*.md` files as research artifacts).

### Recurring build traps (each has bitten us at least twice)
- `CardModel` needs `using MegaCrit.Sts2.Core.Models;` — fixed centrally 6+ times in worker-written powers/cards.
- `DynamicVars.Power<T>()` needs `using BaseLib.Extensions;`.
- STS001 localization keys split at EVERY case boundary: `FTL` -> `SPIRE1-F_T_L`, `CreativeAI` -> `SPIRE1-CREATIVE_A_I`, `AThousandCuts` -> `SPIRE1-A_THOUSAND_CUTS`, `MultiCast` -> `SPIRE1-MULTI_CAST`.
- X-cost cards MUST use base cost `-1` + `HasEnergyCostX` (a worker shipped `MultiCast` with cost 0).
- Card text conventions verified in game: NEVER use StS1 markup. No `NL` (34 keys had to be stripped), no `[E]`/`[B]`/`[W]`/`[G]` energy icons (write `*Energy*`), and keyword markers must be closed (`*Void*`, not `*Void`). Only use `!E!` when the card actually declares an `EnergyVar`.

### Bugs found by user smoke and FIXED
- `Piercing Wail` read "lose -6 Strength". Fix: card stores POSITIVE 6 (+2), and `PiercingWailPower` overrides BaseLib `InvertInternalPowerAmount => true` so -6 is applied internally and the type flips to Debuff.
- `Miracle+` gave 1 Energy instead of 2 — added the missing `OnUpgrade` (the token slice had omitted it; reported by `WatcherUncA2`).
- NOT a bug: `Blade Dance` gives 3 Shivs / 4 upgraded (jar: magic 3, +1). Only `Storm of Steel+` makes upgraded Shivs.

### Deliberately NOT implemented (missing StS2 API — never fake these)
- Defect: `LockOn` (orb damage has no per-target hook), `Amplify` (shipped `BurstPower` only replays Skills), `Electrodynamics` (no orb-AoE modifier).
- Watcher: whatever the three in-flight slices flag; expect pressure on `Fasting` (per-turn energy reduction), `ForeignInfluence` (choose 1 of 3 any-color Attacks), `Omniscience`/`Unraveling` (auto-play), `Vault` (extra turn).

### NEXT WORK, in order
1. Integrate the three in-flight Watcher slices (loop above), then verify Watcher rewards in game.
2. Port the StS1 "?" events: 52 events with exact per-act membership and official text already extracted to `research/sts1data/events.json`; API is `BaseLib.Abstracts.CustomEventModel` (ctor auto-registers, `Acts` empty = shared/any act, gate with `IsAllowed(IRunState)` reading `runState.CurrentActIndex`/`TotalFloor`). StS2 shares NO event name with StS1, so nothing is a duplicate. Localization table is `events` with keys `{ID}.title`, `{ID}.pages.{PAGE}.description`, `{ID}.pages.{PAGE}.options.{OPTION}.title/.description`.
3. M2 monsters: exact StS1 act encounter tables are in `DEVELOP.md` 7d. Still blocked on monster visuals (need to reuse a shipped StS2 monster scene or commission art).
4. THEN, per user instruction and only after the project fully builds: (a) code security review, (b) code efficiency pass.

### Research artifacts (do NOT re-derive)
- `research/sts1data/cards-green-blue-purple.json` — 228 StS1 cards with exact cost/damage/block/magic/rarity/target/flags/upgrade deltas + official English text, extracted from `desktop-1.0.jar` bytecode via `javap`.
- `research/sts1data/cards-colorless.json`, `cards-temp.json` (token cards incl. Shiv/Miracle/Insight/Smite/Safety/Beta/Omega/Expunger), `events.json` (52 events).
- Extraction recipe: StS1 is installed at `G:/steam/steamapps/common/SlayTheSpire`; `G:/zulu17/bin/javap.exe -p -c -constants -cp desktop-1.0.jar <class>` plus `jar xf ... localization/eng/{cards,events}.json`.
- `mod/Spire1Code/Character/SharedCardReuse.cs` — the lean-code reuse list; add to it (and delete our class) whenever a card proves identical to a shipped one.
- `research/sts1data/relics.json` — all 14 StS1 event-granted relics with exact constants, overridden StS1 hooks, decompiled behaviour and official English NAME/DESCRIPTION/FLAVOR. Added in session 3.
- `research/sts1data/specs/` — 18 per-slice spec sheets (Silent/Defect/Watcher card pools, the four event regions) carrying the OFFICIAL extracted English text verbatim. **Consumed**: every card and event they specify is implemented. Archived out of `mod/_staging/` in session 4 so that `_staging` means "in-flight only" — keep them as a wording reference so nobody has to re-run `javap` to check a string.
- **Known bad datum, since corrected**: `cards-colorless.json` recorded `RitualDagger.base.baseDamage = 3`. The bytecode is `misc = 15` then `baseDamage = misc`, so the true value is **15** (`misc` is also the run-persistent accumulator). Fixed in the file with an explanatory `note`. Treat any other card whose `baseDamage` derives from `misc` with the same suspicion.

## ===== HANDOFF (2026-08-19, session 2 end — all four characters + all 52 events build clean) =====
Supersedes the "NEXT WORK" list above: items 1 and 2 are DONE. Build is **0 errors**, deployed to `mods/Spire1/` (`Spire1.dll` + `Spire1.pck`).

### Completed this session
- **Watcher card pool finished: 73/73 non-Basic cards** + 8 token cards + the stance/Mantra subsystem. Card counts on disk: 300 card classes, 49 power classes.
- **All 52 StS1 "?" events ported** into `mod/Spire1Code/Events/` (52 event classes + `Spire1Event.cs` base). Act routing verified to match StS1 exactly: **Act1 11, Act2 15, Act3 9, shared 17**.
- Localization: `cards.json` 663 keys, new `events.json` 632 keys, `characters.json` 57 keys (14–15 per character, all four present).

### Event system contract (all verified against decompiled source, do NOT re-derive)
- `mod/Spire1Code/Events/Spire1Event.cs` is the base: subclasses override `ShippedPortrait` (a stem under `res://images/events/`), and `Acts => Act1|Act2|Act3` (helpers on the base) or nothing at all for a shared event.
- StS1's four regions map 1:1 onto StS2's four shipped acts — `ModelDb.Acts` = `Overgrowth, Underdocks, Hive, Glory` (`ModelDb.cs:300-320`). Exordium→Overgrowth, City→Underdocks, Beyond→Hive, shrines→shared.
- Registration is automatic: the `CustomEventModel(bool autoAdd = true)` ctor calls `CustomContentDictionary.AddEvent(this)`, which routes on `Acts.Length == 0` (`ContentPatches.cs:84-96`); act events are injected by an `ActModel.AllEvents` postfix (`ContentPatches.cs:396-408`).
- Option loc key = `{Id.Entry}.pages.{pageKey}.options.{Slugify(handlerMethodName)}` and BOTH `.title` and `.description` must exist (`CustomEventModel.cs:60-115`). Locked options use `LockedOption(key, pageKey)`, never `Option(null, …)`.
- Multi-page flow: `SetEventState(PageDescription("PAGE"), [ … ])`; terminate with `SetEventFinished(PageDescription("PAGE"))` (`EventModel.cs:478-545`).
- **Overload trap:** `HoverTipFactory.FromCardWithCardHoverTips<T>()` returns `IEnumerable<IHoverTip>`, so it does NOT bind to the `params IHoverTip[]` overload — the compiler silently picks the `LocString title, LocString description` overload and errors. Use `Option(Handler, HoverTipFactory.From…(), "PAGE")` (tips BEFORE pageKey).
- Event art: no event art ships in this repo. Each event points at a thematically matching **shipped** StS2 portrait; the 59 valid stems were enumerated by scanning `SlayTheSpire2.pck` for `res://images/events/*.png`. Do not invent a stem — rescan if you add events.

### Localization mechanics — the rule that caused the most damage this session
`MainFile.Initialize` calls `SimpleLoc.EnableSimpleLoc(ModId)`, and the two loc paths have **opposite** `#` semantics:
- **JSON files** (`Spire1/localization/eng/*.json`), `SimpleLoc.cs:39-57`: a string WITHOUT `#` is simplified (so `*gold*`, `!diffVar!`, `[E]`/`[EE]` energy icons, `-base-+upgraded+` upgrade swap and `{Var:plural:|s}` all work); a string WITH a leading `#` is taken **raw** — every one of those markers would render literally.
- **Code-provided loc** (`RelicLoc`/`PowerLoc` via `ILocalizationProvider.Localization`), `ModelLocPatch.cs:54` → `SimpleLoc.TrySimplify`: exactly inverted — a leading `#` means **simplify**, no `#` means raw. That is why every relic/power string in this project starts with `#`.
Consequences already handled: 13 relic descriptions had been duplicated into `cards.json` as `#`-prefixed entries (dead — relics resolve from code through the `relics` table) and were deleted; `SPIRE1-CONJURE_BLADE.description` legitimately keeps its `#` because it stores pre-expanded `[gold]…[/gold]{IfUpgraded:show:…}` output.
Markup that is NOT StS2 and must always be stripped from ported StS1 text: `NL` line breaks, `#r`/`#g`/`#b`/`#y`/`#p` colours, `~wave~`, `@italic@` (`@…@` additionally collides with SimpleLoc's inverse-diff-variable syntax), and `[B]`/`[W]`/`[G]` energy icons (only `[E]`/`[E+]`/`[E?]` are real). 73 event strings and ~50 card strings were cleaned for this.

### Build hygiene added
- `mod/GlobalUsings.cs` declares `global using MegaCrit.Sts2.Core.Models;` and `global using MegaCrit.Sts2.Core.Events;`. This alone took the event batch from 72 errors to 14 and permanently kills the recurring "CS0246: CardModel / ActModel / EventOption not found" class that had to be patched by hand 6+ times.
- Warning baseline after a full rebuild: 188 CS8602 + 152 CS8604 + 32 CS1998 + 12 CS8600 + 8 CS4014 + 2 CS8601, **0 errors**. The 8 CS4014 are in `Kunai`/`LetterOpener`/`OrnamentalFan`/`Shuriken` and are intentional: `TaskHelper.RunSafely(DoActivateVisuals())` is the shipped fire-and-forget idiom (used 420 times in the game's own code) so a 1-second flash does not block the effect.
- `StS1 SPECIAL` rarity has no StS2 equivalent; it splits into `CardRarity.Token`, `CardRarity.Event` and `CardRarity.Status` (see `Bonfire.cs` reward table).

### Event content gaps — FLAGGED by the writers, never faked
Missing StS1 **relics** blocking event branches: `GoldenIdol`, `BloodyIdol`, `RedMask`, `Circlet`, `WarpedTongs`, `MutagenicStrength`, `Necronomicon`, `Enchiridion`, `Nilry'sCodex`, `NlothsGift`, `SpiritPoop`, `OddMushroom`, plus the Face Trader face relics.
Missing StS1 **cards**: `Apparition`, `Bite`, `RitualDagger`, `J.A.X.`, `Writhe`, `Normality`, `Parasite`, `MarkOfTheBloom`(relic).
Missing **encounters** (no StS1 monsters ported yet): Colosseum Slavers/Nobs, Masked Bandits, Dead Adventurer's elite set (3 Sentries / Gremlin Nob / Lagavulin), The Mushroom Lair, MysteriousSphere's orb walkers, MindBloom's Act-1 boss, Nest, TheJoust, Vampires.
Deliberate deviations, all documented in the source: `Colosseum` overrides `IsAllowed => false` so it cannot spawn and soft-lock a run until its encounters exist; `GremlinMatchGame` reproduces the exact 12-card set and outcome but uses the shipped face-up grid because StS2 has no memory-minigame UI; `NoteForYourself` implements the in-run half only (no cross-run save field); `SecretPortal`'s act jump depends on a `MapCmd` API that must be confirmed before it can work.

### NEXT WORK, in order
1. **In-game smoke test (not yet run for this session's content).** Kill `SlayTheSpire2.exe` first — it locks `Spire1.dll`. Launch: `powershell -NoProfile -Command "Start-Process 'G:\steam\steam.exe' -ArgumentList '-applaunch','2868840'"`; watch `C:/Users/o_Obl/AppData/Roaming/SlayTheSpire2/logs/godot.log` for `LocException`, missing-loc warnings, and any failure in `Spire1` init. Priorities: (a) Watcher stance/Mantra cards actually resolve, (b) a `?` room rolls a ported event and its options execute, (c) event portraits load.
2. Implement the flagged event-blocking relics and cards above (each unblocks concrete event branches).
3. M2 monsters/encounters: exact StS1 act encounter tables are in `DEVELOP.md` 7d; still blocked on monster visuals.
4. THEN, per user instruction and only after the whole project builds: (a) code security review, (b) code efficiency pass.

### Note on a failed helper
`EventDepScout` (a read-only dependency scan meant to produce `research/sts1data/event-deps.json`) exited 1 without writing the file. It is NOT needed — the four event writers each derived and reported their own dependency gaps, consolidated above. Do not re-run it.

## ===== HANDOFF (2026-08-20, session 3 — STOPPED MID-EDIT at user request; read this first) =====
Supersedes the session-2 handoff. Build was **0 errors** at the last verified point, but see "IN-FLIGHT EDIT" — the tree is now one edit short of that state.

### IN-FLIGHT EDIT — do this first, it is the only unfinished thing
`mod/Spire1Code/Events/Mushrooms.cs` is the LAST of 6 event files to be wired to the newly-added cards. The other 5 are done. Apply exactly this:
1. add `using Spire1.Spire1Code.Cards;` after the `MegaCrit.Sts2.Core.Models.Acts` using (keep usings alphabetical);
2. replace the `// FLAGGED: StS1 also adds a Parasite curse …` comment line AND the `await CreatureCmd.Heal(...)` line that follows it (they were lines 41-42) with:
```csharp
        // StS1: eating heals 25% of Max HP and adds one Parasite curse. Parasite is a mod card
        // (SPIRE1-PARASITE, Spire1Curse + Unplayable + MaxHpVar(3)).
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        await CardPileCmd.AddCurseToDeck<Parasite>(Owner);
```
3. then run the central build (recipe below) and expect **0 errors**. Nothing else is pending.

### Completed this session
- **Event base-class efficiency**: `Spire1Event` now caches the per-act `ActModel[]` arrays and the portrait path string. `Acts` is hot (BaseLib reads it once per event at registration and again for every room-visual build) and was allocating a new array on every access across 35 act-bound events.
- **CRITICAL bug fixed — shared mutable list across event clones.** `AbstractModel.MutableClone()` is `MemberwiseClone` (`.tmp/dllsrc/MegaCrit.Sts2.Core.Models/AbstractModel.cs:159-187`), so a `readonly List<T>` instance field on an event is **shared between the canonical model and every per-player mutable clone**: contents accumulate across visits and co-op players corrupt each other. Fixed in `DeadAdventurer.cs` (`_rewards`) and `GremlinMatchGame.cs` (`_cards`) by dropping `readonly` and overriding the engine's `DeepCloneFields()` hook to re-seed the list. **Rule for all future events: an event MUST NOT hold a `readonly` collection field; use `DeepCloneFields()`.**
- **CRITICAL bug fixed — hang.** `StanceCmd.GainMantra` used `while (mantra.Amount >= 10) { ModifyAmount(-10); Enter<Divinity>(); }`. `PowerCmd.ModifyAmount` returns **without changing the amount** when `CombatManager.Instance.IsEnding` or `CombatState == null` (`.tmp/dllsrc/MegaCrit.Sts2.Core.Commands/PowerCmd.cs:219-258`), and hooks may rewrite the offset — so the loop could never terminate and would freeze the game. Now breaks the moment an iteration fails to reduce the counter; threshold extracted to `_mantraThreshold`.
- **4 new cards** (`Bite`, `JAX`, `RitualDagger`, `Parasite`) + 8 loc keys merged into `Spire1/localization/eng/cards.json` (now **671 keys**). Staging file consumed and deleted.
- **3 cards deliberately NOT created — they would have been duplicates.** StS2 ships `Apparition`, `Writhe` and `Normality` with mechanics identical to StS1 **and already registers them in shipped pools** (`EventCardPool.cs:24` for Apparition; `CurseCardPool.cs:34,39` for Normality and Writhe — the very pool our own curses join). Creating mod copies would put two entries of each in one pool. Verified by reading the decompiled classes directly. Reference them as `ModelDb.Card<T>()` / `CardPileCmd.AddCurseToDeck<T>()`; do **not** add them to `SharedCardReuse` either (that list is for cards we would otherwise have written).
- **`research/sts1data/relics.json` written** (14 event-granted relics, 13364 bytes, validated: 14 entries, uniform field order, `loc` = NAME/DESCRIPTION/FLAVOR, spliced numbers preserved). 13 SPECIAL + 1 COMMON. **Reuse shipped**: RedMask, Circlet, RegalPillow (StS2 ships all three). **API risk, resolve before implementing**: GoldenIdol, NlothsGift, OddMushroom, MarkOfTheBloom.
- **Data correction**: `research/sts1data/cards-colorless.json` had `RitualDagger.base.baseDamage = 3`; bytecode is `misc = 15` then `baseDamage = misc`, so the true value is **15** (magicNumber 3, upgrade +2 magic, and `misc` is the run-persistent accumulator). Corrected in the file with an explanatory `note`. Treat other `baseDamage`-from-`misc` cards with suspicion.

### Event wiring completed this session (5 of 6)
`Ghosts` → N× shipped `Apparition` via `RunState.CreateCard<T>` + `CardPileCmd.Add(list, PileType.Deck)`. `TheMausoleum` → shipped `Writhe` via `AddCurseToDeck<T>`. `Vampires` → 5× mod `Bite` (both the Accept and the Blood-Vial branch; count 5 confirmed from bytecode). `Nest` → 1× mod `RitualDagger`. `DrugDealer` → still has two locked options, see below. `Mushrooms` → **PENDING, see IN-FLIGHT EDIT**.

### Security review — CONCLUDED, no findings left
The `security-reviewer` subagent crashed while emitting its report (exit 1), but its probe trail yielded the two CRITICAL/MODERATE bugs above, both now fixed. I completed the remaining sweep myself over the whole tree: **zero** file IO (no `File.*`/`Directory.*`/`StreamWriter`/`FileStream`), **zero** absolute paths / `SpecialFolder` / `AppData` / `GetTempPath` (so the "never write C:" rule holds), **zero** `unsafe` blocks despite `AllowUnsafeBlocks`, **zero** reflection, and `harmony.PatchAll()` in `MainFile` is a no-op because this assembly declares no `[HarmonyPatch]` (all patching is BaseLib's). The three `[SavedProperty]` uses (`GeneticAlgorithm`, `RitualDagger`, `PenNib`) are correct. Loop termination audited: only two `while` loops exist in the mod and both are now bounded. **Do not re-run a security review; it is done.**

### Central build recipe (always build from the coordinator, never inside a worker)
`cwd = G:/omp works/sts2-spire1/mod`, env `DOTNET_CLI_HOME=G:/omp works/sts2-spire1/.dotnethome`, `NUGET_PACKAGES=G:/omp works/sts2-spire1/.nuget/packages`, `NUGET_HTTP_CACHE_PATH=G:/omp works/sts2-spire1/.nuget/http-cache`, `TEMP=TMP=G:/omp works/sts2-spire1/.tmp`:
`dotnet build Spire1.csproj -c Debug /p:Sts2Path="G:/steam/steamapps/common/Slay the Spire 2" -v:q -nologo`
Analyzer error `STS001` = a card/relic exists in code but its `.title`/`.description` keys are missing from the loc table; it is the expected transient state while a card is written but its loc not yet merged.

### NEXT WORK, in order
1. Finish the IN-FLIGHT EDIT above; build to 0 errors.
2. **Implement the 14 event-granted relics** from `research/sts1data/relics.json` — this unblocks the last locked event options. Reuse the 3 shipped ones; resolve the 4 API risks before writing those. `DrugDealer` needs `MutagenicStrength` (falls back to `Circlet` if already owned) and its `[Test J.A.X.]` option can be unlocked immediately now that `JAX.cs` exists — wire it when you touch that file.
3. `Mushrooms` still omits StS1's `[Stomp]` option because it starts "The Mushroom Lair" fight; that is the M2 monster work, still blocked on monster visuals (`DEVELOP.md` 7d).
4. Code efficiency pass (the user asked for it after security; security is now done).

### Verification state — BE HONEST ABOUT THIS
All claims above are from builds and file reads that actually ran. **No in-game smoke test has been run this session.** StS2 launches via Steam DRM, its UI cannot be driven automatically, and a running game locks `Spire1.dll` (so the build must happen before launching). Per `../AGENTS.md` §2 the visual/interactive smoke test belongs to the user: the things worth eyeballing are Council of Ghosts granting Apparitions, Vampires granting 5 Bites, The Nest granting a Ritual Dagger, The Mausoleum granting Writhe, and Mushrooms granting a Parasite.

## ===== SESSION 4 (2026-08-20, long unattended run) — PLAN + LIVE LOG =====
Supersedes the session-3 "NEXT WORK" list: items 1 and the J.A.X. half of item 2 are DONE.

### Development order for this session (do NOT reorder — later phases depend on earlier ones)
1. **Relic integration** — verify the relics on disk, merge their loc, build green.
2. **Event unlocking** — wire every event branch that was locked only because a relic was missing, and update the affected loc entries.
3. **Quality gate** — `reviewer` subagents over all new relics + events, fix findings, rebuild.
4. **Efficiency pass** — the user asked for this explicitly AFTER the security review, which is already done.
5. **Wrap-up** — DEVELOP contract + DEVLOG handoff, clean `_staging`.

### Phase 1 log — relic integration
- **In-flight edit from session 3 finished**: `Mushrooms.cs` now grants the Parasite curse (`CardPileCmd.AddCurseToDeck<Parasite>`). Build 0 errors.
- **`DrugDealer` `[Test J.A.X.]` unlocked** — `LockedOption("TEST_JAX")` became `Option(TestJax)`, which grants the mod `JAX` card via `RunState.CreateCard<JAX>` + `CardPileCmd.Add(..., PileType.Deck)`. `StringHelper.Slugify("TestJax") == "TEST_JAX"`, so the existing loc keys still resolve (verified: `Slugify` = CamelCase→`_`, upper-invariant, special chars stripped — `MegaCrit.Sts2.Core.Helpers/StringHelper.cs`). Added the official StS1 result page `SPIRE1-DRUG_DEALER.pages.JAX.description` (= jar `DESCRIPTIONS[1]`, markers stripped, `NL`→space per this project's event-text convention) and blanked the now-obsolete "not yet ported" option description. `events.json` = **633 keys**. The J.A.X. trade is free in StS1 — the bytecode constructs `new JAX()` with no damage or max-HP call.
- **10 event relics written** by three parallel opus5/high subagents, all reviewed by me against the decompiled engine, build **0 errors**: `GoldenIdol`, `BloodyIdol`, `SpiritPoop`, `MutagenicStrength`, `WarpedTongs`, `OddMushroom`, `MarkOfTheBloom` (on disk and building) plus the three Cursed Tome books in flight.
- **`RelicRarity.Event` is the pool-exclusion mechanism** — verified: `RelicFactory.RollRarity` only ever returns Common/Uncommon/Rare (`MegaCrit.Sts2.Core.Factories/RelicFactory.cs:80-93`), so an `Event` relic can never be pulled from a chest or shop even though `Spire1Relic` carries `[Pool(typeof(Spire1RelicPool))]`. Every event relic MUST use it.
- **Relic art is a non-issue** — `StringExtensions.RelicImagePath()` falls back to a shipped placeholder when the per-relic PNG is absent (`mod/Spire1Code/Extensions/StringExtensions.cs:49-65`). Only `relic.png` / `relic_outline.png` / `big/relic.png` exist; every mod relic shares that placeholder. Disclosed limitation, not a bug.

### Engine APIs discovered this phase (do NOT re-derive)
- **Gold rewards**: `AbstractModel.TryModifyRewards(Player, List<Reward>, AbstractRoom?)` (`AbstractModel.cs:2140`) is the faithful hook for "enemies drop more gold" — it receives BOTH the reward list and the owning room, so it reproduces StS1's `!(currRoom instanceof TreasureRoom)` test exactly. `ModifyGoldGained` is the WRONG hook: it fires for every `PlayerCmd.GainGold` (`PlayerCmd.cs:144`), so Hand of Greed / Maw Bank / event gold inside a combat room would all get boosted, which vanilla never does. Shipped precedent that rewrites `GoldReward` amounts: `Midas.cs`. Pair it with `AfterModifyingRewards()` (`AbstractModel.cs:966`) for the flash, and return `true` only when a reward was actually changed — `Hook.AfterModifyingRewards` filters on the modifier list.
- **Gold gained notification**: `AfterGoldGained(Player)` (`AbstractModel.cs:767`) runs after the `amount > 0` early-return at `PlayerCmd.cs:146`, so it never fires on a zero gain.
- **Card replayed twice**: `ModifyCardPlayCount(CardModel, Creature?, int)` (`AbstractModel.cs:1495`) + `AfterModifyingCardPlayCount(CardModel)` (`AbstractModel.cs:851`). The shipped RELIC template is `MegaCrit.Sts2.Core.Models.Relics/ThrowingAxe.cs` — latch behind a property whose setter calls `AssertMutable()`, `+ playCount` in the modifier, latch + `Flash()` + `Status = RelicStatus.Normal` in the after-hook, re-arm in `AfterRoomEntered`, reset in `AfterCombatEnd`. `RelicStatus` = `{Normal, Active, Disabled}`.
- **Cost actually paid**: `card.EnergyCost.GetResolved()` (`CardEnergyCost.cs:155-162`) — returns `CapturedXValue` for X-cost cards, else `Max(0, GetWithModifiers(All))`. `GetWithModifiers(All)` alone returns raw `_base` for X-cost cards, so it must NOT be used for "costs N or more" tests. Timing is safe: `CapturedXValue` is set at `CardModel.cs:1826`, `GeneratePlayCount` runs at `CardModel.cs:1887`.
- **Random in-combat card generation**: `CardFactory.GetDistinctForCombat(Player, IEnumerable<CardModel>, int count, Rng)` (`CardFactory.cs:119`) — internally applies `FilterForCombat` (`CanBeGeneratedInCombat && Rarity != Basic/Ancient/Event`, `.Distinct()`), which IS StS1's `CardTags.HEALING` exclusion. Then `CardModel.SetToFreeThisTurn()` (`CardModel.cs:1267`) or `SetToFreeThisCombat()` (`CardModel.cs:1273`), then `CardPileCmd.AddGeneratedCardToCombat(card, PileType, Player?, CardPilePosition)` (`CardPileCmd.cs:267`). **Generated mid-combat cards MUST use `AddGeneratedCardToCombat`, never plain `Add`.** There is no `SetCostForCombat`/`ModifyCost`/`CostForTurn` — the whole surface is `CardEnergyCost`. Shipped templates: `Crossbow.cs` (random Attack to hand, free this turn), `Discovery.cs` (pick 1 of 3, free, to hand).
- **1-of-N card choice**: `CardSelectCmd.FromChooseACardScreen(PlayerChoiceContext, IReadOnlyList<CardModel>, Player, bool canSkip = false)` → `Task<CardModel?>` (`CardSelectCmd.cs:252`). Throws if more than 3 cards; calls `UndoEndTurnIfNecessary(player)`, which is what makes it safe to open from a turn-end hook. Takes no prompt argument, so no `selectionScreenPrompt` loc key.
- **`CardPilePosition`** = `{None, Bottom, Top, Random}`; `Random` resolves through `Rng.Shuffle` (`CardPileCmd.cs:508-511`).
- **Relics DO receive damage hooks** — `RunState.IterateHookListeners` adds `player.Relics.Where(r => !r.IsMelted)` alongside deck cards, potions, modifiers and badges, and `Hook.ModifyDamageInternal` multiplies every listener's `ModifyDamageMultiplicative` into one running product. Verified because `OddMushroom` depends on it.
- **Damage truncates, it does not round**: `Creature.LoseHpInternal` does `(int)Math.Clamp(amount, 0m, 999999999m)` (`Creature.cs:449`). Any fractional multiplier composed into damage must account for this.
- **`Player.GetRelic<T>()`** (`Player.cs:532`) is the clean ownership test; `Player.Relics` is `IReadOnlyList<RelicModel>`. `RelicCmd` surface: `Obtain<T>(Player)`, `Obtain(RelicModel, Player, int index = -1)`, `Remove(RelicModel)`, `Replace(RelicModel, RelicModel)`, `Melt(RelicModel)`.

### Deliberately NOT implemented this session (never fake these)
- **`NlothsGift`** ("triple the chance of finding Rare cards from combat rewards") — NO hook exists in core or BaseLib. `CardRarityOdds` bakes the odds in as consts/statics (`MegaCrit.Sts2.Core.Odds/CardRarityOdds.cs`), `CardCreationOptions` only exposes `WithRarityOdds(CardRarityOddsType)` / `WithFilter` / `WithFlags`, and the only rarity hook on `AbstractModel` is `ModifyMerchantCardRarity` (shops only, `AbstractModel.cs:1794`). `AbstractOdds.OverrideCurrentValue` is public but `Roll` mutates the pity counter afterwards, so using it would permanently distort future rewards. Leave N'loth's relic branch locked.
- **`RedMask`, `Circlet`, `RegalPillow`** — StS2 ships all three (`.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Relics/`), so they are reused via `ModelDb`/`RelicCmd.Obtain<T>()`, never reimplemented. Note `Circlet` is `RelicRarity.None` and `IsStackable`, which is exactly StS1's "you already have it" fallback relic.

### Known inexact clauses, each FLAGged in its source file
- `OddMushroom`: StS1's `VulnerablePower.atDamageReceive` early-returns a flat `damage * 1.25`; StS2 has no registration point (`VulnerablePower.cs:42-56` hard-codes `PaperPhrog`/`CrueltyPower`/`DebilitatePower`, each with a non-virtual `ModifyVulnerableMultiplier`, `PaperPhrog` sealed). The relic therefore returns `1.25m / live` from its own `ModifyDamageMultiplicative`, i.e. it forces the flat 1.25 exactly as StS1 does, which necessarily cancels any StS2-only amplifier (Cruelty on the attacker, Debilitate on the holder). **A "halve the live bonus" formula was written first and then REJECTED on review**: that formula exists in neither game, so it would be invented behaviour, and it would leave the holder taking 1.5x under Debilitate while the relic text promises "25% ... rather than 50%". A `+1e-28m` ulp nudge is required because `1.25m/1.5m` rounds down and damage truncates — verified numerically: without it every damage value divisible by 4 comes out 1 short (500 of the first 2000), with it the result matches StS1 for 1..2000 and never exceeds it in 1..200000.
- `Necronomicon`: StS1 skips cards with `freeToPlayOnce`; StS2 cannot see it — `ModifyCardPlayCount` gets no `CardPlay`/`ResourceInfo`, and `BeforeCardPlayed` (which does) runs at `CardModel.cs:1926`, after `GeneratePlayCount` at `:1887`. An auto-played free 2-cost Attack will trigger where StS1 skips.
- `MutagenicStrength`: the HP/damage behaviour is exact, but StS1's visible "lose 3 Strength at end of turn" debuff icon is not reproduced (it would need a `TemporaryStrengthPower` subclass, i.e. a new power class).
- `MarkOfTheBloom`: a cancelled heal still plays the heal sfx and shows a `0`, because `CreatureCmd.Heal` has no zero-amount early return and BaseLib patches inside it.

### Phase 2 log — event unlocking (all 10 event relics landed, so the relic-blocked branches opened)
Dispatched as four parallel opus5/high writers on disjoint files plus one reviewer on the coordinator's own judgment calls; the coordinator did the unowned files itself and built centrally.
- **`CursedTome`** grants one of the three books. Bytecode-exact: candidates in the fixed order Necronomicon / Enchiridion / Nilry's Codex, each added only when unowned, then a single random INDEX (`list.get(miscRng.random(size - 1))`, and StS1's `Random.random(int)` is an INCLUSIVE bound), falling back to one `Circlet` when all three are owned. So 1/3 each with none owned, 1/2 with one owned, certain with two.
- **`MoaiHead`** offers the Golden Idol for exactly **333 gold** (`private static final int goldAmount = 333`), removing the relic FIRST then paying — and StS1 always renders the slot, switching to "[Locked] Requires: Golden Idol." rather than hiding it.
- **`GoldenIdolEvent`** grants the idol **before** the boulder page, not as a survival reward (`spawnRelicAndObtain` at offset 119 precedes `screenNum = 1` at 139). `Circlet` if somehow already owned.
- **`ForgottenAltar`** — the two `gainChalice()` paths are **NOT symmetric**, which is the easy bug here: when the Bloody Idol is already owned StS1 only spawns a `Circlet` and **keeps** the Golden Idol; only the else path unequips the Golden Idol and `instantObtain`s the Bloody Idol via `player.relics.set(idx, this)`, i.e. a slot substitution. `RelicCmd.Replace` is the exact 1:1 primitive (`Remove` + `Obtain` at the original index). Its metrics call `logMetricRelicSwap` on the already-owned path is misleading — ignore it.
- **`DrugDealer`** `[Ingest Mutagens]` grants Mutagenic Strength (`Circlet` if owned) and is **free** — no damage/gold/max-HP call anywhere in `buttonEffect`. `[Test J.A.X.]` was unlocked earlier in the session and is also free.
- **`AccursedBlacksmith`** `[Rummage]` grants the `Pain` curse FIRST and then **`WarpedTongs`** — the relic is HARD-CODED in StS1, not a random tier and not `Circlet`-guarded.
- **`MindBloom`**: `[I am Awake]` upgrades every upgradable card then grants Mark of the Bloom with **no** `Circlet` fallback (verified: `spawnRelicAndObtain` only special-cases an incoming relic whose own id IS "Circlet"). `[I am Rich]` gives exactly **999 gold** and **two separate** Normality instances. `[I am Healthy]` heals an amount equal to max HP (not a set-to-max). StS1's option order is War / Awake / (Rich|Healthy).
- **`TombRedMask`** (coordinator, unowned by any writer): `[Don the Red Mask]` pays **222 gold** and requires already holding the mask; `[Offer: {gold} Gold]` loses ALL gold and grants the shipped `RedMask`. The option title splices the live gold via `!Gold!`, which is why StS1's `OPTIONS[2]`/`[3]` are two fragments.
- **`Bonfire`** (coordinator): the curse filter is GONE — offering a Curse now grants `SpiritPoop`, or `Circlet` when Spirit Poop is already owned, matching the CURSE arm of StS1's rarity switch. Curses are no longer excluded from the selection.
- **`WindingHalls`** (coordinator, found by the reviewer): `[Focus]` now adds the shipped `Writhe` as well as healing.
- **`FaceTrader`** stays LOCKED, and the FLAG now names the blocker exactly: StS1's `getRandomFace()` is a uniform roll over the unowned faces (**not** the 50/50 the option text advertises) among `CultistMask`, `FaceOfCleric`, `GremlinMask`, `NlothsMask`, `SsserpentHead`. **0 of 5 exist** in this mod or in shipped StS2 (searched the whole of `.tmp/dllsrc`; StS2 ships only `RedMask`, `FuneraryMask`, `JeweledMask`, `GremlinHorn`, `Circlet`). Granting `Circlet` alone would replace a five-way roll with a guaranteed no-op — a 100% wrong probability table, worse than a locked option.
- **`Nloth`**, **`MaskedBandits`**: FLAGs sharpened rather than resolved. `Circlet` is available now, so the only blockers are `NlothsGift` (unimplementable) and the unported encounter respectively.
- `Necronomicurse` was written from scratch (`mod/Spire1Code/Cards/Necronomicurse.cs`) so `Necronomicon` no longer has to flag its pickup curse. **Both of StS1's return paths are gated on still holding the relic**, which is what makes losing the relic the intended escape: `BeforeCardRemoved` puts a fresh copy back in the deck, `AfterCardExhausted` returns a temp copy to hand, `AfterTransformedFrom` covers transformation. The relic grants it in `AfterObtained` and strips every copy in `AfterRemoved`.

### Phase 3-4 log — quality gate and efficiency pass
Three reviewers ran in parallel. Two died on provider-side transport errors after doing the work; their findings were recovered over IRC and the coordinator re-verified every load-bearing claim itself.
- **Event review: zero findings** across all 20 modified events (P0-P3 all empty). The coordinator independently ran a mechanical loc-key audit over all 52 events with an exact port of the engine's `Slugify` (`([A-Za-z0-9]|\G(?!^))([A-Z])` → `$1_$2`, upper-invariant, strip `[^A-Z0-9_]`) and reached the same result. Note `IAmAwake` slugs to `I_AM_AWAKE`, and a naive CamelCase splitter gets this wrong.
- **Relic review found one real defect, fixed**: `MutagenicStrength` wrote its per-combat latch directly instead of through a setter calling `AssertMutable()`. On a canonical model that silently corrupts state shared by every player's clone instead of throwing `CanonicalModelException`. Now routed through a property, matching shipped `ThrowingAxe.cs:11-21`. The coordinator then audited all 11 new relics: **no collection instance fields anywhere**, every hook has an owner guard as its cheapest-first test, and `Necronomicon` already used `AssertMutable`.
- **Efficiency pass — the big speculative finding was disproven, and this matters for future sessions**: `CanonicalVars`, `CanonicalKeywords` and `CanonicalTags` are each read **exactly once per model instance** behind a lazy cache (`CardModel.cs:538-549` and `:507-518`; `RelicModel.cs:296`), and `Localization` is read once per model at registration (`ModelLocPatch` is a Harmony postfix on `ModelDb.Init`). So the 300+ `=> [new DamageVar(6)]` collection-expression bodies are **NOT** a performance problem — do not "optimize" them.
- **And caching them would be a BUG**: `DynamicVarSet` stores the `DynamicVar` references by reference with no clone and then calls `SetOwner` on them, so they become the live per-instance vars the engine mutates (`UpgradeValueBy`, `BaseValue`, `ResetToBase`). A shared cache would make upgrading one card upgrade every copy.
- **One real efficiency finding, fixed**: `Clash.cs` and `SignatureMove.cs` used `CardPile.GetCards(Owner, PileType.Hand)` in `IsPlayable`, which is `piles.SelectMany(p => p.GetPile(player).Cards)` — a `params PileType[]` plus a `SelectMany` enumerator allocated on every read, and `IsPlayable` is re-evaluated by `CanPlay` on every hand cost/glow/end-turn UI refresh. Now `PileType.Hand.GetPile(Owner).Cards`, which is an O(1) switch to a cached pile over a backing `List`. Behaviour identical.
- **Non-finding, recorded so it is not re-investigated**: the typed `DynamicVars.Damage`/`.Block`/`.Heal` accessors are the SAME dictionary lookup as `DynamicVars["Damage"]` (`DynamicVarSet.cs:11-43`), so converting string-keyed sites buys nothing in v0.111.0.

### Content totals after this session
305 card classes, 33 relics, 49 powers, 53 event classes. `cards.json` 673 keys, `events.json` 655 keys. Build **0 errors**, deployed to `mods/Spire1/` (`Spire1.dll` + `Spire1.pck`).

### NEXT WORK, in order
1. **In-game smoke test (USER)** — see the verification note below. Nothing else is blocked on it, but it is the only outstanding proof.
2. **M2 monsters.** This is now the single largest blocker: it is what keeps `Colosseum`, `MaskedBandits`, `MysteriousSphere`, `Mushrooms` `[Stomp]`, `DeadAdventurer`'s elite, `MindBloom` `[I am War]` and `SpireHeart` locked or omitted. Exact StS1 act encounter tables are in `DEVELOP.md` 7d. Still blocked on monster visuals — either reuse a shipped StS2 monster scene or commission art. Decide which before writing code.
3. **`Madness`** (`WindingHalls`) and the **five StS1 face relics** (`FaceTrader`) are the only remaining content gaps that are NOT blocked on monsters or on a missing engine API. Both are straightforward to write; the face relics need `CultistMask`, `FaceOfCleric`, `GremlinMask`, `NlothsMask`, `SsserpentHead` extracted from the jar first, the way `research/sts1data/relics.json` was built.
4. Relic art: every mod relic currently shares one placeholder icon. Cosmetic, disclosed, not a bug.

### Verification state — BE HONEST ABOUT THIS
Everything above is from builds and file reads that actually ran; the build is 0 errors and deployed. **No in-game smoke test has been run this session.** StS2 launches via Steam DRM, its UI cannot be driven automatically, and a running game locks `Spire1.dll`, so the build must happen before launching. Per `../AGENTS.md` §2 the visual/interactive smoke test belongs to the user. Worth eyeballing, in rough order of risk:
1. **Cursed Tome `[Take]`** — should hand over one of the three books, and a second visit with one owned should never re-offer it.
2. **Forgotten Altar `[Offer: Golden Idol]`** — the Golden Idol should be REPLACED in place by the Bloody Idol (same relic-bar slot); with the Bloody Idol already owned you should get a Circlet and KEEP the Golden Idol.
3. **Odd Mushroom** — take a hit while Vulnerable and confirm the damage matches +25%, not +50%.
4. **Necronomicon** — pickup should hand you a Necronomicurse; removing that curse at a shop should see it come straight back; melting/losing the relic should let the curse go.
5. **Mind Bloom `[I am Rich]`** — 999 gold and exactly two Normality.
6. **Tomb of Lord Red Mask** — the offer option's title should show your live gold.
7. Earlier session's items, still unverified: Council of Ghosts granting Apparitions, Vampires granting 5 Bites, The Nest granting a Ritual Dagger, The Mausoleum granting Writhe, Mushrooms granting a Parasite.

## ===== SESSION 5 (2026-08-21) — LIBRARY RECON + M2 UNBLOCKED =====

No code was written this session. It was a **reconnaissance session**, and it overturned two of the three things `DEVELOP.md` recorded as hard blockers. Read this before planning anything.

### 5.1 HEADLINE: M2 monsters were never actually blocked
`DEVELOP.md` said custom monsters were blocked on "unported StS1 monster encounters — needs a visuals decision first". That premise was wrong. BaseLib ships the whole surface, and it is present in the **shipped v3.3.5 binary**, not just in source:
- `Abstracts/CustomMonsterModel.cs` — `CustomVisualPath` (:21), `CreateCustomVisuals()` (:39), `SetupCustomAnimationStates(MegaSprite)` (:53), `CustomAttackSfx`/`CustomCastSfx`/`CustomDeathSfx` (:42-44), `SetupAnimationState(...)` static helper (:74).
- Also present: `CustomEncounterModel`, `CustomActModel`, a **real non-placeholder `CustomCharacterModel`**, `CustomOrbModel`, `CustomPetModel`, `CustomRestSiteOption`, `CustomReward`, `CustomBadge`, `CustomAncientModel`, `CustomEnchantmentModel`, `CustomModifierModel`.

**Two independent visual routes, both cheap:**
1. **Reuse a shipped StS2 monster scene.** `CustomMonsterModel.CustomVisualPath` defaults to `res://scenes/creature_visuals/<modname>-<class_name>.tscn`, and `SceneHelper.GetScenePath` resolves against the **base game's** `res://`, so pointing it at a shipped scene works. This is precisely the trick `PlaceholderCharacterModel.cs:12` already uses for our four characters (`CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + PlaceholderID)`). **121 shipped monster classes** exist in `.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Monsters/`, so there is a scene for nearly any StS1 silhouette.
2. **Build visuals from a single PNG.** `NCreatureVisualsFactory` constructs an `NCreatureVisuals` from a plain `Texture2D` — no `.tscn`, no Spine rig. (Found by `BaseLibDeepScout`; exact signature in `research/BaseLib-unused-surface.md`.)

Consequence: `Colosseum`, `MaskedBandits`, `MysteriousSphere`, `Mushrooms [Stomp]`, `DeadAdventurer`'s elite, `MindBloom [I am War]` and `SpireHeart` are all **implementable now**. StS1 act encounter tables are already extracted in `DEVELOP.md` §7d. This is the highest-value next milestone and it needs no new dependency.

### 5.2 Face relics + Madness — data landed, ready to implement
`research/sts1data/face-relics-and-madness.json` (20547 bytes, validated: parses, field order matches `relics.json[0]` exactly, 0 diffs on the `Madness` base fields vs `cards-colorless.json`). 5 relics + 1 card, each with exact constants, decompiled `behavior` prose, official English `loc`, and an `sts2_api_risk` field that is really a **worked-out implementation route with file:line**. All five relics are `RelicRarity.Event`, which is also what keeps them out of random rolls (`RelicFactory.RollRarity` only returns Common/Uncommon/Rare, `RelicFactory.cs:80-93`).

**Five bytecode-verified corrections — each one inverts the obvious reading. Do not lose these:**
1. **`GremlinMask` applies 1 Weak to the PLAYER.** It is a downside relic ("Start each combat with 1 Weak."). Cloning shipped `RedMask` would invert it. Mirror `RedMask.cs:23-30`'s hook shape but retarget from `combatState.HittableEnemies` to `Owner.Creature`.
2. **`FaceOfCleric`** — StS1's `increaseMaxHp(1, true)` never reads its boolean (zero `iload_2` in the body) and always heals. StS2's `CreatureCmd.GainMaxHp` already heals (`CreatureCmd.cs:853`), so ONE call matches; adding a heal would double it. Hook is `AfterCombatVictory(CombatRoom)` (`AbstractModel.cs:556`), **not** `AfterCombatEnd` (`:520`, also fires on defeat).
3. **`NlothsMask`** removes only the first RELIC reward from the next non-boss chest (plus its Sapphire Key link). **The chest still pays its gold** — `AbstractChest.open` adds the gold reward before the relic loop and never removes it. The official word "empty" is a misnomer. Candidate hook `AbstractModel.ShouldGenerateTreasure(Player)` (`AbstractModel.cs:2325`) may suppress the whole chest; if so that is a behavioural difference to FLAG, not hide.
4. **`SsserpentHead`** must test `MapPointType.Unknown`, **NOT** `room is EventRoom`. StS1's `onEnterRoom` sees `EventRoom` for every `?` node because the relic loop runs before the event is rolled, whereas StS2 resolves `?` to Monster/Treasure/Shop up front (`RunManager.cs:985`) — a room-class test would silently skip those nodes. `GOLD_AMT = 50`.
5. **`CultistMask` has no gameplay effect at all** — purely cosmetic ("You feel more talkative"). Its only StS1 literals are `TalkAction` timing floats. Do not invent an effect, and do not ship StS1 audio.

`Madness`: cost 1, colorless SKILL, UNCOMMON, SELF, Exhaust, upgrade drops base cost to 0. The logic lives in `MadnessAction`, not the card — it collects hand cards with cost > 0 and picks among those (not a naive uniform pick over the hand), and guards X-cost with `if (cost != -1)`. StS2 side: `CardModel.SetToFreeThisCombat()` (`CardModel.cs:1273`) is the candidate, **not** `SetToFreeThisTurn()` (`:1267`); the granular `CardEnergyCost` setters (`SetUntilPlayed`/`SetThisTurnOrUntilPlayed`/`SetThisTurn`/`SetThisCombat`) are the fallback if neither is exact.

### 5.3 Third-party libraries — verdicts
The user has **RitsuLib 0.5.13** and **JmcModLib** installed from the workshop. Full artifacts: `research/RitsuLib-api.md`, `research/JmcModLib-api.md`, `research/BaseLib-unused-surface.md`.

RitsuLib (`STS2-RitsuLib`, MIT, 1325 public types, ships an exact `lib/0.111.0/` build **with XML docs**):
- **M2 monsters — not needed.** BaseLib suffices. RitsuLib only helps if we ship our own StS1 PNGs (`VisualCueSet`/`VisualFrameSequence`, `CueAnimationBackend`, `ModAnimStateMachineBuilder`). Its `ModMonsterMoveStateMachines.HeadThenRepeatTail/Cycle/RandomEntry/ConditionalEntry` does map 1:1 onto StS1 move patterns, which is genuinely convenient.
- **`Necronomicon`'s `freeToPlayOnce` — YES, cleanest win.** `Cards.FreePlay.FreePlayBindingRegistry` (`IsFreeForPlay(CardPlay)`, `Resolve(CardPlay)`, `MarkCardFreeNextPlay/ThisTurn/ThisCombat`, `IsCardFreeForUpcomingPlay(CardModel)`) already observes vanilla via an internal patch on `CardModel.SetToFree*`. Ordering vs `GeneratePlayCount` is `[UNVERIFIED]`.
- **Per-relic icons — YES.** `ExternalAssetOverrideRegistry.RegisterRelicIconPathProvider/...TextureProvider` + `RuntimeAssetRefreshCoordinator.RequestRelicsWhere`, model-agnostic, should cover BaseLib relics. This is the fix for our one-placeholder-icon cosmetic gap.
- **M3 act selector — PARTIAL but promising.** `ModContentRegistry.RegisterActEnterForce<TAct>(slotIndex, priority, Func<ActEnterResolveContext,bool>)` replaces a numbered act slot when a predicate passes, and `ActEnterResolveContext` carries `RunManager`/`RunState`/`EnteringActIndex`/`Rng`/`UnlockState`/`IsMultiplayer` — enough to gate on "is this an StS1 dungeon run". Co-op handoff via `RunSavedDataLobbyScope<T>` + `RunSavedDataLobby` staging. Character select has **no** purpose-built hook, only generic node-attachment/screen tooling.
- **`N'loth's Gift` — flat NO.** Zero rarity-roll surface. See 5.4.
- **Character skins — unproven.** `RegisterCharacterAssetReplacement(characterId, CharacterAssetProfile)` is keyed by ID string, so it could override individual slots while we keep `PlaceholderCharacterModel`. But precedence over BaseLib's own getters is `[INFERENCE]` from 27 `[HarmonyAfter("BaseLib")]` patches, and `MesugakiRegentSkinFix` — the one RitsuLib-dependent skin mod — uses none of that surface. Needs a smoke test before anyone believes it.
- **Risks:** pre-1.0 (231 NuGet versions), applies its own Harmony set alongside BaseLib, embeds an update-check URL and a PostHog-style telemetry endpoint (opt-in per `TelemetryConsentState`, silence-before-consent **not** runtime-verified), MIT declared in the nuspec but no licence file shipped. **Recommendation: adopt only for `Necronomicon` + relic icons + M3 act slots. Do not adopt for M2.**

### 5.4 `N'loth's Gift` — possibly not impossible after all
Session 4 recorded it as impossible ("no hook exists in core or BaseLib; `CardRarityOdds` bakes the odds in as consts/statics"). `BaseLibDeepScout` found that **`CardRarityOdds.RollWithoutChangingFutureOdds(type, offset)` is public and `Roll` is a public patchable instance method**, and was verifying which method combat rewards actually call when it died. If they call the public seam, a Harmony patch (BaseLib already bundles Harmony — RitsuLib not required) makes it implementable. Verdict is in `research/BaseLib-unused-surface.md`; check it before re-closing this as impossible.

### 5.5 Other live findings
- **`Girya`** — BaseLib explicitly flags it incomplete for its rest-site option, and StS2 ships `LiftRestSiteOption` and `DigRestSiteOption`. There is a real fix; details in `research/BaseLib-unused-surface.md`. This is an open FLAG in our tree.
- **Version skew, important:** `research/BaseLib-StS2/` is git tag **v3.4.5** but the DLL the game loads is **v3.3.5**, and **73 types exist only in source** — including `IModifyScryAmount`, `IAfterScryed`, `ICardTypeTextModifier`. **Never conclude an API is usable from the source tree alone; confirm it in the shipped binary.** `BaseLibDeepScout` wrote an ECMA-335 metadata parser for exactly this; RitsuLib's equivalent dumps are at `.tmp/ritsu/` (`mdparse.mjs`, `api-0.111.0.json`, `sec-all.txt`, `nsindex.md`, `refs.mjs`, `hpatch.mjs`, `allpatches.txt`).
- **Character skins are `.pck` asset overrides.** `silentSkin`/`necrobinderSkin` ship **no DLL at all** — pure `.pck` overrides of vanilla asset paths. That, not a C# API, is how the workshop re-skins characters.
- **`MutagenicStrength`'s missing temp-Strength icon needs no new capability** — BaseLib `Abstracts/CustomTemporaryPowerModel.cs:24` is already `ITemporaryPower` with `InternallyAppliedPower`/`OriginModel`/`UntilEndOfOtherSideTurn`/`LastForXExtraTurns`.
- **JmcModLib** (workshop `3747526103`; `JmcModLib.Runtime.dll` + `.pck` + bundled `Newtonsoft.Json.dll` + `BuildTools/scripts` + `dispatch`) — verdict in `research/JmcModLib-api.md`.

### 5.6 PROCESS LESSON — five agent deaths, root cause identified
**Five research agents died with `exit 1` at the exact moment they finished researching and began writing their artifact**: `JmcModLibScout` (29m), `RitsuLibApiScout` (1h04m), `BaseLibDeepScout` (1h17m), then both salvage agents sent to recover them — `SalvageBaseLib` (5m07s, "I have the complete final report from the transcript. Writing the artifact now.") and `SalvageRitsu` (5m53s, "All material in hand... Writing the artifact."). The 5-minute deaths prove it is **not** a context-length or long-run problem.

**Root cause: the crashes are specific to `opus5high` subagents, and they land on the large single write.** The one agent in the batch that succeeded, `SalvageJmc`, wrote a small 3070-byte file. This is provider-side instability, not a bug in our code and not something the agent did wrong.

**Dispatch rules that follow — apply to every future agent:**
1. **Write a skeleton artifact to disk within the first few minutes, then append ONE section per call, keeping each call under ~4 KB.** Never compose a large document in one write. A crash then costs one section instead of everything.
2. **Never let an agent research to exhaustion holding the payload in memory.** Incremental disk writes are the only durable state.
3. **Transcripts survive agent release** and are readable via `history://<id>` (`read history://` lists every agent). This is the recovery path, and it worked — nothing was permanently lost.
4. **A dying agent can still be interrogated.** An agent that exits `exit 1` goes *idle*, not dead: `hub send` wakes it and it will dump its findings as prose. That recovered `RitsuLibApiScout`'s entire verdict set (§5.3) after its artifact write failed. Do this **before** the agent is released, because release loses the live context and leaves only the transcript.
5. **Match the model to the work.** As of this session new subagents run `deepseek-v4-flash-0731`. Flash is reliable for mechanical, narrowly specified work — verbatim transcription, stat-only cards, localization JSON — but synthesis-heavy jobs (read a 1h17m transcript, judge what matters, write a nuanced artifact) exceed it. Keep judgment, decomposition and integration on the main agent; give flash agents precise specs, exact data, and an instruction to FLAG rather than invent.

### 5.7 SHIPPED THIS SESSION — face relics + Madness, build 0 errors, deployed
`FaceTrader` and `WindingHalls` are no longer FLAGGED; the last non-monster content gap is closed. Six new content classes, both events wired, **build 0 errors, deployed to `mods/Spire1/` at 01:45** (`Spire1.dll` 661504 B, `Spire1.pck` 305442 B; all six type names confirmed present in the deployed DLL by byte scan).

New files: `Relics/{CultistMask,FaceOfCleric,GremlinMask,NlothsMask,SsserpentHead}.cs`, `Cards/Madness.cs`. All five relics are `RelicRarity.Event`, which keeps them out of random rolls automatically (`RelicFactory.RollRarity` returns only Common/Uncommon/Rare). Relic loc is fully covered by `RelicLoc` in code, so `_staging/face-relics-loc.json` came out `{}`.

Hooks used, each verified against `.tmp/dllsrc`:
- `CultistMask` — `AfterRoomEntered(AbstractRoom)` (`AbstractModel.cs:1153`) + `room is CombatRoom`. Chosen over `BeforeSideTurnStart` because the core doc-comment (`:1147-1148`) names this hook for start-of-combat effects that must run before the first turn, matching StS1 `atBattleStart`. Cosmetic only — no mechanical effect, faithfully.
- `FaceOfCleric` — `AfterCombatVictory(CombatRoom)` (`:556`), NOT `AfterCombatEnd` (`:520`, fires on defeat too). Single `CreatureCmd.GainMaxHp(Owner.Creature, 1m)`; it heals internally at `CreatureCmd.cs:853`, so +1 Max HP and +1 current HP come from one call. `Owner.Creature.IsDead` guard = StS1's `!isDying`.
- `GremlinMask` — `BeforeSideTurnStart(...)` (`:1247`), guarded by `participants.Contains(Owner.Creature) && TurnNumber <= 1` exactly as shipped `RedMask.cs:23-30`, but `PowerCmd.Apply<WeakPower>` targets **`Owner.Creature`** (downside relic), with `HoverTipFactory.FromPower<WeakPower>()`.
- `NlothsMask` — `ShouldGenerateTreasure(Player)` (`:2325`, veto-style per `Hook.cs:2325-2334`). One-shot `[SavedProperty] HasConsumedTreasure` through an `AssertMutable()` setter, `MawBank.cs:23-41` shape. **The gold-still-paid clause needed no FLAG after all**: `TreasureRoom.EnterInternal` calls `BeginRelicPicking` at room entry (`TreasureRoom.cs:47`) which hits `ShouldGenerateTreasure` first (`TreasureRoomRelicSynchronizer.cs:105`) and spends the charge there, while the later chest-open path (`OneOffSynchronizer.cs:129`) sees the spent charge, returns true and pays gold (`:138`) — exactly StS1's "gold yes, relic no". Non-Boss is automatic: the hook is only reached from TreasureRoom paths.
- `SsserpentHead` — `AfterRoomEntered` (`:1153`) + `Owner.RunState.CurrentMapPoint?.PointType == MapPointType.Unknown` + `PlayerCmd.GainGold(50m, Owner)`. `MapPointType.Ancient` deliberately excluded (StS1 has no such point type).

`Madness` decisions worth keeping:
- Cost setter is **`pick.EnergyCost.SetThisCombat(0)`** (`CardEnergyCost.cs:238`), not `CardModel.SetToFreeThisCombat()` (`CardModel.cs:1273`) — the latter additionally calls `SetStarCostThisCombat(0)` (`:1274`), zeroing StS2 star costs that StS1's Madness never touches. `SetToFreeThisTurn()` (`:1267`) is the wrong scope entirely.
- Selection filter `!c.EnergyCost.CostsX && c.EnergyCost.GetResolved() > 0`. The `CostsX` guard is genuinely required in StS2: `GetResolved()` reads `CapturedXValue` (`CardEnergyCost.cs:155-162`), so a replayed X-cost card in hand could be picked, yet `GetWithModifiers` returns `_base` early for `CostsX` (`:105-108`) making the write inert. StS1's `cost != -1` guard has the same purpose.
- `Rng.NextItem` returns null on an empty sequence (`Rng.cs:296-298`), a clean no-op matching `MadnessAction`'s silent `tickDuration()` exit when nothing qualifies.
- FLAG kept: StS1 gold-flashes the picked card (`superFlash(Color.GOLD)`); `CardModel` exposes no Flash API, so the reduction applies without a card flash.

Event wiring:
- **`FaceTrader.[Trade]` costs nothing** — confirmed from bytecode: the Trade branch calls only `getRandomFace()` then `spawnRelicAndObtain`, with `damage()`/`gainGold()` appearing solely in the Touch branch. `getRandomFace()` collects the unowned faces in source order, appends `Circlet` only when that list is empty, shuffles with `new Random(miscRng.randomLong())` and takes element 0 — a uniform draw, so `Rng.NextInt(count)` reproduces it exactly (same mapping `CursedTome` already uses). The advertised "50%: Good Face / 50%: Bad Face" is flavour text.
- **`WindingHalls.[Embrace Madness]` grants exactly 2 Madness** — StS1 queues two independent `ShowCardAndObtainEffect(new Madness(), x, y)` calls; the ±350*xScale offsets are only screen positions. `CardPileCmd.AddCursesToDeck` cannot be used (it throws `ArgumentException` for any non-Curse, `CardPileCmd.cs:1262-1265`), so the deck-add primitive is used directly, matching `Necronomicurse.cs:100`. The existing option text "Receive 2 Madness" is now true.

Loc: `cards.json` 673 → 675 keys (`SPIRE1-MADNESS.title/.description`); `events.json` 655 → 656 (added `SPIRE1-FACE_TRADER.pages.TRADE.description`, converted from StS1 `DESCRIPTIONS[3]` with this repo's rules — `NL` → space, `~word~`/`@WORD@` emphasis and `#y`/`#r`/`#g` colour codes stripped — and cleared the stale "not yet ported" requirement note on the TRADE option).

### 5.8 Three library interface documents now exist — ~340 KB in `docs/`
Per user request, standing API references live in **`docs/`**, separate from the adopt/skip analyses in `research/`. All three are complete and were built with the skeleton-plus-small-appends rule from §5.6, which held: zero crashes.
- `docs/BaseLib-API.md` — **109997 B / 833 lines / 9 sections.** The most authoritative artifact in the repo: **every signature was copied from a real `ilspycmd` decompile of the SHIPPED v3.3.5 `BaseLib.dll`** (dumped to `.tmp/baselib-dll/`), not from the source tree. 28 subsections covering every `Abstracts/` base class, the interface set, visuals and assets, localization, hooks, utilities, patches, and a version-skew table. **111 SHIPPED/SOURCE-ONLY markers, type-level status programmatically verified** by diffing an `ilspycmd -l cise` dump of the installed DLL (614 full type names) against the source tree. Corrections it establishes: the source-only count is **69, not 73** (methodology explained in its §9, and `RelicCollectionTranspiler` is no longer in the tree); `CustomMessage`/`CustomTargetedMessage` **do not exist as classes** in either the binary or the v3.4.5 tree — only `CustomMessageWrapper`/`CustomTargetedMessageWrapper` plus `ICustomMessage`/`ICustomTargetedMessage`; `NodeFactory.UnregisterSceneType`/`HasFactory`/`IsRegistered` and `CustomCharacterModel.DefaultCompendiumOpenModelId` are confirmed **absent from the shipped binary**.
- `docs/RitsuLib-API.md` — 93215 B / 1064 lines / 10 sections. All 92 public namespaces with type counts (1325 public types), exact signatures for `ModContentRegistry` (~120 members), `ExternalAssetOverrideRegistry` (40 providers), the free-play registry, act-enter forcing, the 58-type `Networking.Sidecar` index, all 40 Harmony patches (35 `[HarmonyAfter("BaseLib")]`, 6 `[HarmonyBefore]`, 4 at priority 800), and 18 BaseLib interop points. Flags `BaseLibMaxHandSizeBridge` as documented-but-internal, i.e. unusable. Has an explicit "what could not be read" appendix.
- `docs/JmcModLib-API.md` — 136786 B / 11 sections. **All 602 XML-documented members**, cross-verified against a reflection dump of the shipped `JmcModLib.Runtime.dll` (90 exported types). Availability breakdown: **565 binary-public, 25 internal type, 4 protected, 7 private, 1 internal method** — so ~6% of the documented surface is not actually callable, which is exactly the kind of trap this doc exists to prevent. Also documents the dispatch build toolchain as a reusable technique for the multi-game-version problem.
- `research/RitsuLib-api.md` (116427 B) and `research/BaseLib-unused-surface.md` (57769 B) hold the salvaged verdicts. `research/JmcModLib-api.md` was rewritten by hand — see 5.9.

**TOOLING CHANGE — `ilspycmd` now works.** `DEVELOP.md` §9 previously carried "decompile only if a private member is needed — ilspycmd install currently broken". It is installed and functional at `C:\Users\o_Obl\.dotnet\tools\ilspycmd.exe` and was used successfully this session. Decompiling a shipped DLL is now the cheapest way to settle a shipped-vs-source question; prefer it over reasoning from a source tree. That stale note has been removed from `DEVELOP.md` §9.

### 5.9 JmcModLib — the scout's provenance was wrong, verdict still SKIP
`JmcModLibScout` reported the workshop item as zero-filled stubs with no `content/` directory. **It had merely not finished downloading.** `G:/steam/steamapps/workshop/content/2868840/3747526103` exists with real bytes, including **`JmcModLib.Runtime.xml`, 206083 B, 602 documented members** — the single most authoritative artifact, never consulted by that run. Full upstream source is also on disk at `.tmp/jmc/`.

Re-verified directly against the XML: **zero** documented members mention `Monster`, `Encounter`, `ActModel`, `Character`, `CardRarity`, `CardPool`, `RelicModel`, `PotionModel`, `Reward` or free-play. It is a settings-UI / reflection / logging / secrets / persistence / version-compat library (`Config.UI` 116 members, `UI.PauseMenu` 46, `Reflection.*` 74, `Utils.ModLogger` 23, `Core.ModRegistry` 18, `Security.*` 39, `Persistence` 8) plus a multi-version dispatch build toolchain. Its 33 "multiplayer" members are all `MultiplayerCompat.TryGet*` cross-version accessors, not transport. **Verdict unchanged: do not adopt.** Lesson: when an agent's conclusion rests on "the files are empty", re-check the disk yourself.

### 5.10 NEXT WORK, in order
1. **M2 monsters** (5.1) — the big one, now unblocked. Take the shipped-scene route first; it needs no art. Encounter tables are in `DEVELOP.md` §7d. Start from `docs/BaseLib-API.md` §2.11 (`CustomMonsterModel` — **SHIPPED**) and §2.12 (`CustomEncounterModel`).
2. Resolve `N'loth's Gift` (5.4) and `Girya` (5.5) from `research/BaseLib-unused-surface.md` §3 and §4.
3. Decide on RitsuLib using `docs/RitsuLib-API.md`. If adopted it buys `Necronomicon`'s exact `freeToPlayOnce`, per-relic icons, and the M3 act selector.
4. **M3 has a concrete starting point, do not re-derive it.** `docs/BaseLib-API.md` §2.13: `CustomActModel` is **SHIPPED**, and its constructor is `protected CustomActModel(int actNumber, bool autoAdd = true)` where BaseLib's own doc says **"Set to -1 to prevent your act from spawning naturally. Otherwise, use 1/2/3 for the corresponding act."** That `-1` is exactly the primitive the dungeon selector needs: register all four StS1 acts as non-spawning, then have the selector place them. RitsuLib's `RegisterActEnterForce<TAct>(slotIndex, priority, predicate)` (`docs/RitsuLib-API.md` §3) is the other half if we adopt it. The one genuinely open M3 question remains character select, which has no purpose-built hook in either library.

### Verification state — BE HONEST ABOUT THIS
**Build: 0 errors, rebuilt and deployed at 01:45.** Six new type names confirmed present in the deployed `Spire1.dll` by byte scan; loc merges verified by key count and round-trip read. Everything else in §5.1–§5.6 is from decompiled/binary reads that actually ran, with `[UNVERIFIED]`/`[INFERENCE]` markers preserved in the artifacts.

**No in-game smoke test has been run** — StS2 launches under Steam DRM, its UI cannot be driven automatically, and a running game locks `Spire1.dll`. Per `../AGENTS.md` §2 the interactive smoke test belongs to the user. New items to eyeball, on top of session 4's still-outstanding checklist above:
1. **Face Trader `[Trade]`** — should cost nothing and hand over one random face relic; with all five owned it should give a Circlet instead.
2. **Gremlin Visage** — should apply 1 Weak to **you** at combat start, not to enemies.
3. **Face Of Cleric** — +1 Max HP after a *won* combat only, and current HP should rise with it.
4. **N'loth's Hungry Face** — the next non-boss chest should yield **no relic but still pay its gold**, and the relic should then show as spent.
5. **Ssserpent Head** — 50 gold on entering a `?` node, including ones that resolve into a fight, shop or chest.
6. **Winding Halls `[Embrace Madness]`** — exactly 2 Madness into the deck; playing one should zero a random hand card's cost for the rest of the combat (no gold flash — known cosmetic omission).

The salvaged artifacts were written by agents from transcripts — spot-check a citation or two before building on them.
