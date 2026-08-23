using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 Act-3 "Writhing Mass" elite encounter (<c>MonsterHelper.getEncounter("Writhing Mass")</c>,
/// bytecode 3614-3625: <c>new MonsterGroup(new WrithingMass())</c> — single monster; registered
/// STRONG in act 3 (<c>EnemyData("Writhing Mass", 3, MonsterType.STRONG)</c>)).
/// </summary>
public sealed class WrithingMassEncounter : Spire1Encounter
{
    public WrithingMassEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [3];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<WrithingMass>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<WrithingMass>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Writhing Mass")];
}
