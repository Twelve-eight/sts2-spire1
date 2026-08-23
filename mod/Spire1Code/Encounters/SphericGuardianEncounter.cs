using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Spheric Guardian" weak encounter (<c>MonsterHelper.getEncounter("Spheric Guardian")</c>,
/// bytecode <c>new MonsterGroup(new SphericGuardian())</c> — single monster).
/// </summary>
public sealed class SphericGuardianEncounter : Spire1Encounter
{
    public SphericGuardianEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SphericGuardian>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<SphericGuardian>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Spheric Guardian")];
}
