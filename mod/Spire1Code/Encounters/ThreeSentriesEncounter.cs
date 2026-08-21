using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "3 Sentries" elite encounter (<c>MonsterHelper.getEncounter("3 Sentries")</c>,
/// bytecode <c>new MonsterGroup(new Sentry(-330f, 25f), new Sentry(-85f, 10f),
/// new Sentry(140f, 30f))</c> — three fixed <c>Sentry</c>).
/// StS1 pays 25-35 gold (<c>AbstractRoom</c> elite branch:
/// <c>treasureRng.random(25, 35)</c>), so the range is pinned instead of the shipped
/// RoomType.Elite default of 35..45.
/// </summary>
public sealed class ThreeSentriesEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Elite;

    public override int MinGoldReward => 25;
    public override int MaxGoldReward => 35;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Sentry>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Sentry>().ToMutable(), null),
        (ModelDb.Monster<Sentry>().ToMutable(), null),
        (ModelDb.Monster<Sentry>().ToMutable(), null),
    ];

    public List<(string, string)>? Localization => [("name", "3 Sentries")];
}
