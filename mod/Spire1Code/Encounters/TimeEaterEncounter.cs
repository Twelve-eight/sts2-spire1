using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Time Eater" boss encounter — single <see cref="TimeEater"/>.
/// </summary>
public sealed class TimeEaterEncounter : Spire1Encounter
{
    public TimeEaterEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [3];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<TimeEater>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TimeEater>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Time Eater")];
}
