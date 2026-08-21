using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Blue Slaver" strong encounter (<c>MonsterHelper.getEncounter("Blue Slaver")</c>,
/// bytecode <c>new MonsterGroup(new SlaverBlue(0f, 0f))</c> — single monster).
/// </summary>
public sealed class BlueSlaverEncounter : Spire1Encounter
{
    public BlueSlaverEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SlaverBlue>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<SlaverBlue>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Blue Slaver")];
}
