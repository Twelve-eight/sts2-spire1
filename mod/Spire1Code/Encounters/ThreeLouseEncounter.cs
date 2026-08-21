using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "3 Louse" strong encounter (<c>MonsterHelper.getEncounter("3 Louse")</c>).
/// Bytecode spawns three independent lice, each a 50/50
/// <c>LouseNormal</c>/<c>LouseDefensive</c> roll (<c>getLouse()</c>: one
/// <c>miscRng.randomBoolean()</c> per louse) — reproduced here with three independent
/// <see cref="Rng.NextBool"/> draws.
/// </summary>
public sealed class ThreeLouseEncounter : Spire1Encounter
{
    public ThreeLouseEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<LouseNormal>(),
        ModelDb.Monster<LouseDefensive>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (base.Rng.NextBool()
            ? ModelDb.Monster<LouseNormal>().ToMutable()
            : ModelDb.Monster<LouseDefensive>().ToMutable(), null),
        (base.Rng.NextBool()
            ? ModelDb.Monster<LouseNormal>().ToMutable()
            : ModelDb.Monster<LouseDefensive>().ToMutable(), null),
        (base.Rng.NextBool()
            ? ModelDb.Monster<LouseNormal>().ToMutable()
            : ModelDb.Monster<LouseDefensive>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "3 Louse")];
}
