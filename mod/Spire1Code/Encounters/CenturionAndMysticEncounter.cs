using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Centurion and Healer" strong encounter (<c>MonsterHelper.getEncounter("Centurion and Healer")</c>,
/// bytecode <c>new MonsterGroup(new Centurion(-200f, 15f), new Healer(120f, 0f))</c> — one
/// <see cref="Centurion"/> plus one <see cref="Healer"/> (vanilla display name "Mystic").
/// </summary>
public sealed class CenturionAndMysticEncounter : Spire1Encounter
{
    public CenturionAndMysticEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Centurion>(),
        ModelDb.Monster<Healer>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Centurion>().ToMutable(), null),
        (ModelDb.Monster<Healer>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Centurion and Mystic")];
}
