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

    /// <summary>Encounter display name. Required for every encounter.</summary>
    public abstract List<(string, string)>? Localization { get; }
}
