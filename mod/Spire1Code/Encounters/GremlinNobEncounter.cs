using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Gremlin Nob" elite encounter (<c>MonsterHelper.getEncounter("Gremlin Nob")</c>,
/// bytecode <c>new MonsterGroup(new GremlinNob(0f, 0f))</c> — single monster).
/// StS1 pays 25-35 gold (<c>AbstractRoom</c> elite branch:
/// <c>treasureRng.random(25, 35)</c>), so the range is pinned instead of the shipped
/// RoomType.Elite default of 35..45.
/// </summary>
public sealed class GremlinNobEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Elite;

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<GremlinNob>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<GremlinNob>().ToMutable(), null),
    ];

    public List<(string, string)>? Localization => [("name", "Gremlin Nob")];
}
