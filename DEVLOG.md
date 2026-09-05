# DEVLOG - sts2-spire1

Recovery anchor + working state. Design/contracts: `DEVELOP.md`. Shared conventions: `../AGENTS.md`. Resumable with zero prior chat.
**Sessions 1-3 are archived in `DEVLOG-archive.md`.** This file keeps the live STATUS / latest sessions only.

## STATUS (2026-08-25 03:00, session 14+ 夜间自主批)
- **版本 0.9.1 已发布**：live 三件套 dll `9e0bd0d9` / pck `27020df2` / json，三 zip 同哈希。BaseLib 钉死 3.4.5（csproj 不再浮动）。
- 内容：4 角色可见（观者已归档硬隐藏，77 张卡退出总览）+ 一代地牢可选；306 卡（Cards/ 实测 306 文件）、25 遗物（8 项官方等价已删，flavor 已按 KB 逐字对齐）、49 力量、53 事件。
- 联机层：握手放行+弹窗抑制（IgnoreMpModDifferences）、火堆黑屏通用救援（RestSiteLightingRescuePatch）、地图跳过节点按钮。MP 失同步三案：清单层排除，遗物层分歧未解释（reverify B-5 降级表述）（divergence #563/#249 对拍）。
- 知识库：research/sts1-kb/ 数据卷 460+ 条 + 语义卷 119 规则；research/kb/ 项目事实三卷；双审计报告在 research/audits/。
- 待办焦点：跳过按钮真人局验证；AFTP 上游 issue 发送（文稿已备）；Girya/Nloth 设计决策；覆盖 drain 跑至 18:00 后终态回填本文「Cutoff 追加」。

### 历史 STATUS（2026-08-21, session 6）——已过时，保留供考古
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
【勘误 2026-08-25】Act3 实际落盘 16 场（本日删除一弱变体未回填此行），全游戏合计 55 而非隐含的 56。
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

### seed2 结果 + NaN 对照实验进行中
- **seed P1SMOKE2 = Regent 全程胜利 exit 0**（多 seed 扫描第二角色覆盖 ✓；两角色两胜利链）。NaN 基线（SpeedX 全开）= 3250；ERROR 仅 2 条。
- 对照实验 E1：`autoProceedEnabled=false` 同 seed 重跑中（sts2-p1-seed2b）。判读：NaN≈0 → 定罪 AutoProceed 的按钮/tween 路径；仍 ~3000 → 排除 AutoProceed，下一步关 `turboEnabled`（注意 runTimeout 风险，前段日志即可计数）。

### seed2b 尸检 + E1 判定（SpeedX 配置已恢复原状）
- **E1 判定：AutoProceed 洗清**。NaN 3250（全开）vs 3279（关 AutoProceed）无显著差异 → 头号嫌疑转为 `turboEnabled` 的 10× 时间缩放 × 音频 tween（E2 可选：关 turbo 跑 10 分钟取前段计数即可定罪，无需跑完全程）。
- **seed2b = Regent 第二次斩心**（"That's 2 wins"）。死亡点不在游戏层：杀心→建筑师→结算屏后**回主菜单时**资源加载失败（`char_select_bg_necrobinder.tscn` / `characterselect_necrobinder_skel_data.tres` 解析错误 + LieRenTVmod 缺选人界面图）→ 未走干净关机 exit 5。嫌疑 = NecrobinderFemPortraits/LieRenTV 等肖像类 mod 与当前 beta 选人界面资源冲突——**非我方层问题**，独立待查项。
- `[ERROR] Act 4 is not yet implemented` 为**良性日志**：ProgressSaveManager 给"完成第四幕"发角色 Epoch 时发现原版无此纪元（cs:579 case 3），仅缺一次解锁奖励。
- **无图卡观察**：seed2b 日志零 card_portrait 加载失败；当时出牌均为原版 Regent 卡（GLITTERSTREAM/PANIC_BUTTON/CRUSH_UNDER/REFLECT/GOLD_AXE 等）。"回复4点生命。消耗。"未匹配我方 zhs 文案 → 非我方卡。需用户下次记下卡名才能追；日志侧无异常支撑。

### 战果 #5：302 张卡面占位图全部替换为 StS1 原版美术（用户报告"包扎没卡图"触发）
- 用户目击 `SPIRE1-BANDAGE_UP`（包扎）无卡图。审计：card_portraits 333 张中 **302 张为 ~314B 纯色占位**（250×190 尺寸正确但无内容）——session 11.2 的"331/331 mapped"仅指文件存在，非真美术。big/ 目录同样为 3KB 占位（不可用）。
- 恢复源 = `.tmp/sts1full/images/1024Portraits/`（368 张 jar 解包图）：basename 直配 + 去符号 squeeze（j_a_x→jax 等）+ 全树兜底；10 张显式别名：per-color 共享 strike.png/defend.png、charge_battery→conserve_battery、wreathe_of_flame→wreath_of_flame、judgment→judgement、lessons_learned→lesson_learned。PowerShell System.Drawing 批量 500×380→250×190（脚本 `.tmp/restore-portraits.ps1`）。零残留。
- pck 8.5MB→20.6MB 已部署（18:12）。commit `5588f9f`。**待用户下次进游戏目验包扎与其余卡面。**
- 教训：DEVLOG 11.2 的"331/331 mapped"表述误导（存在≠真图）；后续"已映射"类声明必须附带尺寸/字节数证据。

### 战果 #6：ROOM_FULL_OF_CHEESE 卡池耗尽崩溃修复 + SPIRE1-IRONCLAD 首胜（P1SMOKE3，2026-08-23 晚）
- **现象**：seed P1SMOKE3 首跑 exit 1（AutoSlayer 看门狗）。锚点：`InvalidOperationException: Tried to create a card for a reward, but we couldn't generate a valid card!` at `RoomFullOfCheese.Gorge()` → Act1 F7 选"大快朵颐"。
- **根因**：Gorge 用 `CardCreationOptions.ForNonCombatWithUniformOdds([owner.Character.CardPool], c => Rarity==Common)` 要 **8 张不重复 Common**；`CreateForReward` 内层循环把已选卡累积进 blacklist，池中 eligible Common 耗尽即抛。静态审计（解析全部 Cards/*.cs 的 [Pool] 继承链）：Spire1CardPool(Ironclad) Common 恰好 6 张（Cleave/Clothesline/Flex/HeavyBlade/Warcry/WildStrike——与异常黑名单逐字吻合），SilentCardPool 同为 6（潜伏雷），DefectCardPool 自有仅 3 但 SharedCardReuse 复用后 13 安全，Watcher 19 安全。
- **修复**：SharedCardReuse 扩展 IroncladReuse(+10)/SilentReuse(+11)——取自 `.tmp/duplicate-cards-report.md` A 组（同名同效果，逐字段源码级比对过）的 shipped Common 卡，ModHelper.AddModelToPool 进对应池 → Ironclad 16 / Silent 17。commit `3deabac`。
- **回归**：同 seed P1SMOKE3 重跑——同事件同选项零异常通过，**全程胜利**（第四幕心脏+建筑师演出+回主菜单，exit 0），`[ERROR]` 行数 0（历史最干净），NaN 3711（SpeedX 基线区间），我方资产缺失 1 条：`relics/mutagenic_strength.png` 遗物图缺（待补）。
- **教训**：自定义角色池存在隐式引擎契约（事件可能要求 ≥8 张不重复 Common）；新角色入池时必须过此契约。静态解析注意 `[A-Za-z]+` 匹配不了带数字的类型名（Spire**1**CardPool）。

### 战果 #7：卡面"无图"真根因 = 大图槽位 + E2 定罪失败（SpeedX 二次洗清）
- 用户目验报 17+ 张铁甲卡无图 → 全量审计发现 **card_portraits/big/ 302 张全部为 ~3KB 占位**（早上只修了小图）。BaseLib `CustomCardModel.cs:268-311` 把 `CardModel.PortraitPath` getter 重定向到 `CustomPortraitPath`（=big 槽）→ **卡面主图永远走大图**，小图只喂缩略场景。
- 图鉴（百科全书）判读修正：`未知/???/黑图` 条目 = 未解锁迷雾（正常现象）；"均衡带 BETA" = 官方水印；复用官方卡显示正常恰证 SharedCardReuse 生效。用户报的 17 张全是自家移植类，与该模型完全吻合。
- 修复：`.tmp/restore-big-portraits.ps1` 从 sts1full 500×380 原生分辨率重生成 302 张 big（重跑安全，仅动 <4KB）；同批补 DrugDealer 遗物 mutagenic_strength 三件套（relics/ + outline + big，StS1 名 mutagen）。commit `465efe9`。
- **E2 终审：SpeedX turbo 洗清**。同 seed P1SMOKE3 两局全程胜利：turbo 开 NaN=3711 / 关=3609（差 2.7%，噪声级）。NaN 挂在商店/奖励按钮音效 `set_volume_db`（引擎侧），与时间缩放无关——E1 的"头号嫌疑"撤销，真凶待查（低优先级，纯日志噪音无实感）。
- 教训：修资产先确认**消费方路径**再动手（PortraitPath 被 Harmony 重定向这种事，光看自己基类会漏）；PowerShell 内联经 bash 会丢 `$`，一律落 .ps1 文件。

### 状态：冒烟测试挂起（2026-08-23 晚）
- 用户切换到无 StS2 授权的 Steam 账号游玩 → autoslay 无法跑（workshop 生态订阅同失）。P1SMOKE4 启动 21s 即停，无残留进程。
- **磁盘状态即最终态**：mods/Spire1 已是 dll(卡池修复)+pck 31MB（302 小图+302 大图+遗物三件套）；SpeedX 配置已还原（turboEnabled:true）。换回原账号后无需任何补做。
- **待办（恢复账号后）**：① 目验卡面大图（包扎/暴走/哨卫/缴械/燔祭/递归等）；② seed 扫描 P1SMOKE4 起覆盖 Ironclad/Silent/Defect/Watcher 四角色完整胜利；③ run_history 110 张 70B 遭遇图标为已知低影响缺口（StS1 无官方图标源，宁缺勿造）。

### 全面复核 + 三连修复（2026-08-24 凌晨，用户实机反馈驱动）
**复核层（全部通过）**：资产 0 占位残留、部署 pck/dll 与产物 md5 一致、SpeedX 还原一致；P1SMOKE3 Victory+0 ERROR+0 Exception；NaN 3609 vs 3711 维持 turbo 洗清；卡面链路源码闭合（NCard.cs:1248→CardModel.Portrait:157→PortraitPath:143←BaseLib CustomCardModel.cs:300 前缀重定向到 big 槽）；21 张复用卡经 javap -c desktop-1.0.jar 字节码仲裁——报告 A 组全对，我方记忆五连错（Deflect=4/DnR=4+2/PiercingWail=力量损失/Anger+2/DeadlyPoison 无耗尽），唯一漂移 BladeDance 官方版自耗尽→已移出 SilentReuse、我方类回归现役池；Toggler2 GetWeightedAct 反编译坐实空槽=均匀随机。

**修复 #8：Seek+/Nightmare 打出卡死（selectionScreenPrompt 缺键）**
- 根因：`CardModel.SelectionScreenPrompt`（CardModel.cs:129）在缺 `.selectionScreenPrompt` 键时直接 throw → OnPlay 首行即炸 → 牌僵在屏幕中间无选牌 UI。全量扫描：用选牌界面的 8 张卡中仅 Seek/Nightmare 缺键（其余 6 键即用户当年手工修复）。已补两语言键（Seek 用 {Cards:plural:...} 复数模板支持升级 2 张）。

**修复 #9：描述英文回落+裸 {M:diff()} 模板（通配符↔变量名失配）**
- 机制链：BaseLib SimpleLoc 把 `!X!` 转成 `{映射名:diff()}`，特判表仅 D/CD/B/CB/C/E/H 七个字母，其余透传原名；SmartFormat 按 C# CanonicalVars 注册名解析。zhs 缺键或渲染异常时回落 eng 表。
- 全量扫描器（解析每卡注册变量名 vs 中英通配符）收敛到 5 卡 10 条：Aggregate !E!（整句重写为按 MagicNumber 计能）、Claw !M!→!Increase!、Halt/Prostrate !M!→!MagicNumber!、Streamline !M!→!CostReduction!。已修，复扫=0。误报排除：!CD!/!CB!/!Scry!/!Repeat!/!MaxHp! 等由 MakeCalculated*/专用 Var 类型正确解析。
- 教训：变量通配符必须与 C# 注册名精确一致；715f42d 的"对齐 eng"标准不充分——eng 自己也可能写错，唯一权威是 C# 注册名。

### 修复 #10：Splash（飞溅）候选集语义修正（用户实机反馈）
- 用户引用描述"从其它角色的攻击牌中任选一张。该张牌在本回合免费打出。"→ pck 明文定位为官方稀有技能卡 SPLASH 的 zhs（非药水）。
- 原实现 `list.Remove(owner.CardPool)` 仅按**池对象**排除持有者；对移植角色失效——一代 Defect 通过 SharedCardReuse 已拥有官方缺陷猎手同源牌，这些模型经官方缺陷猎手池仍会进入候选（"可以同时属于一代自己"却以他人身份出现）。
- 修复：`SplashOwnSetSubtractPatch` 前缀整体替换 OnPlay——候选 = 全部角色攻击牌 **减去持有者自身卡池集合（按 Id.Entry 集合差）**；对原版角色为零变化。mock 测试分支逐字保留。
- 同批：Seek/Nightmare 补 `.selectionScreenPrompt` 键（CardModel.cs:129 缺键即 throw → 打出僵死的根因）；5 卡通配符对齐 C# 注册名（Aggregate/Claw/Halt/Prostrate/Streamline）。机制文档化于 SimpleLoc.cs:79-88（!X! 特判表仅 D/CD/B/CB/C/E/H，M 不在表内透传原名）。
- P1SMOKE4 回归：官方 Defect 全程胜利（r1 竞态失败→救援补丁护栏在位，r2 未复现即胜）；**NaN=0 于原版局 ⇒ NaN 与我方自定义内容强相关**（N3 深挖线索）。
- P1SMOKE5：SPIRE1-WATCHER 全程胜利（EXC:0）。**NaN=0 ⇒ NaN 与 SPIRE1-IRONCLAD 专属内容强相关**（Ironclad 局 3600+ / Watcher·官方Defect 局 0），N3 深挖范围收窄至铁甲池内容。
- P1SMOKE6 三连回归：r1/r2 暴露 SimpleLoc 同源竞态第二形态 + PatchAll 一损俱损缺陷 → IsEnabled 补丁重定向声明基类 NClickableControl + MainFile 逐类 try/catch 加固（3ebbab0）→ r3 **SPIRE1-DEFECT 全程胜利**（EXC:0）。覆盖进度：Ironclad✓ Watcher✓ Defect✓，仅剩 Silent。
- P1SMOKE8：SPIRE1-SILENT 全程胜利 ⇒ **覆盖矩阵完成**（五角色+官方Defect加映全胜）。ERR 噪音新归属：AFTP 自身 {Damage} 系模板缺变量渲染（其 dll 内置 loc），非我方，随 AFTP 沟通材料反馈。
- P1SMOKE9：官方铁甲胜利，当前构建累计8胜0崩、NaN全零（修复前铁甲局3700+）。

### 夜间批次（续）：Watcher 归档 + 商店守卫 + 尘封魔典定罪
- **SPIRE1-WATCHER 归档**（用户指令：AFTP 生态已有成品 Watcher）：`Spire1Config.EnableSts1Watcher=false` 默认；`Watcher.cs` override `HideFromVanillaCharacterSelect => !Enable`、`AllowInVanillaRandomCharacterSelect => Enable`。模型保留注册（老存档兼容）。归档前 watcher-cov 注入局已推进 WATCHER 覆盖 32→39/77（余量随归档挂起）。
【2026-08-25 勘误】该开关已随 0c2a… 系列提交彻底移除，归档改为永久硬隐藏（无配置门禁）；后续总览门控见 bd6c539。
- **商店购买守卫**（用户实测定位：autoslay 持续尝试买药水被"添水"类禁药遗物阻止，maxAttempts=50 内反复空转约 1 分钟）：`ShopPurchaseGuardPatch.cs` 重实现 `ShopRoomHandler.HandleAsync` 主循环——购买后槽位仍 stocked ⇒ 判定被拒，加入 failedSlots 黑名单永不重试；AutoSlayer.IsActive 门控。**部署待游戏退出后补做**（dll 锁）。
- **尘封魔典（DUSTY_TOME）机制定罪**（用户报告"发的牌是封印王座"）：
  - 官方遗物 `DustyTome`（RelicRarity.Ancient，zhs"尘封魔典"）：`SetupForPlayer` 从**当前角色卡池**抽 `CardRarity.Ancient` 牌（升级后洗入牌堆）。Regent 池含 TheSealedThrone(Ancient Power) ⇒ "储君→封印王座"闭环。
  - **一代角色四池 Ancient 稀有度牌数=0** ⇒ `NextItem(空集)`返回 null（Rng.cs:289-299 有 default 兜底不崩）→ setter null 守卫跳过 → AfterObtained `GetById<CardModel>(null)` 行为待实证（预计异常或无操作）。**冒烟时用控制台 `relic add DUSTY_TOME` 实测**（modded 运行控制台可用：NDevConsole.cs:359）。
  - 修复候选（待实证后决策）：①给每角色配置一张忠实 Ancient 卡 ②SetupForPlayer 空集 fallback 补丁 ③文档化限制。
- 权威覆盖计算器固化为 `.tmp/night/coverage.js`（继承链解析池归属；played 正则 `/Playing (\S+)/`；官方复用映射表；自动输出 queue-<pool>.txt）。当前：IRONCLAD 32/44、SILENT 34/47、DEFECT 51/58、WATCHER 39/77（归档挂起）；缺口含起始牌替代类（Strike/Defend 系，实际不发牌，标 N/A）。
- 提交：ca8c0b2（41 力量图标重生）、f2f3305（归档+守卫+注入器）。推送走直连 fallback（代理 7897 失效）。

### 非 cards 域通配符审计（挂账清偿）
- 扫描九域 loc（powers/relics/potions/acts/characters/events/settings_ui/ancients/card_keywords/static_hover_tips）：除 events 外全部零占位符（无 !X! 也无 {X}）——此前 worker 报告的"非 cards 域风险"不存在。
- events 域：53 事件 / 1312 键，{X} 占位符与 C# CanonicalVars/DynamicVars 注册名**全量一致，0 失配**。脚本固化 `.tmp/audit-event-vars.js`（id 规则 = `SPIRE1-`+类名蛇形；审计时注意 loc 键带连字符、类内字符串正则须含连字符，否则会空转假绿——已踩坑两次）。

### 联机兼容审计（挂账清偿）+ 尘封魔典认知修正
- **修正**：DustyTome 空池 NRE 早已修复在树——`DustyTomeAncientFallbackPatch`（03ae5d1，评审 7c98579）：原版 `NextItem(items).Id` 对空集直接解引用（我此前"setter null 守卫不崩"的推断有误，NRE 发生在 SetupForPlayer 内部）；补丁回退到 PlaceholderID 对应官方池（ironclad/silent/defect/regent），per-player 上下文。冒烟时 `relic add DUSTY_TOME` 只需验证回退生效。
- **联机语义逐补丁核查**：RewardClampPatch（CreateForReward 按 player 参数钳制数量）、SplashOwnSetSubtractPatch（splash.Owner 上下文、PlayerChoiceContext 多人感知类型）、DustyTomeAncientFallbackPatch（player.Character+player.PlayerRng 每玩家流）——三者均无静态单例假设。AutoSlay 系全部 AutoSlayer.IsActive 门控且仅单机 --autoslay 生效；LocDebug/RestSite/DungeonSelection 为视觉/开发类全局项，StS2 联机要求双方 mod 集一致 ⇒ 无分叉风险。**结论：我方补丁层联机安全。**

### 冒烟批次（守卫+遗物实测，r2-r13+final）
- **商店守卫终版生效**：`ShopEnoughGoldGuardPatch`（MerchantEntry.EnoughGold postfix）——SOZU 在手时药水槽 reason=sozu-ban、被跳过；含药水商店流程秒过。前两代实现全部废弃并记录：①HandleAsync 前缀 .Wait() 主线程死锁（用户目击"对对碰"画面冻结）；②CreateCard+AddGenerated 异步管线在 postfix 上下文 await 不恢复。
- **尘封魔典遗物链全通**：`DebugRelicInjectPatch` 挂 NMapScreen.Initialize（战斗外安全点），`relic.ToMutable()` + DustyTome 先 `SetupForPlayer(player)` → `RelicCmd.Obtain` 成功无失败日志；sozu-ban 即其铁证（禁令 hook 需遗物真实入包）。教训：①discarded task 吞异常必须 ContinueWith 记录；②canonical 模型不可直接 Obtain/CloneCard（"used in incorrect place"）。
- **卡牌注入器四代演进全部失败，回滚原版**：canonical 直传 AddGenerated（Owner/Pile 断言静默炸）、CreateCard+AddGenerated（await 不恢复）、SetUpCombat 时点（DrawPile 未建）、PopulateCombatState+CreateCard/AddInternal（入堆成功但抽到即断回合链，turn3 "No playable turn" 超时）。**关键复盘：历史覆盖增长全部来自自然出牌，注入器从未贡献过覆盖**——跨角色注入的 canonical Owner=null 是根因。已 `git show f2f3305` 回滚；后续覆盖策略=多跑整局自然 drain。
- **最终验证局**：SPIRE1-IRONCLAD ★胜利 F17-A3，ERR:0 EXC:0 NaN:0，无注入残留 ✓。
- **终版矩阵**：IRONCLAD 36/44（缺12中8实缺：Bash/Berserk/LimitBreak/Reaper/SeeingRed/SeverSoule+起始牌替代4张N/A）、SILENT 34/47（10实缺+3N/A）、DEFECT 54/58（**实缺0**，余4全为起始牌替代）、WATCHER 39/77 归档挂起。
- 提交序列见 git log；推送直连 fallback。

### 联机粘液失同步验尸（RitsuLib 转储分析）
- 现场：SLIMED（官方状态牌，打出发1张牌）打出后 checksum #55 分歧；另一局 SPIRE1-SUNDER 出现 "finished execution, but was in state Canceled! task probably kept executing..."（ID91 分歧）。
- 硬数据：`players[1].piles.Draw` 本地 12 张 vs 远端 11 张，远端=本地左移一位（少一张 STRIKE_REGENT）——即某次对 players[1]（工坊 Watcher 局里的原版储君玩家）的抽牌只在单侧生效。全部 RNG 流（含 counter）两侧一致 ⇒ 非洗牌种子问题，是**抽牌动作未同步**。
- 我方排除依据：①分歧堆属于第三方角色；②触发卡为官方状态牌；③我方 dll 双侧一致且无任何抽牌管线 hook；④本局我方卡（RECYCLE/SUNDER 等）执行序列双侧一致。
- 高危嫌疑（环境里有 8 个 gameplay mod）：**Multiplayer Limit Break v0.2.7**（改多人抽牌限制，直插 DrawCmd 的可能性最大）、SpeedX（改动作节奏）、sts2_typing（拦截出牌）；另发现双方 **BaseLib 构建来源不同**（本地 ModsDirectory vs 朋友工坊版，同版本号不同构建体）。
- 处置建议：①最小化 mod 集复测（BaseLib+Spire1），复现→上游 bug，消失→二分定位（先砍 MPLB）；②统一双方 BaseLib 为工坊版；③ Sunder 的 Canceled 模式与官方 HandOfGreed 同构（击杀后 GainEnergy），暂不改码，若最小集下仍复现再做无等待重排。
- 教训：分装包 character.txt 只影响可见性不影响状态一致性（本次双方装了不同包仍完成整场战斗即为证明）。

## 2026-08-25 夜间批次（GA 修正 + 联机容错 + 跳过节点救援 + 部署闭环）

### GA 池归属与语义双修正（af6d1d7）
- 用户实锤"遗传算法不该是红色牌"：`GeneticAlgorithm.cs` 漏挂 `[Pool]` → 继承铁甲池。补 `[Pool(typeof(DefectCardPool))]`。
- jar+官方 loc 双仲裁推翻记忆：GA 官方原文是 **Gain !B! Block**（非敏捷）+ 每打出一次格挡永久 +!M! + **消耗**。
  类路径 `cards/blue/GeneticAlgorithm.class`（ID="Genetic Algorithm" 带空格）；描述在 `localization/`（单数）eng/zhs cards.json。
- 教训固化：数值永远 jar+loc 双源仲裁，不信记忆（用户与我同时记错属性）。

### 无视 mod 差异联机补丁（d0181a0，Spire1Config.IgnoreMpModDifferences 默认开）
- 握手三道闸定位：HandshakeManager.TryReadHandshakeMessage——版本串不符→VersionMismatch；玩法 mod 清单不符→ModMismatch；ModelID 哈希不符→VersionMismatch；非玩法差异仅告警。
- Postfix 放行策略：ModMismatch 一律放行；VersionMismatch 仅在版本串相同时（=哈希成因）放行；真版本差异仍拦截。
- RitsuLib 失同步弹窗抑制：`StateDivergenceDiagnosticsPopup.ShowDeferred(report)` 前缀拦截（第三方类型 AccessTools 解析，缺失静默跳过，MainFile 扫描循环后显式 Apply）。诊断 zip 独立管线照常落盘。
- **假阳性实锤**（divergence #563/#249 对拍）：双方 BaseLib 来源不同（ModsDirectory vs Workshop）、远端多 4 个非玩法 mod、分装包名不同 ⇒ 清单级差异；players/piles/choices/rewards/creatures 逐字段全同。SpeedX 在场但无状态差异。

### 地图页跳过节点按钮（41e7acc，Spire1Config.EnableSkipNodeButton 默认开）
- SkipApiScout 取证：RunState 无房间完成字段，放行=`NMapScreen.IsTravelEnabled` 本地门控（战斗胜利同款 `SetTravelEnabled(true)`）。
- 实现：NMapScreen.Open postfix 注入"跳过当前节点"按钮 → SetTravelEnabled(true)+RecalculateTravelability(反射) → 玩家直接点目标节点走原生 VoteForMapCoordAction 投票管线。零状态改动零新增网络类型=不失同步源。
- 用途：火堆黑屏死锁自救（顶栏地图键本地可用先例 NTopBarMapButton.cs:104）。

### 火堆黑屏归因现状
- 机制确认（用户）：进火堆必黑屏；杀进程后从火堆重启跳过入场故正常；本次上个存档点是事件→死循环。
- 原始 110k 行现场日志已被轮转丢失；现存日志仅剩自救痕迹（win×3/block 222 均为事后戳醒动作，非病因）。我方 RestSiteBackgroundPatch 只作用于 Spire1Act 已排除。
- 缓解已上线（跳过按钮）；复现协议：**冻结瞬间先拷 logs 目录再杀进程**。

### 部署窗口闭环（cb70f82 后全绿）
- 发现并根治清单回退 bug：csproj `CopyToModsFolderOnBuild` 每次构建用 `mod/Spire1.json`（v0.0.0 模板）覆盖 live 清单——昨晚手工同步的 0.9.0 被冲掉即此因。修复=源头改 0.9.0（构建自动带出）。
- live 三件套 dll(ee92ac65)/pck(40025b18)/json(0.9.0) 齐；三 zip 重打（PowerShell Compress-Archive，无 zip CLI），解包校验 dll/pck 与 live 字节一致，character.txt ironclad/silent/defect 各归各。
- 本批上线内容：GA 池修正+Block 语义、握手放行+弹窗抑制、地图跳过按钮、（前批）池系统性修复 8781855。

### 待办移交
- KBBuilder subagent 进行中（research/sts1-kb/ 四色卡牌+遗物+药水+事件，双语原文）。
- 火堆黑屏真根因：待下次复现按新协议取日志。
- Thunderclap jar 归属复核、CodeOpt 流、覆盖 drain 尾巴未动。

## 2026-08-25 深夜追加（观者总览门控 + KB 落库 + 0.9.1 发布）

- **观者卡牌退出总览**（bd6c539/4137492）：引擎钩子=`CardModel.ShouldShowInCardLibrary`
  （getter，NCardLibraryGrid._Ready 唯一入册过滤）。`ArchivedCharacterGatePatch` 按池归属拦截；
  `CharacterArchive.ArchivedPools={WatcherCardPool}` 一处登记即全量生效——后续归档其它角色照抄。
  **刻意不注销模型**：保旧存档兼容 + ModelID 映射稳定（跨版本联机序列化安全）。
- **KB 知识库落库**（566552e，KBBuilder 产出）：research/sts1-kb/ 15 文件——四色+紫77+
  诅咒/状态/衍生/弃用卡、186 遗物、43 药水、54 事件，全部 en/zhs 双语原文，
  数值以字节码 super() 实参为准（javap 抽样对账零警告）。build_kb.mjs 可重跑。
- **0.9.1 发布**：dll c2d99b10 / pck 40025b18，live 与三 zip 字节一致；character.txt 三包各归各。
- **冒烟**（P1SMOKE4）：补丁失败 0；启动日志 `character archive: 77 model type(s)` 与 KB 紫色 77 张精确对账；
  autoslay 正常进主菜单选角。跳过按钮的可视/点击行为需真人局验证（Godot 无 UI 自动化）。
- **运行时序备忘**：hub 启动游戏窗口会抢占桌面焦点——用户在电脑前时先打招呼再弹窗。

## 2026-08-25 凌晨批次（审计双报告 + 三卷知识库 + Critic 修复潮）

### 独立审计（零上下文，均落盘 research/audits/）
- **ProjectCritic**（critique-20260825.md）：17 条问题 P1×2/P2×7/P3×8。总体评价工程素养高，但抓出两个真 P1。
- **DevlogAuditor**（devlog-audit-20260825.md）：需求表 27 条/结论表 45 条全证据核查——34 个提交哈希全部存在、live 部署物 md5 与记载一致、jar 反汇编坐实 GA 原文等三条关键结论；**无虚报**；5 处文档滞后列 C1-C5。

### Critic 修复潮（已全部提交）
- **P1 商店守卫**：`ShopEnoughGoldGuardPatch` 加 `AutoSlayImmortalityPatch.Active` 门控——此前无差别篡改所有对局的商店 EnoughGold 语义（UI 着色+失败原因+日志刷屏）。（fc9ef16）
- **P2 LessonLearned 致命谓词取反**：对照引擎 PowerModel.cs:646 默认 true + Feed/HandOfGreed/TheHunt 官方三例（dllsrc 全 `All(p=>p.…Fatal())` 无否定），一行修正；MinionPower=false 才是例外语义。（3cfbcf1）
- **P2 跳过按钮三缺陷**：每次开图复位 Disabled、文案走 TranslationServer 键 SPIRE1_UI_SKIP_NODE（ui.json 双语新表）、单次失效消除。（fc9ef16）
- **P2 BaseLib 浮动版本**：csproj 钉死 3.4.5。（4695124）
- **P3 zhs 缺表**：补 ancients.json（自译台词，非官方）、card_keywords/static_hover_tips 空镜像。（be0c902）
- **设置 UI 补盲区**：四个无文案开关（纯池/LocKeys/联机忽略差异/跳过按钮）双语 settings_ui 全量补齐。（65a858f）
- 遗留待用户：Girya 死遗物与 Nloth 空壳事件属内容设计决策；跳过按钮真人局验证。

### 知识库三卷齐备
- 一卷数据：research/sts1-kb/（460+ 双语条目，字节码权威）。
- 二卷语义：research/sts1-kb/mechanics/（MechKB 产出 **119 条规则**，javap 字节码控制流提取；用户示例"开局抽牌 vs 消耗 vs 抽到自动打出"在 draw-exhaust.md §6 有唯一确定裁决：三者不交错——初始抽牌为原子块、triggerWhenDrawn 仅五类牌、消耗链整批抽完后按队列位执行）。附 8 条任务书假设勘误（含"Havoc 属 triggerWhenDrawn"系讹传）。
- 项目 KB：research/kb/{engine-facts,aftp-interop,debug-protocols}.md + loc-drift-report.md（318 条目对账，274 对上，A/B/C 三级分类）；skill 已瘦身为纯方法。

### AFTP 线
- 许可证定案：主仓无 License（平台内 fork 私改合法，二进制发布需授权）；MPBalance=MIT。
- 双 fork 建立+克隆+构建绿：Twelve-eight/ActsFromThePast(7416aef 路径移植)、ActsFromThePastMultiplayerBalance（零修改即绿）。产物走 aftp-stage 不进 live。
- **火堆黑屏机制链锁定**：NRestSiteRoom._Ready L321-324 `GetNode("%RestSiteLighting")` 非 OrNull；AFTP 三幕自定义 tscn 任一加载失败/缺节点=黑屏；存档重启跳过入场转场故不复现。上游 issue 英文稿就绪 research/audits/aftp-upstream-issue-draft.md（待用户发送）。
- 我方通用救援已上线：RestSiteLightingRescuePatch（Finalizer 兜底背景+Postfix 注入灯光，对所有幕生效）。（fbff0a8）
- MPBalance 排除：源码零火堆接触面（纯战斗数值），嫌疑名单再划一人。
- 阻塞记录：本机未启用 AFTP 地牢，fork 实机验证待用户决策窗口。

### 夜间覆盖管线
- night_drain.ps1 循环器（hub name=night-drain）：连跑 --autoslay 至 10:45 硬停，逐局归档 godot.log → .tmp/p1-smoke/autoslay-NIGHT*.log。
- 中期覆盖（14 局时点）：IRONCLAD 43/48、SILENT 43/50、DEFECT 59/63、WATCHER 39/77（归档不动）。起始牌曾集体误报缺失——coverage.js 已修为双 id 记账（复用通道落原版 id 如 STRIKE_IRONCLAD）。

### 晚间追加（21:00-22:15）
- pure 稀有度带宽修复（全稀有度自研实现注入）、Armaments 升级数值、三卡双虚无剥离、药水机制反证（Concat 追加非替换）、DingyRug 全无色根因定案。
- Rewind 兼容：Cecil 补丁 attribute 5参→6参，启动 0 异常；pck 误删待用户重装（3a0de3d 起系列提交）。

## 2026-08-26 停止开发审查（freeze-review）

- 三路 reviewer（代码/架构/历史）+ 主会话交叉验证，全部发现经引擎源/git/日志二次取证。
- 实锤：High×4（非pure分支丢孪生注入→CHEESE崩溃回归；HandshakeResult struct缺ref→联机放行从未生效；RestSite救援缺Owner/UniqueNameInOwner→无效；三死开关零消费者）+ Med×8 + Low×8。
- 澄清：IRONCLAD 47/48 系 coverage.js THUNDERCLAP 蛇形化记账 bug，真实 48/48；历史修复声明无虚报但同病灶尾巴普遍（Feed 谓词未随 LessonLearned 修等）。
- 总报告 research/audits/freeze-review-20260826.md；三原始报告同目录 freeze-review-{code,arch,hist}-20260826.md。

## 2026-08-26 推倒重验 + 修补批（reverify）

- 四路 reviewer 重推 DEVLOG 全部结论（引擎事实 28/30✅、数值 19/24、修复声明无虚报、覆盖/联机 10/13）+ 主会话 16 条独立抽样。总报告 research/audits/reverify-20260826.md，四原始报告同目录 reverify-*-20260826.md。
- 推翻并当日修补（全部经 ilspycmd 反编译实锤 + RVFIX1 冒烟 exit 0）：①联机放行 struct 缺 ref（补 ref，4 源证实从未生效）②3a0de3d 误删非 pure 分支 Ironclad/Defect 孪生注入（恢复+删 PureSts1Adds 死代码+删 DarkShackles 双注入）③Feed 谓词取反（去反对齐引擎）④Maw NOM 击数 off-by-one（opening 计 turnCount）⑤火堆救援补 Owner+UniqueNameInOwner ⑥Armaments +3 Block 违官方回滚 ⑦三处代码注释订正。
- 澄清：IRONCLAD 覆盖真实 48/48（coverage.js 蛇形化记账 bug）；divergence zip 含遗物跨端分歧，「清单级假阳性」降级为「清单层排除+遗物分歧未解释」。
- 未验项：ref 联机运行时（需联机局）、火堆救援实机（需 AFTP 幕）。

## 2026-08-27 联机分歧攻坚 + 修复批（哨兵监视驱动）

### 联机分歧三大家族（LogWatch 哨兵 46 轮监视，5 份简报 research/audits/watch-20260827/）
- **A 经典粘液标记丢失**：AFTP ClassicSlimed 的 IsClassicSlimed 是本地 ConditionalWeakTable，网络重建端无标记→双端 Slimed 行为分叉（divergence #28/#286）
- **B RebalancedMode 配置单端**：本地配置双端不一致→同一选项索引两义（DUPLICATOR Host=Kneel/Remote=Leave，#55/#35）
- **C DARV×尘封魔典**：Remote 多出一个 DustyTome 顶掉 VELVET_CHOKER（#558）

### 修复（AFTP fork + Spire1 双仓，全部构建绿+反编译验证）
- AFTP：RebalancedModeEffective（MP 恒走原版分支，35 文件 75 处替换）+ ClassicSlimedOnPlayPatch MP 守卫（联机不整替）
- Spire1：InjectTwin 稀有度漂移三卡改注自研（Bludgeon/Acrobatics/Predator）；Token 卡 11 张归档 LegacyPool（Omega 出奖励修复）；DustyTome 回退过滤自研同名卡+空池兜底（Darv NRE 修复——冒烟实锤 FIXB1 复现/FIXB2 零 NRE）；删自研遗物 7+赤牛+药水 6（官方等价，数值逐字段核对）；Disarm StrengthLoss 正向化（双负号修复）；DualWield/cardsModifierTitle 文本修正
- 冒烟：FIXB1（发现 Darv NRE→修）FIXB2（NRE=0 验证通过；exit1 为 autoslay 不取宝箱缺蓝宝石钥匙的既有局限，历史 4/244 同款）
- FixReview 复核 8 项全过（.tmp/review-fixbatch.md→research/audits/watch-20260827/review-fixbatch.md）
- 已知观察（P3 不阻断）：删类对旧存档不兼容（当前阶段可弃）；Akabeko 官方 Uncommon vs StS1 Common 稀有度渗漏（用户已裁定删自研，接受）
## Session 21 — 2026-08-29：多人机制卷四拆解 + 家族D预防性修复 + 死等观察哨（HEAD df539b2）

用户令：拆解游戏原文件构建知识库——目标'通过原版机制推断并预防 bug（每人选项可以不同是机制）'，喊停前持续工作。

### 拆解→知识（卷四 research/sts1-kb/mechanics-v3/per-player-view-and-mp-divergence.md，212ebcf/1fafb2a）
- **引擎两种事件模型**：每玩家一份 EventModel 克隆（合法不同选项的机制基础）；非共享事件只同步'第几个'（OptionIndexChosenMessage），共享事件（全游戏仅 8 个）投票制 host 定胜
- **事件 RNG 派生式**（EventModel.cs L234）：runSeed+slotIndex+hash(事件Id)——同事件每人专属 RNG 流，'每人不同结果'是设计意图
- **奖励链**：RewardsSet.Id 接收端本地分配；set 未生成→消息**无限期缓冲**（家族C黑屏的直接机制）
- **卡死充要条件 V4-R6**：WaitForSync 无超时 await；解除=收齐或**对端断线**——黑屏只能强退的机制解释；排查口诀=日志冻在 Waiting to receive → 翻对端日志
- **checksum 次数一致要求**（L83-84 注释明文）：单端条件跳过=假阳性 divergence
- **RNG 三层表**：Run 层强制同步（host 下发快照）/Player 层各一份/Event 层各一份
- **模式清单 M1-M8** + 7 条 mod 写手 checklist

### 拆解→预防（两个当日修复）
1. **家族D候选（9b4c4fb）**：GenerateRooms 双端对称执行（StartNewMultiplayerRun 无 host/client 分支，NCharacterSelectScreen L726-790）——AFTP ShrinePatches 两键裸配置门控房间池变异=地图分歧。Effective 化修复，本地部署 317ad034，friends-pack v4 已重打
2. **M8 文本判等模式**：DarvOfferTracker 按 Title.GetFormattedText() 判等——跨语言双端可分歧（已被 Effective 守卫中和）；Spire1 侧扫描无违规（WeMeetAgain 是显示用非判等）

### 自查（checklist 首轮执行）
- Spire1 53 事件干净（'本地随机'命中全是 StS1 注释描述；'直Add牌'是官方同款模式；SecretPortal 转场 FLAGGED 未实现无路径）
- 自家 LegacyActSharedEventFilterPatch 类型判定门控=双端 dll 一致即安全
- AFTP 全仓裸配置扫描：仅 ShrinePatches 两键（已修）

### 防御补丁（df539b2）
- **CombatSyncStallWatchPatch**：WaitForSync Postfix 包 Task——60 秒未解打 Warn（含机制解释与排查指引）。零行为变化（超时后继续等原 task）。STALLW1 冒烟：胜利、零错误、零误报（单人早退路径正确跳过）
- 首版 Prefix 方案自审废弃（async 方法换 __result 的正确姿势是 Postfix 包 Task）——保留教训

### 部署态
- Spire1.dll 8d510cee（含观察哨）/ AFTP fork 317ad034（含家族C+D修复）→ friends-pack v4 已重打（**用户需把 v4 重发朋友**）

### 下一步（喊停前持续）
- 卷五候选：商店/遭遇/地图生成同步链拆解；M8 跨语言判等全生态扫（工坊 mod 也可能犯）
## Session 20 — 2026-08-28 晚：P6.2 小修批 + Critic 批评批 + 工作区盘点卷一二（HEAD 89fa137→c67bf42）

**四路 subagent 并行**（FixBatch/InvMods/InvResearch/Critic 29-33 分钟）+ 主会话集成验证。

### 产出
- **P6.2 全 9 项**（commit 079281b）：ShiftingPower participants 门控、SkipNode loc 键、min_game_version 0.111、Girya 挪表+占位符修复、zhs 36 条 flavor **逐字对齐 KB**（advisory 拦截了 subagent 自拟文案违反 AGENTS §6——机械 join 修正）、#if DEBUG 隔离、LegacySaveCompatPatch 最小剥离版（归档 vs 剥离待用户裁定）、Akabeko 调研跳过（无渗漏面）
- **Critic 16 条批评当日清偿**（报告归档 research/audits/critic-20260828.md）：#1 README 未定态表述+DARV 仍断警告+验证清单；#3 旧三包 dist/deprecated/；#5 Girya 坏占位符；#6 ecosystem 虚假声明修正；#7 FINAL.md 家族B归因勘误；#11 决策权归还（待裁定）；#13 STATUS 腐坏修正（33→25 遗物/假阳性降级/卡数 305→306）；#14 version 0.9.2；**#12 有据驳回**（mismatch 已是 Warn 级双横幅非 Info——批评报告也有错的时候）；#9 P6 瘦身
- **friends-pack v2**：新 dll 4badc11c（含 ShiftingPower 门控等全部 P6.2）+ pck aae4930e + README v2（轻量安装路径+验证清单）；包内四件哈希终验一致
- **知识库盘点卷一二**：inventory-mods.md（35 注册/30 在载基线+双源去重规则）、inventory-research.md（12 子目录导航+决策树+数据流图）、workspace-inventory.md 总索引；卷三（代码仓）待续
- **冒烟 P62FIX1**：Victory 29 战全胜；Spire1 35 条日志零 ERROR/WARN；loc 修复+幕过滤运行确认；legacy strip 空转（预期）

### 追记 2026-08-28 23:39-23:55 — 家族C黑屏现场取证+当场修复（fork f166f11）

**现场**（联机局，用户报告黑屏，进程活/日志冻住）：
- 23:39:38 双端投票走 (1,1) → MoveToMapCoordAction 执行 → Checksum 'Exiting event room EVENT.DARV' → CombatStateSynchronizer 'Waiting to receive all sync messages' → **日志永久停此行**（此后只剩 20 秒 NetQualityTracker 心跳）
- 决定性证据 L11636-11637：**同一 DARV 事件双端选项列表不同**——房主(ECTOPLASM/BLACK_STAR/ASTROLABE) vs 朋友(ECTOPLASM/PHILOSOPHERS_STONE/DUSTY_TOME)
- 朋友选 DUSTY_TOME → dustyTome.SetupForPlayer 生成先古牌奖励 RewardsSet id 12 → 房主端该 set 永不创建 → 5 条 RewardSelectedMessage 永久 Buffering（L11699-11716 连续 5 条 'hasn't been created yet'）→ 房间过渡卡死 → 黑屏

**根因**：DarvUniqueOffersPatch 只看裸配置 DarvOnlyInLegacyActs（本机 cfg=true，朋友默认 false）。08-27 的 RebalancedModeEffective 批（75 处）**漏了这两个同族配置**。

**修复**（照 Effective 模式）：DarvOnlyInLegacyActsEffective + LegacyEnemiesGiveClassicSlimedEffective（SP-only 合取）；调用点全换：DarvPatches ×2、粘液族 ×5、TimeEater。MP 恒走原版分支双端一致。构建 0 错误。

**部署态**：fork dll 96275db4 构建成功；**游戏进程占用中写不进** → 待部署标记 .tmp/pending-deploy-aftp.json，进程退出后补 cp。朋友包需再重打（含新 dll）。

**教训**：Effective 守卫批当时的正确范围=「所有本地差异类配置」而非「RebalancedMode 一个键」——同族键排查应当时做全。
### 教训
- subagent 自拟'风味文本'=发明数据——advisory 系统拦截有效；flavor 类必须机械 join KB
- Critic 值得开（16 条中 14 条实锤 1 条驳回 1 条部分）——但 #12 证明批评也要验
- 沙箱 EPERM subagent 落盘：artifact architecture 字段传递方案跑通（两次成功）

### 下一步
- 联机实测（朋友装 v2 包）→ P1 闭环；inventory 卷三（代码仓）；autoslay 钥匙策略

## Session 22 - 2026-08-29 - migration brief and model language firewall

### Migration state
- Added `SESSION-HANDOFF-20260829.md` as the single next-session entry. It records the vanilla MP teardown, DARV black-screen evidence, AFTP fixes, package hashes, open work, and next research order.
- Refreshed `dist/friends-pack.zip` as pack v4. The archive now contains the current local Spire1 DLL and PCK plus the current AFTP fork DLL and complete PCK. `character.txt=all` enables Ironclad, Silent, and Defect by default.
- Package verification: local and archive hashes match for all four binary files; archive contains seven mod files; no PDB or development residue.

### Language firewall
- Shared `../AGENTS.md` section 5 now requires model-bound text to use only Chinese, English, French, German, or Russian plus ASCII and approved control characters.
- Added `.cursor/rules/model-text-language.mdc`, `.cursor/hooks.json`, `.cursor/hooks/check-agent-text.mjs`, and `tools/check-agent-text.mjs`.
- The checker rejects non-approved Unicode scripts before model dispatch, reports code points and JSON paths, and never silently mutates source evidence. Hook mode fails closed.
- Han code points overlap Chinese and Japanese. The rule therefore forbids pasting unknown raw multilingual text and requires a local path plus line range instead.
- Verified: accepted Chinese, English, French, German, and Cyrillic sample; rejected Hiragana `U+3042`; valid hook input returned `permission=allow`; invalid hook input returned `permission=deny`.

### Decision
- Do not promise that a project hook can inspect transport outside the Codex hook lifecycle. The committed hook protects `beforeSubmitPrompt` and matching tool calls when project hooks are enabled; the CLI checker remains the explicit fallback.
- Rejection is preferred over transliteration because changing a code point can corrupt code, paths, hashes, logs, or source evidence.

### Verification snapshot
- Main repo package and local deployment hashes match: Spire1 DLL `8d510cee7022b94a1abdb65138d9a061`; Spire1 PCK `aae4930e99f24a2c983b4f323299507a`; AFTP DLL `317ad0345f64fccef14d727ddbc46563`; AFTP PCK `ba60133a597bf7b80bddcccdd4c493db`.
- The latest real runtime smoke remains `STALLW1`: victory, zero Spire1 errors, zero stall-watch warnings. This is single-player evidence only; real two-player validation remains open.

### Next session
- Start from `SESSION-HANDOFF-20260829.md`.
- Do not repeat the completed raw-config audit. Continue vanilla source teardown for shop, encounter, map, and room-transition synchronization, then sample-check new risk classes.

## Session 23 - 2026-08-30 - increment review (critic+1) + F1-F4 same-day fixes

Scope: everything after the critic audit (079281b). Four-way parallel review
(AftpForkReview/Spire1IncrementReview/KbVolumeAudit/HandoffHonestyAudit);
report at research/audits/increment-review-20260830.md (commit 4453019).

Findings and same-day fixes:
- F1 (P1) fork family-D polarity inversion: the two shared-event ALLOW flags are
  consumed under negation, so the plain SP&&raw pattern made the filter ALWAYS
  fire in MP (legacy acts lost all base shared events; pools stayed symmetric so
  no desync - content regression, masked in our dual-mod deployment by Spire1's
  unconditional gen-2 filter). Fixed fork 80d8216 (MP returns true), built 0
  errors, deployed AFTP dll 58310ad9. friends-pack.zip still holds old dll
  317ad034 - rebuild before sending to the friend.
- F2 (P2) hook wrapper resolved tools/check-agent-text.mjs via process.cwd();
  a foreign hook cwd would deny every Task/MCP call. Rewritten with
  import.meta.url resolution; verified allow exit 0 from project root AND a
  foreign cwd, deny path exit 2 intact (70a71b2).
- F3 (P2) KB vol5 corrections (a5fe4dc): FlavorSynchronizer is EndTurn/MapPing
  (NOT relic flavor rolls - that is a plain LocString lookup); ReactionSynchronizer
  is the emote wheel (NOT relic flash); OneOffSynchronizer is cross-peer one-off
  scenarios (merchant removal/chest gold/crystal sphere, ~232 lines, not
  mutual-exclusion); family table scope fixed (ActionQueueSynchronizer lives in
  GameActions.Multiplayer; Reward/EventCombat/ActChange syncers omitted);
  VoteForMapCoordMessage does not exist (it is VoteForMapCoordAction via the
  action queue, NMapScreen.cs L947-948); treasure vote has NO disconnect fallback
  - the "only release is disconnect" claim fails for the chest sister. Vol4
  refinement: client sync completion also needs the host SyncRngMessage
  (CheckSyncCompleted demands _rngSet != null) - third hang path.
- F4 (P3) handoff log pointer rotated (godot2026-08-29T15.51.05.log), DEVELOP.md
  counts 305/33 -> 306/25, hash block updated with the new dll and a
  rebuild-pack warning.

Verified clean: package chain (4 hashes + character.txt=all + no PDB residue),
stall watch (3 engine WaitForSync call sites, SP no-false-positive path,
STALLW1 log zero Spire1 ERROR/WARN), Effective wrappers (5 keys, zero raw-read
leftovers, NetService assigned before GenerateRooms), EpochModel/Act4 errors are
the known benign DEVLOG L701 item.

Process lessons (three provider cuts this session, all from ECHOING fullwidth
punctuation, never from reading):
- Dispatch rule going forward: task briefs must forbid verbatim quoting of
  source text; cite file:line and paraphrase in English ASCII. The checker only
  guards outgoing dispatch JSON, not mid-run agent echoes.
- Salvage path proven: a cut agent's transcript (KbVolumeAudit, 155 requests,
  killed on budget) yielded its complete findings by extracting assistant
  thinking blocks and ASCII-sanitizing them (.tmp/kbaudit-salvage-ascii.txt) -
  no re-run needed.
- New agent type language-translator (TRANSLATOR model role, no character
  restrictions) fixed the report's fullwidth punctuation to pass the firewall in
  26s. Use it for any file that must contain Chinese but pass the checker.

Deployment state: Spire1.dll 8d510cee / Spire1.pck aae4930e unchanged;
ActsFromThePast.dll now 58310ad9 (polarity fix) / pck ba60133a unchanged.
Real two-player validation still open; pack rebuild still open.

## Session 24 - 2026-08-30 - KB volume 6 teardown (shop/encounter/map/act transition)

Four hardened scouts (no-verbatim-quote briefs): ShopSyncTeardown (died at 155
req, no output - shop done by main session), EncounterRngTeardown (delivered
full 27m report), MapActTeardown (155 req, salvaged 24 thinking blocks/269KB),
AftpRiskAudit (155 req, salvaged 11 blocks/35KB). Volume landed:
research/sts1-kb/mechanics-v3/shop-encounter-map-transitions.md, indexed in
README.

Key facts (main session re-verified against engine source):
- Encounter selection is 100% run-level RNG: UpFront stream at run start rolls
  all acts' content (RunManager.cs L743-766 -> ActModel.cs L331-386); Unknown
  room types roll per point (UnknownMapPointOdds.cs L127-165). PlayerRngSet
  covers only Rewards/Shops/Transformations - encounters never touch it.
- Mutable encounters never cross the wire: per-peer local ToMutable, identical
  via seed formula runSeed+TotalFloor+hash(encounterId) (EncounterModel.cs
  L263-264). AI uses ONE shared MonsterAi stream (MonsterModel.cs L416-419).
- Shop shelves are per-player local rolls (PlayerRng.Shops, MerchantInventory.cs
  L100/L143); purchases broadcast via RewardSynchronizer (GoldLostMessage/
  RewardObtainedMessage/CardRemovedMessage, location-targeted, combat-buffered).
- Map topology is seed-derived (new Rng(runSeed, act_N_map), StandardActMap.cs
  L112-114) - zero messaging. Host-only map_point_selection RNG breaks vote
  ties (MapSelectionSynchronizer.cs L38-90).
- Act change = distributed AND of VoteToMoveToNextActAction through the action
  queue (ActChangeSynchronizer) - each peer independently runs EnterNextAct.

NEW engine gaps (all main-session verified, recorded in vol6 section 4):
1. MapSelectionSynchronizer has NO disconnect fallback - a disconnected player's
   vote slot stays empty forever, host never MoveToMapCoord, whole team stuck
   on the map screen. (Compare CombatState/RestSite which subscribe
   OnPeerDisconnected.)
2. ActChangeSynchronizer has NO disconnect fallback - a player who never votes
   blocks the act transition permanently.
3. EventCombatSynchronizer readiness barrier has no timeout/disconnect handling
   (option-message loss stalls that peer only).
Known engine TODO: SyncRngMessage.cs L12-15 - client Niche rollback may re-roll
the same value twice.

AFTP risk disposition (no code change this session):
- MatchAndKeep minigame: cards added in a UI callback only on the owner peer
  (NMatchAndKeepScreen.cs L517-518; MatchAndKeepMinigame.cs L121-127 IsMe gate;
  CardPileCmd.Add is local-only). Structural single-ended side effect - needs a
  real two-player test to grade (next combat SyncWithSerializedPlayer may heal
  or checksum may flag). Recorded in vol6 section 5.2 as candidate.
- SecretPortal wall-clock gate (SecretPortal.cs L31 RunTime) can disagree across
  peers - known, rebalanded branch already MP-guarded; wall-clock check itself
  unaddressed. Recorded with avoid guidance.

Process note: three of four scouts hit the 100-request soft budget (155 cap)
without yielding; the transcript-salvage path (extract assistant thinking
blocks, ASCII-sanitize) recovered all three completely. Budgets: deep teardown
needs effort=hi + explicit yield-at-90 instruction; briefs should cap tool
rounds.

## Session 25 - 2026-09-01 - AutoAnthony bridge (StS1 characters + random pools)

User installed Auto-Anthonyology (workshop 3786611028, "东尼算法") and wants StS1
characters to work with it. Full decompile of AutoAnthony 0.2.217 (765 files,
ilspycmd) -> .tmp/autoanthony/.

Root cause (verified in decompile): activation chain
ChaosCharacterMapping.From(CharacterModel) type-checks the five ENGINE character
classes (is Ironclad, ...). Spire1 characters are PlaceholderCharacterModel
subclasses -> never recognized -> SeedBeforeSingleplayerPatch.Prefix calls
DeactivateRun() and passes through; StS1 characters get zero random cards. Same
From() gates multiplayer (SeedBeforeMultiplayerPatch), save restore
(SeedBeforeLoadPatch -> From(SerializableRun)), and run history (From(RunHistory)).

Bridge (commit f680a2a, mod/Spire1Code/Interop/):
- AutoAnthonyCompatBridge: postfix all three From() overloads mapping
  SPIRE1 Ironclad/Silent/Defect -> same-named GeneratedCharacter (only when
  original returned null - engine characters untouched); prefix OUR three
  characters' CardPool getter (Chaos pool when IsRunActive &&
  IsCharacterRunActive) and StartingDeck getter (generated starters when
  ActiveReplaceStartingCards, mirroring CharacterPoolPatchRouting.ReplaceDeck).
  Watcher archived, not mapped.
- AutoAnthonyLoadHook: AssemblyLoad event fallback - no dependency edge between
  the mods, load order is the user's mod-list order; direct Apply at init when
  AutoAnthony loaded first.
- csproj: conditional Reference to .tmp/interop-refs/AutoAnthony.dll (workshop
  copy, gitignored) + SPIRE1_AUTOANTHONY define; absent -> stub, bridge off.
  Runtime resolution = same simple-name assembly already loaded by ModManager
  (identical to the BaseLib NuGet-vs-gamedir pattern). internal
  ChaosCharacterMapping resolved via Type.GetType + GetMethods reflection;
  postfix bodies use strong types (GeneratedCharacter is public in
  ChaosCardGenerator).

Design decisions:
- No manifest dependency on AutoAnthony (hard dep would Failed-load our mod for
  every user without it). Probe + silent disable instead.
- Strong-typed refs to its public API (ChaosRunDefinitions/ChaosCardRegistry/
  Chaos*CardPool) so an AutoAnthony breaking change fails OUR build loudly
  rather than drifting silently.
- Multiplayer: From() bridge is transparent to AutoAnthony's own MP contract
  (host-authoritative pool snapshot; regenerate != 0 -> throw). Both peers need
  both mods; MP sync is AutoAnthony's own responsibility.
- Same-name MP merge: two players picking engine Ironclad + SPIRE1 Ironclad
  share one generated Ironclad pool (AutoAnthony NormalizeCharacters dedups) -
  consistent with its existing same-character semantics.

Verification: build 0 errors 0 warnings, auto-deployed to mods/Spire1 (dll
contains AutoAnthonyCompatBridge/AutoAnthonyLoadHook types - string-scanned).
Smoke DEFERRED: game process in use by the user's live 4-player run (log shows
CHAOS_REGENT_CARD071 in action - AutoAnthony active there). Watcher
(hub name=bridge-smoke) waits for process exit, then sweeps --autoslay seeds
BRIDGE01..06 looking for the "AutoAnthony bridge:" log line; logs to
.tmp/p1-smoke/bridge-*.log.

Known coupling risk (documented in DEVELOP 7f): AutoAnthony updates that move/
rename From() or the registry APIs -> our patches no-op with an Error log line
(character mapping) or fail at Apply (pools). Re-audit on every AutoAnthony
version bump.

### Session 25 addendum - smoke result + user halt (2026-09-01 16:40)

Automated smoke (BRIDGE01, --autoslay): bridge LOADS and patches - "AutoAnthony
bridge applied (9 patch groups)" logged, AutoAnthony 0.2.217 initialized
alongside, 5 chaos pools generated (Ironclad 92 / Silent 94 / Defect 92 /
Necrobinder 92 / Regent 92 cards). But the run never exercised the mapping:
AutoSlayer picked an engine character (random per seed; no "AutoAnthony bridge:
<Char> ->" line, 0 CHAOS_ card plays in log). Watcher swept only 1 seed of 6
before the user's multiplayer session took the machine.

Also observed in that log: AutoAnthony's own startup audit error
"Expected 65 complete v111 Colorless cards, found 73" (its internal count check
vs the modded pool - pre-existing, not bridge-caused; bridge was not involved
in pool generation at startup).

USER DIRECTIVE (16:38): stop launching the game for testing ("停止把mod放进来检
测。我要和别人玩"). Machine is for multiplayer. mods/Spire1 and AFTP were
removed from the game dir by the user for the session. All watchers stopped
(bridge-smoke, bridge-smoke2). No process will be launched by the agent again
this session.

Final fix landed after the first build deployed (commit 496ad54): pool
replacement now global-on-IsRunActive (was gated on IsCharacterRunActive -
would have leaked unplayed StS1 vanilla pools into chaos-run prism pools;
AutoAnthony replaces ALL engine pools unconditionally). The fixed dll is in
mod/.godot/mono/temp/bin/Release/Spire1.dll (16:33 build); the 16:19 dll in
the game dir was deleted with the rest of mods/Spire1 by the user.

REMAINING VERIFICATION (user, whenever convenient, next solo session):
1. Restore mods/Spire1 (from mod/.godot/mono/temp/bin/Release/ or rebuild).
2. New run with a StS1 character (Ironclad/Silent/Defect) + AutoAnthony enabled:
   expect log "AutoAnthony bridge: Ironclad -> Ironclad generated pool",
   chaos-card starting deck (ReplaceStartingCards default on), chaos cards in
   rewards/shops.
3. A saved chaos run as StS1 character should reload with its chaos pool
   (From(SerializableRun) postfix).
MP note: both peers need both mods; host snapshot is authoritative
(AutoAnthony's own contract). A StS1 character + engine character sharing one
GeneratedCharacter share that chaos pool (AutoAnthony dedups by enum).

## Session 25 addendum 2 - upgrade-text-diff audit + MP divergence RCA (2026-09-01 evening)

USER REPORT: "武装 and 武装+ look identical - recurring, never caught by past
audits." Investigation generalized into a full audit + fix cycle:

Root cause (Armaments): behavior upgrade (_all=true) with ZERO upgrade-text
expression in cards.json. Engine renders one description; upgrade differences
must be carried by {IfUpgraded:show:}, -old-+new+ swap, or !var! diff syntax.

Audit tool .tmp/upgrade-diff-audit.mjs (4-class classifier: costOnly /
keyword / numeric / behavior; expression-body-aware OnUpgrade extractor;
swap syntax = -OLD-+NEW+ dashes-then-pluses, verified against
SimpleLoc.UpgradeSwapRegex). Iterations: v1 58 false positives -> accept any
!Name! var -> v2 29 -> separate costOnly/keyword -> v3 12 -> keyword class
added -> v4 5 -> swap satisfies numeric -> FINAL 0 after fix. All 5 confirmed
against research/sts1-kb official upgrade diffs.

Fixed 5 cards (eng+zhs): Armaments (one->all), Trip/Blind (single->ALL; zhs
was missing entirely, eng had broken swap remnants '.+ to ALL enemies.+' -
a half-finished past fix that explains "已出现过的bug"), Burst (zhs var +
'非攻击牌'->'技能牌' mistranslation), Stack (pile-count +3 swap).
End-to-end SimpleLoc.Simplify simulation on all 10 strings: correct
{IfUpgraded:show:|} and :diff() output. Build 0 errors 0 warnings, staged to
.tmp/deploy-stage (game mods/ dir deliberately untouched - user's MP session).

MP divergence RCA (user report "游戏出现了分歧" + "没有看见冒火特效"):
checksum 161, host-only POWER.METALLICIZE_POWER_A4H:17 on Terror Eel.
Act4Heart 1.1.7 GreenKeyHooks super-elite: map marking (SuperEliteQuest) runs
per-peer in ModifyGeneratedMapLate gated on LOCAL config keys_enable; combat
buff (seed+act derived 4-way roll) gated on the same local mark. User's local
dolso.act4_heart.config had keys_enable=false vs host true -> no flame on
user's map (matches "没看见冒火特效"), host applied Metallicize 17 ->
divergence, client kicked. Act4Heart's ConfigSynchronizer broadcasts host
config but never validates peer equality (version is host-side counter).
Mitigation: set keys_enable=true in local config (hot-reloaded via
FileSystemWatcher); structural gap documented in
research/audits/upgrade-text-diff-20260901.md appendix. Not our mod's fault
(Spire1/AFTP not even loaded that run per RitsuLib mod inventory).

## Session 25 addendum 3 - KB build (2026-09-01 night)

Knowledge consolidation from this session into the KB:

1. mechanics-v3 vol 7 (thirdparty-mod-interop.md): AutoAnthony 0.2.217 full
   architecture (activation single-entry ChaosCharacterMapping.From, global
   pool replacement vs per-character starting deck, host-authoritative pool
   snapshot contract, 514 shell cards driven by per-run definitions),
   Act4Heart 1.1.7 super-elite divergence mechanics (local-config-gated map
   hooks = C-grade divergence source; 3-tier safety model A/B/C for mod map/
   combat hooks), bridge methodology SOP (single entry, respect-original-
   semantics, dependency-free probing, compile-time ref + conditional symbol,
   strong-type public APIs so upstream drift fails our build).
2. engine-facts.md: upgrade-text rendering rules (IfUpgraded:show / -old-
   +new+ swap / diff vars; cost badge & keyword line auto-render), upgrade
   4-class taxonomy, card ID formation chain (Slugify + BaseLib prefix),
   mod load order (topological, no order guarantee between unrelated mods ->
   AssemblyLoad fallback pattern), same-name assembly resolution, Act4Heart
   local-config divergence fact, virtual-dispatch-as-hook-surface fact.
3. pitfalls.md: P-11 (upgrade behavior without text expression - the
   Armaments-class defect family + audit blind-spot root cause), P-12
   (third-party local-config-gated hooks causing MP divergence + diagnostic
   path via RitsuLib bundle).

## Session 26 - critic wave + fixed-build smoke (2026-09-01 night)

User directive: smoke allowed; check keyword overlap + possible divergence;
re-validate ALL historical issues from devlogs; isolated-context critics.

Four isolated critics dispatched (reports: .tmp/critic-wave-20260901/):
1. HistoryRevalidationCritic: 96 issues inventoried from pitfalls P-01..P-12 +
   both DEVLOGs + all audits. ZERO REGRESSED. All 3 named regression threads
   (twin injection, Armaments +3, PureSts1Adds) verified recovered at HEAD.
   67 HOLDS, 4 PARTIAL (P-01 residue, N-8, L-8, F-7 tails), 12 OPEN (H-4 dead
   switches, M-3 PureWater, M-4 MarkOfPain, L-1 case-sensitive reflection,
   coverage.js tool bug, stale comments), 5 external-unverifiable. NEW: M-3/M-4
   loc-vs-code contradictions are the top player-visible open items.
2. KeywordOverlapCritic: 33 P-01 double-render instances / 29 cards (residue
   classes the original 48-card pass missed: trailing Exhaust tails + ALL
   non-Exhaust keywords). 5 newly-edited upgrade-text cards verified clean;
   zero orphan swap syntax. Adjacent: FiendFire missing keyword + stale zhs
   line; Void zhs gain/lose verb reversal; Catalyst zhs missing upgrade swap;
   loc-drift-report empty-description recommendation is a hazard (would create
   doubles).
3. CodeQualityCritic (reviewer): 1 BLOCKER - bridge static maps typed with
   AutoAnthony enum force cctor -> dll resolution before presence probe ->
   cached TypeInitializationException kills whole Spire1 initializer on
   absent/Spire1-first loads (empirically confirmed off-repo with a CLR repro).
   + null-guard, partial-patch latch, Burst grammar, Stack coupling, csproj
   binary-drift notes. All Harmony signatures + 10 loc swaps verified correct.
4. DivergenceRiskCritic: stopped over budget after full read pass (155 req);
   reclaimed by Main per AGENTS 11. Main verification: all RNG consumers use
   deterministic engine channels (CombatCardSelection/CombatTargets/
   encounter Rng); zero wall-clock/UI-gated mutation in our code; statics all
   read-only content; all patches mode-gated. No High-severity divergence
   source in mod/Spire1Code. Bridge cctor blocker is load-time (not
   divergence) but fixed pre-MP anyway.

Fixes applied (commit 96b6589): bridge cctor (int-keyed maps + try/catch),
null guard, 33 keyword fixes + FiendFire/Void/Burst/Stack. Build 0e0w,
deployed. 13-case loc assertion suite passes.

Smoke on fixed build:
- SMK2601 (earlier build): Defect -> bridge fired ('AutoAnthony bridge:
  Defect -> Defect generated pool'), chaos deck played, VICTORY, 1 benign
  AA self-audit error (Colorless 65 vs 73 - AA's own bare-count check vs
  modded pools).
- FIX2601 (fixed build, in progress at time of writing): Regent (native AA
  path, no bridge involvement - control case), chaos pool active,
  CHAOS_REGENT cards playing normally. One AA warning observed:
  'Batch hand transform expected 2 replacement(s) in Hand, but received 0' -
  AutoAnthony-internal, on an engine character without any Spire1 code in
  the path; logged for upstream attention, not ours.
- Sweep continues (FIX2602..) until a Spire1 character hits on the fixed
  build to re-verify the bridge path post-blocker-fix.

User note: user watched the Regent smoke run live ("this run rolled
Regent!") - confirmed it is the AutoSlay test run; machine usage coordinated.

### Session 26 addendum - fixed-build bridge-path coverage closed (2026-09-01 late)

Honesty fix: SMK2601 (bridge Defect hit) was the PRE-blocker-fix build. The
fixed build (96b6589) had only run Regent (native path). Sweep2 (script
.tmp/night/bridge_smoke2.ps1, pattern anchored to 'AutoAnthony bridge: <X> ->'
mapping line, not the 'applied' line - the earlier PowerShell match was a
false positive from Select-String matching 'bridge applied'):

FXB2601 FIRST SEED: Spire1 Defect picked, bridge mapping line fired
('AutoAnthony bridge: Defect -> Defect generated pool'), 798 CHAOS_DEFECT
card plays, VICTORY, single benign error (AutoAnthony's own Colorless 65-vs-73
bare-count self-audit, pre-existing, modded-pool related, non-blocking).

Bridge-path runtime verification on the fixed build: CLOSED.
Full smoke matrix: Defect+bridge (pre-fix SMK2601, post-fix FXB2601) both
victory; Regent native control (FIX2601) victory. Machine idle again.

## 2026-09-02 (session 27) — Watcher bridge smoke green; ChaosBridge v0.1.0 built, verified, published

**Watcher bridge (this repo)** — commit 3ac7fec, smoke WCH2607 all green:
`workshop Watcher -> Colorless pool` + `Watcher -> Ironclad` carrier lines, native
Watcher starters played, 360 CHAOS_COLORLESS plays / 0 CHAOS_IRONCLAD (carrier
did not leak into pool), victory. 34 errors all third-party (33x Rewind mod's
pre-existing Watcher KeyNotFoundException, 1x AA's benign colorless self-audit).

**ChaosBridge (standalone universal mod)** — `G:\omp works\chaosbridge`, published
to github.com/Twelve-eight/chaosbridge (private). Design: docs/chaosbridge-design.md
(commit 238a151). Mechanism identical to the Watcher mapping: Ironclad activation
carrier + colorless pool identity + native starting deck, but auto-registered for
EVERY unrecognized modded character (ModelDb.AllCharacters minus engine five minus
already-patched getters via Harmony.GetPatchInfo deferral).

Gotchas proven this session:
- ModManager scans mods/ at ANY depth — renaming to `Spire1.disabled/` still loads
  it. To truly disable, move the folder out of the mods tree entirely.
- ModelDb.AllCharacters cannot be enumerated at mod-initializer time (content
  tables empty — KeyNotFoundException 'CHARACTER.IRONCLAD'). ChaosBridge scans
  lazily at first ChaosCharacterMapping.From call.
- BaseLib SimpleModConfig auto-generates its UI from public static properties —
  no manual Register calls.

Verification: user's live session (manual, no AutoSlay) with Spire1 temporarily
stashed became the e2e test — ChaosBridge fully owned the workshop Watcher
(registration + carrier lines, chaos pool active). Coexistence verified earlier in
CB2601/CB2602 (Spire1 chars deferred). Spire1 mod restored to mods/ afterwards.

**Pending**: AFTP Old Beggar zhs text issue — static audit found nothing (771/771
keys, text matches official StS1 verbatim incl. the odd-but-official '裹着毛衣',
tags engine-supported, workshop 1.0.5 has latest zhs). Waiting for user screenshot
of the actual rendering next time they hit the event.

## 2026-09-02 (session 27, evening) — Pandora's Box watcher-pool leak: root cause, fix, audit

User report: watcher chaos run, Pandora's Box from Darv transformed native
watcher starters into vanilla purple cards. Full analysis in
docs/pandora-watcher-pool-leak.md (commit e40db70). Short version:

- AA's Pandora patch transforms EVERY transformable card via original.Pool;
  chaos cards land in AA-swapped pools, native watcher cards land in
  WatcherCardPool which AA never swaps. Bridge had pool identity but not pool
  contents for the watcher's own pool.
- Fix: WatcherCardPool.AllCards prefix (chaos colorless contents; AA preserved
  originals appended when PreserveOriginalCards) + AllCardIds postfix (ID union
  prevents InvalidProgramException from CardModel.Pool ID-scan).
- Same-class audit: Astrolabe/CardTransformation share the path (covered);
  merchant/reward/combat paths go through redirected Character.CardPool
  (covered); Fasten .First(Defend) downgraded after audit.

Smoke PDB2805 (fixed build, watcher): patch line logged, native deck kept, 476
chaos plays, 0 exceptions, victory. Direct Pandora-hit seed NOT yet obtained
(both sweeps interrupted: round 1 exhausted budget without watcher+pandora
coincidence, round 2 killed by user shutdown). REOPEN VERIFICATION NEXT
SESSION: sweep seeds until watcher run obtains PANDORAS_BOX; expect transformed
outputs all CHAOS_COLORLESS_*, zero new vanilla WATCHER_* mid-run.

Also this session: NoBlockFromCards frequency question (answer pending — atom
catalog math computed: colorless catalog has 52 recipes / NoBlockFromCards in
exactly 1 recipe PanicButton, severe-downside pool is 3 templates; family-pick
probability analysis was interrupted by the Pandora report; resume if user
still cares after seeing numbers).

## 2026-09-04 (session 28) — KB 深化：机制优先级仲裁 8 卷（主会话单线）

用户指令：主会话单线把 StS1/StS2 知识库丰富到"渎神自动死亡 vs 无实体谁优先级高"
这类大批量机制交互仲裁的程度，所有方向同等深度；完成后反思复查一轮。

**新增卷（全部字节码/C# 源取证，编号规则可引用）** — `research/sts1-kb/mechanics/`：
- death-arbitration.md R01-R22：**旗舰裁决=渎神 vs 无实体**。渎神死亡本体是
  EndTurnDeathPower.atStartOfTurn 队列的 LoseHPAction(99999, HP_LOSS)（非脚本死）；
  玩家 damage() 入口无类型门控钳制（>1→1）可拦它；但无实体时长递减在 atEndOfRound
  （新回合块第 1 步）而渎神在 applyStartOfTurnPowers（第 5 步），ReducePowerAction 在
  amount>=power.amount 时 addToTop(Remove) ⇒ **1 层不救（先到期）、≥2 层救（钳 1）、
  回合开始钩子新施加的 1 层也救（不经到期路径）**。妖精/蜥蜴尾可救、MotB 短路、
  钨杆只 -1、Buffer 归零（onAttackedToChangeDamage 无类型门控）。用户验收口径
  （≥2 层或确保下回合开始有无实体→只掉 1 血）与 R19+注 4 一致。
- defense-powers.md R01-R10：五层防御干预点；无实体玩家版(>1,改局部变量)vs怪物版
  (>0,改 info.output)；Buffer 逐源/Invincible 回合预算；④层钩子吃穿透格挡后的余量。
- orbs.md R01-R13：通道全序（满槽=addToTop 三连 Animate→Evoke 最左→Channel；非
  autoEvoke 满槽静默失败）；evokeNewestOrb 不移除=Multi-Cast 基础；RemoveAllOrbs
  不触发 onEvoke；Dark 累积不随 Focus 重算。
- stances.md R01-R12：ChangeStanceAction 全序（订阅者先于 Calm 退场能量；同姿态幂等；
  CannotChangeStancePower 本 build 无施加者=死门）；stance.atStartOfTurn 调用点结案
  （applyStartOfTurnRelics 首条指令，关闭 triggers.md 开放问题 1）；Divinity 自退先于
  渎神 LoseHP 入队；uniqueStancesThisCombat 维护但零消费者。
- energy-cost.md R01-R10：hasEnoughEnergy 七道门序；Confusion 改 cost 本体。
- potions-combat.md R01-R06：药水 use() 是 UI 点击帧同步直调（不走动作队列）；
  妖精自动使用不触发 relics.onUsePotion（唯一实现者 Toy Ornithopter）；SmokeBomb
  同步置 room.smoked/isEscaping。
- monster-ai.md R01-R10：rollMove=getMove(aiRng.random(99))；moveHistory 写入
  （byte==-1 不记史）与 lastMove 族；意图管线。

**新增卷** — `research/kb/sts2-combat-semantics.md` S01-S14（engine-dllsrc C#）：
AttackCommand.Execute（ModifyAttackHitCount 钩子、每击刷新存活目标、
CalculatedDamageVar 逐击重算——与 StS1 单快照相反）；CreatureCmd.Damage 全序
（ModifyDamage 统一入口=附魔→加→乘→帽；Osty 前后双相位掉血修正；死亡批量后置）；
Kill/ShouldDie+preventer+递归 10（StS2 免死形态）；PowerCmd 三态叠层
（InstancedPerApplier!）+ SkipNextDurationTick 仅玩家侧 debuff；StS1→StS2 仲裁速查表。

**反思复查修正的 4 个错误（自查自纠）**：
1. defense-powers R09 钨杆挂点偏移写错（918-936→onBloodied 段；实为 damage() offset 466）。
2. orbs R12"失去 Focus 珠回落"错误：onModifyPower 的珠刷新循环有 hasPower("Focus") 门，
   Focus 1→0 走 Remove 后门为假 ⇒ 已有珠**冻结在 base+1 不回落**（增=刷新/减=逐次/
   移除=冻结的不对称）。
3. monster-ai R06"power 增删不刷新意图"错误：applyPowers 内含 calculateDamage+
   intentImg/tip 刷新，onModifyPower 即时重算意图；实伤快照可能短暂≠显示。
4. turn-phase R02"能量跨回合保留"错误：每回合重置发生在 PlayerTurnEffect 构造器
   （DrawCardAction 3 参 true 构造时同步调 energy.recharge()）；vanilla setEnergy 硬重置，
   冰淇淋/Conserve 才叠加保留。atStartOfTurn 读旧余额、PostDraw 后读模板值。

**其他**：
- javap 反汇编快照（52 个 .txt）留在 research/sts1-kb/.tmp-javap/ 供对账（cls/ 已 gitignore）。
- 提交 63a9841 里混入了 docs/CODE-REVIEW-20260904.md——另一会话的进行中审阅报告，
  被 git add -A 顺带提交，非本会话产出；未改动其内容，请相关会话知悉。
- KB 规则总数：mechanics/ 119→202 条。

## 2026-09-05 (session 29) — KB 持续深化第 2 批：全量矩阵 + StS2 四卷 + 方法卷（主会话单线，持续工作中）

用户指令：主会话单线程研究知识库，在要求停止前持续工作。追加要求：研究中遇到的
实际问题与方法（内联转义等）也要记录。

**新增卷（8 篇，提交 dc193cf..d48be1f）**：
- research/kb/research-methods.md M1-M17：方法与实录坑（javap 别走 -cp；unzip 通配符
  不跨目录 + 清单 CRLF 陷阱；MSYS 反斜杠/ugrep 怪癖/Node 内联转义→落文件；常量池扫描
  法及其"引用≠调用"局限；钩子扫描必须签名正则（子串污染实录）；双钩子复核；FIFO/
  构造期 vs 执行期时序推导；onModifyPower 全局刷新枢纽；随机源分账；StS2 C# 工作流；
  自检纪律 M15-M17）。工具固化 research/sts1-kb/scan-hooks.mjs。
- mechanics/power-lifecycle.md R01-R12：161 power 全量钩子矩阵。要点：毒在持有者
  自己回合开始结算；Metallicize/PlatedArmor/LikeWater 只挂 PreEndTurnCards（金属化
  格挡能挡悔恨）；Equilibrium/Ritual/Malleable 双钩子各司其职；justApplied 9 家族；
  DoubleDamagePower 在 give 层。
- mechanics/relic-triggers.md R01-R16：190 遗物全量钩子矩阵。要点：规则位 boss 遗物
  （Sozu/Ectoplasm/BustedCrown/FusionHammer/RunicDome/CoffeeDripper）零战斗钩子=
  引擎 hasRelic 查询建模；MarkOfPain 真实挂点 atBattleStart（常量池"引用≠调用"实证）。
- mechanics/turn-control.md R01-R06：ScryAction 全序（onScry→选顶 N 张→确认→
  moveToDiscardPile→弃牌堆 triggerOnScry 直调）；skipMonsterTurn 消费者全集
  （Vault 使 applyEndOfTurnPowers 整段跳过 ⇒ 玩家 debuff 该轮不递减）；
  callEndTurnEarlySequence 绕过哨兵链（TimeWarp/Vault 类回合尾少跑一整段）。
- kb/sts2-card-play.md C01-C06：手动出牌先扣资源再 OnPlay（与 StS1 相反）；
  星星按 1:2 抵超额能量钩子；OnPlayWrapper 主循环（playCount 循环重跑=复制品模型、
  附魔 OnPlay 在卡效果后、ResultPile 分流 None/Exhaust/Add）；AutoPlay 免费且 X 捕获
  全部能量。
- kb/sts2-monster-ai.md A01-A07：显式 FSM（ConditionalBranch 按序取首个真分支、
  首个 move 锁初始状态、RunRng.MonsterAi 独立流）；进场即 RollMove；敌人回合循环
  逐怪间 CheckWinCondition；AmountOnTurnStart 快照；ShouldClearBlock preventer
  （Barricade=preventer 钩子非特判）；CheckForEmptyHand 延迟检查设计注记。
- kb/sts2-combat-turn-machine.md T01-T08：PlayerTurnPhase 全集；回合循环持有
  CombatTurnState 快照（跨战斗防串扰）；EndTurnSignal 等待 RunningAction 完成 +
  陈旧信号防护；回合尾两段式（AutoPostPlay→回合尾卡→Ethereal(ShouldEtherealTrigger)
  →BeforeFlush→Flush(ShouldFlush 总闸+逐卡 retain)→AfterSideTurnEnd）。
- kb/sts2-orbs-enchantments.md O01-O07：Channel 自动加首槽+满槽先 EvokeNext；
  EvokeNext/Last/dequeue:false 与 StS1 激发家族一一映射；TriggerPassive 的
  ModifyOrbPassiveTriggerCount=Cables 泛化、ModifyOrbValue=Focus 泛化（无冻结回落
  问题）；附魔一卡一枚、同类型叠数值、伤害计算最外层、OnPlay 在卡效果后。

**索引更新**：mechanics/ 202→236 条；kb/README 新增 6 行索引。
**状态**：持续工作中（用户未叫停）。下一批候选：StS1 逐怪行为数据层、StS2
Affliction 系统、OrbModel 子类数据、StS2 ChecksumTracker/联机一致性、 relics.json
与 relic-triggers.md 对账。

## 2026-09-05 (session 29 续) — KB 持续深化第 3 批（主会话单线，仍在继续）

新增卷（提交 3df6f61..32a1ea9）：
- mechanics/card-rewards.md R01-R07：奖励卡池全序。Blizzard 保底精确常数
  （StartOffset=5/Growth=1/MaxOffset=-40；COMMON 递减、RARE 重置、UNCOMMON 不变；
  roll=cardRng.random(99)+blizz ⇒ 开战首张奖励不可能 RARE）。阈值：普通 3/37、
  精英 10/40（MonsterRoomElite ctor 直证）。RARE 不走自动升级骰。
  player.getCardPool = 角色卡池引擎覆写点（chaosbridge 对应）。
- kb/sts2-hook-matrix.md + scan-sts2-hooks.mjs + JSON：62/71 钩子的实现者全名单。
  架构结论：StS2 分发=遍历全模型调虚方法，无容器/插入序——次序由 Early/Late 变体
  钩子显式表达（14 个变体清单）。免费系挂 BeforeCardPlayed、计数遗物族挂
  AfterCardPlayed（每遍 playCount 一次）；Poison/DemonForm 在 StS2 挂
  AfterSideTurnStart；BiasedCognition 在 StS2 是 Power。
- kb/sts2-afflictions.md F01-F04：ShouldAfflict 引擎钩子 + 类型白名单 + Unplayable
  默认拒；一卡一附灵同类型叠数值（与附魔同构）；OnPlay 在附魔之后；
  Hexed 示范"附灵卡面层 + Power 结算层"双件套。7 个 vanilla 附灵清单。
- mechanics/loot-rewards.md L01-L07：药水稀有度 65/25/10（potionRng）；宝箱
  treasureRng；商店价 = getPrice(rarity) × merchantRng.random(0.9,1.1)，基础价
  50/75/150/9999；四 RNG 流分账重申。

索引：mechanics/ 250 条；kb/README 补 hook-matrix 与 afflictions 行。

下一批候选（按价值排序）：
1. StS2 玩家回合 SetupPlayerTurn 逐行（能量重置/抽牌数/开场球触发的确切顺序）
2. StS1 各幕 cardUpgradedChance 具体值（card-rewards 开放问题 2）
3. StS1 宝箱三档概率字段值（loot-rewards 开放问题 1）
4. StS2 OrbModel 子类数据层（Frost/Lightning 等价物数值）
5. StS1 地图生成（大卷，chaosbridge 不急需）

## 2026-09-05 (session 29 续2) — 用户点题：池架构/玩法不变量卷 + 池归属 lint + 方向总结

用户以两起实机事故（Splash 候选集、AutoAnthony 对第三方角色失效）点题：KB 要丰富到
"完整记录玩法机制、代码实现逻辑"的程度，使 asker 型 agent 能直接读出 bug——autoslay
类机械冒烟在此类问题上零检出能力（两案均为用户实机揭示）。

**事故原貌取证**（DEVLOG 保真）：
- Splash（修复 #10，DEVLOG:740）：原实现 list.Remove(owner.CardPool) 按池**对象**排除；
  SharedCardReuse 令"可调用集合"⊃"自己池对象"，移植 Defect 从"其他角色"选出自己已有
  的官方 Defect 卡。修复 = SplashOwnSetSubtractPatch 按 Id.Entry 集合差。
- AutoAnthony（Session 25，DEVLOG:1097）：ChaosCharacterMapping.From 类型门只认五个
  引擎角色类；第三方角色（Placeholder 子类）不可见 → 零随机牌。桥接 = Spire1 桥 +
  ChaosBridge 通用接管（池身份+池内容）。用户记忆中"工坊观者未建紫色池"为同根因的
  早期表述（chaosbridge-design：AA 从不 swap WatcherCardPool）。

**新增卷**：research/kb/pool-architecture.md —— I0 两代池架构（StS2 关键证据：
ModelDb.AllCharacters 是硬编码 5 元素数组，ModelDb.cs:145；CardPoolModel.AllCardIds
HashSet 天然支持集合差）；I1 可调用集合≠池对象；I2 池注册契约三条（角色注册/卡归属/
容量——ROOM_FULL_OF_CHEESE ≥8 Common）；I3 颜色池不相交与"无色=共享"假设。
每条含：陈述/vanilla 为何安全/mod 如何打破/正确模式/检测手段+冒烟盲区声明。

**开发模式提升（卷内 G1-G4）**：
- G1 KB 先行：跨池选牌类特性必须先写集合运算规格（全集/排除集/容量/第三方行为）。
- G2 语义评审门（asker 位）：pool/registry 改动合入前对照不变量清单逐条提问。
- G3 静态 lint：tools/pool-audit.mjs 落地（[Pool] 归属解析 + SharedCardReuse 孪生
  白名单；兼容 C# 主构造函数；注意 [Pool] Inherited=true——归属可来自基类链）。
  首跑基线：306 文件/310 类，全部可解析归属，0 孤儿；Spire1CardPool 8 直挂+31 孪生。
- G4 冒烟边界声明：autoslay 检出域=崩溃/异常/资产缺失/覆盖增长；玩法语义偏差
  （选错集合/第三方失效/错色错池）不在其检出域，不得以冒烟绿放行此类特性。

**研究方向总结（用户两案 → KB 待办）**：
D1 池架构与归属契约（已成卷）；D2 可调用集合语义（已成卷）；D3 引擎注册表可见性
与第三方兼容层（chaosbridge-design 已有，卷内互链）；D4 池容量契约（已成卷）；
D5 玩法语义不变量目录（卷已建立，今后每次"vanilla 安全假设被 mod 打破"的事故追加
一条）；D6 开发模式四件套（已成卷）。
后续深挖候选：①StS2 原生"跨池选牌"卡全量清单（哪些卡用了池并集/差集，逐一标注算
法定义）；②ModHelper.ConcatModelsFromMods 的注册顺序与去重语义；③StS2
ModelDb.AllCards 与 AllCharacters 的补丁注入点全景（BaseLib/ChaosBridge/AA 三方）。

## 2026-09-05 (session 29 续3) — 22:53 恢复工作 + 评审门首轮实测

用户指出上一轮在 19:33 提前收束（违反"持续工作"常设指令）。复盘：状态无损
（末提交 d4be0c3 于 19:32 已推送），本段补上承诺的交付并继续。

**新增交付**（提交 86ced72 / 6bf99b8）：
- research/kb/semantics-review-checklist.md：G2 固化——P1-P8（池/注册表）+
  M1-M4（模型/联机）提问表，附可整段派发的 reviewer 提示词（只引用 KB 路径防漂移）。
- research/kb/invariants.md I4-I10：联机状态一致性（粘液失同步验尸）、canonical/
  mutable 生命周期（注入器四代失败复盘）、资产存在≠内容（302 占位图）、本地化变量
  权威（修复 #9）、标识符正则坑（Spire1CardPool）、事件池隐式数量（CHEESE/DustyTome）、
  注册时序（I0b+）。全部带 DEVLOG 行号锚点。
- **评审门首轮实测（G3+G2 联动）**：
  - P1：grep 池对象排除模式 → 唯一命中 SplashOwnSetSubtractPatch.cs:45，确认为
    "Remove 后跟 Id 差集"的正例锚点；P1 措辞已按此细化。
  - I7：选牌提示键交叉核对 → **抓到疑似真缺陷**：DualWield 两语言缺
    .selectionScreenPrompt 且直读 protected getter（CardModel.cs:129-141 缺键即抛）
    ⇒ 玩到即炸风险；ForeignInfluence/Wish 走 3 参重载疑似良性。附带教训：loc 键为
    扁平复合串，嵌套解析假阴性。
  - **定性边界**：未改码（本会话为 KB 会话）；修复属实现会话（补两键+定向冒烟），
    修复前先用控制台 card play 验证 DualWield 是否真触达 throw（覆盖矩阵未解释
    为何 8 胜冒烟未炸——可能从未被抽到/打出）。
- 续3补：评审门机械子集固化为 tools/semantics-audit.mjs（P4+P1+I7 一键运行；
  首跑 verdict=request-changes，DualWield 发现在案）。提交 06f343a。
  下一方向（按序）：①DualWield 定向验证+修复（实现会话：控制台 card play → 补两键 → 冒烟）
  ②不变量目录继续扩充（StS2 联机池快照、事件卡牌来源审计）
  ③G2 清单与 tools 的联动已闭环，等待下一次实机事故回填。
- **勘误（06f343a 段）**：DualWield"缺键即炸"为 lint 误报——卡牌 loc id 分隔符是下划线
  （SPIRE1-DUAL_WIELD，键两语言都在），连字符是 events 域习惯。ForeignInfluence/Wish
  走 3 参 FromChooseACardScreen（横幅=通用 CHOOSE_CARD_HEADER）不需要键。审计器已改
  （I7 只约束直读 SelectionScreenPrompt 的卡），门全绿。教训入 research-methods M18
  （宣告发现前先从存量键反推命名约定）。提交 8b7cf57。
- 续3再补：I11 候选集双重过滤契约（解锁态+联机约束；GetUnlockedCards 唯一规范入口，
  直读 AllCards 双漏）。评审清单加 I11 行。提交 0d13fca。
  KB 状态小结（本日 19:33-23:0x 连续段）：新增/修正 research-methods(M18/M19)、
  pool-architecture(I0b+)、cross-pool(C 系列)、event-pool-usage(E 系列)、
  invariants(I4-I11)、semantics-review-checklist(P/M/I11 三套提问)、
  tools/{pool-audit,semantics-audit}.mjs。全部已推送。
- 续3三补：StS1 事件赠卡普查（R08-R09，56 事件类；getCopy 指定卡为主/池随机为辅，
  与 StS2 CreateForReward 范式对照）+ 工具 stS1-event-pool-usage.js。提交 244a67d。
  注：本轮 node -e 内联第三次踩坑后已全部改落文件脚本（M5 纪律执行）。
- 续3四补：I12 联机同步拓扑+checksum 纪律（ChecksumTracker 全文：host 比对/20 条滚动窗/
  id 配对乱序队列/StateDivergenceException；11 个 Synchronizer 通道清单）；EA 宝珠普查
  O08（五珠数值与 StS1 同源、Glass 新珠 evoke=Passive×2、Plasma 豁免 ModifyOrbValue、
  Plasma 唯一回合开始珠）。提交 9e8adb4/7a08890。
- 续3五补：StS1 逐怪钩子面普查数据层 v0（73 类；0 怪物覆写 rollMove/首掷由基类
  universalPreBattle 承担；逃逸家族=5 Gremlin；changeState 37/damage 35/die 46/
  preBattle 39；GremlinLeader summon）。工具 stS1-monster-scan.js + JSON。提交 47f4c99。
- 续3六补：T06 结案（checksum 插桩点全量：CombatManager 五处+EventRoom+RestSite+
  RunManager action 级）；kb/sts2-mock-testing.md T1-T3（46 官方 Mock 类分布、
  Mock=真实模型子类的测试哲学、TestMode 表现层闸门、本项目 Mock→控制台→冒烟
  三层验证策略）。提交 4d208f6/75473ef。
- 续3七补：I0c 引擎角色五件套（Ironclad/Silent/Regent/Necrobinder/Defect 四绑定全录；
  官方 StS2 自带 Defect ⇒ 移植一代 Defect 的同名卡碰撞是结构性的）。提交 1d44de1。
  本段连续增量小结（用户"继续"指令）：I12/宝珠 O08/怪物普查/Checksum 结案/Mock 卷/
  I0c 五角色 + 此前 I4-I11/评审门/两普查/审计器。KB 完整度显著提升，全部已推送。
- 续3八补：I13 存档持久面契约（SerializableCard 五字段白名单；跨战斗成长必须
  [SavedProperty]；ModelId 身份=类改名断老档；RNG 集合存 seed+计数）。评审清单加
  I13 行。提交 9b327c4。
- 续3九补：card-rewards R10（TheLibrary 20 抽 1 去重/rollRarity 复用实证；
  GremlinMatchGame 奖品分支）。提交后推送。
- 续3十补：I14 联机加入契约（JoinFlow.AttemptJoin 只传 unlockState+飞升上限；
  池不传输由两端 mod 集重建）+ M12 RNG 目录补全（RunRngSet 12 条 run 级流全名 +
  PlayerRngType 3 条玩家级流）。提交 c62eb5f。
- **下一卷预研**：RewardsSetSynchronizer/RewardSynchronizer（453+341 行）——每玩家
  rewardsStack + BufferedMessage 乱序缓冲 + RewardSetCompleteState 三态机；概览已扫，
  深度语义（双人同时选择/跳过的合并规则）留作 I15 专卷。
- **队列**：①I15 联机奖励分配专卷 ②StS1 剩余事件抽查（12 个）③StS1 逐怪数据层 v1
  （getMove 内部语义，非本会话）④不变量回填（待下次实机事故）。
- 续3十一补：I15 联机奖励分配（每玩家独立 rewardsStack 三态机、双入口纪律、
  OnSkipped 语义、乱序缓冲）。提交后推送。
- 续3十二补：I2c 容量基线表（vanilla 五角色池 90-91、共享池 65/18/28/4/13/1；
  我方 mod 池对照）+ I0c 起始牌组构成（三回归角色与 StS1 完全一致；
  Regent=FallingStar+Venerate、Necrobinder=Bodyguard+Unleash）。提交后推送。
- 续3十三补：纪元过滤层解码（Ironclad2/5/7Epoch 包示范；BaseLib mod 卡恒可用）。
  I11 细化为三层过滤。提交后推送。
- 续3十四补：R11 事件诅咒来源表（10 事件与已知数据吻合；DrugDealer/NoteForYourself
  两处 ldc 提取待人工复核）。工具 stS1-event-cards.js。提交后推送。
- 续3十五补：R11 定稿（DrugDealer=J.A.X.；NoteForYourself=pref 驱动赠卡默认
  Iron Wave——跨局偏好机制，独特模式）。工具正则修正（允许点号）。提交后推送。
- 续3十六补：loot-rewards L12 遗物掉落池（五池弹头式、三级降级链→Circlet、
  remove(0) 实证）。开放问题 3 结案。提交后推送。
