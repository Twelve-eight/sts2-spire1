using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Acts;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Base for every ported vanilla StS1 encounter.
/// <para>
/// Gated to StS1 acts only, so a vanilla StS2 run never draws StS1 monsters, and gated again
/// on <see cref="Spire1Config.DungeonEnabled"/> so the whole dungeon is runtime-toggleable
/// without uninstalling. BaseLib's own guidance for custom acts is to attach encounters this
/// way and leave the act's encounter list empty.
/// </para>
/// <para>
/// No encounter scene is shipped: <c>EncounterModel.HasScene</c> defaults to false and
/// <c>Slots</c> to empty, in which case the engine lays monsters out itself — so
/// <c>GenerateMonsters</c> returns null slot names.
/// </para>
/// </summary>
public abstract class Spire1Encounter : CustomEncounterModel, ILocalizationProvider
{
    protected Spire1Encounter(RoomType roomType) : base(roomType)
    {
    }

    public override bool IsValidForAct(ActModel act) => Spire1Config.DungeonEnabled && act is Spire1Act;

    /// <summary>
    /// Encounter display name. Required for every encounter.
    /// </summary>
    public abstract List<(string, string)>? Localization { get; }

    /// <summary>
    /// Run-history / top-bar boss icon. The engine's fallback path
    /// (<c>images/ui/run_history/&lt;id&gt;.png</c>) lives OUTSIDE our <c>Spire1/</c> pck prefix, so it can
    /// never resolve for a modded encounter: the preload marks it failed, and when the top bar later asks
    /// <c>AssetCache.GetTexture2D</c> for that same path it throws "Asset previously failed to load" — which
    /// aborts <c>NGlobalUi.Initialize</c>, skips <c>NMapScreen.Initialize</c>, and crashes run start with an
    /// NRE inside <c>NMapScreen.SetMap</c>. Pointing BaseLib at icons we DO ship (placeholder 1×1 PNGs under
    /// <c>res://Spire1/images/run_history/</c>) keeps every lookup on the happy path.
    /// </summary>
    public override string? CustomRunHistoryIconPath => CustomIconPath(Id.Entry);

    public override string? CustomRunHistoryIconOutlinePath => CustomIconPath(Id.Entry + "_outline");

    /// <c>Id.Entry</c> is uppercase ("SPIRE1-THE_GUARDIAN_ENCOUNTER"); Godot pack lookups are
    /// case-sensitive, so lowercase it to match the shipped file names ("spire1-the_guardian_encounter").
    private static string CustomIconPath(string id) => $"res://Spire1/images/run_history/{id.ToLowerInvariant()}.png";
}
