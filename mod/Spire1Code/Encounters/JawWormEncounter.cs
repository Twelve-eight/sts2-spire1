using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Jaw Worm" weak encounter (<c>MonsterHelper.getEncounter("Jaw Worm")</c>, bytecode
/// <c>new MonsterGroup(new JawWorm(0f, 25f))</c> — single monster).
/// </summary>
public sealed class JawWormEncounter : Spire1Encounter
{
    public override RoomType RoomType => RoomType.Monster;

    public override bool IsWeak => true;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<JawWorm>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<JawWorm>().ToMutable(), null),
    ];

    public List<(string, string)>? Localization => [("name", "Jaw Worm")];
}
