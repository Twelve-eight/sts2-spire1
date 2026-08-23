using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Slavers" elite encounter (<c>MonsterHelper.getEncounter("Slavers")</c>, bytecode
/// <c>new MonsterGroup(new SlaverBlue(-385f, -15f), new Taskmaster(-133f, 0f),
/// new SlaverRed(125f, -30f))</c>) — the Act-1 slaver pair with an Act-2 <see cref="Taskmaster"/>
/// between them.
/// </summary>
public sealed class SlaversEncounter : Spire1Encounter
{
    public SlaversEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [2];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SlaverBlue>(),
        ModelDb.Monster<Taskmaster>(),
        ModelDb.Monster<SlaverRed>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<SlaverBlue>().ToMutable(), null),
        (ModelDb.Monster<Taskmaster>().ToMutable(), null),
        (ModelDb.Monster<SlaverRed>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Slavers")];
}
