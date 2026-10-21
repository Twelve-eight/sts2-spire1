// Shared StS2 mod source: console display names.
// Spec: docs/knowledge/CONSOLE-DISPLAY-FORMAT.md
//
// WHY SOURCE-SHARED, NOT A PROJECT REFERENCE
// A separate library DLL would make every mod carry a loader-level hard dependency on it:
// ModManager sets `mod.state = Failed` when a declared dependency is missing (AGENTS.md §2d),
// so a user without the shared DLL would lose every mod at once. Compiling this file into each
// mod adds no dependency at all. `Sts/sts2-spire1/tools/form-effects-probe/FormEffectsProbe.csproj`
// already uses the same pattern for the Forms sources.
//
// CONSUME (add to the mod's csproj, once):
//   <ItemGroup>
//     <Compile Include="$(MSBuildThisFileDirectory)../../_shared/ModConventions/ConsoleDisplay.cs"
//              Link="ModConventions/ConsoleDisplay.cs" />
//   </ItemGroup>
// Then `using Sts2.ModConventions;` at the call site.
//
// DO NOT copy this file into a mod. There is exactly one source.

using MegaCrit.Sts2.Core.Localization;

namespace Sts2.ModConventions;

/// <summary>
/// Renders an entity reference for a player-facing surface as <c>&lt;id&gt;（&lt;title&gt;）</c>,
/// using the active language's loc table. Falls back to the bare id when no
/// <c>&lt;id&gt;.title</c> entry exists — never an English placeholder (spec §2).
///
/// <para>
/// Use this only on paths a player reads: <c>CmdResult</c> messages, console output, UI text.
/// <b>Never</b> on machine paths — <c>Log.Info</c> into godot.log, serialization, network sync —
/// because the parentheses and localized text would break id-based grep (spec §5.4).
/// </para>
/// </summary>
public static class ModConsole
{
    /// <summary>Core renderer. <paramref name="table"/> is the loc table name (spec §4).</summary>
    public static string WithLoc(string table, string id)
    {
        var title = LocString.GetIfExists(table, id + ".title")?.GetFormattedText();
        return string.IsNullOrEmpty(title) ? id : $"{id}（{title}）";
    }

    // One thin wrapper per table so call sites are self-describing (spec §5.2).
    public static string CardLoc(string id) => WithLoc("cards", id);
    public static string RelicLoc(string id) => WithLoc("relics", id);
    public static string PotionLoc(string id) => WithLoc("potions", id);
    public static string PowerLoc(string id) => WithLoc("powers", id);
    public static string MonsterLoc(string id) => WithLoc("monsters", id);
    public static string ModifierLoc(string id) => WithLoc("modifiers", id);
    public static string EpochLoc(string id) => WithLoc("epochs", id);
    public static string EventLoc(string id) => WithLoc("events", id);
    public static string EncounterLoc(string id) => WithLoc("encounters", id);
    public static string AncientLoc(string id) => WithLoc("ancients", id);
    public static string AfflictionLoc(string id) => WithLoc("afflictions", id);
    public static string ActLoc(string id) => WithLoc("acts", id);
    public static string CharacterLoc(string id) => WithLoc("characters", id);
    public static string KeywordLoc(string id) => WithLoc("card_keywords", id);
    public static string AchievementLoc(string id) => WithLoc("achievements", id);
}
