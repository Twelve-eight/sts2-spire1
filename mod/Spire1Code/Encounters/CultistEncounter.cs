using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Cultist" weak encounter (<c>MonsterHelper.getEncounter("Cultist")</c>, bytecode
/// <c>new MonsterGroup(new Cultist(0f, -10f))</c> — single monster).
/// </summary>
public sealed class CultistEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Cultist>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Cultist>().ToMutable(), null),
    ];

    public List<(string, string)>? Localization => [("name", "Cultist")];
}
