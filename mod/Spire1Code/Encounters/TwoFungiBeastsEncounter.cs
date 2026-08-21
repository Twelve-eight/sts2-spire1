using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "2 Fungi Beasts" strong encounter (<c>MonsterHelper.getEncounter("2 Fungi Beasts")</c>,
/// bytecode <c>new MonsterGroup(new FungiBeast(-400f, 30f), new FungiBeast(-40f, 20f))</c> —
/// two fixed <c>FungiBeast</c>).
/// </summary>
public sealed class TwoFungiBeastsEncounter : Spire1Encounter
{
    public TwoFungiBeastsEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<FungiBeast>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<FungiBeast>().ToMutable(), null),
        (ModelDb.Monster<FungiBeast>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "2 Fungi Beasts")];
}
