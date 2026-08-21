using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Small Slimes" weak encounter (<c>MonsterHelper.spawnSmallSlimes()</c>).
/// Bytecode: one <c>miscRng.randomBoolean()</c> picks between two fixed pairs —
/// true: <c>SpikeSlime_S + AcidSlime_M</c>; false: <c>AcidSlime_S + SpikeSlime_M</c>.
/// </summary>
public sealed class SmallSlimesEncounter : Spire1Encounter
{
    public SmallSlimesEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<AcidSlimeS>(),
        ModelDb.Monster<SpikeSlimeM>(),
        ModelDb.Monster<SpikeSlimeS>(),
        ModelDb.Monster<AcidSlimeM>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        base.Rng.NextBool()
            ? new List<(MonsterModel, string?)>
            {
                (ModelDb.Monster<SpikeSlimeS>().ToMutable(), null),
                (ModelDb.Monster<AcidSlimeM>().ToMutable(), null),
            }
            : new List<(MonsterModel, string?)>
            {
                (ModelDb.Monster<AcidSlimeS>().ToMutable(), null),
                (ModelDb.Monster<SpikeSlimeM>().ToMutable(), null),
            };

    public override List<(string, string)>? Localization => [("title", "Small Slimes")];
}
