# AutoAnthony current read-only review (2026-10-04)

- Request file: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-current-deepseek-review-request-20261004.md`
- User-specified model: `global:deepseek-v4.1-flash`, route `gateway/wb2api`, thinking level `max` (per request file; this report does NOT claim the actually resolved session route was verified - see the 未知 section)
- Role: read-only reviewer; only writable path = this file; no product code change, no build, no test, no deploy, no game launch, no Steam write, no shared mod_configs write, no C: write.
- Review time: 2026-10-04 (Asia/Shanghai)
- Language check: `node G:\omp works\Sts\sts2-spire1\tools\check-agent-text.mjs --file <this file>` -> `agent text accepted` (exit 0). ASCII punctuation only.

## Byte identity (basis of this review)

| Object | Path | Identity | Time |
|---|---|---|---|
| Source | `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs` | 987 lines | 2026-10-03 06:25:14 |
| Source | `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs` | 908 lines | 2026-10-03 06:56:13 |
| Source | `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs` | 317 lines | 2026-10-04 06:27:59 (working tree has 1 uncommitted modification, see 未知 section) |
| Source | `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` | 655 lines | 2026-10-02 13:08:49 |
| Source | `G:\omp works\Sts\sts2-spire1\mod\Spire1.json` | 548 B | 2026-09-30 02:01:48 |
| Release DLL | `G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll` | `E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB`, 900608 B | 2026-10-04 11:18:36 |
| r12 gates | `G:\omp works\.tmp\spire1-release-r12-20261004-central\evidence\release-gates.json` | `passed=true` | 2026-10-04 11:19:11 |
| r12 smoke | `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r12-current-20261004-rerun\run-final.json` | staged DLL = `E31D10...` | 2026-10-04 11:27:37 |

---

## 已确认

### C0-1 [Evidence] Current Release bytes are identical across gates / payload / staging / smoke

- Release DLL measured `SHA256=E31D10C112B4B5C9BBD7ECA1C53DB8F78596C8CA592BF2190548CD2BEF14EDFB`, length=900608, LastWrite=2026-10-04T11:18:36+08:00. Repro: `Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll' -Algorithm SHA256`.
- `G:\omp works\.tmp\spire1-release-r12-20261004-central\evidence\release-manifest.json` (line 11) records payload `Spire1.dll` with the same hash and length; GeneratedAt=11:19:09; SourceCommit=`a6e46e53ae891e4faa7b640a64c1000a9e566c9a`.
- Payload file `G:\omp works\.tmp\spire1-release-r12-20261004-central\payload\mods\Spire1\Spire1.dll` measured with the same hash and length.
- `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r12-current-20261004-rerun\staging-current.json` stagedFiles Spire1.dll = same hash/length; `run-final.json` line 25 `stagedSpire1DllSha256` = same hash.
- Conclusion: current r12 gate and smoke evidence are valid for byte `E31D10...`. PASS (evidence chain, not a product-behavior claim).

### C0-2 [Evidence] Old cross-launch logs are not applicable to the current byte

- `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-cross-launch-post-fix-review-20261003.md` line 12 recorded the then-current Release DLL = `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7`, 781312 B.
- `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\autoanthony-late-load-reviewer-rework2-20261002.md` line 11 recorded the r30 deployed byte = `DD935F68241F0060D1DDED62DE72E0D1B92CA0D1A32F4D656A745E3D58E13F8B`, 776704 B.
- Neither matches the current `E31D10...` / 900608 B: old cross-launch and r30 runtime logs must not be used for current-state claims; historical context only.
- Current smoke `run-final.json` line 33-37 mods = `["BaseLib","Watcher","Spire1"]`: no AutoAnthony, so it only covers the "Spire1 + Watcher, no AutoAnthony" path (see 未知 section).
- Conclusion: old evidence marked not applicable. PASS (applicability determination).

### C0-3 [Evidence] Current Release byte carries the reviewed implementation markers

- String markers extracted from the current Release DLL (ISO-8859-1 byte scan): `AutoAnthonyLoadHook`, `NeedsRetryWithoutAssemblyLoad`, `AutoAnthonyWatcher`, `OfficialAddonPending`, `ThirdPartyPoolContentsPrefix`, `ExecuteDeferredApply` all present.
- Repro: read all bytes, decode as ISO-8859-1, `IndexOf(marker)` for each.
- Boundary: this proves the reviewed symbols are inside the built artifact; it is not a byte-for-byte mapping of source line to IL.

### C1 [PASS] Missing Watcher type stays Pending; no partial settled capability

- File and lines: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyCompatBridge.cs`
  - `:554-559` Watcher assembly absent from AppDomain -> return false; state stays at the default `Pending`.
  - `:561-572` assembly loaded but `WatcherMod.Watcher` or `WatcherMod.WatcherCardPool` type missing -> `ClearThirdPartyTransientState()` + state = `Pending` + return false; no patch installed.
  - `:587-598` 3 getter patches incomplete -> roll back installed ones; only when rollback fails are methods recorded in `ThirdPartyPartialPatchedMethods`; state = `Pending`.
  - `:543-552` stale partial present and rollback fails -> state = `Pending`, remains retryable.
- Trigger: Watcher.dll is inside its registration window (assembly loaded, type not yet visible), or a getter is missing / patch fails.
- Current control flow: `PatchThirdPartyEntriesCore` returns false -> `PatchThirdPartyEntries` returns false -> `Apply` sets `optionalSettled=false` -> `settled=false`; only `:600-606` (all 3 patches installed) writes `ThirdPartyMap` / `ThirdPartyEntryMap` and sets `LegacyBridge`.
- Conclusion: PASS. No map write, no LegacyBridge, no partial settled capability on the missing-type path.
- Minimum fix scope: none.
- Unverified runtime boundary: the real registration-window race was not injected at runtime (static control-flow evidence).

### C2 [PASS] Pending / exception rollback / OfficialAddonPending all have AssemblyLoad + timer/deferred retry sources

- File and lines: `AutoAnthonyCompatBridge.cs`
  - `:285-328` `NeedsRetryWithoutAssemblyLoad`: core incomplete or partial lists present (`:294-301`) -> true; `OfficialAddonPending` (`:308-311`) -> true; official addon loaded and not settled (`:316-319`) -> true; `Pending`/`Unsupported` with Watcher or official addon assembly present (`:324-326`) -> true.
  - `:486-507` exception rollback: after rollback state becomes `OfficialAddonPending` (addon present) or `Pending`; on rollback failure the attempted methods are recorded in `ThirdPartyPartialPatchedMethods`, so the next probe at `:294-301` returns true.
- File and lines: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\AutoAnthonyLoadHook.cs`
  - `:452-469` settled=false -> `HookAssemblyLoad()` (`:457`) always retains the AssemblyLoad source; `NeedsPeriodicRetry()` true -> `EnsureRetryWakeSource()` (`:458-461`), false -> stop only the periodic source (`:468`, non-permanent).
  - `:501-550` AssemblyLoad subscription; `:582-612` OnAssemblyLoad only notifies for names `AutoAnthony` / `Watcher` / `AutoAnthonyWatcher` (`:861-864`).
  - `:643-713` periodic sources: 1s `ThreadingTimer` (`:677-681`); on construction failure an independent background thread (`:689-712`); if both fail, the boundary is explicitly stated at `:705-710` (only AssemblyLoad / external call remain).
  - `:214-271` deferred submission: `Callable.CallDeferred` (`:255`); on submission failure the flag is cleared and `EnsureRetryWakeSourceIfNeeded()` runs (`:257-270`); `:834-859` watchdog resubmits a dropped deferred callback (driven by the periodic source at `:812`).
- Trigger: late Watcher / official addon load, or patch exception rollback, or a cancelled CallDeferred.
- Current control flow: every non-main-thread notification -> `RequestApply` -> `QueueDeferredApply` -> CallDeferred -> `ExecuteDeferredApply` -> `ApplyOnMainThread`; periodic sources only notify, never touch Harmony.
- Conclusion: PASS. When `Pending` and no optional assembly is present, AssemblyLoad is the only source; that is sufficient (state can only change via a new assembly load, and an assembly already loaded before the hook is seen by the probe at `:458`, which then starts the periodic source).
- Minimum fix scope: none.
- Unverified runtime boundary: the extreme path where timer and fallback both fail was not injected at runtime.

### C3 [PASS] initializer / AssemblyLoad / timer / fallback all go through main-thread deferred Apply; no bypass Harmony Apply

- File and lines: `AutoAnthonyLoadHook.cs`
  - `:59-68` initializer entry `TryApplyBridge` -> `RequestApply(harmony,"initializer")`.
  - `:191-205` `RequestApply`: main thread -> `ApplyOnMainThread`; non-main thread -> `QueueDeferredApply` -> `Callable.CallDeferred` (`:255`) -> `ExecuteDeferredApply` (`:273-310`) -> `ApplyOnMainThread`.
  - `:582-612` AssemblyLoad notification -> `RequestApply`; `:793-832` timer/fallback notification -> `RequestApply`; neither touches Harmony directly.
  - `:318-333` `ApplyOnMainThread` main-thread assertion; `:420-425` `ExecuteApplyCore` re-checks; `:430` the only call to `AutoAnthonyCompatBridge.Apply`.
  - In `AutoAnthonyCompatBridge.cs` the only Harmony write operations are `:450` (Unpatch), `:774` / `:924` (Patch), all private and reachable only through the `Apply` path.
- Locks: no nested holding of `ApplyGate` / `RetryGate` / `AssemblyLoadGate` was found; the coalesced path explicitly places `EnsureRetryWakeSourceIfNeeded` outside `ApplyGate` (`:365-370`); `ExecuteApplyCore` releases locks between Unhook / Clear / Hook / Ensure steps.
- Conclusion: PASS. No bypass Harmony Apply and no lock inversion found.
- Minimum fix scope: none.
- Unverified runtime boundary: real multi-thread late-load race was not injected.

### C4 [PASS] Official AutoAnthonyWatcher late-load takeover rolls back legacy first; failure is fail-closed and retryable

- File and lines: `AutoAnthonyCompatBridge.cs`
  - `:515-519` official addon assembly present -> unconditionally `SetOfficialWatcherCapability` (also when legacy is installed).
  - `:609-635` takeover: first rolls back `ThirdPartyPatchedMethods` (`:616`) and `ThirdPartyPartialPatchedMethods` (`:617`); if either fails -> `ClearThirdPartyTransientState()` (maps and pool instance cleared; residual callbacks early-exit via state guards at `:673` / `:696` / `:726`) + state = `OfficialAddonPending` + return false (`:618-627`); method lists kept for later unpatch retry.
  - Rollback success -> clear lists and transient state -> `OfficialAddon` -> return true (`:629-634`); when core is also settled, `Apply` returns true and `AutoAnthonyLoadHook.cs:481-487` permanently stops the periodic source.
  - Retryable: `OfficialAddonPending` returns true at `:308-311` -> periodic source; AssemblyLoad source retained by `AutoAnthonyLoadHook.cs:457`.
- Trigger: official `AutoAnthonyWatcher.dll` loads after the legacy Watcher bridge is installed (`LegacyBridge`).
- Current control flow: next Apply (AssemblyLoad or timer) -> `PatchThirdPartyEntriesCore` sees the addon at the top -> `SetOfficialWatcherCapability` -> rollback / set state.
- Conclusion: PASS (static control flow). Fail-closed and retryability both hold.
- Minimum fix scope: none.
- Unverified runtime boundary: the real "legacy first, official addon very late" race was not injected; the current smoke has no AutoAnthony/AutoAnthonyWatcher.

### C5 [PASS] AutoAnthony / Watcher / AutoAnthonyWatcher are string+reflection only; current r12 gate evidence re-verified

- Source evidence:
  - `AutoAnthonyCompatBridge.cs:1-6` usings = `System.Reflection`, `HarmonyLib`, engine namespaces, own `Spire1...Character`; no AutoAnthony/Watcher using.
  - Type and member access is via strings / reflection: `:104-107` `"Watcher"` / `"WatcherMod.Watcher"` / `"WatcherMod.WatcherCardPool"` / `"WATCHER"`; `:132-140` `AaAssembly` / `AaType` / `GeneratedCharacterType`; `:343-359` `ChaosRunDefinitions`, `ChaosCardRegistry`, member lookups.
  - All HarmonyMethod callback `typeof(...)` calls in this file reference `AutoAnthonyCompatBridge` itself (`:578`, `:581`, `:584`, `:787`, `:790`, `:793`, `:881-896`); other `typeof` calls reference engine or own types (`CharacterModel`, `SerializableRun`, `RunHistory`, `int`, `Ironclad`, `Silent`, `Defect`, `ModelDb`); none reference AutoAnthony/Watcher types.
- Build config evidence: `G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj` lines 22-27 reference only `0Harmony` and `sts2`; lines 32-37 are a comment stating NO compile-time reference; lines 41-50 are PackageReferences (Krafs.Publicizer, Alchyr.Sts2.BaseLib, Alchyr.Sts2.ModAnalyzers, BSchneppe.StS2.PckPacker); no AutoAnthony/Watcher `Reference`.
- Manifest: `G:\omp works\Sts\sts2-spire1\mod\Spire1.json` dependencies = only BaseLib >= 3.4.5.
- Gate evidence: `G:\omp works\.tmp\spire1-release-r12-20261004-central\evidence\release-gates.json` -> passed=true; 3 gates PASS: assemblyref-forbidden (5 checked), manifest-consistency (binary mod AssemblyRef = BaseLib; manifest = BaseLib), typedef-forbidden (6 exact + 1 namespace).
- Independent re-run (read-only, no build, no JSON write, env redirected to G:): `dotnet G:\omp works\Sts\sts2-spire1\tools\build-gates\bin\Release\net9.0\Spire1ReleaseGate.dll --dll <current Release DLL> --config G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json --gates assemblyref,manifest,typedef` -> AssemblyRef 15, TypeDef 1014, all 3 PASS, EXITCODE=0. Gate tool byte: `16926FCC89DAF2943EB294F03620505F62B99DA1028DE80803EDD2B8C906705D`.
- Config: `G:\omp works\Sts\sts2-spire1\tools\build-gates\gate-config.json:7-13` forbids `AutoAnthony`, `AutoAnthonyWatcher`, `Watcher`, `DirectConnectIP`, `ActsFromThePast`; `:2` states gates assert on ECMA-335 metadata tables, not string contains.
- Conclusion: PASS. No hard AssemblyRef to the three optional assemblies; current r12 gates valid for byte `E31D10...`.
- Boundary: `release-gates.json` itself has no `dllSha256`; byte binding relies on `release-manifest.json` hash chain + timestamps (11:19:11 gates > 11:18:36 DLL; 11:19:09 manifest records the hash). The independent re-run above was executed by this review against the current DLL and passed.

### C6 [PASS] Old cross-launch logs are not applicable; current-byte log evidence covers only the absent path

- Applicability: see C0-2 (hashes differ).
- Current-byte runtime log: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-form-smoke-r12-current-20261004-rerun\stdout-final.log` line 317 `[Spire1] AutoAnthony absent - StS1 characters keep their normal card pools.` - current byte runs, bridge detects absence, no patches, normal pools.
- The same run has mods = BaseLib/Watcher/Spire1 (run-final.json line 33-37), so it does NOT cover AutoAnthony present / late-load / official takeover paths.
- Conclusion: PASS for the applicability and absent-path claims; present/late-load paths remain unverified at runtime (see 未知 section).

### C7 [P3 - observation, not blocking] Permanent reflection-resolution failure keeps a futile 1s retry and repeats Error logs

- File and lines: `AutoAnthonyCompatBridge.cs`
  - `:230-234` `Apply`: `ResolveReflection()` false -> log Error + return false; `_reflectionReady` stays false, core group bits stay false.
  - `:332-379` `ResolveReflection`: on failure (missing type/member or thrown exception) returns false and never sets `_reflectionReady`.
  - `:285-301` `NeedsRetryWithoutAssemblyLoad`: with `AaAssembly != null` but `!_fromPatchesApplied` / `!_poolDeckPatchesApplied`, returns true.
  - `AutoAnthonyLoadHook.cs:866-882` `NeedsPeriodicRetry` uses that property; `:438-469` settled=false -> `HookAssemblyLoad()` + `EnsureRetryWakeSource()` -> 1s timer (`:677-681`).
- Trigger: AutoAnthony assembly present and loaded, but `ChaosCardGenerator.GeneratedCharacter` / `ChaosRunDefinitions` / `ChaosCardRegistry` or one required member is missing (incompatible AutoAnthony version).
- Current control flow: every capability AssemblyLoad and every 1s timer tick re-runs Apply -> `ResolveReflection` re-attempts -> fails -> false -> Error log + timer kept alive. Runtime behavior remains fail-closed (bridge disabled, StS1 characters keep normal pools).
- Assessment: not a blocking finding for the requested targets. The requested item 2 states cover Pending / exception rollback / OfficialAddonPending (all confirmed); this is a separate "incompatible assembly" state where the retry cannot succeed because the loaded assembly cannot change. Cost is a 1s timer and a repeated Error log until process exit.
- Minimum fix scope if the team wants to bound it: add a distinct terminal state (for example `Incompatible`) after a failed `ResolveReflection` with `AaAssembly != null`, so the periodic source can stop while AssemblyLoad stays hooked; optional, no functional patch needed for the reviewed contracts.
- Unverified runtime boundary: no run with an incompatible AutoAnthony version.
## 进行中

- None. All six requested review targets (items 1-6) have a confirmed result above.

## 未知

- Runtime evidence for AutoAnthony-present paths (initial Apply, late load, official addon takeover) on byte `E31D10...` does not exist in this round; r12 smoke mods = BaseLib/Watcher/Spire1 only.
- The actually resolved model / provider route for this session (`global:deepseek-v4.1-flash`, `gateway/wb2api`, `max`) was not verified from session metadata; this report records the user-specified values only.
- `MainFile.cs` has one uncommitted working-tree modification (`git status` = M); `release-manifest.json` `SourceCommit=a6e46e5` is the build-time HEAD pointer and does not by itself prove the DLL matches the committed tree byte-for-byte. The C0-3 marker scan confirms the reviewed implementation is inside the built DLL.
- Timer + fallback both failing, ProcessExit concurrent with in-flight Apply, and the real registration-window / late-addon races were not injected at runtime.
- `MainFile.cs` Phase3 sets `_phase3Completed = true` at `:287` even when `TryApplyBridge` returns false (bridge still pending). This is not a bridge-contract violation: `AutoAnthonyLoadHook.TryApplyBridge` installs its own AssemblyLoad/timer sources and is idempotent, so a later load can still settle; noted as unverified interaction only.

## Final verdict

SUPERVISION_PASS