using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "3 Byrds" weak encounter (<c>MonsterHelper.getEncounter("3 Byrds")</c>).
/// Bytecode spawns three <c>Byrd</c> at x = -360/-80/200, each with a per-bird random
/// flight offset: <c>y = MathUtils.random(25, 70)</c>. The StS2 engine lays monsters out
/// itself (no shipped scene), so only the monster count is reproduced; the cosmetic y
/// jitter is dropped.
/// </summary>
public sealed class ThreeByrdsEncounter : Spire1Encounter
{
    public ThreeByrdsEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Byrd>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Byrd>().ToMutable(), null),
        (ModelDb.Monster<Byrd>().ToMutable(), null),
        (ModelDb.Monster<Byrd>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "3 Byrds")];
}
