using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Snecko" strong encounter (<c>MonsterHelper.getEncounter("Snecko")</c>, bytecode
/// <c>new MonsterGroup(new Snecko())</c> — single monster).
/// </summary>
public sealed class SneckoEncounter : Spire1Encounter
{
    public SneckoEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Snecko>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Snecko>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Snecko")];
}
