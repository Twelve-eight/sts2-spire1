# DEVLOG - sts2-spire1

Recovery anchor + working state. Design/contracts: `DEVELOP.md`. Shared conventions: `../AGENTS.md`. Resumable with zero prior chat.
**Sessions 1-3 are archived in `DEVLOG-archive.md`.** This file keeps the live STATUS / latest sessions only.

## STATUS (2026-08-21, session 6)
- **Single dependency: BaseLib 3.4.5** (installed in-game AND compiled against; md5-verified identical to the NuGet payload). RitsuLib/JmcModLib rejected. `Spire1.json` declares exactly `[{"id": "BaseLib", "min_version": "3.4.5"}]`.
- M4 content complete (4 characters, 305 cards, 33 relics, 49 powers, 53 events) and deployed. **M2 monsters in flight**: shared bases + `Exordium` act + dungeon-selector patch landed; 6 subagents writing monsters/encounters right now.
- **git initialized this session** (first commit `9bcfc06`); durable research artifacts moved from `.tmp/` into `research/` (`engine-dllsrc/`, `baselib-dll/`, `sts1-javap/`).
- Outstanding user smoke test: face relics + Madness checklist (DEVLOG archive §5.7) — still not run.

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

## ===== SESSION 6 (2026-08-21) — SINGLE-DEPENDENCY, BaseLib 3.4.5, M2 MONSTERS =====

### 6.1 BaseLib 3.4.5 installed — a live compile/runtime mismatch is now closed
- Discovered: we compiled against NuGet `Alchyr.Sts2.BaseLib` **3.4.5** while the game loaded **3.3.5**. Any source-only API would have compiled cleanly and thrown at load. Ironclad got away with it only because it happens to use shipped APIs.
- User downloaded the official build (`I:/Downloads/BaseLib.3.4.5.zip`); its three files are **md5-identical** to the NuGet package's `Content/`+`lib/net9.0/` payload (we compile against exactly this build). Installed into `mods/BaseLib/`; 3.3.5 kept at `mods/BaseLib-3.3.5-backup/`. Game was not running during the swap.
- Consequence: the whole 3.4.5 surface is now legal at runtime; `docs/BaseLib-API.md` §9's skew table is history, not a constraint.

### 6.2 Single dependency, decided with evidence
- `mods/BaseLib/BaseLib.json` declares `dependencies: []` — the earlier "runtime deps BaseLib+RitsuLib" note in DEVELOP.md §0 was simply wrong.
- Our code references zero Ritsu/Jmc symbols; `Spire1.json` declares exactly `[{"id": "BaseLib", "min_version": "3.4.5"}]`.
- RitsuLib rejected; its two headline benefits dissolved under inspection: relic icons need no second library because `RelicModel.IconBaseName/PackedIconPath/BigIconPath` are all virtual (engine `RelicModel.cs:128-140`) — the same donor trick characters and monsters use; and Necronomicon's `freeToPlayOnce` maps onto engine `CardModel.SetToFreeThisTurn()` → `EnergyCost.SetThisTurnOrUntilPlayed(0)` (`CardModel.cs:1267-1271`).

### 6.3 Monster/encounter contract, nailed against the binaries
- `CustomMonsterModel` is BaseLib's ONLY content base without `ILocalizationProvider`, so our `Spire1Monster` adds it; `ModelLocPatch` maps category `MonsterModel` → table `monsters`, so `LocTable` stays null. Loc is in-code via `MonsterLoc` (keys become `moves.<STATE_ID>.title`).
- Visuals: `Spire1Monster.DonorId` → `SceneHelper.GetScenePath("creature_visuals/" + DonorId)`; BaseLib's `VisualsPath.cs` patch substitutes it. Engine default animator already matches shipped rig convention — no animation work per monster.
- Encounters attach via `IsValidForAct`; BaseLib postfixes `GenerateAllEncounters` (must be *declared* — it is abstract in `ActModel`, so every act qualifies) and appends custom encounters whose `IsValidForAct` accepts the act.
- `act.Index = -2` (from `CustomActModel(-1)`): engine reads `.Index` in one place only (`ModelDb.cs:334`) behind `if (Index >= 0)` — negative acts stay out of natural rotation safely. `CustomActModel.AllAncients` THROWS on non-basegame index (overridden in Exordium); `BaseNumberOfRooms` falls back to 15 harmlessly.
- Landed: `Monsters/Spire1Monster.cs`, `Monsters/Spire1Encounter.cs`, `Acts/Spire1Act.cs`, `Acts/Exordium.cs` (surviving artifact from the killed wave — shipped act-1 art paths from Overgrowth, AllAncients/rooms/map-point overrides, empty encounter list by design), `Config.UseSts1Dungeon` toggle (default OFF), `Patches/DungeonSelectionPatch.cs`.

### 6.4 The dungeon selector turned out to be one patch, not a subsystem
- StS2 has no act-sequencing API and needs none: `NGame.StartNewSingleplayerRun(character, shouldSave, acts, ...)` takes the run's act list as a parameter ("The canonical acts that should be in the run") and hands it to `RunState.CreateForNewRun`, which walks it by list position via `CurrentActIndex`. Choosing a dungeon = rewriting that one argument.
- One prefix patch covers character-select/custom/daily singleplayer. Multiplayer deliberately NOT patched yet: co-op has a second direct `RunState.CreateForNewRun` call site (`NCharacterSelectScreen.cs:745`) and per-client config substitution would desync a lobby. Host-authoritative choice is M3 work.
- Completeness gate added after review feedback: until all four StS1 acts exist, the selector refuses to substitute (a one-act run would end after Exordium's boss). Fail-safe default: vanilla act sequence.

### 6.5 M2 fan-out — and what killed the first wave
- First wave (opus5): GremlinLouseWriter died after 18m20s having written NOTHING — transcript shows solid extraction work, then death mid-synthesis. Root cause matches DEVLOG §5.6: batching writes to the end. Two more socket deaths followed.
- Second wave (same model as main agent), six slices: SlimesW / GremlinsW / HumanoidsW / LousesW(→LousesW2 after a socket death; the dead agent's extracted louse data was salvaged via IRC and archived to `mod/_staging/louse-extracted-data.md`) / BossesW / EncountersW. Anti-crash rule in every brief: write each file the moment its data is extracted; report ≤2 KB.
- Reusable javap dumps archived to `research/sts1-javap/` (MonsterHelper, AbstractDungeon, AbstractRoom, absmon, lice, nob, gremlinfat, anger).

### Engineering debt paid this session (user-approved review items)
- **git initialized** (first commit `9bcfc06`); `.gitignore` excludes .tmp/.nuget/caches/build output; nested research obj/bin removed from index.
- Durable artifacts moved out of `.tmp/`: decompiled trees now at `research/engine-dllsrc/` (3538 cs) + `research/baselib-dll/` (452 cs); javap dumps at `research/sts1-javap/`. `.tmp3/` deleted.
- DEVLOG split: sessions 1-3 → `DEVLOG-archive.md`; live file starts with a STATUS header.

### Verification state — BE HONEST ABOUT THIS
- Central build green AFTER base classes landed (0 errors, before the worker wave). NOT yet rebuilt with Exordium + patch (Exordium references three encounter classes being written now).
- No in-game smoke test of anything from this session yet. The face-relics checklist from session 5 is still outstanding with the user, who is awake and available — hand it over when the wave lands and the build is green.

## Session 7 — M2 wave landed, cross-reviewed, fixed (2026-08-21)

### 7.1 What exists now
- **25/25 Act-1 monsters** on disk under `mod/Spire1Code/Monsters/` (AcidSlime L/M/S, SpikeSlime L/M/S, Cultist, JawWorm, FungiBeast, Looter, SlaverBlue/Red, Sentry, Lagavulin, LouseNormal/Defensive, GremlinWarrior/Thief/Fat/Shield/Wizard/Nob, TheGuardian, Hexaghost, SlimeBoss) + shared infra (`SlimeSplit.cs`, `ISlimeSplitSpawn.cs`) and new powers (`SporeCloudPower`, `SlimeSplitPower`, `AngryPower`, `ModeShiftPower`, `SharpHidePower`).
- **20/20 encounters** in `mod/Spire1Code/Encounters/` (14 Monster / 3 Elite / 3 Boss), gold overrides on elites 25-35 / bosses 95-105.
- Central build: **0 errors** (214 pre-existing nullable warnings, all in old Events/Relics/Cards code).

### 7.2 Cross-review round (6 reviewers) — verdicts & adjudications
- ReviewHumanoids: FAIL → 4 P1 (Lagavulin AsleepPower hard-cast crash; Lagavulin post-wake branch inverted; Sentry predicates self-locking BOLT; SporeCloud Owner.Player NRE) + 4 P2. All fixed (writer rewrote Lagavulin/Sentry/SporeCloud; main agent patched compile fallout).
- ReviewEncounters: FAIL → 3 mechanical blockers (missing ctor chaining CS7036; Localization not `override` CS0534; key "name" never read — engine reads `.title`/`.loss`). Main agent batch-fixed all 20 files via script.
- ReviewLouses: PASS-with-1-blocker → green louse missing A17+ "Spit Web never back-to-back" guard. Fixed with weight lambda mirroring red louse pattern.
- ReviewGremlins: FAIL → P1 (AngryPower trigger) **REJECTED after javap adjudication**: vanilla `AngryPower.onAttacked(DamageInfo,int)` EXISTS in desktop-1.0.jar with exactly the implemented gate (owner!=null && dmg>0 && !HP_LOSS && !THORNS). Reviewer had confused it with `AngerPower` (the card power). 4 accepted: Wizard escape wiring orphaned; Shield Protect loop ignores escapeNext; Protect pool must exclude self (GainBlockRandomMonsterAction bytecode); Fat Blunt invalid Attack trigger.
- ReviewSlimes: FAIL → Acid L/M attack pairing swapped vs bytecode (Slimed belongs on the 11/12 & 7/8 attacks); Spike lick caps inverted base/A17+; SpikeL missing A17 3-Frail. All fixed by writer.
- BossesW delivered Guardian/Hexaghost/SlimeBoss (split = 1×SpikeSlimeL + 1×AcidSlimeL per bytecode, NOT ×2) and flagged a real runtime crash in SlimeSplit: `new T {}` is canonical/immutable → `CreatureCmd.Add` AssertMutable throws. Fixed: `(T)ModelDb.Monster<T>().ToMutable()` then set SpawnHp.

### 7.3 Main-agent fixes after writer termination (perf)
- JawWorm AI rewritten from bytecode getMove: band picker 25/30/45 + conditional sub-rolls (0.5625 after Bellow→56.25% THRASH; 0.357 after THRASH²→35.7% CHOMP; 0.416 after CHOMP→41.6% CHOMP), first move CHOMP, per-turn cached sub-roll via `base.Rng.NextFloat()`.
- SlaverRed Entangle now 25%/turn (`RollHundred() >= 75`, once per combat), STAB-after-entangle gated `_stabRun < 2 && roll >= 55`.
- A17/A18-only values dropped to base (Looter gold 15, SlaverBlue Weak 1, SlaverRed Vuln 1; Lagavulin debuff already -1 const) with comments — StS2 caps at A10 so those tiers are unreachable; gating them at A9 silently changed reachable balance.

### 7.4 Engine facts learned this session
- `AddBranch(state, 0, N, Wf)` binds the `(state,int cooldown,int maxRepeats,Func<float>)` overload — a bare float weight does NOT convert; use `() => Wf`. maxRepeats=0 = never repeat.
- Branch states (ConditionalBranchState/RandomBranchState) MUST be registered in the state machine's states list or FindNextMoveState throws "no valid state found".
- `RollMove` draws from `RunRng.MonsterAi`; per-combat seeded `MonsterModel.Rng` is available for sub-rolls.
- Donor rigs don't all follow idle_loop/cast/attack/hurt/die: fat_gremlin has awake_loop/FleeTrigger/WakeUpTrigger only; lagavulin_matriarch has sleep_loop/eyes_closed tracks; torch_head_amalgam & slimed_berserker lack cast. Use BaseLib `SetupAnimationState(controller, idle, dead, hitName:, attackName:)` overrides; unknown triggers no-op with Log.Warn (silent visual gap).
- `CreatureCmd.SetMaxAndCurrentHp(creature, decimal)` exists (used by SlimeBoss split inheritance).
- Encounter loc keys are `.title`/`.loss` (EncounterLoc record); monster loc keys are `.name` + `moves.<STATE_ID>.title`.

### 7.5 Re-review verdicts + post-verdict fixes (2026-08-21 evening)
- ReReviewB-2 (JawWorm/SlaverRed/Gremlins): FAIL, 3 findings — ALL CONFIRMED by javap and fixed:
  - JawWorm `JAW_WORM_BANDS` had no outgoing state and `BAND_PICKER` was never wired → would throw "No valid next state found" on turn 2+. Fixed: `bands.AddState(bandPicker, () => true)` after declaration.
  - JawWorm band A/C history conditions inverted vs bytecode (band A guards on last CHOMP → 56.25% BELLOW; band C is the BELLOW band → 41.6% CHOMP). Fixed to match jawworm.txt truth table.
  - SlaverRed base-tier scrape guard must be `lastTwoMoves(SCRAPE)` (two scrapes allowed); the single-move guard is A17+-only (dropped per policy). Replaced `_lastWasScrape` bool with `_scrapeRun` counter (<2), maintained in Stab/Entangle (reset) and Scrape (++).
- ReReviewA-2 (slimes/lagavulin/lices/sentries): FAIL, 6 findings — adjudicated against fresh javap dumps (.tmp/acidslimes.txt, lousedef.txt, lousenorm.txt, spikeslimes.txt, slaverblue.txt):
  - AcidSlimeL/M weight tables swapped both tiers vs bytecode. Truth: L base 30/40/30, L A17+ 40/30/30; M base 30/40/30, M A17+ 40/40/20. Fixed. (Reviewer's repeat-cap numbers were wrong — vanilla uses lastTwo/lastMove conditional sub-rolls, not flat caps; documented in comments.)
  - Lagavulin damage-wake STUN never consumed a turn: stun MoveState lacked `MustPerformOnceBeforeTransitioning` (engine's own Creature.StunInternal sets it). Fixed.
  - LouseDefensive/LouseNormal AI inverted: vanilla is two deterministic history-map branches on one roll (`<25: lastMove(WEB|GROW)?BITE:X; >=25: lastTwo(BITE)?X:BITE`), long-run ~80% BITE / ~20% debuff-buff. Rewritten as nested ConditionalBranchState with WebGuard/GrowGuard (base lastMove guard, A17+ lastTwo guard via DeadlyEnemies gate) + per-turn cached LastSubRoll(0.25).
  - SpikeSlimeL/M A17+ caps reversed: base tackle max2/lick max1; A17+ tackle max1/lick max2. Fixed both files.
  - SlaverBlue never double-stabbed: bytecode yields cycle S,S,R (stab max2/rake max1, first move 60/40). Fixed AddBranch(stab,2)/AddBranch(rake,1).
- Post-fix independent review (FixReviewA/FixReviewB) dispatched over all touched files.
- Build green (Debug + Release); Release auto-deployed to game mods dir 20:59 (Spire1.dll/.pck).

### 7.6 Second-round review (FixReviewA/B) — 10 findings, all confirmed, all fixed
- P0 crash class: LouseDefensive/LouseNormal/SpikeSlimeL/M left MoveStates without
  `FollowUpState` → engine `MoveState.GetNextState` throws "No valid followup state." on the
  first post-first-move RollMove (player turn 2). Wired every move state back to its root.
- JawWorm band A else-branch routes to CHOMP (bytecode off 101-123), not BELLOW; fixed +
  stale class-doc truth table rewritten (56.25/43.75, 35.7/64.3, 41.6/58.4).
- SpikeSlimeL/M maxRepeats: base tackle2/lick2, A17+ tackle2/lick1 (earlier table had the
  base >=30 guard as lastMove(LICK); bytecode is lastTwoMoves — main agent's error, reviewer caught).
- SlaverBlue: STAB weight is 60% (num>=40), rake max2 (base lastTwo(RAKE)); first move random
  via initial state = roll. Fixed from 40/60 + stab-max2/rake-max1 + fixed first move.
- AcidSlimeL/M comment truth tables corrected (guards: base <70 lastMove(TACKLE)→40%WOUND/60%WEAK;
  A17+ <70 lastTwo(TACKLE), >=70 lastMove(LICK)).
- SlaverRed write-only `_lastWasScrape` removed (counter `_scrapeRun` drives the guard).

### 7.7 Verification state
- In-game interactive smoke still blocked for the agent (Steam DRM launch, UI not drivable,
  running game locks Spire1.dll); deployed artifacts are CURRENT (20:59). Smoke checklist handed to user.

## Session 8 — four-act dungeon shell, config localization, launch incident (2026-08-21 night)

### 8.1 Second-round review (FixReviewA/B) — 10 findings, all confirmed vs bytecode, all fixed
- P0 crash class: LouseDefensive/LouseNormal/SpikeSlimeL/M left MoveStates without
  `FollowUpState` → engine `MoveState.GetNextState` throws "No valid followup state." on the first
  post-first-move RollMove (player turn 2). Every move state now wired back to its root.
- JawWorm band A else-branch routes to CHOMP (jawworm.txt off 101–123), not BELLOW; class doc
  truth table rewritten (56.25/43.75 · 35.7/64.3 · 41.6/58.4).
- SpikeSlimeL/M maxRepeats corrected: base tackle2/lick2, A17+ tackle2/lick1 — the base >=30
  guard is `lastTwoMoves(LICK)` (main agent's earlier table was wrong; reviewer caught it).
- SlaverBlue: STAB weight is 60% (`num>=40`), rake max2 (base lastTwo(RAKE)); first move random
  via initial state = roll.
- AcidSlimeL/M comment truth tables fixed (base <70 lastMove(TACKLE)→40%WOUND/60%WEAK;
  A17+ <70 lastTwo(TACKLE), >=70 lastMove(LICK)).
- SlaverRed write-only `_lastWasScrape` removed.
- Commits: `0c02dfc` (M2 wave + fixes), rebuilt Debug+Release, deployed 20:59.

### 8.2 Four-act dungeon shell (commit `fcb9ad2`)
- `TheCity.cs` / `TheBeyond.cs` / `TheEnding.cs`: shipped act2(hive)/act3(glory) art+music+banks;
  bytecode room counts (weak 2 + strong 12 for City/Beyond; The Ending has NO normal fights in
  vanilla — generateMonsters empty); encounter pools EMPTY until M2.5 ports their monsters.
  Ancients: borrowed StS2 Act2/Act3Ancients (engine requires non-empty; StS1 has no ancients).
- `DungeonSelectionPatch`: full four-act sequence; the old "must have all 4 acts" gate removed.
  Normal rooms tolerate an empty pool (AddWithoutRepeatingTags null-checks), but act 2–4 boss
  floors WILL fail until their bosses exist. TheEnding is explicitly a placeholder shell.
- Engine facts nailed: BaseLib config label key = `settings_ui:{PREFIX}{SLUG}.title`, hover tip =
  `.hover.desc` (required) + `.hover.title`; PREFIX = uppercase root namespace + '-' ("SPIRE1-");
  SLUG = StringHelper.Slugify(propertyName) (CamelCase→SNAKE_CASE). Act Id = Slugify(class name)
  → acts.json keys EXORDIUM/THE_CITY/THE_BEYOND/THE_ENDING .title. Game locales incl. zhs
  (confirmed by scanning SlayTheSpire2.pck tail index). Rng.NextItem(empty)=null;
  GenerateRooms fills events from AllEvents + ModelDb.AllSharedEvents.
- `settings_ui.json` + `acts.json` written eng+zhs under mod/Spire1/localization/.

### 8.3 Launch incident (commit `04eb5f7`)
- User launched game via Steam → fatal error dialog. Cause: `mods/BaseLib-3.3.5-backup/` was
  scanned as a mod and loaded FIRST (v3.3.5 incompatible with game 0.111.0 → Harmony patch
  exceptions), then real v3.4.5 failed with duplicate-id. FIX: moved backup dir out of mods/
  (now at `<game>/BaseLib-3.3.5-backup-REMOVED-from-mods`). Relaunch: BaseLib 3.4.5 loaded,
  Spire1 initialized clean.
- User smoke: entered Exordium floor 1 fine (Watcher, placeholder Regent art — expected).
- Known cosmetic log noise: missing `res://images/ui/run_history/spire1-*_encounter*.png`.

### 8.4 OPEN INCIDENT — next session starts here
- User selected character **StS1-Silent** with ALL mod settings enabled → **game error**.
  Log excerpt saved to `DEVLOG-crash-snapshot.txt` (repo root, committed). NOT yet diagnosed.
  First suspects: Silent's card/relic pool init touching content gated by config flags, or
  character-select path requiring assets not in pck. NO fixes attempted yet per user hold.

## Session 9 — Silent crash root-caused & fixed; zhs localization + loc-debug mode (2026-08-21 night)

### 9.1 Silent StS1-dungeon start NRE — root cause chain (fixed, commit `3806762`)
- Symptom: selecting StS1-Silent with all settings on → "内部错误" NRE dialog. Watcher worked
  earlier only because that run went to vanilla UNDERDOCKS (UseSts1Dungeon was off then).
- Chain: our boss encounters ship no run-history icons → engine fallback path
  `images/ui/run_history/<id>.png` lives OUTSIDE the `Spire1/` pck prefix → preload marks it
  failed → `NTopBar.RefreshBossIcon` (via RitsuLib's `Initialize_Patch3`) asks AssetCache for the
  same path → `AssetLoadException: previously failed to load` → aborts `NGlobalUi.Initialize`
  mid-way → `NMapScreen.Initialize(runState)` never runs → `_runState == null` →
  `NMapScreen.SetMap` NREs at `map_jitter_{_runState.CurrentActIndex}`.
- Fix: `Spire1Encounter` overrides `CustomRunHistoryIconPath/OutlinePath` to
  `res://Spire1/images/run_history/{id.ToLowerInvariant()}.png`; shipped 40 placeholder 1×1 PNGs
  (20 encounters × main/outline) in pck. GOTCHA: BaseLib hands us `Id.Entry` UPPERCASE
  (`SPIRE1-THE_GUARDIAN_ENCOUNTER`) — Godot pack lookups are case-sensitive, must lowercase.
  Second launch confirmed in-game: Silent + all-on + StS1 dungeon starts fine.
- Residual log noise (harmless): `NBossMapPoint._Ready` still asks for
  `animations/map/spire1-*_encounter/*_skel_data.tres.png` (Spine atlas fallback when no .tres);
  exception is caught by Godot and play continues. Real Spine scenes are M3+ art work.

### 9.2 zhs localization wave + loc-debug mode (in flight)
- Data prep done: unpacked official StS1 zhs json from desktop-1.0.jar (cards/powers/relics/
  events/monsters). Auto-mapped via normalized-name index: cards 309/331, powers 40/50,
  relics 34/37 official names; remainder need hand translation (beta-era cards like Claw,
  Rushdown, Pressure Points never shipped in StS1 zhs).
- Worklists: `.tmp/card-zhs-worklist.json`, `.tmp/cards-zhs-draft.json` (602 keys pre-filled),
  `.tmp/cards-zhs-missing.json`, `.tmp/powers-eng.json`, `.tmp/relics-eng.json`.
- 4 parallel workers spawned: ZhsCards (zhs/cards.json), ZhsPowersRelicsChars,
  ZhsEvents (656 keys), LocDebugMode (`LocTable.GetRawText` postfix appending key to SPIRE1-
  strings behind new `Spire1Config.DebugShowLocKeys` toggle — gives 中文名 (SPIRE1-X.title)
  in-game for console testing).

### 9.3 Engine facts learned this session
- `LocManager.GetTable(name)` / `LocTable.GetRawText(key)` is THE text path; tables keyed by
  filename (cards/events/...), mod files merge into same tables under `SPIRE1-` prefixed keys.
- `EncounterModel.MapNodeAssetPaths`: if `BossNodePath` (.tres) exists → preload tres; else asks
  for `tres.png` + `_tres_outline.png`. `BossNodePath` is virtual per encounter.
- `ImageHelper.GetRoomIconPath` is patched by BaseLib `RoomIconPathPatch`: non-null
  `CustomRunHistoryIconPath` short-circuits vanilla path. Same for outline variant.
- `AssetCache` poisons failed paths: any later `GetTexture2D` on them throws
  "Asset previously failed to load" instead of returning null — this is what turns a cosmetic
  missing icon into a startup crash when RitsuLib's TopBar patch propagates it.

## Session 10 — card dedup + slime state-machine freeze fix (2026-08-22)

### 10.1 Duplicate-card audit & dedup (commits `62478e3`)
- `DupCardsAudit` compared all 306 mod cards vs StS2's 596: **A 105 same-name-same-effect /
  B 24 same-name-different-effect / C 177 StS1-only** → `.tmp/duplicate-cards-report.md`.
  Starter-deck audit extended it: every Ironclad/Silent/Defect starter (Strike/Defend variants,
  Bash, Neutralize, Survivor, Zap, Dualcast) also has an identical native StS2 model; Watcher
  does NOT exist in StS2, so its whole kit stays modded.
- Dedup mechanism: new `Spire1LegacyPool` sink pool (no character references it, IsShared=false
  → never surfaces anywhere). The 111 duplicated cards now `[Pool(typeof(Spire1LegacyPool))]`;
  their SPIRE1-* ids stay resolvable so old saves keep loading. Starter decks of
  Ironclad/Silent/Defect and the NoteForYourself event's IronWave use fully-qualified native
  `MegaCrit.Sts2.Core.Models.Cards.*` models. B/C untouched.
- GOTCHA: `[Pool]` is Inherited=true — removing the attribute falls back to the base class's
  pool, so a sink pool attribute must be explicit. BaseLib `AddModel` throws without any Pool.

### 10.2 Combat freeze: "No valid followup state" on turn 2 (commit `f5f7261`)
- Symptom: LotsOfSlimes fight, enemy turn completes but turn loop dies before player turn 2.
- Stack: `PrepareForNextTurn → RollMove → FindNextMoveState → MoveState.GetNextState` throws.
  Engine semantics (`MonsterMoveStateMachine.FindNextMoveState`): each turn calls
  `GetNextState()` on the CURRENT state; MoveState returns `FollowUpState?.Id ?? throw`.
  Turn 1 rolls from initialState; turn 2 asks the last MOVE for its followup — AcidSlimeS/M/L
  and SpikeSlimeL moves had none → crash. Session 8 fixed Louses/SpikeSlimeM only.
- Fix: wire every move back to its AI root (tackle/lick/spit → ai; split → ai as a safe
  fallback even though split removes the monster). Final sweep: ALL monsters wired.
- Split-spawn safety confirmed: `SetUpForCombat()` regenerates the state machine per creature,
  so `ToMutable()` children get fresh machines.

### 10.3 Static review findings (no game launch; user busy with CoD)
- LOW: AcidSlimeS uses AddBranch maxRepeats=1 → forbids consecutive same-move even on base
  difficulty (StS1 base is a free 50/50). Cosmetic deviation, revisit if desired.
- Known log noise: `NBossMapPoint._Ready` still throws a caught exception per map screen over
  missing `animations/map/spire1-*_skel_data.tres(.png)` — path has no Spire1/ prefix so our
  PckPacker cannot ship it; needs real Spine scenes or a BaseLib BossNodePath patch (none today).
- Run-history icons are 1×1 transparent placeholders: invisible in library/history screens.
- LocDebug postfix runs `StartsWith("SPIRE1-")` on every GetRawText call — acceptable overhead.
- A combat that died to the old bug stays stuck until the room is restarted (engine behavior);
  saves from before the fix may hold such rooms.

## Session 11 — Darv NRE, full art conversion, dual review (2026-08-22 overnight)

### 11.1 Darv (ancient) event freeze — fixed (`03ae5d1` + `7c98579`)
- Act-2 ancient **达弗/Darv** froze after its first line. Log: NRE at
  `DustyTome.SetupForPlayer ← Darv.GenerateInitialOptions`. Chain: DustyTome rolls a random
  Ancient-rarity card from `player.Character.CardPool`; our placeholder pools contain only
  SPIRE1-* cards (none Ancient) → `NextItem(empty)` → null → `.Id` throws; the exception kills
  `BeginEvent`, so the event screen never opens.
- Fix: `DustyTomeAncientFallbackPatch` prefix — if the character's own pool has no Ancients,
  roll from the native pool of the PlaceholderID stand-in instead (ironclad/silent/defect/
  **regent** for Watcher — the regent mapping was the P1 gap EffReview caught). Guarded against
  an empty fallback list too. StS1 has no Ancient rarity; borrowing the native pool's is the
  intended stopgap until legacy ancients are ported as cards.

### 11.2 Full art conversion from desktop-1.0.jar (`6128311`)
- Unpacked all 2188 images; System.Drawing (PowerShell) batch pipeline in `.tmp/convert-*.ps1`.
- Cards: 320 auto-mapped by normalized name + 11 manual (relic-styled cards, potion cards,
  beta-era cards) = **331/331** card_portraits at 250×190 + big 1000×760 (StS1 ships 500×380;
  scaled ±2× bicubic).
- Relics: **36/38** classes mapped to relics/*.png (94×94), outline/ (94×94), largeRelics/
  (256×256, falls back to upscaling the small art when a large variant doesn't exist).
  FaceOfCleric=clericFace; MutagenicStrength has no StS1 counterpart (kept placeholder).
- Powers: 30 auto + 20 semantic substitutes (StS1 buff icons live only inside texture atlases,
  not standalone files) = 50 icons at 64×64 + 256×256, center-square-cropped from power-card
  illustrations.
- Potions: 8 potion CARDS composed from layered glass+liquid atlases (StS1 composites these
  at runtime); outlines from the glass layer.
- NOT converted: monster battle sprites (Spine atlas pieces — need skeleton data to pose;
  M3 real-rig work), event illustrations (StS2 events are scene-based), charui (BaseLib
  routes those through PlaceholderID to shipped assets already).
- Final pck: 893 art entries, 7.0 MB.

### 11.3 zhs text defects found via log triage (`bf7ff73`, `a1c2bb2`)
- "Found end tag center, expected G" tooltip errors: 22 zhs keys carried orphaned StS1 energy
  tags `[G]/[R]/[B]/[W]` — StS2 parses `[G]` as an unclosed BBCode color tag. Replaced with
  `*能量*`. All 17 localization JSONs now scan clean of legacy tags.
- 183 zhs card descriptions contained literal " NL " separators (StS1 line-break syntax);
  replaced with real `\n`.

### 11.4 Dual review (both reports in .tmp/)
- Efficiency (`review-efficiency.md`): no P0. P1 Watcher regent gap → fixed. P2s: FilterAncient
  allocation acceptable (once-per-run, not worth caching keyed on UnlockState);
  LocDebug config-read could snapshot to a field if init order ever changes; stray decompile
  scratch files removed.
- Security (`review-security.md`): **no Critical/High**. Low ×3 — floating `Version="*"` deps
  (fix: pin + lock file), BBCode option-title prefixes `[离开]` trip ParseBbcode in
  CallDeferred (caught, cosmetic; fix: escape or use 【】), PckPacker packs everything except
  four extensions (currently clean, add whitelist target). Zero network/process/shell APIs in
  the codebase; path joins use compile-time constants only.

## Session 12 — Act2/3/4 full migration, multi-path review, deploy (2026-08-23 morning)

### 12.1 Night batch landed (uncommitted → this session's commits)
- **Act2 The City** (20 monsters, 17 encounters, HomeActs=[2]): Byrd, Centurion, Chosen,
  SnakePlant, Snecko, Bandit trio, Mugger, TorchHead, BookOfStabbing, SphericGuardian,
  Healer, Taskmaster, BronzeOrb/Automaton, TheCollector, Champ.
- **Act3 The Beyond** (17 monsters, 17 encounters, HomeActs=[3]): AwakenedOne, TimeEater,
  Donu/Deca, Nemesis, Darkling, Maw, WrithingMass, Transient, OrbWalker, SpireGrowth,
  Spiker/Repulsor/Exploder, SnakeDagger, GiantHead, GremlinLeader, ShelledParasite,
  Reptomancer. Custom powers: Fading/Shifting/Regrow/PlatedArmor (+Constricted later).
- **The Ending**: SpireShield/SpireSpear/CorruptHeart + ShieldAndSpear/CorruptHeart
  encounters (HomeActs=[4], Boss via BossDiscoveryOrder). NOTE: an earlier worker had
  claimed these landed but never wrote them — re-dispatched and verified on disk.
- Build triage 280→0 errors: batch using-fixer over Monsters/, MoveRepeatType namespace,
  Nemesis scythe local→field promotion, TimeEater Heal(creature,amount) signature,
  ConditionalBranchState has no FollowUpState (WrithingMass), ConstrictedPower written on
  CustomPowerModel (AfterSideTurnEnd(choiceContext, side, participants) signature).

### 12.2 Multi-path review (4 reviewers; concurrency capped at 3 per user)
Findings fixed in this session:
- **P0 WrithingMass**: six reroll branch states had no exit → engine throws "No valid next
  state" (~60% of turns). Fixed: each reroll AddState(bands, () => true).
- **P1 AwakenedOne**: phase pickers used Turn<25/<50 instead of a per-turn roll — added the
  Champ-style RollHundred() (one cached 0-99 roll per round).
- **P1 Maw**: turnCount only bumped during opening evaluation (once) — NOM hits were stuck
  at 1. Bumped inside bands' first predicate instead.
- **P1 Snecko**: main-loop predicate chain inverted vs bytecode (BITE nearly unreachable).
  Restored glare→tail(roll<40)→tail(lastTwoWere(bite))→bite.
- **P1 Act4 missing** — see 12.1; also E-fixes below.
- Encounters: AwakenedOneEncounter is 3-body (2 Cultists + boss, monsterhelper_full.txt);
  new WrithingMassEncounter (elite, act 3) + run_history pair; Shapes encounters restored
  vanilla randomness (with-replacement multiset draws / independent getAncientShape rolls);
  SpireGrowth/Transient weak variants deleted (vanilla strong-pool-only); GremlinLeader
  minions now drawn WITH replacement (fresh 8-entry multiset per slot).
- Powers: TimeWarp counts statuses/curses too AND gives every monster +2 Strength on
  trigger (both were missing); Fading fires at owner-side turn START (vanilla duringTurn:
  at 1 stack die without acting) — Transient now attacks 4× not 5×; Constricted damage is
  blockable (StS1 THORNS passes block) and localized via PowerLoc convention.
- Bytecode fidelity P2/P3: BookOfStabbing A18 growth on the LastTwoWere path + initial
  state = branch (first turn joins the roll); GiantHead lastTwo(GLARE) guard + intent-time
  preview property (vanilla decrements count before setMove); Centurion initial state =
  branch; TheCollector weights 26f/45f/29f; Maw ROAR + Snecko GLARE DebuffIntent(strong);
  Reptomancer dagger slots: after-self → daggers[0]; Spiker comment package beyond.
- Act4 review fixes: CorruptHeart Debilitate no longer increments moveCount (vanilla early
  return); BUFF uses raw buffCount tableswitch so 5th+ buff = Strength +50; SpireSpear
  BURN_STRIKE intent shows ×2 hits.

### 12.3 Deploy blocker found & removed
- `mod/Spire1/scenes/rest_site/*.tscn` (untracked leftovers against the established
  RestSiteBackgroundPatch decision) made PckPacker skip packing entirely ("unsupported
  files detected") — deployed .pck was stale since Aug 22. Deleted the directory;
  pck packs clean and auto-copies again. New run_history placeholder pairs shipped for
  writhing_mass / shield_and_spear / corrupt_heart encounters.

### 12.4 State
- Release build 0 errors; Mods/Spire1/{Spire1.dll,Spire1.pck} fresh (2026-08-23 11:46).
- Known flags (documented in file headers): engine lacks Invincible/BeatOfDeath powers
  (CorruptHeart opens without them); Surrounded uses Kaiser Crab directional variant;
  CorruptHeart is a solo boss per bytecode (no summon phase exists in StS1 either).

## Session 13 — 竞品调查：(BETA BRANCH ONLY) Acts from the Past (2026-08-23)

工坊物品 [3746969593](https://steamcommunity.com/sharedfiles/filedetails/?id=3746969593)，作者 Cany0udance，
v1.0.5（更新 07-30），111.45MB，479 评价；已下载到
`G:\steam\steamapps\workshop\content\2868840\3746969593`（json+pck 105MB+dll 1.1MB），**未装入 mods/**。
开源：github.com/Cany0udance/ActsFromThePast，作者明示允许参考/复用代码。依赖 BaseLib >= 3.3.6、min_game_version 0.109.0、**仅 public-beta**（本机 appmanifest BetaKey=public-beta buildid 24724944，满足）。

### 范围对比（反编译 DLL 全类型清单）
- AFTP = 三幕（Exordium/City/Beyond，无 The Ending）+ 全部 StS1 敌人/遭遇 + ~52 事件 + 17 遗物（含全部 5 张脸面具 + N'loth's Gift）+ 8 事件卡（含 Madness）+ 29 敌方 power + 音乐/SFX/全套 StS1 立绘动画 VFX（LibGdxAtlas 解析 .atlas + 自建动画类，非 Spine）。**无角色/玩家卡池/职业内容**。
- 本项目独有：4 角色、305 卡、33 遗物、药水、The Ending 第 4 幕、选单地牢 selector、运行时开关。
- 双方在怪物/遭遇/事件上已达实质等价（本侧 Session 12 已落地三幕+第四幕全部敌人遭遇）。

### 他们解决了我们的三个缺口（可直接借鉴）
1. **N'loth's Gift**：Harmony Transpiler+Prefix 打 `CardRarityOdds.RollWithoutChangingFutureOdds(CardRarityOddsType, float offset)` —— Prefix 按 `baseRareOdds*3` 改写 offset（不污染 pity 状态），Dup+CaptureRoll 捕获 roll 用于闪光。推翻本文件早前"无钩子可用"结论（当时只看了 `Roll`）。
2. **五脸面具+FaceTrader**：CultistHeadpiece/FaceOfCleric/GremlinVisage/NlothsHungryFace/SsserpentHead 各为 CustomRelicModel 入 `EventRelicPool`，事件内对未拥有面具均匀 roll。解锁我们 LOCKED 的 FaceTrader。
3. **Madness 卡**：Cards.Madness 已有成品可对照。

### 关键架构事实
- Act 注册：`CustomActModel` 子类自动进 `ModelDb.Acts`；`ExordiumAct : base(1, true)`，`Index=0`、`IsDefault=false`；遭遇经 `IsValidForAct(act) => act is XAct` 类型判定绑定（与本侧同型）；事件经 `CustomEventModel.Acts` 绑定。
- 原版 UI 假设硬编码：他们需 Transpiler 改 `NRelicCollectionCategory.LoadRelics` 的 "act list" throw 才能让自定义遗物集合页工作——M3 收尾时留意同类坑。
- 配置项：RebalancedMode（默认关）、AllowNonLegacySharedEventsInLegacyActs（默认开）、AllowLegacySharedEventsInNonLegacyActs（默认关）、DarvOnlyInLegacyActs、LegacyEnemiesGiveClassicSlimed。Ancients（Darv 等）映射到 StS1 幕。
- pck 为 GST2 自定义容器（GDPC v3 头 + GST2 目录 magic），内嵌 RIFF/WAV + WebP。

### 共存风险（若同时安装）
- ID 前缀不同（SPIRE1-* vs 无前缀）→ 无 ModelId 冲突；遭遇/事件按 act 类型隔离，互不入对方幕。
- 但双方共享事件（shrine 类）都注册为全局共享 → 我们的 shrine 会出现在他们的幕里；他们的 `ShrinePatches.EventPoolPatch`/`RepeatableShrineValidityPatch` 是全局补丁，行为面待测。
- 双方都有 GoldenIdol 等同名事件遗物（各自事件授予，获取路径隔离）；若都改 `CardRarityOdds` 需确认叠加语义。
- 多人：AFTP 有已知联机问题（事件不同步卡死、增益叠乘），作者明确不管多人——这是本项目的差异化质量线。

### 待用户决策去向（选项见会话报告）：全量自研收尾+定向吸收 vs 收缩为互补层。

### 13.1 生态位补充调查（用户提示 + 页面核实，2026-08-23）
- **辨误**：3737158447 不是联机模组——是 Cany 的旧版 **Act Toggler（已停支持，NO LONGER SUPPORTED）**，仅控制哪些幕进轮换池；继任版 = Darkglade 的 **3787796638**（同时支持 main+beta 分支，Cany 已同意移植）。AFTP 评论区作者指的联机模组实为 **3785039319 "Multiplayer Rebalance for Acts from the Past"**（Kziz3988，08-17 发布，开源 github.com/Kziz3988/ActsFromThePastMultiplayerBalance）：只做**平衡**——Gremlin Nob/Mad Gremlin/AwakenedOne 的 Strength 改为对触发玩家施加 Extra Damage 减益、Transient 按人数调伤害与衰减；**不解决事件不同步卡死**。
- **Act 4 已有生态实现（用户确认）**：Thrayonlosa 的 **Act 4 Heart (3747537811)**。核实页面：StS1 The Ending+Corrupt Heart；三钥匙入门（打败超精英/放弃宝箱遗物/篝火回忆）；受 Ascension 影响；自称多人兼容；钥匙门禁可配置开关；可中途加入 run、不可中途移除；225 评价 5 星；kullay 贡献中文本地化。AFTP 官方 FAQ 即推荐此 mod 而非自己做 Act 4。
- 三者本地均未下载（workshop 目录无 3747537811/3785039319/3787796638）。
- **对我方差异化结论的修正**：①"The Ending 第 4 幕"不再是独占卖点；②"多人正确性"弱化为部分卖点（AFTP 联机可用但需全员同配置，平衡已有社区补丁；事件不同步卡死仍无人修）。剩余真实独占：**角色/玩家卡池/遗物层、选单地牢 selector UX**（生态评论区 Azusa 明确请求、无人做）、超越平衡补丁的联机正确性。另发现我方 The Ending 未实现三钥匙门禁（StS1 原版与 Act 4 Heart 都有）——若保留自研第四幕需补此保真缺口。

## Session 14 — 方向决策：全力转向互补层 (2026-08-23)

**用户决策**：放弃"自研地牢呈现"主线，Spire1 定位为生态互补层（角色/卡池/遗物/药水/事件），运行于社区幕栈之上。
生态栈四件全部订阅并下载到位（本地 workshop 目录核实）：AFTP `3746969593`（1-3 幕）、Act 4 Heart `3747537811`（The Ending+三钥匙，多人兼容）、Darkglade Act Toggler `3787796638`（main+beta）、Kziz3988 MP Rebalance `3785039319`（联机平衡）。

### 本会话落地
- 遗留改动收编（build 0 errors 验证后提交 `84bc1f9`）：`AutoSlayGatePatch.cs`（暴露引擎 `--autoslay` 冒烟路径，P1 互操作测试的基础设施）、`RestSiteBackgroundPatch` 补 `%RestSiteLighting` 场景唯一名（Owner+UniqueNameInOwner，修复休息点房间初始化崩溃）、8 张 map_bgs 瘦身为 146B 占位（自研幕降级为 fallback 的第一刀）。
- 备份钩子安装（`575b5dd`，`.omp/hooks/pre/backup.ts` ← playderata 模板；下次会话启动生效）。
- **DEVELOP.md 改写**：vision/§0/§1（P1 互操作验证、P2 缺口收口、P3 层内 UX 三新里程碑）/§2b（SUPERSEDED）/§9（N'loth's Gift、FaceTrader、Madness 解锁路线）/§10（AFTP 参考二进制位置）。

### 后续执行序（P1 → P2 → P3，见 DEVELOP.md §1）
1. **P1 互操作**：四件套 + Spire1 双装冒烟（AutoSlay）；审计点 = 双方 shrine 共享事件串扰、`ShrinePatches.EventPoolPatch` 全局行为、`CardRarityOdds` 补丁叠加语义、AFTP 的 `NRewardButton.Reload`/`LoadRelics` transpiler 与我方补丁面交集。
2. **P2 缺口收口**：N'loth's Gift（Prefix `RollWithoutChangingFutureOdds` 改写 offset，勿碰 `Roll` 的 pity 计数）；五脸面具 + FaceTrader 解锁；Madness 对照 AFTP 实现。
3. **P3**：选单可见性/门禁打磨；评估是否在生态幕之上做轻量地牢选择 UX。

### 14.1 三件套侦察（P1 前置，ilspy 类型清单）
- **Act4Heart v1.1.7**（dll 作者 Dolso）：`TheEnding`/`TheEndingMap`(CustomActModel)+CorruptHeart/SpireShield/SpireSpear；**三钥匙 = KeyRelicModel 子类（Ruby/Sapphire/Emerald）入自建 KeyRelicPool**，配 Red/Green/BlueKeyHooks（超精英任务/弃宝箱/篝火回忆 RecallSiteOption）；自带 **InvinciblePower、BeatOfDeathPower**、Metallicize/Regenerate 变体——正是我方标记"引擎缺失"的两个 power。Dolso 框架含 **ConfigSynchronizer（联机配置同步）**与特性式 HookManager，均为我方多人正确性的参考实现。
- ⚠️ **碰撞面 #1**：我方 fallback 的 The Ending（CorruptHeart/SpireShield/SpireSpear/HomeActs=[4]）与 Act4Heart 同内容双份并存——若用户同时启用我方幕与 AFTP 栈，会出现两个第四幕/两套心脏。P1 审计首项：确认 BossDiscoveryOrder/act 池在双装时的行为，必要时默认关停我方幕（config 门禁已具备）。
- **MP Rebalance v0.0.1**：仅打 AFTP 自家怪物类（AwakenedOne/Nob/MadGremlin/Transient/Shifting）+ExtraDamage 等 power，依赖 AFTP≥1.0.5；与我方补丁面零交集。
- **ActToggler2 v1.0.0**：单一 `ActTogglerPatch`，按配置开关幕池；依赖 BaseLib≥3.4.0。待验证粒度（是否区分幕来源 mod——影响它会不会把我方 fallback 幕也关掉）。

## Session 15 — P1 互操作冒烟开跑 (2026-08-23 下午)

**运行环境事实（后续会话必读）**：
- 引擎**自动加载已订阅 workshop mod**（日志 "Looking for mods to load from Steam Workshop"），手动复制进 mods/ 会触发重复加载警告并被引擎消解（本地版优先，Steam 版禁用）——**不要再手动复制订阅 mod 进 mods/**。
- `--autoslay` 契约（NGame.cs:694）：需 `IsReleaseGame()==false`（我方 AutoSlayGatePatch 已解锁）+ `--autoslay`；`--seed X` 固定序列、`--log-file Y` 落专用日志。AutoSlayer **随机选角色但同 seed 必同角色**（Rng 以 seed 哈希播种——用户观察到的"每次都是亡灵契约师/Necrobinder"即此）。runTimeout=25min。要覆盖不同角色→换 seed。
- **hub 启动游戏必须 `pty:false`**：pty:true 会把 Godot stdout 灌进 omp 终端与 TUI 重绘打架（用户报告的显示 bug）。

### 战果 #1：WrithingMass 无意图环 → native 栈溢出（P0，已修复）
- 现象：seed P1SMOKE1 两次均于第三幕 42 层 `SPIRE1-WRITHING_MASS_ENCOUNTER` 开战瞬间 fatal native crash（exit 0x7FFFFFFF），最后日志 `[IntentGraph] Generating intent graph for monster: 扭曲团块`。
- 根因：六个 reroll 条件态与 bands 形成不经过任何招式节点的纯条件环（reroll0_39⇄reroll40_99 等）；无环保护的原版拓扑假设被静态走图者（第三方 IntentGraph，引擎 AI guard 同理风险）递归爆栈。
- 修复 `361d330`：重掷改为代码内急切解析（ResolveFirst/ResolveBands，**RNG 抽取顺序逐一保持**）；静态机 = root→5 招式→root，与原版怪物同形。审计脚本扫全部 50 怪物文件：仅此一家有纯条件环。
- 附带发现：`res://animations/map/spire1-awakened_one_encounter/*` 地图图标缺失（非致命，回退加载）；`missing_power.png` 未打包（我方 power 图标回退链断一截）——待补 pck 资产。

### 冒烟正面信号（崩溃前 42 层）
AFTP/Act4Heart/ActToggler2/MP-Rebalance + Spire1 五件套正常同载；随机到 Necrobinder 打出了 SPIRE1-MADNESS（共享卡池互通 ✓）；我方遭遇（FOUR_SHAPES/DARKLINGS 等）连续正常出怪战斗；休息点/商店/事件 handler 全部工作。

### 战果 #1 验证通过（同 seed P1SMOKE1 复跑）
修复后 `sts2-p1-smoke3`：42 层扭曲团块正常出招（MULTI_HIT→ATTACK_BLOCK）并获胜，run 完整走完生命周期（AutoSlay 随机败局→结算→exit 0，5m09s）。前两次对照：3m44s 同点 native 崩溃。**修复判定：成立**。
遗留资产小账：本 run 共 106 条 [ERROR]（均为非致命资源回退）——`spire1-awakened_one_encounter` 地图图标、`missing_power.png` 未打包等，归入 pck 资产补全任务。

### P1 后续队列
1. 多 seed 扫描（换 seed 覆盖不同角色/路径，含我方四角色被选中时的完整 run）。
2. pck 资产补全（遭遇地图图标对、missing_power.png 兜底图）。
3. 幕池行为审计：本 run 走的是哪套幕（日志显示 SPIRE1 遭遇 → 疑为我方 fallback 幕被选中；需确认 AFTP 幕与我方幕在同池时的选择语义与 ActToggler2 粒度）。

### 战果 #2：AFTP 转盘事件三方交互（已上报 + 已自修）
目标环境首跑在 Act1-F12 `ACTSFROMTHEPAST-WHEEL_OF_CHANGE` 中止：引擎 AutoSlay 无该自定义屏 handler（"No handler for screen type: NWheelSpinScreen"）+ 用户 SpeedX 的 AutoProceed 盲点 Proceed → 看门狗 5.1s 判死退出（exit 5；AutoSlayer 自身 quit(1)）。
- **AFTP**：issue #10 已提交（API 稳定性承诺 + ProceedButton 时序 + 标记接口建议）。
- **MegaCrit**：无公开 tracker，草稿存 `.tmp/issues/megacrit-autoslay-extensibility.md`（_screenHandlers 开放扩展点）。
- **SpeedX**：无公开仓库（B 站视频首发）→ **用户自行联系作者**（待办，非我方动作）。
- **我方修复** `AutoSlayModdedScreenHandlersPatch`（--autoslay 门禁）：反射注册 NWheelSpin/NMatchAndKeep 屏 handler，等待滑入动画后调小游戏公开 Complete()（结果构造时已定，旋转纯演出）。PortalMapBuilder 无公开完成方法，暂不注册。**用户指令：此类兼容补丁随 Spire1 发布承载（AFTP 冻结），已写入 DEVELOP §0。**

### 战果 #3：心脏已斩，结局链最后一环 = 我方不死补丁误伤脚本处决
heart4（MoveNext 转译生效后）：**第四幕全程自动驱动，CORRUPT_HEART_BOSS 4 回合斩杀** ✓。
结局链卡在 TheArchitect 事件：等 GameOverScreen 超时（exit 1）。根因 = 我方不死补丁无差别清零：
原版胜利演出里建筑师对玩家执行**真实处决** `CreatureCmd.cs:533 LoseHpInternal(currentHp, Unblockable|Unpowered)`，
被前缀挡下 → 死亡不注册 → 结算屏不来。**建筑师"劈死"不是纯演出，是等额处决**（此前"纯演出"判断有误，纠正）。
修复 v3 `afd3b05`：免死范围收窄到 Monster/Elite/Boss 房；事件房内真实伤害放行（保真：事件致死本来就该致死）。
`RunManager.Instance.DebugOnlyGetState().CurrentRoom.RoomType` 判定，状态不可读时 fail-safe 保持不死。

### 音频核验（用户报告"攻击音效与自玩时不同"）
- NaN 音量错误：我方测试跑 **3511 次** vs 用户 8-22 手动游玩日志 **0 次** → 测试环境特有。
- AFTP 音量数学（LinearToDb(vol²)+offset）在正常输入只产生 -inf 不产生 NaN → 排除。
- 首现位置紧贴 SpeedX AutoProceed 活动 → 头号嫌疑 SpeedX 加速×音频 tween；次嫌 AutoSlay 注入的 999 Plating/200 Strength 触发的 UI 路径。待对照实验（禁 SpeedX 重跑一次）定罪。
- 另实测 AFTP pck 缺 `sfx/chosen/chosen_death.ogg`（加载失败 2 次）——部分 StS1 音效缺失即"听感不同"的构成之一；遗产幕内 AFTP 有意替换音频为设计行为。

### 对外沟通规则（用户指令）
- 所有对外 issue/评论/草稿**必须披露 agent 参与**（对齐 AFTP 作者自曝 AI 参与的标准）：注明研究/撰写由 ox-alpha agent 在 Twelve-eight 指挥下完成、证据来源。已回填 AFTP #10 正文与评论、MegaCrit 草稿，规则入 DEVELOP §5。
- AFTP #10 已追加**标准结束协议**提案（标准 NGameOverScreen + 幂等 run-over 闩锁 + 可声明运行长度；含"多周目需手动开局故不受影响"论证）：issuecomment-5384987489。
- MegaCrit 无公开 tracker，完整草稿（含协议附录）在 `.tmp/issues/megacrit-autoslay-extensibility.md`，待用户经 Discord/论坛递交。

### 战果 #4：**全链路验证完成**（heart5 exit 0）
`CORRUPT_HEART_BOSS` 4 回合斩杀 → 建筑师处决正常落地（免死收窄 v3 生效：事件房真实伤害放行）→ 标准 `NGameOverScreen` 出现 → 回主菜单 → **"Victory! Run completed" → exit 0**。
完整链 = AFTP 三幕自动驱动（含转盘全自动：Complete + 后续按钮清扫）+ Act4Heart 第四幕 + 我方角色/卡池/事件层全程在线。

### 层数核验（供对外信件引用）
原版 `BaseNumberOfRooms`：Overgrowth 15 / Hive 14 / Glory 13 / Underdocks 15 → 原版局 ≈48 层封顶，引擎 `TotalFloor < 49` 恰按原版上限校准。任何加幕/拉长流程必撞线；AFTP 纯三幕自身即有越界风险（StS1 长度）。修复用 MoveNext 字面量 49→120。

### 对外沟通落地（礼貌版 + 完整上下文 + agent 披露）
- AFTP #10 新跟进评论（issuecomment-5385088440）：先谢后请、补齐 Spire1 定位/验证方法学/层数数学、明确我方承载补丁不 upstream 化。
- Act4Heart：无公开仓库 → 礼貌版协议请求草稿存 `.tmp/issues/act4heart-ending-protocol.md`，**待用户转贴工坊 Bug reports 区**。
- MegaCrit 草稿含协议附录与 agent 披露，待用户递交 Discord/论坛。

### 层数勘误（用户要求核实后修正）
实测（heart5，seed P1SMOKE1）：AFTP 三幕 = **17+16+15 = 48** 层，恰好不越界；越界仅发生在追加第四幕进幕瞬间（TotalFloor=49）。StS1 原版字节码 `MAP_HEIGHT=15` + Boss 行 = 每幕 16 层，三幕+两次幕间篝火 ≈ 50 —— "原版 StS1 可超 48"成立，但"纯 AFTP 三幕自行越界"不成立，此前论断有误。
- 已公开更正 AFTP #10 跟随评论（PATCH issuecomment-5385088440）。
- 心脏草稿按用户格式重写：问题事实前置（引擎上下文→mod 改变量→实测数据→失败日志→归因→请求）→ 非问题确认清单 → 礼貌收尾 + agent 披露。待用户转贴工坊。

### #10 第二条发言撤稿致歉（用户指令）
反思确认：层数越界与 AFTP 无关（三幕恰 48 < 49；越界需第四幕追加；根因 = 引擎封闭假设）。已 PATCH 重写 issuecomment-5385088440：仅留情况简述 + 双方（人类指挥者与 agent）共同道歉 + 指明编辑历史可回看原文；删除错误问题报告与协议请求内容。协议请求仍保留在第一条跟进评论（5384987489，属 AFTP 职责范围的小游戏可驱动性 API）。MegaCrit/Act4Heart 草稿中的协议内容不变（归属正确）。
- 用户将错误评论 hide 为 low-quality；改为输出第三条评论文稿（`.tmp/issues/aftp10-third-comment.md`）由用户手动发布：低姿态个人口吻、正确结果直出、错处看编辑史、agent 参与透明化（起草自 AI、核实责任在人）。

### P1 资产项关闭 + 音频归因实验设计
- **资产缺口项关闭**：heart5 实测我方 pck **零真实缺失**（唯一 warning 为引擎对 missing_power.png 兜底图的按需提示）。此前 106 条 ERROR 大头是**原版资源懒加载警告**（regen/strength/necrobinder_energy_icon 等原版物）+ AFTP 的 chosen_death.ogg（归 AFTP）+ NaN 刷屏。无补全需求。
- **NaN 归因实验**：SpeedX 配置位于 `%APPDATA%/SlayTheSpire2/ModConfig/sts2.piyixiajiuhenfen.speedx.json`。基线 = seed2 全开运行；对照 = 同 seed 关 `autoProceedEnabled`；若仍复现再关 `turboEnabled`（注意关加速后 25min runTimeout 可能不够跑完全程——但日志前段足以计数 NaN）。定罪后材料并入 SpeedX 反馈。
- 多 seed 扫描已启动：seed `P1SMOKE2`（sts2-p1-seed2）。
