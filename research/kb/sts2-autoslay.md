# StS2 AutoSlay Engine - sts2-spire1 Knowledge Base

## Scope of This Volume
The engine-side AutoSlay automation (namespace `MegaCrit.Sts2.Core.AutoSlay`, decompiled under `research/engine-dllsrc/`): seed-driven run playback, timeouts and the watchdog, room/screen handler architecture, and the mod-side integration patches that extend it for modded content. **Legend**: **High** = directly readable in decompile / **Medium** = inferred (noted).

## 1. Orchestrator

**S01 AutoSlayer lifecycle** - Source `MegaCrit.Sts2.Core.AutoSlay/AutoSlayer.cs`: `Start(seed, logFile)` (L99), `RunAsync` (L147), `PlayRunAsync` (L165), `AbandonRunAsync` (L493), `QuitGame(exitCode)` (L525). Confidence: **High**
Single orchestrator class: Start seeds the run, RunAsync drives it, AbandonRunAsync bails out, QuitGame terminates the process with a code (the harness reads that code).

**S02 Seed determinism** - Source `AutoSlayer.cs`: seed hashed via `StringHelper.GetDeterministicHashCode` (L183), epoch overrides applied (L179-182). Confidence: **High**
Same seed + same content set => same run. The epoch override slots (L179-182) let the harness pin unlock state across machines - the reason our coverage runs are reproducible.

**S03 Run loop shape** - Source `AutoSlayer.cs` `PlayRunAsync` (L165): `TotalFloor < 49` loop condition, per-room `_watchdog.Reset()`. Confidence: **High**
49 floors = 3 acts + TheEnding full clear. The watchdog is reset at every room transition, so a hang must occur INSIDE a room to trip it.

**S04 Screen draining** - Source `AutoSlayer.cs`: `DrainOverlayScreensAsync` (L326) with infinite-loop detection (~L368); `PlayMainMenuAsync` (L452). Confidence: **High**
Overlay screens are drained in a loop with a stuck-detection bail-out - modded screens that never close are detected here (see also our mod-side handler patch, S08).

## 2. Timeouts and Watchdog

**S05 Config constants** - Source `AutoSlay/AutoSlayConfig.cs`: `runTimeout` 25 min (L11), `maxFloor` 49 (L46), `watchdogTimeout` 30 s (L64). Confidence: **High**

**S06 Watchdog mechanics** - Source `AutoSlay.Helpers/Watchdog.cs`: `Watchdog` (L13), `Reset` (L26), `Check` (L33, throws `AutoSlayTimeoutException`), `DumpState` (L61). Confidence: **High**
`Check` throws `AutoSlayTimeoutException` on expiry; `DumpState` emits the diagnostic snapshot the harness correlates with log lines.

## 3. Handler Architecture

**S07 Handler families** - Source `AutoSlay.Handlers/` (IHandler/IRoomHandler/IScreenHandler), `AutoSlay.Handlers.Rooms/` (6 files incl. CombatRoomHandler, ShopRoomHandler), `AutoSlay.Handlers.Screens/` (13 files incl. MapScreenHandler, RewardsScreenHandler); card pick via `AutoSlay.Helpers/AutoSlayCardSelector.cs` (L17, seed-driven). Confidence: **High**
Two parallel families: rooms (behavior per room type) and screens (behavior per overlay screen). New room/screen types need a matching handler or the drain loop stalls (S04 detection).

## 4. Mod-Side Integration (our patches)

**S08 Modded-content integration points** - Source `mod/Spire1Code/`: `Patches/AutoSlayGatePatch.cs` (gate class L20), `Patches/AutoSlayModdedScreenHandlersPatch.cs` (modded screen handlers, L35), `AutoSlay/ShopEnoughGoldGuardPatch.cs`, `Patches/AutoSlayImmortalityPatch.cs` (L51); inject queue via `Patches/DebugCardInjectPatch.cs` (L14-16) and `Patches/DebugRelicInjectPatch.cs` (L17). Confidence: **High** (existence + roles; our own code)
Engine AutoSlay knows nothing about modded content; the mod side bridges: gate patch (run admission), screen-handler registration for modded screens, shop gold guard, immortality for coverage runs, and the card/relic inject queue that drives coverage growth.

## 5. Coverage Statistics - Boundary

**S09 Coverage accounting lives mod-side, not engine-side** - The ENGINE logs rooms/screens/actions (`AutoSlayLog.cs`); the accounting of "which cards/relics got exercised" is OUR inject-queue bookkeeping (DebugCardInjectPatch / DebugRelicInjectPatch). Confidence: **High**
Corollary for G4: engine AutoSlay detects crashes/exceptions/missing assets/stalls (S04-S06); it does NOT evaluate gameplay semantics (see invariants G4 and the Splash/AutoAnthony postmortems - both surfaced only on live user machines).

## 6. Arbitration Case Table

| Scenario | Outcome | Basis |
|---|---|---|
| Coverage run hangs | Watchdog throws after 30 s in-room; 25 min run cap as backstop | S03/S05/S06 |
| Modded screen stalls the run | DrainOverlayScreensAsync stuck-detection fires; our ModdedScreenHandlers patch is the sanctioned fix | S04/S08 |
| Reproducing a coverage result | Pin seed + epoch overrides; content set must be identical (registration order included - pool-architecture I0b+) | S02
| Extending coverage to new mod content | Add an inject-queue entry + verify a handler exists for any new screen; engine needs no changes | S07/S08/S09 |

## 7. Open Questions / Low-Confidence Items

1. Exact room-handler dispatch order when multiple handlers match a room (presumed first-match; not read line-by-line). Confidence: **Medium**.
2. MemoryProfiler.cs thresholds (OOM detection) not itemized. Confidence: **Medium**.
