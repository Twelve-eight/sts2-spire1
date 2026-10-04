using System.IO;
using System.Reflection;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Config;

/// <summary>
/// 分装角色门控：读取与 dll 同目录的 character.txt（小写，trim）决定本包启用哪个职业。
/// 取值：ironclad | silent | defect | all（缺省/无法解析 = all，三职业全开）。
/// 分发包在 mods/Spire1/ 内预置对应标记；dll/pck 三包字节一致，联机校验不受影响。
/// <para>
/// This is a DISTRIBUTION gate (which flavor was packaged), not the runtime content
/// switch. The runtime switch is <see cref="Spire1Config.IsEnabled(Spire1ContentGroup)"/>;
/// both must be true for a character to appear. Keeping the two separate means a
/// character.txt flavor can never silently re-enable content the user disabled in settings.
/// </para>
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

    /// <summary>
    /// Effective gate: the packaged flavor AND the runtime content switches
    /// (<c>EnableSts1Content</c> + <c>EnableSts1Characters</c>). Consumers
    /// (character-select visibility, random-select eligibility, start-run checks)
    /// read this single property so a dead per-group switch cannot reappear.
    /// The per-run snapshot deliberately does not apply here: character visibility
    /// is decided before a run exists (see Spire1Config.RegisterContentNextRun doc).
    /// </summary>
    // r8d: 独立不可用状态优先, 不读 Spire1Config (静态构造失败时不得把异常传播到角色选择路径).
    public static bool IroncladEnabled =>
        !Spire1PowersGate.ContentUnavailableActive && _ironclad && Spire1Config.CharactersEnabled;

    /// <inheritdoc cref="IroncladEnabled"/>
    public static bool SilentEnabled =>
        !Spire1PowersGate.ContentUnavailableActive && _silent && Spire1Config.CharactersEnabled;

    /// <inheritdoc cref="IroncladEnabled"/>
    public static bool DefectEnabled =>
        !Spire1PowersGate.ContentUnavailableActive && _defect && Spire1Config.CharactersEnabled;
}
