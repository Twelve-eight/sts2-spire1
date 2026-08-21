using BaseLib.Config;

namespace Spire1.Spire1Code.Config;

/// <summary>
/// Runtime toggles for this mod, shown in Settings -> Mod Settings (auto-generated UI).
/// All default ON. Read at run / act / pool generation time to gate content, so a run can
/// use none of this mod's content while it stays installed (no uninstall needed).
/// </summary>
[ConfigHoverTipsByDefault]
internal class Spire1Config : SimpleModConfig
{
    /// <summary>Master switch. When false, all StS1 content is gated off.</summary>
    public static bool EnableSts1Content { get; set; } = true;

    /// <summary>Show the StS1 characters ("StS1 - X") in character select.</summary>
    public static bool EnableSts1Characters { get; set; } = true;

    /// <summary>Inject StS1 colorless cards into the shared reward pool.</summary>
    public static bool EnableSts1Cards { get; set; } = true;

    /// <summary>Inject StS1 relics into the shared reward pools.</summary>
    public static bool EnableSts1Relics { get; set; } = true;

    /// <summary>Offer the StS1 dungeon (4 acts) at character select and enable StS1 encounters.</summary>
    public static bool EnableSts1Dungeon { get; set; } = true;

    /// <summary>
    /// Play the StS1 dungeon instead of the StS2 act sequence on the next run started.
    /// <para>
    /// Deliberately separate from <see cref="EnableSts1Dungeon"/>, which only makes the content
    /// exist, and deliberately defaulted OFF: installing this mod must never silently change what
    /// a vanilla StS2 run looks like. This is the dungeon selector until the character-select UI
    /// lands; the substitution happens in <c>DungeonSelectionPatch</c>.
    /// </para>
    /// </summary>
    public static bool UseSts1Dungeon { get; set; } = false;

    /// <summary>
    /// Debug mode: append the localization key to every mod string shown in-game,
    /// e.g. "打击 (SPIRE1-STRIKE_SILENT.title)". Makes console spawning and testing easier.
    /// </summary>
    public static bool DebugShowLocKeys { get; set; } = false;

    // --- gate helpers (computed; getter-only, not surfaced as settings) ---
    [ConfigIgnore] public static bool CharactersEnabled => EnableSts1Content && EnableSts1Characters;
    [ConfigIgnore] public static bool CardsEnabled => EnableSts1Content && EnableSts1Cards;
    [ConfigIgnore] public static bool RelicsEnabled => EnableSts1Content && EnableSts1Relics;
    [ConfigIgnore] public static bool DungeonEnabled => EnableSts1Content && EnableSts1Dungeon;
    [ConfigIgnore] public static bool LocDebug => EnableSts1Content && DebugShowLocKeys;

    /// <summary>True when a newly started run should run the StS1 acts.</summary>
    [ConfigIgnore] public static bool Sts1DungeonSelected => DungeonEnabled && UseSts1Dungeon;
}
