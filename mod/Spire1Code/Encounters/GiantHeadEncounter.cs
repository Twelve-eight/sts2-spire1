using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Giant Head" elite encounter — single <see cref="GiantHead"/>.
/// </summary>
public sealed class GiantHeadEncounter : Spire1Encounter
{
    public GiantHeadEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [3];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<GiantHead>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<GiantHead>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Giant Head")];
}
