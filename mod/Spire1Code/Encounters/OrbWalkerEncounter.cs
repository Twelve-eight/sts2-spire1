using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Orb Walker" weak encounter — single <see cref="OrbWalker"/>.
/// </summary>
public sealed class OrbWalkerEncounter : Spire1Encounter
{
    public OrbWalkerEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<OrbWalker>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<OrbWalker>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Orb Walker")];
}
