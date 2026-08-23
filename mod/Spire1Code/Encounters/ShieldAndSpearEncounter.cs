using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 The Ending "Shield and Spear" elite encounter
/// (<c>MonsterHelper.getEncounter("Shield and Spear")</c>, bytecode
/// <c>new MonsterGroup(new SpireShield(-250.0f, 30.0f), new SpireSpear(250.0f, 30.0f))</c>) —
/// the two flanking elites, with the player Surrounded between them
/// (see <see cref="SpireShield"/> / <see cref="SpireSpear"/>).
/// </summary>
public sealed class ShieldAndSpearEncounter : Spire1Encounter
{
    public ShieldAndSpearEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [4];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SpireShield>(),
        ModelDb.Monster<SpireSpear>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<SpireShield>().ToMutable(), null),
        (ModelDb.Monster<SpireSpear>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Shield and Spear")];
}