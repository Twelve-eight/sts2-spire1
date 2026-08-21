using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Usage: typedump <assembly.dll> [--members] [filter ...]
// Reads PE metadata directly (no assembly load, no deps). Prints type full names
// (optionally with fields/methods) whose full name matches any case-insensitive filter.
if (args.Length == 0) { Console.Error.WriteLine("need dll path"); return 1; }
string dll = args[0];
bool members = args.Contains("--members");
var filters = args.Skip(1).Where(a => a != "--members").Select(s => s.ToLowerInvariant()).ToArray();

using var fs = File.OpenRead(dll);
using var pe = new PEReader(fs);
var mr = pe.GetMetadataReader();

string FullName(TypeDefinition td)
{
    var ns = mr.GetString(td.Namespace);
    var name = mr.GetString(td.Name);
    return ns.Length > 0 ? ns + "." + name : name;
}

int count = 0;
foreach (var h in mr.TypeDefinitions)
{
    var td = mr.GetTypeDefinition(h);
    string full = FullName(td);
    string fl = full.ToLowerInvariant();
    if (filters.Length > 0 && !filters.Any(f => fl.Contains(f))) continue;

    // base type name (for context: PowerModel? CardModel? etc.)
    string base_ = "";
    if (td.BaseType.Kind == HandleKind.TypeReference)
    {
        var tr = mr.GetTypeReference((TypeReferenceHandle)td.BaseType);
        base_ = mr.GetString(tr.Name);
    }
    else if (td.BaseType.Kind == HandleKind.TypeDefinition)
    {
        base_ = mr.GetString(mr.GetTypeDefinition((TypeDefinitionHandle)td.BaseType).Name);
    }
    Console.WriteLine($"{full}{(base_.Length > 0 ? "  : " + base_ : "")}");
    count++;

    if (members)
    {
        foreach (var fh in td.GetFields())
        {
            var fd = mr.GetFieldDefinition(fh);
            Console.WriteLine("   ." + mr.GetString(fd.Name));
        }
        foreach (var mh in td.GetMethods())
        {
            var md = mr.GetMethodDefinition(mh);
            var attrs = md.Attributes;
            if ((attrs & MethodAttributes.Public) == 0 && (attrs & MethodAttributes.Family) == 0) continue;
            Console.WriteLine("   ()" + mr.GetString(md.Name));
        }
    }
}
Console.Error.WriteLine($"[{count} types matched]");
return 0;
