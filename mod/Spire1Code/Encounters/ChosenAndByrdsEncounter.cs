using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Chosen and Byrds" strong encounter (<c>MonsterHelper.getEncounter("Chosen and Byrds")</c>,
/// bytecode <c>new MonsterGroup(new Byrd(-170f, random(25,70)), new Chosen(80f, 0f))</c> —
/// one <see cref="Byrd"/> plus one <see cref="Chosen"/>).
/// </summary>
public sealed class ChosenAndByrdsEncounter : Spire1Encounter
{
    public ChosenAndByrdsEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Byrd>(),
        ModelDb.Monster<Chosen>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Byrd>().ToMutable(), null),
        (ModelDb.Monster<Chosen>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Chosen and Byrds")];
}
