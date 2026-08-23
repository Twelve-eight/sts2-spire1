using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Four Shapes" strong encounter (<c>MonsterHelper.spawnShapes(false)</c>, bytecode
/// 2998-3132): 4 draws without replacement from the 6-entry multiset
/// [Repulsor, Repulsor, Exploder, Exploder, Spiker, Spiker], so the same shape can repeat.
/// </summary>
public sealed class FourShapesEncounter : Spire1Encounter
{
    public FourShapesEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [3];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Spiker>(),
        ModelDb.Monster<Repulsor>(),
        ModelDb.Monster<Exploder>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // spawnShapes: miscRng.random(0, size-1) per draw, then remove — no replacement.
        List<MonsterModel> pool =
        [
            ModelDb.Monster<Repulsor>(),
            ModelDb.Monster<Repulsor>(),
            ModelDb.Monster<Exploder>(),
            ModelDb.Monster<Exploder>(),
            ModelDb.Monster<Spiker>(),
            ModelDb.Monster<Spiker>(),
        ];

        List<(MonsterModel, string?)> monsters = new(4);
        while (monsters.Count < 4)
        {
            MonsterModel picked = pool[base.Rng.NextInt(pool.Count)];
            pool.Remove(picked);
            monsters.Add((picked.ToMutable(), null));
        }

        return monsters;
    }

    public override List<(string, string)>? Localization => [("title", "Four Shapes")];
}
