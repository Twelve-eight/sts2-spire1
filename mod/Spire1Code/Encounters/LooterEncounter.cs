using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Looter" strong encounter (<c>MonsterHelper.getEncounter("Looter")</c>, bytecode
/// <c>new MonsterGroup(new Looter(0f, 0f))</c> — single monster).
/// </summary>
public sealed class LooterEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Looter>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Looter>().ToMutable(), null),
    ];

    public List<(string, string)>? Localization => [("name", "Looter")];
}
