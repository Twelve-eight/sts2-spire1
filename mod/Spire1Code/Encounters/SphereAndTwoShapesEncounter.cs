using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Sphere and Two Shapes" strong encounter (bytecode 3506-3552): a
/// <see cref="SphericGuardian"/> at (110, 10) and two independently rolled shapes at (-435, 10)
/// and (-210, 0) — each <c>getAncientShape</c> is its own miscRng.random(2), so the same shape
/// can appear twice.
/// </summary>
public sealed class SphereAndTwoShapesEncounter : Spire1Encounter
{
    public SphereAndTwoShapesEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SphericGuardian>(),
        ModelDb.Monster<Spiker>(),
        ModelDb.Monster<Repulsor>(),
        ModelDb.Monster<Exploder>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // getAncientShape: miscRng.random(2) → 0 Spiker, 1 Repulsor, 2 Exploder.
        MonsterModel RollShape() => base.Rng.NextInt(3) switch
        {
            0 => ModelDb.Monster<Spiker>(),
            1 => ModelDb.Monster<Repulsor>(),
            _ => ModelDb.Monster<Exploder>(),
        };

        return
        [
            (RollShape().ToMutable(), null),
            (RollShape().ToMutable(), null),
            (ModelDb.Monster<SphericGuardian>().ToMutable(), null),
        ];
    }

    public override List<(string, string)>? Localization => [("title", "Sphere and Two Shapes")];
}
