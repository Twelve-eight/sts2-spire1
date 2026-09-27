// Program.cs -- Spire1 发布门禁入口。
//
// 用法:
//   Spire1ReleaseGate --dll <path> [--config <gate-config.json>] [--manifest <Spire1.json>]
//                     [--gates assemblyref,manifest,typedef] [--json <out.json>]
//
//   --dll       必填。要校验的已构建 Spire1.dll。
//   --config    可选。默认取工具自身目录下的 gate-config.json。
//   --manifest  可选。默认取 config 中 manifestRelativePath (相对 config 目录) 解析。
//   --gates     可选。逗号分隔要跑的门禁子集; 缺省=全部。
//   --json      可选。把机器可读结果另存一份 JSON。
//
// 退出码 (供 CI / 发布脚本判定, 见 docs 门禁 checklist):
//   0 = 全部选定门禁通过
//   2 = 至少一个门禁失败 (命中禁止 AssemblyRef / TypeDef, 或 manifest 不一致)
//   3 = 用法/输入错误 (缺参、DLL/config 找不到、解析失败)
//
// 三道门禁都对**编译后字节的结构**断言, 不做字符串 contains:
//   #1 assemblyref  -- AssemblyRef 表禁止出现指定 mod 程序集
//   #2 manifest     -- AssemblyRef 中的 mod 程序集必须都在 Spire1.json dependencies 声明
//   #3 typedef      -- TypeDef 表禁止出现指定实验/桥接/held-back/Debug 类型 (精确全名 + 整命名空间)

using System.Text.Json;
using Spire1.BuildGates;

static int Fail(string msg) { Console.Error.WriteLine("[gate] 用法错误: " + msg); return 3; }

// ---- 解析参数 ----
string? dllPath = null, configPath = null, manifestPath = null, gatesArg = null, jsonOut = null;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dll": dllPath = ArgVal(ref i); break;
        case "--config": configPath = ArgVal(ref i); break;
        case "--manifest": manifestPath = ArgVal(ref i); break;
        case "--gates": gatesArg = ArgVal(ref i); break;
        case "--json": jsonOut = ArgVal(ref i); break;
        case "-h": case "--help": PrintUsage(); return 0;
        default: return Fail("未知参数 " + args[i]);
    }
}
string? ArgVal(ref int i) => (i + 1 < args.Length) ? args[++i] : null;

if (string.IsNullOrEmpty(dllPath)) return Fail("缺 --dll <path>");
if (!File.Exists(dllPath)) return Fail("DLL 不存在: " + dllPath);

configPath ??= Path.Combine(AppContext.BaseDirectory, "gate-config.json");
// BaseDirectory 是 bin 输出目录; 源目录也放了一份, 回退到工具源目录旁。
if (!File.Exists(configPath))
{
    string alt = Path.Combine(FindToolSourceDir(), "gate-config.json");
    if (File.Exists(alt)) configPath = alt;
}
if (!File.Exists(configPath)) return Fail("config 不存在: " + configPath);

GateConfig cfg;
try
{
    cfg = JsonSerializer.Deserialize<GateConfig>(File.ReadAllText(configPath), JsonOpts())
          ?? throw new Exception("config 反序列化为 null");
}
catch (Exception e) { return Fail("解析 config 失败: " + e.Message); }

// manifest 路径解析: 显式 --manifest 优先; 否则 config.manifestRelativePath 相对 config 目录。
if (string.IsNullOrEmpty(manifestPath))
{
    string cfgDir = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
    manifestPath = Path.GetFullPath(Path.Combine(cfgDir, cfg.ManifestRelativePath ?? "../../mod/Spire1.json"));
}

var runSet = ParseGates(gatesArg);

// ---- 解析 DLL 结构 ----
PeMetadata meta;
try { meta = PeMetadataReader.Read(dllPath); }
catch (Exception e) { return Fail("解析 DLL 元数据失败 (" + dllPath + "): " + e.Message); }

Console.WriteLine($"[gate] 目标 DLL: {dllPath}");
Console.WriteLine($"[gate] AssemblyRef {meta.AssemblyRefNames.Count} 个, TypeDef {meta.TypeDefs.Count} 个");
Console.WriteLine($"[gate] 运行门禁: {string.Join(",", runSet)}");

var results = new List<GateResult>();

if (runSet.Contains("assemblyref"))
    results.Add(GateAssemblyRef(meta, cfg));
if (runSet.Contains("manifest"))
    results.Add(GateManifest(meta, cfg, manifestPath));
if (runSet.Contains("typedef"))
    results.Add(GateTypeDef(meta, cfg));

// ---- 汇总 ----
bool anyFail = results.Any(r => !r.Passed);
Console.WriteLine();
foreach (var r in results)
{
    Console.WriteLine($"[gate] {(r.Passed ? "PASS" : "FAIL")}  {r.Name}: {r.Summary}");
    foreach (var d in r.Details) Console.WriteLine("         - " + d);
}
Console.WriteLine();
Console.WriteLine(anyFail ? "[gate] 结果: FAIL (发布被拦截)" : "[gate] 结果: PASS (全部门禁通过)");

if (!string.IsNullOrEmpty(jsonOut))
{
    var payload = new
    {
        dll = dllPath,
        passed = !anyFail,
        gates = results.Select(r => new { r.Name, r.Passed, r.Summary, r.Details })
    };
    File.WriteAllText(jsonOut, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("[gate] JSON 结果已写: " + jsonOut);
}

return anyFail ? 2 : 0;


// ================= 门禁 #1: 禁止硬引用的 mod 程序集 =================
static GateResult GateAssemblyRef(PeMetadata meta, GateConfig cfg)
{
    var forbidden = new HashSet<string>(cfg.ForbiddenAssemblyRefs?.Names ?? [], StringComparer.OrdinalIgnoreCase);
    // AssemblyRef 表里出现即命中 -- 这是编译器实际写进引用表的硬依赖, #if 两分支同名类型无法伪造。
    var hits = meta.AssemblyRefNames.Where(forbidden.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    if (hits.Count == 0)
        return GateResult.Pass("assemblyref-forbidden", $"AssemblyRef 表不含任何禁止的 mod 程序集 (检查了 {forbidden.Count} 个)");
    return GateResult.FailWith("assemblyref-forbidden",
        $"命中 {hits.Count} 个禁止硬引用的程序集",
        hits.Select(h => $"禁止的 AssemblyRef: {h}  (应改为纯反射运行期解析, 不得编进引用表)").ToList());
}

// ================= 门禁 #2: manifest / 二进制依赖一致性 =================
static GateResult GateManifest(PeMetadata meta, GateConfig cfg, string manifestPath)
{
    var details = new List<string>();
    if (!File.Exists(manifestPath))
        return GateResult.FailWith("manifest-consistency", "manifest 找不到: " + manifestPath, ["请用 --manifest 指定, 或修正 config.manifestRelativePath"]);

    // 读 manifest dependencies[].id
    HashSet<string> declared;
    try
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (doc.RootElement.TryGetProperty("dependencies", out var deps) && deps.ValueKind == JsonValueKind.Array)
            foreach (var d in deps.EnumerateArray())
                if (d.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    declared.Add(id.GetString()!);
    }
    catch (Exception e) { return GateResult.FailWith("manifest-consistency", "解析 manifest 失败: " + e.Message, []); }

    var mc = cfg.ManifestConsistency ?? new ManifestConsistency();
    var nonModExact = new HashSet<string>(mc.NonModExactNames ?? [], StringComparer.OrdinalIgnoreCase);
    var nonModPrefixes = mc.NonModPrefixes ?? [];

    bool IsModAssembly(string name)
    {
        if (nonModExact.Contains(name)) return false;
        foreach (var pre in nonModPrefixes)
            if (name.StartsWith(pre, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    // 二进制里实际引用的 mod 程序集
    var referencedMods = meta.AssemblyRefNames.Where(IsModAssembly).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    details.Add("二进制引用的 mod 程序集: " + (referencedMods.Count == 0 ? "(无)" : string.Join(", ", referencedMods)));
    details.Add("manifest 声明的依赖 id: " + (declared.Count == 0 ? "(无)" : string.Join(", ", declared)));

    // 差异 A: 引用了但没声明 (这是历史上 AutoAnthony 的坑)
    var undeclared = referencedMods.Where(m => !declared.Contains(m)).ToList();
    // 差异 B (信息性, 不单独判失败): 声明了但没引用 (可能是纯运行期软依赖, 由 optionalIds 白名单豁免)
    var optional = new HashSet<string>(mc.ManifestOptionalIds?.Ids ?? [], StringComparer.OrdinalIgnoreCase);

    if (undeclared.Count > 0)
    {
        details.InsertRange(0, undeclared.Select(m =>
            $"二进制硬引用了 '{m}' 但 manifest 未在 dependencies 声明 -> 或补声明, 或改为纯反射不硬引用"));
        return GateResult.FailWith("manifest-consistency", $"{undeclared.Count} 个二进制依赖未在 manifest 声明", details);
    }
    return GateResult.PassWith("manifest-consistency", "二进制引用的 mod 程序集都已在 manifest 声明", details);
}

// ================= 门禁 #3: 条件编译符号产物 (TypeDef 黑名单) =================
static GateResult GateTypeDef(PeMetadata meta, GateConfig cfg)
{
    var ft = cfg.ForbiddenTypeDefs ?? new ForbiddenTypeDefs();
    var forbiddenFull = new HashSet<string>(ft.FullNames ?? [], StringComparer.Ordinal);
    var forbiddenNs = ft.ForbiddenNamespaces ?? [];

    var hits = new List<string>();
    foreach (var td in meta.TypeDefs)
    {
        string full = td.FullName;
        if (forbiddenFull.Contains(full))
        {
            hits.Add($"禁止类型 (精确): {full}");
            continue;
        }
        // 整命名空间前缀匹配, 以 '.' 收尾避免同前缀命名空间 (如 ...Forms vs ...FormsExtra) 误伤;
        // 命名空间恰好等于禁止值本身也算命中。
        foreach (var ns in forbiddenNs)
        {
            if (td.Namespace == ns || td.Namespace.StartsWith(ns + ".", StringComparison.Ordinal))
            {
                hits.Add($"禁止命名空间 '{ns}' 下的类型: {full}");
                break;
            }
        }
    }

    if (hits.Count == 0)
        return GateResult.Pass("typedef-forbidden",
            $"TypeDef 表不含任何实验/桥接/held-back/Debug 类型 (精确 {forbiddenFull.Count} 项 + {forbiddenNs.Count} 个禁含命名空间)");
    return GateResult.FailWith("typedef-forbidden", $"命中 {hits.Count} 个禁止类型", hits);
}


// ---- helpers ----
static HashSet<string> ParseGates(string? arg)
{
    var all = new[] { "assemblyref", "manifest", "typedef" };
    if (string.IsNullOrWhiteSpace(arg)) return new HashSet<string>(all, StringComparer.OrdinalIgnoreCase);
    return new HashSet<string>(arg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                               StringComparer.OrdinalIgnoreCase);
}

static string FindToolSourceDir()
{
    // 从 bin 输出往上找回工具源目录 (含 gate-config.json 的那层)。
    var d = new DirectoryInfo(AppContext.BaseDirectory);
    for (int i = 0; i < 6 && d != null; i++, d = d.Parent)
        if (File.Exists(Path.Combine(d.FullName, "gate-config.json"))) return d.FullName;
    return AppContext.BaseDirectory;
}

static JsonSerializerOptions JsonOpts() => new()
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
};

static void PrintUsage()
{
    Console.WriteLine("Spire1ReleaseGate --dll <Spire1.dll> [--config <gate-config.json>] [--manifest <Spire1.json>] [--gates a,b,c] [--json <out>]");
    Console.WriteLine("退出码: 0=全通过 2=门禁失败 3=用法/输入错误");
}
