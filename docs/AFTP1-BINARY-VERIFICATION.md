# AFTP-1 independent binary verification (main session, 2026-09-15)

Subject: `sts2-spire1/mod/Spire1Code/Interop/AftpEffectLifecycleCompat.cs` (commit `dbc9db2`,
unchanged since; build integration `8ac4650`).
Target: the REAL workshop binary
`G:/steam/steamapps/workshop/content/2868840/3746969593/ActsFromThePast.dll`,
sha256 `57362376a48c47209b013dfdacc185b34eec854a12f8549bc4b8174ebf4b40f0`.

Method: `ilspycmd` decompile of that DLL to `.tmp/aftp-decomp-20260915/ActsFromThePast.decompiled.cs`
(1114 types), then structural analysis of every subscription site and every family member.
Reflection is not used for the subscription claims because method bodies are invisible to
reflection; the decompile is the authoritative artifact here. Reflection-only shape checks
remain a separate, still-open item (see "Not verified").

## Claim 1 - `OnTreeEntered` is the family's only ProcessFrame subscription point: HOLDS

Every `ProcessFrame +=` / `ProcessFrame -=` site in the whole assembly (15 total), with its
declaring class and base type:

| line | class | base | in NSts1Effect family |
|---|---|---|---|
| 14901, 14909 | `InteractableTorchEffect` | `Control` | no |
| 15740, 15747, 15753 | `NSts1Effect` | `Node2D` | **yes (the base itself)** |
| 28318, 28323, 28657 | `TheCityBackground` | `NCombatBackground` | no |
| 30723, 30728, 31095 | `TheBeyondBackground` | `NCombatBackground` | no |
| 36296, 36304, 36318, 36544 | `ExordiumBackground` | `NCombatBackground` | no |

No non-family class derives from `NSts1Effect`; the four non-family types are `Control` or
`NCombatBackground` subclasses, each pairing its own `_ExitTree`/`OnTreeExited` unsubscribe.
`ExordiumBackground` additionally guards itself with a one-shot
`TreeEntered -= OnTreeEntered` (line 36316) before subscribing.

## Claim 2 - no subclass can subscribe independently: HOLDS

Family reconstruction (transitive `: NSts1Effect`) yields **47 members** (the base plus 46
subclasses, matching the earlier 46-subclass enumeration).

- Classes in the family that declare their own `OnTreeEntered`: **exactly one**, `NSts1Effect`
  itself at line 15737. No subclass declares or overrides it.
- Classes in the family that override `Initialize` and `Update`: **all 46**, i.e. the extension
  surface subclasses actually use is `Initialize`/`Update`, both of which the compat layer's
  replacement path invokes (via the compiled open-instance `Initialize` delegate and the normal
  `Update` tick).

Therefore a subclass cannot reach `SceneTree.ProcessFrame` on its own: the only route is the
base `OnTreeEntered` body, which is exactly what the compat prefix intercepts. The compat
layer's central design assumption is confirmed on the real binary.

## Claim 3 - the original body's side effects are Initialize + subscribe: HOLDS

`NSts1Effect.OnTreeEntered` (line 15737-15756 region) performs, in order: the one-shot
`TreeEntered -= OnTreeEntered` guard, `GetTree().ProcessFrame += OnProcessFrame`, then
`Initialize()`. The replacement reproduces Initialize-then-subscribe ordering; the ordering
difference is recorded as a review item rather than asserted as equivalent, because the base
body is the authority and the replacement is not byte-identical in sequence.

## Claim 4 - build state: VERIFIED on a real warning stream

`dotnet build mod/Spire1.csproj -c Release --no-incremental` -> **60 warnings / 0 errors**.
Full log at `.tmp/aftp-acceptance/build-full.log`; `grep -c AftpEffectLifecycleCompat` on that
log returns **0**, so the compat file contributes no warnings. (An earlier check of mine ran
against an incremental no-op build whose warning stream was empty; the conclusion was right but
the evidence was not, and is now replaced by this forced rebuild.)

The built DLL `mod/.godot/mono/temp/bin/Release/Spire1.dll` contains the compat type and
`TryApply`. Its sha256 changes with unrelated card work (currently
`20168070c3dc969422ae706a62799a99eff8b009c169b28c23bbbbbcbae977da`); the AFTP-1-era snapshot was
`70001d0784f0f40dfce292a8a1797456f1c149bc224dfe3055535b963d33cec1` at commit `8ac4650`. The
compat source itself is unchanged since `dbc9db2`, so the AFTP-1 conclusions are unaffected by
the later rebuilds.

## Claim 5 - reflection shape contract against the real DLL: HOLDS

Probe `docs/workspace-state-evidence/2026-09-15/fixtures/aftp-shape/` (Program.cs + csproj +
`probe-output.txt`), run against the workshop DLL with GodotSharp 4.5.1 and 0Harmony:
**25/25 PASS, PROBE OK**.

It mirrors `AccessTools.Method`/`AccessTools.Field` (the exact lookups `InstallCapability`
performs, including the base-class search) and asserts every shape the install path validates:

- `NSts1Effect` exists, base `Godot.Node2D`.
- `OnTreeEntered`: resolved by AccessTools, **private**, instance, non-abstract, `void`, 0 args,
  and declared on `NSts1Effect` itself rather than inherited - which is what makes it a valid
  prefix target for the whole family.
- `Initialize`: **protected**, instance, `void`, 0 args, `IsVirtual=True` (so the 46 subclasses
  genuinely override it, and the compiled open-instance delegate dispatches to them).
- `Update`: **protected**, instance, `void`, exactly 1 `float` parameter, `IsAbstract=True`.
- `IsDone`: resolves as a **field** of type `bool`, instance, and no same-named property exists
  (so the `AccessTools.Field` lookup cannot silently change meaning).
- Family: exactly **46** direct subclasses; **none** declares its own `OnTreeEntered`; every one
  overrides `Update(float)`.

## Not verified (deferred to GATE-1)

- **No runtime probe exists for AFTP-1.** `aftp1-verify/` contains decompile and static analysis
  only; every `probe*/` project in `.tmp/` belongs to PERFECT-1. The card's native SceneTree
  create/attach/detach/reenter/free/room-exit stress, and paused/menu/combat fidelity, are
  therefore **UNVERIFIED** - no artifact behind them.
- Exception/reentrancy paths of the prefix (`Initialize` throwing, nested `TreeEntered` during
  `Initialize`, the fallback returning true after a partial bind) are unreviewed by a second
  party: the slice assigned to them was cut off by API quota. The code's own comments assert
  these are safe (latch-before-call, subscription as last side effect); that assertion is
  reasoned, not independently reproduced.
- `Callable` delegate identity for the cached handler's `-=` (whether the unsubscribe actually
  disconnects for delegate-marshaled callables) was being investigated when a slice was cut off;
  unresolved. The decompile shows the original body uses the same `+=`/`-=` pattern with a
  method-group delegate, which is suggestive but not proof of identity semantics.
- The `_fallbackLogged` path is process-wide (a single bool), so after one node falls back,
  subsequent failures log nothing. Bounded-logging intent, but it means a systematic failure
  would appear as exactly one log line.
