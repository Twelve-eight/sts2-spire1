using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Collector" boss encounter (<c>MonsterHelper.getEncounter("Collector")</c>, bytecode
/// <c>new MonsterGroup(new TheCollector())</c> — the boss alone).
/// <para>
/// Vanilla's <see cref="TheCollector"/> spawns its minions itself on turn 1
/// (<c>TorchHead</c>s at <c>spawnX + i * -185</c>, plus later respawns) — the boss's own summon
/// move is the single source of truth. Pre-seeding adds here would duplicate them.
/// </para>
/// </summary>
public sealed class TheCollectorEncounter : Spire1Encounter
{
    public TheCollectorEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [2];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<TheCollector>(),
        ModelDb.Monster<TorchHead>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TheCollector>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "The Collector")];
}
