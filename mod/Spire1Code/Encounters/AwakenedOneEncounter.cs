using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Awakened One" boss encounter (<c>MonsterHelper.getEncounter("Awakened One")</c>,
/// bytecode 3703-3760): two <see cref="Cultist"/> minions at (-590, 10) and (-298, -10), then the
/// <see cref="AwakenedOne"/> at (100, 15). Engine lays monsters out itself (null slots), so only order matters.
/// </summary>
public sealed class AwakenedOneEncounter : Spire1Encounter
{
    public AwakenedOneEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [3];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<AwakenedOne>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Cultist>().ToMutable(), null),
        (ModelDb.Monster<Cultist>().ToMutable(), null),
        (ModelDb.Monster<AwakenedOne>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Awakened One")];
}
