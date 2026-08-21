using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "The Guardian" boss (<c>MonsterHelper.getEncounter("The Guardian")</c>, bytecode
/// <c>new MonsterGroup(new TheGuardian())</c> — single monster). StS1 pays a fixed 100 gold
/// (<c>AbstractRoom</c>: <c>100 + miscRng.random(-5, 5)</c>), so the range is pinned to
/// 95..105 instead of the shipped RoomType.Boss default of 100..100.
/// </summary>
public sealed class TheGuardianEncounter : Spire1Encounter
{
    public TheGuardianEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<TheGuardian>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TheGuardian>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "The Guardian")];
}
