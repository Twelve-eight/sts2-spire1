using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Gremlin Leader" elite encounter (<c>MonsterHelper.getEncounter("Gremlin Leader")</c>).
/// Bytecode (3555-3630): two independent <c>spawnGremlin(x, y)</c> rolls at GremlinLeader.POSX/POSY[0..1] —
/// (-366, -4) and (-170, 6) — each roll rebuilds the full 8-entry multiset
/// (2x GremlinWarrior, 2x GremlinThief, 2x GremlinFat, 1x GremlinTsundere, 1x GremlinWizard) and draws
/// once (WITH replacement, so the two minions may be identical), then the <see cref="GremlinLeader"/>
/// herself at (148, -15). Reproduced by rebuilding the multiset for each of the two minion rolls.
/// </summary>
public sealed class GremlinLeaderEncounter : Spire1Encounter
{
    public GremlinLeaderEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [2];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<GremlinWarrior>(),
        ModelDb.Monster<GremlinThief>(),
        ModelDb.Monster<GremlinFat>(),
        ModelDb.Monster<GremlinShield>(),
        ModelDb.Monster<GremlinWizard>(),
        ModelDb.Monster<GremlinLeader>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // Each spawnGremlin roll draws independently from a freshly built 8-entry multiset.
        List<(MonsterModel, string?)> monsters = new(3);
        for (int i = 0; i < 2; i++)
        {
            MonsterModel picked = base.Rng.NextItem<MonsterModel>(
            [
                ModelDb.Monster<GremlinWarrior>(),
                ModelDb.Monster<GremlinWarrior>(),
                ModelDb.Monster<GremlinThief>(),
                ModelDb.Monster<GremlinThief>(),
                ModelDb.Monster<GremlinFat>(),
                ModelDb.Monster<GremlinFat>(),
                ModelDb.Monster<GremlinShield>(),
                ModelDb.Monster<GremlinWizard>(),
            ])!;

            monsters.Add((picked.ToMutable(), null));
        }

        // GremlinLeader.POSX/POSY slot [2] — she always spawns last.
        monsters.Add((ModelDb.Monster<GremlinLeader>().ToMutable(), null));
        return monsters;
    }

    public override List<(string, string)>? Localization => [("title", "Gremlin Leader")];
}
