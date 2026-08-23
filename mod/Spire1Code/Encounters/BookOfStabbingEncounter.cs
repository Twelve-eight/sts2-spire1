using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Book of Stabbing" elite encounter (<c>MonsterHelper.getEncounter("Book of Stabbing")</c>,
/// bytecode <c>new MonsterGroup(new BookOfStabbing())</c> — single monster).
/// </summary>
public sealed class BookOfStabbingEncounter : Spire1Encounter
{
    public BookOfStabbingEncounter() : base(RoomType.Elite) { }

    public override RoomType RoomType => RoomType.Elite;

    public override IReadOnlyList<int> HomeActs => [2];

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<BookOfStabbing>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<BookOfStabbing>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Book of Stabbing")];
}
