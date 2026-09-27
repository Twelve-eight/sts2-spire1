// PeMetadataReader.cs
// 手写 ECMA-335 CLI 元数据读取器 -- 只解析门禁需要的两张表:
//   AssemblyRef (table 0x23) -> 引用的程序集名
//   TypeDef     (table 0x02) -> 定义的类型 (命名空间 + 名字)
//
// 为什么手写而不是用 System.Reflection.Metadata / Assembly.Load / ilspycmd:
//  1) 结构性、可证伪: 直接读 PE -> CLI header -> #~ 元数据流 -> 表堆里的真实行,
//     而不是对 DLL 做 "字符串 contains 类型名"。#if / #else 两分支都定义同名类型时,
//     字符串扫描不可证伪(两分支都命中) -- 这正是历史上假通过的根因。读 AssemblyRef 表
//     则是 "编译器实际写进引用表的程序集" 的硬证据: 硬引用一个 mod = 表里必有它的行,
//     纯反射调用 = 表里没有它的行。无法被 #if 两分支同名类型欺骗。
//  2) 零外部依赖: 只用 BCL,离线可构建,不需要 restore、不需要 ilspycmd。
//  3) 不 Load 程序集: 加载 Spire1.dll 会拖入 sts2/Godot/BaseLib 依赖解析,在 CI 上不可行;
//     纯字节解析没有这个问题。
//
// 参考: ECMA-335 6th ed, II.24 (Metadata physical layout), II.22 (table schemas)。

using System.Text;

namespace Spire1.BuildGates;

/// <summary>一个类型定义的 命名空间.名字。</summary>
public readonly record struct TypeDefName(string Namespace, string Name)
{
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;
}

/// <summary>手写 PE + CLI 元数据解析结果: AssemblyRef 名列表 + TypeDef 全名列表。</summary>
public sealed class PeMetadata
{
    public required IReadOnlyList<string> AssemblyRefNames { get; init; }
    public required IReadOnlyList<TypeDefName> TypeDefs { get; init; }
}

public static class PeMetadataReader
{
    public static PeMetadata Read(string dllPath)
    {
        byte[] buf = File.ReadAllBytes(dllPath);
        return Parse(buf);
    }

    private static PeMetadata Parse(byte[] b)
    {
        // ---- PE 头 ----
        int peOff = ReadU32(b, 0x3C);
        if (ReadU32(b, peOff) != 0x00004550) throw new BadImageFormatException("PE signature 'PE\\0\\0' not found");
        int coffOff = peOff + 4;
        int numSections = ReadU16(b, coffOff + 2);
        int optSize = ReadU16(b, coffOff + 16);
        int optOff = coffOff + 20;
        int magic = ReadU16(b, optOff);
        bool pe32Plus = magic == 0x20B;

        // 数据目录[14] = CLI header。PE32: 目录起点 optOff+96; PE32+: optOff+112。
        int ddOff = optOff + (pe32Plus ? 112 : 96);
        int clrRva = ReadU32(b, ddOff + 14 * 8);
        if (clrRva == 0) throw new BadImageFormatException("no CLI header (not a managed assembly)");

        // ---- 节表, 用于 RVA->文件偏移 ----
        int secOff = optOff + optSize;
        var sections = new (uint Va, uint VSize, uint Raw, uint RawSize)[numSections];
        for (int i = 0; i < numSections; i++)
        {
            int s = secOff + i * 40;
            sections[i] = (
                Va: (uint)ReadU32(b, s + 12),
                VSize: (uint)ReadU32(b, s + 8),
                Raw: (uint)ReadU32(b, s + 20),
                RawSize: (uint)ReadU32(b, s + 16));
        }
        int RvaToOff(uint rva)
        {
            foreach (var s in sections)
            {
                uint span = Math.Max(s.VSize, s.RawSize);
                if (rva >= s.Va && rva < s.Va + span) return (int)(s.Raw + (rva - s.Va));
            }
            throw new BadImageFormatException($"RVA 0x{rva:X} not mapped to any section");
        }

        // ---- CLI header -> 元数据根 ----
        int clrOff = RvaToOff((uint)clrRva);
        uint metaRva = (uint)ReadU32(b, clrOff + 8);
        int metaOff = RvaToOff(metaRva);
        if (ReadU32(b, metaOff) != 0x424A5342) throw new BadImageFormatException("metadata 'BSJB' signature not found");

        int verLen = ReadU32(b, metaOff + 12);
        int p = metaOff + 16 + verLen + 2; // 跳过 version 字符串 + flags(2)
        int numStreams = ReadU16(b, p); p += 2;

        int tildeOff = -1, stringsHeapOff = -1;
        for (int i = 0; i < numStreams; i++)
        {
            int off = ReadU32(b, p); p += 4;
            int size = ReadU32(b, p); p += 4;
            var sb = new StringBuilder();
            while (b[p] != 0) { sb.Append((char)b[p]); p++; }
            p++; // null
            while ((p - metaOff) % 4 != 0) p++; // 4 字节对齐
            string name = sb.ToString();
            if (name == "#~" || name == "#-") tildeOff = metaOff + off;
            else if (name == "#Strings") stringsHeapOff = metaOff + off;
        }
        if (tildeOff < 0) throw new BadImageFormatException("no #~ / #- metadata table stream");
        if (stringsHeapOff < 0) throw new BadImageFormatException("no #Strings heap");

        // ---- 表流头 ----
        byte heapSizes = b[tildeOff + 6];
        bool sWide = (heapSizes & 0x01) != 0; // #Strings 索引宽度
        bool gWide = (heapSizes & 0x02) != 0; // #GUID
        bool bWide = (heapSizes & 0x04) != 0; // #Blob
        ulong valid = ReadU64(b, tildeOff + 8);

        var rows = new int[64];
        int rp = tildeOff + 24;
        for (int i = 0; i < 64; i++)
            if (((valid >> i) & 1) != 0) { rows[i] = ReadU32(b, rp); rp += 4; }
        int rowsStart = rp;

        int R(int id) => rows[id];
        int strS = sWide ? 4 : 2;
        int guidS = gWide ? 4 : 2;
        int blobS = bWide ? 4 : 2;
        int SimpleIdx(int id) => R(id) < 65536 ? 2 : 4;

        // 编码索引宽度: 取参与表里最大行数, 与 (2^(16-tagbits)) 比较。
        int Coded(params int[] tables)
        {
            int nonNeg = 0; foreach (var t in tables) if (t >= 0) nonNeg++;
            int bits = (int)Math.Ceiling(Math.Log2(nonNeg));
            int max = 0; foreach (var t in tables) if (t >= 0) max = Math.Max(max, R(t));
            return max < (1 << (16 - bits)) ? 2 : 4;
        }
        int TypeDefOrRef() => Coded(0x02, 0x01, 0x1B);
        int HasConstant() => Coded(0x04, 0x08, 0x17);
        int HasCustomAttribute() => Coded(0x06, 0x04, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x00, 0x0E, 0x17, 0x14, 0x11, 0x1A, 0x1B, 0x20, 0x23, 0x26, 0x27, 0x28, 0x2C, 0x2D);
        int HasFieldMarshal() => Coded(0x04, 0x08);
        int HasDeclSecurity() => Coded(0x02, 0x06, 0x20);
        int MemberRefParent() => Coded(0x02, 0x01, 0x1A, 0x06, 0x1B);
        int HasSemantics() => Coded(0x14, 0x17);
        int MethodDefOrRef() => Coded(0x06, 0x0A);
        int MemberForwarded() => Coded(0x04, 0x06);
        int Implementation() => Coded(0x26, 0x23, 0x27);
        int CustomAttributeType() => Coded(-1, -1, 0x06, 0x0A, -1);
        int ResolutionScope() => Coded(0x00, 0x1A, 0x23, 0x02);
        int TypeOrMethodDef() => Coded(0x02, 0x06);

        // 各表行字节大小 (ECMA-335 II.22)。只需覆盖 <= 0x23 且实际出现的表。
        var sz = new int[64];
        sz[0x00] = 2 + strS + guidS + guidS + guidS;                       // Module
        sz[0x01] = ResolutionScope() + strS + strS;                        // TypeRef
        sz[0x02] = 4 + strS + strS + TypeDefOrRef() + SimpleIdx(0x04) + SimpleIdx(0x06); // TypeDef
        sz[0x04] = 2 + strS + blobS;                                       // Field
        sz[0x06] = 4 + 2 + 2 + strS + blobS + SimpleIdx(0x08);             // MethodDef
        sz[0x08] = 2 + 2 + strS;                                           // Param
        sz[0x09] = SimpleIdx(0x02) + TypeDefOrRef();                       // InterfaceImpl
        sz[0x0A] = MemberRefParent() + strS + blobS;                       // MemberRef
        sz[0x0B] = 1 + 1 + HasConstant() + blobS;                          // Constant
        sz[0x0C] = HasCustomAttribute() + CustomAttributeType() + blobS;   // CustomAttribute
        sz[0x0D] = HasFieldMarshal() + blobS;                              // FieldMarshal
        sz[0x0E] = 2 + HasDeclSecurity() + blobS;                          // DeclSecurity
        sz[0x0F] = 2 + 4 + SimpleIdx(0x02);                                // ClassLayout
        sz[0x10] = 4 + SimpleIdx(0x04);                                    // FieldLayout
        sz[0x11] = blobS;                                                  // StandAloneSig
        sz[0x12] = SimpleIdx(0x02) + SimpleIdx(0x06);                      // EventMap
        sz[0x14] = 2 + strS + TypeDefOrRef();                              // Event
        sz[0x15] = SimpleIdx(0x02) + SimpleIdx(0x17);                      // PropertyMap
        sz[0x17] = 2 + strS + blobS;                                       // Property
        sz[0x18] = 2 + SimpleIdx(0x06) + HasSemantics();                   // MethodSemantics
        sz[0x19] = SimpleIdx(0x02) + MethodDefOrRef() + MethodDefOrRef();  // MethodImpl
        sz[0x1A] = strS;                                                   // ModuleRef
        sz[0x1B] = blobS;                                                  // TypeSpec
        sz[0x1C] = 2 + MemberForwarded() + strS + SimpleIdx(0x1A);         // ImplMap
        sz[0x1D] = 4 + SimpleIdx(0x04);                                    // FieldRVA
        sz[0x20] = 4 + 2 + 2 + 2 + 2 + 4 + blobS + strS + strS;            // Assembly
        sz[0x23] = 2 + 2 + 2 + 2 + 4 + blobS + strS + strS + blobS;        // AssemblyRef
        sz[0x26] = 4 + strS + blobS;                                       // File
        sz[0x27] = 4 + 4 + strS + strS + Implementation();                 // ExportedType
        sz[0x28] = 4 + 4 + strS + Implementation();                        // ManifestResource
        sz[0x29] = SimpleIdx(0x02) + SimpleIdx(0x02);                      // NestedClass
        sz[0x2A] = 2 + 2 + strS + TypeOrMethodDef();                       // GenericParam
        sz[0x2B] = MethodDefOrRef() + blobS;                               // MethodSpec
        sz[0x2C] = SimpleIdx(0x2A) + TypeDefOrRef();                       // GenericParamConstraint

        int TableOffset(int target)
        {
            int off = rowsStart;
            for (int id = 0; id < target; id++)
            {
                if (rows[id] != 0)
                {
                    if (sz[id] == 0) throw new BadImageFormatException($"unhandled table 0x{id:X2} precedes target 0x{target:X2}");
                    off += rows[id] * sz[id];
                }
            }
            return off;
        }

        string ReadStr(int idx)
        {
            var sb = new StringBuilder();
            int o = stringsHeapOff + idx;
            while (b[o] != 0) { sb.Append((char)b[o]); o++; }
            return sb.ToString();
        }
        int ReadSIdx(int off) => sWide ? ReadU32(b, off) : ReadU16(b, off);

        // ---- AssemblyRef (0x23): name 在 major/minor/build/rev(8)+flags(4)+pubkey(blob) 之后 ----
        var arNames = new List<string>();
        if (rows[0x23] != 0)
        {
            int baseOff = TableOffset(0x23);
            int nameFieldOff = 2 + 2 + 2 + 2 + 4 + blobS;
            for (int i = 0; i < rows[0x23]; i++)
            {
                int row = baseOff + i * sz[0x23];
                arNames.Add(ReadStr(ReadSIdx(row + nameFieldOff)));
            }
        }

        // ---- TypeDef (0x02): flags(4) + name(str) + namespace(str) ----
        var typeDefs = new List<TypeDefName>();
        if (rows[0x02] != 0)
        {
            int baseOff = TableOffset(0x02);
            for (int i = 0; i < rows[0x02]; i++)
            {
                int row = baseOff + i * sz[0x02];
                string name = ReadStr(ReadSIdx(row + 4));
                string ns = ReadStr(ReadSIdx(row + 4 + strS));
                typeDefs.Add(new TypeDefName(ns, name));
            }
        }

        return new PeMetadata { AssemblyRefNames = arNames, TypeDefs = typeDefs };
    }

    private static int ReadU16(byte[] b, int o) => b[o] | (b[o + 1] << 8);
    private static int ReadU32(byte[] b, int o) => b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24);
    private static ulong ReadU64(byte[] b, int o) => (uint)ReadU32(b, o) | ((ulong)(uint)ReadU32(b, o + 4) << 32);
}
