# 2026-09-02 — Pandora's Box watcher-pool leak (class-of-bug analysis)

## Symptom (user report, run `6BWKY03DMXNN` / current log)

In a workshop-Watcher chaos run (ChaosBridge/Spire1 bridge mapping, colorless pool,
native starting deck), obtaining Pandora's Box from Darv (floor 18 ancient event)
transformed a batch of deck cards into **normal purple Watcher cards** — vanilla
cards leaking into a chaos run.

## Root cause chain (verified against decompiled sources)

1. Engine `PandorasBox.AfterObtained` transforms only `IsBasicStrikeOrDefend`
   cards. AutoAnthony **replaces** it with `PandorasBoxChaosPatch` (prefix, active
   when `ChaosRunDefinitions.IsRunActive && ActiveReplaceStartingCards`): it
   transforms **every** `IsTransformable` card in the deck.
2. Each transform calls `CardFactory.CreateRandomCardForTransform(card, ...)` →
   `GetDefaultTransformationOptions(original)`:
   ```csharp
   CardPoolModel cardPoolModel = (original.Type != Quest && rarity not in
       {Event, Ancient, Token}) ? original.Pool : ModelDb.CardPool<ColorlessCardPool>();
   ```
   Candidate set = `original.Pool.GetUnlockedCards(...)`.
3. For the five engine characters + chaos cards, `original.Pool` resolves to a
   **ChaosXxxCardPool whose AllCards AA has swapped to generated cards** —
   transforms stay chaos. ✔
4. For our bridged Watcher: native watcher starters (`WATCHER_STRIKE_P`,
   `DEFEND_P`, `ERUPTION_P`, `VIGILANCE`, `MIRACLE` — kept native by design)
   resolve `original.Pool` → `WatcherMod.WatcherCardPool`. AA does not know the
   Watcher and never swaps that pool's contents → candidates are the **83
   vanilla watcher cards** → purple leaks. ✘

Log evidence: post-Pandora plays include 12× WATCHER_STRIKE_P, 5× ERUPTION_P,
4× VIGILANCE, 3× DEFEND_P, 2× MIRACLE alongside CHAOS_COLORLESS_* (deck was a
mix of native starters + chaos cards; the transformed outputs of native cards
were purple watcher cards).

## The class of bug (why it was missed)

The bridge did **pool identity** (Watcher.CardPool getter → ColorlessCardPool)
but not **pool contents** for the watcher's own pool. Any engine path that
enumerates a card's *own* pool by `original.Pool` bypasses our getter redirect,
because the native watcher cards' `Pool` property resolves by ID scan over
`AllCardPools` — it finds WatcherCardPool directly, never our redirected getter.

AA itself has the same shape of coverage: it swaps contents of
ColorlessCardPool + the five Chaos pools. It doesn't need more for engine
characters because every card in an engine chaos deck lives in a swapped pool.
Our native-starter retention introduced cards that live in an **unswapped** pool.

## Fix (implemented in AutoAnthonyCompatBridge, commit pending)

Harmony patches on `WatcherMod.WatcherCardPool` getters (reflection-resolved,
silently skipped if the watcher mod is absent):

- `AllCards` prefix — during chaos runs return the colorless chaos content
  (`ChaosCardRegistry.ColorlessTypes`), appending AA's preserved-original
  colorless cards when `ActivePreserveOriginalCards` (reflection: internal API).
  Mirrors AA's `ColorlessPoolContentsPatch` for ColorlessCardPool.
- `AllCardIds` postfix — union with the chaos card IDs. Without this, native
  watcher cards lose their pool-identity lookup (`CardModel.Pool` scans
  `AllCardIds`) and throw `InvalidProgramException` the first time the engine
  asks a native card for its pool.

Non-chaos runs: both patches pass through (vanilla 83-card pool).

## Same-class audit — other pool-enumeration paths during chaos runs

Every engine path that creates cards from `original.Pool` /
`Owner.Character.CardPool` / `ColorlessCardPool`:

| Path | Source | Status for bridged Watcher |
|---|---|---|
| Pandora's Box transform | `PandorasBoxChaosPatch` → `original.Pool` | FIXED by AllCards swap |
| Astrolabe (transform strikes/defends) | `Astrolabe.cs:25` → `original.Pool` | same path as Pandora — covered by the same fix |
| CardTransformation default | `CardTransformation.cs:71` | same `original.Pool` path — covered |
### RISK-1 (audited, downgraded): Fasten-like `.First(Defend)` searches

`Fasten.cs` does `.First(c => c.Tags.Contains(CardTag.Defend))` on the
character pool; the redirected chaos colorless pool has no Defend-tagged card.
Downgraded: Fasten is a Silent-pool engine card; the chaos colorless pool
contains only `ChaosColorlessCardModel` instances (AA's own audit at
`ChaosModelDbReadyPatch.cs:812` asserts exactly this), so Fasten cannot appear
in a bridged watcher run through pool enumeration. Residual exposure only via
third-party mods granting Fasten — engine-level concern, out of bridge scope.

### RISK-2 (found during audit, unfixed): reward-clamp on small pools

AA's own RewardClampPatch exists for AFTP small pools. For the 73-card colorless
chaos pool, "choose N distinct non-duplicate cards" events are fine. No action.

## Verification

Auto-sweep smoke pending (authorized by user 2026-09-02): watcher run → obtain
Pandora's Box via Darv (seed-dependent) → expect transformed outputs to be
CHAOS_COLORLESS_* only, zero vanilla WATCHER_* outputs, no
InvalidProgramException from AllCardIds, run completes.

Dev evidence anchors:
- `.tmp/autoanthony/AutoAnthony.Patches/PandorasBoxChaosPatch.cs` (TransformAll)
- `.tmp/autoanthony/AutoAnthony.Patches/ColorlessPoolContentsPatch.cs` (contents swap semantics)
- `research/engine-dllsrc/MegaCrit.Sts2.Core.Factories/CardFactory.cs:177+` (GetDefaultTransformationOptions)
- `research/engine-dllsrc/MegaCrit.Sts2.Core.Models/CardPoolModel.cs:44+60` (AllCards / AllCardIds caching)
- `research/engine-dllsrc/MegaCrit.Sts2.Core.Models/CardModel.cs:298+` (Pool ID-scan resolution)
- `.tmp/watchermod/WatcherMod/WatcherCardPool.cs` (83-card pool)
