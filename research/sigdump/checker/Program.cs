using dnlib.DotNet;
using dnlib.DotNet.Emit;

// Usage: ildump <assembly.dll> <typeFilter> [methodFilter]
// Prints IL of public/protected methods (and ctor) of types whose FullName contains typeFilter.
var cargs = Environment.GetCommandLineArgs().Skip(1).ToArray();
if (cargs.Length < 2) { Console.Error.WriteLine("need dll and type filter"); return 1; }
string dll = cargs[0];
var typeFilter = cargs[1].ToLowerInvariant();
var methodFilter = cargs.Length > 2 ? cargs[2].ToLowerInvariant() : null;

var asm = ModuleDefMD.Load(dll);
foreach (var type in asm.GetTypes())
{
    string full = type.FullName;
    if (!full.ToLowerInvariant().Contains(typeFilter)) continue;
    Console.WriteLine($"== {full}  : {type.BaseType?.FullName ?? ""}");
    foreach (var m in type.Methods)
    {
        if (!m.IsPublic && !m.IsFamily && !m.IsConstructor)
        {
            if (!full.Contains("/<")) continue; // only dump private methods of async state machines
        }
        if (methodFilter != null && !m.Name.ToLowerInvariant().Contains(methodFilter)) continue;
        var ps = m.Parameters.Where(p => p.IsNormalMethodParameter).Select(p => p.Type.FullName + " " + p.Name);
        Console.WriteLine($"-- {m.ReturnType.FullName} {m.Name}({string.Join(", ", ps)})");
        if (m.HasBody)
        {
            foreach (var i in m.Body.Instructions)
                Console.WriteLine($"   {i.OpCode} {i.Operand}");
        }
    }
}
return 0;
