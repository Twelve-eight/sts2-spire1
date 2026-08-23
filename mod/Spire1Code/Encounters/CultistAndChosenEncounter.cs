using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Cultist and Chosen" strong encounter (<c>MonsterHelper.getEncounter("Cultist and Chosen")</c>,
/// bytecode <c>new MonsterGroup(new Cultist(-230f, 15f, false), new Chosen(100f, 25f))</c> — the
/// Act-1 <see cref="Cultist"/> paired with the Act-2 <see cref="Chosen"/>.
/// </summary>
public sealed class CultistAndChosenEncounter : Spire1Encounter
{
    public CultistAndChosenEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Cultist>(),
        ModelDb.Monster<Chosen>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Cultist>().ToMutable(), null),
        (ModelDb.Monster<Chosen>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Cultist and Chosen")];
}
