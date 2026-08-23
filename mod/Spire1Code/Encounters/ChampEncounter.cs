using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Champ" boss encounter (<c>MonsterHelper.getEncounter("Champ")</c>, bytecode
/// <c>new MonsterGroup(new Champ())</c> — single monster).
/// </summary>
public sealed class ChampEncounter : Spire1Encounter
{
    public ChampEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [2];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Champ>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Champ>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "The Champ")];
}
