// GateConfig.cs -- gate-config.json 的强类型映射 (System.Text.Json)。
namespace Spire1.BuildGates;

public sealed class GateConfig
{
    public string? AssemblyName { get; set; }
    public string? ManifestRelativePath { get; set; }
    public ForbiddenAssemblyRefs? ForbiddenAssemblyRefs { get; set; }
    public ManifestConsistency? ManifestConsistency { get; set; }
    public ForbiddenTypeDefs? ForbiddenTypeDefs { get; set; }
}

public sealed class ForbiddenAssemblyRefs
{
    public List<string>? Names { get; set; }
}

public sealed class ManifestConsistency
{
    public List<string>? NonModExactNames { get; set; }
    public List<string>? NonModPrefixes { get; set; }
    public ManifestOptionalIds? ManifestOptionalIds { get; set; }
}

public sealed class ManifestOptionalIds
{
    public List<string>? Ids { get; set; }
}

public sealed class ForbiddenTypeDefs
{
    public List<string>? FullNames { get; set; }
    public List<string>? ForbiddenNamespaces { get; set; }
}

/// <summary>单个门禁的结果。</summary>
public sealed class GateResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public required string Summary { get; init; }
    public required List<string> Details { get; init; }

    public static GateResult Pass(string name, string summary) =>
        new() { Name = name, Passed = true, Summary = summary, Details = [] };
    public static GateResult PassWith(string name, string summary, List<string> details) =>
        new() { Name = name, Passed = true, Summary = summary, Details = details };
    public static GateResult FailWith(string name, string summary, List<string> details) =>
        new() { Name = name, Passed = false, Summary = summary, Details = details };
}
