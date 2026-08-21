# JmcModLib — API survey and dependency verdict

**Verdict: SKIP.** JmcModLib is not a content-modding library. It ships **zero** card, relic, monster, encounter, character, act, card-pool, potion or reward abstraction. It is a *settings-UI / reflection / logging / secrets / persistence / version-compat* utility library plus a multi-version DLL dispatch build toolchain. It does not address a single content gap in this project.

Verified directly against the shipped XML documentation, 2026-08-21. Supersedes the `JmcModLibScout` run, whose substantive verdict was right but whose provenance was wrong (see §5).

## 1. What is actually installed

`G:/steam/steamapps/workshop/content/2868840/3747526103` — real, non-zero, complete:

| file | bytes | note |
|---|---|---|
| `JmcModLib.Runtime.dll` | 516096 | the library |
| `JmcModLib.Runtime.xml` | 206083 | **XML docs — 602 documented members, the authoritative API surface** |
| `JmcModLib.pck` | 998408 | Godot assets (settings UI scenes) |
| `Newtonsoft.Json.dll` | 723368 | bundled JSON dependency |
| `JmcModLib.dll` | 19456 | thin loader |
| `JmcModLib.Dispatch.targets` | 8380 | multi-version dispatch build logic |
| `JmcModLib.Sts2.props` | 587 | MSBuild props for consumers |
| `JmcModLib.runtime.config` | 278 | runtime config |
| `JmcModLib.json` | 274 | mod manifest |
| `BuildTools/`, `dispatch/` | — | empty in the workshop copy; populated in the GitHub source |

Full upstream source is also on disk at `G:/omp works/sts2-spire1/.tmp/jmc/` (repo `JMC-Mods/SlayTheSpire2_JmcModLib`, tarball 13,945,218 bytes), including `docs/legency/JmcModLib_STS2_API.md` and `JmcModLib_STS2_QuickStart.md`.

## 2. Verdict per project gap

Measured by counting documented members whose signature mentions each engine type (`grep -c '<member name="[A-Z]:[^"]*<Type>' JmcModLib.Runtime.xml`):

| gap | members | verdict |
|---|---|---|
| custom monsters | **0** | NO |
| custom encounters | **0** | NO |
| `ActModel` / act sequencing | **0** | NO |
| character (visuals or model) | **0** | NO |
| `CardRarity` odds — `N'loth's Gift` | **0** | NO |
| `CardPool` | **0** | NO |
| `RelicModel` | **0** | NO |
| `PotionModel` | **0** | NO |
| free-play / card-play resource info — `Necronomicon` | **0** | NO |
| combat `Reward` | **0** | NO |
| `RunState` | 1 | NO (incidental) |
| multiplayer | 33 | NO — compat shims only, see §4 |

Not one gap is addressed.

## 3. What the library actually is

Top namespaces by documented member count:

| namespace | members | purpose |
|---|---|---|
| `JmcModLib.Config.UI` | 116 | settings-screen widget framework (buttons, dropdowns, sliders, tickboxes, colour picker, keybind capture, hover tips) |
| `JmcModLib.UI.PauseMenu` | 46 | pause-menu injection |
| `JmcModLib.Reflection.MethodAccessor` | 33 | cached reflection invoker |
| `JmcModLib.Utils.ModLogger` | 23 | logging |
| `JmcModLib.Reflection.MemberAccessor` | 21 | cached field/property accessor |
| `JmcModLib.Reflection.ReflectionAccessorBase` | 20 | reflection base |
| `JmcModLib.Core.ModRegistry` | 18 | mod self-registration |
| `JmcModLib.Utils.ExprHelper` | 17 | expression-tree helpers |
| `JmcModLib.Security.SecretAttribute` | 15 | secret marking |
| `JmcModLib.Security.JmcSecretOptions` | 15 | secret storage options |
| `JmcModLib.Core.RegistryBuilder` | 14 | registry builder |
| `JmcModLib.Compat.ModCompat` | 12 | cross-version compat shims |
| `JmcModLib.Prefabs.*` | 31 | report popup, secret-input popup |
| `JmcModLib.Multiplayer.OptionalNetworkFeature*` | 10 | gating a feature on network idle / restart |
| `JmcModLib.Security.JmcSecretSlot` | 9 | secret slot |
| `JmcModLib.Persistence.JmcRunDataSlot` | 8 | per-run data persistence |

## 4. The multiplayer members are compat shims, not transport

All of `JmcModLib.Compat.MultiplayerCompat` is defensive accessors over engine types whose shape changes between game versions — `TryGetConnectionExtraInfo(NetErrorInfo, out ConnectionFailureExtraInfo)`, `TryGetJoinFlowNetService(JoinFlow, out INetGameService)`, `GetRunLobbyPlayerIds(RunLobby)`, `GetLoadRunLobbyPlayerIds(LoadRunLobby)`, `GetConnectedHostPeerIds(INetHostGameService)`, `TryReadJoinFlowNetService(...)`. Plus `JmcModLib.Multiplayer.OptionalNetworkFeatureAttribute` / `OptionalNetworkFeatureHandle` / `OptionalNetworkFeatureApplyState{Applied, PendingNetworkIdle, RestartRequired}`, which gate a mod feature until the network is idle.

Useful if you are writing multiplayer UI mods across game versions. Irrelevant to M3's dungeon selector, which needs act sequencing and run-setup staging — neither of which exists here.

## 5. Correction to the `JmcModLibScout` run

That agent concluded the library was uninstalled: it found `workshop/downloads/2868840/3747526103` holding zero-filled stubs, no `content/` directory, and `appworkshop_2868840.acf` reporting `"BytesDownloaded" "0"`. It therefore worked from the GitHub tarball and a string-scan of the repo's shipped DLL.

**That provenance was wrong** — the download had merely not finished at the time. `content/2868840/3747526103` exists with real bytes (timestamps 20:57–23:47 on 2026-08-20, i.e. after the scout's read). The XML documentation, the single most authoritative artifact, was never consulted by that run.

Its substantive verdict nevertheless holds, and §2 above re-establishes it from the XML directly.

## 6. Dependency assessment

- Consumers import `JmcModLib.Sts2.props` and `JmcModLib.Dispatch.targets`; `BuildTools/Jmc.Sts2Mod.Build.{props,targets}` (GitHub source) drive a multi-version dispatch build, i.e. one mod compiled against several game versions with a bootstrap picking the right variant — the same problem RitsuLib solves with `lib/<game-version>/`.
- Bundles its own `Newtonsoft.Json.dll`, an assembly-identity collision risk if another mod ships a different version.
- Patches the pause menu and settings screen by Harmony, overlapping BaseLib's own UI patches.

**Recommendation: do not adopt.** BaseLib already covers our content-config needs, and the one area where JmcModLib is genuinely strong — a rich settings-UI widget framework — is not a gap we have.
