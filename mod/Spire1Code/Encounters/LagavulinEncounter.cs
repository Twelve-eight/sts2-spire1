using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Lagavulin" elite encounter (<c>MonsterHelper.getEncounter("Lagavulin")</c>,
/// bytecode <c>new MonsterGroup(new Lagavulin(true))</c> — single monster, asleep=true).
/// StS1 pays 25-35 gold (<c>AbstractRoom</c> elite branch:
/// <c>treasureRng.random(25, 35)</c>), so the range is pinned instead of the shipped
/// RoomType.Elite default of 35..45.
/// </summary>
public sealed class LagavulinEncounter : Spire1Encounter
{
    public LagavulinEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Lagavulin>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Lagavulin>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Lagavulin")];
}
