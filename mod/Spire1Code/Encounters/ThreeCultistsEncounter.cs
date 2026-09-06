using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// StS1 "3 Cultists" strong encounter (<c>MonsterHelper.getEncounter("3 Cultists")</c>).
/// Bytecode (<c>.tmp/r9-tmp/MonsterHelper.txt</c>, getEncounter case 7):
/// <c>new MonsterGroup(new Cultist(-465f, -20f, false), new Cultist(-130f, 15f, false),
/// new Cultist(200f, -5f))</c> - three Act-1 <see cref="Cultist"/>s. The first two spawn
/// with <c>talky=false</c> (the (FFZ) ctor); the third uses the (FF) ctor whose talky
/// defaults true, but the pre-fight babble is cosmetic and StS2 has no per-monster talk
/// channel here, so it is dropped.
/// <para>
/// Official run-history display name (<c>localization/eng/ui.json</c>
/// <c>RunHistoryMonsterNames.TEXT[9]</c>): "Triple Cultists"; zhs
/// <c>localization/zhs/ui.json</c> index 9: "三邪教徒".
/// </para>
/// <para>
/// Gold: StS1 normal monster rooms pay <c>treasureRng.random(10, 20)</c>
/// (<c>AbstractRoom.applyEvents</c> bytecode :1004-1015), matching the shipped StS2
/// <c>RoomType.Monster</c> default of 10..20 - no override needed.
/// </para>
/// </summary>
public sealed class ThreeCultistsEncounter : Spire1Encounter
{
    public ThreeCultistsEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Cultist>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Cultist>().ToMutable(), null),
        (ModelDb.Monster<Cultist>().ToMutable(), null),
        (ModelDb.Monster<Cultist>().ToMutable(), null),
    ];

    // zh title is the official StS1 zhs run-history name (localization/zhs/ui.json,
    // RunHistoryMonsterNames.TEXT[9]); loss line reuses the shipped StS2 zhs Cultists
    // death message (encounters-zhs.json CULTISTS_NORMAL.loss).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new EncounterLoc(Tr("Triple Cultists", "三邪教徒"),
            Tr("{character} was slain by some [gold]{encounter}[/gold]. [sine]Caaaaw[/sine]...",
               "{character}被一些[gold]{encounter}[/gold]杀害。[sine]咔咔[/sine]......"));
}
