# JmcModLib API Reference — Slay the Spire 2

Interface reference for **JmcModLib 1.9.0** (`JMC-Mods/SlayTheSpire2_JmcModLib`), a third-party utility library for Slay the Spire 2. Target game: **StS2 v0.111.0** (Godot 4.5 / C# / .NET 9). JmcModLib ships **no content abstractions** (no card / relic / monster / encounter / character / act models). It provides: settings UI, reflection helpers, logging, secrets, persistence, version-compatibility shims, and a multi-version dispatch build toolchain.

## Sources of authority (per entry)

- **XML** — installed `JmcModLib.Runtime.xml` (workshop 3747526103; 602 `<member>` entries, the shipped IntelliSense surface). All `<summary>`/`<remarks>` prose is quoted verbatim from here; members **not** present in the XML are marked `no XML doc`.
- **BIN** — reflection metadata dump of the installed `JmcModLib.Runtime.dll` (assembly name `JmcModLib`, 90 exported types). Every signature is verified against this dump.
- **SRC** — upstream repo at `.tmp/jmc/` (v1.9.0 matches installed). Signatures are cited as `file:line`.

## Availability tags (CRITICAL)

| Tag | Meaning |
|---|---|
| `✓ binary-public` | present in the shipped DLL as public API — usable |
| `⚠ internal` / `⚠ protected` / `⚠ private` | documented (XML) but **not callable** from a consumer assembly |
| `✗ absent` | in source/XML but not in the shipped DLL |
| `+undoc` | public in the shipped DLL but **not** covered by the shipped XML |

Tally: 602 documented members → 565 `✓ binary-public`, 25 on `internal` types (`JmcModLib.Input.*` ×8 types + `MethodAccessor.ParamSignature`), 12 documented-but-non-public (4 protected, 1 internal method, 7 private).

## Contents

1. Consuming the library (manifest, MSBuild, loader split, registration)
2. `Config` and `Config.UI` (settings model + widget framework)
3. `UI.PauseMenu` and `Prefabs`
4. `Reflection` (`MethodAccessor`, `MemberAccessor`, `ReflectionAccessorBase`, `Utils.ExprHelper`)
5. `Utils.ModLogger`
6. `Security` (`SecretAttribute`, `JmcSecretOptions`, `JmcSecretSlot`)
7. `Persistence` (`JmcRunDataSlot` and siblings)
8. `Compat` (`ModCompat`, `MultiplayerCompat`)
9. `Multiplayer.OptionalNetworkFeature*`
10. The dispatch build toolchain
11. Full member index (all 602 documented members)

*This file is a reference, not an adoption verdict — judgements live in `research/`.*

---

# 1. Consuming the library

## 1.1 Manifest dependency (`JmcModLib.json`, installed)

```json
{
  "id": "JmcModLib",
  "name": "JmcModLib",
  "author": "JMC",
  "version": "1.9.0",
  "description": "JmcModLib for Slay the Spire 2.",
  "has_pck": true,
  "has_dll": true,
  "dependencies": [],
  "affects_gameplay": false,
  "min_game_version": "0.107.1"
}
```
A consumer mod declares the dependency in its own `*.json` manifest as `"dependencies": [{ "id": "JmcModLib", "min_version": "1.4.0" }]` — the default template the BuildTools generate (`BuildTools/Jmc.Sts2Mod.Build.targets`).

## 1.2 Runtime loading descriptor (`JmcModLib.runtime.config`, installed)

```json
{
  "runtimeAssembly": "JmcModLib.Runtime.dll",
  "initializerType": "JmcModLib.MainFile",
  "initializerMethod": "Initialize",
  "probeDirectories": [".", "lib", "libs"],
  "dependencies": ["Newtonsoft.Json.dll"],
  "probeAllDlls": true
}
```

## 1.3 Loader split

The game loads **`JmcModLib.dll`** (19 KB — a thin bootstrap named after the manifest). `BootstrapMain.Initialize` (`Bootstrap/BootstrapMain.cs:17`) installs a Linux Harmony fallback, reads the descriptor, installs an `AssemblyResolve` handler, loads dependencies, then loads **`JmcModLib.Runtime.dll`** (504 KB, assembly name `JmcModLib`) and reflects `JmcModLib.MainFile.Initialize` (`MainFile.cs:12`). Consumers reference **`JmcModLib.Runtime.dll`** only. `JmcModLib.pck` (975 KB) carries the settings-UI Godot scenes; `JmcModLib.Sts2.props` is the MSBuild reference entry point.

## 1.4 MSBuild integration (`JmcModLib.Sts2.props`, installed — verbatim)

```xml
<Project>
  <PropertyGroup>
    <JmcModLibPublishDir Condition="'$(JmcModLibPublishDir)' == ''">$(MSBuildThisFileDirectory)</JmcModLibPublishDir>
    <JmcModLibRoot Condition="'$(JmcModLibRoot)' == ''">$(JmcModLibPublishDir)</JmcModLibRoot>
    <JmcModLibRuntimePath Condition="'$(JmcModLibRuntimePath)' == ''">$(JmcModLibPublishDir)JmcModLib.Runtime.dll</JmcModLibRuntimePath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="JmcModLib">
      <HintPath>$(JmcModLibRuntimePath)</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```
Import from the installed mod directory, e.g. `<Import Project="$(Sts2Path)\mods\JmcModLib\JmcModLib.Sts2.props" />`. Build-time `.Dispatch.targets` is covered in §10.

## 1.5 Registration: `Core.ModRegistry` / `Core.RegistryBuilder`

XML `T:JmcModLib.Core.ModRegistry` summary (verbatim): *维护 STS2 子 MOD 与托管程序集之间的注册上下文，并分发生命周期事件。* ("Maintains the registration context between STS2 child mods and their managed assemblies, and dispatches lifecycle events.") Remarks (verbatim): *注册时会为目标程序集启用 JML 默认服务，包括按程序集隔离的 `ModLogger`、配置管理器和 Attribute 扫描管线。子 MOD 通常只需要在入口处调用一次泛型 `Register<MainFile>()`，即可自动完成上下文推断和 Attribute 扫描。* ("Registration enables JML default services for the target assembly: per-assembly `ModLogger`, config manager, and the attribute-scan pipeline. A child mod usually only needs one generic `Register<MainFile>()` call in its entry point.")

```csharp
// JmcModLib.Core.ModRegistry (static) — Core/Registry/ModRegistry.cs
public static event Action<ModContext>? OnRegistered;                 // :33
public static event Action<ModContext>? OnUnregistered;               // :38
public static RegistryBuilder Register(string modId, string? displayName = null, string? version = null, Assembly? assembly = null);           // :54
public static RegistryBuilder? Register(bool deferredCompletion, string modId, string? displayName = null, string? version = null, Assembly? assembly = null); // :89
public static RegistryBuilder? Register(bool deferredCompletion, object? modInfo, string? displayName = null, string? version = null, Assembly? assembly = null); // :121  (reads id/displayName/version from an object)
public static void Register<T>();                                     // :154
public static RegistryBuilder? Register<T>(bool deferredCompletion);  // :175
public static RegistryBuilder Register<T>(string modId, string? displayName = null, string? version = null); // :200
public static RegistryBuilder? Register<T>(bool deferredCompletion, string modId, string? displayName = null, string? version = null); // :214
public static bool IsRegistered(Assembly? assembly = null);           // :228
public static bool TryGetContext(out ModContext? context, Assembly? assembly = null); // :239
public static ModContext? GetContext(Assembly? assembly = null);      // :249
public static string GetModId(Assembly? assembly = null);             // :273
public static string GetDisplayName(Assembly? assembly = null);       // :284
public static string GetVersion(Assembly? assembly = null);           // :295
public static string GetTag(Assembly? assembly = null);               // :306
public static bool Unregister(Assembly? assembly = null);             // :318
```
XML `T:JmcModLib.Core.RegistryBuilder` summary (verbatim): *表示一次 MOD 注册过程中的链式补充设置。* ("Chainable supplemental settings for one mod registration.") Remarks: *所有补充设置完成后必须调用 `Done`，否则 Attribute 标记的配置、按钮和热键不会被扫描。* ("`Done()` MUST be called after all supplemental settings, or attribute-declared config, buttons and hotkeys will not be scanned.")

```csharp
// JmcModLib.Core.RegistryBuilder (sealed) — Core/Registry/RegistryBuilder.cs
public RegistryBuilder WithDisplayName(string displayName);                       // :43
public RegistryBuilder WithVersion(string version);                               // :54
public RegistryBuilder WithConfigStorage(IConfigStorage storage);                 // :75
public RegistryBuilder RegisterButton(out string key, string description, Action action,
    string buttonText = "按钮", string group = ConfigAttribute.DefaultGroup, string? storageKey = null,
    string? helpText = null, string? locTable = null, string? displayNameKey = null, string? helpTextKey = null,
    string? buttonTextKey = null, string? groupKey = null, int order = 0, UIButtonColor color = UIButtonColor.Default); // :112
public RegistryBuilder RegisterButton(string description, Action action, /* same tail defaults */); // :163
public RegistryBuilder RegisterSecret(out JmcSecretSlot slot, string key, JmcSecretOptions options); // :215
public RegistryBuilder RegisterSecret(JmcSecretSlot slot, string key, JmcSecretOptions options);     // :231
public RegistryBuilder RegisterSecret(string key, JmcSecretOptions options);                        // :246
public RegistryBuilder RegisterPauseMenuButton(string key, string text, Action action,
    int order = 0, PauseMenuButtonAnchor anchor = PauseMenuButtonAnchor.BeforeExitActions,
    string? locTable = null, string? textKey = null,
    Func<PauseMenuButtonContext, bool>? visibleWhen = null, Func<PauseMenuButtonContext, bool>? enabledWhen = null,
    bool closeMenuOnClick = false, UIButtonColor color = UIButtonColor.Default);                      // :267  (+ Action<PauseMenuButtonContext> :302, Func<Task> :337, Func<PauseMenuButtonContext,Task> :372)
public ModContext Done();                                                       // :399  (repeatable; first call completes + scans)
```

XML `T:JmcModLib.Core.ModContext` summary (verbatim): *描述一个已注册 MOD 的程序集、标识、显示名、版本和注册状态。* ("Describes a registered mod's assembly, id, display name, version and registration state.")

```csharp
// JmcModLib.Core.ModContext (sealed) — Core/Registry/ModContext.cs
public Assembly Assembly { get; }                                   // :22
public string ModId { get; internal set; }                          // :27
public string DisplayName { get; internal set; }                    // :32
public string Version { get; internal set; }                        // :37
public bool IsCompleted { get; internal set; }                      // :42
public string LoggerContext => $"{DisplayName} v{Version}";         // :47
public string Tag => $"[{DisplayName} v{Version}]";                 // :52
```
`Core.ModRuntime` and `Core.VersionInfo` are public in the binary (+undoc, no XML doc entries); see the authors' reference (`docs/JML_API_Reference_en.md` §2.2/§2.6) for their behavior. `JmcModLib.Core.VersionInfo` (SRC `Core/VersionInfo.cs:8-11`): `const string Name = "JmcModLib"`, `const string Version = "1.9.0"`, `string Tag`. `Core.ModRuntime` (SRC `Core/Runtime/ModRuntime.cs:8-85`): `Mod? TryGetLoadedMod(Assembly? = null)`, `ModManifest? TryGetManifest(...)`, `string? GetManifestId(...)`, `string GetPckName(...)`, `string GetDisplayName(...)`, `Version? GetLoadedVersion(...)`, `Mod? FindModById(string)`, `Mod? FindLoadedMod(string)`.

## 1.6 Attribute scanning: `Core.AttributeRouter`

XML `T:JmcModLib.Core.AttributeRouter.AttributeRouter` summary (verbatim): *Scans registered mod assemblies and routes discovered attributes to handlers.* Handlers are registered per attribute type and receive every matching `ReflectionAccessorBase`:

```csharp
// JmcModLib.Core.AttributeRouter.AttributeRouter (static) — Core/AttributeRouting/AttributeRouter.cs
public static event Action<Assembly>? AssemblyScanned;                              // :19
public static event Action<Assembly>? AssemblyUnscanned;                            // :21
public static bool IsInitialized { get; }                                           // :23
public static void Init();                                                          // :25
public static void Dispose();                                                       // :37
public static void RegisterHandler<TAttribute>(IAttributeHandler handler) where TAttribute : Attribute;              // :57
public static void RegisterHandler<TAttribute>(Action<Assembly, ReflectionAccessorBase, TAttribute> action) where TAttribute : Attribute; // :72
public static bool UnregisterHandler(IAttributeHandler handler);                    // :78
public static void ScanAssembly(Assembly assembly);                                 // :94
public static void UnscanAssembly(Assembly assembly);                               // :133
// interface IAttributeHandler — Core/AttributeRouting/IAttributeHandler.cs:10
//   void Handle(Assembly assembly, ReflectionAccessorBase accessor, Attribute attribute);
//   Action<Assembly, IReadOnlyList<ReflectionAccessorBase>>? Unregister { get; }
```
`SimpleAttributeHandler<TAttribute>` (same file `:260`, +undoc in binary) wraps an action handler; its `Unregister` is `null`.

**Use:** every custom monster/act/run feature of this project will register via `ModRegistry.Register<MainFile>()` or the builder; the attribute pipeline is how `[Config]`, `[UIButton]`, `[UIHotkey]`, `[PauseMenuButton]`, `[Secret]` and persistence attributes become live entries. There is no content-model API anywhere in this library.

---

# 2. `Config` and `Config.UI` (116 documented members — largest area)

The settings model: a static field/property marked `[Config]` (+ an optional `UIConfigAttribute` widget) is scanned after registration, becomes a `ConfigEntry`, gets a row in the in-game **Mod Settings** tab (pck scenes), and is persisted through an `IConfigStorage`. Values write back to the field/property immediately; `OnChanged` callbacks and `ValueChanged` events fire; `FlushOnSet` persists on each change.

## 2.1 `ConfigAttribute` — the entry marker

XML `T:JmcModLib.Config.ConfigAttribute` summary (verbatim): *Marks a static field or property as a configuration entry.* Only the ctor has an XML doc entry; properties are `+undoc` (public in binary, source `Config/ConfigAttribute.cs`).

```csharp
// JmcModLib.Config.ConfigAttribute (sealed : Attribute) — Config/ConfigAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]   // :9
public sealed class ConfigAttribute(string displayName, string? onChanged = null, string group = ConfigAttribute.DefaultGroup) : Attribute  // :10
{
    public const string DefaultGroup = "DefaultGroup";      // :15
    public string DisplayName { get; }                      // :17
    public string? OnChanged { get; }                       // :19  (static method name to call on change)
    public string Group { get; }                            // :21
    public string? Key { get; set; }                        // :23  (storage key; default = member name)
    public string? Description { get; set; }                // :25
    public string? LocTable { get; set; }                   // :27
    public string? DisplayNameKey { get; set; }             // :29
    public string? DescriptionKey { get; set; }             // :31
    public string? GroupKey { get; set; }                   // :33
    public int Order { get; set; }                          // :35
    public bool RestartRequired { get; set; }               // :37
    public static bool IsValidMethod(MethodInfo method, Type valueType, out LogLevel? level, out string? errorMessage); // :39 +undoc
}
```

## 2.2 `ConfigManager` — registration + persistence hub

XML `T:JmcModLib.Config.ConfigManager` summary (verbatim): *Central registration and persistence layer for config entries.* (No member has an XML doc entry; all `+undoc`.)

```csharp
// JmcModLib.Config.ConfigManager (static) — Config/ConfigManager.cs
public static bool FlushOnSet { get; set; } = true;                                   // :25
public static event Action<Assembly>? AssemblyRegistered;                             // :27
public static event Action<Assembly>? AssemblyUnregistered;                           // :29
public static event Action<ConfigEntry>? EntryRegistered;                             // :31
public static event Action<ConfigEntry, object?>? ValueChanged;                       // :33
public static bool IsInitialized { get; }                                             // :35
public static void Init();                                                            // :37
public static void Dispose();                                                         // :56
public static void SetStorage(IConfigStorage storage, Assembly? assembly = null);     // :73
public static IConfigStorage GetStorage(Assembly? assembly = null);                   // :80
public static string CreateStorageKey(Type declaringType, string memberName);         // :86
public static string CreateKey(string storageKey, string group = ConfigAttribute.DefaultGroup); // :91
public static void Flush(Assembly? assembly = null);                                  // :96
public static IReadOnlyCollection<ConfigEntry> GetEntries(Assembly? assembly = null); // :102
public static IEnumerable<ConfigEntry> GetEntries(string group, Assembly? assembly = null); // :110
public static IEnumerable<string> GetGroups(Assembly? assembly = null);               // :115
public static bool TryGetEntry(string key, [NotNullWhen(true)] out ConfigEntry? entry, Assembly? assembly = null); // :127
public static object? GetValue(string key, Assembly? assembly = null);                // :141
public static bool SetValue(string key, object? value, Assembly? assembly = null);    // :146
public static void ResetAssembly(Assembly? assembly = null);                          // :151
public static string RegisterConfig<TValue>(string displayName, Func<TValue> getter, Action<TValue> setter,
    string group = ConfigAttribute.DefaultGroup, Action<TValue>? onChanged = null, UIConfigAttribute? uiAttribute = null,
    string? storageKey = null, string? locTable = null, string? displayNameKey = null, string? groupKey = null,
    string? description = null, string? descriptionKey = null, int order = 0, bool restartRequired = false,
    Assembly? assembly = null);                                                       // :171
public static string RegisterButton(string description, Action action, string buttonText = "按钮",
    string group = ConfigAttribute.DefaultGroup, Assembly? assembly = null, string? storageKey = null,
    string? helpText = null, string? locTable = null, string? displayNameKey = null, string? helpTextKey = null,
    string? buttonTextKey = null, string? groupKey = null, int order = 0, UIButtonColor color = UIButtonColor.Default); // :235
public static void Unregister(Assembly? assembly = null);                             // :279
```

## 2.3 Entries and storage

XML `T:JmcModLib.Config.Entry.ConfigEntry` summary (verbatim): *Base class for a single registered config entry.* Its public ctor is `⚠ protected` (primary ctor, `Config/Entry/ConfigEntry.cs:13`); use `ConfigEntry<TValue>` (binary `+undoc`) or `ConfigManager.RegisterConfig<TValue>`.

```csharp
// JmcModLib.Config.Entry.ConfigEntry (abstract) — Config/Entry/ConfigEntry.cs
public Assembly Assembly { get; }                                  // :21
public string StorageKey { get; }                                  // :23
public string Group { get; }                                       // :25
public string DisplayName { get; }                                 // :27
public string Key => CreateKey(StorageKey, Group);                 // :29
public ConfigAttribute Attribute { get; }                          // :31
public UIConfigAttribute? UIAttribute { get; }                     // :33
public UIDropdownOptionsProviderAttribute? DropdownOptionsProviderAttribute { get; internal set; } // :38
public UIVisibleWhenAttribute? VisibleWhenAttribute { get; internal set; }                         // :43
public Type? SourceDeclaringType { get; internal set; }            // :45
public string? SourceMemberName { get; internal set; }             // :47
public abstract Type ValueType { get; }                            // :49
public abstract object? DefaultValue { get; }                      // :51
public abstract object? GetValue();                                // :53
public abstract void SetValue(object? value);                      // :55
public abstract bool Reset();                                      // :57
public event Action<ConfigEntry, object?>? ValueChanged;           // :59
public static string CreateStorageKey(Type declaringType, string memberName);  // :61
public static string CreateKey(string storageKey, string group = ConfigAttribute.DefaultGroup); // :68
```
```csharp
// JmcModLib.Config.Entry.ConfigEntry<TValue> (binary +undoc) — Config/Entry/ConfigEntry.cs:85-…
public sealed class ConfigEntry<TValue> : ConfigEntry   // (primary ctor: assembly, storageKey, group, displayName, defaultValue, getter, setter, onChanged, attribute, uiAttribute)
{
    public TValue GetTypedValue();   public void SetTypedValue(TValue value);
    public TValue DefaultValueTyped { get; }
    public override Type ValueType { get; }   public override object? DefaultValue { get; }
    public override object? GetValue();       public override void SetValue(object? value);
    public override bool Reset();
}
// JmcModLib.Config.Entry.ButtonEntry (binary +undoc) — Config/Entry/ButtonEntry.cs:30-133
public string ButtonText { get; }  public string? ButtonTextKey { get; }  public UIButtonColor Color { get; }
public override Type ValueType => typeof(void);  public override object? DefaultValue => null;
public void Invoke();   // runs the registered Action
```

Storage backends implement `IConfigStorage` (`Config/Storage/IConfigStorage.cs:5`): `string GetFileName(Assembly?)`, `string GetFilePath(Assembly?)`, `bool Exists(Assembly?)`, `void Save(string key, string group, object? value, Assembly?)`, `bool TryLoad(string key, string group, Type valueType, out object? value, Assembly?)`, `void Flush(Assembly?)`. Two shipped backends (both `✓ binary-public`):

- `JsonConfigStorage` — XML summary (verbatim): *Dependency-free JSON storage backend for mod configuration files.* (`Config/Storage/JsonConfigStorage.cs:13`)
- `NewtonsoftConfigStorage` — XML summary (verbatim): *Newtonsoft.Json based storage backend for mod configuration files.* (`Config/Storage/NewtonsoftConfigStorage.cs:15`) — **the default**; bundled `Newtonsoft.Json.dll` is loaded by the bootstrap.

Both constructors take `string? rootDirectory = null`; the default root resolves to the game user-data directory. Persistence shape: `{ "groups": { "<Group>": { "<StorageKey>": <value> } } }`; the file name derives from the mod id (e.g. `<ModId>.json` in the config root). `Godot.Color` values serialize as hex via `JmcColorValue.ToHex` (alpha by default).

## 2.4 Widget framework — UI attributes (`Config.UI`)

The public surface is the **attribute layer**; the Godot widget controls themselves (`JmcSettingsButton`, `JmcSettingsDropdown`, `JmcSettingsSlider`, `JmcSettingsTickbox`, `JmcColorPickerEditor`, `JmcKeybindButton`, `JmcKeybindInputRelay`, `ModSettingsPanel`, `SettingsUiTemplates`, `JmcSettingsHoverTips`, …) are all `internal` in the binary (`Config/UI/Controls/*`, `Panels/*`) — you declare with attributes, you never touch controls. The Mod Settings tab is injected by `ModConfigUiBridge`/`ModSettingsTabBridge` (internal) into the native settings screen; `JmcModLib.pck` supplies the cloned templates.

```csharp
// Config/UI/Attributes/ConfigUiAttribute.cs
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UIButtonAttribute(string description, string buttonText = "按钮", string group = ConfigAttribute.DefaultGroup) : Attribute  // :11
{
    public string Description { get; }          public string ButtonText { get; }    public string Group { get; }
    public string? Key { get; set; }            public string? LocTable { get; set; }
    public string? DisplayNameKey { get; set; } public string? DescriptionKey { get; set; }
    public string? ButtonTextKey { get; set; }  public string? GroupKey { get; set; }
    public UIButtonColor Color { get; set; } = UIButtonColor.Default;   // :34
    public int Order { get; set; }              public string? HelpText { get; set; }
    public static bool IsValidMethod(MethodInfo method, out LogLevel? level, out string? errorMessage); // :40 +undoc
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public abstract class UIConfigAttribute : Attribute { public virtual bool IsValid(Type valueType, object? defaultValue, out string? errorMessage); } // :76-79
public abstract class UIConfigAttribute<TValue> : UIConfigAttribute { }   // :85 — enforces valueType == typeof(TValue)
public sealed class UIToggleAttribute : UIConfigAttribute<bool> { }       // :101  (empty)
public sealed class UIKeybindAttribute(bool allowController = false, bool allowKeyboard = true) : UIConfigAttribute { } // :110
public sealed class UIInputAttribute(int characterLimit = 0, bool multiline = false) : UIConfigAttribute<string>        // :156
{ public int CharacterLimit { get; } public bool Multiline { get; } }
public enum UIColorPalette { None, Basic, Game, CardRarity, Rainbow }     // :163 (+undoc)
public sealed class UIColorAttribute(params string[] presets) : UIConfigAttribute<Color>  // :172 (+undoc)
{ public string[] Presets { get; } public UIColorPalette Palette { get; set; } = UIColorPalette.Game;
  public bool AllowCustom { get; set; } = true; public bool AllowAlpha { get; set; } = true; }
public interface ISliderConfigAttribute { double Min { get; } double Max { get; } double Step { get; } } // :185 (+undoc)
public sealed class UISliderAttribute(double min, double max, double step = 1.0) : UIConfigAttribute, ISliderConfigAttribute // :192 (+undoc)
{ public double Min { get; } public double Max { get; } public double Step { get; } }
public sealed class UIIntSliderAttribute(int min, int max, int characterLimit = 5) : UIConfigAttribute<int>, ISliderConfigAttribute // :241 (+undoc)
{ public int CharacterLimit { get; } public double Min { get; } public double Max { get; } public double Step { get; } = 1.0; }
public sealed class UIDropdownAttribute(params string[]? exclude) : UIConfigAttribute  // :271 (+undoc)
{ public IReadOnlyList<string> Options { get; } public IReadOnlyList<string> Exclude { get; } }  // string => static options; enum => exclusions
```

XML `T:JmcModLib.Config.UI.UIButtonAttribute` summary (verbatim): *Adds a button row to the in-game mod settings UI.* `T:UIConfigAttribute`: *Base metadata attribute for later in-game config UI bridging.* `T:UIKeybindAttribute`: *将 `Godot.Key` 或 `JmcKeyBinding` 配置项渲染为按键绑定控件。* ("Renders a `Godot.Key`/`JmcKeyBinding` config entry as a keybind control.")

> **Availability note:** `UISliderAttribute`, `UIIntSliderAttribute`, `UIDropdownAttribute`, `UIToggleAttribute`, `UIInputAttribute`, `UIColorAttribute`, `UIColorPalette`, `ISliderConfigAttribute`, `UIConfigAttribute<TValue>` are all **present and public in the shipped DLL** but have **no XML doc entries** (`+undoc`). The shipped XML documents only `UIConfigAttribute` among the widget base types.

## 2.5 Dynamic UI: dropdown providers, visibility, runtime context

XML `T:JmcModLib.Config.UI.UIDropdownOptionsProviderAttribute` summary (verbatim): *指定下拉配置项的运行时候选项提供器。* ("Declares a runtime options provider for a dropdown config entry.") Remarks: *该 Attribute 应与 `UIDropdownAttribute` 配合使用。提供器可以是同一配置类型中的静态方法或静态属性；方法可以不带参数，也可以接收一个 `IConfigUiContext` 参数用于读取当前 MOD 的其他配置项。* ("Use with `UIDropdownAttribute`. The provider is a static method or static property on the same config type; a method may take no parameters or one `IConfigUiContext` to read the mod's other config entries.")

```csharp
// Config/UI/Attributes/UIDropdownOptionsProviderAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UIDropdownOptionsProviderAttribute(string providerName) : Attribute            // :11
{
    public UIDropdownOptionsProviderAttribute(string providerName, params string[] dependsOn);     // :28
    public string ProviderName { get; }                                                            // :37
    public string[] DependsOn { get; set; } = [];                                                  // :42
    public UIDropdownInvalidValuePolicy InvalidValuePolicy { get; set; } = UIDropdownInvalidValuePolicy.KeepCurrent; // :47
}
public enum UIDropdownInvalidValuePolicy { KeepCurrent, SelectFirstAvailable, ResetToDefault }    // :53 (binary values 0,1,2)
```
XML `T:UIDropdownInvalidValuePolicy` summary (verbatim): *动态下拉候选项变化后，当前值不再存在时的处理策略。* ("Policy when the current value vanishes after the dynamic dropdown options change.")

XML `T:JmcModLib.Config.UI.UIVisibleWhenAttribute` summary (verbatim): *指定配置项在设置 UI 中何时显示。* ("Specifies when a config entry is shown in the settings UI.") Remarks: *该 Attribute 只影响 UI 中的显示状态，不影响配置项注册、读取、写入或持久化。未声明该 Attribute 的配置项会保持默认行为：始终显示。* ("UI display only — registration, read/write and persistence are unaffected. Without it, entries are always visible.")

```csharp
// Config/UI/Attributes/UIVisibleWhenAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UIVisibleWhenAttribute : Attribute
{
    public UIVisibleWhenAttribute(string dependsOn);                                  // :17  (bool true)
    public UIVisibleWhenAttribute(string dependsOn, bool expectedValue);              // :27
    public UIVisibleWhenAttribute(string dependsOn, string expectedValue);            // :37  (enum names resolved)
    public UIVisibleWhenAttribute(string dependsOn, int expectedValue);               // :47
    public UIVisibleWhenAttribute(string dependsOn, double expectedValue);            // :57
    public string DependsOn { get; }                                                  // :72
    public object ExpectedValue { get; }                                              // :77
    public bool Invert { get; set; }                                                  // :82
    public bool IgnoreCase { get; set; } = true;                                      // :87
}
```
XML `T:JmcModLib.Config.UI.IConfigUiContext` summary (verbatim): *配置 UI 运行时上下文，用于动态候选项或动态 UI 状态判断读取当前 MOD 的其他配置项。* ("Runtime context for reading other entries of the current mod, used by dynamic dropdowns and dynamic UI state.") Interface: `T Get<T>(string key)`, `bool TryGet<T>(string key, out T value)`, `object? Get(string key)`, `bool TryGet(string key, out object? value)` (`Config/UI/State/ConfigUiContext.cs`).

XML `T:JmcModLib.Config.UI.UIButtonColor` summary (verbatim): *JML 按钮控件可复用的颜色风格。* ("Reusable colour styles for JML button controls.") Enum (binary): `Default=0, Green=1, Red=2, Gold=3, Blue=4, Reset=5` (`Config/UI/Attributes/UIButtonColor.cs`).

## 2.6 Hotkeys and keybindings (`Config.UI`)

XML `T:JmcModLib.Config.UI.JmcHotkeyAttribute` summary (verbatim): *将一个静态无参方法绑定到已有的热键配置成员。* ("Binds a static parameterless method to an existing hotkey config member.") XML `T:JmcModLib.Config.UI.UIHotkeyAttribute` summary (verbatim): *创建一个可在设置界面修改的热键项，并将其绑定到静态无参方法。* ("Creates a hotkey entry editable in the settings UI and binds it to a static parameterless method.")

```csharp
// Input/Hotkeys/HotkeyAttributes.cs  (namespace JmcModLib.Config.UI)
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class JmcHotkeyAttribute(string bindingMember) : Attribute   // :12
{
    public string BindingMember { get; }              // :17  static field/property name holding the binding
    public string? Key { get; set; }                  // :22  runtime key; auto from method name when empty
    public bool ConsumeInput { get; set; } = true;    // :27
    public bool ExactModifiers { get; set; } = true;  // :32
    public bool AllowEcho { get; set; }               // :37
    public ulong DebounceMs { get; set; } = 150;      // :42
}
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UIHotkeyAttribute(string displayName, string group = ConfigAttribute.DefaultGroup) : Attribute  // :51
{
    public string DisplayName { get; }   public string Group { get; }
    public string? Key { get; set; }     public string? Description { get; set; }
    public string? LocTable { get; set; } public string? DisplayNameKey { get; set; }
    public string? DescriptionKey { get; set; } public string? GroupKey { get; set; }
    public int Order { get; set; }       public bool RestartRequired { get; set; }
    public Key DefaultKeyboard { get; set; } = Godot.Key.None;   // :108
    public JmcKeyModifiers DefaultModifiers { get; set; }        // :113
    public string? DefaultController { get; set; }               // :117 (Steam Input action name)
    public bool AllowKeyboard { get; set; } = true;  public bool AllowController { get; set; }
    public bool ConsumeInput { get; set; } = true;   public bool ExactModifiers { get; set; } = true;
    public bool AllowEcho { get; set; }              public ulong DebounceMs { get; set; } = 150;
}
```

XML `T:JmcModLib.Config.UI.JmcHotkeyManager` summary (verbatim): *JML 运行时热键分发器，用于处理 MOD 自有的可配置热键。* ("JML runtime hotkey dispatcher for a mod's own configurable hotkeys.")

```csharp
// JmcModLib.Config.UI.JmcHotkeyManager (static) — Input/Hotkeys/JmcHotkeyManager.cs
public static bool IsInitialized { get; }                                              // :29
public static void Init();                                                             // :34
public static void Register(string key, Func<JmcKeyBinding> bindingGetter, Action action,
    bool consumeInput = true, bool exactModifiers = true, bool allowEcho = false, ulong debounceMs = 150, Assembly? assembly = null); // :56
public static void Register(string key, Func<Key> keyGetter, Action action,
    bool consumeInput = true, bool exactModifiers = true, bool allowEcho = false, ulong debounceMs = 150, Assembly? assembly = null); // :90
public static bool Unregister(string key, Assembly? assembly = null);                  // :118
public static void UnregisterAssembly(Assembly? assembly = null);                      // :132
```

XML `T:JmcModLib.Config.UI.HotkeyOptions` summary (verbatim): *运行时热键的触发选项。* ("Runtime hotkey trigger options.") — `public readonly record struct HotkeyOptions(bool ConsumeInput = true, bool ExactModifiers = true, bool AllowEcho = false, ulong DebounceMs = 150)` (`JmcHotkeyManager.cs:394`).

XML `T:JmcModLib.Config.UI.JmcKeyBinding` summary (verbatim): *表示由 MOD 自己持有的热键绑定，不会直接写入游戏原生输入命令表。* ("A hotkey binding owned by the mod; never written into the game's native input command table.") XML `T:JmcModLib.Config.UI.JmcKeyModifiers` summary (verbatim): *键盘热键使用的修饰键组合。* ("Modifier-key combinations for keyboard hotkeys.") `[Flags] enum JmcKeyModifiers { None=0, Ctrl=1, Shift=2, Alt=4, Meta=8 }` (`Input/Hotkeys/JmcKeyBinding.cs:9`).

```csharp
// JmcModLib.Config.UI.JmcKeyBinding (public readonly record struct) — Input/Hotkeys/JmcKeyBinding.cs
public JmcKeyBinding();                                                   // :71
public JmcKeyBinding(Key keyboard);                                       // :80
public JmcKeyBinding(Key keyboard = Key.None, string controller = "", JmcKeyModifiers modifiers = JmcKeyModifiers.None, bool enabled = true); // :92
public JmcKeyBinding(Key keyboard, string controller, JmcKeyModifiers modifiers);     // :110
public JmcKeyBinding(Key keyboard, JmcKeyModifiers modifiers, bool enabled = true);   // :121
public Key Keyboard { get; init; }                                        // :47
public string Controller { get; init; }                                   // :52  (Steam Input / Godot action name)
public JmcKeyModifiers Modifiers { get; init; }                           // :57
public bool Enabled { get; init; }                                        // :62
public bool HasKeyboard => Keyboard != Key.None;                          // :129
public bool HasModifiers => Modifiers != JmcKeyModifiers.None;            // :134
public bool HasController => !string.IsNullOrWhiteSpace(Controller);      // :139
public JmcKeyBinding WithKeyboard(Key keyboard);                          // :146
public JmcKeyBinding WithKeyboard(Key keyboard, JmcKeyModifiers modifiers); // :157
public JmcKeyBinding WithController(string? controller);                  // :171
public JmcKeyBinding WithEnabled(bool enabled);                           // :181
public bool IsPressed(InputEvent inputEvent, bool allowEcho = false, bool exactModifiers = true); // :193
public bool IsReleased(InputEvent inputEvent);                            // :221
public bool IsDown(bool exactModifiers = true);                           // +undoc
public static bool IsPressed(Key keyboard, InputEvent inputEvent, bool allowEcho = false); // +undoc
public static bool IsReleased(Key keyboard, InputEvent inputEvent);       // +undoc
public static JmcKeyModifiers ReadModifiers(InputEventKey keyEvent);      // +undoc
public static JmcKeyModifiers ReadCurrentModifiers();                     // +undoc
public static bool IsModifierKey(Key key);                                // +undoc
public static Key ReadKey(InputEventKey keyEvent);                        // +undoc
public string ToKeyboardText();                                           // +undoc
public static implicit operator JmcKeyBinding(Key keyboard);              // :270  ✓ binary-public (XML documents op_Implicit)
```

The runtime input dispatch layer (`JmcHotkeyInputRelay`, `JmcInputManager`, `JmcInputActionRegistry`, `SteamInputBackend`, `IJmcInputBackend`, `GodotActionInputBackend`, `JmcSteamInputManifestInstaller`, `SteamInputManifestMerger`, `SteamInputPatches`) is **`internal`** in source and **not exported** by the DLL (`Input/*.cs`); the XML documents those 8 types anyway → **`⚠ internal`, unusable from a consumer assembly**. Steam Input is driven implicitly by `[UIHotkey]`/`JmcKeyBinding` with `AllowController`; a merged Steam Input manifest is generated and installed before `SteamInput.Init`.

## 2.7 Declaring and showing a settings page (end to end)

1. Mark a static field/property `[Config("Display Name", group: "MyGroup")]` + a widget attribute (`[UISlider(0, 100)]`, `[UIIntSlider]`, `[UIDropdown]`, `[UIToggle]`, `[UIKeybind]`, `[UIColor]`, `[UIInput]`); mark a static method `[UIButton("Description")]` or `[UIHotkey("Display Name")]`.
2. Register: `ModRegistry.Register<MainFile>()` (or builder + `.Done()`). The attribute router scans the assembly (`ConfigAttributeHandler`, `Config/ConfigAttributeHandler.cs:13`) and creates `ConfigEntry` / `ButtonEntry` / hotkey registrations.
3. The game's Mod Settings screen shows a **Mod Settings** tab whenever registered mods have entries; `ModSettingsPanel` (internal) renders per-group rows, hover tips (`JmcSettingsHoverTips.Attach`), and per-mod reset.
4. Values persist through the default `NewtonsoftConfigStorage` (or `JsonConfigStorage`); `ConfigManager.FlushOnSet = true` writes on every change; `RestartRequired` entries surface the restart banner (`GameRestart`, §5) plus the Modding-screen restart-button patch (`Config/UI/Bridge/ModdingScreenRestartButtonPatch.cs`).

---

# 3. `UI.PauseMenu` and `Prefabs`

## 3.1 Pause-menu injection (`UI.PauseMenu`, 46 documented members)

XML `T:JmcModLib.UI.PauseMenu.PauseMenuButtonAttribute` summary (verbatim): *将静态方法声明为运行中暂停菜单里的按钮条目。* ("Declares a static method as a button entry in the in-run pause menu.") Remarks: supported signatures are `void M()`, `void M(PauseMenuButtonContext)`, `Task M()`, `Task M(PauseMenuButtonContext)`.

```csharp
// UI/PauseMenu/PauseMenuButtonAttribute.cs
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PauseMenuButtonAttribute(string text) : Attribute   // :20
{
    public string Text { get; }                                       // :25
    public string? Key { get; set; }                                  // :30  auto from method name when empty
    public PauseMenuButtonAnchor Anchor { get; set; } = PauseMenuButtonAnchor.BeforeExitActions; // :35
    public int Order { get; set; }                                    // :40
    public string? LocTable { get; set; }                             // :45
    public string? TextKey { get; set; }                              // :50
    public bool CloseMenuOnClick { get; set; }                        // :55
    public UIButtonColor Color { get; set; } = UIButtonColor.Default; // :60
}
```
XML `T:JmcModLib.UI.PauseMenu.PauseMenuButtonAnchor` summary (verbatim): *暂停菜单按钮相对于原生按钮的插入锚点。* ("Insertion anchor for pause-menu buttons relative to native buttons.") Enum (binary): `AfterResume=0, AfterSettings=1, AfterCompendium=2, BeforeExitActions=3, End=4`.

XML `T:JmcModLib.UI.PauseMenu.PauseMenuButtonOptions` summary (verbatim): *描述一个暂停菜单按钮的显示、排序、本地化与运行时状态选项。* ("Display, ordering, localization and runtime-state options of a pause-menu button.")

```csharp
// UI/PauseMenu/PauseMenuButtonOptions.cs (sealed)
public sealed class PauseMenuButtonOptions { public PauseMenuButtonOptions(); public PauseMenuButtonOptions(string key, string text); }  // :13,:22
{   public string? Key { get; set; }   public string? Text { get; set; }
    public PauseMenuButtonAnchor Anchor { get; set; } = PauseMenuButtonAnchor.BeforeExitActions;  // :41
    public int Order { get; set; }     public string? LocTable { get; set; }  public string? TextKey { get; set; }
    public Func<PauseMenuButtonContext, bool>? VisibleWhen { get; set; }     // :61
    public Func<PauseMenuButtonContext, bool>? EnabledWhen { get; set; }     // :66
    public bool CloseMenuOnClick { get; set; }   public UIButtonColor Color { get; set; } = UIButtonColor.Default; }
```
XML `T:JmcModLib.UI.PauseMenu.PauseMenuButtonContext` summary (verbatim): *暂停菜单按钮在可见性判断、启用判断和点击回调中使用的上下文。* ("Context used by visibility checks, enabled checks and click callbacks.") Remarks: *普通 MOD 通常只需要读取运行状态属性。`Menu` 和 `Button` 暴露的是原生节点，修改它们可能影响暂停菜单行为，请仅在确有需要时使用。* ("Most mods only read the run-state properties. `Menu`/`Button` are native nodes — mutating them can affect pause-menu behaviour.")

```csharp
// UI/PauseMenu/PauseMenuButtonContext.cs (sealed)
public ModContext Mod { get; }          // :39
public Assembly Assembly { get; }       // :44
public IRunState? RunState { get; }     // :49  (game's IRunState; may be null on some lifecycle refreshes)
public NPauseMenu Menu { get; }         // :54  (native node)
public NButton Button { get; }          // :59  (native node)
public bool IsMultiplayerClient { get; }// :64
public bool IsRunInProgress { get; }    // :69
public bool IsGameOver { get; }         // :74
```
XML `T:JmcModLib.UI.PauseMenu.PauseMenuRegistry` summary (verbatim): *管理子 MOD 注册到运行中暂停菜单的按钮条目。* ("Manages button entries registered to the in-run pause menu by child mods.") Remarks: identity is *assembly + button key*; re-registering the same key replaces the entry; different assemblies can share a key.

```csharp
// UI/PauseMenu/PauseMenuRegistry.cs (static)
public static void RegisterButton(PauseMenuButtonOptions options, Action action, Assembly? assembly = null);                    // :43
public static void RegisterButton(PauseMenuButtonOptions options, Action<PauseMenuButtonContext> action, Assembly? assembly = null); // :59
public static void RegisterButton(PauseMenuButtonOptions options, Func<Task> action, Assembly? assembly = null);                // :75
public static void RegisterButton(PauseMenuButtonOptions options, Func<PauseMenuButtonContext, Task> action, Assembly? assembly = null); // :87
public static bool UnregisterButton(string key, Assembly? assembly = null);                                                      // :102
public static void UnregisterAssembly(Assembly? assembly = null);                                                                // :114
public static IReadOnlyCollection<PauseMenuButtonOptions> GetEntries(Assembly? assembly = null);                                 // :125
```
Injection itself is a Harmony patch (`UI/PauseMenu/PauseMenuBridge.cs` — `internal`): a postfix on `NPauseMenu` open/refresh calls `PauseMenuBridge.Refresh(menu, runState, scheduleDeferred: true)`, which builds/clones buttons from registered entries (anchor-sorted). `RegistryBuilder.RegisterPauseMenuButton(...)` (§1.5) is the builder-path equivalent.

**Use:** custom acts / run-flow extensions of this project can add "Abandon run", "Restart", or act-specific actions into the in-run pause menu; `VisibleWhen`/`EnabledWhen` give per-run-state control (e.g. only during Act 3).

## 3.2 Prefabs (53 documented members; popups over native modal container)

All popups render through the game's native `NModalContainer`; `IsAvailable` means the modal container exists and has no open modal.

XML `T:JmcModLib.Prefabs.JmcConfirmationPopup` summary (verbatim): *通过游戏原生的 `NGenericPopup` 与 `NModalContainer` 显示通用弹窗。* ("Shows generic popups through the game's native `NGenericPopup` and `NModalContainer`.")

```csharp
// JmcModLib.Prefabs.JmcConfirmationPopup (static) — Prefabs/JmcConfirmationPopup.cs
public static bool IsAvailable { get; }                    // :18  (+undoc)
public static Task<bool> ShowConfirmationAsync(string title, string body, string? confirmText = null, string? cancelText = null, bool showBackstop = true, Assembly? assembly = null); // :31
public static Task<bool> ShowMessageAsync(string title, string body, string? okText = null, bool showBackstop = true, Assembly? assembly = null);                                  // :67
public static Task<bool> ShowConfirmationAsync(LocString title, LocString body, LocString? confirmText = null, LocString? cancelText = null, bool showBackstop = true, Assembly? assembly = null); // :101
public static Task<bool> ShowMessageAsync(LocString title, LocString body, LocString? okText = null, bool showBackstop = true, Assembly? assembly = null);                            // :137
```
(XML summaries: *显示原生双按钮确认弹窗。* / *显示只有一个确认按钮的原生提示弹窗。* with LocString variants prefixed *使用本地化文本…*; result `true` = confirm pressed, `false` = cancel/close/unavailable.)

XML `T:JmcModLib.Prefabs.JmcReportPopup` summary (verbatim): *通过游戏模态容器显示适合长文本、诊断报告和日志摘要的可滚动报告弹窗。* ("Shows a scrollable report popup for long text, diagnostic reports and log summaries via the game modal container.")

```csharp
// JmcModLib.Prefabs.JmcReportPopup (static) — Prefabs/JmcReportPopup.cs
public static bool IsAvailable { get; }                       // :27
public static JmcReportPopupHandle? Open(JmcReportPopupOptions options, Assembly? assembly = null); // :36  (null when container busy/invalid)

public enum JmcReportPopupBodyFormat { PlainText = 0, RichText = 1, Markdown = 2 }   // :113  (Markdown renders the body)

public sealed class JmcReportPopupOptions                      // :134
{   public required string Title { get; init; }                                     // :139
    public string Body { get; init; } = string.Empty;                               // :144  (plain text by default; [ ] not rich-text)
    public string? Subtitle { get; init; }                                          // :149
    public string? Status { get; init; }                                            // :154
    public JmcReportPopupBodyFormat BodyFormat { get; init; } = JmcReportPopupBodyFormat.PlainText; // :159
    public bool BodyUsesRichText { get; init; }                                     // :164  (legacy; use BodyFormat)
    public IReadOnlyList<JmcReportPopupButton> Buttons { get; init; } = Array.Empty<JmcReportPopupButton>(); // :169  (auto Close button when empty)
    public bool ShowBackstop { get; init; } = true;                                 // :174
    public bool CloseOnEscape { get; init; } = true;                                // :179
    public Vector2 MinimumSize { get; init; } = new(940f, 700f);                    // :184 }

public sealed class JmcReportPopupButton                          // :190
{   public JmcReportPopupButton(string key, string text, Action<JmcReportPopupHandle>? action = null, bool closeOnClick = false, bool enabled = true); // :200
    public string Key { get; }   public string Text { get; }   public Action<JmcReportPopupHandle>? Action { get; }
    public bool CloseOnClick { get; }   public bool Enabled { get; } }

public sealed class JmcReportPopupHandle                          // :263  (live handle; no-ops once closed)
{   public bool IsOpen { get; }                                                       // :272
    public void SetTitle(string title);                                              // :278
    public void SetSubtitle(string? subtitle);                                        // :287  (null/blank hides)
    public void SetStatus(string? status);                                            // :296
    public void SetBody(string body);                                                 // :305
    public void SetBody(string body, bool bodyUsesRichText);                          // :314
    public void SetBody(string body, JmcReportPopupBodyFormat bodyFormat);            // :323
    public void SetButtonEnabled(string key, bool enabled);                           // :333
    public void Close();                                                              // :343 }
```

XML `T:JmcModLib.Prefabs.JmcSecretInputPopup` summary (verbatim): *通过游戏模态容器显示 Secret 输入框。* ("Shows a secret input box via the game modal container.")

```csharp
// JmcModLib.Prefabs.JmcSecretInputPopup — Prefabs/JmcSecretInputPopup.cs
public static bool IsAvailable { get; }                        // :71
public static Task<string?> PromptAsync(JmcSecretInputPopupOptions options, Assembly? assembly = null); // :80 (null = cancelled/closed/unavailable)

public sealed class JmcSecretInputPopupOptions                  // :15
{   public required string Title { get; init; }   public string? Description { get; init; }
    public string? Placeholder { get; init; }     public string? ConfirmText { get; init; }
    public string? CancelText { get; init; }      public string? EmptyText { get; init; }
    public JmcSecretProtectionLevel ProtectionLevel { get; init; }   // :50  shown as risk hint
    public bool ShowBackstop { get; init; } = true;               // :55
    public Vector2 MinimumSize { get; init; } = new(720f, 360f);  // :60 }
```

**Use:** the report popup is ideal for diagnostics/log dumps from this project (Markdown body, live-update handle); the secret popup pairs with §6 for API tokens. `JmcReportPopup` is the only Markdown renderer JML ships.

---

# 4. `Reflection` (80 documented members; general-purpose, most reusable)

XML `T:JmcModLib.Reflection.ReflectionAccessorBase` summary (verbatim): *所有访问器的基类* ("Base class of all accessors"). `T:ReflectionAccessorBase``2` (i.e. ``ReflectionAccessorBase`2``): *MemberAccessor 和 MethodAccessor 的派生基类* ("Derived base class of MemberAccessor and MethodAccessor").

```csharp
// JmcModLib.Reflection.ReflectionAccessorBase — Reflection/ReflectionAccessorBase.cs
public abstract class ReflectionAccessorBase
{
    public const BindingFlags DefaultFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic; // :14  (no inheritance)
    public abstract string Name { get; }                  // :21
    public abstract Type DeclaringType { get; }           // :26
    public virtual bool IsStatic { get; protected set; }  // :31
    public static bool IsSaveOwner(Type? declaringType);  // :38  (visible + not a compiler-generated owner)
    public T? GetAttribute<T>() where T : Attribute;      // :62
    public bool HasAttribute<T>() where T : Attribute;    // :68
    public abstract Attribute[] GetAttributes(Type? type = null); // :74
    public Attribute[] GetAllAttributes();                // :79
    protected readonly ConcurrentDictionary<Type, Attribute[]> _attrCache = new(); // :57 ⚠ protected
}

public abstract class ReflectionAccessorBase<TMemberInfo, TAccessor>(TMemberInfo member) : ReflectionAccessorBase
    where TMemberInfo : MemberInfo
    where TAccessor : ReflectionAccessorBase<TMemberInfo, TAccessor>                       // :90
{
    public static int CacheCount { get; }                  // :104
    public static void ClearCache();                       // :117
    protected static TAccessor GetOrCreate(TMemberInfo member, Func<TMemberInfo, TAccessor> factory); // :109 ⚠ protected
    public TMemberInfo MemberInfo { get; }                 // :126
    public override string Name => MemberInfo.Name;        // :132
    public override Type DeclaringType => MemberInfo.DeclaringType!; // :137
    public override Attribute[] GetAttributes(Type? type = null);    // :146
}
```
> Availability note: `GetAttribute<T>`/`HasAttribute<T>`/`GetAllAttributes` and the public ctor are present **public** in the DLL; the *generic base ctor* (``ReflectionAccessorBase`2.#ctor``) and `GetOrCreate` are `⚠ protected`, `_attrCache` is `⚠ protected` — documented by XML but subclass-only. `MethodAccessor.CreateInvoker`/`EmitUnboxWithEnumSupport` are `⚠ private`; `MemberAccessor.IsSupportedMember` is `⚠ private`; `ExprHelper.Expect`/`CreateAccessorsByExpressionTree`/`CreateAccessorsByEmit`/`ClearAll` are `⚠ private`.

XML `T:JmcModLib.Reflection.TypeAccessor` summary (verbatim): *类型访问器 - 提供对 Type 本身及其成员的统一访问* ("Type accessor — unified access to a Type and its members").

```csharp
// JmcModLib.Reflection.TypeAccessor — Reflection/TypeAccessor.cs
public class TypeAccessor : ReflectionAccessorBase<Type, TypeAccessor>
{
    public TypeAccessor(Type type);                       // :20
    public Type Type => MemberInfo;                       // :17
    public static TypeAccessor Get(Type type);            // :32
    public static TypeAccessor Get<T>();                  // :42  (+undoc in XML)
    public static IEnumerable<TypeAccessor> GetAll(Assembly asm); // :44 (+undoc)
    public object? CreateInstance();                      // :60
    public object? CreateInstance(params object?[] args); // :76
    public T? CreateInstance<T>() where T : class;        // :92 (+undoc)
}
```

XML `T:JmcModLib.Reflection.MemberAccessor` summary (verbatim): *字段 / 属性 的统一高性能访问器。* ("Unified high-performance accessor for fields/properties.") — fields, properties **and indexers**, with cached compiled getter/setter delegates.

```csharp
// JmcModLib.Reflection.MemberAccessor (sealed) — Reflection/MemberAccessor.cs
public sealed class MemberAccessor : ReflectionAccessorBase<MemberInfo, MemberAccessor>
{
    public bool CanRead { get; }          public bool CanWrite { get; }     // :18,:23
    public Type ValueType { get; }                                         // :32
    public MemberTypes MemberType => MemberInfo.MemberType;                // :40
    public Delegate? TypedGetter { get; }   public Delegate? TypedSetter { get; } // :52,:57  (null for ref/ref-like/indexer/non-writable)
    public object? GetValue(object? target);                               // :356  (throws for indexers)
    public void SetValue(object? target, object? value);                   // :385
    public object? GetValue(object? target, params object?[] indexArgs);   // :416  (indexers)
    public void SetValue(object? target, object? value, params object?[] indexArgs); // :442
    public TValue GetValue<TTarget, TValue>(TTarget target);               // :465
    public void SetValue<TTarget, TValue>(TTarget target, TValue value);   // :485
    public TValue GetValue<TValue>();                                      // :504  (static only)
    public void SetValue<TValue>(TValue value);                            // :519  (static only)
    public static MemberAccessor Get(Type type, string memberName);        // :535
    public static MemberAccessor GetIndexer(Type type, params Type[] parameterTypes); // :587
    public static MemberAccessor Get(MemberInfo member);                   // :602
    public static IEnumerable<MemberAccessor> GetAll(Type type, BindingFlags flags = DefaultFlags); // :645
    public static IEnumerable<MemberAccessor> GetAll<T>(BindingFlags flags = DefaultFlags);          // :660
}
```

XML `T:JmcModLib.Reflection.MethodAccessor` summary (verbatim): *用于反射方法* ("For reflecting methods"). XML `T:MethodAccessor.ParamSignature` (verbatim): *参数签名（用于缓存键）* — notes: `null`/no parameter list ⇒ `Length = -1` (default value); generic placeholders all map to `RuntimeTypeHandle = default`, so different `T` on one generic method definition share a signature. `ParamSignature` is nested **`internal`** (`⚠ internal`).

```csharp
// JmcModLib.Reflection.MethodAccessor (sealed) — Reflection/MethodAccessor.cs
public sealed class MethodAccessor : ReflectionAccessorBase<MethodInfo, MethodAccessor>
{
    public override bool IsStatic => MemberInfo.IsStatic;   // :71
    public Delegate? TypedDelegate { get; }                 // :90
    public static MethodAccessor Get(MethodInfo method);    // :129
    public Delegate GetTypedDelegate();                     // :142  (throws when unavailable)
    public static IEnumerable<MethodAccessor> GetAll(Type type, BindingFlags flags = DefaultFlags); // :151
    public static IEnumerable<MethodAccessor> GetAll<T>(BindingFlags flags = DefaultFlags);          // :224
    public static MethodAccessor Get(Type type, string methodName, Type[]? parameterTypes = null);   // :235
    public MethodAccessor MakeGeneric(params Type[] genericTypes);  // :333
    public object? Invoke(object? instance, params object?[] args); // :350
    public object? Invoke(object? instance);                 // :403  (fast paths a0..a2)
    public object? Invoke(object? instance, object? a0);     // :417
    public object? Invoke(object? instance, object? a0, object? a1);          // :431
    public object? Invoke(object? instance, object? a0, object? a1, object? a2); // :445
    // typed helpers (all ✓ binary-public; Invoke`2..`5 etc. documented in XML):
    public TResult Invoke<TTarget, TResult>(TTarget instance);                                   // +typed 0..3 args
    public void InvokeVoid<TTarget>(TTarget instance);                 // + 1..3 args (T1..T3)
    public TResult InvokeStatic<TResult>();                           // + 1..3 args
    public void InvokeStaticVoid();                                   // + 1..3 args
}
```

XML `T:JmcModLib.Utils.ExprHelper` summary (verbatim): *解析表达式的一些库* ("A small library for parsing expressions"). `T:Utils.ExprHelper.MemberAccessMode`: *生成Accessor的后端模式* ("Backend modes for generating accessors") — `Reflection=0, ExpressionTree=1, Emit=2, Default=2` (binary; `Default` aliases `Emit`). `T:Utils.ExprHelper.MemberAccessors`: *类型访问器辅助类* — `public record MemberAccessors(Delegate Getter, Delegate Setter)` (`ExprHelper.cs:142`).

```csharp
// JmcModLib.Utils.ExprHelper (static) — Utils/ExprHelper.cs
public static bool EnableCache { get; set; }                                              // :51 (per-assembly)
public static MemberAccessMode AccessMode { get; set; }                                   // :85
public static (Func<T> getter, Action<T> setter) GetOrCreateAccessors<T>(Expression<Func<T>> expr, Assembly? assembly = null);  // :158
public static (Func<T> getter, Action<T> setter) GetOrCreateAccessors<T>(Expression<Func<T>> expr, out bool cacheHit, Assembly? assembly = null); // :177
public static void ClearAssemblyCache(Assembly? assembly = null);                         // :444
```
`GetOrCreateAccessors<T>` accepts an expression like `() => someField` / `() => instance.Prop` and returns compiled getter/setter; `Assembly` selects the per-assembly mode/cache config. `ExprHelper.Expect`/`CreateAccessorsByExpressionTree`/`CreateAccessorsByEmit`/`ClearAll` are `⚠ private` (documented in XML, not callable).

**Use:** `MethodAccessor`/`MemberAccessor` are the fastest way to poke game internals this project needs (acts, run state, card-reward internals) without per-call reflection; `ExprHelper` compiles field/property accessors from expression trees.

---

# 5. `Utils.ModLogger` (23 documented members)

XML `T:JmcModLib.Utils.ModLogger` summary (verbatim): *JML 对 STS2 原生日志器的轻量封装，按程序集隔离日志上下文、类型和格式。* ("JML's lightweight wrapper over the STS2 native logger; isolates log context, level and format per assembly.") Output lands in the **game's native `Logger`** (STS2 logging system; per-assembly context name from `ModContext.LoggerContext`), with levels mapped to STS2 `LogLevel` (`Load`, `VeryDebug`, `Debug`, `Info`, `Warn`, `Error`).

XML `T:JmcModLib.Utils.LogPrefixFlags` summary (verbatim): *控制 JML 日志前缀的附加内容。* ("Controls what JML prepends to log lines.") `[Flags] enum { None=0, Timestamp=1, Default=1 }` (binary; `Default` == `Timestamp`).

XML `T:JmcModLib.Utils.AssemblyLogConfiguration` summary (verbatim): *指定程序集的 JML 日志配置。* ("Per-assembly JML log configuration.") — properties `LogType` (default `LogType.Generic`), `PrefixFlags`, `ThrowOnFatal` (default `true`), `IncludeExceptionDetails` (default `true`). XML `T:JmcModLib.Utils.LoggerSnapshot` summary (verbatim): *指定程序集当前日志配置的只读快照。* ("Read-only snapshot of an assembly's current log configuration.") — `public readonly record struct LoggerSnapshot(LogType LogType, LogPrefixFlags PrefixFlags, bool ThrowOnFatal, bool IncludeExceptionDetails, string Context)` (`ModLogger.cs:63`).

```csharp
// JmcModLib.Utils.ModLogger (static partial) — Utils/Logger/ModLogger.cs
public static LogType DefaultLogType { get; set; } = LogType.Generic;                     // :83
public static LogPrefixFlags DefaultPrefixFlags { get; set; } = LogPrefixFlags.Default;   // :88
public static bool DefaultThrowOnFatal { get; set; } = true;                              // :93
public static bool DefaultIncludeExceptionDetails { get; set; } = true;                   // :98
public static void RegisterAssembly(Assembly? assembly = null, LogPrefixFlags prefixFlags = LogPrefixFlags.Default,
    bool throwOnFatal = true, LogType logType = LogType.Generic, bool includeExceptionDetails = true); // :118
public static void UnregisterAssembly(Assembly? assembly = null);                         // :139
public static LogType GetLogType(Assembly? assembly = null);                              // :151
public static void SetLogType(LogType logType, Assembly? assembly = null);                // :161
public static LogPrefixFlags GetPrefixFlags(Assembly? assembly = null);                   // :173
public static void SetPrefixFlags(LogPrefixFlags flags, Assembly? assembly = null);       // :183
public static bool HasPrefixFlag(LogPrefixFlags flag, Assembly? assembly = null);         // :194
public static void TogglePrefixFlag(LogPrefixFlags flag, Assembly? assembly = null);      // :204
public static LoggerSnapshot GetSnapshot(Assembly? assembly = null);                      // :216
public static void Load(string message, Assembly? assembly = null);                       // :228  (STS2 LogLevel.Load)
public static void Trace(string message, Assembly? assembly = null);                      // :238  (VeryDebug)
public static void Debug(string message, Assembly? assembly = null);                      // :248
public static void Info(string message, Assembly? assembly = null);                       // :258
public static void Warn(string message, Assembly? assembly = null);                       // :268
public static void Warn(string message, Exception exception, Assembly? assembly = null);  // :278
public static void Error(string message, Assembly? assembly = null);                      // :288
public static void Error(string message, Exception exception, Assembly? assembly = null); // :298
public static void Fatal(Exception exception, string? message = null, Assembly? assembly = null); // :308  (rethrows when ThrowOnFatal)
```
All `assembly` parameters default to the caller's assembly via `AssemblyResolver`. Registering a mod assembly automatically configures its logger (`ModRegistry` → `ModLogger.RegisterAssembly`).

---

# 6. `Security` (65 documented members; secrets never go into config JSON)

XML `T:JmcModLib.Security.SecretAttribute` summary (verbatim): *将静态 `JmcSecretSlot` 字段或属性声明为一个 Secret 槽位。* ("Declares a static `JmcSecretSlot` field/property as a secret slot.") Remarks (verbatim): *Secret 槽位会显示在 JML 设置页中，但不会写入普通配置 JSON；保存、读取和删除都通过 `JmcSecretStore` 的独立后端完成。* ("The slot shows in the JML settings page but is never written to normal config JSON; save/read/delete go through the independent backends of `JmcSecretStore`.")

```csharp
// JmcModLib.Security.SecretAttribute (sealed : Attribute) — Security/SecretAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class SecretAttribute(string key) : Attribute   // :13
{
    public string Key { get; }                                // :28
    public string Group { get; set; } = ConfigAttribute.DefaultGroup;   // :33
    public string? LocTable { get; set; }                     // :38
    public string? DisplayName { get; set; }                  // :43
    public string? Description { get; set; }                  // :48
    public string? DisplayNameKey { get; set; }               // :53
    public string? DescriptionKey { get; set; }               // :58
    public string? SetButtonTextKey { get; set; }             // :63
    public string? ClearButtonTextKey { get; set; }           // :68
    public string? GroupKey { get; set; }                     // :73
    public string? ScopeProvider { get; set; }                // :78  (static parameterless string member; participates in storage isolation)
    public bool AllowWeakFileProtection { get; set; }         // :83
    public int Order { get; set; }                            // :88
}
```

XML `T:JmcModLib.Security.JmcSecretOptions` summary (verbatim): *手动注册 Secret 槽位时使用的显示、分组和后端选项。* ("Display, grouping and backend options when manually registering a secret slot.") — `public sealed class JmcSecretOptions` with the same properties as the attribute but `Func<string>? ScopeProvider` instead of a name (`Security/JmcSecretOptions.cs:8-78`).

XML `T:JmcModLib.Security.JmcSecretSlot` summary (verbatim): *子 MOD 持有的 Secret 槽位句柄。* ("Secret slot handle held by a child mod.") Remarks (verbatim): *本类型只保存槽位元数据，不保存密钥明文。读取到的 `String` 明文无法被 .NET 清零，调用方应避免记录日志、长时间缓存或传递到不可信位置。* ("This type holds only slot metadata, never plaintext. Read `string` plaintext cannot be zeroed by .NET — avoid logging, long caching, or passing it to untrusted places.")

```csharp
// JmcModLib.Security.JmcSecretSlot (sealed) — Security/JmcSecretSlot.cs
public string Key { get; }                                    // :17  ("" when unbound)
public string ModId { get; }                                  // :22
public string Scope { get; }                                  // :27  (resolved runtime scope)
public JmcSecretProtectionLevel ProtectionLevel { get; }      // :32
public bool TryRead(out string value, out JmcSecretReadStatus status);   // :43
public bool TrySave(string value, out JmcSecretWriteStatus status);       // :61
public bool TryDelete(out JmcSecretWriteStatus status);                  // :77
public bool Exists();                                         // :92
```
XML `T:JmcModLib.Security.JmcSecretStore` summary (verbatim): *JML Secret 的统一读写入口。* ("JML unified secret read/write entry.") Remarks: prefer `JmcSecretSlot`; this static class is for advanced keyed access. Enums (`Security/JmcSecretStatuses.cs`):

- `JmcSecretProtectionLevel`: *表示当前 Secret 后端能够提供的保护等级。* — `Unknown=0, SystemKeychain=1, UserProfileProtected=2, WeakFileProtection=3, SessionOnly=4, Unavailable=5`
- `JmcSecretReadStatus`: *表示读取 Secret 的结果状态。* — `Success=0, Missing=1, Unavailable=2, AccessDenied=3, DecryptionFailed=4, BackendError=5`
- `JmcSecretWriteStatus`: *表示写入或删除 Secret 的结果状态。* — `Success=0, Unavailable=1, AccessDenied=2, WeakProtectionNotAllowed=3, BackendError=4`

```csharp
// JmcModLib.Security.JmcSecretStore (static) — Security/JmcSecretStore.cs
public static JmcSecretProtectionLevel GetProtectionLevel();                                          // :21
public static bool TryRead(string key, out string value, out JmcSecretReadStatus status, string? scope = null, Assembly? assembly = null);   // :35
public static bool TrySave(string key, string value, out JmcSecretWriteStatus status, string? scope = null, Assembly? assembly = null);     // :55
public static bool TryDelete(string key, out JmcSecretWriteStatus status, string? scope = null, Assembly? assembly = null);                 // :74
public static bool Exists(string key, string? scope = null, Assembly? assembly = null);               // :91
```
**Where and how it is stored** (backends are `internal`, `Security/Backends/*`): the backend is selected at runtime (`SecretBackendSelector`):
- Windows: **DPAPI** (`WindowsDpapiSecretBackend`, `CryptProtectData` CurrentUser) → `UserProfileProtected`; file `mods/secrets/<ModId>/secrets.v1.json` (encrypted blobs, base64 `DataBlob`).
- Without system storage: `WeakFileSecretBackend` → `WeakFileProtection`, file `mods/secrets/<ModId>/weak-secrets.v1.json` (plaintext JSON) — **only if** `AllowWeakFileProtection = true`, otherwise `WeakProtectionNotAllowed`.
- Neither available: `UnavailableSecretBackend` → `Unavailable`.
Paths derive from the game user-data dir (`SecretIdentifier.ModSecretDirectory`, `Security/Backends/SecretIdentifier.cs:37`). **What is encrypted:** the secret values (the slot key/scope structure is plain); DPAPI binds them to the current Windows user profile.

**Use:** per-user API keys (e.g. LLM keys, account tokens) for this project's tooling; `[Secret]` gives you a settings-page row with set/clear buttons and a protection-level risk hint.

---

# 7. `Persistence` (58 documented members; five scopes + slot handles)

Five attribute scopes (`Persistence/`), each applicable to a static field/property that is either a slot (`JmcDataSlot<T>` / `JmcRunDataSlot<T>`) or a plain static value. All attributes share `string Key`, `int SchemaVersion { get; set; } = 1` (phase 1: written to the document, **no automatic migration**), and `JmcDataWritePolicy WritePolicy { get; set; } = WhenChanged`:

| Attribute (XML `T:` summary, verbatim) | Scope |
|---|---|
| `JmcLocalPreferenceAttribute(string key)` | *将静态字段或静态属性注册为当前机器本地的 JML 客户端偏好数据。* — machine-local; not in game saves, not profile-switched, not cloud/MP synced (UI state, sort order, collapsed state, window position). |
| `JmcGlobalDataAttribute(string key)` | *将静态字段或静态属性注册为当前账号范围内的 JML 全局持久化数据。* — account-wide; shared across profiles (caches, stats). |
| `JmcProfileDataAttribute(string key)` | *将静态字段或静态属性注册为当前 profile 范围内的 JML 持久化数据。* — reloads on profile switch. |
| `JmcRunDataAttribute(string key)` | *将静态字段或静态属性注册为当前 run 范围内的 JML 非同步持久化数据。* — run-scoped, local only; no MP/reconnect sync in phase 1. |
| `JmcClientRunDataAttribute(string key)` | *将静态 `JmcRunDataSlot<T>` 字段或属性注册为当前客户端、当前 run 生命周期内的数据。* — written to a **local sidecar file**, never the run save; survives save+quit, restored on load, cleaned up when the run ends/aborts/deletes or a new run starts. |

Sources: `JmcLocalPreferenceAttribute.cs:12`, `JmcGlobalDataAttribute.cs:11`, `JmcProfileDataAttribute.cs:11`, `JmcRunDataAttribute.cs:11`, `JmcClientRunDataAttribute.cs:11` (all `sealed : Attribute`, ctor `(string key)`).

XML `T:JmcModLib.Persistence.JmcDataSlot``1` (i.e. ``JmcDataSlot`1``) summary (verbatim): *子 MOD 用于读写本地偏好、全局或 profile 持久化数据的槽位句柄。* ("Slot handle for a mod to read/write local-preference, global or profile data.") Remarks: for reference-type data mutated in place, wrap mutations in `Modify(...)` — do not rely on mutating the object returned by `Value` and expecting auto-save.

```csharp
// JmcModLib.Persistence.JmcDataSlot<T> (sealed) — Persistence/JmcDataSlot.cs
public JmcDataSlot();                      public JmcDataSlot(T defaultValue);   // :19, :28
public bool IsBound { get; }               // :36
public string Key { get; }                 // :41  ("" when unbound)
public T Value { get; }                    // :46  (binding value; type default when unbound)
public JmcDataWriteResult SetValue(T newValue);   // :53  (local preferences flush to disk immediately)
public JmcDataWriteResult Modify(Action<T> update); // :69
```
XML `T:JmcModLib.Persistence.JmcRunDataSlot``1` (i.e. ``JmcRunDataSlot`1``) summary (verbatim): *子 MOD 用于读写当前 run 或当前客户端本局非同步持久化数据的槽位句柄。* ("Slot handle for the current run or the current client's local unsynced run data.") — same members (`Persistence/JmcRunDataSlot.cs:12-90`): `JmcRunDataSlot()`, `JmcRunDataSlot(T defaultValue)`, `bool IsBound`, `string Key`, `T Value`, `JmcDataWriteResult SetValue(T)`, `JmcDataWriteResult Modify(Action<T>)`.

XML `T:JmcModLib.Persistence.JmcDataWritePolicy` summary (verbatim): *指定持久化数据在刷新时的写入策略。* — `enum { WhenChanged=0, Always=1 }`. XML `T:JmcDataWriteResult`: *表示一次持久化槽位写入请求的结果。* — `public readonly struct JmcDataWriteResult` with `bool Success`, `string Message`, `static Succeeded()`, `static Failed(string message)` (`Persistence/JmcDataWriteResult.cs:6-47`).

XML `T:JmcModLib.Persistence.JmcPersistenceManager` summary (verbatim): *JML Persistence 的统一初始化与刷新入口。* ("JML Persistence unified init and flush entry.") Remarks: child mods usually only declare the five attributes; this type is for manual flush.

```csharp
// JmcModLib.Persistence.JmcPersistenceManager (static) — Persistence/JmcPersistenceManager.cs
public static bool IsInitialized { get; }          // :39
public static void Init();                         // :44
public static void Dispose();                      // :66
public static void Flush(Assembly? assembly = null);           // :95   (all scopes of the assembly)
public static void FlushLocalPreferences(Assembly? assembly = null); // :108
public static void FlushClientRunData(Assembly? assembly = null);   // :118
public static void FlushAll();                     // :127
```
**Interaction with the engine save system:** run data is attached to the game's run-save JSON under the root property `_jml` (`RunPersistenceDocument.RootPropertyName`, `Persistence/Run/RunPersistenceDocument.cs:8`); a Harmony postfix on the run-save path appends the JML document (`RunPersistenceManager.AppendPersistenceAfterOriginalSaveAsync`, `Persistence/Run/RunPersistenceManager.cs:266`, internal) and load restores it (`LoadRunDocumentFromSave`, `:230`). Client-run data lives in a **sidecar file** keyed by run identity (`PersistencePathProvider.TryGetClientRunFilePath`, `Persistence/Storage/PersistencePathProvider.cs:63`), not in the save. Non-run scopes persist to per-scope JSON documents via `NewtonsoftPersistenceStorage` in the user-data dir (`PersistencePathProvider`).

---

# 8. `Compat` (19 documented members; cross-version accessors — no new capability)

XML `T:JmcModLib.Compat.ModCompat` summary (verbatim): *封装不同 STS2 版本中的 MOD 列表、程序集与 manifest 成员差异。* ("Encapsulates version differences in the MOD list, assemblies and manifest members across STS2 versions.") Remarks (verbatim): *已归档的游戏 DLL 中，0.99.1 至 0.107.1 的 `Mod` 使用单个 `assembly` 字段；0.108 将其改为 `assemblies` 列表… 0.99.1 的 MOD 列表与加载状态分别由 `AllMods`/`LoadedMods` 和 `wasLoaded` 表示；0.103 起改为 `Mods`/`GetLoadedMods()` 和 `state`。其他 PascalCase 候选名用于防御性兼容，不表示已确认它们存在于上述归档版本。* ("Archived game DLLs 0.99.1–0.107.1 use a single `assembly` field on `Mod`; 0.108 changed it to an `assemblies` list… Other PascalCase candidates are defensive, not confirmed to exist in those archived versions.")

```csharp
// JmcModLib.Compat.ModCompat (static) — Compat/ModCompat.cs
public static IReadOnlyList<Mod> GetKnownMods();                        // :49   (engine: Mod manager list; 0.99.1 AllMods vs 0.103+ Mods)
public static IReadOnlyList<Mod> GetLoadedMods();                       // :69   (0.99.1 LoadedMods vs 0.103+ GetLoadedMods())
public static bool IsLoaded(Mod? mod);                                  // :95   (0.103-0.108 ModLoadState state; earlier wasLoaded)
public static IReadOnlyList<Assembly> GetAssemblies(Mod? mod);          // :112  (0.99.1-0.107.1 single `assembly`; 0.108+ `assemblies` list)
public static Assembly? GetPrimaryAssembly(Mod? mod);                   // :140
public static bool ContainsAssembly(Mod? mod, Assembly assembly);       // :152
public static ModManifest? GetManifest(Mod? mod);                       // :167
public static string? GetPckName(Mod? mod);                             // :183
public static string? GetManifestId(ModManifest? manifest);             // :195
public static string? GetManifestName(ModManifest? manifest);           // :207
public static string? GetManifestVersion(ModManifest? manifest);        // :219
```
XML `T:JmcModLib.Compat.MultiplayerCompat` summary (verbatim): *封装不同 STS2 版本中的多人错误信息与加入流程成员差异。* ("Encapsulates version differences in multiplayer error info and the join flow.")

```csharp
// JmcModLib.Compat.MultiplayerCompat (static) — Compat/MultiplayerCompat.cs
public static bool TryGetConnectionExtraInfo(NetErrorInfo info, [NotNullWhen(true)] out ConnectionFailureExtraInfo? extraInfo); // :98
    // 0.99.1–0.107.1: private readonly field _connectionExtraInfo; 0.108+: public property ConnectionExtraInfo
public static bool TryGetJoinFlowNetService(JoinFlow flow, [NotNullWhen(true)] out INetGameService? service); // :129
public static IReadOnlyList<ulong> GetRunLobbyPlayerIds(RunLobby? lobby);           // :146  (0.109.1- ConnectedPlayerIds vs 0.110+ PlayerIds)
public static IReadOnlyList<ulong> GetLoadRunLobbyPlayerIds(LoadRunLobby? lobby);   // :161
public static IReadOnlyList<ulong> GetConnectedHostPeerIds(INetHostGameService hostService); // :179  (host-interface property 0.107.1-0.110.1 vs native host impl 0.111)
// internal (⚠): TryReadJoinFlowNetService(JoinFlow, out INetGameService?) :225; TryGetGameplayModMismatch(...)
```
**Plainly:** every `Get*`/`TryGet*` here is a **cross-version accessor** over archived engine layouts — it reads the *same* logical data the game exposes differently across 0.99.1→0.111.0; it adds no new capability. `CompatMemberResolver` (internal) performs the member probing; candidate names not confirmed in an archived build are handled defensively (return `null`/`false`). **Use:** this project's mod-manifest/assembly lookups (`ModRuntime` uses these) and multiplayer host/lobby reads should go through these shims rather than direct engine reflection.

---

# 9. `Multiplayer.OptionalNetworkFeature*` (25 documented members)

A feature that **may change network behaviour** is declared once and gated: while its config is enabled, it participates in join-compatibility checks; the runtime applies the enabled state to the current protocol only when the network is idle (or marks the run as requiring a restart).

XML `T:JmcModLib.Multiplayer.OptionalNetworkFeatureAttribute` summary (verbatim): *将一个静态布尔配置声明为可选网络功能，并指定该功能独占的网络消息标记接口。* ("Declares a static boolean config as an optional network feature and names the network-message marker interface it exclusively owns.") Remarks (verbatim): the target member **must also** be registered as a static `bool` config via `ConfigAttribute`; `messageMarkerType` must be an interface deriving from the game's `INetMessage` and may only mark messages owned by this feature; the mod manifest must start with `affects_gameplay=false`; the declaration must be scanned during normal `ModRegistry.Register` init — **cannot be registered late** after the base protocol is up.

```csharp
// JmcModLib.Multiplayer.OptionalNetworkFeatureAttribute (sealed : Attribute) — Multiplayer/OptionalNetworkFeatureAttribute.cs
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class OptionalNetworkFeatureAttribute(string id, Type messageMarkerType) : Attribute  // :17
{
    public string Id { get; }                 // :22  (stable id within the owning mod)
    public Type MessageMarkerType { get; }    // :27
    public string CompatibilityVersion { get; set; } = "1";  // :32  (bump on message layout / flow incompatibility)
}
```

XML `T:JmcModLib.Multiplayer.OptionalNetworkFeatureHandle` summary (verbatim): *提供可选网络功能的配置意图、当前生效状态和应用进度。* ("Provides a feature's config intent, currently effective state, and apply progress.")

```csharp
// JmcModLib.Multiplayer.OptionalNetworkFeatureHandle (sealed) — Multiplayer/OptionalNetworkFeatureHandle.cs
public string Id { get; }                          // :35
public string ModId { get; }                       // :40
public string CompatibilityVersion { get; }        // :45
public bool RequestedEnabled { get; }              // :50  (user config request; may not be applied yet)
public bool EffectiveEnabled { get; }              // :64  (what the current runtime protocol actually uses)
public OptionalNetworkFeatureApplyState ApplyState { get; }  // :78
public bool HasPendingApply { get; }               // :93
public event Action<OptionalNetworkFeatureHandle>? StateChanged;             // :107
public event Action<OptionalNetworkFeatureHandle>? EffectiveEnabledChanged;  // :112
```

XML `T:JmcModLib.Multiplayer.OptionalNetworkFeatureApplyState` summary (verbatim): *表示可选网络功能的配置状态如何应用到当前运行时协议。* ("How the feature's config state applies to the current runtime protocol.") Enum (binary): `Applied=0, PendingNetworkIdle=1, RestartRequired=2`.

XML `T:JmcModLib.Multiplayer.OptionalNetworkFeatures` summary (verbatim): *提供可选网络功能运行时句柄的查询入口。* ("Query entry for runtime handles of optional network features.")

```csharp
// JmcModLib.Multiplayer.OptionalNetworkFeatures (static) — Multiplayer/OptionalNetworkFeatures.cs
public static OptionalNetworkFeatureHandle Get(string id, Assembly? assembly = null);  // :19  (KeyNotFoundException when unregistered/invalid)
public static OptionalNetworkFeatureHandle Get<TOwner>(string id);                    // :38
public static bool TryGet(string id, [NotNullWhen(true)] out OptionalNetworkFeatureHandle? handle, Assembly? assembly = null); // :50
```
XML `T:JmcModLib.Multiplayer.OptionalNetworkMismatch` summary (verbatim): *提供 JML 可选网络功能不匹配错误的路由判断。* ("Routing judgement for JML optional-network-feature mismatch errors.") — `public static bool ShouldHandle(NetErrorInfo info)` (`Multiplayer/OptionalNetworkMismatch.cs:29`): true when the join error was caused by a registered optional feature mismatch and the local peer is not the host.

**How gating works** (manager internal, `Multiplayer/Internal/OptionalNetworkFeatureManager.cs`): during join validation the manager compares each feature's `CompatibilityVersion` + `RequestedEnabled` across peers; on mismatch it routes to `OptionalNetworkMismatch` and points the user at the settings entry. Applying a config change: if the network is idle the feature's effective state switches to `Applied` immediately; if a run/network session is active it stays `PendingNetworkIdle` until the network goes idle; if the protocol cannot change mid-session it becomes `RestartRequired`. Patches live in `Multiplayer/Patches/OptionalNetworkFeaturePatches.cs`. **Use:** this project's co-op-affecting features (e.g. shared map state, sync'd rerolls) should declare an `INetMessage` marker interface + `CompatibilityVersion` and gate via `RequestedEnabled`/`EffectiveEnabled` instead of custom net checks.

---

# 10. The dispatch build toolchain (multi-version technique)

JML ships two cooperating MSBuild layers. The **BuildTools** layer is the generic StS2 mod build pipeline (game references, PCK export, deploy); the **Dispatch** layer makes one mod build against **several game versions**, with a version-selecting bootstrap DLL as the manifest entry.

## 10.1 `BuildTools/Jmc.Sts2Mod.Build.props` / `.targets` (repo `BuildTools/`, also copied into the publish dir)

`Jmc.Sts2Mod.Build.props` (3.7 KB): defaults for `TargetFramework` (`net10.0`), `Nullable`/`ImplicitUsings`/`LangVersion=latest`, author metadata (`Author`, `AuthorEmail`, `AuthorBilibiliUrl`, `AuthorGitHubUrl`, `SupportQQGroup`, `AuthorContactInfo`), tool paths (`SteamLibraryPath`, `Sts2Path`, `Sts2DataDir`, `GodotExe`), output layout (`ModLocalDir`, `PublishDir`, `ModManifestPath`, `ModGameDir`, `ModOneDriveDir`, `VersionInfoFile`), feature switches (`JmcSts2ModBuildEnabled`, `JmcSts2AddGameReferences`, `JmcSts2CopyDefaultDllToPublishDir`, `JmcSts2ExportPck`, `JmcSts2DeployToGameDir`, `JmcSts2DeployToOneDriveDir`, `PromptLaunchGameAfterBuild`), and the game reference item group: `GodotSharp.dll`, **`sts2.dll`** (the whole game assembly), `0Harmony.dll` (all `Private=false`, from `$(Sts2DataDir)`).

`Jmc.Sts2Mod.Build.targets` (13.5 KB) targets, all gated on `JmcSts2ModBuildEnabled != false`:
- `JmcSts2ModPrepareProjectFiles` — creates `project.godot`, `export_presets.cfg` and a default manifest (dependency `JmcModLib >= 1.4.0`, `affects_gameplay=false`) when absent; never overwrites an existing manifest.
- `JmcSts2ModReadMetadata` (`BeforeTargets=GetAssemblyVersion`) — parses `Core\VersionInfo.cs` for `Version` and the manifest for `id`/`name`/`author`/`description`/`url`; injects `AssemblyMetadataAttribute` entries (`ModId`, `ModName`, `ModAuthor`, `ModVersion`, `ModDescription`, `AuthorEmail`, `AuthorBilibiliUrl`, `AuthorGitHubUrl`, `SupportQQGroup`, `AuthorContactInfo`, `ModRepositoryUrl`). One source of truth: `VersionInfo.cs` drives both assembly version and manifest `version`.
- `JmcSts2ModSyncManifestVersion` — runs `scripts/Sync-ModManifestVersion.ps1` to rewrite only the `version` field, preserving JSON formatting.
- `JmcSts2ModCopyDefaultDllToPublishDir` → `JmcSts2ModExportPck` (Godot `--headless --export-pack "Windows Desktop" modPublish\<ModName>.pck`) → `JmcSts2ModDeployToGameDir` / `JmcSts2ModDeployToOneDriveDir` (robocopy `/E /XO`) → `JmcSts2ModBuildAndDeploy` (`AfterTargets=Build`) → `JmcSts2ModAskToLaunchGame` (writes `steam_appid.txt` = 2868840, prompts to launch).

## 10.2 `JmcModLib.Dispatch.targets` (installed, 8.2 KB — the multi-version part)

For a mod project named `<ModName>` producing `<ModName>.Runtime.dll`:
- Property defaults: `JmcDispatchEnabled=true`, `JmcDispatchModName` (falls back to `ModName`, then project name), `JmcDispatchPublishDir`, `JmcDispatchRuntimeAssemblyName = <ModName>.Runtime`, `JmcDispatchInitializerType = <ModName>.MainFile`, `JmcDispatchInitializerMethod = Initialize`, descriptor `<ModName>.dispatch.json` in the publish dir, bootstrap project at `dispatch\JmcModLib.Dispatch.Bootstrap.csproj` (falling back to the JML-installed copy).
- When enabled it sets `<AssemblyName>` to the runtime name and declares a default `JmcDispatchRuntime` item (`default` → `runtimes/default/<Runtime>.dll`).
- `JmcDispatchNormalize` — fills `RuntimeAssembly` (`runtimes/<id>/…`), `ProbeDirectories`, `ProbeAllDlls`, `SourcePath` (`$(TargetPath)`), `SourceDirectory` per entry.
- `JmcDispatchWriteDescriptor` — writes `<ModName>.dispatch.json`: `{ "initializerType", "initializerMethod", "entries": [ { "id", "minGameVersion", "maxGameVersionExclusive", "runtimeAssembly", "probeDirectories", "dependencies", "probeAllDlls" } ] }`.
- `JmcDispatchBuildBootstrap` — MSBuild-restores/builds the shared bootstrap project with `AssemblyName=<ModName>` and copies `<ModName>.dll` to the publish dir. The bootstrap is **zero JML runtime dependency**: it only knows how to read the descriptor and load a runtime.
- `JmcDispatchCopyRuntimes` — copies each built runtime's `*.dll/*.pdb/*.xml/*.json` into `publish\runtimes\<id>\`.
- `JmcDispatchPublish` (`AfterTargets=Build`) chains all of the above.

Resulting layout: `publish\<ModName>.dll` (bootstrap entry), `publish\<ModName>.dispatch.json`, `publish\runtimes\<id>\<ModName>.Runtime.dll` per game-version range.

## 10.3 Bootstrap behaviour (Dispatch sources, `internal` — technique, not API)

`DispatchBootstrap.Initialize` (repo `Dispatch/DispatchBootstrap.cs`): resolves the mod dir, reads `<ModName>.dispatch.json`, reads the current game version via `ReleaseInfoManager` (`GameVersionInfo(RawVersion, SemVer)`), `SelectEntry(gameVersion)` picks the first `DispatchEntry` whose `minGameVersion ≤ v < maxGameVersionExclusive` (semver compare; entries without ranges match anything — `Dispatch/DispatchDescriptor.cs:176`), installs a dependency resolver (`BootstrapDependencyResolver.Install`, `Dispatch/BootstrapDependencyResolver.cs:20`), loads the chosen runtime assembly, and invokes the initializer by reflection. No entry matches ⇒ explicit `InvalidOperationException` (fail loud, not silently).

**Why it matters for this project:** the game's managed surface changed repeatedly (0.99.1 → 0.111.0; see §8). A single DLL cannot safely reference the union of engine APIs. The dispatch pattern — keep the manifest entry assembly dependency-free, build one runtime per game version, select at load time by semver — is the documented JML technique for shipping one mod across several StS2 versions; `docs/JML_Dispatch.md` is the authors' usage guide.

---

# 11. Full member index (all 602 documented members)

All rows are XML `<member>` entries from the installed `JmcModLib.Runtime.xml`. `Bin` column: `✓` = public in the shipped `JmcModLib.Runtime.dll`; `⚠ int. type` = type is internal (not exported) — members unusable; `⚠ internal`/`⚠ protected`/`⚠ private` = member not callable from a consumer assembly. Type rows are the XML `T:` entries (kind `type`). Member signatures are shown with their XML parameter lists; summaries are quoted in §§1–10.

### `Config.UI` — 116 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| JmcKeyModifiers | `Alt` | field | ✓ |
| JmcKeyModifiers | `Ctrl` | field | ✓ |
| JmcKeyModifiers | `Meta` | field | ✓ |
| JmcKeyModifiers | `None` | field | ✓ |
| JmcKeyModifiers | `Shift` | field | ✓ |
| UIButtonColor | `Blue` | field | ✓ |
| UIButtonColor | `Default` | field | ✓ |
| UIButtonColor | `Gold` | field | ✓ |
| UIButtonColor | `Green` | field | ✓ |
| UIButtonColor | `Red` | field | ✓ |
| UIButtonColor | `Reset` | field | ✓ |
| UIDropdownInvalidValuePolicy | `KeepCurrent` | field | ✓ |
| UIDropdownInvalidValuePolicy | `ResetToDefault` | field | ✓ |
| UIDropdownInvalidValuePolicy | `SelectFirstAvailable` | field | ✓ |
| HotkeyOptions | `HotkeyOptions(System.Boolean,System.Boolean,System.Boolean,System.UInt64)` | method | ✓ |
| IConfigUiContext | `Get(System.String)` | method | ✓ |
| IConfigUiContext | `Get``1(System.String)` | method | ✓ |
| IConfigUiContext | `TryGet(System.String,System.Object@)` | method | ✓ |
| IConfigUiContext | `TryGet``1(System.String,``0@)` | method | ✓ |
| JmcHotkeyAttribute | `JmcHotkeyAttribute(System.String)` | method | ✓ |
| JmcHotkeyManager | `Init()` | method | ✓ |
| JmcHotkeyManager | `Register(System.String,System.Func{Godot.Key},System.Action,System.Boolean,System.Boolean,System.Boolean,System.UInt64,System.Reflection.Assembly)` | method | ✓ |
| JmcHotkeyManager | `Register(System.String,System.Func{JmcModLib.Config.UI.JmcKeyBinding},System.Action,System.Boolean,System.Boolean,System.Boolean,System.UInt64,System.Reflection.Assembly)` | method | ✓ |
| JmcHotkeyManager | `Unregister(System.String,System.Reflection.Assembly)` | method | ✓ |
| JmcHotkeyManager | `UnregisterAssembly(System.Reflection.Assembly)` | method | ✓ |
| JmcKeyBinding | `JmcKeyBinding()` | method | ✓ |
| JmcKeyBinding | `JmcKeyBinding(Godot.Key,JmcModLib.Config.UI.JmcKeyModifiers,System.Boolean)` | method | ✓ |
| JmcKeyBinding | `JmcKeyBinding(Godot.Key,System.String,JmcModLib.Config.UI.JmcKeyModifiers,System.Boolean)` | method | ✓ |
| JmcKeyBinding | `JmcKeyBinding(Godot.Key,System.String,JmcModLib.Config.UI.JmcKeyModifiers)` | method | ✓ |
| JmcKeyBinding | `JmcKeyBinding(Godot.Key)` | method | ✓ |
| JmcKeyBinding | `IsDown(System.Boolean)` | method | ✓ |
| JmcKeyBinding | `IsModifierKey(Godot.Key)` | method | ✓ |
| JmcKeyBinding | `IsPressed(Godot.InputEvent,System.Boolean,System.Boolean)` | method | ✓ |
| JmcKeyBinding | `IsPressed(Godot.Key,Godot.InputEvent,System.Boolean)` | method | ✓ |
| JmcKeyBinding | `IsReleased(Godot.InputEvent)` | method | ✓ |
| JmcKeyBinding | `IsReleased(Godot.Key,Godot.InputEvent)` | method | ✓ |
| JmcKeyBinding | `op_Implicit(Godot.Key)` | method | ✓ |
| JmcKeyBinding | `ReadCurrentModifiers()` | method | ✓ |
| JmcKeyBinding | `ReadKey(Godot.InputEventKey)` | method | ✓ |
| JmcKeyBinding | `ReadModifiers(Godot.InputEventKey)` | method | ✓ |
| JmcKeyBinding | `ToKeyboardText()` | method | ✓ |
| JmcKeyBinding | `ToString()` | method | ✓ |
| JmcKeyBinding | `WithController(System.String)` | method | ✓ |
| JmcKeyBinding | `WithEnabled(System.Boolean)` | method | ✓ |
| JmcKeyBinding | `WithKeyboard(Godot.Key,JmcModLib.Config.UI.JmcKeyModifiers)` | method | ✓ |
| JmcKeyBinding | `WithKeyboard(Godot.Key)` | method | ✓ |
| UIButtonAttribute | `UIButtonAttribute(System.String,System.String,System.String)` | method | ✓ |
| UIDropdownOptionsProviderAttribute | `UIDropdownOptionsProviderAttribute(System.String,System.String[])` | method | ✓ |
| UIDropdownOptionsProviderAttribute | `UIDropdownOptionsProviderAttribute(System.String)` | method | ✓ |
| UIHotkeyAttribute | `UIHotkeyAttribute(System.String,System.String)` | method | ✓ |
| UIKeybindAttribute | `UIKeybindAttribute(System.Boolean,System.Boolean)` | method | ✓ |
| UIVisibleWhenAttribute | `UIVisibleWhenAttribute(System.String,System.Boolean)` | method | ✓ |
| UIVisibleWhenAttribute | `UIVisibleWhenAttribute(System.String,System.Double)` | method | ✓ |
| UIVisibleWhenAttribute | `UIVisibleWhenAttribute(System.String,System.Int32)` | method | ✓ |
| UIVisibleWhenAttribute | `UIVisibleWhenAttribute(System.String,System.String)` | method | ✓ |
| UIVisibleWhenAttribute | `UIVisibleWhenAttribute(System.String)` | method | ✓ |
| HotkeyOptions | `AllowEcho` | property | ✓ |
| HotkeyOptions | `ConsumeInput` | property | ✓ |
| HotkeyOptions | `DebounceMs` | property | ✓ |
| HotkeyOptions | `ExactModifiers` | property | ✓ |
| JmcHotkeyAttribute | `AllowEcho` | property | ✓ |
| JmcHotkeyAttribute | `BindingMember` | property | ✓ |
| JmcHotkeyAttribute | `ConsumeInput` | property | ✓ |
| JmcHotkeyAttribute | `DebounceMs` | property | ✓ |
| JmcHotkeyAttribute | `ExactModifiers` | property | ✓ |
| JmcHotkeyAttribute | `Key` | property | ✓ |
| JmcHotkeyManager | `IsInitialized` | property | ✓ |
| JmcKeyBinding | `Controller` | property | ✓ |
| JmcKeyBinding | `Enabled` | property | ✓ |
| JmcKeyBinding | `HasController` | property | ✓ |
| JmcKeyBinding | `HasKeyboard` | property | ✓ |
| JmcKeyBinding | `HasModifiers` | property | ✓ |
| JmcKeyBinding | `Keyboard` | property | ✓ |
| JmcKeyBinding | `Modifiers` | property | ✓ |
| UIDropdownOptionsProviderAttribute | `DependsOn` | property | ✓ |
| UIDropdownOptionsProviderAttribute | `InvalidValuePolicy` | property | ✓ |
| UIDropdownOptionsProviderAttribute | `ProviderName` | property | ✓ |
| UIHotkeyAttribute | `AllowController` | property | ✓ |
| UIHotkeyAttribute | `AllowEcho` | property | ✓ |
| UIHotkeyAttribute | `AllowKeyboard` | property | ✓ |
| UIHotkeyAttribute | `ConsumeInput` | property | ✓ |
| UIHotkeyAttribute | `DebounceMs` | property | ✓ |
| UIHotkeyAttribute | `DefaultController` | property | ✓ |
| UIHotkeyAttribute | `DefaultKeyboard` | property | ✓ |
| UIHotkeyAttribute | `DefaultModifiers` | property | ✓ |
| UIHotkeyAttribute | `Description` | property | ✓ |
| UIHotkeyAttribute | `DescriptionKey` | property | ✓ |
| UIHotkeyAttribute | `DisplayName` | property | ✓ |
| UIHotkeyAttribute | `DisplayNameKey` | property | ✓ |
| UIHotkeyAttribute | `ExactModifiers` | property | ✓ |
| UIHotkeyAttribute | `Group` | property | ✓ |
| UIHotkeyAttribute | `GroupKey` | property | ✓ |
| UIHotkeyAttribute | `Key` | property | ✓ |
| UIHotkeyAttribute | `LocTable` | property | ✓ |
| UIHotkeyAttribute | `Order` | property | ✓ |
| UIHotkeyAttribute | `RestartRequired` | property | ✓ |
| UIKeybindAttribute | `AllowController` | property | ✓ |
| UIKeybindAttribute | `AllowKeyboard` | property | ✓ |
| UIVisibleWhenAttribute | `DependsOn` | property | ✓ |
| UIVisibleWhenAttribute | `ExpectedValue` | property | ✓ |
| UIVisibleWhenAttribute | `IgnoreCase` | property | ✓ |
| UIVisibleWhenAttribute | `Invert` | property | ✓ |
| — | `HotkeyOptions` | type | ✓ |
| — | `IConfigUiContext` | type | ✓ |
| — | `JmcHotkeyAttribute` | type | ✓ |
| — | `JmcHotkeyManager` | type | ✓ |
| — | `JmcKeyBinding` | type | ✓ |
| — | `JmcKeyModifiers` | type | ✓ |
| — | `UIButtonAttribute` | type | ✓ |
| — | `UIButtonColor` | type | ✓ |
| — | `UIConfigAttribute` | type | ✓ |
| — | `UIDropdownInvalidValuePolicy` | type | ✓ |
| — | `UIDropdownOptionsProviderAttribute` | type | ✓ |
| — | `UIHotkeyAttribute` | type | ✓ |
| — | `UIKeybindAttribute` | type | ✓ |
| — | `UIVisibleWhenAttribute` | type | ✓ |

### `Reflection` — 80 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| ReflectionAccessorBase | `_attrCache` | field | ⚠ protected |
| ReflectionAccessorBase | `DefaultFlags` | field | ✓ |
| MemberAccessor | `Get(System.Reflection.MemberInfo)` | method | ✓ |
| MemberAccessor | `Get(System.Type,System.String)` | method | ✓ |
| MemberAccessor | `GetAll(System.Type,System.Reflection.BindingFlags)` | method | ✓ |
| MemberAccessor | `GetAll``1(System.Reflection.BindingFlags)` | method | ✓ |
| MemberAccessor | `GetIndexer(System.Type,System.Type[])` | method | ✓ |
| MemberAccessor | `GetValue(System.Object,System.Object[])` | method | ✓ |
| MemberAccessor | `GetValue(System.Object)` | method | ✓ |
| MemberAccessor | `GetValue``1()` | method | ✓ |
| MemberAccessor | `GetValue``2(``0)` | method | ✓ |
| MemberAccessor | `IsSupportedMember(System.Reflection.MemberInfo)` | method | ⚠ private |
| MemberAccessor | `SetValue(System.Object,System.Object,System.Object[])` | method | ✓ |
| MemberAccessor | `SetValue(System.Object,System.Object)` | method | ✓ |
| MemberAccessor | `SetValue``1(``0)` | method | ✓ |
| MemberAccessor | `SetValue``2(``0,``1)` | method | ✓ |
| MethodAccessor | `CreateInvoker(System.Reflection.MethodInfo)` | method | ⚠ private |
| MethodAccessor | `EmitUnboxWithEnumSupport(System.Reflection.Emit.ILGenerator,System.Type)` | method | ⚠ private |
| MethodAccessor | `Get(System.Reflection.MethodInfo)` | method | ✓ |
| MethodAccessor | `Get(System.Type,System.String,System.Type[])` | method | ✓ |
| MethodAccessor | `GetAll(System.Type,System.Reflection.BindingFlags)` | method | ✓ |
| MethodAccessor | `GetAll``1(System.Reflection.BindingFlags)` | method | ✓ |
| MethodAccessor | `GetTypedDelegate()` | method | ✓ |
| MethodAccessor | `Invoke(System.Object,System.Object,System.Object,System.Object)` | method | ✓ |
| MethodAccessor | `Invoke(System.Object,System.Object,System.Object)` | method | ✓ |
| MethodAccessor | `Invoke(System.Object,System.Object)` | method | ✓ |
| MethodAccessor | `Invoke(System.Object,System.Object[])` | method | ✓ |
| MethodAccessor | `Invoke(System.Object)` | method | ✓ |
| MethodAccessor | `Invoke``2(``0)` | method | ✓ |
| MethodAccessor | `Invoke``3(``0,``1)` | method | ✓ |
| MethodAccessor | `Invoke``4(``0,``1,``2)` | method | ✓ |
| MethodAccessor | `Invoke``5(``0,``1,``2,``3)` | method | ✓ |
| MethodAccessor | `InvokeStatic``1()` | method | ✓ |
| MethodAccessor | `InvokeStatic``2(``0)` | method | ✓ |
| MethodAccessor | `InvokeStatic``3(``0,``1)` | method | ✓ |
| MethodAccessor | `InvokeStatic``4(``0,``1,``2)` | method | ✓ |
| MethodAccessor | `InvokeStaticVoid()` | method | ✓ |
| MethodAccessor | `InvokeStaticVoid``1(``0)` | method | ✓ |
| MethodAccessor | `InvokeStaticVoid``2(``0,``1)` | method | ✓ |
| MethodAccessor | `InvokeStaticVoid``3(``0,``1,``2)` | method | ✓ |
| MethodAccessor | `InvokeVoid``1(``0)` | method | ✓ |
| MethodAccessor | `InvokeVoid``2(``0,``1)` | method | ✓ |
| MethodAccessor | `InvokeVoid``3(``0,``1,``2)` | method | ✓ |
| MethodAccessor | `InvokeVoid``4(``0,``1,``2,``3)` | method | ✓ |
| MethodAccessor | `MakeGeneric(System.Type[])` | method | ✓ |
| ReflectionAccessorBase | `GetAllAttributes()` | method | ✓ |
| ReflectionAccessorBase | `GetAttribute``1()` | method | ✓ |
| ReflectionAccessorBase | `GetAttributes(System.Type)` | method | ✓ |
| ReflectionAccessorBase | `HasAttribute``1()` | method | ✓ |
| ReflectionAccessorBase | `IsSaveOwner(System.Type)` | method | ✓ |
| ReflectionAccessorBase`2 | `ReflectionAccessorBase`2(`0)` | method | ⚠ protected |
| ReflectionAccessorBase`2 | `ClearCache()` | method | ✓ |
| ReflectionAccessorBase`2 | `GetAttributes(System.Type)` | method | ✓ |
| ReflectionAccessorBase`2 | `GetOrCreate(`0,System.Func{`0,`1})` | method | ⚠ protected |
| TypeAccessor | `CreateInstance()` | method | ✓ |
| TypeAccessor | `CreateInstance(System.Object[])` | method | ✓ |
| TypeAccessor | `CreateInstance``1()` | method | ✓ |
| TypeAccessor | `Get(System.Type)` | method | ✓ |
| TypeAccessor | `Get``1()` | method | ✓ |
| MemberAccessor | `CanRead` | property | ✓ |
| MemberAccessor | `CanWrite` | property | ✓ |
| MemberAccessor | `MemberType` | property | ✓ |
| MemberAccessor | `TypedGetter` | property | ✓ |
| MemberAccessor | `TypedSetter` | property | ✓ |
| MemberAccessor | `ValueType` | property | ✓ |
| MethodAccessor | `IsStatic` | property | ✓ |
| MethodAccessor | `TypedDelegate` | property | ✓ |
| ReflectionAccessorBase | `DeclaringType` | property | ✓ |
| ReflectionAccessorBase | `IsStatic` | property | ✓ |
| ReflectionAccessorBase | `Name` | property | ✓ |
| ReflectionAccessorBase`2 | `CacheCount` | property | ✓ |
| ReflectionAccessorBase`2 | `DeclaringType` | property | ✓ |
| ReflectionAccessorBase`2 | `MemberInfo` | property | ✓ |
| ReflectionAccessorBase`2 | `Name` | property | ✓ |
| TypeAccessor | `Type` | property | ✓ |
| — | `MemberAccessor` | type | ✓ |
| — | `MethodAccessor` | type | ✓ |
| — | `ReflectionAccessorBase` | type | ✓ |
| — | `ReflectionAccessorBase`2` | type | ✓ |
| — | `TypeAccessor` | type | ✓ |

### `Security` — 65 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| JmcSecretProtectionLevel | `SessionOnly` | field | ✓ |
| JmcSecretProtectionLevel | `SystemKeychain` | field | ✓ |
| JmcSecretProtectionLevel | `Unavailable` | field | ✓ |
| JmcSecretProtectionLevel | `Unknown` | field | ✓ |
| JmcSecretProtectionLevel | `UserProfileProtected` | field | ✓ |
| JmcSecretProtectionLevel | `WeakFileProtection` | field | ✓ |
| JmcSecretReadStatus | `AccessDenied` | field | ✓ |
| JmcSecretReadStatus | `BackendError` | field | ✓ |
| JmcSecretReadStatus | `DecryptionFailed` | field | ✓ |
| JmcSecretReadStatus | `Missing` | field | ✓ |
| JmcSecretReadStatus | `Success` | field | ✓ |
| JmcSecretReadStatus | `Unavailable` | field | ✓ |
| JmcSecretWriteStatus | `AccessDenied` | field | ✓ |
| JmcSecretWriteStatus | `BackendError` | field | ✓ |
| JmcSecretWriteStatus | `Success` | field | ✓ |
| JmcSecretWriteStatus | `Unavailable` | field | ✓ |
| JmcSecretWriteStatus | `WeakProtectionNotAllowed` | field | ✓ |
| JmcSecretSlot | `Exists()` | method | ✓ |
| JmcSecretSlot | `TryDelete(JmcModLib.Security.JmcSecretWriteStatus@)` | method | ✓ |
| JmcSecretSlot | `TryRead(System.String@,JmcModLib.Security.JmcSecretReadStatus@)` | method | ✓ |
| JmcSecretSlot | `TrySave(System.String,JmcModLib.Security.JmcSecretWriteStatus@)` | method | ✓ |
| JmcSecretStore | `Exists(System.String,System.String,System.Reflection.Assembly)` | method | ✓ |
| JmcSecretStore | `GetProtectionLevel()` | method | ✓ |
| JmcSecretStore | `TryDelete(System.String,JmcModLib.Security.JmcSecretWriteStatus@,System.String,System.Reflection.Assembly)` | method | ✓ |
| JmcSecretStore | `TryRead(System.String,System.String@,JmcModLib.Security.JmcSecretReadStatus@,System.String,System.Reflection.Assembly)` | method | ✓ |
| JmcSecretStore | `TrySave(System.String,System.String,JmcModLib.Security.JmcSecretWriteStatus@,System.String,System.Reflection.Assembly)` | method | ✓ |
| SecretAttribute | `SecretAttribute(System.String)` | method | ✓ |
| JmcSecretOptions | `AllowWeakFileProtection` | property | ✓ |
| JmcSecretOptions | `ClearButtonText` | property | ✓ |
| JmcSecretOptions | `ClearButtonTextKey` | property | ✓ |
| JmcSecretOptions | `Description` | property | ✓ |
| JmcSecretOptions | `DescriptionKey` | property | ✓ |
| JmcSecretOptions | `DisplayName` | property | ✓ |
| JmcSecretOptions | `DisplayNameKey` | property | ✓ |
| JmcSecretOptions | `Group` | property | ✓ |
| JmcSecretOptions | `GroupKey` | property | ✓ |
| JmcSecretOptions | `LocTable` | property | ✓ |
| JmcSecretOptions | `Order` | property | ✓ |
| JmcSecretOptions | `ScopeProvider` | property | ✓ |
| JmcSecretOptions | `SetButtonText` | property | ✓ |
| JmcSecretOptions | `SetButtonTextKey` | property | ✓ |
| JmcSecretSlot | `Key` | property | ✓ |
| JmcSecretSlot | `ModId` | property | ✓ |
| JmcSecretSlot | `ProtectionLevel` | property | ✓ |
| JmcSecretSlot | `Scope` | property | ✓ |
| SecretAttribute | `AllowWeakFileProtection` | property | ✓ |
| SecretAttribute | `ClearButtonTextKey` | property | ✓ |
| SecretAttribute | `Description` | property | ✓ |
| SecretAttribute | `DescriptionKey` | property | ✓ |
| SecretAttribute | `DisplayName` | property | ✓ |
| SecretAttribute | `DisplayNameKey` | property | ✓ |
| SecretAttribute | `Group` | property | ✓ |
| SecretAttribute | `GroupKey` | property | ✓ |
| SecretAttribute | `Key` | property | ✓ |
| SecretAttribute | `LocTable` | property | ✓ |
| SecretAttribute | `Order` | property | ✓ |
| SecretAttribute | `ScopeProvider` | property | ✓ |
| SecretAttribute | `SetButtonTextKey` | property | ✓ |
| — | `JmcSecretOptions` | type | ✓ |
| — | `JmcSecretProtectionLevel` | type | ✓ |
| — | `JmcSecretReadStatus` | type | ✓ |
| — | `JmcSecretSlot` | type | ✓ |
| — | `JmcSecretStore` | type | ✓ |
| — | `JmcSecretWriteStatus` | type | ✓ |
| — | `SecretAttribute` | type | ✓ |

### `Persistence` — 58 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| JmcDataWritePolicy | `Always` | field | ✓ |
| JmcDataWritePolicy | `WhenChanged` | field | ✓ |
| JmcClientRunDataAttribute | `JmcClientRunDataAttribute(System.String)` | method | ✓ |
| JmcDataSlot`1 | `JmcDataSlot`1()` | method | ✓ |
| JmcDataSlot`1 | `JmcDataSlot`1(`0)` | method | ✓ |
| JmcDataSlot`1 | `Modify(System.Action{`0})` | method | ✓ |
| JmcDataSlot`1 | `SetValue(`0)` | method | ✓ |
| JmcDataWriteResult | `Failed(System.String)` | method | ✓ |
| JmcDataWriteResult | `Succeeded()` | method | ✓ |
| JmcDataWriteResult | `ToString()` | method | ✓ |
| JmcGlobalDataAttribute | `JmcGlobalDataAttribute(System.String)` | method | ✓ |
| JmcLocalPreferenceAttribute | `JmcLocalPreferenceAttribute(System.String)` | method | ✓ |
| JmcPersistenceManager | `Dispose()` | method | ✓ |
| JmcPersistenceManager | `Flush(System.Reflection.Assembly)` | method | ✓ |
| JmcPersistenceManager | `FlushAll()` | method | ✓ |
| JmcPersistenceManager | `FlushClientRunData(System.Reflection.Assembly)` | method | ✓ |
| JmcPersistenceManager | `FlushLocalPreferences(System.Reflection.Assembly)` | method | ✓ |
| JmcPersistenceManager | `Init()` | method | ✓ |
| JmcProfileDataAttribute | `JmcProfileDataAttribute(System.String)` | method | ✓ |
| JmcRunDataAttribute | `JmcRunDataAttribute(System.String)` | method | ✓ |
| JmcRunDataSlot`1 | `JmcRunDataSlot`1()` | method | ✓ |
| JmcRunDataSlot`1 | `JmcRunDataSlot`1(`0)` | method | ✓ |
| JmcRunDataSlot`1 | `Modify(System.Action{`0})` | method | ✓ |
| JmcRunDataSlot`1 | `SetValue(`0)` | method | ✓ |
| JmcClientRunDataAttribute | `Key` | property | ✓ |
| JmcClientRunDataAttribute | `SchemaVersion` | property | ✓ |
| JmcClientRunDataAttribute | `WritePolicy` | property | ✓ |
| JmcDataSlot`1 | `IsBound` | property | ✓ |
| JmcDataSlot`1 | `Key` | property | ✓ |
| JmcDataSlot`1 | `Value` | property | ✓ |
| JmcDataWriteResult | `Message` | property | ✓ |
| JmcDataWriteResult | `Success` | property | ✓ |
| JmcGlobalDataAttribute | `Key` | property | ✓ |
| JmcGlobalDataAttribute | `SchemaVersion` | property | ✓ |
| JmcGlobalDataAttribute | `WritePolicy` | property | ✓ |
| JmcLocalPreferenceAttribute | `Key` | property | ✓ |
| JmcLocalPreferenceAttribute | `SchemaVersion` | property | ✓ |
| JmcLocalPreferenceAttribute | `WritePolicy` | property | ✓ |
| JmcPersistenceManager | `IsInitialized` | property | ✓ |
| JmcProfileDataAttribute | `Key` | property | ✓ |
| JmcProfileDataAttribute | `SchemaVersion` | property | ✓ |
| JmcProfileDataAttribute | `WritePolicy` | property | ✓ |
| JmcRunDataAttribute | `Key` | property | ✓ |
| JmcRunDataAttribute | `SchemaVersion` | property | ✓ |
| JmcRunDataAttribute | `WritePolicy` | property | ✓ |
| JmcRunDataSlot`1 | `IsBound` | property | ✓ |
| JmcRunDataSlot`1 | `Key` | property | ✓ |
| JmcRunDataSlot`1 | `Value` | property | ✓ |
| — | `JmcClientRunDataAttribute` | type | ✓ |
| — | `JmcDataSlot`1` | type | ✓ |
| — | `JmcDataWritePolicy` | type | ✓ |
| — | `JmcDataWriteResult` | type | ✓ |
| — | `JmcGlobalDataAttribute` | type | ✓ |
| — | `JmcLocalPreferenceAttribute` | type | ✓ |
| — | `JmcPersistenceManager` | type | ✓ |
| — | `JmcProfileDataAttribute` | type | ✓ |
| — | `JmcRunDataAttribute` | type | ✓ |
| — | `JmcRunDataSlot`1` | type | ✓ |

### `Utils` — 55 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| LogPrefixFlags | `Default` | field | ✓ |
| LogPrefixFlags | `None` | field | ✓ |
| LogPrefixFlags | `Timestamp` | field | ✓ |
| ExprHelper | `ClearAll()` | method | ⚠ private |
| ExprHelper | `ClearAssemblyCache(System.Reflection.Assembly)` | method | ✓ |
| ExprHelper | `CreateAccessorsByEmit``1(System.Reflection.MemberInfo,System.Object)` | method | ⚠ private |
| ExprHelper | `CreateAccessorsByExpressionTree``1(System.Reflection.MemberInfo,System.Object)` | method | ⚠ private |
| ExprHelper | `Expect``2()` | method | ⚠ private |
| ExprHelper | `GetOrCreateAccessors``1(System.Linq.Expressions.Expression{System.Func{``0}},System.Boolean@,System.Reflection.Assembly)` | method | ✓ |
| ExprHelper | `GetOrCreateAccessors``1(System.Linq.Expressions.Expression{System.Func{``0}},System.Reflection.Assembly)` | method | ✓ |
| GameRestart | `RequestRestart(System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| GameRestart | `ShowRestartConfirmationAsync(System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| GameRestart | `TryScheduleRestart(System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| LoggerSnapshot | `LoggerSnapshot(MegaCrit.Sts2.Core.Logging.LogType,JmcModLib.Utils.LogPrefixFlags,System.Boolean,System.Boolean,System.String)` | method | ✓ |
| ModLogger | `Debug(System.String,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Error(System.String,System.Exception,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Error(System.String,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Fatal(System.Exception,System.String,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `GetLogType(System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `GetPrefixFlags(System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `GetSnapshot(System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `HasPrefixFlag(JmcModLib.Utils.LogPrefixFlags,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Info(System.String,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Load(System.String,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `RegisterAssembly(System.Reflection.Assembly,JmcModLib.Utils.LogPrefixFlags,System.Boolean,MegaCrit.Sts2.Core.Logging.LogType,System.Boolean)` | method | ✓ |
| ModLogger | `SetLogType(MegaCrit.Sts2.Core.Logging.LogType,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `SetPrefixFlags(JmcModLib.Utils.LogPrefixFlags,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `TogglePrefixFlag(JmcModLib.Utils.LogPrefixFlags,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Trace(System.String,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `UnregisterAssembly(System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Warn(System.String,System.Exception,System.Reflection.Assembly)` | method | ✓ |
| ModLogger | `Warn(System.String,System.Reflection.Assembly)` | method | ✓ |
| AssemblyLogConfiguration | `IncludeExceptionDetails` | property | ✓ |
| AssemblyLogConfiguration | `LogType` | property | ✓ |
| AssemblyLogConfiguration | `PrefixFlags` | property | ✓ |
| AssemblyLogConfiguration | `ThrowOnFatal` | property | ✓ |
| ExprHelper | `AccessMode` | property | ✓ |
| ExprHelper | `EnableCache` | property | ✓ |
| GameRestart | `IsRestartSupported` | property | ✓ |
| LoggerSnapshot | `Context` | property | ✓ |
| LoggerSnapshot | `IncludeExceptionDetails` | property | ✓ |
| LoggerSnapshot | `LogType` | property | ✓ |
| LoggerSnapshot | `PrefixFlags` | property | ✓ |
| LoggerSnapshot | `ThrowOnFatal` | property | ✓ |
| ModLogger | `DefaultIncludeExceptionDetails` | property | ✓ |
| ModLogger | `DefaultLogType` | property | ✓ |
| ModLogger | `DefaultPrefixFlags` | property | ✓ |
| ModLogger | `DefaultThrowOnFatal` | property | ✓ |
| — | `AssemblyLogConfiguration` | type | ✓ |
| — | `ExprHelper` | type | ✓ |
| — | `GameRestart` | type | ✓ |
| — | `L10n` | type | ✓ |
| — | `LoggerSnapshot` | type | ✓ |
| — | `LogPrefixFlags` | type | ✓ |
| — | `ModLogger` | type | ✓ |

### `Prefabs` — 53 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| JmcReportPopupBodyFormat | `Markdown` | field | ✓ |
| JmcReportPopupBodyFormat | `PlainText` | field | ✓ |
| JmcReportPopupBodyFormat | `RichText` | field | ✓ |
| JmcConfirmationPopup | `ShowConfirmationAsync(MegaCrit.Sts2.Core.Localization.LocString,MegaCrit.Sts2.Core.Localization.LocString,MegaCrit.Sts2.Core.Localization.LocString,MegaCrit.Sts2.Core.Localization.LocString,System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| JmcConfirmationPopup | `ShowConfirmationAsync(System.String,System.String,System.String,System.String,System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| JmcConfirmationPopup | `ShowMessageAsync(MegaCrit.Sts2.Core.Localization.LocString,MegaCrit.Sts2.Core.Localization.LocString,MegaCrit.Sts2.Core.Localization.LocString,System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| JmcConfirmationPopup | `ShowMessageAsync(System.String,System.String,System.String,System.Boolean,System.Reflection.Assembly)` | method | ✓ |
| JmcReportPopup | `Open(JmcModLib.Prefabs.JmcReportPopupOptions,System.Reflection.Assembly)` | method | ✓ |
| JmcReportPopupButton | `JmcReportPopupButton(System.String,System.String,System.Action{JmcModLib.Prefabs.JmcReportPopupHandle},System.Boolean,System.Boolean)` | method | ✓ |
| JmcReportPopupHandle | `Close()` | method | ✓ |
| JmcReportPopupHandle | `SetBody(System.String,JmcModLib.Prefabs.JmcReportPopupBodyFormat)` | method | ✓ |
| JmcReportPopupHandle | `SetBody(System.String,System.Boolean)` | method | ✓ |
| JmcReportPopupHandle | `SetBody(System.String)` | method | ✓ |
| JmcReportPopupHandle | `SetButtonEnabled(System.String,System.Boolean)` | method | ✓ |
| JmcReportPopupHandle | `SetStatus(System.String)` | method | ✓ |
| JmcReportPopupHandle | `SetSubtitle(System.String)` | method | ✓ |
| JmcReportPopupHandle | `SetTitle(System.String)` | method | ✓ |
| JmcSecretInputPopup | `PromptAsync(JmcModLib.Prefabs.JmcSecretInputPopupOptions,System.Reflection.Assembly)` | method | ✓ |
| JmcReportPopup | `IsAvailable` | property | ✓ |
| JmcReportPopupButton | `Action` | property | ✓ |
| JmcReportPopupButton | `CloseOnClick` | property | ✓ |
| JmcReportPopupButton | `Enabled` | property | ✓ |
| JmcReportPopupButton | `Key` | property | ✓ |
| JmcReportPopupButton | `Text` | property | ✓ |
| JmcReportPopupHandle | `IsOpen` | property | ✓ |
| JmcReportPopupOptions | `Body` | property | ✓ |
| JmcReportPopupOptions | `BodyFormat` | property | ✓ |
| JmcReportPopupOptions | `BodyUsesRichText` | property | ✓ |
| JmcReportPopupOptions | `Buttons` | property | ✓ |
| JmcReportPopupOptions | `CloseOnEscape` | property | ✓ |
| JmcReportPopupOptions | `MinimumSize` | property | ✓ |
| JmcReportPopupOptions | `ShowBackstop` | property | ✓ |
| JmcReportPopupOptions | `Status` | property | ✓ |
| JmcReportPopupOptions | `Subtitle` | property | ✓ |
| JmcReportPopupOptions | `Title` | property | ✓ |
| JmcSecretInputPopup | `IsAvailable` | property | ✓ |
| JmcSecretInputPopupOptions | `CancelText` | property | ✓ |
| JmcSecretInputPopupOptions | `ConfirmText` | property | ✓ |
| JmcSecretInputPopupOptions | `Description` | property | ✓ |
| JmcSecretInputPopupOptions | `EmptyText` | property | ✓ |
| JmcSecretInputPopupOptions | `MinimumSize` | property | ✓ |
| JmcSecretInputPopupOptions | `Placeholder` | property | ✓ |
| JmcSecretInputPopupOptions | `ProtectionLevel` | property | ✓ |
| JmcSecretInputPopupOptions | `ShowBackstop` | property | ✓ |
| JmcSecretInputPopupOptions | `Title` | property | ✓ |
| — | `JmcConfirmationPopup` | type | ✓ |
| — | `JmcReportPopup` | type | ✓ |
| — | `JmcReportPopupBodyFormat` | type | ✓ |
| — | `JmcReportPopupButton` | type | ✓ |
| — | `JmcReportPopupHandle` | type | ✓ |
| — | `JmcReportPopupOptions` | type | ✓ |
| — | `JmcSecretInputPopup` | type | ✓ |
| — | `JmcSecretInputPopupOptions` | type | ✓ |

### `UI.PauseMenu` — 46 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| PauseMenuButtonAnchor | `AfterCompendium` | field | ✓ |
| PauseMenuButtonAnchor | `AfterResume` | field | ✓ |
| PauseMenuButtonAnchor | `AfterSettings` | field | ✓ |
| PauseMenuButtonAnchor | `BeforeExitActions` | field | ✓ |
| PauseMenuButtonAnchor | `End` | field | ✓ |
| PauseMenuButtonAttribute | `PauseMenuButtonAttribute(System.String)` | method | ✓ |
| PauseMenuButtonOptions | `PauseMenuButtonOptions()` | method | ✓ |
| PauseMenuButtonOptions | `PauseMenuButtonOptions(System.String,System.String)` | method | ✓ |
| PauseMenuRegistry | `GetEntries(System.Reflection.Assembly)` | method | ✓ |
| PauseMenuRegistry | `RegisterButton(JmcModLib.UI.PauseMenu.PauseMenuButtonOptions,System.Action,System.Reflection.Assembly)` | method | ✓ |
| PauseMenuRegistry | `RegisterButton(JmcModLib.UI.PauseMenu.PauseMenuButtonOptions,System.Action{JmcModLib.UI.PauseMenu.PauseMenuButtonContext},System.Reflection.Assembly)` | method | ✓ |
| PauseMenuRegistry | `RegisterButton(JmcModLib.UI.PauseMenu.PauseMenuButtonOptions,System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Threading.Tasks.Task},System.Reflection.Assembly)` | method | ✓ |
| PauseMenuRegistry | `RegisterButton(JmcModLib.UI.PauseMenu.PauseMenuButtonOptions,System.Func{System.Threading.Tasks.Task},System.Reflection.Assembly)` | method | ✓ |
| PauseMenuRegistry | `UnregisterAssembly(System.Reflection.Assembly)` | method | ✓ |
| PauseMenuRegistry | `UnregisterButton(System.String,System.Reflection.Assembly)` | method | ✓ |
| PauseMenuButtonAttribute | `Anchor` | property | ✓ |
| PauseMenuButtonAttribute | `CloseMenuOnClick` | property | ✓ |
| PauseMenuButtonAttribute | `Color` | property | ✓ |
| PauseMenuButtonAttribute | `Key` | property | ✓ |
| PauseMenuButtonAttribute | `LocTable` | property | ✓ |
| PauseMenuButtonAttribute | `Order` | property | ✓ |
| PauseMenuButtonAttribute | `Text` | property | ✓ |
| PauseMenuButtonAttribute | `TextKey` | property | ✓ |
| PauseMenuButtonContext | `Assembly` | property | ✓ |
| PauseMenuButtonContext | `Button` | property | ✓ |
| PauseMenuButtonContext | `IsGameOver` | property | ✓ |
| PauseMenuButtonContext | `IsMultiplayerClient` | property | ✓ |
| PauseMenuButtonContext | `IsRunInProgress` | property | ✓ |
| PauseMenuButtonContext | `Menu` | property | ✓ |
| PauseMenuButtonContext | `Mod` | property | ✓ |
| PauseMenuButtonContext | `RunState` | property | ✓ |
| PauseMenuButtonOptions | `Anchor` | property | ✓ |
| PauseMenuButtonOptions | `CloseMenuOnClick` | property | ✓ |
| PauseMenuButtonOptions | `Color` | property | ✓ |
| PauseMenuButtonOptions | `EnabledWhen` | property | ✓ |
| PauseMenuButtonOptions | `Key` | property | ✓ |
| PauseMenuButtonOptions | `LocTable` | property | ✓ |
| PauseMenuButtonOptions | `Order` | property | ✓ |
| PauseMenuButtonOptions | `Text` | property | ✓ |
| PauseMenuButtonOptions | `TextKey` | property | ✓ |
| PauseMenuButtonOptions | `VisibleWhen` | property | ✓ |
| — | `PauseMenuButtonAnchor` | type | ✓ |
| — | `PauseMenuButtonAttribute` | type | ✓ |
| — | `PauseMenuButtonContext` | type | ✓ |
| — | `PauseMenuButtonOptions` | type | ✓ |
| — | `PauseMenuRegistry` | type | ✓ |

### `Core` — 42 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| ModRegistry | `OnRegistered` | event | ✓ |
| ModRegistry | `OnUnregistered` | event | ✓ |
| ModRegistry | `GetContext(System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `GetDisplayName(System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `GetModId(System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `GetTag(System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `GetVersion(System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `IsRegistered(System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `Register(System.Boolean,System.Object,System.String,System.String,System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `Register(System.Boolean,System.String,System.String,System.String,System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `Register(System.String,System.String,System.String,System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `Register``1()` | method | ✓ |
| ModRegistry | `Register``1(System.Boolean,System.String,System.String,System.String)` | method | ✓ |
| ModRegistry | `Register``1(System.Boolean)` | method | ✓ |
| ModRegistry | `Register``1(System.String,System.String,System.String)` | method | ✓ |
| ModRegistry | `TryGetContext(JmcModLib.Core.ModContext@,System.Reflection.Assembly)` | method | ✓ |
| ModRegistry | `Unregister(System.Reflection.Assembly)` | method | ✓ |
| ModRuntime | `FindLoadedMod(System.String)` | method | ✓ |
| ModRuntime | `FindModById(System.String)` | method | ✓ |
| RegistryBuilder | `Done()` | method | ✓ |
| RegistryBuilder | `RegisterButton(System.String,System.Action,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.Int32,JmcModLib.Config.UI.UIButtonColor)` | method | ✓ |
| RegistryBuilder | `RegisterButton(System.String@,System.String,System.Action,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.String,System.Int32,JmcModLib.Config.UI.UIButtonColor)` | method | ✓ |
| RegistryBuilder | `RegisterPauseMenuButton(System.String,System.String,System.Action,System.Int32,JmcModLib.UI.PauseMenu.PauseMenuButtonAnchor,System.String,System.String,System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Boolean,JmcModLib.Config.UI.UIButtonColor)` | method | ✓ |
| RegistryBuilder | `RegisterPauseMenuButton(System.String,System.String,System.Action{JmcModLib.UI.PauseMenu.PauseMenuButtonContext},System.Int32,JmcModLib.UI.PauseMenu.PauseMenuButtonAnchor,System.String,System.String,System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Boolean,JmcModLib.Config.UI.UIButtonColor)` | method | ✓ |
| RegistryBuilder | `RegisterPauseMenuButton(System.String,System.String,System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Threading.Tasks.Task},System.Int32,JmcModLib.UI.PauseMenu.PauseMenuButtonAnchor,System.String,System.String,System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Boolean,JmcModLib.Config.UI.UIButtonColor)` | method | ✓ |
| RegistryBuilder | `RegisterPauseMenuButton(System.String,System.String,System.Func{System.Threading.Tasks.Task},System.Int32,JmcModLib.UI.PauseMenu.PauseMenuButtonAnchor,System.String,System.String,System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Func{JmcModLib.UI.PauseMenu.PauseMenuButtonContext,System.Boolean},System.Boolean,JmcModLib.Config.UI.UIButtonColor)` | method | ✓ |
| RegistryBuilder | `RegisterSecret(JmcModLib.Security.JmcSecretSlot,System.String,JmcModLib.Security.JmcSecretOptions)` | method | ✓ |
| RegistryBuilder | `RegisterSecret(JmcModLib.Security.JmcSecretSlot@,System.String,JmcModLib.Security.JmcSecretOptions)` | method | ✓ |
| RegistryBuilder | `RegisterSecret(System.String,JmcModLib.Security.JmcSecretOptions)` | method | ✓ |
| RegistryBuilder | `WithConfigStorage(JmcModLib.Config.Storage.IConfigStorage)` | method | ✓ |
| RegistryBuilder | `WithDisplayName(System.String)` | method | ✓ |
| RegistryBuilder | `WithVersion(System.String)` | method | ✓ |
| ModContext | `Assembly` | property | ✓ |
| ModContext | `DisplayName` | property | ✓ |
| ModContext | `IsCompleted` | property | ✓ |
| ModContext | `LoggerContext` | property | ✓ |
| ModContext | `ModId` | property | ✓ |
| ModContext | `Tag` | property | ✓ |
| ModContext | `Version` | property | ✓ |
| — | `ModContext` | type | ✓ |
| — | `ModRegistry` | type | ✓ |
| — | `RegistryBuilder` | type | ✓ |

### `Multiplayer` — 25 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| OptionalNetworkFeatureHandle | `EffectiveEnabledChanged` | event | ✓ |
| OptionalNetworkFeatureHandle | `StateChanged` | event | ✓ |
| OptionalNetworkFeatureApplyState | `Applied` | field | ✓ |
| OptionalNetworkFeatureApplyState | `PendingNetworkIdle` | field | ✓ |
| OptionalNetworkFeatureApplyState | `RestartRequired` | field | ✓ |
| OptionalNetworkFeatureAttribute | `OptionalNetworkFeatureAttribute(System.String,System.Type)` | method | ✓ |
| OptionalNetworkFeatures | `Get(System.String,System.Reflection.Assembly)` | method | ✓ |
| OptionalNetworkFeatures | `Get``1(System.String)` | method | ✓ |
| OptionalNetworkFeatures | `TryGet(System.String,JmcModLib.Multiplayer.OptionalNetworkFeatureHandle@,System.Reflection.Assembly)` | method | ✓ |
| OptionalNetworkMismatch | `ShouldHandle(MegaCrit.Sts2.Core.Entities.Multiplayer.NetErrorInfo)` | method | ✓ |
| OptionalNetworkFeatureAttribute | `CompatibilityVersion` | property | ✓ |
| OptionalNetworkFeatureAttribute | `Id` | property | ✓ |
| OptionalNetworkFeatureAttribute | `MessageMarkerType` | property | ✓ |
| OptionalNetworkFeatureHandle | `ApplyState` | property | ✓ |
| OptionalNetworkFeatureHandle | `CompatibilityVersion` | property | ✓ |
| OptionalNetworkFeatureHandle | `EffectiveEnabled` | property | ✓ |
| OptionalNetworkFeatureHandle | `HasPendingApply` | property | ✓ |
| OptionalNetworkFeatureHandle | `Id` | property | ✓ |
| OptionalNetworkFeatureHandle | `ModId` | property | ✓ |
| OptionalNetworkFeatureHandle | `RequestedEnabled` | property | ✓ |
| — | `OptionalNetworkFeatureApplyState` | type | ✓ |
| — | `OptionalNetworkFeatureAttribute` | type | ✓ |
| — | `OptionalNetworkFeatureHandle` | type | ✓ |
| — | `OptionalNetworkFeatures` | type | ✓ |
| — | `OptionalNetworkMismatch` | type | ✓ |

### `Input` — 24 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| GodotActionInputBackend | `Initialize()` | method | ⚠ int. type |
| GodotActionInputBackend | `Process()` | method | ⚠ int. type |
| GodotActionInputBackend | `Shutdown()` | method | ⚠ int. type |
| IJmcInputBackend | `Initialize()` | method | ⚠ int. type |
| IJmcInputBackend | `Process()` | method | ⚠ int. type |
| IJmcInputBackend | `Shutdown()` | method | ⚠ int. type |
| JmcInputManager | `Initialize()` | method | ⚠ int. type |
| JmcInputManager | `Process()` | method | ⚠ int. type |
| JmcInputManager | `Shutdown()` | method | ⚠ int. type |
| SteamInputBackend | `Initialize()` | method | ⚠ int. type |
| SteamInputBackend | `Process()` | method | ⚠ int. type |
| SteamInputBackend | `Shutdown()` | method | ⚠ int. type |
| GodotActionInputBackend | `Name` | property | ⚠ int. type |
| IJmcInputBackend | `Name` | property | ⚠ int. type |
| JmcInputManager | `RegisteredBackends` | property | ⚠ int. type |
| SteamInputBackend | `Name` | property | ⚠ int. type |
| — | `GodotActionInputBackend` | type | ⚠ int. type |
| — | `IJmcInputBackend` | type | ⚠ int. type |
| — | `JmcInputActionRegistry` | type | ⚠ int. type |
| — | `JmcInputManager` | type | ⚠ int. type |
| — | `JmcSteamInputManifestInstaller` | type | ⚠ int. type |
| — | `SteamInputBackend` | type | ⚠ int. type |
| — | `SteamInputManifestMerger` | type | ⚠ int. type |
| — | `SteamInputPatches` | type | ⚠ int. type |

### `Compat` — 19 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| ModCompat | `ContainsAssembly(MegaCrit.Sts2.Core.Modding.Mod,System.Reflection.Assembly)` | method | ✓ |
| ModCompat | `GetAssemblies(MegaCrit.Sts2.Core.Modding.Mod)` | method | ✓ |
| ModCompat | `GetKnownMods()` | method | ✓ |
| ModCompat | `GetLoadedMods()` | method | ✓ |
| ModCompat | `GetManifest(MegaCrit.Sts2.Core.Modding.Mod)` | method | ✓ |
| ModCompat | `GetManifestId(MegaCrit.Sts2.Core.Modding.ModManifest)` | method | ✓ |
| ModCompat | `GetManifestName(MegaCrit.Sts2.Core.Modding.ModManifest)` | method | ✓ |
| ModCompat | `GetManifestVersion(MegaCrit.Sts2.Core.Modding.ModManifest)` | method | ✓ |
| ModCompat | `GetPckName(MegaCrit.Sts2.Core.Modding.Mod)` | method | ✓ |
| ModCompat | `GetPrimaryAssembly(MegaCrit.Sts2.Core.Modding.Mod)` | method | ✓ |
| ModCompat | `IsLoaded(MegaCrit.Sts2.Core.Modding.Mod)` | method | ✓ |
| MultiplayerCompat | `GetConnectedHostPeerIds(MegaCrit.Sts2.Core.Multiplayer.Game.INetHostGameService)` | method | ✓ |
| MultiplayerCompat | `GetLoadRunLobbyPlayerIds(MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.LoadRunLobby)` | method | ✓ |
| MultiplayerCompat | `GetRunLobbyPlayerIds(MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.RunLobby)` | method | ✓ |
| MultiplayerCompat | `TryGetConnectionExtraInfo(MegaCrit.Sts2.Core.Entities.Multiplayer.NetErrorInfo,MegaCrit.Sts2.Core.Entities.Multiplayer.ConnectionFailureExtraInfo@)` | method | ✓ |
| MultiplayerCompat | `TryGetJoinFlowNetService(MegaCrit.Sts2.Core.Multiplayer.Game.JoinFlow,MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService@)` | method | ✓ |
| MultiplayerCompat | `TryReadJoinFlowNetService(MegaCrit.Sts2.Core.Multiplayer.Game.JoinFlow,MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService@)` | method | ⚠ internal |
| — | `ModCompat` | type | ✓ |
| — | `MultiplayerCompat` | type | ✓ |

### `Utils.ExprHelper` — 7 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| MemberAccessMode | `Default` | field | ✓ |
| MemberAccessMode | `Emit` | field | ✓ |
| MemberAccessMode | `ExpressionTree` | field | ✓ |
| MemberAccessMode | `Reflection` | field | ✓ |
| MemberAccessors | `MemberAccessors(System.Delegate,System.Delegate)` | method | ✓ |
| — | `MemberAccessMode` | type | ✓ |
| — | `MemberAccessors` | type | ✓ |

### `Config.Entry` — 4 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| ConfigEntry | `ConfigEntry(System.Reflection.Assembly,System.String,System.String,System.String,JmcModLib.Config.ConfigAttribute,JmcModLib.Config.UI.UIConfigAttribute)` | method | ⚠ protected |
| ConfigEntry | `DropdownOptionsProviderAttribute` | property | ✓ |
| ConfigEntry | `VisibleWhenAttribute` | property | ✓ |
| — | `ConfigEntry` | type | ✓ |

### `Config` — 3 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| ConfigAttribute | `ConfigAttribute(System.String,System.String,System.String)` | method | ✓ |
| — | `ConfigAttribute` | type | ✓ |
| — | `ConfigManager` | type | ✓ |

### `Config.Storage` — 2 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| — | `JsonConfigStorage` | type | ✓ |
| — | `NewtonsoftConfigStorage` | type | ✓ |

### `Core.AttributeRouter` — 2 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| — | `AttributeRouter` | type | ✓ |
| — | `IAttributeHandler` | type | ✓ |

### `Reflection.MethodAccessor` — 1 members

| Type | Member | Kind | Bin |
|---|---|---|---|
| — | `ParamSignature` | type | ⚠ int. type |
