using dnlib.DotNet;

// Usage: sigdump <assembly.dll> [filter ...]
// Prints public/protected method signatures and public fields for types whose
// full name contains any filter (case-insensitive). Uses dnlib.
if (args.Length == 0) { Console.Error.WriteLine("need dll path"); return 1; }
string dll = args[0];
var filters = args.Skip(1).Select(s => s.ToLowerInvariant()).ToArray();

var asm = ModuleDefMD.Load(dll);
int count = 0;
foreach (var type in asm.Types)
{
    string full = type.FullName;
    string fl = full.ToLowerInvariant();
    if (filters.Length > 0 && !filters.Any(f => fl.Contains(f))) continue;
    count++;
    Console.WriteLine($"== {full}  : {type.BaseType?.FullName ?? ""}");
    foreach (var m in type.Methods)
    {
        if (!m.IsPublic && !m.IsFamily) continue;
        string decl = "";
        if (m.IsStatic) decl += "static ";
        if (m.IsVirtual && !m.IsNewSlot) decl += "override ";
        else if (m.IsVirtual) decl += "virtual ";
        if (m.IsAbstract) decl += "abstract ";
        string gen = m.GenericParameters.Count > 0 ? "<" + string.Join(",", m.GenericParameters.Select(g => g.Name)) + ">" : "";
        var ps = m.Parameters.Where(p => p.IsNormalMethodParameter).Select(p => p.Type.FullName + " " + p.Name);
        Console.WriteLine($"  {decl}{m.ReturnType.FullName} {m.Name}{gen}({string.Join(", ", ps)})");
    }
    foreach (var f in type.Fields)
    {
        if (!f.IsPublic && !f.IsFamily) continue;
        string decl = f.IsStatic ? "static " : "";
        Console.WriteLine($"  .field {decl}{f.FieldType.FullName} {f.Name}");
    }
}
Console.Error.WriteLine($"[{count} types matched]");
return 0;
