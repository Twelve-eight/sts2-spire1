using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Transient" strong encounter — single <see cref="Transient"/>.
/// </summary>
public sealed class TransientStrongEncounter : Spire1Encounter
{
    public TransientStrongEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Transient>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Transient>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Transient")];
}
