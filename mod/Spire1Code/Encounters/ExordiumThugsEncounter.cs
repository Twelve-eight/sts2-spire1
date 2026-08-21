using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Exordium Thugs" strong encounter (<c>MonsterHelper.bottomHumanoid()</c>).
/// Bytecode: slot 0 = <c>bottomGetWeakWildlife</c> (uniform over {getLouse, SpikeSlime_M,
/// AcidSlime_M}, where getLouse is itself a 50/50 LouseNormal/LouseDefensive roll) and
/// slot 1 = <c>bottomGetStrongHumanoid</c> (uniform over {Cultist, getSlaver, Looter},
/// where getSlaver is a 50/50 SlaverRed/SlaverBlue roll). Two independent draws.
/// </summary>
public sealed class ExordiumThugsEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<LouseNormal>(),
        ModelDb.Monster<LouseDefensive>(),
        ModelDb.Monster<SpikeSlimeM>(),
        ModelDb.Monster<AcidSlimeM>(),
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<SlaverRed>(),
        ModelDb.Monster<SlaverBlue>(),
        ModelDb.Monster<Looter>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // bottomGetWeakWildlife: uniform over {getLouse, SpikeSlime_M, AcidSlime_M}.
        MonsterModel weakSlot = base.Rng.NextInt(3) switch
        {
            0 => base.Rng.NextBool()
                ? ModelDb.Monster<LouseNormal>()
                : ModelDb.Monster<LouseDefensive>(),
            1 => ModelDb.Monster<SpikeSlimeM>(),
            _ => ModelDb.Monster<AcidSlimeM>(),
        };

        // bottomGetStrongHumanoid: uniform over {Cultist, getSlaver, Looter}.
        MonsterModel strongSlot = base.Rng.NextInt(3) switch
        {
            0 => ModelDb.Monster<Cultist>(),
            1 => base.Rng.NextBool()
                ? ModelDb.Monster<SlaverRed>()
                : ModelDb.Monster<SlaverBlue>(),
            _ => ModelDb.Monster<Looter>(),
        };

        return new List<(MonsterModel, string?)>
        {
            (weakSlot.ToMutable(), null),
            (strongSlot.ToMutable(), null),
        };
    }

    public List<(string, string)>? Localization => [("name", "Exordium Thugs")];
}
