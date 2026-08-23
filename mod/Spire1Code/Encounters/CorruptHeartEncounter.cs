using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 The Ending "Corrupt Heart" boss encounter — single <see cref="CorruptHeart"/>.
/// <para>
/// The vanilla fight is a solo boss (the Shield/Spear are a separate elite encounter); the
/// Heart's "invincible phase" powers (Invincible, Beat of Death) are not in the StS2 engine,
/// so the Heart is a plain act-4 boss in this port — see <see cref="CorruptHeart"/> remarks.
/// </para>
/// </summary>
public sealed class CorruptHeartEncounter : Spire1Encounter
{
    public CorruptHeartEncounter() : base(RoomType.Boss) { }

    public override RoomType RoomType => RoomType.Boss;

    public override IReadOnlyList<int> HomeActs => [4];

    public override int MinGoldReward => 95;
    public override int MaxGoldReward => 105;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<CorruptHeart>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<CorruptHeart>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Corrupt Heart")];
}