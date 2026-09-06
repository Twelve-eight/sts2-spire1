using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Shelled Parasite and Fungi" strong encounter
/// (<c>MonsterHelper.getEncounter("Shelled Parasite and Fungi")</c>).
/// Bytecode (<c>.tmp/r9-tmp/MonsterHelper.txt</c>, getEncounter case 13):
/// <c>new MonsterGroup(new ShelledParasite(-260f, 15f), new FungiBeast(120f, 0f))</c> —
/// one <see cref="ShelledParasite"/> (Act-2) plus one <see cref="FungiBeast"/> (Act-1).
/// <para>
/// Official run-history display name (<c>localization/eng/ui.json</c>
/// <c>RunHistoryMonsterNames.TEXT[13]</c>): "Parasite and Fungi Beast"; zhs index 13:
/// "寄生怪与真菌兽".
/// </para>
/// <para>
/// Gold: StS1 normal monster rooms pay <c>treasureRng.random(10, 20)</c>
/// (<c>AbstractRoom.applyEvents</c> bytecode :1004-1015), matching the shipped StS2
/// <c>RoomType.Monster</c> default of 10..20 — no override needed.
/// </para>
/// </summary>
public sealed class ShelledParasiteAndFungiEncounter : Spire1Encounter
{
    public ShelledParasiteAndFungiEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<ShelledParasite>(),
        ModelDb.Monster<FungiBeast>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<ShelledParasite>().ToMutable(), null),
        (ModelDb.Monster<FungiBeast>().ToMutable(), null),
    ];

    // zh title is the official StS1 zhs run-history name (localization/zhs/ui.json,
    // RunHistoryMonsterNames.TEXT[13]); loss line reuses the shipped StS2 zhs infestation
    // death message (encounters-zhs.json DENSE_VEGETATION_EVENT_ENCOUNTER.loss).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new EncounterLoc(Tr("Parasite and Fungi Beast", "寄生怪与真菌兽"),
            Tr("{character} was infested by [gold]{encounter}[/gold].",
               "{character}遭[gold]{encounter}[/gold]寄生而死。"));
}
