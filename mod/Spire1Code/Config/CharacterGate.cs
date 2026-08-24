using System.IO;
using System.Reflection;

namespace Spire1.Spire1Code.Config;

/// <summary>
/// 分装角色门控：读取与 dll 同目录的 character.txt（小写，trim）决定本包启用哪个职业。
/// 取值：ironclad | silent | defect | all（缺省/无法解析 = all，三职业全开）。
/// 分发包在 mods/Spire1/ 内预置对应标记；dll/pck 三包字节一致，联机校验不受影响。
/// </summary>
public static class CharacterGate
{
    private static readonly bool _ironclad;
    private static readonly bool _silent;
    private static readonly bool _defect;

    static CharacterGate()
    {
        string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string marker = Path.Combine(dir ?? ".", "character.txt");
        string value = "all";
        try
        {
            if (File.Exists(marker))
            {
                value = File.ReadAllText(marker).Trim().ToLowerInvariant();
            }
        }
        catch
        {
            value = "all";
        }
        switch (value)
        {
            case "ironclad":
                _ironclad = true;
                break;
            case "silent":
                _silent = true;
                break;
            case "defect":
                _defect = true;
                break;
            default:
                _ironclad = _silent = _defect = true;
                break;
        }
    }

    public static bool IroncladEnabled => _ironclad;
    public static bool SilentEnabled => _silent;
    public static bool DefectEnabled => _defect;
}
