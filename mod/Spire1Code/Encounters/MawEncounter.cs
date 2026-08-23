using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Maw" strong encounter — single <see cref="Maw"/>.
/// </summary>
public sealed class MawEncounter : Spire1Encounter
{
    public MawEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Maw>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Maw>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Maw")];
}
