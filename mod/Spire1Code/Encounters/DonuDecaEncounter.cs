using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Donu and Deca" boss encounter — <see cref="Donu"/> and <see cref="Deca"/>.
/// </summary>
public sealed class DonuDecaEncounter : Spire1Encounter
{
    public DonuDecaEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [3];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Donu>(),
        ModelDb.Monster<Deca>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Donu>().ToMutable(), null),
        (ModelDb.Monster<Deca>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Donu and Deca")];
}
