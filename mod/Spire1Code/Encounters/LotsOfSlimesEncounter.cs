using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Lots of Slimes" strong encounter (<c>MonsterHelper.spawnManySmallSlimes()</c>).
/// Bytecode: a pool of 5 entries — <c>[SpikeSlime_S, SpikeSlime_S, SpikeSlime_S,
/// AcidSlime_S, AcidSlime_S]</c> — and five successive draws of
/// <c>miscRng.random(pool.size - 1)</c> with removal (i.e. a shuffle-without-replacement
/// over the multiset), so every fight is exactly 3 Spike Slimes S + 2 Acid Slimes S in a
/// random order. Reproduced by shuffling the same multiset with <see cref="Rng.NextItem"/>.
/// </summary>
public sealed class LotsOfSlimesEncounter : Spire1Encounter
{
    public LotsOfSlimesEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SpikeSlimeS>(),
        ModelDb.Monster<AcidSlimeS>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // Bytecode multiset: three SpikeSlime_S entries, two AcidSlime_S entries.
        List<MonsterModel> pool =
        [
            ModelDb.Monster<SpikeSlimeS>(),
            ModelDb.Monster<SpikeSlimeS>(),
            ModelDb.Monster<SpikeSlimeS>(),
            ModelDb.Monster<AcidSlimeS>(),
            ModelDb.Monster<AcidSlimeS>(),
        ];
        List<(MonsterModel, string?)> monsters = new(5);
        while (pool.Count > 0)
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

    public override List<(string, string)>? Localization => [("title", "Lots of Slimes")];
}
