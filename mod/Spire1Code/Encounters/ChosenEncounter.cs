using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Chosen" weak encounter (<c>MonsterHelper.getEncounter("Chosen")</c>, bytecode
/// <c>new MonsterGroup(new Chosen())</c> — single monster).
/// </summary>
public sealed class ChosenEncounter : Spire1Encounter
{
    public ChosenEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Chosen>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Chosen>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Chosen")];
}
