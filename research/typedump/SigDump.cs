using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

// Usage: sigdump <assembly.dll> [filter ...]
// Prints full type list and public/protected method signatures (with param types)
// for types whose full name contains any filter (case-insensitive).
if (args.Length == 0) { Console.Error.WriteLine("need dll path"); return 1; }
string dll = args[0];
var filters = args.Skip(1).Select(s => s.ToLowerInvariant()).ToArray();

using var fs = File.OpenRead(dll);
using var pe = new PEReader(fs);
var mr = pe.GetMetadataReader();

string TypeName(TypeReference tr)
{
    var name = mr.GetString(tr.Name);
    var ns = mr.GetString(tr.Namespace);
    var scope = tr.ResolutionScope.Kind == HandleKind.TypeReference
        ? TypeName(mr.GetTypeReference((TypeReferenceHandle)tr.ResolutionScope)) + "+"
        : (ns.Length > 0 ? ns + "." : "");
    return scope + name;
}

string TypeName(TypeDefinition td)
{
    var ns = mr.GetString(td.Namespace);
    var name = mr.GetString(td.Name);
    return ns.Length > 0 ? ns + "." + name : name;
}

string SigType(EntityHandle h, GenericContext ctx)
{
    switch (h.Kind)
    {
        case HandleKind.TypeDefinition:
            return TypeName(mr.GetTypeDefinition((TypeDefinitionHandle)h));
        case HandleKind.TypeReference:
            return TypeName(mr.GetTypeReference((TypeReferenceHandle)h));
        case HandleKind.TypeSpecification:
            return SigTypeSpec(mr.GetTypeSpecification((TypeSpecificationHandle)h), ctx);
        default:
            return h.Kind.ToString();
    }
}

string SigTypeSpec(TypeSpecification ts, GenericContext ctx)
{
    var sb = new StringBuilder();
    BlobReader br = mr.GetBlobReader(ts.Signature);
    byte b = br.ReadByte();
    if ((b & 0x0F) == 0x0F) // generic inst
    {
        bool isValueType = (b & 0x20) != 0;
        var gtc = br.ReadCompressedInteger(); // generic type def/ref coded index
        EntityHandle gth = gtc < 0 ? default : MetadataTokens.EntityHandle(gtc);
        string baseName = "";
        if (gth.Kind == HandleKind.TypeDefinition) baseName = TypeName(mr.GetTypeDefinition((TypeDefinitionHandle)gth));
        else if (gth.Kind == HandleKind.TypeReference) baseName = TypeName(mr.GetTypeReference((TypeReferenceHandle)gth));
        sb.Append(baseName).Append('<');
        int n = br.ReadCompressedInteger();
        for (int i = 0; i < n; i++)
        {
            if (i > 0) sb.Append(',');
            var arg = ReadType(ref br, ctx);
            sb.Append(arg);
        }
        sb.Append('>');
        return sb.ToString();
    }
    return "?";
}

string ReadType(ref BlobReader br, GenericContext ctx)
{
    byte b = br.ReadByte();
    switch (b & 0x1F) // ELEMENT_TYPE
    {
        case 0x02: return "bool";
        case 0x03: return "char";
        case 0x04: return "sbyte";
        case 0x05: return "byte";
        case 0x06: return "short";
        case 0x07: return "ushort";
        case 0x08: return "int";
        case 0x09: return "uint";
        case 0x0A: return "long";
        case 0x0B: return "ulong";
        case 0x0C: return "float";
        case 0x0D: return "double";
        case 0x0E: return "string";
        case 0x0F: return "IntPtr";
        case 0x11: return "UIntPtr";
        case 0x12: return "object";
        case 0x13: return "void";
        case 0x14: return "typedref";
        case 0x15: return "!!" + br.ReadCompressedInteger(); // generic method param
        case 0x16: return "!" + br.ReadCompressedInteger();  // generic type param
        case 0x1B: return ReadType(ref br, ctx) + "&";       // byref
        case 0x1C: return ReadType(ref br, ctx) + "*";       // pointer
        case 0x1D: { int n = br.ReadCompressedInteger(); return ReadType(ref br, ctx) + "[" + new string(',', n - 1) + "]"; }
        case 0x1E: { var elem = ReadType(ref br, ctx); int n = br.ReadCompressedInteger(); return elem + "[" + (n == 0 ? "" : n.ToString()) + "]"; }
        case 0x1F: return ReadType(ref br, ctx) + "[]";      // szarray
        case 0x20: return ReadType(ref br, ctx) + "&";       // pinned
        case 0x08 + 0x40: return "int modreq"; // hmm
        case 0x0F + 0x40: return "IntPtr modreq";
        case 0x12 + 0x40: return "object modreq";
        case 0x15: return "??";
        case 0x1B + 0x40: return "byref modreq";
        case 0x1F + 0x40: return "array modreq";
        case 0x1C + 0x40: return "ptr modreq";
        default:
            if (b == 0x1B || b == 0x1C) return ReadType(ref br, ctx);
            // type def/ref/spec encoded
            {
                int coded = b;
                EntityHandle h;
                if ((coded & 0x03) == 0) h = MetadataTokens.TypeDefinitionHandle(coded >> 2);
                else if ((coded & 0x03) == 1) h = MetadataTokens.TypeReferenceHandle(coded >> 2);
                else h = MetadataTokens.TypeSpecificationHandle(coded >> 2);
                return SigType(h, ctx);
            }
    }
}

int count = 0;
foreach (var h in mr.TypeDefinitions)
{
    var td = mr.GetTypeDefinition(h);
    string full = TypeName(td);
    string fl = full.ToLowerInvariant();
    if (filters.Length > 0 && !filters.Any(f => fl.Contains(f))) continue;
    count++;
    Console.WriteLine($"== {full}");
    foreach (var mh in td.GetMethods())
    {
        var md = mr.GetMethodDefinition(mh);
        if ((md.Attributes & MethodAttributes.Public) == 0 && (md.Attributes & MethodAttributes.Family) == 0) continue;
        var sb = new StringBuilder();
        sb.Append("  ");
        var sig = md.DecodeSignature(new SigProvider(), 0);
        var ret = sig.ReturnType;
        sb.Append(SigType(ret, default)).Append(' ').Append(mr.GetString(md.Name)).Append('(');
        for (int i = 0; i < sig.ParameterTypes.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(SigType(sig.ParameterTypes[i], default));
        }
        sb.Append(')');
        Console.WriteLine(sb);
    }
    foreach (var fh in td.GetFields())
    {
        var fd = mr.GetFieldDefinition(fh);
        if ((fd.Attributes & FieldAttributes.Public) == 0 && (fd.Attributes & FieldAttributes.Family) == 0) continue;
        var sb = new StringBuilder();
        sb.Append("  .field ");
        var sig = fd.DecodeSignature(new SigProvider(), 0);
        sb.Append(SigType(sig, default)).Append(' ').Append(mr.GetString(fd.Name));
        Console.WriteLine(sb);
    }
}
Console.Error.WriteLine($"[{count} types matched]");

class SigProvider : ISignatureTypeProvider<string, GenericContext>
{
    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[]";
    public string GetByReferenceType(string elementType) => elementType + "&";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr";
    public string GetGenericInstantiation(string genericType, System.Collections.Immutable.ImmutableArray<string> typeArguments) => genericType + "<" + string.Join(",", typeArguments) + ">";
    public string GetGenericMethodParameter(GenericContext genericContext, int index) => "!!" + index;
    public string GetGenericTypeParameter(GenericContext genericContext, int index) => "!" + index;
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetPinnedType(string elementType) => elementType;
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => reader.GetString(handle) switch { var n => (reader.GetString(reader.GetTypeDefinition(handle).Namespace).Length > 0 ? reader.GetString(reader.GetTypeDefinition(handle).Namespace) + "." : "") + n };
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => reader.GetString(handle) switch { var n => (reader.GetString(reader.GetTypeReference(handle).Namespace).Length > 0 ? reader.GetString(reader.GetTypeReference(handle).Namespace) + "." : "") + n };
    public string GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => "spec";
}

readonly record struct GenericContext();
