# BaseLib API Reference

**Target:** BaseLib for Slay the Spire 2 v0.111.0 (Godot 4.5 / C# / .NET 9)

**Versions covered:**
- **Shipped binary (what the game loads):** `G:/steam/steamapps/common/Slay the Spire 2/mods/BaseLib/BaseLib.dll`, manifest `BaseLib.json` → `"version": "v3.3.5"` (installed mod folder). Assembly version metadata is `0.0.0.0`, so the manifest is the only version evidence.
- **Source tree:** `G:/omp works/sts2-spire1/research/BaseLib-StS2/`, git tag `v3.4.5` (HEAD `2275793`). Richer (XML docs) but **ahead of the binary**.

**Availability markers used throughout:**
- `SHIPPED` — present in the installed v3.3.5 DLL (verified against a full ilspycmd decompile; signatures copied from it).
- `SOURCE-ONLY` — exists in the v3.4.5 source tree but **not** in the shipped v3.3.5 binary. **Unusable against the installed DLL.** Building against it will compile but crash/reflection-fail at load. Never use these for new code.
- `ENGINE` — part of the base game (`MegaCrit.Sts2.Core.*`), documented here because BaseLib code builds on it.

**Citation convention:** every entry cites where it was read from:
- `dll <path>` — decompiled shipped binary, file under `G:/omp works/sts2-spire1/.tmp/baselib-dll/` (path + line).
- `src <path>` — source tree file under `research/BaseLib-StS2/` (path + line).
- `engine <path>` — decompiled base game under `G:/omp works/sts2-spire1/.tmp/dllsrc/`.
- `prior-audit` — `research/BaseLib-unused-surface.md` (a verified audit of this same library; reuse of its citations).

Signatures are copied verbatim from the shipped decompile (or from source for SOURCE-ONLY members, and marked as such). Where a member exists in both but differs, the shipped signature wins and the drift is noted.

Sections:
1. How a BaseLib mod is structured
2. `Abstracts/` — the content base classes
3. Interfaces
4. Visuals and assets
5. Localization
6. Hooks
7. Utilities and extensions
8. Patches
9. Version skew table

---

## 1. How a BaseLib mod is structured

### 1.1 On-disk layout

A mod ships as a folder inside `Slay the Spire 2/mods/<ModId>/` with at least two files:

| File | Purpose |
|---|---|
| `<ModId>.dll` | Compiled C# assembly (carries `[ModInitializer]` entry point and Harmony patches) |
| `<ModId>.json` | Manifest: `{ "id", "name", "author", "description", "version", "has_pck", "has_dll", "dependencies", "affects_gameplay" }` (`src BaseLib.json:1-9`; the shipped copy is identical minus `min_game_version`) |
| `<ModId>.pck` | Optional Godot 4.5 package with `res://<ModId>/...` assets (scenes, images, `localization/<lang>/*.json`) — BaseLib ships `BaseLib.pck` + `BaseLib.json` (`dll` mod folder listing) |

BaseLib itself declares `"id": "BaseLib"`, `"has_pck": true`, `"has_dll": true`, `"affects_gameplay": false` (`shipped BaseLib.json:1-9`). Mods that change gameplay must list `"dependencies": ["BaseLib"]` **and** `"affects_gameplay": true`; BaseLib only enables its gameplay-modifying patches when a loaded gameplay-affecting mod depends on it (`src Patches/PostModInitPatch.cs:49-60`, `EarlyPostInit`).

### 1.2 Mod id, content-id prefix, and how IDs are formed

- The mod id (`BaseLib.json` `id`) is the folder name and the `res://<ModId>` asset root.
- Every content type gets a **content-id** = `<PREFIX><entry>` where `<PREFIX>` is the root namespace of the class, uppercased, plus `-` (`TypeExtensions.GetPrefix`, `src Extensions/TypePrefix.cs:8-17`, `SHIPPED`). Example: class `Spire1.Spire1Code.Relics.BurningBlood` in our mod → id `SPIRE1-BURNING_BLOOD`.
- The prefixing is applied at `ModelDb.GetEntry` time by `PrefixIdPatch` (`[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry))]`, `src Patches/Content/PrefixIdPatch.cs:10-40`, `SHIPPED`): any type assignable to `ICustomModel` gets the prefix; `[CustomID]` (`Utils/Attributes/CustomIDAttribute.cs`, `SHIPPED`) overrides it wholesale.
- `RemovePrefix()` strips it back (`src Extensions/StringExtensions.cs:11-14`, `SHIPPED`). Our mod's path helpers rely on this: `Spire1Relic` builds `"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png"` (`mod/Spire1Code/Relics/Spire1Relic.cs:27`).

### 1.3 How content classes are discovered and registered

Two mechanisms, both active:

1. **Engine reflection sweep.** `ModelDb.AllAbstractModelSubtypes` unions the built-in list with `ReflectionHelper.GetSubtypesInMods<AbstractModel>()` (`engine MegaCrit.Sts2.Core.Models/ModelDb.cs:68-88`). The base game therefore discovers **every** `AbstractModel` subtype in a mod assembly on its own — this is why `CustomMonsterModel`'s constructor only calls `RegisterType` while pools/characters/acts/encounters need explicit `CustomContentDictionary` registration.
2. **BaseLib's `CustomContentDictionary`** (`src Patches/Content/ContentPatches.cs:24-90`, shipped as `Baselib.Patches.Content/CustomContentDictionary.cs`, `SHIPPED`; it patches `ModelDb.InitIds`). Public statics: `RegisteredTypes`, `CustomCharacters`, `CustomEncounters`, `CustomAncients`, `CustomBadgeTypes`, `ActCustomEvents`, `SharedCustomEvents`, `CustomActs`, plus `RegisterType(Type)`, `AddModel(Type)` (requires a `[Pool]` attribute, `Utils/PoolAttribute.cs`, `SHIPPED`), `AddEncounter(CustomEncounterModel)`, `AddAncient(CustomAncientModel)`, `AddEvent(CustomEventModel)`, `AddBadge(Type)`, `AddAct(CustomActModel)`, `AddCharacter(CustomCharacterModel)`.

The abstract constructors call these: `CustomCardModel`/`CustomRelicModel`/`CustomPotionModel` → `AddModel(GetType())`; `CustomEncounterModel` → `AddEncounter(this)`; `CustomActModel` → `AddAct(this)`; `CustomCharacterModel` → `AddCharacter(this)`; `CustomEventModel` → `AddEvent(this)`; `CustomMonsterModel` → `RegisterType(GetType())`; `CustomCharacterSelectEntry` → `CustomCharacterSelectEntryRegistry.Register(this)`.

### 1.4 Initialization order

1. **Mod assembly init:** the engine calls `[ModInitializer] Initialize()` (our `MainFile.Initialize`, `mod/Spire1Code/MainFile.cs:24-45`): `SimpleLoc.EnableSimpleLoc(ModId)` → `ScriptManagerBridge.LookupScriptsInAssembly` → `ModConfigRegistry.Register(ModId, config)` → `harmony.PatchAll()`. BaseLib's own `BaseLibMain.Initialize` (shipped `dll BaseLib/BaseLibMain.cs:47-78`) does `NodeFactory.Init()`, script lookup, config registration, three targeted patches (`ExtendedSavePatches`, `TheBigPatchToCardPileCmdAdd`, `CustomBadgesPatch`), then `MainHarmony.TryPatchAll(assembly)` and `CustomLocTableManager.Register("card_modifiers")`.
2. **Early post-init** — `PostModInitPatch.EarlyPostInit` runs as a **prefix on `LocManager.Initialize`** (`src Patches/PostModInitPatch.cs:39-46`): computes `CanModifyGameplay`, calls `CardModifier.RegisterSave()` when applicable, initializes custom net-message wrappers, patches `AddActContent`, and processes every mod type for `CustomResource` and `IAutoRegisterFormatSpecifier` registration.
3. **Late post-init** — `PostModInitPatch.LatePostInit` runs as a **prefix on `ModelDb.InitIds`** (`src Patches/PostModInitPatch.cs:99-104`), after `SavedPropertiesTypeCache` exists.
4. **Scene registration** — nested `RegisterSceneConversions` class runs as a **prefix on `ModelDb.Preload`** (`src Patches/PostModInitPatch.cs:118-136`): it calls `RegisterSceneConversions()` on every `ISceneConversions` model. It is patched (not done in constructors) because virtual path properties may depend on fields set in derived constructors.
5. **Loc** — `ModelLocPatch` (postfix on `ModelDb.Init`) writes `ILocalizationProvider.Localization` entries into loc tables (`src Patches/Localization/ModelLocPatch.cs:33-57`), and `CustomLocTableManager` adds custom table names to `ModManager.GetModdedLocTables` (`dll BaseLib.Utils/CustomLocTableManager.cs`, `SHIPPED`).

---

## 2. `Abstracts/` — the content base classes

All are in namespace `BaseLib.Abstracts`. "abstract members a subclass must override" are marked **MUST**; the rest are optional `virtual` overrides. Every type below is **SHIPPED** unless flagged SOURCE-ONLY.

### 2.1 `CustomCardModel` — SHIPPED
`public abstract class CustomCardModel : CardModel, ICustomModel, ILocalizationProvider` (`dll BaseLib.Abstracts/CustomCardModel.cs:16`; docs `src Abstracts/CustomCardModel.cs`)

Constructor: `public CustomCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true)` — auto-registers via `CustomContentDictionary.AddModel(GetType())` (`src :23-26`).

| Member | Signature | Notes |
|---|---|---|
| `GainsBlock` | `public override bool GainsBlock => DynamicVars.Any(dynVar => dynVar.Value is BlockVar or CalculatedBlockVar)` | convenience override (`src :16-19`) |
| `CustomFrame` | `public virtual Texture2D? CustomFrame => null` | custom card-back texture, load via `ResourceLoader.Load<Texture2D>` |
| `CustomFrameMaterial` | `public Material? CustomFrameMaterial { get; }` | lazily wraps `CreateCustomFrameMaterial` |
| `CustomBannerMaterial` | `public Material? CustomBannerMaterial { get; }` | lazily wraps `CreateCustomBannerMaterial` |
| `CreateCustomFrameMaterial` | `public virtual Material? CreateCustomFrameMaterial => null` | see `ShaderUtils.GenerateHsv` |
| `CreateCustomBannerMaterial` | `public virtual Material? CreateCustomBannerMaterial => null` | |
| `CustomBannerMaterialPath` | `public virtual string? CustomBannerMaterialPath => null` | base-game paths live in `CardModel.BannerMaterialPath` |
| `CustomPortraitPath` | `public virtual string? CustomPortraitPath => null` | |
| `CustomPortrait` | `public virtual Texture2D? CustomPortrait => null` | |
| `Localization` | `public virtual List<(string, string)>? Localization => null` | ILocalizationProvider, see §5 |

Statics: `FinishMakeCalculatedVar(CalculatedVar var, int baseVal, int bonusVal)`, `MakeCalculatedVar(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1)`, `MakeCalculatedDamage(int baseVal, ...)`, `MakeCalculatedDamage(string name, ...)`, `MakeCalculatedBlock(int baseVal, ...)`, `MakeCalculatedBlock(string name, ...)` — the `...` being `Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = (ValueProp)8`; all return `IEnumerable<DynamicVar>` (`dll :37-56`).
**Use:** every card in `mod/Spire1Code/Cards/` derives from it (via our `CustomCardModel`-derived classes, e.g. `Cards/Anger.cs`).

### 2.2 `ConstructedCardModel` — SHIPPED
`public abstract class ConstructedCardModel : CustomCardModel` (`dll BaseLib.Abstracts/ConstructedCardModel.cs:16`; docs `src Abstracts/ConstructedCardModel.cs`)

Fluent builder for simple cards; vars/keywords/tips/tags are collected then **sealed**: `protected sealed override IEnumerable<DynamicVar> CanonicalVars`, `public sealed override IEnumerable<CardKeyword> CanonicalKeywords`, `protected sealed override IEnumerable<IHoverTip> ExtraHoverTips`, `protected sealed override HashSet<CardTag> CanonicalTags` (`dll :24-32`).

Builder methods (all `protected`, all return `ConstructedCardModel`, default `int upgrade = 0` where noted): `WithVars(params DynamicVar[])`, `WithVar(string name, int baseVal, int upgrade = 0)`, `WithVar(DynamicVar)`, `WithBlock(int, int upgrade = 0)`, `WithDamage(int, int upgrade = 0)`, `WithCards(int, int upgrade = 0)`, `WithEnergy(int, int upgrade = 0)`, `WithHeal(int, int upgrade = 0)`, `WithPower<T>(int baseVal, int upgrade = 0) where T : PowerModel`, `WithPower<T>(string name, ...)`, `WithTags(params CardTag[])`, `WithCalculatedVar(...)` (2 overloads), `WithCalculatedBlock(...)` (4 overloads), `WithCalculatedDamage(...)` (4 overloads), `WithKeywords(params CardKeyword[])`, `WithKeyword(CardKeyword, UpgradeType upgradeType = UpgradeType.None)`, `WithCostUpgradeBy(int amount)`, `WithTip(TooltipSource)`, `WithTips(Func<CardModel, IEnumerable<IHoverTip>>)`, `WithEnergyTip()`, `WithUpgradingCardTip<T>(Action<T, CardModel>? modifyTipCard = null) where T : CardModel` (`dll :34-299`).
`protected enum UpgradeType { None, Add, Remove }` (`dll :18-24`); `public void ConstructedUpgrade()` applies upgrade keywords + `CostUpgrade` (`dll :355-373`).

### 2.3 `CustomRelicModel` — SHIPPED
`public abstract class CustomRelicModel : RelicModel, ICustomModel, ILocalizationProvider` (`dll BaseLib.Abstracts/CustomRelicModel.cs:12`)

- `public CustomRelicModel(bool autoAdd = true)` — `CustomContentDictionary.AddModel(GetType())` when `autoAdd`.
- `public virtual List<(string, string)>? Localization => null`
- `public virtual RelicModel? GetUpgradeReplacement() => null` — called when a starter relic is upgraded (see `TouchOfOrobas.GetUpgradedStarterRelic` patch, §8).

**Use:** our `Relics/Spire1Relic.cs` derives from it and overrides `PackedIconPath` / `PackedIconOutlinePath` / `BigIconPath` with `ImagePath()` helpers; `Relics/MagicFlower.cs` additionally implements `IHealAmountModifier` and returns a `RelicLoc`.

### 2.4 `CustomPowerModel` — SHIPPED
`public abstract class CustomPowerModel : PowerModel, ICustomPower, ICustomModel, ILocalizationProvider, IHealthBarForecastSource` (`dll BaseLib.Abstracts/CustomPowerModel.cs:13`)

- `public virtual string? CustomPackedIconPath => null`
- `public virtual string? CustomBigIconPath => null`
- `public virtual string? CustomBigBetaIconPath => null`
- `public virtual List<(string, string)>? Localization => null`
- `public virtual IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context) => Array.Empty<...>()` — HP-bar forecast painting is built in (Poison/Doom-style prediction), §6.

**Use:** our `Powers/Spire1Power.cs` derives from it, overriding `CustomPackedIconPath`/`CustomBigIconPath` from `PowerImagePath()`, and forces abstract `Type`/`StackType`.

### 2.5 `CustomTemporaryPowerModel` — SHIPPED
`public abstract class CustomTemporaryPowerModel : CustomPowerModel, ITemporaryPower, IBetaCompatTempPower, IAddDumbVariablesToPowerDescription` (`dll BaseLib.Abstracts/CustomTemporaryPowerModel.cs:22`)

A power that applies another power and wears off at the end of the side's turn. Abstract members (**MUST**):
```csharp
protected abstract Func<PlayerChoiceContext, Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc { get; }
public abstract PowerModel InternallyAppliedPower { get; }
public abstract AbstractModel OriginModel { get; }
```
Virtuals: `protected virtual bool UntilEndOfOtherSideTurn => false;`, `protected virtual int LastForXExtraTurns => 0;`, `protected virtual bool InvertInternalPowerAmount => false;` (negative-amount power, e.g. Strength-down). Fixed behavior: `public override PowerType Type => InternallyAppliedPower.Type;`, `public override PowerStackType StackType => (PowerStackType)1;` (Counter), `public override bool AllowNegative => true;`. Lifecycle: `public override async Task BeforeApplied(...)`, `AfterPowerAmountChanged(...)`, `AfterSideTurnEnd(...)`; `public void AddDumbVariablesToPowerDescription(LocString description)` adds `{TemporaryPowerTitle}`; `public void IgnoreNextInstance()` (`dll :24-90`).

### 2.6 `CustomTemporaryPowerModelWrapper<TModel, TPower>` — SHIPPED
`public abstract class CustomTemporaryPowerModelWrapper<TModel, TPower> : CustomTemporaryPowerModel where TModel : AbstractModel where TPower : PowerModel` (`dll BaseLib.Abstracts/CustomTemporaryPowerModelWrapper.cs:17`; docs `src :10-14`)

"Ease of use" wrapper: `OriginModel => ModelDb.GetById<AbstractModel>(ModelDb.GetId<TModel>())`, `InternallyAppliedPower => ModelDb.Power<TPower>()`, `ApplyPowerFunc` delegates to `PowerCmd.Apply` via `BetaMainCompatibility.PowerCmd_`; icon paths flip on sign using shipped `BaseLib/images/powers/baselib-power_temp_{up,down}.png` / `big/` variants (`src :20-32`); `Title`/`ExtraHoverTips` derive from `OriginModel` by model type (`src :38-100`); `Description` picks `BASELIB-CUSTOM_TEMPORARY_POWER_MODEL.{UP,DOWN}.description` (`dll :66-72`).
**Use:** this is the pattern for StS1 "temporary X" powers (Mutagenic Strength) — `prior-audit §7`.

### 2.7 `CustomPotionModel` — SHIPPED
`public abstract class CustomPotionModel : PotionModel, ICustomModel, ILocalizationProvider` (`dll BaseLib.Abstracts/CustomPotionModel.cs:13`)

- `public CustomPotionModel()` / `public CustomPotionModel(bool autoAdd = true)` — `AddModel` when `autoAdd`.
- `public virtual string? CustomPackedImagePath => null` / `public virtual string? CustomPackedOutlinePath => null` — prefixes on the engine's `PackedImagePath`/`PackedOutlinePath` getters via nested `ImagePatch`/`OutlinePatch` (`dll :17-40`).
- `public virtual List<(string, string)>? Localization => null`
- `[Obsolete] public virtual bool AutoAdd => true` — pass `autoAdd` in the constructor instead (`dll :43-45`).

### 2.8 `CustomCharacterModel` — SHIPPED
`public abstract class CustomCharacterModel : CharacterModel, ICustomModel, ILocalizationProvider, ISceneConversions` (`dll BaseLib.Abstracts/CustomCharacterModel.cs:16`; docs `src Abstracts/CustomCharacterModel.cs:23`)

Constructor `public CustomCharacterModel()` → `CustomContentDictionary.AddCharacter(this)`. All visual/audio paths default `null` (→ engine fallbacks):

`CustomVisualPath`, `CustomTrailPath`, `CustomIconTexturePath`, `CustomIconOutlineTexturePath`, `CustomIconPath`, `CustomIcon` (`Control?`), `CustomEnergyCounter` (`CustomEnergyCounter?`, struct at `dll :141-156`), `CustomEnergyCounterPath`, `CustomRestSiteAnimPath`, `CustomMerchantAnimPath`, `CustomArmPointingTexturePath`, `CustomArmRockTexturePath`, `CustomArmPaperTexturePath`, `CustomArmScissorsTexturePath`, `CustomYummyCookie` (`RelicIconData?`), `CustomCharacterSelectBg`, `CustomCharacterSelectIconPath`, `CustomCharacterSelectLockedIconPath`, `CustomCharacterSelectTransitionPath`, `CustomMapMarkerPath`, `CustomAttackSfx`, `CustomCastSfx`, `CustomDeathSfx` — all `public virtual string? ... => null` (`dll :21-62`). Flags: `HideFromVanillaCharacterSelect`, `AllowInVanillaRandomCharacterSelect` (inverted), `HideInCompendium` (`dll :19-21`). Engine-backed defaults: `StartingGold => 99`, `AttackAnimDelay => 0.15f`, `CastAnimDelay => 0.25f`, `UnlocksAfterRunAs => null`, `DeathAnimTime => 1.5f` (`dll :63-69`).

Visuals: `public virtual NCreatureVisuals? CreateCustomVisuals() => null` (return non-null to bypass the path pipeline); `public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null`; `public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, string? deadName = null, bool deadLoop = false, string? hitName = null, bool hitLoop = false, string? attackName = null, bool attackLoop = false, string? castName = null, bool castLoop = false, string? relaxedName = null, bool relaxedLoop = true)` (`dll :71-119`). `public void RegisterSceneConversions()` registers `CustomVisualPath`, `CustomRestSiteAnimPath`, `CustomMerchantAnimPath`, `CustomEnergyCounterPath` for auto-conversion (`dll :121-126`).
**Drift (SOURCE-ONLY):** `DefaultCompendiumOpenModelId` exists in source (`src :58`) but **not** in the shipped binary.

### 2.9 `PlaceholderCharacterModel` — SHIPPED
`public abstract class PlaceholderCharacterModel : CustomCharacterModel` (`dll BaseLib.Abstracts/PlaceholderCharacterModel.cs:11`; docs `src Abstracts/PlaceholderCharacterModel.cs:8`)

A table of redirections keyed on `public virtual string PlaceholderID => "ironclad"` — every member points at a shipped character's asset path: `CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + PlaceholderID)`, `CustomTrailPath => "vfx/card_trail_" + PlaceholderID`, `CustomMapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_<id>.png")`, `CustomIconPath => SceneHelper.GetScenePath("ui/character_icons/<id>_icon")`, `CustomIconTexturePath`/`CustomIconOutlineTexturePath` (top_panel pngs), `CustomEnergyCounterPath => "combat/energy_counters/<id>_energy_counter"`, `CustomRestSiteAnimPath`, `CustomMerchantAnimPath`, arm textures (`ui/hands/multiplayer_hand_<id>_{point,rock,paper,scissors}.png`), `CustomCharacterSelectBg`, `CustomCharacterSelectTransitionPath => "res://materials/transitions/<id>_transition_mat.tres"`, select icons (`packed/character_select/char_select_<id>[_locked].png`), `CharacterSelectSfx`/`CharacterTransitionSfx`/`CustomAttackSfx`/`CustomCastSfx`/`CustomDeathSfx` (FMOD `event:/sfx/...` paths), and `GetArchitectAttackVfx()` returning 5 vanilla vfx paths (`dll :13-68`). All 23 members are `virtual` — override only what you need.
**Use:** our `Character/Defect.cs`, `Silent.cs`, `Watcher.cs` derive from it (`PlaceholderID => "defect"` etc.), overriding only `NameColor`, `Gender`, `StartingHp`, `StartingDeck`, `StartingRelics`, `CardPool`.

### 2.10 `CustomCharacterSelectEntry` — SHIPPED
`public abstract class CustomCharacterSelectEntry : ICustomModel` (`dll BaseLib.Abstracts/CustomCharacterSelectEntry.cs:13`; docs `src Abstracts/CustomCharacterSelectEntry.cs:15`)

A dungeon-selector entry (button) on the character select screen. Auto-registers in the protected constructor (`CustomCharacterSelectEntryRegistry.Register(this)`); the registry is `internal static` with a `List<CustomCharacterSelectEntry> Entries` sorted by `SortOrder` then `EntryId` (`dll CustomCharacterSelectEntryRegistry.cs:8-25`).

- **MUST:** `public abstract string ButtonIconPath { get; }`
- Virtual: `EntryId` (`StringHelper.Slugify(GetType().FullName)`), `EntryTitle` (`GetType().Name`), `EntryDescription` (empty), `SortOrder` (0), `VisibleInCharacterSelect` (true), `AvailabilitySourceCharacter` (`CharacterModel?`, null), `UnlockedInCharacterSelect` (delegates to `CustomCharacterSelectEntryAvailability.IsUnlocked` → `SaveManager.Instance.GenerateUnlockStateFromProgress().Characters.Contains(character)`), `InitialCharacter` (`CharacterModel?`), `ShowVanillaInfoPanelWhenUnresolved`/`ShowVanillaInfoPanelWhenResolved` (true), `LockedTitle`/`LockedDescription`, `CharacterSelectScenePath`/`CharacterSelectForegroundScenePath` (`string?`, null), `CreateCharacterSelectScene()`/`CreateCharacterSelectForegroundScene()` (load+instantiate the paths; throw if path null), `RegisterScene(Control root, CustomCharacterSelectContext context)`, `RegisterForegroundScene(Control root, CustomCharacterSelectContext context)` (`dll :14-96`).

**`CustomCharacterSelectContext`** (sealed, `dll CustomCharacterSelectContext.cs:13-52`): `Entry`, `Screen` (`NCharacterSelectScreen`), **`Lobby => Screen.Lobby` (`StartRunLobby` — the co-op seam)**, `SceneRoot`, `ForegroundSceneRoot`, `SelectedCharacter`, `VanillaInfoPanelVisible`; methods `SetCharacter(CharacterModel?)`, `ClearCharacter()`, `SetVanillaInfoPanelVisible(bool)`. Wired by `Patches/UI/CustomCharacterSelectEntryPatch` and `BaseLibScenes/NCustomCharacterSelectEntryButton` (implements `ICharacterSelectButtonDelegate`, exposes `Lobby`).
**Use:** this is the primitive for our StS1 dungeon selector (M3) — an arbitrary-UI entry with co-op `StartRunLobby` access; `VisibleInCharacterSelect`/`UnlockedInCharacterSelect` gate visibility, `InitialCharacter` can preselect the character.
**Note:** `CustomCharacterSelectEntryAvailability` is SHIPPED (public static `IsUnlocked`).

### 2.11 `CustomMonsterModel` — SHIPPED
`public abstract class CustomMonsterModel : MonsterModel, ICustomModel, ISceneConversions` (`dll BaseLib.Abstracts/CustomMonsterModel.cs:14`; docs `src Abstracts/CustomMonsterModel.cs:11`)

Constructor `public CustomMonsterModel()` → `CustomContentDictionary.RegisterType(GetType())` (no pool registration — engine reflection sweep finds it, §1.3). Members:
- `public virtual string? CustomVisualPath => null` — default convention `res://scenes/creature_visuals/<modname>-<class_name>.tscn` (`src :19-21`)
- `public virtual string? CustomAttackSfx / CustomCastSfx / CustomDeathSfx => null` — override because vanilla FMOD paths derive from the model Id and won't resolve for modded ids; or set `HasDeathSfx => false`
- `public virtual NCreatureVisuals? CreateCustomVisuals() => null` — if overridden, also override `AssetPaths` to drop `VisualPath`
- `public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => null`; `public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, string? deadName = null, bool deadLoop = false, string? hitName = null, bool hitLoop = false, string? attackName = null, bool attackLoop = false, string? castName = null, bool castLoop = false)` (`dll :23-82`)
- `public void RegisterSceneConversions()` — registers `CustomVisualPath` for `NCreatureVisuals` conversion (`dll :84-86`)

**Engine contract** a monster subclass must satisfy (all in `engine MegaCrit.Sts2.Core.Models/MonsterModel.cs`): MUST `MinInitialHp`/`MaxInitialHp` (`:56,58`) and `GenerateMoveStateMachine()` (`:408`); virtual `Title` (`L10NMonsterLookup(Id.Entry + ".name")`, `:54`), `IsHealthBarVisible`, `VisualsPath` (`creature_visuals/<id>`), `AssetPaths`, `AttackSfx`/`CastSfx`/`DeathSfx` (`:151-155`), `HasDeathSfx` (`:157`). `CreateVisuals()` (`:278`) is **not virtual** and falls back to `creature_visuals/fallback` on error (`:280-295`) — a broken visual degrades to the error scene instead of crashing.
**Use:** none of ours yet; `Monsters/MoveBuilder.cs` (§7) is the companion intent-builder.

### 2.12 `CustomEncounterModel` — SHIPPED
`public abstract class CustomEncounterModel : EncounterModel, ICustomModel` (`dll BaseLib.Abstracts/CustomEncounterModel.cs:14`; docs `src Abstracts/CustomEncounterModel.cs:11`)

- `protected CustomEncounterModel(RoomType roomType, bool autoAdd = true)` — warns if `roomType` isn't Monster/Elite/Boss; `AddEncounter(this)` when `autoAdd` (`src :15-28`)
- **MUST:** `public abstract bool IsValidForAct(ActModel act)` — "If making a custom act, you are suggested to add your encounters to the act this way and leave the act's normal encounter list empty" (`src :31-40`)
- `public virtual string? CustomScenePath => null` — an encounter scene is a 1920×1080 `Control`, Full Rect anchors, MouseFilter Ignore, with `Marker2D` children as enemy slots, addressable by name via `CreatureCmd.Add` (`src :63-74`)
- `public override IReadOnlyList<string> Slots` — reads `Marker2D` children from the custom scene (`dll :44-63`)
- `public override bool HasScene` — true if `CustomScenePath` resolves **or** a scene exists at the base-game convention `res://scenes/encounters/<modname>-<encounter_name>.tscn` (`src :94-100`)
- `public virtual string? CustomRunHistoryIconPath / CustomRunHistoryIconOutlinePath => null`
- Backgrounds: `protected bool HasCustomBackground`, `protected void PrepCustomBackground(ActModel parentAct, Rng rng)`, `public virtual BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => null`

**Engine contract** (`engine .../EncounterModel.cs`): MUST `RoomType`, `AllPossibleMonsters`, `GenerateMonsters()`; `HasScene => false` and `Slots => empty` by default, so **no scene is required**. Optional: `IsWeak`, `ShouldGiveRewards`, `MinGoldReward`/`MaxGoldReward`, `Tags`, `FullyCenterPlayers`, `CustomBgm`, `AmbientSfx`, `BossNodePath`/`BossNodeSpineResource`, `ExtraAssetPaths`, `GetCameraScaling()`.

### 2.13 `CustomActModel` — SHIPPED
`public abstract class CustomActModel : ActModel, ICustomModel, ISceneConversions` (`dll BaseLib.Abstracts/CustomActModel.cs:23`; docs `src Abstracts/CustomActModel.cs:22`)

- `protected CustomActModel(int actNumber, bool autoAdd = true)` — `Index = actNumber - 1`; doc: **"Set to -1 to prevent your act from spawning naturally. Otherwise, use 1/2/3 for the corresponding act."** (`src :32-41`)
- **MUST (abstract):** `protected abstract string CustomMapTopBgPath { get; }`, `CustomMapMidBgPath`, `CustomMapBotBgPath`, `CustomRestSiteBackgroundPath` (`dll :157-170`)
- Virtual: `protected virtual string CustomBackgroundScenePath => "res://BaseLib/scenes/dynamic_background.tscn"`; `public virtual string? CustomChestScene => null` (custom treasure-chest scene, see `NCustomTreasureRoomChest` §7); `protected virtual ActMap? CustomCreateMap(RunState runState, bool replaceTreasureWithElites) => null`; `protected virtual BackgroundAssets CustomGenerateBackgroundAssets(Rng rng)` (default `new BackgroundAssets("glory", rng)`); `public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState)`, `public override bool IsUnlocked(UnlockState) => true`, `protected override void ApplyActDiscoveryOrderModifications(UnlockState)`, `public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)` (`dll :184-246`)
- Defaults mirror Act 3: colors (`MapTraveledColor "27221C"`, `MapUntraveledColor "6E7750"`, `MapBgColor "9B9562"`), `BgMusicOptions`/`MusicBankPaths` (act3), `AmbientSfx`, `ChestSpineResourcePath`/`ChestSpineSkinNameNormal`/`ChestSpineSkinNameStroke`/`ChestOpenSfx`, `IsDefault => false`; `AllAncients`/`BaseNumberOfRooms` switch on `Index` (0→15, 1→14, 2→13 rooms; non-basegame index throws on `AllAncients`, override it) (`dll :89-156`)
- `[Obsolete] public int ActNumber => this.ActNumber()` — use `Index` (0-based) (`dll :83-86`)
- `public void RegisterSceneConversions()` — registers `CustomChestScene` (`dll :336-338`)
- Registration: `CustomContentDictionary.AddAct`; `ModelDbCustomActsPatch` reorders `ModelDb.Acts` with `.ThenByDescending(act => act.IsDefault)` (`src Patches/Content/ContentPatches.cs:315-335`), and `AddActContent.Patch` runs in `PostModInitPatch` to catch modded acts (`src :348-350`).
- No act-*sequencing* API exists (no next-act/order hooks) — driving a 4-act progression requires your own patch (`prior-audit §8`).
**Use:** our StS1 dungeon (M3) builds on this — one `CustomActModel` per act with `actNumber = -1` for acts that only spawn through our own sequencing, plus `IsValidForAct`-gated `CustomEncounterModel`s (§2.12).

### 2.14 `CustomOrbModel` — SHIPPED
`public abstract class CustomOrbModel : OrbModel, ICustomModel, ILocalizationProvider` (`dll BaseLib.Abstracts/CustomOrbModel.cs:12`)

- Constructor registers into `internal static readonly List<CustomOrbModel> RegisteredOrbs` (`dll :15,58-60`)
- `public virtual string? CustomIconPath => null`, `public virtual string? CustomSpritePath => null`, `public virtual bool IncludeInRandomPool => false`, `public virtual string? CustomPassiveSfx / CustomEvokeSfx / CustomChannelSfx => null` (the three `protected override string PassiveSfx/EvokeSfx/ChannelSfx` getters fall back to base when null), `public virtual List<(string,string)>? Localization => null`, `public virtual Node2D? CreateCustomSprite() => null`
- Backed by patches `CustomOrbIconPath` / `CustomOrbSpritePath` / `CustomOrbCreateSprite` / `CustomOrbRandomPool` (`dll` file names).

### 2.15 `CustomPetModel` — SHIPPED
`public abstract class CustomPetModel : PetModel, ICustomModel` (`dll BaseLib.Abstracts/CustomPetModel.cs`). Constructor calls `CustomContentDictionary.RegisterType(GetType())` (engine reflection sweep finds it). Same visual members as monsters: `CustomVisualPath`, `CreateCustomVisuals()`, `SetupCustomAnimationStates(MegaSprite)`, plus `RegisterSceneConversions()` for `NCreatureVisuals`.

### 2.16 Pool models — SHIPPED
All three pool abstracts share the same shape (`dll BaseLib.Abstracts/CustomCardPoolModel.cs`, `CustomRelicPoolModel.cs`, `CustomPotionPoolModel.cs`):

```csharp
public abstract class CustomCardPoolModel : CardPoolModel, ICustomModel, ICustomEnergyIconPool
// CustomRelicPoolModel : RelicPoolModel, ICustomModel, ICustomEnergyIconPool
// CustomPotionPoolModel : PotionPoolModel, ICustomModel, ICustomEnergyIconPool
```

- `public virtual bool IsShared => false` — when true, constructor calls `ModelDbShared{Card,Relic,Potion}PoolsPatch.Register(this)` (registers into the engine's shared-pool lists, patched getters `ModelDb.AllSharedCardPools` etc.)
- `public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id)`
- `public virtual string? BigEnergyIconPath => null` / `public virtual string? TextEnergyIconPath => null` (the `ICustomEnergyIconPool` members, wired by `CustomEnergyIconPatches`)
- `public virtual bool SeenByDefault => false`
- Card pool extra: `public override string CardFrameMaterialPath => "card_frame_red"`, `public virtual Color ShaderColor => new Color("FFFFFF")` with `H`/`S`/`V` getters, `public virtual Texture2D? CustomFrame(CustomCardModel card) => null`
- The `GenerateAll*` methods return empty by default: `protected override CardModel[] GenerateAllCards()` / `IEnumerable<RelicModel> GenerateAllRelics()` / `IEnumerable<PotionModel> GenerateAllPotions()` — subclass overrides supply content
- `[Pool]` attribute (`Utils/PoolAttribute.cs`) is required on the pool-derived classes for `AddModel` validation (`src Patches/Content/ContentPatches.cs:55-68`)

**Use:** our `Character/DefectCardPool.cs` (`CustomCardPoolModel` with `EnergyColorName => "defect"`, `CardFrameMaterialPath => "card_frame_blue"`, `DeckEntryCardColor`), plus per-character relic/potion pools in `Character/`.

### 2.17 `CustomEventModel` — SHIPPED
`public abstract class CustomEventModel : EventModel, ICustomModel, ILocalizationProvider` (`dll BaseLib.Abstracts/CustomEventModel.cs:15`)

- `public CustomEventModel(bool autoAdd = true)` — `CustomContentDictionary.AddEvent(this)`; `ActCustomEvents` vs `SharedCustomEvents` split by `Acts` (shared events check `runState.CurrentActIndex`, `src :19`)
- `public virtual ActModel[] Acts => Array.Empty<ActModel>()` — empty = shared/any act
- `public virtual string? CustomInitialPortraitPath => null`, `CustomBackgroundScenePath => null`, `CustomVfxPath => null`
- `public virtual List<(string,string)>? Localization => null`
- Option helpers (protected): `Option(Func<Task>? onChosen, LocString title, LocString description, params IHoverTip[] tips)`, `Option(Func<Task>? onChosen, string pageKey = "INITIAL", params IHoverTip[] tips)`, `Option(Func<Task>? onChosen, IEnumerable<IHoverTip> tips, string pageKey = "INITIAL")` — the latter two derive the loc key from the delegate method name (`...pages.<pageKey>.options.<slugified-method-name>`); `LockedOption(string locKey, string pageKey = "INITIAL", params IHoverTip[] tips)`; `PageDescription(string pageKey)` → `L10NLookup(Id.Entry + ".pages." + pageKey + ".description")` (`dll :36-96`).

### 2.18 `CustomRestSiteOption` — SHIPPED
`public abstract class CustomRestSiteOption : RestSiteOption` (`dll BaseLib.Abstracts/CustomRestSiteOption.cs:8`)

- `protected CustomRestSiteOption(Player owner) : base(owner)`
- `public virtual string? CustomIconPath => null` (prefix on the icon path getter)

Engine dispatch: `RestSiteOption.Generate(Player)` builds Heal/Smith (+Mend in multiplayer) then calls `Hook.ModifyRestSiteOptions(player.RunState, player, list2)` (`engine .../RestSiteOption.cs:53-74`); relics hook it via `RelicModel.TryModifyRestSiteOptions(Player, ICollection<RestSiteOption>)` (`engine .../Girya.cs:59-71`). StS2 ships `LiftRestSiteOption` hard-bound to vanilla `Girya` (increments `TimesLifted`, `:41`) — not reusable for our own relic (`prior-audit §4`).

### 2.19 `CustomReward` — SHIPPED
`public abstract class CustomReward : Reward` (`dll BaseLib.Abstracts/CustomReward.cs:11`; docs `src Abstracts/CustomReward.cs`)

- `public override int RewardsSetIndex => 9`
- **MUST:** `public abstract CreateRewardFromSave<CustomReward> DeserializeMethod { get; }` where `public delegate T CreateRewardFromSave<out T>(SerializableReward save, Player player) where T : CustomReward` (`src :18`)
- `protected CustomReward(Player player) : base(player)`
- `public LocString GetLoc()` → `new LocString("gameplay_ui", type.GetPrefix() + StringHelper.Slugify(type.Name))` (`dll :27-31`)
- `public virtual void Initialize()` — validates `DeserializeMethod` is static, then `CustomRewardPatches.RegisterCustomReward(RewardType, DeserializeMethod)` (`dll :33-41`); call it from the constructor of the concrete class
- Shipped concrete rewards (`BaseLib.Common.Rewards`, all SHIPPED): `CardTransformReward` (`RewardType CardTransform`, `required bool Upgrade`, `required int Amount`), `CardUpgradeReward` (`RewardType CardUpgrade`, `RewardsSetIndex => 8`), `RandomCardUpgradeReward` (`RewardsSetIndex => 8`) — each with `CreateFromSerializable(SerializableReward, Player)` and `[CustomEnum] public static RewardType X` fields; extensions `RewardSynchronizer.DoCardUpgrade(Player, int amount = 1)` / `DoUnsyncedCardTransform(Player, int amount = 1, bool upgrade = false)`.

### 2.20 `CustomEnchantmentModel` — SHIPPED
`public abstract class CustomEnchantmentModel : EnchantmentModel, ICustomModel` (`dll BaseLib.Abstracts/CustomEnchantmentModel.cs:10`) — `protected virtual string? CustomIconPath => null` (prefix on the icon getter).

### 2.21 `CustomModifierModel` — SHIPPED
`public abstract class CustomModifierModel : ModifierModel, ICustomModel` (`dll BaseLib.Abstracts/CustomModifierModel.cs:11`)

- **MUST:** `public abstract ModifierAlignment Alignment { get; }` (`enum ModifierAlignment { None, Good, Bad }`, `dll ModifierAlignment.cs`)
- `public virtual IEnumerable<ModifierModel> MutuallyExclusiveGroup => Array.Empty<ModifierModel>()`
- `public virtual int SortOrder => 0`

### 2.22 `CustomAncientModel` — SHIPPED
`public abstract class CustomAncientModel : AncientEventModel, ICustomModel, ILocalizationProvider` (`dll BaseLib.Abstracts/CustomAncientModel.cs:20`; docs `src Abstracts/CustomAncientModel.cs`)

- `public CustomAncientModel(bool autoAdd = true, bool logDialogueLoad = false)` — `AddAncient(this)` when `autoAdd`
- **MUST:** `protected abstract OptionPools MakeOptionPools { get; }` (public `OptionPools` getter memoizes it, `dll :29-37`)
- `public override IEnumerable<EventOption> AllPossibleOptions` — derived from pools via `RelicOption(relic, "INITIAL", null)` (`dll :39-41`)
- `public virtual string? CustomScenePath / CustomMapIconPath / CustomMapIconOutlinePath / CustomRunHistoryIconPath / CustomRunHistoryIconOutlinePath => null`
- `public virtual bool IsValidForAct(ActModel act) => true`; `public virtual bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) => false`
- Statics: `MakePool(params RelicModel[] options)` / `MakePool(params AncientOption[] options)` → `WeightedList<AncientOption>`; `AncientOption<T>(int weight = 1, Func<T, RelicModel>? relicPrep = null, Func<T, IEnumerable<RelicModel>>? makeAllVariants = null) where T : RelicModel` (`dll :60-72`)
- Dialogues: `protected override AncientDialogueSet DefineDialogues()`; `GetAssetPaths(IRunState)`; helper `Utils/AncientDialogueUtil.cs` (`BaseLocKey(ancientId, charId)`, `SfxPath`, `GetDialoguesForKey`).

### 2.23 `CustomBadge` — SHIPPED
`public abstract class CustomBadge(bool requiresWin, bool multiplayerOnly)` (`dll BaseLib.Abstracts/CustomBadge.cs:15`; docs `src :10-11` "does not actually inherit from Badge"; no-parameter constructor required on subclasses)

- `public readonly bool RequiresWin;` / `public readonly bool MultiplayerOnly;`
- `public virtual string Id => GetType().GetPrefix() + GetType().Name.ToSnakeCase().ToUpperInvariant()`
- `public virtual string? CustomBadgeIconPath => null` (stored into `public static readonly SpireField<Badge, string?> CustomBadgeIconPathDict`)
- **MUST:** `public abstract BadgeRarity Rarity(SerializableRun run, SerializablePlayer player);` and `public abstract bool IsObtained(SerializableRun run, SerializablePlayer player);`
- `public Badge ToRealBadge(SerializableRun run, bool won, ulong playerId)` — code-generates a real `Badge` subclass via `ModuleBuilder` (main-branch vs beta constructor handling)

### 2.24 `CustomPile` — SHIPPED
`public abstract class CustomPile : CardPile` (`dll BaseLib.Abstracts/CustomPile.cs:11`; docs `src Abstracts/CustomPile.cs`)

- `public CustomPile(PileType pileType) : base(pileType)`
- `public virtual string? IconPath => null`; `public virtual LocString? Name => null`; `public virtual bool NeedsCustomTransitionVisual => false`
- **MUST:** `public abstract bool CardShouldBeVisible(CardModel card);` and `public abstract Vector2 GetTargetPosition(CardModel model, Vector2 size);`
- `public virtual NCard? GetNCard(CardModel card) => null`; `public virtual bool CustomTween(Tween tween, CardModel card, NCard cardNode, CardPile oldPile) => false`

The pile system is patched across `PileTypeExtensions.IsCombatPile`/`GetTargetPosition`, `GetCombatPile`, `GetNCardPile`, `GetPilePosition`, `TheBigPatchToCardPileCmdAdd`, `SpecialPileInCombat` (`Baselib.Patches.Content/`, SHIPPED).

### 2.25 `CustomSingletonModel` — SHIPPED
`public abstract class CustomSingletonModel : SingletonModel, ICustomModel` (`dll BaseLib.Abstracts/CustomSingletonModel.cs:15`)

A hook listener that is neither card, relic nor power — ideal for global rules.

- `public enum HookType { None, Combat, Run }`
- `public CustomSingletonModel(HookType hookType)` — `Combat` → `ShouldReceiveCombatHooks = true` + `ModHelper.SubscribeForCombatStateHooks(Id.Entry, delegate => [this])`; `Run` → `SubscribeForRunStateHooks` (`dll :22-43`)
- `[Obsolete] public CustomSingletonModel(bool receiveCombatHooks, bool receiveRunHooks)` — use `HookType` ("a singleton receiving both types of hooks will receive some hooks twice")
- `public override bool ShouldReceiveCombatHooks { get; }`
**Use:** ideal host for global StS1 rules that are neither card/relic/power — a combat-scoped singleton receives every `AbstractModel` hook (§6.2) with no owner constraints.

### 2.26 Custom messages — wrappers SHIPPED, base classes SOURCE-ONLY
What ships (`dll BaseLib.Abstracts/CustomMessageWrapper.cs`, `CustomTargetedMessageWrapper.cs`):

```csharp
public sealed class CustomMessageWrapper : INetMessage, IPacketSerializable
{ public required ICustomMessage Message; public static byte WrapperMessageId { get; set; }
  public static void Initialize(); public static void Send(ICustomMessage msg, INetGameService? netService = null); ... }
public sealed class CustomTargetedMessageWrapper : IRunLocationTargetedMessage, INetMessage, IPacketSerializable
{ public required ICustomTargetedMessage Message; public static byte WrapperMessageId { get; set; }
  public static void Initialize(); public static void Send(ICustomTargetedMessage msg, INetGameService? netService = null); ... }
```

The interfaces a mod implements (`SHIPPED`, `dll ICustomMessage.cs` / `ICustomTargetedMessage.cs`):
```csharp
public interface ICustomMessage : IPacketSerializable
{ bool ShouldBroadcast { get; }  bool ShouldBuffer => true;  NetTransferMode Mode => (NetTransferMode)2;
  LogLevel LogLevel => (LogLevel)0;  void HandleMessage(ulong senderId); }
public interface ICustomTargetedMessage : IPacketSerializable
{ bool IsRewardMessage { get; }  RunLocation Location { get; }  bool ShouldBroadcast { get; }
  bool ShouldBuffer => true;  NetTransferMode Mode => (NetTransferMode)2;  LogLevel LogLevel => (LogLevel)0;
  void HandleMessage(ulong senderId); }
```
The abstract convenience classes `CustomMessage`/`CustomTargetedMessage` listed in some v3.4.5 material do **not** exist in the shipped binary (nor as classes in the v3.4.5 tree — only the wrappers + interfaces). Type mapping and registration: `CustomMessageWrapper.Initialize()` (called from `PostModInitPatch.EarlyPostInit`), per-buffer `Register`/`Unregister` from `Patches/Networking/CustomMessagePatches.cs` (SHIPPED as `AdjustCustomMessageKeys` + handlers).

### 2.27 `CardModifier` — SHIPPED
`public abstract class CardModifier : AbstractModel, IComparable<CardModifier>` (`dll BaseLib.Abstracts/CardModifier.cs:30`)

Cross-cutting per-card modification (not per-card state — that's `[SavedProperty]`). Core:
- `public int Amount { get; set; }` (setter asserts mutable), `public CardModel? Owner { get; private set; }`, `public int Priority { get; set; }` (sort order), `public DynamicVarSet DynamicVars` (built from `protected virtual IEnumerable<DynamicVar> CanonicalVars`)
- `public override bool ShouldReceiveCombatHooks` — follows `Owner`
- **MUST:** none — all hook methods are virtual: `StoreSaveData(ModifierSave)`/`LoadSaveData(ModifierSave)`, `ApplyStacked(CardModifier newApplied) => false`, `GetLoc(string subKey = "description")`, `ModifyDescription(Creature? target, ref string description)`, `ModifyDescriptionPost(...)`, `AddTips(List<IHoverTip>)`, `OnInitialApplication()`, `OnUpgrade()`, `OnDowngrade()`, `UpdateDynamicVarPreview(...)`, `AfterClonedOnCard(CardModel)`, `ModifyBaseDamageAdditive(decimal, ValueProp) => 0m`, `ModifyBaseDamageMultiplicative(decimal, ValueProp) => 1m`, `ModifyBaseBlockAdditive(decimal) => 0m`, `ModifyBaseBlockMultiplicative(decimal) => 1m`, `OnPlay(PlayerChoiceContext, CardPlay) => Task.CompletedTask` (`dll :90-279`)
- Statics: `Modifiers(CardModel)` (read-only), `DirectModifiers(CardModel)`, `AddModifier<T>(CardModel)`, `AddModifier<T>(CardModel, int amount)`, `AddModifier(CardModel, CardModifier)`, `RemoveModifier(CardModel, CardModifier)`, `Get<T>()` (`ModelDbExtensions.CardModifier<T>()`) (`dll :135-171`)
- `sealed class ModifierSave : IPacketSerializable` — `Id`, `Amount`, `IntProperties`, `AdditionalProperties`, `FromModifier`/`ToRealMod` — persists via `RegisterSave()` (`dll :31-88`), wired into `DescriptionOverrides.CustomizeDescription` and combat-hook subscription (`dll :172-226`)
- Extension accessors: `CardExtensions.AddModifier`/`GetModifiers`/`GetModifier<T>`/`TryGetModifier<T>`/`GetModifier(ModelId)`/`TryGetModifier(ModelId)` (`dll BaseLib.Extensions/CardExtensions.cs:60-109`)

### 2.28 `CustomResource` — **SOURCE-ONLY** (whole system)
The entire custom-resource (non-energy card cost) system exists only in v3.4.5 source (`src Abstracts/CustomResource.cs`, 1211 lines) and is **absent from the shipped v3.3.5 binary**. Types: `CustomResource`, `BasicCustomResource`, `CustomResources<T>`, `CustomResourceCost<T>`, `ICustomResourceCost`, `ICustomCostVisualsHandler`, `ICustomResourceVisualsHandler`, `BasicCostVisualsHandler`, `BasicResourceVisualsHandler`, `ResourceHandler`, `CustomResourcePatches`, `CustomResourceUiPatches` (+ `Hooks/CustomResourceHooks.cs`: `IModifyResourceCostInCombat<T>`, `IAfterSpendResource<T>`). Registration happens in `PostModInitPatch.EarlyPostInit` via reflection (`CustomResources<T>.Register`). For completeness, the abstract contract (`src :1030-1166`):

```csharp
public abstract class CustomResource(string id)
{
  public event Action<int, int>? AmountChanged;
  public string Id { get; protected set; } = id;
  public abstract ICustomCostVisualsHandler? CostVisualsHandler();
  public abstract ICustomResourceVisualsHandler? ResourceVisualsHandler();
  public virtual bool ApplySharedModification => true;
  public virtual bool IsDefaultOptional => false;
  public virtual void PrepForCombat(PlayerCombatState playerCombatState) { }
  public virtual int Amount { get; set; }
  public virtual void StartOfTurnReset(PlayerCombatState playerCombatState, ICombatState combatState) { }
  public virtual async Task<bool> Spend<T>(ICombatState combatState, AbstractModel? spender, int amount, bool optional) where T : CustomResource { ... }
  public void ModifyAmount(int change) => Amount += change;
  public virtual UnplayableReason UnplayableReason => UnplayableReason.EnergyCostTooHigh;
  public virtual bool CanAfford(CardModel card, int cost) => Amount >= cost;
}
```
**Do not build against it on v3.3.5.** (In v3.4.5 it patches `PlayerCombatState.AfterCombatEnd`, `HasEnoughResourcesFor`, `CardModel.SpendResources` (async transpiler via `AsyncMethodCall`), `CardPlay.Card` setter, `CardEnergyCost.AfterCardPlayedCleanup`, `CardModel.EndOfTurnCleanup`/`SetToFreeThisCombat`/`SetToFreeThisTurn`, `BeforeEnergyResetHook`.)

---

## 3. Interfaces

Namespace `BaseLib.Abstracts` unless noted. All SHIPPED unless flagged.

### 3.1 `ICustomModel` — SHIPPED (`dll BaseLib.Abstracts/ICustomModel.cs`)
`public interface ICustomModel { }` — empty marker. Its real effect is in `PrefixIdPatch`: any type assignable to `ICustomModel` gets the `<ROOTNAMESPACE>-` id prefix at `ModelDb.GetEntry` (§1.2). Every `Custom*Model` abstract implements it.

### 3.2 `ICustomPower` — SHIPPED (`dll ICustomPower.cs`)
```csharp
public interface ICustomPower : ICustomModel
{ string? CustomPackedIconPath => null;  string? CustomBigIconPath => null;  string? CustomBigBetaIconPath => null; }
```
Implementing it (or extending `CustomPowerModel`, which implements it) makes the engine read those three virtual icon paths for a power's packed/big/beta icons instead of the vanilla convention.

### 3.3 `ILocalizationProvider` — SHIPPED (`dll ILocalizationProvider.cs`)
```csharp
public interface ILocalizationProvider
{ string? LocTable => null;  List<(string, string)>? Localization { get; } }
```
Implementing it makes `ModelLocPatch` (postfix on `ModelDb.Init`) write your `Localization` pairs into the loc table chosen by `LocTable` (or the category default — §5) under keys `{Id.Entry}.{subkey}`, each value passed through `SimpleLoc.TrySimplify` (`src Patches/Localization/ModelLocPatch.cs:33-57`). Note `CustomMonsterModel` does **not** implement it — a monster subclass must add it itself (`prior-audit §7`).

### 3.4 `ICustomEnergyIconPool` — SHIPPED (`dll ICustomEnergyIconPool.cs`)
```csharp
public interface ICustomEnergyIconPool { string? BigEnergyIconPath { get; }  string? TextEnergyIconPath { get; } }
```
Implemented by `CustomCardPoolModel`/`CustomRelicPoolModel`/`CustomPotionPoolModel`. `CustomEnergyIconPatches` (`dll BaseLib.Patches.UI/CustomEnergyIconPatches.cs:12-63`) prefixes `EnergyIconHelper.GetPath` and the `EnergyIconsFormatter` formatter (delimiter `'∴'`), so a pool can supply custom energy-icon art.

### 3.5 `ISceneConversions` — SHIPPED (`dll ISceneConversions.cs`)
`public interface ISceneConversions { void RegisterSceneConversions(); }` — called on every implementer by `PostModInitPatch.RegisterSceneConversions` (prefix on `ModelDb.Preload`, §1.4). Implementations register `CustomVisualPath`-style properties with `RegisterSceneForConversion<TNode>` so the scene auto-conversion system knows them (§4).

### 3.6 `IAutoRegisterFormatSpecifier` — SHIPPED (`dll IAutoRegisterFormatSpecifier.cs`)
`public interface IAutoRegisterFormatSpecifier : IFormatter { }` — implement `IFormatter` (SmartFormat) on a class with a parameterless constructor; `PostModInitPatch.EarlyPostInit` instantiates it and adds it to `LocManager._smartFormatter` after `LoadLocFormatters` (`src Patches/PostModInitPatch.cs:118-138`). This is how custom `{...}` format specifiers become available in all loc strings.

### 3.7 `IHasSecondAmount` — SHIPPED (`dll IHasSecondAmount.cs`)
`public interface IHasSecondAmount { string GetSecondAmount(); }` — a second displayed number on a power (e.g. StS2 two-amount powers); `PowerExtensions.InvokeSecondAmountChanged` + `TwoAmountPowers` patch make the UI refresh it (`dll BaseLib.Extensions/PowerExtensions.cs`, `Baselib.Patches.Utils/TwoAmountPowers.cs`).

### 3.8 `ITomeCard` — SHIPPED (`dll ITomeCard.cs`)
```csharp
public interface ITomeCard
{ CharacterModel TomeCharacter { get { ... } } }  // default impl: first character whose CardPool contains this card; throws otherwise
```
Default implementation scans `ModelDb.AllCharacters` for the owner character; override it for cards usable by a custom character.

### 3.9 `ITranscendenceCard` — SHIPPED (`dll ITranscendenceCard.cs`)
`public interface ITranscendenceCard { CardModel GetTranscendenceTransformedCard(); }` — the card a Transcendence transformation turns this card into; consumed by `ArchaicToothTranscendenceUpgradesPatch`/`DustyTomeCardPatch` (§8).

### 3.10 `ITrashHeapCard` / `ITrashHeapRelic` — SHIPPED (`dll ITrashHeapCard.cs`, `ITrashHeapRelic.cs`)
Empty marker interfaces. Implementing them opts the card/relic into the **Trash Heap** mechanic (`TrashHeapCardsPatch`/`TrashHeapRelicsPatch` prefix `TrashHeap.Cards`/`TrashHeap.Relics`, `Baselib.Patches.Content/`, SHIPPED) — the modded-content trash/removal UI lists them.

### 3.11 Custom-type-text trio — **SOURCE-ONLY**
- `ICustomTypeTextCard` (`src Abstracts/ICustomTypeTextCard.cs`) — **not in shipped v3.3.5** (empty stub file in the decompile proves absence).
- `ICardTypeTextModifier` (`src Hooks/ICardTypeTextModifier.cs:11-20`) — `public IEnumerable<LocString> GetTypeModifiers(CardModel card);` each string gets a `{Type}` format argument; intended for non-card models that modify card type text. **SOURCE-ONLY.**
- These are driven by `BaseLibHooks` / `HookUtils` (§6), also source-only.

### 3.12 Message interfaces — SHIPPED
`ICustomMessage` / `ICustomTargetedMessage` (signatures in §2.26). Implementing them makes a class serializable and sendable over the run's net service through the `CustomMessageWrapper`/`CustomTargetedMessageWrapper`; `HandleMessage(ulong senderId)` is invoked on the receiving side.

### 3.13 Helper interfaces on the utilities side — SHIPPED
- `IMatcher` (`dll BaseLib.Utils.Patching/IMatcher.cs`) — IL pattern matching (§7).
- `ICloneableField`, `ISavedSpireField`, `IAddedNodes`, `IWeighted` (`dll BaseLib.Utils/`) — implemented by the `SpireField` family, `AddedNode`, and `WeightedList`/`AncientOption` respectively (§7).
- `IAddDumbVariablesToPowerDescription`, `IBetaCompatTempPower` — SHIPPED (`dll BaseLib.Abstracts/`); implemented by `CustomTemporaryPowerModel` for description vars and beta-branch compat.

---

## 4. Visuals and assets

### 4.1 Path helpers — ENGINE, used pervasively by BaseLib

```csharp
// ENGINE (MegaCrit.Sts2.Core.Helpers)
public static class SceneHelper
{ public static string GetScenePath(string innerPath)   // "res://scenes/" + innerPath + ".tscn"; leading '/' stripped
  private static PackedScene Load(string innerPath)
  public static T Instantiate<T>(string innerPath) where T : Node }
public static class ImageHelper
{ public static string GetImagePath(string innerPath)   // "res://images/" + innerPath; leading '/' stripped
  public static string? GetRoomIconPath(MapPointType mapPointType, RoomType roomType, ModelId? modelId)
  public static string? GetRoomIconOutlinePath(MapPointType mapPointType, RoomType roomType, ModelId? modelId) }
```
(`engine MegaCrit.Sts2.Core.Helpers/SceneHelper.cs:11-58`, `ImageHelper.cs:12-105`)

**Key point:** `GetScenePath` resolves against the **base game's** `res://scenes/...` — it is exactly what lets `PlaceholderCharacterModel` reuse a shipped scene (`SceneHelper.GetScenePath("creature_visuals/" + PlaceholderID)`). A mod's own scenes live under its own root `res://<ModId>/scenes/...` and are **not** fed through `SceneHelper`; the mod supplies the full path directly (e.g. `CustomVisualPath => "res://Spire1/scenes/my_creature.tscn"`).

BaseLib's own extension (SHIPPED, `dll BaseLib.Extensions/ImageHelperExtensions.cs:19-21`):
```csharp
public static string GetModImagePath(string innerPath, Type? type = null)
// => Path.Join("res://" + (type?.GetRootNamespace() ?? Assembly.GetCallingAssembly().GetName().Name), "images", innerPath)
```
This is a direct replacement for our hand-rolled `mod/Spire1Code/Extensions/StringExtensions.cs:8-88` (nine `ImagePath()`/`...ImagePath()` methods building `Path.Join(MainFile.ResPath, "images", ...)` by hand). BaseLib's version also logs nothing on missing files, whereas ours logs and falls back to placeholder pngs — the fallback behavior is the only reason to keep ours (`prior-audit §7`).

### 4.2 `NodeFactory` / `NodeFactory<T>` — SHIPPED (`dll BaseLib.Utils.NodeFactories/NodeFactory.cs`)

```csharp
public abstract class NodeFactory
{ public static void Init()   // creates ControlFactory, NCreatureVisualsFactory, NRestSiteCharacterFactory, NMerchantCharacterFactory, NEnergyCounterFactory, NCustomTreasureRoomChestFactory
  public static void RegisterSceneType<TNode>(string scenePath, Action<TNode>? postConversionAction = null) where TNode : Node
  public static void RegisterSceneType<TNode>(string scenePath, (Type, Action<TNode>?) nodeType) where TNode : Node
  protected abstract Node CreateFromNode(Node source); ... }
public abstract class NodeFactory<T> : NodeFactory where T : Node, new()
{ public static T CreateFromResource(object resource)          // string path | Texture2D | ...
  public static T CreateFromScene(string scenePath)
  public static T CreateFromScene(PackedScene scene)
  protected virtual T CreateBareFromResource(object resource)  // throws unless overridden
  protected virtual void ConvertScene(T target, Node? source)
  protected virtual void TransferAndCreateNodes(T target, Node? source)
  protected virtual Node ConvertNodeType(Node node, Type targetType)
  protected abstract void GenerateNode(Node target, INodeInfo required); }
```
`CreateFromResource` also accepts a path string (`ResourceLoader.Load` when it resolves, `dll :258-276`) and **requires the main thread** (`dll :262-268`). `RegisterSceneType` normalizes paths (`res://`/`user://`/`uid://` prefix check, `StringExtensions.SimplifyPath`) and warns on overwrite (`dll :95-121`).
**SOURCE-ONLY drift:** `UnregisterSceneType`, `HasFactory`, `IsRegistered` appear in the v3.4.5 docs (`src docs/auto_conversion.md:98-102`) but are **not in shipped v3.3.5** — only the two `RegisterSceneType` overloads ship.

### 4.3 Auto-conversion — SHIPPED (`dll BaseLib.Patches.UI/SceneConversionPatch.cs:11-26`)

`SceneConversionPatch` is a Harmony postfix on the **non-generic** `PackedScene.Instantiate(GenEditState)` that calls `NodeFactory.TryAutoConvert(scene, ref result)`. Because Godot's generic `Instantiate<T>()` casts *after* the non-generic call returns, a scene whose root is a plain `Node2D`/`Control` can satisfy `Instantiate<NCreatureVisuals>()` when its path is registered (`src docs/auto_conversion.md:14-47`). The public one-liner is `string.RegisterSceneForConversion<TNode>(string scenePath, Action<TNode>? postConversion = null)` (`dll BaseLib.Extensions/StringExtensions.cs:21-26`). `ISceneConversions.RegisterSceneConversions()` implementations call it for each path property (§3.5).

### 4.4 `NCreatureVisualsFactory` — SHIPPED (class is `internal`; reachable via `NodeFactory<NCreatureVisuals>`)

The node contract its constructor requires (`dll BaseLib.Utils.NodeFactories/NCreatureVisualsFactory.cs:12-28`):

| Node name | Type | Required |
|---|---|---|
| `%Visuals` | `Node2D` (or `Sprite2D`) | **YES** — cannot be generated; logs `'Visuals' node must be provided for NCreatureVisuals` |
| `%PhobiaModeVisuals` | `Node2D` | generated if missing |
| `Bounds` | `Control` | fixed path (comment: "Although it will use uniqueName, NCreature requires fixed path"); default 240×280 at (-120,-280) |
| `%CenterPos` | `Marker2D` | default `bounds.Position + bounds.Size*(0.5, 0.6)` |
| `IntentPos` | `Marker2D` | default `bounds.Position + bounds.Size*(0.5, 0) + (0,-70)` |
| `%OrbPos` | `Marker2D` | generated |
| `%TalkPos` | `Marker2D` | generated |
| `%FormVfx` | `Control` | zero-size mouse-ignoring Control moved to child 0 |

**Texture2D route:** `CreateBareFromResource` builds a full node tree from a bare texture: `Bounds` = Control sized `img.GetSize() * 1.1f` at `(-size.X/2, -size.Y)`, `Visuals` = `Sprite2D` with `Texture = img` at `(0, -imgSize.Y * 0.5f)` (`dll :31-45`). So **one PNG per monster/character is enough**: `NodeFactory<NCreatureVisuals>.CreateFromResource(texture2d)`, or set `CustomVisualPath` to a `.tscn` carrying `%Visuals` + `Bounds`.

### 4.5 What a monster or character needs on disk

- **Monster (PNG route):** a `.png` returned from `CreateCustomVisuals()` (or a `.tscn` with `%Visuals` + `Bounds`; `Bounds` needed for click targeting), an HP range, a `GenerateMoveStateMachine()` (build intents with `MoveBuilder`, §7), localization (§5), and sfx overrides (`CustomAttackSfx`/`CustomCastSfx`/`CustomDeathSfx`) because vanilla FMOD paths won't resolve for a modded id (`prior-audit §2`). With a PNG there is no `MegaSprite`, so leave `SetupCustomAnimationStates` null and drive motion with `Utils/CustomAnimation` (`HasCustomAnimation(Node)`, `PlayCustomAnimation(Node, params string[])` — handles `AnimationPlayer`/`AnimationTree`/`AnimatedSprite2D`).
- **Character (Spine route):** shipped StS2 characters are Spine: `<id>.skel` + `<id>.atlas` + `<id>.png` under `res://animations/characters/<id>/`, imported to `.spskel`/`.spatlas`/`.ctex`, with parallel rigs at `animations/character_select/<id>/`, `animations/rest_site/<id>/`, `animations/merchant/<id>/` (`prior-audit §5`). `PlaceholderCharacterModel` sidesteps all of it by borrowing the shipped rigs; full animation parity needs `SetupAnimationState` or a Spine rig.
- **Character (PNG route):** `CustomVisualPath` scene (or `CreateCustomVisuals()` texture) + optional rest-site/merchant single-root-`Sprite2D` scenes + character-select icon, map marker, top-panel icon, multiplayer hand PNGs — every un-overridden surface falls back to the `PlaceholderID` asset (`prior-audit §5`).

### 4.6 Relic art: `RelicImageOverridePatch` / `RelicIconData` — SHIPPED
```csharp
public record RelicIconData(string? BigIconPath, string? PackedIconPath, string? PackedIconOutlinePath);   // dll BaseLib.Patches.UI/RelicImageOverridePatch.cs:15
public static void AddOverride<TRelicType>(RelicIconData data, Func<RelicModel, bool>? condition = null) where TRelicType : RelicModel
```
Three prefixes on `RelicModel.PackedIconPath`/`PackedIconOutlinePath`/`BigIconPath` (`dll :24-52`). This is the tool for retexturing relics you don't own. For our own relics no API is needed — the engine properties are already virtual (`engine .../RelicModel.cs:130-134`) and the engine implements the fallback chain in `ResolvedBigIconPath` (`:148-168`), so our `StringExtensions.RelicImagePath`/`BigRelicImagePath` fallbacks duplicate the engine (`prior-audit §6`).

---

## 5. Localization

### 5.1 File layout

A mod ships JSON loc tables under `res://<ModId>/localization/<lang>/` where `<lang>` is a BCP-47 folder (`eng`, `deu`, `jpn`, `kor`, `rus`, `zhs`, `ita`, ...). BaseLib's own pck contains `localization/{eng,deu,jpn,kor,rus,zhs,ita}/card_keywords.json, card_selection.json, credits.json (eng only), gameplay_ui.json, main_menu_ui.json, powers.json, settings_ui.json, static_hover_tips.json` (`src BaseLib/localization/`). The engine's `LocManager` loads them via `ModManager.GetModdedLocTables`, which `CustomLocTableManager` augments (`dll BaseLib.Utils/CustomLocTableManager.cs:19-37`, SHIPPED): `public static void Register(string name)` / `RegisterCustomLocTable(this LocManager, string name)` — appends `.json` if missing.

### 5.2 The `ILocalizationProvider` contract (SHIPPED)

```csharp
public interface ILocalizationProvider { string? LocTable => null;  List<(string, string)>? Localization { get; } }
```
`ModelLocPatch` (postfix on `ModelDb.Init`, `src Patches/Localization/ModelLocPatch.cs:33-57`) writes every pair as `{Id.Entry}.{subkey} -> value` into the table given by `LocTable` or by category default:

| Model category | Loc table |
|---|---|
| `ActModel` → `acts`, `AfflictionModel` → `afflictions`, `CardModel` → `cards`, `CharacterModel` → `characters`, `EnchantmentModel` → `enchantments`, `EncounterModel` → `encounters`, `ModifierModel` → `modifiers`, `MonsterModel` → `monsters`, `OrbModel` → `orbs`, `PotionModel` → `potions`, `PowerModel` → `powers`, `RelicModel` → `relics` | (`src :14-30`) |

Each value passes through `SimpleLoc.TrySimplify` (`src :54`). A mod-level table file for, say, relics is `localization/eng/relics.json` with entries keyed `"<ID>.title"`, `"<ID>.description"`, `"<ID>.flavor"`.

### 5.3 Exact key naming scheme (engine side)

| Type | Table | Keys |
|---|---|---|
| Card | `cards` | `<ID>.title`, `<ID>.description` (`engine .../CardModel.cs:108,127`) |
| Relic | `relics` | `<ID>.title`, `<ID>.description`, `<ID>.flavor` (`engine .../RelicModel.cs:48-67`) |
| Power | `powers` | `<ID>.title`, `<ID>.description` (`engine .../PowerModel.cs:49-51`) |
| Potion | `potions` | `<ID>.title`, `<ID>.description` (`engine .../PotionModel.cs:42-44`) |
| Event | `events` | `<ID>.title`, `<ID>.pages.<PAGE>.description`, `<ID>.pages.<PAGE>.options.<OPTION>.title/.description` (`engine .../EventModel.cs:62-64`, `CustomEventModel.Option`) |
| Monster | `monsters` | `<ID>.name`, `moves.<MOVE_ID>.title` (`engine .../MonsterModel.cs:54`, `MonsterLoc`) |
| Character | `characters` | `<ID>.title`, `.titleObject`, `.description`, `.pronounObject`, `.pronounSubject`, `.pronounPossessive`, `.possessiveAdjective`, `.aromaPrinciple`, `.banter.alive.endTurnPing`, `.banter.dead.endTurnPing`, `.eventDeathPrevention`, `.goldMonologue`, `.cardsModifierTitle`, `.cardsModifierDescription` (`dll CharacterLoc.cs`) |

Where `<ID>` is the full content id including prefix (§1.2) — e.g. our relic `Burning Blood` → `SPIRE1-BURNING_BLOOD.title`.

### 5.4 Helper record types (SHIPPED, `dll BaseLib.Abstracts/*Loc.cs`)

Each has an implicit conversion to `List<(string, string)>` producing the exact subkeys above:

- `CardLoc(string Title, string Description, params (string,string)[] ExtraLoc)` → `("title", ...)`, `("description", ...)`
- `RelicLoc(string Title, string Description, string Flavor, params ...)` → `title`/`description`/`flavor`
- `PowerLoc(string Title, string Description, string SmartDescription, params ...)` → + `smartDescription`
- `PotionLoc(string Title, string Description, params ...)` → `title`/`description`
- `MonsterLoc(string Name, IEnumerable<(string,string)> MoveTitles, params ...)` → `name` + `moves.<id>.title` pairs
- `EncounterLoc(string Title, string LossText, params ...)` → `title`/`loss`
- `CharacterLoc(...)` — 14 named fields listed above
- `EventLoc(string Title, params EventPageLoc[] Pages)`; `EventPageLoc(string PageKey, string Description, params EventOptionLoc[] Options)`; `EventOptionLoc(string OptionKey, string Title, string Description)` → `pages.<k>.description`, `pages.<k>.options.<k>.title/.description`
- `ActLoc(string Title, params ...)`; `OrbLoc(string Title, string Description, string SmartDescription, params ...)`; `ModifierLoc(string Title, string Description, params ...)`; `CardModifierLoc(string Title, string Description, string? ExtraCardText = null, params ...)` (+ `extraCardText`)
- `CardModifier` loc table: `card_modifiers` (registered in `BaseLibMain.Initialize` via `CustomLocTableManager.Register("card_modifiers")`, `dll BaseLib/BaseLibMain.cs:76`); keys `<ID>.description` from `CardModifier.GetLoc(subKey)`.

**Use:** `Relics/MagicFlower.cs` returns `new RelicLoc("Magic Flower", "#Healing is 50% more effective.", "It never wilts.")` — the `#` prefix is the SimpleLoc marker (below).

### 5.5 SimpleLoc markup (SHIPPED, `src Patches/Localization/SimpleLoc.cs`)

`SimpleLoc.EnableSimpleLoc(string modId)` (call from your mod initializer — our `MainFile.Initialize` does) opts the mod into post-processing of every loc string on `LocManager.LoadTable` (`src :13-24`). `SimpleLoc.TrySimplify(string)` applies it to a single string if (and only if) it starts with `#`; `ModelLocPatch` routes every `ILocalizationProvider` value through it.

Markup recognized (`src :27-56`):
- `!Var!` — **diff-style variable** (`DiffVariableRegex`), shows the value change (e.g. `!B!` damage/block deltas). `@Var@` — inverse variable.
- `*keyword*` — gold-highlight text (`GoldHighlightRegex`); `$...$` — blue highlight (`BlueHighlightRegex`).
- `{Var}` — normal SmartFormat variable (energy icons: `[E]`, `[E?]`, `[E][E]` via `EnergyIconsRegex`; upgrade swaps via `-(old)-(new)+` / `+(new)+`).
- Leading `#` — "simplify me" marker; strings not starting with `#` pass through untouched.
Special vars and pluralization are handled by `SpecialVarDictionary` and `PluralizeRegex` (`src :58-73`).

### 5.6 Power description extra: `PowerModelLocPatch` and tooltips
`PowerModelLocPatch` (SHIPPED as `dll BaseLib.Patches.Localization/PowerModelLocPatch.cs`) writes power loc the same way; `CustomTooltips`/`ExtraTooltips`/`HoverTipFactoryPatch`/`AutoKeywordText`/`DefaultLoc` (all SHIPPED) extend hover tips: `TooltipSource` (`dll BaseLib.Utils/TooltipSource.cs`) converts a `Type`, `CardKeyword` or `StaticHoverTip` into `IHoverTip` providers, and `DynamicVarExtensions.WithTooltip(var, locKey?, locTable = "static_hover_tips")` attaches keyword tips to dynamic vars (`dll BaseLib.Extensions/DynamicVarExtensions.cs`).

---

## 6. Hooks

### 6.1 BaseLib's own hook interfaces — SHIPPED (namespace `BaseLib.Hooks`)

The shipped v3.3.5 binary contains exactly **four** hook interfaces plus the health-bar forecast support types. Dispatch pattern: hook listeners are `AbstractModel` subtypes (relics, powers, stances, `CustomSingletonModel`, `CardModifier`) in the active combat/run state, found via `IRunState.IterateHookListeners` / `ModHelper.SubscribeForCombatStateHooks`.

| Hook | Signature | When it fires | Veto-style? |
|---|---|---|---|
| `IHealAmountModifier` (`dll Hooks/IHealAmountModifier.cs:9-22`) | `decimal ModifyHealAdditive(Creature creature, decimal amount) => 0m`; `decimal ModifyHealMultiplicative(Creature creature, decimal amount) => 1m` | every heal application, additive then multiplicative | no (returns modified value) |
| `IMaxHandSizeModifier` (`dll Hooks/IMaxHandSizeModifier.cs:10-27`) | `int ModifyMaxHandSize(Player player, int currentMaxHandSize) => currentMaxHandSize`; `int ModifyMaxHandSizeLate(Player player, int currentMaxHandSize) => currentMaxHandSize` | hand-size resolution; six transpile sites in `MaxHandSizePatches` (`dll BaseLib.Patches.Hooks/MaxHandSizePatches.cs`) | no |
| `IAfterCardDowngraded` (`dll Hooks/IAfterCardDowngraded.cs:10-27`) | `void AfterCardDowngraded(CardModel card)` | postfix on `CardModel.DowngradeInternal`; doc warns it also fires on the card-inspection screen — guard on combat/deck membership (`src :12-19`) | no (notification) |
| `IHealthBarForecastSource` (`dll Hooks/IHealthBarForecastSource.cs`) | `IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)` | health-bar refresh; segments collected via `HealthBarForecastRegistry` and drawn by `HealthBarForecastPatch` | no |

**`CustomPowerModel` already implements `IHealthBarForecastSource`** — every power we own can paint HP-bar forecast segments for free (`dll BaseLib.Abstracts/CustomPowerModel.cs:29-33`). Supporting types (all SHIPPED, `dll BaseLib.Hooks/`):
- `public readonly record struct HealthBarForecastSegment(int Amount, Color Color, HealthBarForecastDirection Direction, int Order, Material? OverlayMaterial, Color? OverlaySelfModulate = null)` (+ two convenience ctors, `HealthBarForecastSegment.cs:10-35`)
- `public readonly record struct HealthBarForecastContext(Creature Creature)` with `CombatState`/`CurrentSide` (`HealthBarForecastContext.cs`)
- `public enum HealthBarForecastDirection { FromRight, FromLeft }`; `public static class HealthBarForecastOrder { ForSideTurnStart(...), ForSideTurnEnd(...) }` (`HealthBarForecastDirection.cs`, `HealthBarForecastOrder.cs`)
- `public static class HealthBarForecastRegistry` — `Register<TSource>(string modId, string? sourceId = null) where TSource : IHealthBarForecastSource, new()`, `Register(string modId, string sourceId, IHealthBarForecastSource source)`, `RegisterForeign(string modId, string sourceId, Func<Creature, IEnumerable<object>> provider)` (duck-typed foreign segments), `Unregister(string modId, string sourceId)` (`HealthBarForecastRegistry.cs:31-47`)

**SOURCE-ONLY hook machinery** (not in v3.3.5 — building against it fails):
- `IModifyScryAmount` (`src Hooks/IModifyScryAmount.cs:33,61`) — `int ModifyScryAmount(Player player, int amount)` + follow-up `Task AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount)`; non-positive final amount cancels the scry.
- `IAfterScryed` (`src Hooks/IAfterScryed.cs:53`) — `Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded)`; fires only when a scry actually happened.
- `ICardTypeTextModifier` (§3.11), `ICustomTypeTextCard` (§3.11), `IModifyResourceCostInCombat<T>` / `IAfterSpendResource<T>` (part of the source-only CustomResource system, §2.28).
- Dispatchers: `BaseLibHooks` (`AfterScryed`, `ModifyScryAmount`, `AfterModifyingScryAmount`, `AfterSpendCustomResource<T>`, `ModifyResourceCostInCombat<T>`) and `HookUtils` (`Dispatch<T>`, `Modify<T>`, `AfterModifying`) — both SOURCE-ONLY (`src Hooks/BaseLibHooks.cs:16-131`, `src Utils/HookUtils.cs`).
- `HealthBarForecasts` + `HealthBarForecastSequenceBuilder`/`HealthBarForecastLaneBuilder`/`HealthBarForecastLeftOriginLayout` — richer forecast layout API, SOURCE-ONLY (`src Hooks/HealthBarForecasts.cs`). The shipped binary has only the registry + segment/context/order types above.

### 6.2 Engine hooks from `AbstractModel` (not BaseLib)

`MegaCrit.Sts2.Core.Models.AbstractModel` (ENGINE, `engine MegaCrit.Sts2.Core.Models/AbstractModel.cs`) defines 189 virtual/abstract hook members that any model in the combat/run hook-listener set receives. BaseLib adds none of these — it only supplies `CustomSingletonModel` (§2.25) and `CardModifier` (§2.27) as hook-receiving vehicles plus `ModHelper.SubscribeForCombatStateHooks`/`SubscribeForRunStateHooks` wiring. The families (line numbers in `engine .../AbstractModel.cs`):

- **After events** (`After*`, all `Task`): `AfterActEntered` (208), `AfterAddToDeckPrevented` (217), `AfterAttack` (240), `AfterAuto{Post,Pre}PlayPhaseEntered[*]` (253-294), `AfterBlockCleared` (304), `AfterBlockGained`/`AfterBlockBroken` (330/347), `AfterCardChangedPiles[*]` (359/373), `AfterCardDiscarded` (384), `AfterCardDrawn[*]` (398/410), `AfterCardEnteredCombat` (420), `AfterCardGeneratedForCombat` (435), `AfterCardExhausted` (447), `AfterCardPlayed[*]` (477/488), `AfterCombatEnd` (520), `AfterCombatVictory[*]` (545/556), `AfterCreatureAddedToCombat` (565), `AfterCurrentHpChanged` (578), `AfterDamageGiven/Received[*]` (593-640), `AfterDeath` (668), `AfterDiedToDoom` (679), `AfterEnergyReset[*]` (689/701), `AfterEnergySpent` (712), `AfterFlush[*]` (732/758), `AfterGoldGained` (767), `AfterHandEmptied` (804), `AfterItemPurchased` (815), `AfterMapGenerated` (825), `AfterModifying*` (block amount 842, card play count 851, card play result location 861, orb passive trigger count 870, card reward options 878, damage amount 886, energy gain 894, gold gained 904, hand draw 913, hp lost before/after Osty 930/938, power amount received/given 948/958, rewards 966), `AfterOrbChanneled/Evoked` (977/988), `AfterOstyRevived` (997), `AfterPotionUsed/Discarded/Procured` (1008-1039), `AfterPowerAmountChanged` (1077), `AfterPreventingBlockClear` (1088), `AfterPreventingDeath` (1097), `AfterRestSiteHeal/Smith` (1112/1121), `AfterRewardTaken` (1131), `AfterRoomEntered` (1153), `AfterShuffle` (1164), `AfterStarsSpent/Gained` (1175/1186), `AfterForge` (1198), `AfterSummon` (1210), `AfterTakingExtraTurn` (1220), `AfterTargetingBlockedVfx` (1230), `AfterSideTurnStart[*]` (1268-1290), `AfterPlayerTurnStart[*]` (1306-1336), `AfterSideTurnEnd[*]` (1406-1425)
- **Before events**: `BeforeAttack` (228), `BeforeBlockGained` (317), `BeforeCardAutoPlayed` (459), `BeforeCardPlayed` (468), `BeforeCombatStart[*]` (498/510), `BeforeCombatRewardOffered` (532), `BeforeDamageReceived` (608), `BeforeDeath` (650), `BeforeCardRemoved` (721), `BeforeFlush[*]` (732/745), `BeforeHandDraw[*]` (779/793), `BeforePotionUsed` (1008), `BeforePowerAmountChanged` (1057), `BeforeRoomEntered` (1140), `BeforeSideTurnStart` (1247), `BeforeSideTurnEnd[Very]Early/…` (1354-1388)
- **Modifiers** (`Modify*`, return the modified value): `ModifyAttackHitCount` (1436), `ModifyBlockAdditive/Multiplicative` (1459/1482), `ModifyCardPlayCount` (1495), `ModifyCardPlayResultLocation` (1512), `ModifyOrbPassiveTriggerCounts` (1524), `ModifyCardRewardCreationOptions[*]` (1536/1549), `ModifyCardRewardUpgradeOdds` (1561), `ModifyDamageAdditive/Cap/Multiplicative` (1579-1613), `ModifyEnergyGain` (1624), `ModifyGoldGained` (1635), `ModifyGeneratedMap[*]` (1646/1660), `ModifyHandDraw[*]` (1671/1683), `ModifyHpLostBefore/AfterOsty[*]` (1702-1761), `ModifyMaxEnergy` (1771), `ModifyMerchantCardPool` (1783), `ModifyMerchantCardRarity` (1794), `ModifyMerchantPrice` (1818), `ModifyOrbValue` (1829), `ModifyPowerAmountGivenAdditive/Multiplicative` (1845/1861), `ModifyRestSiteHealAmount` (1872), `ModifyShuffleOrder` (1887), `ModifySummonAmount` (1902), `ModifyUnblockedDamageTarget` (1915), `ModifyNextEvent` (1925), `ModifyUnknownMapPointRoomTypes` (1935), `ModifyOddsIncreaseForUnrolledRoomType` (1947), `ModifyXValue` (1958)
- **Veto-style** (`Try*` / `Should*` / `bool`): `TryModifyCardBeingAddedToDeck[*]` (1970/1984), `TryModifyCardRewardAlternatives` (1998), `TryModifyCardRewardOptions[*]` (2009/2021), `TryModifyEnergyCostInCombat[*]` (2034/2049), `TryModifyKeywordsInCombat` (2066), `TryModifyStarCost` (2078), `TryModifyPowerAmountReceived` (2093), `TryModifyRestSiteOptions` (2105), `TryModifyRestSiteHealRewards` (2122), `TryModifyRewards[*]` (2140/2155), `ModifyExtraRestSiteHealText` (2167), `ShouldAddToDeck` (2177), `ShouldAfflict` (2188), `ShouldAllowAncient` (2200), `ShouldAllowHitting` (2211), `ShouldAllowTargeting` (2221), `ShouldAllowSelectingMoreCardRewards` (2232), `ShouldClearBlock` (2242), `ShouldDie[*]` (2252/2264)
- Abstract: `public abstract bool ShouldReceiveCombatHooks { get; }` (68) — implemented by `CustomSingletonModel`, `CardModifier`, and every engine relic/power.

`RestSiteOption`-related hooks (our Girya-style relics) are engine `Hook.ModifyRestSiteOptions` + `RelicModel.TryModifyRestSiteOptions` (§2.18), not BaseLib. BaseLib itself adds **no** card-rarity hook — `CardRarityOdds.Roll(CardRarityOddsType)` is a public engine instance method reachable by a plain Harmony prefix if a relic ever needs to change rarity odds (`engine MegaCrit.Sts2.Core.Odds/CardRarityOdds.cs:69-81`; `prior-audit §3`).

---

## 7. Utilities and extensions

Namespace `BaseLib.Utils` unless noted. All SHIPPED unless flagged. Full signatures were copied from the shipped decompile (`dll BaseLib.Utils/...`); this section lists the surface compactly.

### 7.1 Combat scripting — `CommonActions`, `MonsterActions`, `MoveBuilder`

`CommonActions` (static, `dll Utils/CommonActions.cs`) — the workhorse for card code, used heavily by our `mod/Spire1Code/Cards/`:
- `AttackCommand CardAttack(CardModel card, Creature? target, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)` + overloads taking `CardPlay?`, `decimal damage`, `ValueProp valueProp`, `CalculatedDamageVar`
- `Task<decimal> CardBlock(CardModel card, CardPlay? play)` (+ `BlockVar`/`DynamicVar` overloads with `bool fast = false`)
- `Task<IEnumerable<CardModel>> Draw(CardModel card, PlayerChoiceContext context)`
- `Task<T?> Apply<T>(...)` / `Task<IReadOnlyList<T>> Apply<T>(...)` / `Task<T?> ApplySelf<T>(...) where T : PowerModel` — many overloads over `(Creature target, DynamicVarSource|CardModel|decimal, bool silent)` and `(PlayerChoiceContext context, ...)`
- `Task<IEnumerable<CardModel>> SelectCards(CardModel, CardSelectorPrefs|LocString, PlayerChoiceContext, PileType[, Func<CardModel,bool>? filter][, int count|minCount/maxCount])`, `Task<CardModel?> SelectSingleCard(...)`
- `IEnumerable<CardModel> GenerateCards(CardModel, int count, Func<CardModel,bool>? filter = null)`, `CardModel? GenerateSingleCard(...)`

`MonsterActions` (static, `dll Utils/MonsterActions.cs`): `AttackCommand Attack(MonsterModel monster, int baseDmg, int hitCount = 1)`; `Task<T?> ApplySelf<T>(MonsterModel, decimal amount, PlayerChoiceContext? context = null, bool silent = false)`; `Task<IReadOnlyList<T>> Apply<T>(MonsterModel, decimal amount, IEnumerable<Creature> targets, ...)`.

`MoveBuilder` (`dll Monsters/MoveBuilder.cs:15-86`) — fluent monster move authoring:
```csharp
public class MoveBuilder
{ public enum PowerIntent { /* None, Buff, Debuff, ... */ }
  public readonly MonsterModel Monster; public readonly string Id;
  public readonly List<Func<IReadOnlyList<Creature>, Task>> Actions;
  public readonly List<AbstractIntent> Intents;
  public string? FollowUpStateId { get; set; }
  public MoveBuilder(MonsterModel monster, string id);
  public MoveBuilder Attack(int damage, int hitCount = 1, (string, float, bool)? attackerAnim = null, string? attackerVfx = null, string? attackerSfx = null, string? attackerTmpSfx = null, string? hitVfx = null, string? hitSfx = null, string? hitTmpSfx = null);
  public MoveBuilder Block(int amount, ValueProp props = (ValueProp)8);
  public MoveBuilder ApplyToPlayers<T>(int amount, bool isStrongDebuff, bool silent = false) where T : PowerModel;
  public MoveBuilder ApplyToSelf<T>(int amount, bool silent = false) where T : PowerModel;
  public MoveBuilder ApplyToSomeone<T>(int amount, Func<IEnumerable<Creature>> targets, PowerIntent intent = PowerIntent.None, bool silent = false) where T : PowerModel;
  public MoveBuilder HealSelf(int amount, bool autoScaleWithPlayers = true);  public MoveBuilder HealSelf(Func<int> amount);
  public MoveBuilder PlaySfx(string key);  public MoveBuilder PlaySfx(ModSound sound, float volumeAdd = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f);
  public MoveBuilder PlayAnim(string animKey, float waitTime);
  public MoveBuilder CustomAction(Func<IReadOnlyList<Creature>, Task> action);
  public MoveBuilder AddIntent(AbstractIntent intent);
  public MoveBuilder FollowingState(string stateId);
  public MoveState Build();  public static implicit operator MoveState(MoveBuilder builder); }
```

### 7.2 State attachment — `SpireField` family (SHIPPED, `dll Utils/SpireField.cs` etc.)

Attach typed state to engine objects with clone/save semantics:
- `SpireField<TKey, TVal> where TKey : class` — `TVal? this[TKey]`, `Get/Set`, `CopyOnClone(Action<TKey,TKey,TVal?>? cloneVal = null)`; `Clone(AbstractModel src, AbstractModel dst)` hooks `AbstractModel.MutableClone` via `SavedSpireFieldPatch` (`dll Baselib.Patches.Utils/SavedSpireFieldPatch.cs`)
- `NotNullSpireField<TKey, TVal>` (non-null defaults, `Get/Set/this`), `ReadonlySpireField<TKey, TVal>` (`new void Set`)
- `SavedSpireField<TKey, TVal> : SpireField<TKey,TVal>, ISavedSpireField` — `Name`, `TargetType`, `Serializer`/`Deserializer`, `Export/Import(SavedProperties)`, `RegisterCustomSave()`, `IsBasegameSupported` — persists via the engine's `[SavedProperty]`-adjacent pipeline
- `ICloneableField` (marker), `ISavedSpireField`, `AddedNode<TParentType, TNode> : ReadonlySpireField<TParentType, TNode>, IAddedNodes<TParentType>` — `ctor(Func<TParentType,TNode>)` or `ctor(string scenePath, Action<TParentType,TNode>? extraSetup = null)`, `GetNode(TParentType)`
- `ExtendedSaveInfo<DataSourceType, DataHolderType>` record + `SavePatchUtils` (`Serializer`/`Deserializer` statics, `QuickProps`, `TryGetSerializerDeserializer`) — engine save-system glue

**Use:** our cards persist counters with the engine's own `[SavedProperty]` (`Cards/GeneticAlgorithm.cs:23,34`; `Cards/RitualDagger.cs:59`) — no BaseLib save API needed for that; `SavedSpireField`/`CardModifier.ModifierSave` are the BaseLib paths when a field must follow clones.

### 7.3 Misc utilities (SHIPPED unless noted)

- `CustomAnimation` — `bool HasCustomAnimation(Node visualRoot)`, `bool PlayCustomAnimation(Node n, params string[] tryAnimNames)`; internal handlers for `AnimationPlayer`/`AnimationTree`/`AnimatedSprite2D`; `CustomAnimationPatch` (`dll BaseLib.Patches.UI/CustomAnimationPatch.cs`) routes `NCreature.SetAnimationTrigger`/`AnimDie`/`AnimTempRevive`/`StartDeathAnim` through it
- `CustomBackgroundAssets : BackgroundAssets` — 5 ctors incl. `(string layersPath, Rng rng)`, `(string layersPath, string bgScenePath, Rng rng)`, `(string, List<string>, string)`, `(string, IEnumerable<IEnumerable<string>>, IEnumerable<string>, Rng)` (`dll Utils/CustomBackgroundAssets.cs`)
- `CustomCharacterUtils` — `TryOrderCustomCharacters(List<Type>)` + 10 generic overloads `TryOrderCustomCharacters<T1..T10>() where T# : CustomCharacterModel` (order in character select)
- `GodotUtils` — `CreatureVisualsFromImage(string path)`, `CreatureVisualsFromScene(string path)`, `TransferAllNodes<T>(this T obj, string sourceScene, params string[] uniqueNames) where T : Node`
- `ShaderUtils` — `GenerateHsv(float h, float s, float v)`, `CreateDoomBarShaderMaterial(GradientTexture1D)`, `CreateVanillaDoomBarGradientTexture()`, `CreateVanillaDoomBarNoiseTexture()`
- `WeightedList<T>` — `Add(T)`, `Add(T, int weight)`, `GetRandom(Rng rng[, bool remove])`, `IList<T>`; nested `WeightedItem(T val, int weight)`
- `OptionPools` — ctors over 1–3 `WeightedList<AncientOption>`, `AllOptions`, `List<AncientOption> Roll(Rng rng, AncientEventModel ancient)`; `AncientOption(int weight) : IWeighted` abstract + `AncientOption<T> where T : RelicModel` + `explicit operator AncientOption(RelicModel)` (custom-ancient support; `RelicModelExtensions.AddCustomAncientSpawnCondition(RelicModel, Func<AncientEventModel,bool>)` hooks it)
- `AncientDialogueUtil` — `SfxPath(string dialogueLoc)`, `BaseLocKey(string ancientId, string charId)`, `GetDialoguesForKey(string locTable, string baseKey, StringBuilder? log = null)`
- `FmodAudio` — FMOD event/bank wrappers: `PlayEvent(string eventPath[, Dictionary<string,float> parameters|int cooldownMs])`, `PlayEventByGuid(string)`, `CreateEventInstance`, `PlayFile/PreloadFile/UnloadFile/PreloadMusic/PlayMusic/CreateSoundInstance`, `LoadBank/UnloadBank`, `RegisterReplacement(string originalEvent, Func<string,float,bool> handler)`, `RegisterFileReplacement`/`RegisterEventReplacement`/`RemoveReplacement`/`ClearReplacements`, `CreatePool/AddToPool/PlayPool/RemovePool`, `StartSnapshot/StopSnapshot`, bus volume/mute/pause, `SetGlobalParameter`/`GetGlobalParameter` (`dll Utils/FmodAudio.cs`)
- `ModAudio`/`ModSound`/`AutoModAudio` (`dll BaseLib.Audio/`) — Godot-stream audio: `ModSound(string file, ModAudio.SoundType soundType = SoundType.Sfx)` record with `Play(...)`; `ModAudio.PlaySoundGlobal/PlaySoundInRun/PlaySound(ModSound, volumeAdd, volumeMult, pitchVariation, basePitch, Node? targetNode)`; `AutoModAudio(string folder)` with `PlaySfx/PlayMusic/PlayAmbience` relative to a folder; `enum SoundType { Sfx, Music, Ambience }`
- `CombatStateWrapper` — reflection-free accessors over a `CombatState`: `RunState`, `Allies`, `Enemies`, `Creatures`, `Players`, `Modifiers`, `MultiplayerScalingModel`, `RoundNumber`, `CurrentSide`, `EscapedCreatures`, `HittableEnemies`, `GetCreaturesOnSide/GetOpponentsOf/GetTeammatesOf/GetPlayer/HappenedThisTurn`
- `BetaMainCompatibility` — reflection-based `VariableMethod`/`VariableReference` accessors (`PowerCmd_.Apply`, `AttackCommand_`, `Hook_`, `Creature_.CombatState`, `CardModel_.CombatState`, `RunState.IterateHookListeners`, `_ModManifest.HasDependency`) — the seam BaseLib uses to survive beta-branch API drift; **our code should not need it directly**
- `DynamicVarSource` — `required DynamicVarSet DynamicVars`, `Creature? Owner`, `Card`/`Relic`/`Power`/`Potion`/`Enchantment`/`CardModifier` with implicit conversions from each model type
- `GeneratedNodePool` / `GeneratedNodePool<T> where T : Node, IPoolable` — `Init<T>(Func<T>, int prewarmCount)`, `Get()`, `Free(T)`
- `GodotMethod` / `GodotMethodDelegate` (`public delegate Variant GodotMethodDelegate(GodotObject obj, params Variant[] args)`) — dynamic Godot method invocation
- `ChainedEnumerable<T>` + `Chain<T>(this IEnumerable<T>, [ParamCollection] IEnumerable<T>)`, `ReflectionUtils` (`GetSetterForProperty`), `TooltipSource` (§5.6), `VariableMethod`/`VariableReference`, `PoolAttribute`, `SavePatchUtils`, `AncientOption`, `OptionPools`, `CustomLocTableManager` (§5.1)
- SOURCE-ONLY: `WhatMod` (mod attribution registry), `BaseLibTip` (tip base), `ModCredits` (`enum Layout` inside), `HookUtils` (§6.1), `CustomCharacterStatsPatch` etc. — see §9.

### 7.4 Patching helpers (`BaseLib.Utils.Patching`, SHIPPED)
`InstructionPatcher` (positional IL walker: `Match(params IMatcher[])`, `MatchFromEnd`, `Step`, `AddLabel`, `GetOperandLabel`, `GetOperand`, `TryGetIntValue`, ...), `InstructionMatcher` (fluent: `any()`, `stloc_any()`, `ldloc_any()`, `call_any()`, `PredicateMatch`, `StoreOperand`, `LazyMatch`), `CallMatcher`, `IMatcher`, `OpCodeValues`, `AsyncMethodCall.Create(ILGenerator, IEnumerable<CodeInstruction>, MethodBase original, MethodInfo callMethod, MethodBase? beforeState = null, MethodBase? afterState = null, string? resultName = null)` (async-method transpiler support). Plus `HarmonyExtensions.TryPatchAll(this Harmony, Assembly, string? category = null)` and `PatchAsyncMoveNext` (`dll BaseLib.Extensions/HarmonyExtensions.cs:14-36`); `CodeInstructionExtensions.TryGetIntValue`, `MethodInfoExtensions.Call/CallVirt/StateMachineType`, `FieldInfoExtensions.Stfld/Ldfld/Ldflda`, `MethodBaseExtensions.ArgIndex`, `TypeExtensions.FindStateMachineField/GetMethodExt`, `IEnumerableExtensions.CheckCode`.

### 7.5 `Extensions/` (SHIPPED, `dll BaseLib.Extensions/`)

| Extension class | Members | Use |
|---|---|---|
| `CardExtensions` | `GetTargets(this CardModel)`, `AddModifier(this CardModel, CardModifier)`, `AddModifier<T>(this CardModel, int amount = 0) where T : CardModifier`, `GetModifiers`, `GetModifier<T>`/`TryGetModifier<T>`, `GetModifier(ModelId)`/`TryGetModifier(ModelId)` | CardModifier accessors |
| `AttackCommandExtensions` | `WithValueProp(this AttackCommand, ValueProp)`; `ExecuteAndCheckFatal(PlayerChoiceContext)` (in source; shipped has `WithValueProp`) | StS1 on-kill effects |
| `PlayerExtensions` | `HasPower<T>(this Player) where T : PowerModel`, `TryGetRelic<T>(this Player, out T? relic) where T : RelicModel` | |
| `PowerExtensions` | `InvokeSecondAmountChanged(this IHasSecondAmount)` | |
| `ModelExtensions` | `LocKey(this AbstractModel, string subKey)`, `GetDynamicVar(this AbstractModel, string varKey)` | |
| `RelicModelExtensions` | `AddCustomAncientSpawnCondition(this RelicModel, Func<AncientEventModel,bool>)`, `RelicCanSpawnAtCustomAncient(this RelicModel, AncientEventModel)` | |
| `ActModelExtensions` | `ActNumber(this ActModel)` (1-based; the `CustomActModel.ActNumber` obsolete property calls this) | |
| `DynamicVarExtensions` | `WithUpgrade<TDynamicVar>(this TDynamicVar, decimal upgradeValue)`, `CalculateBlock(DynamicVar, Creature, ValueProp, CardPlay?, CardModel?)`, `WithTooltip<TDynamicVar>(this TDynamicVar, string? locKey = null, string locTable = "static_hover_tips")` | |
| `DynamicVarSetExtensions` | `Power<T>(this DynamicVarSet)`, `Var<T>(this DynamicVarSet, string? name = null)` | |
| `StringExtensions` | `RemovePrefix()`, `RegisterSceneForConversion<TNode>(this string, Action<TNode>? postConversion = null)`, `ComputeBasicHash()`, `TryGetType()` | §1.2, §4.3 |
| `ImageHelperExtensions` | `GetModImagePath(string innerPath, Type? type = null)` | §4.1 |
| `TypeExtensions` / `TypePrefix` | `FindStateMachineField`, `GetMethodExt`, `GetPrefix(this Type)` / `GetRootNamespace(this Type)` | §1.2 |
| `ListExtensions` | `InsertSorted<T>(this List<T>, T[, IComparer<T>])` | |
| `HarmonyExtensions` | `TryPatchAll(this Harmony, Assembly, string? category = null)`, `PatchAsyncMoveNext` | BaseLib's own patch application |
| `NodeExtensions` | `AddUnique(this Node, Node child, string? name = null)`, `FindFirstFocusable(this Node?)` | |
| `ControlExtensions` | `DrawDebug(this Control[, Control])`, `AddThemeFontSizeOverrideAll(this Control, int)`, `ClearFocusNeighbors(this Control)` | |
| `FloatExtensions` | `OrFast(this float time)` | |
| `IEnumerableExtensions` | `AsReadable<T>(sep = ",")`, `NumberedLines<T>()`, `CheckCode(IEnumerable<CodeInstruction>)` | |
| `AudioStreamPlayerExtensions` | `FadeIn(this AudioStreamPlayer, float)`, `FadeOut(this AudioStreamPlayer, float)` | |
| `CardSelectorPrefsExtensions` | `TransformAndUpgradeSelectionPrompt` (`LocString`) | |
| `PublicPropExtensions` | `IsPoweredAttack_`, `IsPoweredCardOrMonsterMoveBlock_`, `IsCardOrMonsterMove_` | |
| `MethodBaseExtensions` / `MethodInfoExtensions` / `FieldInfoExtensions` / `CodeInstructionExtensions` | IL-emit helpers (§7.4) | |
| `GeneralExtensions`, `IComparableExtensions`, `PlayerCombatStateExtensions` | **SOURCE-ONLY** (v3.4.5 additions) | |

**Supersedes hand-rolled code in `mod/Spire1Code/Extensions/` (`prior-audit §7`):**
- `ImageHelperExtensions.GetModImagePath` replaces most of our `StringExtensions.cs` path builders (all nine `...ImagePath()` methods construct `Path.Join(MainFile.ResPath, "images", ...)`). Keep ours only for the missing-file fallback-to-placeholder behavior.
- `CardExtensions.GetTargets` / `AddModifier` family — use instead of ad-hoc targeting/modifier code.
- `AttackCommandExtensions.ExecuteAndCheckFatal` — exactly what StS1 on-kill cards (Ritual Dagger, Feed) want.
- Our `Extensions/StanceCmd.cs` and `IOnStanceChanged.cs` have no BaseLib equivalent (stance system is engine-only) — keep them.
- Our relic/big-relic path fallbacks duplicate the engine's `ResolvedBigIconPath` chain (§4.6) — deletable if `ResourceLoader.Exists` gating is acceptable.

### 7.6 `Config/` (SHIPPED, `dll BaseLib.Config/`)

| Type | Purpose |
|---|---|
| `ModConfig` (abstract) | `event EventHandler? ConfigChanged`, `event Action? OnConfigReloaded`, `ModPrefix`, `[ConfigIgnore] ModId`, nested `ModConfigLogger` (`Warn/Error(string, bool showInGui)`, `PendingUserMessages`), `HasSettings()`/`HasVisibleSettings()`, `virtual bool VisibleInModList()`, `GetDefaultValue<T>(string)`, restore-defaults, **MUST** `public abstract void SetupConfigUI(Control optionContainer)`, `Changed()`, `Load<T>()`/`SaveDebounced<T>(int delayMs = 1000)`/`Save()`, `GetLabelText(string, bool slugify)` |
| `SimpleModConfig : ModConfig` | auto-generated settings UI from public static properties: `SetupConfigUI`, `ConfirmRestoreDefaults()`, `CreateToggleOption/SliderOption/DropdownOption/LineEditOption/ColorPickerOption/Button`, `CreateSectionHeader/CollapsibleSection`, `AddRestoreDefaultsButton` |
| `ModConfigRegistry` | `Register(string modId, ModConfig)`, `Get(string?)`, `Get<T>()`, `GetAll()` |
| `BaseLibConfig` | BaseLib's own settings (log window, sfx player limit, harmony patch dump, `ShowModConfigInMainMenu`, `LastModConfigModId`) |
| Attributes | `ConfigButtonAttribute`, `ConfigColorPickerAttribute`, `ConfigDropdownOverrideLocalizationAttribute`, `ConfigHideInUI`, `ConfigHoverTipAttribute`, `ConfigHoverTipsByDefaultAttribute`, `ConfigIgnoreAttribute`, `ConfigIgnoreRestoreDefaultsAttribute`, `ConfigSectionAttribute`, `ConfigSliderAttribute` (`Min/Max/Step/Format`), `ConfigTextInputAttribute` (`AllowedCharactersRegex`, `MaxLength`, ctor `(TextInputPreset)`), `ConfigVisibleIfAttribute` (`TargetName`, `Args`, `Invert`), `ConfigVisibleWhenAttribute`, `SliderRangeAttribute`, `SliderLabelFormatAttribute`; `enum TextInputPreset`; `GodotColorConverter` |
| `NConfig*` UI nodes | `NConfigButton`, `NConfigCollapsibleSection`, `NConfigColorPicker`, `NConfigDropdown(+Item)`, `NConfigLineEdit`, `NConfigOptionRow`, `NConfigSlider`, `NConfigTickbox`, `NModConfigSubmenu`, `NModListButton`, `NNativeScrollableContainer`, `ISelectionReticle` |
| `NativeFileDialogChrome` | **SOURCE-ONLY** |

**Use:** our `Config/Spire1Config.cs : SimpleModConfig` with `[ConfigHoverTipsByDefault]` and getter-only `[ConfigIgnore]` gate helpers — the whole class is read by run/act/pool generation code to toggle content (§1.4 registers it).

### 7.7 Cards/Variables, Commands, BaseLibScenes (SHIPPED)
- `BaseLib.Cards.Variables`: `CustomCalculatedVar(string name)` with `virtual decimal CalculateCustom(Creature? target)` + `WithMultiplier(Func<RelicModel,Creature?,decimal>)` / `(Func<PowerModel,Creature?,decimal>)` / `GeneralMultiplier(Func<DynamicVarSource,Creature?,decimal>)`; `CustomCalculatedDamageVar` / `CustomCalculatedBlockVar` (same shape); `CustomExtraDamageVar(string baseName, decimal damage)`; `DisplayVar<T>(string name, Func<T,string>)`; `ExhaustiveVar(decimal)` (`static int ExhaustiveCount(CardModel, int baseExhaustive)`); `PersistVar(decimal)`; `RefundVar(decimal)` — see also `BaseLibKeywords.Purge` (`dll BaseLib.Cards/BaseLibKeywords.cs`)
- `BaseLib.Commands.MultiPileCardSelect` — `Select(PlayerChoiceContext, Player, CardSelectorPrefs, Func<CardModel,bool>? filter = null, params PileType[] pileTypes)` and a `List<CardModel>` overload (multi-pile selection UI; `ScryCmd`/`ScryVar` are SOURCE-ONLY)
- `BaseLib.BaseLibScenes`: `NLogWindow` (log UI), `NHorizontalScrollContainer` (draggable scroll container; used by character select), `NCustomCharacterSelectEntryButton` (§2.10), `Acts/NCustomTreasureRoomChest` (`static Create(NTreasureRoom, IRunState, NButton, string scenePath)`), `Acts/NDynamicCombatBackground` (the `res://BaseLib/scenes/dynamic_background.tscn` backing type) — `NRewardHighlight`/`NCustomLinkedRewardSet` are SOURCE-ONLY
- `BaseLib.Utils.ModInterop` (SHIPPED): `ModInteropAttribute(string modId, string? type = null)`, `InteropTargetAttribute`, `InteropClassWrapper` — soft interop with other mods' classes; processed in `PostModInitPatch.EarlyPostInit` and `Patches/Features/ModInteropPatch.cs`

---

## 8. Patches

BaseLib applies its patches with a single `Harmony("BaseLib")` instance via `MainHarmony.TryPatchAll(assembly)` (`dll BaseLib/BaseLibMain.cs:73`), plus three hand-applied patches before that (`ExtendedSavePatches`, `TheBigPatchToCardPileCmdAdd`, `CustomBadgesPatch`, `dll :66-71`) and `AddActContent.Patch(harmony)` from `PostModInitPatch` (`src :92`). Targets below were extracted from the shipped decompile and the source tree (`[HarmonyPatch(typeof(...))]` attributes). Grouped by area:

### 8.1 Content registration & IDs (`Baselib.Patches.Content`, SHIPPED)
`CustomContentDictionary` (`ModelDb.InitIds` postfix — §1.3), `PrefixIdPatch` (`ModelDb.GetEntry` — §1.2), `ModelDbCustomActsPatch` (`ModelDb.Acts` getter — act ordering), `ModelDbSharedCardPoolsPatch`/`ModelDbSharedRelicPoolsPatch`/`ModelDbSharedPotionPoolsPatch` (shared-pool getters), `CustomSharedEvents` (`ModelDb.AllSharedEvents`), `AddCustomCharacters`/`AddCustomAncientsToPool`/`CustomAncientExistence` (character/ancient registration into pools), `AddActContent`, `CustomEnums`/`GenEnumValues`/`CustomEnumAttribute` (runtime-generated `[CustomEnum]` values), `StarterUpgradePatches` (`TouchOfOrobas.GetUpgradedStarterRelic`), `ArchaicToothTranscendenceUpgradesPatch`, `DustyTomePatch` (`DustyTome` content), `TrashHeapCardsPatch`/`TrashHeapRelicsPatch` (`TrashHeap.Cards`/`TrashHeap.Relics`), `CustomPiles`/`SpecialPileInCombat`/`GetCombatPile`/`GetNCardPile`/`GetPilePosition`/`IsCombatPile`/`TheBigPatchToCardPileCmdAdd` (`PileTypeExtensions` + `CardPileCmd.Add`), `CustomRewardPatches` (`Reward` serialize/deserialize + `RewardsSetSynchronizer.SelectLocalR...`), `RewardSynchronizerPatches` (`RewardSynchronizer`), `CurrentGeneratingRunState`, `CustomKeywords`/`AutoKeywordPosition`/`KeywordPropertiesAttribute`/`GetCustomLocKey`, `CustomPilePatches`, `CustomRewardPatches`, `DustyTomeCardPatch`, `TrashHeapPatch`, `PrefixIdPatch`.

### 8.2 Saves & multiplayer (`Baselib.Patches.Content` + `Patches/Saves`/`Networking`, SHIPPED)
The whole `Serializable*` family — `SerializableRun` (`Serialize`/`Deserialize`/`Anonymized`), `SerializableReward`, `SerializableRelic`, `SerializablePotion`, `SerializablePlayer`, `SerializableEnchantment`, `SerializableCard` (Serialize+Deserialize each), plus `RunState.FromSerializable`, `RunManager.ToSave`/`GenerateRooms`/`CanonicalizeSave`, `Reward.FromSerializable`/`ToSerializable`, `RelicModel.ToSerializable`/`FromSerializable`, `PotionModel`, `Player`, `Reward` — the extended save-data pipeline (`Patches/Saves/ExtendedSaveHandlers.cs`/`ExtendedSaveTypes.cs`, `IgnoreUnknownRun`/`IgnoreUnknownCoopRun` compat). Networking: `CustomMessagePatches` (`AdjustCustomMessageKeys`, wrapper Register/Unregister on net service buffers).

### 8.3 Hook plumbing (`BaseLib.Patches.Hooks`, SHIPPED)
`MaxHandSizePatches` — six sites: `CardConsoleCmd_Process`, `CardOnPlay`, `CardPileCmd_Add`, `CardPileCmd_CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot`, `CardPileCmd_Draw`, `CombatManager_SetupPlayerTurn` (drives `IMaxHandSizeModifier`, `dll BaseLib.Patches.Hooks/MaxHandSizePatches.cs`); `ModifyHealAmountPatches` (drives `IHealAmountModifier`); `ModifyBaseDamagePatches`; `AfterCardPlayedPatch` (dispatch after card play). (`IAfterCardDowngraded.DowngradeHook` is a nested postfix on `CardModel.DowngradeInternal` inside the interface itself, §6.1.)

### 8.4 Model surface (`Patches/Utils` + content, SHIPPED)
`UpgradeInternalPatch` (`CardModel.UpgradeInternal` + `FinalizeUpgradeInternal`/`UpgradeModifiers`/`FinalizeModifierUpgrade`/`DowngradeModifiers`), `SavedSpireFieldPatch` (`SavedProperties` init / `AbstractModel.MutableClone` postfix), `SelfApplyDebuffPatch` (`NPower`/self-debuff), `TwoAmountPowers` (`SecondAmountRegistry`), `LogPatch`/`LogListener` (log capture), `HarmonyPatchDumpMainMenuPatch`/`NMainMenuReadyOpenLogWindowPatch`/`Inject*ModConfig*Patch`/`NSettingsScreen_OnSubmenuShown_Patch` (settings injection), `SetVolumePatches` (`MasterVol`/`MusicVol`/`AmbienceVol`/`Sfx...`).

### 8.5 UI (`BaseLib.Patches.UI`, SHIPPED)
Character select: `CustomCharacterSelectEntryPatch` (`NCharacterSelectScreen.OnEmbarkPressed`, `OnSubmenuOpened`/`Closed`, `SelectCharacter`, `OnUnreadyPressed`), `ScrollCharSelectPatch`, `HideVanillaCharacterSelectCharactersPatch`, `VanillaRandomCharacterEligibilityPatch`, `MerchantCharacterAnimPatch` (`NMerchantCharacter._Ready`/`PlayAnimation`). Cards/relics/powers/potions/orbs: `CustomCompendiumPatch`/`InRunCompendiumPatch`*, `ModelUiPatch` (HoverTips on Card/Relic/Power/Potion/Orb + `NCardLibrary`), `CustomEnergyIconPatches` (§3.4), `RelicImageOverridePatch` (§4.6), `RoomIconPathPatch` (`ImageHelper.GetRoomIconPath`/`GetRoomIconOutlinePath`), `CustomRunScreenScrollPatch` (`NCustomRunScreen.InitCharacterButtons`), `HealthBarForecastPatch` (`NHealthBar.SetHpBarContainerSizeWithOffsetsImmediately`/`RefreshText`/`RefreshMiddleground`/`RefreshForeground`), `CustomAnimationPatch` (§7.3), `CustomCharacterStatsPatch`*, `AddSubtypesToTypePlaquePatch`* (`NCard.UpdateTypePlaque`), `CharacterSelectStartingRelicsPatch`*, `ModSourceTooltip`*/`MonsterSourceLabel`*/`EventSourceLabel`*/`AncientSourceLabel`* (mod attribution), `NCreditsScreenPatch`* (`NCreditsScreen`), `ShowModelIdCacheHash`, `SceneConversionPatch` (§4.3), `BadgeIconGetterPatch`/`NBadgeCreateStringPatch`. (`*` = SOURCE-ONLY types, §9.)

### 8.6 Localization (`BaseLib.Patches.Localization`, SHIPPED)
`ModelLocPatch` (`ModelDb.Init` — §5.2), `PowerModelLocPatch`, `SimpleLoc` (`LocManager.LoadTable` — §5.5), `CustomLocTablePatches` (`LocManager.ListLocalizationFiles` + `ModManager.GetModdedLocTables`), `CustomTooltips`/`ExtraTooltips`/`HoverTipFactoryPatch` (`HoverTipFactory.Static`), `AutoKeywordText`, `DefaultLoc`, `DescriptionOverrides` (custom description delegate), `AddAncientDialogues`.

### 8.7 Fixes, features, audio, compatibility (SHIPPED unless noted)
- `Patches/Fixes`: `CardRewardSerializationPatches` (card reward save paths, incl. `CardRarityOddsType` plumbing), `AnyPlayerCardTargetingPatches` (`NCardPlay.TryPlayCard`, `NControllerCardPlay.Start`, `NMouseCardPlay.TargetSelection`), `SkipSentryShutdownPatch`* (SOURCE-ONLY).
- `Patches/Features`: `AutoPlayCustomTargetPatch`/`CustomTargetType` (`CardCmd.AutoPlay`), `ExhaustivePatch`, `PersistPatch`, `PurgePatch` (+ `BetaExhaustivePatch`/`BetaPurgePatch`/`OldExhaustivePatch`/`OldPurgePatch` compat), `BetterConsoleAutocompletePatch`, `ModInteropPatch` (§7.7), `NPlayerHandStartCardPlayShortcutSafePatch`.
- `Patches/Audio`: `PlayResourcePatch` (`PlayResource`), `SetVolumePatches`.
- `Patches/Compatibility`: `MissingLocPatch`, `UnknownCharacterPatches` (unknown-character save tolerance), `OptionalFormNodePatch`* (SOURCE-ONLY).
- `BaseLib.Abstracts` nested patches (SHIPPED): `CustomPotionModel.ImagePatch`/`OutlinePatch`, `CustomEnchantmentModel.IconPatch`, `CustomActModel.CustomCreateMapPatch`/`CustomActBackgroundScenePath`/`CustomActMap{Top,Mid,Bot}BgPath`/`CustomActRestSiteBackgroundPath`/`CustomActGenerateBackgroundAssets`/`CustomActTreasureChest` (`NTreasureRoom._Ready`), `CustomEncounterModel.ScenePathPatch`/`GetCustomBackgroundAssets`/`ScenePatch`, `CustomBadgesPatch`/`BadgeIconGetterPatch`, `CustomCardPool{MarkAsSeen,Material}Patch`/`CustomRelicPoolMarkAsSeenPatch`/`CustomPotionPoolMarkAsSeenPatch`, `EnergyCounter{Path,OutlineColor,StarAnchor}Patch`, `CustomCharacterVisualPath`/`CustomCharacterVisuals`/`GenerateAnimatorPatch`/`GenerateAnimatorPatchMonster`, `CustomCharacterSelectBg`/`CustomCharacterSelectIconPath`... patches (the `Custom*Path` files under `dll BaseLib.Abstracts/` are prefixes on the corresponding engine getters).

### 8.8 Why it matters
The engine behaviours listed above are **already intercepted and therefore extensible through BaseLib's own abstractions**: model registration and ids (§1.2-1.3), shared pools, acts, custom piles, custom rewards, custom badges, character select, card frames/banners/portraits, power/relic/potion/orb icons, energy icons, HP-bar forecasts, save/load of extended data, net messages, scry-less hand-size and heal modifiers. Anything *not* in this list (card-rarity odds, act sequencing/next-act, StS1-style rest-site gating beyond `TryModifyRestSiteOptions`) is untouched engine surface — patch it yourself with a second `Harmony`, as our `MainFile` does.

---

## 9. Version skew table — types in v3.4.5 source but NOT in shipped v3.3.5

**All of these are unusable against the installed DLL.** A reader must never build against them. Count: **69 types** (computed by diffing the v3.4.5 source declarations against a full type dump of the shipped v3.3.5 binary; the prior audit's figure of 73 used a slightly different methodology and included a couple of names no longer present in the tree, e.g. `RelicCollectionTranspiler`).

### 9.1 Hook & scry machinery (SOURCE-ONLY)
`BaseLibHooks`, `HookUtils`, `IModifyScryAmount`, `IAfterScryed`, `ICardTypeTextModifier`, `ICustomTypeTextCard`, `IModifyResourceCostInCombat`, `IAfterSpendResource`, `HealthBarForecasts`, `HealthBarForecastSequenceBuilder`, `HealthBarForecastLaneBuilder`, `HealthBarForecastLeftOriginLayout`, `ScryVar`, `ScryResult`, `ScryCmd`.

### 9.2 Custom resource system (SOURCE-ONLY)
`CustomResource`, `BasicCustomResource`, `CustomResources`, `CustomResourceCost`, `ICustomResourceCost`, `ICustomCostVisualsHandler`, `ICustomResourceVisualsHandler`, `BasicCostVisualsHandler`, `BasicResourceVisualsHandler`, `ResourceHandler`, `CustomResourcePatches`, `CustomResourceUiPatches`.

### 9.3 Linked rewards (SOURCE-ONLY)
`CustomLinkedRewardSet`, `CustomLinkedRewardChoiceMessage`, `LinkedRewardType`, `SerializableCustomLinkedRewardSet`, `NCustomLinkedRewardSet`, `NCustomLinkedRewardSetPatches`, `NRewardButtonEvents`, `NRewardHighlight`, `RedirectNestedRewardSelection`, `BufferedCustomRewardMessage` (this one is in the shipped *decompile* under `RewardSynchronizerPatches` — it ships; the rest of this group does not).

### 9.4 Attribution / compendium / stats UI (SOURCE-ONLY)
`WhatMod`, `ModCredits` (+ its nested `enum Layout`), `BaseLibTip`, `ModSourceTooltip`, `CardTips`, `RelicTips`, `PowerTips`, `PotionTips`, `OrbTips`, `EnchantmentTips`, `AfflictionTips`, `MonsterSourceLabel`, `EventSourceLabel`, `AncientSourceLabel`, `NCreditsScreenPatch`, `CreditsNav`, `InRunCompendiumPatch`, `CharacterSelectStartingRelicsPatch`, `CustomCharacterStatsPatch`, `AddSubtypesToTypePlaquePatch`, `StartingRelicRowState`.

### 9.5 Misc (SOURCE-ONLY)
`GeneralExtensions`, `IComparableExtensions`, `PlayerCombatStateExtensions`, `ModifyDamageVars`, `ModifyExtraDamageVar`, `SkipSentryShutdownPatch`, `OptionalFormNodePatch`, `NativeFileDialogChrome`, `LargeImagePatch`.

### 9.6 Member-level drift (type ships, member does not)
| Type | Member | Status |
|---|---|---|
| `CustomCharacterModel` | `DefaultCompendiumOpenModelId` (`src Abstracts/CustomCharacterModel.cs:58`) | SOURCE-ONLY — absent from shipped v3.3.5 |
| `NodeFactory` | `UnregisterSceneType`, `HasFactory`, `IsRegistered` (`src docs/auto_conversion.md:98-102`) | SOURCE-ONLY — shipped has only the two `RegisterSceneType` overloads |
| `CustomActModel` | `ActNumber` property | SHIPPED but `[Obsolete]` — use `Index` |
| `CustomPotionModel` | `AutoAdd` property | SHIPPED but `[Obsolete]` — pass `autoAdd` to the constructor |
| `CustomSingletonModel` | `(bool receiveCombatHooks, bool receiveRunHooks)` ctor | SHIPPED but `[Obsolete]` — use `HookType` |

### 9.7 Everything else in this document is SHIPPED
The 30+ `Custom*Model` abstracts, all `ICustom*` interfaces (except §9.1/§9.2), the `*Loc` records, `CardModifier`, `CustomMessageWrapper`/`CustomTargetedMessageWrapper`, `NodeFactory`/`NodeFactory<T>` and all node factories, `MoveBuilder`, `CommonActions`, `MonsterActions`, `CustomAnimation`, `CustomBackgroundAssets`, `CustomCharacterUtils`, `CustomLocTableManager`, `SpireField` family, `WeightedList`, `OptionPools`/`AncientOption`, `FmodAudio`/`ModAudio`/`ModSound`, `CombatStateWrapper`, `BetaMainCompatibility`, `MultiPileCardSelect`, the three shipped `CustomReward` concretes, `CustomCalculatedVar` family, `ExhaustiveVar`/`PersistVar`/`RefundVar`/`DisplayVar`, `ModConfig`/`SimpleModConfig`/`ModConfigRegistry` + attributes, `BaseLibKeywords`, the `InstructionPatcher`/`AsyncMethodCall` patching suite, and the `CustomEnergyIconPatches`/`RelicImageOverridePatch`/`SceneConversionPatch`/`HealthBarForecastRegistry` support types.
