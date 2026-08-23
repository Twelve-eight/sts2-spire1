using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Reptomancer" elite encounter — <see cref="Reptomancer"/> with two
/// <see cref="SnakeDagger"/>s.
/// </summary>
public sealed class ReptomancerEncounter : Spire1Encounter
{
    public ReptomancerEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [3];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Reptomancer>(),
        ModelDb.Monster<SnakeDagger>(),
        ModelDb.Monster<SnakeDagger>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Reptomancer>().ToMutable(), null),
        (ModelDb.Monster<SnakeDagger>().ToMutable(), null),
        (ModelDb.Monster<SnakeDagger>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Reptomancer")];
}
