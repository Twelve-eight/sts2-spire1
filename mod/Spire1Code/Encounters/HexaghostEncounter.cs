using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Hexaghost" boss (<c>MonsterHelper.getEncounter("Hexaghost")</c>, bytecode
/// <c>new MonsterGroup(new Hexaghost())</c> — single monster). StS1 pays a fixed 100 gold
/// (<c>AbstractRoom</c>: <c>100 + miscRng.random(-5, 5)</c>), so the range is pinned to
/// 95..105 instead of the shipped RoomType.Boss default of 100..100.
/// </summary>
public sealed class HexaghostEncounter : Spire1Encounter
{
    public HexaghostEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Hexaghost>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Hexaghost>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Hexaghost")];
}
