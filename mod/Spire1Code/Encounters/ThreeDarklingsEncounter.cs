using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Three Darklings" weak encounter — three <see cref="Darkling"/>s.
/// </summary>
public sealed class ThreeDarklingsEncounter : Spire1Encounter
{
    public ThreeDarklingsEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Darkling>(),
        ModelDb.Monster<Darkling>(),
        ModelDb.Monster<Darkling>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Darkling>().ToMutable(), null),
        (ModelDb.Monster<Darkling>().ToMutable(), null),
        (ModelDb.Monster<Darkling>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Three Darklings")];
}
