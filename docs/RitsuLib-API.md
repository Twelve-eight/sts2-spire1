# RitsuLib API Reference

**Library:** STS2-RitsuLib v0.5.13 (assembly `STS2-RitsuLib`, MIT, repo `github.com/BAKAOLC/STS2-RitsuLib`, author OLC)
**Game target:** Slay the Spire 2 **0.111.0** (Godot 4.5 / C# / .NET 9) — variant shipped in `lib/0.111.0/`
**Public surface:** 1,325 public types across 92 namespaces
**Scope:** standing interface reference for writing code against RitsuLib, for the sts2-spire1 project.

## Source & attribution conventions

Every entry below is attributed with one of:

- `[XML]` — author documentation `lib/0.111.0/STS2-RitsuLib.xml` (authoritative; summaries quoted verbatim, English or Chinese as published)
- `[dump <file>]` — pre-built audit dumps in `.tmp/ritsu/` (`sec-all.txt`, `sec-registry.txt`, `sec-cards.txt`, `sec-visuals.txt`, `sec-content.txt`, `sec-rewards.txt`, `allpatches.txt`, `ns.txt`, `nsindex.md`, `api-0.111.0.json`)
- `[loader.json]` — metadata dump of the loader assembly `STS2-RitsuLib.Loader`
- `[file]` — e.g. `mod_manifest.json`, `ritsulib-variants.manifest`, `compat-target.txt`, nuspec

`R::` in dumps abbreviates `STS2RitsuLib::` (e.g. `R::Content.ModContentRegistry` = `STS2RitsuLib.Content.ModContentRegistry`); `Sts2::` abbreviates `MegaCrit.Sts2::`. Dump signatures preserve member order and omit parameter names; XML does not carry signatures. Where a type exists in the source/XML but is **not public in the shipped binary**, it is marked `⚠ not in shipped binary` (verified against `api-0.111.0.json` visibility).

## Contents

1. Consuming the library
2. Namespace index
3. Content registration
4. Animation and visuals without Spine
5. Cards
6. Assets and runtime refresh
7. Run, lobby and multiplayer
8. UI scaffolding
9. Harmony patch inventory
10. Interop with BaseLib

## 1. Consuming the library

### 1.1 Workshop item layout

Workshop item `3747602295` ("RitsuLib", id `STS2-RitsuLib`, manifest version `0.5.13`) ships a small **loader** assembly plus per-game-version library variants:

```
3747602295/
├─ mod_manifest.json          # id "STS2-RitsuLib", version "0.5.13", min_game_version "0.111.0", has_dll true, has_pck false [file]
├─ STS2-RitsuLib.dll          # LOADER shim, 33 KB — assembly "STS2-RitsuLib.Loader" 1.0.0.0 [loader.json]
├─ STS2-RitsuLib.Loader.pdb
├─ ritsulib-variants.manifest # variant index: compatTarget → lib/<ver>/STS2-RitsuLib.dll + sha256 [file]
├─ viewer/                    # static HTML viewer assets [file]
└─ lib/
   ├─ 0.107.1/  ├─ 0.109.0/  ├─ 0.110.0/  └─ 0.111.0/   # one variant per game version
      each contains: STS2-RitsuLib.dll (8.0 MB), STS2-RitsuLib.pdb, STS2-RitsuLib.xml (5.9 MB), compat-target.txt
```

`compat-target.txt` contains exactly `0.111.0` (7 bytes + newline) and names the game version that variant was built against. Variants exist for 0.107.1, 0.109.0, 0.110.0 and 0.111.0. [file]

### 1.2 The loader and variant selection

The mod root assembly is a **loader shim**; the real library lives in `lib/<version>/`. The game loads the root `STS2-RitsuLib.dll`, whose only public surface is `STS2RitsuLib.Loader.Bootstrap`:

```csharp
namespace STS2RitsuLib.Loader;                      // assembly "STS2-RitsuLib.Loader" 1.0.0.0 [loader.json]
public static class Bootstrap {
    public static void Initialize();                 // [loader.json] row 27 — the entry point
}
```

`Initialize()` performs (private helpers, from `[loader.json]`): resolve the current game version (`Sts2HostVersion`); read `ritsulib-variants.manifest` (`LoadVariantManifest`); pick the variant whose `compatTarget` matches (`PickVariant`); verify the variant's SHA-256 (`MatchesExpectedHash`); load that `lib/<ver>/STS2-RitsuLib.dll`; associate it with the mod (via a patched reflection bridge, `ReflectionHelperModTypesPatch`, plus `AssociateAssemblyWithModMethod`); and invoke the real initializer type in the loaded assembly (`InvokeRealInitializer` → `RitsuLibFramework`). The manifest's `sha256` fields are `904b998…1558` (0.107.1), `4825dd7…90cf` (0.109.0), `704410e…b0e51` (0.110.0), `3e42c74…bcf0b` (0.111.0). [file]

### 1.3 NuGet PackageReference

Consumers reference the signed NuGet package **`STS2.RitsuLib` 0.5.13** (`lib/net9.0/STS2-RitsuLib.dll` + `.xml`; deps `GodotSharp 4.5.1`, `Godot.SourceGenerators 4.5.1`, `System.IO.Hashing 9.0.0`; MIT; repo commit `6a1d7db…`). [file: nuspec]

The package's `buildTransitive/STS2.RitsuLib.targets` copies the DLL, PDB, XML, `mod_manifest.json` and `viewer/` into `$(RitsuLibDeployDir)` after build. Control properties: `RitsuLibAutoCopy` (default `true`), `RitsuLibDeployDir` (set to your mod's output folder; empty disables copying). [file: targets]

### 1.4 Mod-manifest dependency

The game's manifest schema (`MegaCrit.Sts2.Core.Modding.ModManifest`, decompiled source) accepts `dependencies` as a list of `{ id, min_version? }` objects. To depend on RitsuLib, a consumer's `mod_manifest.json` declares:

```json
{ "dependencies": [ { "id": "STS2-RitsuLib", "min_version": "0.5.13" } ] }
```

`id` must match RitsuLib's own manifest id (`STS2-RitsuLib`). [file: mod_manifest.json; dllsrc `ModDependency.cs`]

### 1.5 Required initialization call

After your mod assembly loads (and before the discovery pipeline runs), register your assembly for RitsuLib's one-shot mod-type discovery:

```csharp
STS2RitsuLib.Interop.ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);   // [dump sec-all.txt]
```

```csharp
public static class ModTypeDiscoveryHub {                    // STS2RitsuLib.Interop [dump sec-all.txt]
    public static void RegisterContributor(IModTypeDiscoveryContributor);
    public static void RegisterModAssembly(string, System.Reflection.Assembly);
    public static void LogDiagnostics();
}
```

> XML (ModTypeDiscoveryHub, `[XML]`): "Extensible post-mod-load type-discovery pipeline invoked during early localization initialization. It mirrors BaseLib's scan timing without coupling discovery to one feature."
> XML (`RegisterModAssembly`, `[XML]`): "Associates an assembly with a mod for the one-shot discovery pipeline. Call it from the mod initializer before `ModTypeDiscoveryPatch` runs. On hosts that expose mod-assembly associations, RitsuLib also forwards the association to the game after mod initialization."
> XML (`RegisterContributor`, `[XML]`): "Custom contributors must be registered from the mod initializer before the discovery pipeline runs; `RitsuLibFramework` registers built-ins."

`RitsuLibFramework` is the loaded assembly's integration entry point: "Provides shared runtime bootstrap and public integration entry points for RitsuLib and the mods that use it." `[XML]`

## 2. Namespace index

Counts = public types in the shipped 0.111.0 binary (`[api-0.111.0.json]`). Namespaces not expanded in §3–§8 are covered by this index; expanded namespaces still list their full member set in their sections.

| Public types | Namespace | Purpose |
|---|---|---|
| 139 | `STS2RitsuLib.Scaffolding.Content` | Registration-entry classes for every content kind (each `XxxRegistrationEntry` implements `IContentRegistrationEntry`), asset-profile records, and the model templates (`ModMonsterTemplate`, `ModEncounterTemplate`, `ModActTemplate`, `ModCardTemplate`, …). |
| 127 | `STS2RitsuLib.Settings` | Full mod-settings framework: typed entry definitions, bindings, UI controls and the mod-settings screen. |
| 101 | `STS2RitsuLib` | Framework lifecycle event structs (about 90 `*Event` types + `Const`, `RitsuLibFramework`, `RitsuModInfo`). |
| 89 | `STS2RitsuLib.Models.Capabilities` | Model capability system: `IModelCapability`, capability base classes per model kind, and contributor interfaces (card properties, costs, titles, type text, overlays, …). |
| 88 | `STS2RitsuLib.Combat.SecondaryResources` | Secondary-resource system: registry, card-cost UI, counters, hover tips, multiplayer ticker, star-counter VFX. |
| 76 | `STS2RitsuLib.Interop.AutoRegistration` | `[RegisterXxx]` attributes that auto-register content via the discovery pipeline. |
| 58 | `STS2RitsuLib.Networking.Sidecar` | Out-of-band multiplayer wire protocol: envelopes, opcodes, chunk streams, sync services, session management, typed messages. |
| 58 | `STS2RitsuLib.Audio` | FMOD wrappers: handles, playback options, routing, adaptive music, vanilla bridge. |
| 47 | `STS2RitsuLib.Ui.Shell.Theme` | Shell theme token/metric/color classes. |
| 34 | `STS2RitsuLib.Utils.HarmonyIl` | Harmony IL analysis/editing toolkit (control-flow graph, effect analysis, async IL bridges, match helpers). |
| 32 | `STS2RitsuLib.Scaffolding.Content.Patches` | Patch-side asset-override interfaces (`IModCardAssetOverrides`, …), `ExternalAssetOverrideRegistry`, `RuntimeAssetRefreshCoordinator`, material/icon override registries. |
| 27 | `STS2RitsuLib.CardPiles` | Custom card piles: `ModCardPile`, registry, handlers, flight/pile UI specs. |
| 23 | `STS2RitsuLib.Scaffolding.Characters` | Character scaffolding: `CharacterAssetProfile` + per-asset-set records, `ModCharacterTemplate<TPool…>`, selection-policy interfaces. |
| 19 | `STS2RitsuLib.Saves.RawProgress` | Raw progress commit/read/recovery bridge API (cloud-save interop). |
| 18 | `STS2RitsuLib.Telemetry` | Telemetry adapters (PostHog/HTTP/disabled), envelopes, registry, consent. |
| 16 | `STS2RitsuLib.Content` | `ModContentRegistry` itself plus act-entry resolution, placeholder descriptors, public-entry options, compendium placement. |
| 15 | `STS2RitsuLib.Utils` | General utilities: `WeightedList<T>`, dynamic enums, `I18N`, resource paths, `AttachedState`, text segments. |
| 14 | `STS2RitsuLib.Interop` | Mod interop: `ModTypeDiscoveryHub`, reflection-static channels, JSON DOM transport, interop attributes. |
| 14 | `STS2RitsuLib.Interactions.RightClick` | Right-click interaction registry for models/cards/relics/potions/powers/orbs. |
| 14 | `STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels` | Extra corner amount labels for intents/powers/relics. |
| 14 | `STS2RitsuLib.Combat.HealthBars` | Health-bar forecast segments and visual grafts (plus BaseLib bridges). |
| 12 | `STS2RitsuLib.Diagnostics.DevConsole` | Dev-console autocomplete and owned-id catalog enhancements. |
| 11 | `STS2RitsuLib.RunData` | Run-saved-data store: run-scoped and per-player slots, lobby staging. |
| 10 | `STS2RitsuLib.Ui.Catalog` | `RitsuCatalogBrowser` and catalog item/filter/presentation models. |
| 10 | `STS2RitsuLib.Cards.DynamicVars` | Computed dynamic card vars (`{Computed}`, `{ComputedPower}`, …) and tooltip registry. |
| 8 | `STS2RitsuLib.Utils.Persistence` | Profile-manager persistence: `ProfileManager`, `PersistentDataEntry<T>`, save scopes, lifecycle events. |
| 8 | `STS2RitsuLib.TopBar` | Top-bar buttons: registry, definitions, specs, hover tips. |
| 8 | `STS2RitsuLib.Timeline.Scaffolding` | Epoch/story templates (`ModEpochTemplate`, `ModStoryTemplate`, unlock epoch templates). |
| 8 | `STS2RitsuLib.Search` | Search/fuzzy-match utilities with expansion providers. |
| 8 | `STS2RitsuLib.Scaffolding.Godot.NodeAttachments` | `_Ready`-time child-node attachment registry, options, policies. |
| 7 | `STS2RitsuLib.Utils.Json` | Canonical JSON, JSON patch/merge-patch, pointers. |
| 7 | `STS2RitsuLib.Scaffolding.Visuals.StateMachine` | `ModAnimStateMachine`, `ModAnimState`, builder, `IAnimationBackend` interface. |
| 7 | `STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends` | Animation backends: cue, Spine, AnimationPlayer, AnimatedSprite2D, AnimationTree, composite, form-switching. |
| 7 | `STS2RitsuLib.RuntimeInput` | Runtime hotkeys and Steam input action registry. |
| 7 | `STS2RitsuLib.Patching.Models` | Patch model records: `IModPatches`, `ModPatchInfo`, targets, results. |
| 7 | `STS2RitsuLib.Combat.Rewards` | Mod rewards: registry, custom reward base, serialization, linked reward sets. |
| 6 | `STS2RitsuLib.Updates` | Mod-update checker (manifest/options/result). |
| 6 | `STS2RitsuLib.Ui.Toast` | Toast notifications: service, requests, handles, anchors, presets. |
| 6 | `STS2RitsuLib.Scaffolding.Visuals.Definition` | `VisualCueSet`, `VisualFrameSequence`, `VisualFrame`, `VisualNodeStyle` + builders. |
| 6 | `STS2RitsuLib.Keywords` | Keyword registration and `ModKeywordRegistry`. |
| 5 | `STS2RitsuLib.Utils.Speculation` | Speculative-execution budget/session/diagnostics. |
| 5 | `STS2RitsuLib.Utils.Persistence.Migration` | Data migration: `IMigration`, `MigrationManager`, versioning. |
| 5 | `STS2RitsuLib.Unlocks` | Unlock rules (post-run, counted, elite epochs) and `ModUnlockRegistry`. |
| 5 | `STS2RitsuLib.Timeline` | Timeline/epoch registries: `ModTimelineRegistry`, era icons, layouts, epoch-gated content. |
| 5 | `STS2RitsuLib.Scaffolding.Godot` | Godot node factories (`RitsuGodotNodeFactories`), `RitsuGodotTreeCompat`, packed-scene helper. |
| 5 | `STS2RitsuLib.Scaffolding.Characters.Visuals.Definition` | Merchant/rest-site world scene visuals and `CharacterWorldProceduralVisualSet`. |
| 5 | `STS2RitsuLib.Scaffolding.Cards.HandOutline` | Card hand-outline rules registry and switch rules. |
| 5 | `STS2RitsuLib.Scaffolding.Cards.HandGlow` | Card hand-glow rules registry. |
| 5 | `STS2RitsuLib.Networking.ManagedActions` | `RitsuLibManagedNetActions` + managed-action descriptors/context/base class. |
| 5 | `STS2RitsuLib.Models` | `HookedSingletonModel`, `ModelCloneRegistry`, `ModelLocStringSource`, title extensions. |
| 5 | `STS2RitsuLib.Combat.CardTargeting` | Custom target types and targeting extensions (plus BaseLib target bridge). |
| 5 | `STS2RitsuLib.Cards` | Card on-play hooks, card-type text hooks, contexts. |

| Public types | Namespace | Purpose |
|---|---|---|
| 4 | `STS2RitsuLib.Ui.Shell` | Shell chrome/panel styles, tooltip theme, theme paths. |
| 4 | `STS2RitsuLib.Ui.RichTextEffects` | `ModRichTextEffectRegistry`, tags, parameters. |
| 4 | `STS2RitsuLib.Patching.Core` | `ModPatcher`, `PatchTargetMethodResolver`, `PatchLog`, `ModPatcherExtensions`. |
| 4 | `STS2RitsuLib.Localization.SmartFormat` | SmartFormat extension registry and injection. |
| 4 | `STS2RitsuLib.Compat` | `RitsuModInfo`, `RitsuModManager`, `RitsuModSource`, `RitsuModLoadState`. |
| 4 | `STS2RitsuLib.Combat.PlayerResources` | Player-resource gain/loss hooks (`IPlayerResourceHookListener`, `PlayerResourceKind`). |
| 4 | `STS2RitsuLib.CardTags` | Custom card tags: registry, definitions, serialization entries. |
| 3 | `STS2RitsuLib.Utils.Persistence.Context` | `StorageContext` and typed keys. |
| 3 | `STS2RitsuLib.Ui.Windows` | `RitsuFloatingWindow` + options + geometry. |
| 3 | `STS2RitsuLib.RunRngs` | Mod run-RNG state/snapshots and `ModRunRngRegistry`. |
| 3 | `STS2RitsuLib.Diagnostics.DebugTools` | Debug-tools page registry. |
| 3 | `STS2RitsuLib.Diagnostics.CardExport` | Card PNG export (request/capture mode/exporter). |
| 3 | `STS2RitsuLib.Combat.Healing` | Heal hooks and context. |
| 3 | `STS2RitsuLib.Combat.AttackHits` | Attack-hit hooks and context. |
| 3 | `STS2RitsuLib.Cards.FreePlay` | Free-play detection (see §5). |
| 3 | `STS2RitsuLib.CardPiles.Nodes` | `NModCardPileButton`, `NModExtraHand`, `NModTopBarPileButton` Godot nodes. |
| 2 | `STS2RitsuLib.Utils.Persistence.Interop` | Interop migration adapter, JSON-document interop. |
| 2 | `STS2RitsuLib.Scaffolding.Characters.Visuals` | Cue playback helpers (`ModCreatureVisualPlayback`), world-scene node factory. |
| 2 | `STS2RitsuLib.Scaffolding.Ancients.Options` | Ancient-option registry and rules. |
| 2 | `STS2RitsuLib.Relics.Visibility` | Relic-visibility hook (`IModRelicVisibility`) and registry. |
| 2 | `STS2RitsuLib.Patching.Rules` | `ModPatchRule`, `PatchRuleBuilder`. |
| 2 | `STS2RitsuLib.Models.Identity` | `ModModelIdentity` struct/token. |
| 2 | `STS2RitsuLib.Localization` | `AncientDialogueLocalization`, `I18NLocTableBridge`. |
| 2 | `STS2RitsuLib.Diagnostics.CompendiumExport` | Compendium PNG export. |
| 2 | `STS2RitsuLib.Diagnostics.Commands` | Console commands (`RitsuLibConsoleCmd`, `OpenLogViewerConsoleCmd`). |
| 2 | `STS2RitsuLib.Data` | `ModDataStore`, `ModDataStoreCache<T>`. |
| 2 | `STS2RitsuLib.Combat.Powers` | Temporary-power templates (`ModTemporaryPowerTemplate`, `ModTemporaryAppliedPowerTemplate<TOrigin,TPower>`). |
| 2 | `STS2RitsuLib.Combat.HandSize` | Max-hand-size modifiers and calculator (see §10). |
| 2 | `STS2RitsuLib.CardTags.Serialization` | Card-tag JSON converters. |
| 2 | `STS2RitsuLib.Cards.Transforms` | Card-transform registry and context. |
| 1 | `STS2RitsuLib.Screens` | `ModScreenService` (see §8). |
| 1 | `STS2RitsuLib.Scaffolding.Visuals` | `ModVisualCues` factories (`CueSet()`, `FrameSequence()`). |
| 1 | `STS2RitsuLib.Scaffolding.MonsterMoves` | `ModMonsterMoveStateMachines` (see §4). |
| 1 | `STS2RitsuLib.Scaffolding.Content.Visuals` | `AncientStageProceduralRootFactory`. |
| 1 | `STS2RitsuLib.Scaffolding.Combat` | `CombatTurnPhaseExtensions`. |
| 1 | `STS2RitsuLib.Saves` | `PreservedProgressRecords`. |
| 1 | `STS2RitsuLib.Patching` | `PrivateAccess`. |
| 1 | `STS2RitsuLib.Patching.Builders` | `DynamicPatchBuilder`. |
| 1 | `STS2RitsuLib.Networking.MessageExtensions` | `RitsuNetMessageTailExtensions` (see §7). |
| 1 | `STS2RitsuLib.Data.Models` | `RitsuLibSettings`. |

Counts come from `[api-0.111.0.json]` (public types only, excluding the nested Godot `MethodName`/`PropertyName`/`SignalName` helper classes the type dumps list separately); type lists per namespace cross-check `nsindex.md` `[dump]`.

## 3. Content registration

### 3.1 `STS2RitsuLib.Content.ModContentRegistry`

Per-mod registry for pool models, standalone models, act-scoped content, and stable public-entry overrides used by the patched `ModelDb` identity system. `[XML]` — "Provides a per-mod registry for pool models, standalone models, act-scoped content, and stable public-entry overrides used by the patched `ModelDb` identity system."

Get an instance with `ModContentRegistry.For(modId)` — "Gets the registry for `modId`, creating it on first use." `[XML]`

```csharp
public sealed class ModContentRegistry {                       // [dump sec-registry.txt]
    public string ModId { get; }
    public static bool IsFrozen { get; set; }
    public static ContentRegistrationState State { get; }       // enum Open | Frozen
    public static bool HasAnyActEnterRegistration { get; }
    public static ModContentRegistry For(string modId);

    // ---- pool & standalone model registration ----
    public void RegisterCard<TPool,TCard>();                     public void RegisterCard(Type poolType, Type cardType);
    public void RegisterCard<TPool,TCard>(ModelPublicEntryOptions options);
    public void RegisterCard(Type poolType, Type cardType, ModelPublicEntryOptions options);
    public void RegisterRelic<TPool,TRelic>();                   public void RegisterRelic(Type, Type);
    public void RegisterRelic<TPool,TRelic>(ModelPublicEntryOptions);   public void RegisterRelic(Type, Type, ModelPublicEntryOptions);
    public void RegisterPotion<TPool,TPotion>();                 public void RegisterPotion(Type, Type);
    public void RegisterPotion<TPool,TPotion>(ModelPublicEntryOptions); public void RegisterPotion(Type, Type, ModelPublicEntryOptions);
    public void RegisterCharacter<TCharacter>();                 public void RegisterCharacter(Type);
    public void RegisterPower<TPower>();                         public void RegisterPower(Type);
    public void RegisterOrb<TOrb>();                             public void RegisterOrb(Type);
    public void RegisterEnchantment<TEnchantment>();             public void RegisterEnchantment(Type);
    public void RegisterAffliction<TAffliction>();               public void RegisterAffliction(Type);
    public void RegisterAchievement<TAchievement>();             public void RegisterAchievement(Type);
    public void RegisterSingleton<TSingleton>();                 public void RegisterSingleton(Type);
    public void RegisterBadge<TBadge>();                         public void RegisterBadge(Type);
    public void RegisterGoodModifier<TModifier>();               public void RegisterGoodModifier(Type);
    public void RegisterGoodModifier<TModifier>(int);            public void RegisterGoodModifier(Type, int);
    public void RegisterBadModifier<TModifier>();                public void RegisterBadModifier(Type);
    public void RegisterBadModifier<TModifier>(int);             public void RegisterBadModifier(Type, int);
    public void RegisterMutuallyExclusiveModifierGroup(params Type[]);  public void RegisterMutuallyExclusiveModifierGroup(IReadOnlyList<Type>);

    // ---- shared pools & acts ----
    public void RegisterSharedCardPool<TPool>();                 public void RegisterSharedCardPool(Type);
    public void RegisterSharedRelicPool<TPool>();                public void RegisterSharedRelicPool(Type);
    public void RegisterSharedPotionPool<TPool>();               public void RegisterSharedPotionPool(Type);
    public void RegisterSharedEvent<TEvent>();                   public void RegisterSharedEvent(Type);
    public void RegisterAct<TAct>();                             public void RegisterAct(Type);
    public void RegisterActEncounter<TAct,TEncounter>();         public void RegisterActEncounter(Type, Type);
    public void RegisterGlobalEncounter<TEncounter>();           public void RegisterGlobalEncounter(Type);
    public void RegisterActEvent<TAct,TEvent>();                 public void RegisterActEvent(Type, Type);
    public void RegisterSharedAncient<TAncient>();               public void RegisterSharedAncient(Type);
    public void RegisterActAncient<TAct,TAncient>();             public void RegisterActAncient(Type, Type);
    public void RegisterAncientOption<TAncient>(ModAncientOptionRule);  public void RegisterAncientOption(Type, ModAncientOptionRule);
    public void RegisterMonster<TMonster>();                     public void RegisterMonster(Type);

    // ---- model capabilities ----
    public void RegisterModelCapability<TCapability>();          public void RegisterModelCapability(Type);
    public void RegisterModelCapability<TCapability>(ModelPublicEntryOptions);
    public void RegisterModelCapability(Type, ModelPublicEntryOptions);
    public void ConfigureDefaultModelCapabilities(Type, string, Action<AbstractModel, ModelCapabilityList>, int order);
    public void ConfigureDefaultModelCapabilities<TModel>(string, Action<TModel, ModelCapabilityList>, int order);

    // ---- starter content ----
    public void RegisterCharacterStarterCard<TCharacter,TCard>(int count);           public void RegisterCharacterStarterCard<TCharacter,TCard>(int count, int order);
    public void RegisterCharacterStarterCard(Type, Type, int);                        public void RegisterCharacterStarterCard(Type, Type, int, int);
    public void RegisterCharacterStarterRelic<TCharacter,TRelic>(int);                public void RegisterCharacterStarterRelic<TCharacter,TRelic>(int, int);
    public void RegisterCharacterStarterRelic(Type, Type, int);                       public void RegisterCharacterStarterRelic(Type, Type, int, int);
    public void RegisterCharacterStarterPotion<TCharacter,TPotion>(int);              public void RegisterCharacterStarterPotion<TCharacter,TPotion>(int, int);
    public void RegisterCharacterStarterPotion(Type, Type, int);                      public void RegisterCharacterStarterPotion(Type, Type, int, int);

    // ---- character asset replacement ----
    public void RegisterCharacterAssetReplacement(string characterId, CharacterAssetProfile profile);
    public void RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile profile);
    public bool RemoveCharacterAssetReplacement(string characterId);
    public bool ClearGlobalCharacterAssetReplacement();
    public void RegisterCardPoolAssetReplacement<TPool>(CardPoolAssetProfile);        public void RegisterCardPoolAssetReplacement(string, CardPoolAssetProfile);
    public bool RemoveCardPoolAssetReplacement<TPool>();                              public bool RemoveCardPoolAssetReplacement(string);
    public void RegisterCharacterOwnedRelicVisualOverride<TCharacter,TRelic>(RelicAssetProfile);   public void RegisterCharacterOwnedRelicVisualOverride(string, string, RelicAssetProfile);
    public void RegisterCharacterOwnedPotionVisualOverride<TCharacter,TPotion>(PotionAssetProfile); public void RegisterCharacterOwnedPotionVisualOverride(string, string, PotionAssetProfile);
    public void RegisterCharacterOwnedCardVisualOverride<TCharacter,TCard>(CardAssetProfile);      public void RegisterCharacterOwnedCardVisualOverride(string, string, CardAssetProfile);
    public void RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(string stableId, string iconTexturePath);
    public void RegisterCardLibraryCompendiumSharedPoolFilter<TPool>(string, string, IReadOnlyList<CardLibraryCompendiumPlacementRule>);
    public void RegisterCardLibraryCompendiumSharedPoolFilter(string, string, Type poolType);
    public void RegisterCardLibraryCompendiumSharedPoolFilter(string, string, Type, IReadOnlyList<CardLibraryCompendiumPlacementRule>);

    // ---- hand glow / outline / placeholders / trash heap ----
    public void RegisterCardHandGlow<TCard>(ModCardHandGlowRules);
    public void RegisterCardHandOutline<TCard>(ModCardHandOutlineRules<TCard>);
    public void RegisterCardHandOutline<TCard>(ModCardHandOutlineSwitchRule<TCard>);
    public void RegisterCardHandOutline<TCard>(params ModCardHandOutlineSwitchRule<TCard>[]);
    public void RegisterCardHandOutline<TCard>(Func<TCard, Color?> colorFor, int order, bool showWhenCardPlayable, bool showWhenUnplayable);
    public void RegisterPlaceholderCard<TPool>(string publicEntry, PlaceholderCardDescriptor);
    public void RegisterPlaceholderCard<TPool>(ModelPublicEntryOptions, PlaceholderCardDescriptor);
    public void RegisterPlaceholderRelic<TPool>(string, PlaceholderRelicDescriptor);  public void RegisterPlaceholderRelic<TPool>(ModelPublicEntryOptions, PlaceholderRelicDescriptor);
    public void RegisterPlaceholderPotion<TPool>(string, PlaceholderPotionDescriptor); public void RegisterPlaceholderPotion<TPool>(ModelPublicEntryOptions, PlaceholderPotionDescriptor);
    public void RegisterTrashHeapCard<TCard>();                   public void RegisterTrashHeapCard(Type);
    public void RegisterTrashHeapRelic<TRelic>();                 public void RegisterTrashHeapRelic(Type);

    // ---- id/identity helpers (all static) ----
    public static string NormalizePublicStem(string);
    public static string NormalizeCharacterAssetEntryKey(string);
    public static string NormalizeOwnedModelIdEntry(string);
    public static bool TryGetOwnerModId(Type, out string);
    public static bool TryGetFixedPublicEntry(Type, out string);
    public static string GetFixedPublicEntry(string, Type);
    public static string GetCompoundId(string, string, string);
    public static string GetQualifiedKeywordId(string, string);
    public static string GetQualifiedCardPileId(string, string);
    public static string GetQualifiedCardTagId(string, string);
    public static string GetQualifiedRewardId(string, string);
    public static string GetQualifiedTargetTypeId(string, string);
    public static string GetQualifiedModelCapabilityId(string, string);
    public static string GetQualifiedTopBarButtonId(string, string);
    public static string GetQualifiedRightClickId(string, string);
    public static IReadOnlyList<Type> GetRegisteredModelsInPool(string, Type);
    public static ModContentRegisteredTypeSnapshot[] GetRegisteredTypeSnapshots();

    // nested
    public readonly struct ModContentRegisteredTypeSnapshot { string ModId; Type ModelType; ModelId ModelDbId; string ExpectedPublicEntry; bool HasExplicitPublicEntryOverride; string TypeNamePublicEntry; }
    public static class VanillaCharacterIds { public const string Ironclad, Silent, Defect, Regent, Necrobinder; }  // [XML] "Provides well-known base-game character IDs."
}
```

### 3.2 Act-entry resolution (`RegisterActEnter*`) and `ActEnterResolveContext`

```csharp
public enum ActEnterPoolModeKind { Uniform, Weighted }                                  // [dump sec-registry.txt]
public struct ActEnterResolveContext {                                                  // [dump sec-registry.txt]
    public RunManager RunManager { get; set; }
    public RunState    RunState { get; set; }
    public int         EnteringActIndex { get; set; }
    public Rng         Rng { get; set; }
    public UnlockState UnlockState { get; set; }
    public bool        IsMultiplayer { get; set; }
    public ActEnterResolveContext(RunManager, RunState, int, Rng, UnlockState, bool);
    public void Deconstruct(out RunManager, out RunState, out int, out Rng, out UnlockState, out bool);
}
```
`[XML]` `ActEnterResolveContext`: "Provides run state, random number generation, and unlock state to act-entry resolvers."

```csharp
// ModContentRegistry act-entry methods                                      [dump sec-registry.txt]
public void RegisterActEnterForce<TAct>(int slotIndex, int priority, Func<ActEnterResolveContext, bool> eligible);
public void RegisterActEnterUniformPool(int slotIndex);
public void RegisterActEnterUniformPoolCandidate<TAct>(int slotIndex, Func<ActEnterResolveContext, bool> eligible);
public void RegisterActEnterWeightedPool(int slotIndex);
public void RegisterActEnterWeightedPoolCandidate<TAct>(int slotIndex, Func<ActEnterResolveContext, bool> eligible, Func<ActEnterResolveContext, double> weight);
public void RegisterActEnterWeightedPoolBaseline(int slotIndex, Func<ActEnterResolveContext, double> weight);
```

Author prose (`[XML]`):

- `RegisterActEnterForce<TAct>`: "Registers a rule that replaces `slotIndex` with `TAct` when eligible. Higher priority wins, with earlier registration breaking ties."
- `RegisterActEnterUniformPool`: "Declares a uniform act-entry pool for `slotIndex`. Register this before adding candidates."
- `RegisterActEnterUniformPoolCandidate<TAct>`: "Adds an eligible candidate to the uniform pool for `slotIndex`."
- `RegisterActEnterWeightedPool`: "Declares a weighted act-entry pool for `slotIndex`. Register this before adding candidates or a baseline."
- `RegisterActEnterWeightedPoolCandidate<TAct>`: "Adds an eligible candidate and its weight provider to a weighted act-entry pool."
- `RegisterActEnterWeightedPoolBaseline`: "Registers the weight of the act already occupying `slotIndex`. Weighted pools have no implicit baseline."

### 3.3 Registration semantics (XML prose)

- `RegisterCard<TPool,TCard>` — "Registers `TCard` with `TPool` using the default public entry." `[XML]`
- `RegisterMonster<TMonster>` — "Registers a mod monster model for identity tracking, dynamic injection, and inclusion in the patched `ModelDb.Monsters` list." `[XML]`
- `RegisterAct<TAct>` — "Registers a mod act model for inclusion in `ModelDb.Acts`. This does not add it to the vanilla randomized act list; implement `IModActRandomListPolicy` to opt in." `[XML]`
- `RegisterActEncounter<TAct,TEncounter>` — "Registers an encounter model scoped to `TAct`." `[XML]`
- `RegisterGlobalEncounter<TEncounter>` — "Registers a global encounter appended to every act's `GenerateAllEncounters` result, after vanilla and act-scoped mod encounters. Use `RegisterActEncounter<TAct,TEncounter>` for an encounter belonging to only one act." `[XML]`
- `RegisterCharacter<TCharacter>` — "Registers a mod character model for inclusion in `ModelDb.AllCharacters`." `[XML]`
- `RegisterPower<TPower>` — "Registers a mod power model for inclusion in `ModelDb.AllPowers`." `[XML]`
- `RegisterCharacterAssetReplacement(string, CharacterAssetProfile)` — "Registers asset replacements for a character ID. Non-null fields from later registrations take precedence." `[XML]`
- `RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile)` — "Registers this mod's asset replacements for all characters. Character-specific replacements take precedence." `[XML]`

`ModelPublicEntryOptions` — "Configures the public `ModelDb` entry assigned to a RitsuLib-registered model." Static factories: `FromTypeName`, `FromStem(string)`, `FromFullPublicEntry(string)`. `[dump sec-registry.txt; XML]`

### 3.4 Model templates

```csharp
public abstract class ModMonsterTemplate : MonsterModel, IModCreatureVisualsFactory, IModMonsterCreatureVisualsFactory,
    IModCreatureAnimatorFactory, IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory {   // [dump sec-content.txt]
    public MonsterAssetProfile AssetProfile { get; }
    public string CustomVisualsPath { get; }
    protected virtual NCreatureVisuals TryCreateCreatureVisuals();
    protected virtual CreatureAnimator SetupCustomCreatureAnimator(MegaSprite sprite);
    protected virtual ModAnimStateMachine SetupCustomCombatAnimationStateMachine(Godot.Node node, MonsterModel monster);
    protected virtual ModAnimStateMachine SetupCustomNonSpineAnimationStateMachine(Godot.Node node, MonsterModel monster);
    protected ModMonsterTemplate();
}
```
`[XML]` `ModMonsterTemplate`: "Provides a base `MonsterModel` for mods with a creature-visuals scene replacement, optional runtime visual creation, and optional custom animation systems." `MonsterAssetProfile` carries one slot: `string VisualsScenePath`. `[dump sec-content.txt]`

```csharp
public abstract class ModEncounterTemplate : EncounterModel, IModEncounterAssetOverrides,
    IModEncounterCombatSceneFactory, IModEncounterActValidity {                     // [dump sec-content.txt]
    public bool HasScene { get; }
    protected bool UseActCombatBackground { get; }
    protected bool UseProgrammaticCombatBackground { get; }
    protected bool HasCustomBackground { get; }
    protected bool SuppliesEncounterCombatSceneFromFactory { get; }
    public EncounterAssetProfile AssetProfile { get; }
    public string CustomEncounterScenePath { get; }
    public string CustomBackgroundScenePath { get; }
    public string CustomBackgroundLayersDirectoryPath { get; }
    public string CustomBossNodePath { get; }
    public IEnumerable<string> CustomExtraAssetPaths { get; }
    public IEnumerable<string> CustomMapNodeAssetPaths { get; }
    public string CustomRunHistoryIconPath { get; }
    public string CustomRunHistoryIconOutlinePath { get; }
    public virtual bool IsValidForAct(ActModel act);
    protected virtual Control TryCreateEncounterCombatScene();
    protected virtual BackgroundAssets BuildProgrammaticCombatBackground(ActModel, Rng);
    protected ModEncounterTemplate();
}
```
`[XML]` `ModEncounterTemplate`: "Provides a base `EncounterModel` for mods with asset overrides, optional runtime combat scene creation, act-specific eligibility, and a choice of act, encounter-specific, or programmatically created combat backgrounds. … Register it for one act through `RegisterActEncounter`, or for every act through `RegisterGlobalEncounter`. Each `MonsterModel` used by the encounter must also be registered."

```csharp
public abstract class ModActTemplate : ActModel, IModActAssetOverrides, IModActRandomListPolicy {   // [dump sec-content.txt]
    public string ChestSpineResourcePath { get; }
    public ActAssetProfile AssetProfile { get; }
    public string CustomBackgroundScenePath { get; }
    public string CustomRestSiteBackgroundPath { get; }
    public string CustomMapTopBgPath { get; }
    public string CustomMapMidBgPath { get; }
    public string CustomMapBotBgPath { get; }
    public string CustomChestSpineResourcePath { get; }
    public string CustomBackgroundLayersDirectoryPath { get; }
    public bool AllowInRandomActList { get; }
    protected ModActTemplate();
}
```
`[XML]` `ModActTemplate`: "Provides a base `ActModel` for mods with chest Spine replacement, scene and map asset overrides, and an optional combat-background layer directory containing `_bg_` and `_fg_` scenes. To reuse an existing act's assets, return a profile created by `ContentAssetProfiles.FromVanillaActId(string)` from `AssetProfile`, using the base-game asset folder name rather than this model's ID."

```csharp
public abstract class ModCharacterTemplate<TCardPool,TRelicPool,TPotionPool> : CharacterModel,
    IModCharacterAssetOverrides, IModCreatureVisualsFactory, IModCharacterCreatureVisualsFactory,
    IModCreatureAnimatorFactory, IModCharacterCreatureAnimatorFactory,
    IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory,
    IModCharacterMerchantAnimationStateMachineFactory, IModCharacterRestSiteAnimationStateMachineFactory,
    IModCharacterEpochTimelineRequirement, … {                                        // [dump sec-visuals.txt]
    public CardPoolModel CardPool { get; }
    public RelicPoolModel RelicPool { get; }
    public PotionPoolModel PotionPool { get; }
    public IEnumerable<CardModel> StartingDeck { get; }
    public IReadOnlyList<RelicModel> StartingRelics { get; }
    public IReadOnlyList<PotionModel> StartingPotions { get; }
    public string CharacterSelectSfx { get; }  public string CharacterTransitionSfx { get; }
    public string PlaceholderCharacterId { get; }
    public CharacterAssetProfile AssetProfile { get; }          // + Custom* path properties per asset set
    public VisualCueSet VisualCues { get; }
    public CharacterWorldProceduralVisualSet WorldProceduralVisuals { get; }
    protected CharacterModel UnlocksAfterRunAs { get; }         protected Type UnlocksAfterRunAsType { get; }
    protected IEnumerable<StartingDeckEntry> StartingDeckEntries { get; }
    protected IEnumerable<Type> StartingDeckTypes { get; }  protected IEnumerable<Type> StartingRelicTypes { get; }  protected IEnumerable<Type> StartingPotionTypes { get; }
    protected CharacterAssetProfile ResolvedAssetProfile { get; }
    protected static IEnumerable<TModel> ResolveModels<TModel>(IEnumerable<Type>);
    protected ModCharacterTemplate();
}
```
`[XML]` `ModCharacterTemplate<…>`: "Base class for mod characters with typed content pools, extensible starting content, and asset overrides." `StartingDeckEntry` = `{ Type CardType; int Count; }` with `static StartingDeckEntry Of<TCard>(int count)`. `[dump sec-visuals.txt]`

### 3.5 Policy interfaces and `CharacterAssetProfile`

```csharp
public interface IModEncounterActValidity {                                          // [dump sec-content.txt]
    bool IsValidForAct(Sts2::Core.Models.ActModel act);
}
```
`[XML]` — "Optionally determines whether a mod `EncounterModel` can enter the encounter pool for a particular `ActModel` during room generation."

```csharp
public interface IModActRandomListPolicy { bool AllowInRandomActList { get; } }      // [dump sec-content.txt]
```
`[XML]` — "Controls whether a registered mod act can appear in vanilla act-list randomization."

```csharp
public interface IModCharacterVanillaSelectionPolicy {                              // [dump sec-visuals.txt]
    bool HideFromVanillaCharacterSelect { get; }
    bool AllowInVanillaRandomCharacterSelect { get; }
    bool HideInCardLibraryCompendium { get; }
}
```
`[XML]` — "Controls a mod character's participation in base-game character selection and the Card Library."

Related one-property policies (`[dump sec-content.txt]`): `IModOrbRandomPoolPolicy { bool AllowInRandomOrbPool }`; `IModCharacterEpochTimelineRequirement { bool RequiresEpochAndTimeline }`; `IModCharacterUnlockPrerequisite { Type UnlocksAfterRunAsType }`; `IModCharacterCardLibraryCompendiumPlacement { IReadOnlyList<CardLibraryCompendiumPlacementRule> CardLibraryCompendiumPlacementRules }`.

#### `CharacterAssetProfile` — full slot list

`[dump sec-visuals.txt]` — `[XML]`: "Groups optional assets and visual overrides for a mod character."

```csharp
public sealed class CharacterAssetProfile {            // slots (all get/set):
    public CharacterSceneAssetSet      Scenes;      // VisualsPath, EnergyCounterPath, MerchantAnimPath, RestSiteAnimPath
    public CharacterUiAssetSet         Ui;          // IconTexturePath, IconOutlineTexturePath, IconPath, CharacterSelectBgPath,
                                                    //   CharacterSelectIconPath, CharacterSelectLockedIconPath,
                                                    //   CharacterSelectTransitionPath, MapMarkerPath
    public CharacterVfxAssetSet        Vfx;         // TrailPath, TrailStyle (CharacterTrailStyle)
    public CharacterSpineAssetSet      Spine;       // CombatSkeletonDataPath
    public CharacterAudioAssetSet      Audio;       // CharacterSelectSfx, CharacterTransitionSfx, AttackSfx, CastSfx, DeathSfx
    public CharacterMultiplayerAssetSet Multiplayer;// ArmPointingTexturePath, ArmRockTexturePath, ArmPaperTexturePath, ArmScissorsTexturePath
    public VisualCueSet                VisualCues;
    public CharacterWorldProceduralVisualSet WorldProceduralVisuals;   // Merchant {CueSet}, RestSite {CueSet}
    public CharacterVanillaRelicVisualOverride[] VanillaRelicVisualOverrides;   // { string RelicModelIdEntry; RelicAssetProfile Assets }
    public CharacterVanillaPotionVisualOverride[] VanillaPotionVisualOverrides; // { string PotionModelIdEntry; PotionAssetProfile Assets }
    public CharacterVanillaCardVisualOverride[]  VanillaCardVisualOverrides;    // { string CardModelIdEntry;  CardAssetProfile Assets }
    public static CharacterAssetProfile Empty { get; }
}
```

`CharacterTrailStyle` slots (all `Color?`/`float?`/`Vector2?`): `OuterTrailModulate`, `OuterTrailWidth`, `InnerTrailModulate`, `InnerTrailWidth`, `BigSparksColor`, `LittleSparksColor`, `PrimarySpriteModulate`, `PrimarySpriteScale`, `SecondarySpriteModulate`, `SecondarySpriteScale`. `[dump sec-visuals.txt]`

Fluent helpers in `CharacterAssetProfiles` (`[dump sec-visuals.txt]`): `FromCharacterId(string)`, `Resolve(profile, characterId)`, `Merge(a, b)`, `FillMissingFrom(a, b)`, `WithPlaceholder`, `WithScenes`, `WithUi`, `WithVfx`, `WithSpine`, `WithAudio`, `WithMultiplayer`, `WithVisualCues`, `WithWorldProceduralVisuals`, `WithVanillaRelicVisualOverrides`, `WithVanillaPotionVisualOverrides`, `WithVanillaCardVisualOverrides`, plus per-vanilla-character factories `Ironclad()`, `Silent()`, `Defect()`, `Regent()`, `Necrobinder()`. `CharacterAssetPathHelper` provides default path helpers (`GetVisualsPath`, `GetEnergyCounterPath`, `GetEnergyIconPath`, `GetCharacterSelectBackgroundPath`, `GetCharacterSelectIconPath`, `GetCharacterSelectLockedIconPath`, `GetMapMarkerPath`, `GetTrailPath`, `EnumerateDefaultCharacterAssets`). `[dump sec-visuals.txt]`

## 4. Animation and visuals without Spine

The non-Spine model is **texture path + duration**: a cue names an animation; the cue set maps it to either a static texture path, a `VisualFrameSequence` (list of `{ TexturePath, DurationSeconds }` frames), or both; `CueAnimationBackend` plays these on a plain `Sprite2D`. No `SpriteFrames` resources and no `.tscn` scenes are required — only `res://`-style texture paths. `[dump sec-visuals.txt; XML]`

### 4.1 Definitions

```csharp
public sealed class VisualCueSet {                                 // [dump sec-visuals.txt]
    public IReadOnlyDictionary<string, string> TexturePathByCue { get; set; }
    public IReadOnlyDictionary<string, VisualFrameSequence> FrameSequenceByCue { get; set; }
    public IReadOnlyDictionary<string, VisualNodeStyle> TextureStyleByCue { get; set; }
    public VisualCueSet(IReadOnlyDictionary<string,string>, IReadOnlyDictionary<string,VisualFrameSequence>);
    public VisualCueSet(IReadOnlyDictionary<string,string>, IReadOnlyDictionary<string,VisualFrameSequence>, IReadOnlyDictionary<string,VisualNodeStyle>);
}
```
`[XML]` — "Defines immutable visuals for named cues, with one static texture, one `VisualFrameSequence`, or both for each cue. Cue sets support combat, game-over screens, merchant and rest-site characters, Ancient foreground layers, and similar contexts."

```csharp
public struct VisualFrame { public string TexturePath; public float DurationSeconds; }   // [dump sec-visuals.txt]
public sealed class VisualFrameSequence {                          // [dump sec-visuals.txt]
    public IReadOnlyList<VisualFrame> Frames { get; set; }
    public bool Loop { get; set; }
    public VisualNodeStyle DefaultStyle { get; set; }
    public IReadOnlyList<VisualNodeStyle> FrameStyles { get; set; }
    public VisualFrameSequence(IReadOnlyList<VisualFrame>, bool loop);
    public VisualFrameSequence(IReadOnlyList<VisualFrame>, bool, VisualNodeStyle, IReadOnlyList<VisualNodeStyle>);
}
```
`[XML]` `VisualFrameSequence` — "Defines an immutable ordered frame sequence for one logical cue, such as combat, a merchant room, or an Ancient event stage."

```csharp
public sealed class VisualNodeStyle {                              // [dump sec-visuals.txt]
    public static VisualNodeStyle Empty { get; }
    public Vector2? Position, Offset, Scale, PivotOffset;  public float? RotationRadians, Skew;
    public Color? Modulate, SelfModulate;  public int? ZIndex;  public bool? Visible, Centered, FlipH, FlipV;
    // + With* fluent variants (WithPosition, WithOffset, WithScale, WithRotationDegrees, WithRotationRadians,
    //   WithSkew, WithPivotOffset, WithModulate, WithSelfModulate, WithZIndex, WithVisible, WithCentered,
    //   WithFlip, Hidden())
}

public static class ModVisualCues {                                // [dump sec-visuals.txt]
    public static VisualCueSetBuilder CueSet();
    public static VisualFrameSequenceBuilder FrameSequence();
}
public sealed class VisualCueSetBuilder {                          // [dump sec-visuals.txt]
    public static VisualCueSetBuilder Create();
    public VisualCueSetBuilder Single(string cue, string texturePath);
    public VisualCueSetBuilder Single(string cue, string texturePath, VisualNodeStyle style);
    public VisualCueSetBuilder Single(string cue, string texturePath, float durationSeconds);
    public VisualCueSetBuilder Single(string cue, string texturePath, float durationSeconds, VisualNodeStyle style);
    public VisualCueSetBuilder Sequence(string cue, VisualFrameSequence sequence);
    public VisualCueSetBuilder Sequence(string cue, Action<VisualFrameSequenceBuilder> configure);
    public VisualCueSet Build();
}
public sealed class VisualFrameSequenceBuilder {                   // [dump sec-visuals.txt]
    public static VisualFrameSequenceBuilder Create();
    public VisualFrameSequenceBuilder Frame(string texturePath, float durationSeconds);
    public VisualFrameSequenceBuilder Frame(string texturePath, float durationSeconds, VisualNodeStyle style);
    public VisualFrameSequenceBuilder DefaultStyle(VisualNodeStyle);  public VisualFrameSequenceBuilder Loop(bool);
    public VisualFrameSequence Build();
}
```
`[XML]` `VisualCueSetBuilder.Single(…, float)` — "Binds one texture to a non-looping timed cue. The cue completes after its effective `durationSeconds`, allowing state machines to advance." `Sequence(string, VisualFrameSequence)` — "Binds a completed frame sequence to a cue, replacing any static texture registered for the same key."

### 4.2 `CueAnimationBackend`

```csharp
public sealed class CueAnimationBackend : IAnimationBackend, IAnimationTimingProvider {   // [dump sec-visuals.txt]
    public event … Started, Completed, Interrupted;                // IAnimationBackend events
    public Node OwnerNode { get; }
    public CueAnimationBackend(Godot.Node ownerNode, Godot.Sprite2D sprite, VisualCueSet cueSet);
    public bool HasAnimation(string cue);   public void Play(string cue, bool loop);   public void Queue(string cue, bool loop);
    public void Stop();   public void Dispose();
    public bool TryGetAnimationDuration(string, out float);   public bool TryGetCurrentAnimationRemaining(out float);
}
```
`[XML]` — "Drives cue-based visuals from static textures and `VisualFrameSequence` data." Other backends: `SpineAnimationBackend(MegaSprite)`, `GodotAnimationPlayerBackend(AnimationPlayer)`, `AnimatedSprite2DBackend(AnimatedSprite2D)`, `AnimationTreeStateMachineBackend(AnimationTree)`, `CompositeAnimationBackend(IReadOnlyList<IAnimationBackend>, Node)`, `FormSwitchingAnimationBackend(IReadOnlyDictionary<string,IAnimationBackend>, string activeFormId, Node)`. `[dump sec-visuals.txt]`

### 4.3 `ModAnimStateMachine` and builder

```csharp
public sealed class ModAnimState {                                 // [dump sec-visuals.txt]
    public string Id { get; }   public bool IsLooping { get; }   public ModAnimState NextState { get; set; }
    public string BoundsContainer { get; set; }   public bool HasLooped { get; set; }
    public ModAnimState(string id, bool isLooping);
    public void AddBranch(string trigger, ModAnimState target, Func<bool> condition);
    public bool HasTrigger(string);   public void MarkHasLooped();
}
public sealed class ModAnimStateMachine {                          // [dump sec-visuals.txt]
    public event … BoundsUpdated, AnimationStarted, AnimationCompleted, AnimationInterrupted;
    public ModAnimState Current { get; set; }   public IAnimationBackend Backend { get; }
    public ModAnimStateMachine(IAnimationBackend backend);
    public void AddAnyState(string trigger, ModAnimState target, Func<bool> condition);
    public void Start(ModAnimState state);   public bool HasTrigger(string);
    public bool TryGetCurrentAnimationDuration(out float);  public bool TryGetCurrentAnimationRemaining(out float);
    public void SetTrigger(string trigger);   public void Dispose();
}
public sealed class ModAnimStateMachineBuilder {                   // [dump sec-visuals.txt]
    public static ModAnimStateMachineBuilder Create();
    public StateScope AddState(string id, bool isLooping);         // StateScope: WithNext/WithBounds/AsInitial/Done
    public ModAnimStateMachineBuilder AddBranch(string fromId, string toId, string trigger, Func<bool> condition);
    public ModAnimStateMachineBuilder AddAnyState(string trigger, string stateId, Func<bool> condition);
    public ModAnimStateMachine Build(IAnimationBackend backend);
    public ModAnimStateMachine BuildSpine(MegaSprite sprite);
    public ModAnimStateMachine BuildForVisualsRoot(Godot.Node visualsRoot, CharacterModel character, VisualCueSet cueSet);
}
```
`[XML]` `ModAnimStateMachine` — "Drives `ModAnimState` transitions through any `IAnimationBackend`." `SetTrigger` — "Evaluates `trigger` against any-state, then the current state, and enters the first matching target." `Build` — "Materializes the graph against `backend` and starts the resulting state machine." `BuildForVisualsRoot` — "Discovers cue, Spine, Godot AnimationPlayer, and AnimatedSprite2D backends under `visualsRoot`, then builds the state machine."

`ModAnimStateMachines` (`[dump sec-visuals.txt]`, `[XML]` "convenience factories for the standard creature-animation state graph… state graph shape corresponds to baselib's `CustomCharacterModel.SetupAnimationState`"): `Standard(MegaSprite, string idle, bool idleLoop, string attack, …, string death, bool deathLoop)` returns `CreatureAnimator`; `StandardCue(Node, CharacterModel, …same 8 state args…, VisualCueSet)` / `StandardMerchantCue` / `StandardRestSiteCue` return `ModAnimStateMachine`. (Exact parameter list per dump: 12 `string`/`bool` pairs + loop flags — see `sec-visuals.txt`.)

### 4.4 Creature factory interfaces

```csharp
public interface IModCreatureCombatAnimationStateMachineFactory {                    // [dump sec-content.txt]
    ModAnimStateMachine TryCreateCombatAnimationStateMachine(Godot.Node node);
}
public interface IModNonSpineAnimationStateMachineFactory {                         // [dump sec-content.txt]
    ModAnimStateMachine TryCreateNonSpineAnimationStateMachine(Godot.Node node);
}
```
`[XML]` `IModCreatureCombatAnimationStateMachineFactory` — "Defines a runtime combat `ModAnimStateMachine` factory for creature models whose `NCreature.SetAnimationTrigger(string)` calls should be handled by `ModAnimStateMachine.SetTrigger(string)`. It supports Spine and non-Spine animation backends." (Companions: `IModCreatureVisualsFactory.TryCreateCreatureVisuals()`, `IModCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite)`, `IModCharacterCreatureVisualsFactory`, `IModCharacterCreatureAnimatorFactory`, `IModCharacterMerchantAnimationStateMachineFactory.TryCreateMerchantAnimationStateMachine(Node, CharacterModel)`, `IModCharacterRestSiteAnimationStateMachineFactory.TryCreateRestSiteAnimationStateMachine(Node, CharacterModel)` — all `[dump sec-content.txt]`.)

### 4.5 `ModMonsterMoveStateMachines`

```csharp
public static class ModMonsterMoveStateMachines {                                   // [dump sec-rewards.txt]
    public static MonsterMoveStateMachine.MonsterMoveStateMachine SingleMoveLoop(MoveState move);
    public static MonsterMoveStateMachine.MonsterMoveStateMachine Cycle(params MoveState[] states);
    public static MonsterMoveStateMachine.MonsterMoveStateMachine Cycle(IReadOnlyList<MoveState> states);
    public static MonsterMoveStateMachine.MonsterMoveStateMachine HeadThenRepeatTail(MoveState head, MoveState tail);
    public static MonsterMoveStateMachine.MonsterMoveStateMachine RandomEntry(string name, Action<RandomBranchState> configure, IReadOnlyList<MonsterState> states);
    public static MonsterMoveStateMachine.MonsterMoveStateMachine ConditionalEntry(string name, Action<ConditionalBranchState> configure, IReadOnlyList<MonsterState> states);
}
```
`[XML]` — "Provides common `MonsterMoveStateMachine.MonsterMoveStateMachine` construction patterns for mod monsters, keeping `MonsterModel.GenerateMoveStateMachine` implementations concise." Types are the game's own `MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine.*` (`MoveState`, `RandomBranchState`, `ConditionalBranchState`, `MonsterState`).

## 5. Cards

### 5.1 `STS2RitsuLib.Cards.FreePlay` (full namespace, 3 public types)

```csharp
public static class FreePlayBindingRegistry {                                       // [dump sec-rewards.txt]
    public static void Register(string bindingId, Func<CardPlay, bool> detector);
    public static void MarkCardFreeNextPlay(CardModel card);
    public static void MarkCardFreeThisTurn(CardModel card);
    public static void MarkCardFreeThisCombat(CardModel card);
    public static void MarkCurrentPlayFree(CardPlay play);
    public static FreePlayResolution Resolve(CardPlay play);
    public static bool IsFreeForPlay(CardPlay play);
    public static bool IsCardFreeForUpcomingPlay(CardModel card);
    public static bool ClearCardFreeThisTurn(CardModel card);
    public static bool ClearCardFreeAfterPlayed(CardModel card);
}
public static class CardModelFreePlayExtensions {                                   // [dump sec-rewards.txt]
    public static void SetToFreeForRestOfTurn(this CardModel card);
}
public sealed class FreePlayResolution {                                            // [dump sec-rewards.txt]
    public bool IsAutoPlayNoSpend { get; set; }
    public bool IsCardBindingFree { get; set; }
    public bool IsRegisteredDetectorFree { get; set; }
    public bool IsFree { get; }        // == any of the three
    public FreePlayResolution(bool isAutoPlayNoSpend, bool isCardBindingFree, bool isRegisteredDetectorFree);
}
```
`[XML]` `FreePlayBindingRegistry` — "Provides an extensible registry for determining whether a card play is free." `FreePlayResolution` — "Describes which detection sources marked a card play as free."

Method prose (`[XML]`): `Register` — "Registers an additional free-play detector. The detector should return `true` when mod-defined rules consider the specified `CardPlay` free." `MarkCardFreeNextPlay` — "Marks the card's base costs as free for its next play." `MarkCardFreeThisTurn` — "Marks the card's base costs as free until the end of the turn or its next play." `MarkCardFreeThisCombat` — "Marks the card's base costs as free for the current combat." `MarkCurrentPlayFree` — "Immediately marks the current `CardPlay` as free." `Resolve` — "Resolves the free-play sources for this `CardPlay`." `IsFreeForPlay` — "Returns whether any source marks the play as free." `IsCardFreeForUpcomingPlay` — "Returns whether the card is marked free before a `CardPlay` exists, without consuming a next-play charge." `ClearCardFreeThisTurn` — "Clears current-turn free-play charges that were not consumed by playing the card." `ClearCardFreeAfterPlayed` — "Clears free-play bindings that expire after the card is played." `SetToFreeForRestOfTurn` — "Makes the card's fixed base costs free for the rest of the current turn, including every subsequent play of that card during the turn."

### 5.2 `Cards.ICardOnPlayHookListener` and contexts

```csharp
public interface ICardOnPlayHookListener {                                          // [dump sec-cards.txt]
    Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context);
    Task        AfterCardOnPlay(AfterCardOnPlayContext context);
}
public struct BeforeCardOnPlayContext {                                             // [dump sec-cards.txt]
    public ICombatState CombatState { get; set; }
    public PlayerChoiceContext ChoiceContext { get; set; }
    public CardPlay CardPlay { get; set; }
    public BeforeCardOnPlayContext(ICombatState, PlayerChoiceContext, CardPlay);
}
public struct AfterCardOnPlayContext {                                              // [dump sec-cards.txt]
    public ICombatState CombatState { get; set; }
    public PlayerChoiceContext ChoiceContext { get; set; }
    public CardPlay CardPlay { get; set; }
    public bool OriginalOnPlayRan { get; set; }
    public AfterCardOnPlayContext(ICombatState, PlayerChoiceContext, CardPlay, bool);
}
public static class CardOnPlayHook {                                                // [dump sec-cards.txt]
    public static void RegisterGlobalListener(ICardOnPlayHookListener listener);
    public static Task RunCardOnPlayHooks(CardModel card, PlayerChoiceContext ctx, CardPlay play);
    public static Task<bool> BeforeCardOnPlay(BeforeCardOnPlayContext context);
    public static Task AfterCardOnPlay(AfterCardOnPlayContext context);
}
```
`[XML]` `ICardOnPlayHookListener` — "Receives hooks immediately before and after a card's own `OnPlay` method." `BeforeCardOnPlayContext` — "Context for hooks that run before a card's own `OnPlay` method." `ICardOnPlayHookListener.BeforeCardOnPlay` — "Runs before the card's own `OnPlay` method. Return `true` to skip that method without skipping the remaining `CardModel.OnPlayWrapper` flow." `CardOnPlayHook.RegisterGlobalListener` — "Registers a process-wide listener. Effects owned by a model should normally implement `ICardOnPlayHookListener` directly."

### 5.3 `Models.Capabilities.ICardPropertyContributor`

```csharp
public interface ICardPropertyContributor {                                        // [dump sec-all.txt]
    CardType?   GetCardType(CardModel card);
    CardRarity? GetCardRarity(CardModel card);
    TargetType? GetTargetType(CardModel card);
    IEnumerable<CardTag> GetTags(CardModel card);
}
```
`[XML]` — "Optional card capability that contributes card-facing property overrides." (Types are `MegaCrit.Sts2.Core.Entities.Cards.*`.)

### 5.4 Related card hooks

- `CardTypeTextHook` (`STS2RitsuLib.Cards`) — `RegisterGlobalModifier(ICardTypeTextModifier)`; `[XML]`: "Applies BaseLib-compatible card-type text modifiers supplied by cards, model capabilities, run or combat listeners, and registered global modifiers." See §10.
- `Cards.Transforms.ModCardTransformRegistry` — per-mod registry with `Register(string id, Action<ModCardTransformContext>)` / async / guarded variants and typed `Register<TOriginal,TReplacement>(string, Action<TOriginal,TReplacement>)`, `RegisterFrom<TOriginal>`, `RegisterTo<TReplacement>`, `Unregister(string)`; context = `{ CardModel Original, Replacement; CardPile OriginalPile; int OriginalPileIndex }`. `[dump sec-cards.txt]`
- `Cards.DynamicVars.*` — `ModCardVars` factory (10 types, `[dump sec-cards.txt]`): static `Int`, `String`, `Bool`, `Cards`, `Damage`, `OstyDamage`, `Block`, `Gold`, `Heal`, `HpLoss`, `MaxHp`, `Repeat`, `Forge`, `Summon`, `Energy`, `Stars`, `Power<T>`, plus `Computed`/`ComputedEnergy`/`ComputedStars`/`ComputedPower<T>`/`ComputedPowerAmountGiven<T>`/`ComputedDamage`/`ComputedOstyDamage`/`ComputedBlock` factories taking `Func<CardModel,decimal>` or `ComputedDynamicVarFactory` (delegate over `ComputedDynamicVarContext`). `[XML]` `Computed` — "Creates a `ComputedDynamicVar` with optional preview-specific evaluation."

## 6. Assets and runtime refresh

### 6.1 `Scaffolding.Content.Patches.ExternalAssetOverrideRegistry`

"Provides external asset overrides for non-card content." `[XML]` — full member list `[dump sec-content.txt]`. Every method is `static void Register<Kind>Provider(string id, Func<Model, …> provider)`; `Unregister(string)` and `Clear()` apply registry-wide.

```csharp
public static class ExternalAssetOverrideRegistry {
    // relic icons (the per-relic icon providers)
    public static void RegisterRelicIconPathProvider(string id, Func<RelicModel, string> provider);
    public static void RegisterRelicIconOutlinePathProvider(string id, Func<RelicModel, string> provider);
    public static void RegisterRelicIconTextureProvider(string id, Func<RelicModel, Texture2D> provider);
    public static void RegisterRelicIconOutlineTextureProvider(string id, Func<RelicModel, Texture2D> provider);
    public static void RegisterRelicBigIconTextureProvider(string id, Func<RelicModel, Texture2D> provider);
    // powers
    public static void RegisterPowerIconPathProvider(string id, Func<PowerModel, string>);
    public static void RegisterPowerIconTextureProvider(string id, Func<PowerModel, Texture2D>);
    public static void RegisterPowerBigIconTextureProvider(string id, Func<PowerModel, Texture2D>);
    // potions
    public static void RegisterPotionImagePathProvider(string id, Func<PotionModel, string>);
    public static void RegisterPotionOutlinePathProvider(string id, Func<PotionModel, string>);
    public static void RegisterPotionImageTextureProvider(string id, Func<PotionModel, Texture2D>);
    public static void RegisterPotionOutlineTextureProvider(string id, Func<PotionModel, Texture2D>);
    // orbs
    public static void RegisterOrbIconPathProvider(string id, Func<OrbModel, string>);
    public static void RegisterOrbIconTextureProvider(string id, Func<OrbModel, CompressedTexture2D>);
    public static void RegisterOrbVisualsScenePathProvider(string id, Func<OrbModel, string>);
    // acts
    public static void RegisterActBackgroundScenePathProvider(string id, Func<ActModel, string>);
    public static void RegisterActRestSiteBackgroundPathProvider(string id, Func<ActModel, string>);
    public static void RegisterActMapTopBgPathProvider(string id, Func<ActModel, string>);
    public static void RegisterActMapMidBgPathProvider(string id, Func<ActModel, string>);
    public static void RegisterActMapBotBgPathProvider(string id, Func<ActModel, string>);
    // events
    public static void RegisterEventBackgroundScenePathProvider(string id, Func<EventModel, string>);
    public static void RegisterEventLayoutScenePathProvider(string id, Func<EventModel, string>);
    public static void RegisterEventInitialPortraitTextureProvider(string id, Func<EventModel, Texture2D>);
    public static void RegisterEventBackgroundSceneProvider(string id, Func<EventModel, PackedScene>);
    public static void RegisterEventVfxSceneProvider(string id, Func<EventModel, PackedScene>);
    // encounters
    public static void RegisterEncounterScenePathProvider(string id, Func<EncounterModel, string>);
    public static void RegisterEncounterBackgroundScenePathProvider(string id, Func<EncounterModel, string>);
    public static void RegisterEncounterBackgroundLayersDirectoryProvider(string id, Func<EncounterModel, string>);
    public static void RegisterEncounterBossNodePathProvider(string id, Func<EncounterModel, string>);
    public static void RegisterEncounterMapNodeAssetPathsProvider(string id, Func<EncounterModel, IEnumerable<string>>);
    public static void RegisterEncounterRunHistoryIconPathProvider(string id, Func<EncounterModel, string>);
    public static void RegisterEncounterRunHistoryIconOutlinePathProvider(string id, Func<EncounterModel, string>);
    // ancients
    public static void RegisterAncientMapIconPathProvider(string id, Func<AncientEventModel, string>);
    public static void RegisterAncientMapIconOutlinePathProvider(string id, Func<AncientEventModel, string>);
    public static void RegisterAncientRunHistoryIconPathProvider(string id, Func<AncientEventModel, string>);
    public static void RegisterAncientRunHistoryIconOutlinePathProvider(string id, Func<AncientEventModel, string>);
    // afflictions, enchantments, modifiers
    public static void RegisterAfflictionOverlayPathProvider(string id, Func<AfflictionModel, string>);
    public static void RegisterAfflictionOverlaySceneProvider(string id, Func<AfflictionModel, PackedScene>);
    public static void RegisterEnchantmentIconPathProvider(string id, Func<EnchantmentModel, string>);
    public static void RegisterModifierIconPathProvider(string id, Func<ModifierModel, string>);
    public static bool Unregister(string id);
    public static void Clear();
}
```

Sibling registries in the same namespace (`[dump sec-content.txt]`): `ExternalBadgeIconOverrideRegistry` (`RegisterIconPathProvider(string, Func<string,string>)`, `RegisterFrameProvider(…)`), `ExternalCardMaterialOverrideRegistry` (material providers per card), `CardPoolDeckViewStyleRegistry` (`RegisterProvider(string, Func<CardPoolModel, CardPoolDeckViewStyle>)`).

### 6.2 `Scaffolding.Content.Patches.RuntimeAssetRefreshCoordinator`

"Coalesces runtime visual refresh requests for supported node types." `[XML]`

```csharp
public static class RuntimeAssetRefreshCoordinator {                                // [dump sec-all.txt]
    public static void Request(RuntimeAssetRefreshScope scope);
    public static void RequestCardsWhere(Predicate<CardModel> predicate);
    public static void RequestRelicsWhere(Predicate<RelicModel> predicate);
    public static void RequestPotionsWhere(Predicate<PotionModel> predicate);
    public static void RequestPowersWhere(Predicate<PowerModel> predicate);
    public static void RequestOrbsWhere(Predicate<OrbModel> predicate);
}
public enum RuntimeAssetRefreshScope { None, Cards, Relics, Potions, Powers, Orbs, AllSafe }
```
`Request(scope)` — "Requests a deferred refresh pass for the specified `scope`." `[XML]` (The `Where` overloads refresh only models matching the predicate; `AllSafe` excludes none of the safe categories.)

## 7. Run, lobby and multiplayer

### 7.1 Run saved data and lobby staging (`STS2RitsuLib.RunData`)

Run-scoped and per-player saved-data slots, with a lobby-staging phase for multiplayer start-run lobbies. All from `[dump sec-all.txt]`; summaries `[XML]`.

```csharp
public static class RunSavedDataStore {                          // "Provides a per-mod registry of run saved-data slots."
    public static RunSavedData<T> Register<T>(string key, Func<T> factory, RunSavedDataOptions options);
    public static PlayerRunSavedData<T> RegisterPerPlayer<T>(string key, Func<T> factory, RunSavedDataOptions options);
    // (+ GetOrCreate-style accessors; see RunSavedDataStore in sec-all.txt for the full list)
}
public sealed class RunSavedData<T> {                            // "Provides access to a saved-data slot shared by the whole run."
    public RunSavedDataLobbyScope<T> Lobby { get; }
    public T Get(RunState run);  public bool TryGet(RunState, out T);  public void Set(RunState, T);
    public bool Remove(RunState);  public T Modify(RunState, Action<T>);
}
public sealed class PlayerRunSavedData<T> {                      // "Provides access to run saved data stored separately for each player."
    public PlayerRunSavedDataLobbyScope<T> Lobby { get; }
    public T Get(RunState, ulong playerNetId);  public T Get(Player);
    public bool TryGet(RunState, ulong, out T);  public void Set(RunState, ulong, T);  public bool Remove(RunState, ulong);
    public T Modify(RunState, ulong, Action<T>);  public T Modify(Player, Action<T>);
}
public sealed class RunSavedDataLobbyScope<T> {                  // "Provides lobby staging access to a slot shared by the whole run."
    public T GetOrCreate(StartRunLobby lobby);  public bool TryGet(StartRunLobby, out T);
    public void Set(StartRunLobby, T);  public bool Remove(StartRunLobby);  public T Modify(StartRunLobby, Action<T>);
}
public sealed class PlayerRunSavedDataLobbyScope<T> {
    public T GetOrCreate(StartRunLobby, ulong);  public T GetOrCreate(StartRunLobby, Player);
    public bool TryGet(StartRunLobby, ulong, out T);  public void Set(StartRunLobby, ulong, T);  public void Set(StartRunLobby, Player, T);
    public bool Remove(StartRunLobby, ulong);  public T Modify(StartRunLobby, ulong, Action<T>);  public T Modify(StartRunLobby, Player, Action<T>);
}
public static class RunSavedDataLobby {                          // "Coordinates lobby-scoped run saved-data staging and commits staged values when a new run begins."
    public static void NotifyStagingChanged(StartRunLobby lobby);
    public static bool TryPushContribution(StartRunLobby lobby);
}
public sealed class RunSavedDataLobbyStagingEvent : IFrameworkLifecycleEvent {
    public StartRunLobby Lobby { get; set; }  public bool IsMultiplayer { get; set; }  public bool IsHost { get; set; }
    public RunSavedDataLobbyStagingReason Reason { get; set; }   public DateTimeOffset OccurredAtUtc { get; set; }
}
public enum RunSavedDataLobbyStagingReason { ContributionMerged, PlayerJoined, Manual, Committing, PlayerLeft }
public sealed class RunSavedDataOptions { int SchemaVersion; RunSavedDataWritePolicy WritePolicy; bool SyncLobbyOnChange; IReadOnlyList<IMigration> Migrations; }
```
`[XML]` `RunSavedDataLobbyStagingEvent` — "Notifies mods that start-run lobby staging data can be read or changed before it is committed to the run." Also `RunSavedDataPreparingEvent` exists for pre-run preparation. `[dump sec-all.txt]`

### 7.2 Managed net actions

```csharp
public static class RitsuLibManagedNetActions {                                     // [dump sec-all.txt]
    public const int MaxPayloadBytes;
    public static ulong Register<T>(RitsuLibManagedNetActionDescriptor<T> descriptor);
    public static bool Request<T>(RunManager runManager, RitsuLibManagedNetActionDescriptor<T> descriptor, T payload, ulong? targetNetId = null);
}
```
`[XML]` `RitsuLibManagedNetActions` — "Registers and requests RitsuLib-managed actions through vanilla action-enqueue messages." `Register<T>` — "Registers a managed net-action descriptor and returns its stable opcode. Registering the same module, action key, type, and action type is idempotent; an opcode conflict throws." `Request<T>` — "Serializes and requests a managed action through the vanilla action-queue synchronizer. A `true` result means the enqueue request was issued, not that its executor ran successfully."

Supporting types (`[dump sec-all.txt]`): `RitsuLibManagedNetAction` (abstract; "Base class for vanilla queue-action messages that carry RitsuLib-managed actions"), `RitsuLibManagedGameAction`, `RitsuLibManagedNetActionDescriptor<T>` (`{ string Module, ActionKey; Func<T,byte[]> Serialize; Func<ReadOnlySpan<byte>,T> Deserialize; Func<RitsuLibManagedNetActionContext<T>,Task> Execute; GameActionType ActionType }`), `RitsuLibManagedNetActionContext<T>` ("Provides runtime context to a managed net-action executor").

### 7.3 Message tail extensions

```csharp
public static class RitsuNetMessageTailExtensions {                                 // [dump sec-all.txt]
    public static void RegisterBytes<TMessage>(string extensionId, int maxPayloadBytes,
        Func<TMessage, byte[]> serialize, Action<int, ReadOnlyMemory<byte>> onReceive);
    public static void Write<TMessage>(this PacketWriter writer, TMessage message);
    public static void Read<TMessage>(this PacketReader reader);
}
```
`[XML]` — "Registers and dispatches bounded, versioned extension payloads appended to vanilla network messages." `RegisterBytes<TMessage>` — "Registers a bounded binary extension for `TMessage`." `Write<TMessage>` — "Appends all registered extensions for `TMessage` after its vanilla body." `Read<TMessage>` — "Reads and dispatches all registered extensions following the vanilla `TMessage` body."

### 7.4 `Networking.Sidecar.*` type-level index (58 public types)

OOB wire protocol layered on the vanilla multiplayer transport. `[api-0.111.0.json]`; key summaries `[XML]`.

- **Envelope/wire:** `RitsuLibSidecar` ("Builds Sidecar envelopes for the current wire layout"; `CreateEnvelope`, `CreateEnvelopeCompressed`, `CreateEnvelopeWithDelivery`, `CreateEnvelopeWithDeliveryCompressed`), `RitsuLibSidecarEnvelope`, `RitsuLibSidecarChunkGapBinary`, `RitsuLibSidecarHandshakeBinary`, `RitsuLibSidecarHeaderExtension`, `RitsuLibSidecarWire`, `RitsuLibSidecarWireFlags` (enum), `RitsuLibSidecarDeliverySemantics` (enum), `RitsuLibSidecarPayloadCompression` (enum), `RitsuLibSidecarControlOpcodes`, `RitsuLibSidecarOpcodes` (`For(module, action)`).
- **Bus/dispatch:** `RitsuLibSidecarBus` ("Dispatches Sidecar payloads and one-shot waiters by 64-bit opcode": `RegisterHandler(ulong, Action<RitsuLibSidecarDispatchContext>)`, `UnregisterHandler`, `ClearHandlers`, `WaitForNextAsync(ulong, TimeSpan, Func<…,bool>, bool, CancellationToken)`), `RitsuLibSidecarDispatchContext` ("Context for a received Sidecar envelope after magic detection, length checks, optional decompression, and opcode dispatch"; has `WithOwnedEnvelopeMemory()` for deferred work).
- **Chunked transfers:** `RitsuLibSidecarChunkBinary` (`FixedHeaderSize`, `DefaultMaxSegmentDataBytes`, `WriteFrame`, `ReadFrame`), `RitsuLibSidecarChunkStream`, `RitsuLibSidecarChunkReceiveProgress` (struct), `RitsuLibSidecarChunkStreamSendProgress` (struct), `RitsuLibSidecarChunkTransferNotifications`.
- **Typed messaging:** `RitsuLibSidecarTypedMessageRegistry`, `RitsuLibSidecarMessageDescriptor<T>`, `RitsuLibSidecarMessageBinding`, `RitsuLibSidecarTypedDispatchContext<T>` (struct), `IRitsuLibSidecarMessageCodec<T>` (`ulong Opcode; bool TryDecode(ReadOnlySpan<byte>, out T); void Encode(IBufferWriter<byte>, T)`), `RitsuLibSidecarJsonSerializer<T>`, `RitsuLibSidecarTypedMessageReceivedEvent`, `RitsuLibSidecarSyncMessages`, `RitsuLibSidecarSyncMessageDescriptor<T>`, `RitsuLibSidecarSyncMessageContext<T>` (struct), `IRitsuLibSidecarSyncProcessor<T>` (`void Apply(T, ref RitsuLibSidecarDispatchContext)`), `RitsuLibSidecarSyncBroadcastScope` (enum), `RitsuLibSidecarSyncFailurePolicy` (enum).
- **Sessions/connection:** `RitsuLibSidecarSessionManager`, `RitsuLibSidecarConnectionSession`, `RitsuLibSidecarConnectionExchange`, `RitsuLibSidecarNetworkMapping`, `RitsuLibSidecarProtocol`, `RitsuLibSidecarNetworkingLifecycle`, `RitsuLibSidecarEvents`, `RitsuLibSidecarHighLevelSend`, `RitsuLibSidecarSend`, `RitsuLibSidecarRequestCorrelation`, `RitsuLibSidecarRequestReply`, `RitsuLibSidecarTrafficCounters`, `RitsuLibSidecarNetDiagnosticsOptions`, `RitsuLibSidecarGodotMainLoopScheduling`, `RitsuLibSidecarResourcePolicy`.
- **Capabilities/config:** `IRitsuLibSidecarCapabilityValidationRoute` (`Name`, `Order`, `IsAvailable(INetGameService)`, `PublishLocalEvidence`, `TryResolve(INetGameService, ulong)`), `RitsuLibSidecarRequiredCapabilities`, `RitsuLibSidecarRequiredCapabilityPolicy` (enum), `RitsuLibSidecarPeerFeatures` (enum), `RitsuLibSidecarPeerReachability` (enum), `RitsuLibSidecarConfigSyncService` ("Provides host-authoritative Sidecar configuration synchronization"; `TopicChanged` event), `SidecarConfigTopicChangedEvent`, `SidecarPeerReachabilityChangedEvent`, `SidecarRequiredCapabilityCheckCompletedEvent`, `SidecarRequiredCapabilityMiss`, `SidecarSessionBoundEvent`, `SidecarSessionUnboundEvent`, `SidecarHandshakeCompletedEvent`.

## 8. UI scaffolding

### 8.1 `Scaffolding.Godot.NodeAttachments.ModNodeAttachmentRegistry`

"Provides a per-mod registry for attaching child nodes when a Godot parent enters `_Ready`." `[XML]`

```csharp
public sealed class ModNodeAttachmentRegistry {                                     // [dump sec-visuals.txt]
    public static ModNodeAttachmentRegistry For(string modId);
    public NodeAttachmentDefinition RegisterReadyChild<TParent,TNode>(string localId, Func<TParent,TNode> factory, NodeAttachmentOptions options);
    public NodeAttachmentDefinition RegisterReadyChild<TParent,TNode>(string localId, Func<TParent,TNode> factory, Action<TParent,TNode> setup, NodeAttachmentOptions options);
    public NodeAttachmentDefinition RegisterReadyChildFromScene<TParent,TNode>(string localId, string scenePath, Action<TParent,TNode> setup, NodeAttachmentOptions options);
    public NodeAttachmentDefinition RegisterReadyChildFromConvertedScene<TParent,TNode>(string localId, string scenePath, Action<TParent,TNode> setup, NodeAttachmentOptions options);
    public bool TryGetAttached<TParent,TNode>(TParent parent, string localId, out TNode node);
    public static bool TryGetAttachedById<TParent,TNode>(TParent, string qualifiedId, out TNode);
    public static void EnsureReadyAttachments(Node parent);
    public static NodeAttachmentDefinition[] GetDefinitionsSnapshot();
    public static string GetQualifiedNodeAttachmentId(string modId, string localId);
}
```
`RegisterReadyChild<TParent,TNode>` — "Registers a factory-created child for `TParent` `_Ready` callbacks." `EnsureReadyAttachments` — "Ensures that all `_Ready`-time attachments registered for `parent` are applied." `[XML]`

Options/policy types `[dump sec-visuals.txt]`:

```csharp
public sealed class NodeAttachmentOptions {                 // "Provides options for attaching child nodes during _Ready." [XML]
    public string Name;  public int Order;  public bool UniqueNameInOwner;  public bool IncludeDerivedParentTypes;
    public NodeAttachmentDuplicatePolicy DuplicatePolicy;   // AllowDuplicateName | ReuseExistingByName | SkipIfExistingByName | ReplaceExistingByName | ThrowIfExistingByName
    public NodeAttachmentAddMode AddMode;                   // AddChildSafely | AddChildDirect
    public Func<Node,Node> AttachParentSelector;  public NodeAttachmentSetupTiming SetupTiming;  // BeforeAdd | AfterAdd
    public int? ChildIndex;  public string InsertBeforeName;  public string InsertAfterName;  public bool QueueFreeReplacedNode;
}
```
`NodeAttachmentDefinition` exposes `ModId`, `Id`, `LocalId`, `ParentType`, `NodeType`, `Options`, `SourceKind`, `ScenePath`, `Order`, `Name`, `Setup`. `[dump sec-visuals.txt]`

### 8.2 `Screens.ModScreenService`

```csharp
public static class ModScreenService {                                              // [dump sec-all.txt]
    public static ICapstoneScreen CurrentCapstoneScreen { get; }
    public static bool IsCapstoneOpen { get; }
    public static bool Open(ICapstoneScreen screen);
    public static bool Close();
    public static bool Toggle(ICapstoneScreen screen);
}
```
`[XML]` — "Opens, closes, and queries custom `ICapstoneScreen` instances through `NCapstoneContainer`." `Open` — "Mounts `screen` in `NCapstoneContainer`. Opening the screen replaces a different current screen; opening the already current instance is a no-op." `Close` — "Closes the current Capstone screen, if any." `Toggle` — "Closes `screen` when it is current; otherwise opens it."

### 8.3 `Ui.Windows.RitsuFloatingWindow`

```csharp
public sealed class RitsuFloatingWindow : Godot.PanelContainer {                    // [dump sec-all.txt]
    public event EventHandler Closed;   public event EventHandler GeometryChanged;
    public RitsuFloatingWindowOptions Options { get; set; }
    public bool InteractionLocked { get; set; }
    public RitsuFloatingWindow();  public RitsuFloatingWindow(RitsuFloatingWindowOptions options);
    public void Configure(RitsuFloatingWindowOptions options);
    public Control SetContent(Control content);     // returns previous content (detached, not freed)
    public Control TakeContent();
    public RitsuFloatingWindowGeometry CaptureGeometry();
    public void ApplyGeometry(RitsuFloatingWindowGeometry geometry);
    public void Close();
    // Godot lifecycle: _Ready, _ExitTree, _Input
}
public sealed class RitsuFloatingWindowOptions {                                     // [dump sec-all.txt]
    public string Title;  public Vector2 InitialSize;  public bool FitInitialSizeToContent;
    public Vector2 MinimumSize;  public Vector2 MaximumSize;
    public bool Movable;  public bool Resizable;  public bool Closable;
    public bool StartCentered;  public bool ConstrainToViewport;
}
public struct RitsuFloatingWindowGeometry { public Vector2 Position; public Vector2 Size; }
```
`[XML]` `RitsuFloatingWindow` — "Provides a themed content window that can remain fixed or allow dragging and eight-direction resizing. It also supports replacing content and saving or restoring window geometry." `SetContent` — "Replaces the window content and returns the previous content. The new content must be a valid unattached control. Replaced content is detached but not freed."

### 8.4 `Scaffolding.Godot.RitsuGodotTreeCompat`

```csharp
public static class RitsuGodotTreeCompat {                                          // [dump sec-visuals.txt]
    public static void AddChildSafely(Node parent, Node child);
    public static void MoveChildSafely(Node parent, Node child, int index);
}
```
`[XML]` — "Provides tree mutations matching the base game's `GodotTreeExtensions` behavior on versions that expose those helpers. Game version 0.103.2 lacks `MoveChildSafely`, so this compatibility API allows the same layout code to compile against every supported version." `AddChildSafely` — "Adds a child immediately or defers the call using the same conditions as the base game's `MegaCrit.Sts2.Core.Helpers.GodotTreeExtensions.AddChildSafely`."

Related: `RitsuGodotNodeFactories` ("Provides explicitly invoked Godot node construction APIs. These methods do not patch `PackedScene.Instantiate`, so BaseLib scene conversion and base-game loading retain control of their own hooks." `[XML]`) with `CreateFromResource<TNode>`, `CreateFromScene<TNode>(PackedScene, …)`, `CreateFromScenePath<TNode>(string, …)`, plus `RegisterFactory<TNode>(…)`. `[dump sec-visuals.txt]`

## 9. Harmony patch inventory

40 Harmony patch classes are shipped; every one is annotated with `[HarmonyPriority(0)]` except four keyword patches. The inventory below is verbatim from `allpatches.txt` `[dump]`.

### 9.1 Unannotated (1)

- `STS2RitsuLib.Ui.Overlay.RitsuOverlayActiveScreenPatch` — `[HarmonyPriority(0)]`

### 9.2 `[HarmonyAfter(["BaseLib"])]` — 35 patches (define coexistence: run after BaseLib's equivalent hooks)

Settings screen integration:
- `STS2RitsuLib.Settings.Patches.MainMenuModSettingsButtonPatch`
- `STS2RitsuLib.Settings.Patches.ModSettingsSubmenuPatch`
- `STS2RitsuLib.Settings.Patches.SettingsScreenModSettingsButtonPatch`

Run-history / asset path routing (Scaffolding.Content.Patches):
- `ImageHelperAncientModRunHistoryIconPathPatch`, `ImageHelperModEncounterRunHistoryIconPathPatch`, `MonsterVisualsPathPatch`

Character asset paths & SFX (Scaffolding.Characters.Patches) — 25 patches: `CardLibraryCompendiumPatch`, `CharacterIconOutlineTexturePathPatch`, `CharacterVisualsPathPatch`, `CharacterEnergyCounterPathPatch`, `CharacterMerchantAnimPathPatch`, `CharacterRestSiteAnimPathPatch`, `CharacterIconTexturePathPatch`, `CharacterIconPathPatch`, `CharacterSelectBgPathPatch`, `CharacterSelectIconPathPatch`, `CharacterSelectLockedIconPathPatch`, `CharacterMapMarkerPathPatch`, `CharacterSelectTransitionPathPatch`, `CharacterTrailPathPatch`, `CharacterAttackSfxPatch`, `CharacterCastSfxPatch`, `CharacterDeathSfxPatch`, `CharacterArmPointingTexturePathPatch`, `CharacterArmRockTexturePathPatch`, `CharacterArmPaperTexturePathPatch`, `CharacterArmScissorsTexturePathPatch`, `CharacterEnergyCounterStarAnchorPatch`

Health bars (Combat.HealthBars.Patches):
- `NHealthBarReadyForecastPatch`, `NHealthBarRefreshForegroundOrderedPatch`, `NHealthBarContainerResizeForecastPatch`, `NHealthBarRefreshMiddlegroundForecastPatch`, `NHealthBarRefreshTextForecastPatch`

### 9.3 `[HarmonyBefore(["BaseLib"])]` — 6 patches

Animation playback (must own the frame before BaseLib converts):
- `STS2RitsuLib.Scaffolding.Characters.Patches.ModCreatureCombatAnimationPlaybackPatch`
- `STS2RitsuLib.Scaffolding.Characters.Patches.ModMerchantCharacterVisualPlaybackPatch`

Keyword routes — `[HarmonyPriority(800)]` (higher than the default 0):
- `STS2RitsuLib.Keywords.Patches.CardKeywordGetTitleModRoutePatch`
- `STS2RitsuLib.Keywords.Patches.CardKeywordGetDescriptionModRoutePatch`
- `STS2RitsuLib.Keywords.Patches.CardKeywordGetCardTextModRoutePatch`
- `STS2RitsuLib.Keywords.Patches.HoverTipFactoryFromKeywordPatch`

### 9.4 Patches outside `allpatches.txt`

The loader shim additionally installs `STS2RitsuLib.Loader.ReflectionHelperModTypesPatch` (postfix on `ReflectionHelper.GetModTypes`) and the reflection-bridge patch that associates the loaded variant assembly with the mod. `[loader.json]`. `ModTypeDiscoveryPatch` (in `STS2RitsuLib.Interop.Patches`) runs the discovery hub once "at the same lifecycle point used by BaseLib, before later game systems consume localization data." `[XML]`

### 9.5 Patching API (for your own patches)

`STS2RitsuLib.Patching.Core.ModPatcher` + `ModPatcherExtensions`, `PatchTargetMethodResolver`, `PatchLog`; `Patching.Models`: `IModPatches`, `ModPatchInfo`, `ModPatchTarget`, `ModPatchResult`, `PatchTarget`; `Patching.Rules`: `ModPatchRule`, `PatchRuleBuilder`; `Patching.Builders.DynamicPatchBuilder`; `Patching.PrivateAccess`. `[dump sec-patching.txt; nsindex.md]` `IModPatches` is the conventional Harmony-patch container consumed by `ModPatcher`.

## 10. Interop with BaseLib

Every explicit BaseLib accommodation found in the library (signatures `[dump sec-all.txt]`, prose `[XML]`). Harmony ordering between the two libraries is covered in §9.

**Identity constants** — `STS2RitsuLib.Const` exposes `BaseLibHarmonyId` ("BaseLib's primary Harmony instance ID") and `FrameworkContentRegistryHarmonyId`. `[XML]`

**Card type text** — the composition contract is shared with BaseLib:
```csharp
public interface ICardTypeTextModifier {                        // STS2RitsuLib.Models.Capabilities
    IEnumerable<LocString> GetTypeModifiers(CardModel card);
}
public static class CardTypeTextHook {                           // STS2RitsuLib.Cards
    public static void RegisterGlobalModifier(ICardTypeTextModifier modifier);
}
public interface ICustomTypeTextCard { IEnumerable<LocString> GetTypeModifiers(); }
```
`[XML]` `ICardTypeTextModifier` — "Optional model or model-capability hook for visually modifying cards' type text. The method signature and composition contract match BaseLib's `ICardTypeTextModifier`." `ICustomTypeTextCard` — "Optional card interface for visually modifying its own type text. Returned strings use the same composition contract as BaseLib: entries containing `{Type}` wrap the selected base text, while entries without it replace the base text." `CardTypeTextHook` — "Applies BaseLib-compatible card-type text modifiers supplied by cards, model capabilities, run or combat listeners, and registered global modifiers." Implementation is `Models.Capabilities.Patches.CardModelCapabilityPatches.TypeTextPatch` — "Applies BaseLib-compatible type-text modifiers before the plaque LocString is formatted."

**Custom target types** — `Combat.CardTargeting.BaseLibTargetTypeBridge` — "Bridges BaseLib custom-target predicates when BaseLib is loaded." (Internal plumbing; consumers use `CustomTargetType.RegisterSingleTargetType/RegisterMultiTargetType/…`.) `[XML]`

**Max hand size** — `Combat.HandSize.MaxHandSizeCalculator.Calculate(Player)` — "Calculates the effective maximum hand size for `player`. Uses BaseLib's value as the base when available, then applies RitsuLib hook-listener modifiers once." `[XML]` Supporting surface: `IMaxHandSizeModifier { int ModifyMaxHandSize(Player, int); int ModifyMaxHandSizeLate(Player, int); }` and `MaxHandSizeCalculator.ApplyHookListenerModifiers(Player, int)`. ⚠ `Combat.HandSize.BaseLibMaxHandSizeBridge` — "Bridges BaseLib's maximum-hand-size support by detecting its active patches, extending its calculator with RitsuLib modifiers, and using its result as the base value when available" — is documented in the XML but **`NotPublic` (internal) in the shipped binary** (`[api-0.111.0.json]`); consumer code cannot call it directly.

**Health bars** — two registries bridge into BaseLib's foreign-consumer API so one renderer serves both libraries:
- `Combat.HealthBars.BaseLibHealthBarForecastBridge` — "Bridges `HealthBarForecastRegistry.GetSegments(Creature)` to BaseLib's `HealthBarForecastRegistry.RegisterForeign` API so a single renderer can consume both libraries' forecast segments."
- `Combat.HealthBars.BaseLibVisualGraftBridge` — "Bridges `HealthBarVisualGraftRegistry.Aggregate(Creature)` to BaseLib's `HealthBarVisualGraftRegistry.RegisterForeign` API so a single renderer can consume both libraries' visual-extension metrics."
- `Combat.HealthBars.Patches.NHealthBarForecastPatchHelper` — "Renders RitsuLib and imported legacy BaseLib forecasts on `NHealthBar` while BaseLib's current renderer has not taken ownership."
Both bridge types are public in the binary (`[api-0.111.0.json]`); their exact members are internal (dumps list only the type-level XML summary).

**Type discovery timing** — `ModTypeDiscoveryHub` — "Extensible post-mod-load type-discovery pipeline invoked during early localization initialization. It mirrors BaseLib's scan timing without coupling discovery to one feature." Its `ModTypeDiscoveryPatch` "Runs `ModTypeDiscoveryHub` once at the same lifecycle point used by BaseLib, before later game systems consume localization data." `STS2RitsuLib.Lifecycle.Patches.ReflectionHelperModTypeCachePostModLoadPatch` — "Clears the base-game mod-type cache at the first initialization point after mods load, before other mods such as BaseLib consume `ReflectionHelper.ModTypes`." `[XML]`

**Animation graph shape** — `Scaffolding.Visuals.StateMachine.ModAnimStateMachines` — "They mirror baselib's `CustomCharacterModel.SetupAnimationState` shape for Spine and for the non-Spine backends selected from a visuals root." `[XML]` (§4.3).

**Godot node construction — no global instantiation patch** — `Scaffolding.Godot.RitsuGodotNodeFactories` — "Provides explicitly invoked Godot node construction APIs. These methods do not patch `PackedScene.Instantiate`, so BaseLib scene conversion and base-game loading retain control of their own hooks." Related types that name BaseLib explicitly: `RitsuGodotNodeSlot<T>` ("Stores named-slot metadata… corresponding to BaseLib's `NodeInfo<T>`"), `RitsuNode2DSceneRootFactory` ("The factory mirrors BaseLib's flexible root conversion without requiring any named child slots"), `RitsuNRestSiteCharacterNodeFactory` ("This is RitsuLib's explicit-factory counterpart to BaseLib's `NRestSiteCharacter`…"), `RitsuGodotNodeFactoryRegistry` ("Conversion runs only through explicit factory calls; no global `PackedScene.Instantiate` postfix is installed, so BaseLib and base-game scene loading are unaffected"), `RitsuNCreatureVisualsNodeFactory`. `[XML]`

**Settings storage** — `Settings.ModSettingsCallbackValueBinding<TValue>` — "Binds a mod setting to custom read, write, and save callbacks instead of `RitsuLibFramework.GetDataStore(string)`, for example when using a BaseLib JSON configuration or a third-party store." `[XML]`

## Appendix — what could not be read

- Parameter **names** are not preserved by the metadata dumps (only types, in declaration order); XML docs carry no signatures. Signatures above are type-exact, name-free except where XML prose names them (`slotIndex`, `priority`, `eligible`, `modId`, `extensionId`, `maxPayloadBytes`).
- The exact 12-arg shape of `ModAnimStateMachines.Standard/StandardCue/StandardMerchantCue/StandardRestSiteCue` is truncated in `sec-visuals.txt` (768-char line cap); the dump records the parameter types as repeated `string, bool` pairs — re-read `sec-visuals.txt` lines for `ModAnimStateMachines` before writing code against it.
- `ModCharacterTemplate<…>`'s interface list is truncated in the dump (`…,`); the listed member set is complete.
- Full member lists of the three BaseLib bridge classes (public type, internal members) and `RunSavedDataStore`'s remaining accessors: read from `api-0.111.0.json` / `sec-all.txt` if needed.
- `ModEncounterTemplate`'s full virtual surface in the dump matches the listed members; `BuildProgrammaticCombatBackground` returns the game's `Sts2.Core.Rooms.BackgroundAssets`.
