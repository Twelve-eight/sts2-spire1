using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "2 Louse" weak encounter (<c>MonsterHelper.getEncounter("2 Louse")</c>).
/// Bytecode spawns two independent lice, each a 50/50
/// <c>LouseNormal</c>/<c>LouseDefensive</c> roll (<c>getLouse()</c>: one
/// <c>miscRng.randomBoolean()</c> per louse) — reproduced here with two independent
/// <see cref="Rng.NextBool"/> draws.
/// </summary>
public sealed class TwoLouseEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

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
    ];

    public List<(string, string)>? Localization => [("name", "2 Louse")];
}
