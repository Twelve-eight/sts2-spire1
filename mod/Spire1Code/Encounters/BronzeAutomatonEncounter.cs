using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Automaton" boss encounter (<c>MonsterHelper.getEncounter("Automaton")</c>, bytecode
/// <c>new MonsterGroup(new BronzeAutomaton())</c> — the boss ALONE).
/// <para>
/// The boss spawns its two <see cref="BronzeOrb"/>s itself on turn 1 (move <c>SPAWN_ORBS</c>:
/// <c>SpawnMonsterAction(new BronzeOrb(-300f, 200f, 0))</c> + <c>(200f, 130f, 1)</c>) — that move
/// is the single source of truth. Pre-seeding the orbs here too would duplicate them (4 total).
/// </para>
/// </summary>
public sealed class BronzeAutomatonEncounter : Spire1Encounter
{
    public BronzeAutomatonEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [2];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<BronzeAutomaton>(),
        ModelDb.Monster<BronzeOrb>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<BronzeAutomaton>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Bronze Automaton")];
}
