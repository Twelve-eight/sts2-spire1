using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Spire Growth" strong encounter — single <see cref="SpireGrowth"/>.
/// </summary>
public sealed class SpireGrowthStrongEncounter : Spire1Encounter
{
    public SpireGrowthStrongEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SpireGrowth>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<SpireGrowth>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Spire Growth")];
}
