using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Exordium Wildlife" strong encounter (<c>MonsterHelper.bottomWildlife()</c>).
/// Bytecode: exactly 2 monsters — slot 0 = <c>bottomGetStrongWildlife</c> (uniform
/// FungiBeast / JawWorm), slot 1 = <c>bottomGetWeakWildlife</c> (uniform over {getLouse,
/// SpikeSlime_M, AcidSlime_M}, getLouse a 50/50 LouseNormal/LouseDefensive roll).
/// (The 3-monster branch in the bytecode is dead code: it is guarded by
/// <c>if (size == 3)</c> while <c>size</c> is hard-set to 2 two instructions earlier.)
/// </summary>
public sealed class ExordiumWildlifeEncounter : Spire1Encounter
{
    public ExordiumWildlifeEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<FungiBeast>(),
        ModelDb.Monster<JawWorm>(),
        ModelDb.Monster<LouseNormal>(),
        ModelDb.Monster<LouseDefensive>(),
        ModelDb.Monster<SpikeSlimeM>(),
        ModelDb.Monster<AcidSlimeM>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // bottomGetStrongWildlife: uniform FungiBeast / JawWorm.
        MonsterModel strongSlot = base.Rng.NextBool()
            ? ModelDb.Monster<FungiBeast>()
            : ModelDb.Monster<JawWorm>();

        // bottomGetWeakWildlife: uniform over {getLouse, SpikeSlime_M, AcidSlime_M}.
        MonsterModel weakSlot = base.Rng.NextInt(3) switch
        {
            0 => base.Rng.NextBool()
                ? ModelDb.Monster<LouseNormal>()
                : ModelDb.Monster<LouseDefensive>(),
            1 => ModelDb.Monster<SpikeSlimeM>(),
            _ => ModelDb.Monster<AcidSlimeM>(),
        };

        return new List<(MonsterModel, string?)>
        {
            (strongSlot.ToMutable(), null),
            (weakSlot.ToMutable(), null),
        };
    }

    public override List<(string, string)>? Localization => [("title", "Exordium Wildlife")];
}
