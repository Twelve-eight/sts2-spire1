using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "2 Thieves" weak encounter (<c>MonsterHelper.getEncounter("2 Thieves")</c>, bytecode
/// <c>new MonsterGroup(new Looter(-200f, 15f), new Mugger(80f, 0f))</c>).
/// <para>
/// NOTE: despite the name, this is NOT the Bandit trio (BanditPointy/BanditLeader/BanditBear
/// belong to the "Masked Bandits" event); vanilla's own bytecode pairs the Act-1
/// <see cref="Looter"/> with the Act-2 <see cref="Mugger"/>.
/// </para>
/// </summary>
public sealed class TwoThievesEncounter : Spire1Encounter
{
    public TwoThievesEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Looter>(),
        ModelDb.Monster<Mugger>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Looter>().ToMutable(), null),
        (ModelDb.Monster<Mugger>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "2 Thieves")];
}
