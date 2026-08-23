using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Three Shapes" weak encounter (<c>MonsterHelper.spawnShapes(true)</c>, bytecode
/// 2998-3132): 3 draws without replacement from the 6-entry multiset
/// [Repulsor, Repulsor, Exploder, Exploder, Spiker, Spiker], so the same shape can repeat.
/// </summary>
public sealed class ThreeShapesEncounter : Spire1Encounter
{
    public ThreeShapesEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

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

        List<(MonsterModel, string?)> monsters = new(3);
        while (monsters.Count < 3)
        {
            MonsterModel picked = pool[base.Rng.NextInt(pool.Count)];
            pool.Remove(picked);
            monsters.Add((picked.ToMutable(), null));
        }

        return monsters;
    }

    public override List<(string, string)>? Localization => [("title", "Three Shapes")];
}
