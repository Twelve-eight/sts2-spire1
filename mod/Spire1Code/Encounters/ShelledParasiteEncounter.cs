using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Shell Parasite" weak encounter (<c>MonsterHelper.getEncounter("Shell Parasite")</c>,
/// bytecode <c>new MonsterGroup(new ShelledParasite())</c> — single monster; the encounter key
/// says "Shell", the class is <c>ShelledParasite</c>).
/// </summary>
public sealed class ShelledParasiteEncounter : Spire1Encounter
{
    public ShelledParasiteEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<ShelledParasite>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<ShelledParasite>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Shell Parasite")];
}
