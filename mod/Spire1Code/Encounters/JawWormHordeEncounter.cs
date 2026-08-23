using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Jaw Worm Horde" strong encounter — three <see cref="JawWorm"/>s.
/// </summary>
public sealed class JawWormHordeEncounter : Spire1Encounter
{
    public JawWormHordeEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<JawWorm>(),
        ModelDb.Monster<JawWorm>(),
        ModelDb.Monster<JawWorm>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<JawWorm>().ToMutable(), null),
        (ModelDb.Monster<JawWorm>().ToMutable(), null),
        (ModelDb.Monster<JawWorm>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Jaw Worm Horde")];
}
