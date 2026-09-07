# StS2 UnlockState / Epoch System - sts2-spire1 Knowledge Base

## Scope of This Volume
Progression gating in StS2: the UnlockState snapshot, the Epoch unlock-bundle registry, reveal/obtain lifecycle, and CharacterStats linkage. Sources: engine decompile `research/engine-dllsrc/`. **Legend**: **High** = directly readable / **Medium** = inferred (noted).

## 1. UnlockState

**U01 UnlockState shape** - Source `MegaCrit.Sts2.Core.Unlocks/UnlockState.cs`: `UnlockState` (L30), `Characters` (L57, gated per `IsEpochRevealed<X1Epoch>` style checks), `CharacterCardPools` (L111), `Cards` (L117), `CardPools` (L122), `IsEpochRevealed<T>()` (L189). Confidence: **High**
A per-run snapshot of what is unlocked: which characters are visible, which card pools/cards are live. `IsEpochRevealed<T>()` (L189) is the single query gate the pool layer consumes.

**U02 Pool-layer consumption** - every official `*CardPool/*RelicPool/*PotionPool` `GetUnlocked*` override consults UnlockState (see pool-architecture.md I0d for the relic/potion mirror and I2c-note for the card-side `FilterThroughEpochs`). Confidence: **High**

## 2. Epoch Registry

**E01 EpochModel registry** - Source `MegaCrit.Sts2.Core.Timeline/EpochModel.cs`: `EpochModel` (L25), `AgnosticUnlock` record (L33), `AgnosticUnlocks` (L73, 18 score-bar unlocks), static registry of 57 epochs (L180+), `AllEpochs`/`AllEpochIds` (L68-70). Confidence: **High**
Epochs are the unlock bundles (progression milestones); 57 registered, each with per-character and agnostic unlock records.

**E02 Epoch state lifecycle** - Source `MegaCrit.Sts2.Core.Saves/EpochState.cs` (enum at L11: None/NoSlot/Revealed/Obtained...); `ProgressState.ObtainEpochOverride` (`Saves/ProgressState.cs` L355), `SaveManager.ObtainEpochOverride` (L890). Confidence: **High**
State machine: None -> NoSlot -> Revealed -> Obtained. The ObtainEpochOverride entries (ProgressState L355 / SaveManager L890) are the save-side levers AutoSlay's epoch overrides (sts2-autoslay.md S02) pin for reproducible coverage.

## 3. CharacterStats Linkage

**C01 CharacterStats** - Source `MegaCrit.Sts2.Core.Saves/CharacterStats.cs`: `CharacterStats` (L11), `MaxAscension` (L17), `TotalWins`/`TotalLosses` (L23/L26), `Badges` (L41). Confidence: **High**
Per-character run history aggregates. Population path: `UnlockConsoleCmd.cs` L197-200 (GetOrCreateCharacterStats); post-run epoch checks + all-characters victory epoch at `ProgressSaveManager.cs` L342-346.

**C02 Post-run epoch awards** - Source `ProgressSaveManager.cs` L342-346. Confidence: **High**
After a run, the save manager re-evaluates epoch conditions (including the all-characters victory epoch) and awards Revealed/Obtained transitions.

## 4. Arbitration Case Table

| Scenario | Outcome | Basis |
|---|---|---|
| Mod cards gated behind progression | BaseLib CustomCardPoolModel does NOT override FilterThroughEpochs => mod cards always unlocked; to gate them, override GetUnlockedCards in the pool subclass | U02 + pool-architecture I11 |
| Reproducible coverage runs | Pin epochs via ObtainEpochOverride (the AutoSlay lever) rather than editing save files | E02 + autoslay S02 |
| Porting StS1 ascension unlock gates | StS2 has no ascension-gated card pools; CharacterStats.MaxAscension exists (L17) but pool layer does not consult it - any StS1-style gate must be built in mod code | C01/U02 |

## 5. Open Questions / Low-Confidence Items

1. The 57-epoch registry contents (which epoch bundles which cards) not enumerated - read EpochModel.cs L180+ on demand. Confidence: **Medium** (count High, contents unread).
2. Whether modded characters can register custom epochs (no evidence either way; EpochModel registry is static). Confidence: **Medium**.
