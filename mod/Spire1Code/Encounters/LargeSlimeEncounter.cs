using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Large Slime" strong encounter (<c>MonsterHelper.getEncounter("Large Slime")</c>).
/// Bytecode: one <c>miscRng.randomBoolean()</c> — true: single <c>AcidSlime_L</c>;
/// false: single <c>SpikeSlime_L</c>.
/// </summary>
public sealed class LargeSlimeEncounter : Spire1Encounter
{
    public LargeSlimeEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<AcidSlimeL>(),
        ModelDb.Monster<SpikeSlimeL>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (base.Rng.NextBool()
            ? ModelDb.Monster<AcidSlimeL>().ToMutable()
            : ModelDb.Monster<SpikeSlimeL>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Large Slime")];
}
