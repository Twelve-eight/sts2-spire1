using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using HarmonyLib;

// This runner calls the compiled Spire1 DLL against the real host RunState type. Only Godot's
// unavailable headless OS/logging entry points are intercepted, as in snapshot-probe; no content
// gate, latch, RunState binding or save method is replaced. Bare RunState instances are identity
// carriers only: their constructors, run lifecycle, save pipeline and multiplayer are not covered.
if (args.Length != 3)
    throw new ArgumentException("Usage: ContentLatchProbe <Spire1.dll> <Sts2DataDir> <BaseLibDir>");
string modFile = Path.GetFullPath(args[0]);
string dataDir = Path.GetFullPath(args[1]);
string baseDir = Path.GetFullPath(args[2]);
foreach (string item in new[] { modFile, dataDir, baseDir })
    if (!item.StartsWith("G:", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("All inputs must be on G:.");
foreach (string file in new[] { modFile, Path.Combine(dataDir, "sts2.dll"), Path.Combine(dataDir, "GodotSharp.dll"), Path.Combine(baseDir, "BaseLib.dll") })
    if (!File.Exists(file)) throw new FileNotFoundException("Required actual dependency is missing", file);

string[] directories = [Path.GetDirectoryName(modFile)!, dataDir, baseDir];
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    foreach (string directory in directories)
    {
        string candidate = Path.Combine(directory, name.Name + ".dll");
        if (File.Exists(candidate)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate);
    }
    return null;
};
Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dataDir, "sts2.dll"));
Assembly godot = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dataDir, "GodotSharp.dll"));
Assembly mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(modFile);
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
var support = new Harmony("Spire1.ContentLatchProbe.Platform");
MethodInfo platform(string name) => typeof(HeadlessPlatform).GetMethod(name, flags)!;
support.Patch(godot.GetType("Godot.OS", true)!.GetMethod("GetCmdlineArgs", flags)!, prefix: new HarmonyMethod(platform(nameof(HeadlessPlatform.Cmdline))));
support.Patch(godot.GetType("Godot.OS", true)!.GetMethod("HasFeature", flags)!, prefix: new HarmonyMethod(platform(nameof(HeadlessPlatform.Feature))));
support.Patch(game.GetType("MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter", true)!.GetMethod("Print", flags)!, prefix: new HarmonyMethod(platform(nameof(HeadlessPlatform.Log))));

int failures = 0;
int cases = 0;
try
{
    Type config = mod.GetType("Spire1.Spire1Code.Config.Spire1Config", true)!;
    Type content = mod.GetType("Spire1.Spire1Code.Run.Spire1RunContent", true)!;
    Type runState = game.GetType("MegaCrit.Sts2.Core.Runs.RunState", true)!;
    object runA = RuntimeHelpers.GetUninitializedObject(runState);
    object runB = RuntimeHelpers.GetUninitializedObject(runState);
    object unbound = RuntimeHelpers.GetUninitializedObject(runState);
    PropertyInfo prop(Type type, string name) => type.GetProperty(name, flags) ?? throw new MissingMemberException(type.FullName, name);
    MethodInfo latch = content.GetMethod("LatchForNewRun", flags, null, [runState, typeof(bool)], null)
        ?? throw new MissingMethodException(content.FullName, "LatchForNewRun(RunState,bool)");
    MethodInfo restore = content.GetMethod("RestoreFromSave", flags, null, [typeof(bool)], null)
        ?? throw new MissingMethodException(content.FullName, "RestoreFromSave(bool)");
    MethodInfo lookup = content.GetMethod("TryReadInstanceBinding", flags, null, [typeof(object)], null)
        ?? throw new MissingMethodException(content.FullName, "TryReadInstanceBinding(object)");
    MethodInfo reset = content.GetMethod("ResetForMenu", flags) ?? throw new MissingMethodException(content.FullName, "ResetForMenu");
    bool get(Type type, string name) => (bool)prop(type, name).GetValue(null)!;
    void set(string name, bool value) => prop(config, name).SetValue(null, value);
    void bind(object run, bool value) => latch.Invoke(null, [run, value]);
    void load(bool value) => restore.Invoke(null, [value]);
    object? binding(object run) => lookup.Invoke(null, [run]);
    void check(string name, Func<bool> assertion)
    {
        cases++;
        try
        {
            if (assertion()) Console.WriteLine($"PASS {name}");
            else { failures++; Console.Error.WriteLine($"FAIL {name}"); }
        }
        catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {name}: {e.GetBaseException()}"); }
    }
    string sha(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 32768, FileOptions.SequentialScan);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
    Console.WriteLine($"IDENTITY mod={modFile} sha256={sha(modFile)} mvid={mod.ManifestModule.ModuleVersionId}");
    Console.WriteLine($"IDENTITY game={game.Location} sha256={sha(game.Location)} mvid={game.ManifestModule.ModuleVersionId}");
    set("EnableSts1Content", true);
    set("EnableSts1Cards", true);
    set("EnableSts1Relics", true);
    check("default run latch enabled", () => get(content, "ContentActiveThisRun"));
    check("next-run registration default enabled", () => get(config, "RegisterContentNextRun"));
    check("new run false closes cards and relics", () => { bind(runA, false); return !get(content, "ContentActiveThisRun") && !get(config, "CardsEnabled") && !get(config, "RelicsEnabled") && Equals(binding(runA), false); });
    check("new run true opens cards and relics", () => { bind(runB, true); return get(content, "ContentActiveThisRun") && get(config, "CardsEnabled") && get(config, "RelicsEnabled") && Equals(binding(runB), true); });
    check("restore false overrides current latch", () => { load(false); return !get(content, "ContentActiveThisRun") && !get(config, "CardsEnabled"); });
    check("restore true overrides current latch", () => { load(true); return get(content, "ContentActiveThisRun") && get(config, "CardsEnabled"); });
    check("relic gate independent of cards", () => { set("EnableSts1Relics", false); bool ok = !get(config, "RelicsEnabled") && get(config, "CardsEnabled"); set("EnableSts1Relics", true); return ok; });
    check("master gate closes both", () => { set("EnableSts1Content", false); bool ok = !get(config, "CardsEnabled") && !get(config, "RelicsEnabled"); set("EnableSts1Content", true); return ok; });
    check("run bindings remain distinct after restore", () => Equals(binding(runA), false) && Equals(binding(runB), true));
    check("unbound real RunState has no guessed setting", () => binding(unbound) is null);
    check("menu reset does not rewrite prior run bindings", () => { bind(runA, false); reset.Invoke(null, null); return get(content, "ContentActiveThisRun") && Equals(binding(runA), false) && Equals(binding(runB), true); });
}
finally { support.UnpatchAll("Spire1.ContentLatchProbe.Platform"); }
Console.WriteLine($"cases={cases} failures={failures}; boundary=real compiled latch and host RunState identity; headless OS/log intercepted; no RunState constructor, save, Harmony lifecycle or multiplayer");
return failures == 0 ? 0 : 1;

public static class HeadlessPlatform
{
    public static bool Cmdline(ref string[] __result) { __result = []; return false; }
    public static bool Feature(ref bool __result) { __result = false; return false; }
    public static bool Log() => false;
}
