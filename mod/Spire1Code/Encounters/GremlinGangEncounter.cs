using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Gremlin Gang" strong encounter (<c>MonsterHelper.spawnGremlins()</c>).
/// Bytecode: a pool of 8 entries — 2x GremlinWarrior, 2x GremlinThief, 2x GremlinFat,
/// 1x GremlinTsundere (Sneaky), 1x GremlinWizard — and four successive draws of
/// <c>miscRng.random(pool.size - 1)</c> with removal, i.e. a shuffle-without-replacement
/// over that multiset. Per-type caps therefore follow from the multiset itself:
/// at most 2 Warrior/Thief/Fat, at most 1 Sneaky/Wizard. Reproduced by shuffling the
/// same multiset with <see cref="Rng.NextItem"/>.
/// </summary>
public sealed class GremlinGangEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<GremlinWarrior>(),
        ModelDb.Monster<GremlinThief>(),
        ModelDb.Monster<GremlinFat>(),
        ModelDb.Monster<GremlinShield>(),
        ModelDb.Monster<GremlinWizard>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // Bytecode multiset: 2 Warrior, 2 Thief (Sneaky), 2 Fat, 1 Shield (Tsundere),
        // 1 Wizard.
        List<MonsterModel> pool =
        [
            ModelDb.Monster<GremlinWarrior>(),
            ModelDb.Monster<GremlinWarrior>(),
            ModelDb.Monster<GremlinThief>(),
            ModelDb.Monster<GremlinThief>(),
            ModelDb.Monster<GremlinFat>(),
            ModelDb.Monster<GremlinFat>(),
            ModelDb.Monster<GremlinShield>(),
            ModelDb.Monster<GremlinWizard>(),
        ];

        List<(MonsterModel, string?)> monsters = new(4);
        for (int i = 0; i < 4 && pool.Count > 0; i++)
        {
            MonsterModel? picked = base.Rng.NextItem(pool);
            if (picked == null)
            {
                break;
            }

            pool.Remove(picked);
            monsters.Add((picked.ToMutable(), null));
        }

        return monsters;
    }

    public List<(string, string)>? Localization => [("name", "Gremlin Gang")];
}
