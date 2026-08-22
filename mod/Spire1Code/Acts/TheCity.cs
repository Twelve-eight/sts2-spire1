using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Acts;

/// <summary>
/// StS1 Act 2, "The City" (<c>com.megacrit.cardcrawl.dungeons.TheCity</c>).
/// <para>
/// Monster/elite/boss pools are empty until the Act-2 monsters are ported (M2.5); encounters
/// attach via <see cref="Spire1Encounter.IsValidForAct"/> once they exist. The engine requires a
/// non-empty ancient list (CustomActModel throws otherwise), so this act borrows the shipped
/// act-2 ancients — StS1 has no ancients mechanic, and Neow only belongs to act 1.
/// </para>
/// <para>
/// Bytecode: <c>generateMonsters()</c> = <c>generateWeakEnemies(2)</c> +
/// <c>generateStrongEnemies(12)</c> + <c>generateElites(10)</c>; map height is the global
/// AbstractDungeon.MAP_HEIGHT = 15, matching BaseNumberOfRooms.
/// </para>
/// </summary>
public sealed class TheCity : Spire1Act, ILocalizationProvider
{

    public override int Sts1ActNumber => 2;
    // ---- shipped act-2 art (hive), redirected through BaseLib's CustomActModel hooks ----

    protected override string CustomMapTopBgPath =>
        "res://Spire1/images/map_bgs/map_top_the_city.png";

    protected override string CustomMapMidBgPath =>
        "res://Spire1/images/map_bgs/map_middle_the_city.png";

    protected override string CustomMapBotBgPath =>
        "res://Spire1/images/map_bgs/map_bottom_the_city.png";

    protected override string CustomRestSiteBackgroundPath =>
        "res://scenes/rest_site/hive_rest_site.tscn";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng) => new("hive", rng);

    // ---- Index-dependent members that must not reach CustomActModel's switch ----

    /// <summary>Borrowed shipped act-2 ancients; see type remarks.</summary>
    public override IEnumerable<AncientEventModel> AllAncients => Act2Ancients;

    protected override int BaseNumberOfRooms => 15;

    /// <summary>TheCity.generateMonsters: generateWeakEnemies(2).</summary>
    protected override int NumberOfWeakEncounters => 2;

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) =>
        new(MapPointTypeCounts.StandardRandomUnknownCount(mapRng), mapRng.NextGaussianInt(7, 1, 6, 7));

    // ---- act contents ----

    /// <summary>Empty until Act-2 monsters land (M2.5); see type remarks.</summary>
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => [];

    public override IEnumerable<EventModel> AllEvents => [];

    /// <summary>Empty until the Act-2 bosses (Champ / Automaton / Collector) are ported.</summary>
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => [];

    // ---- act-2 presentation, copied from the shipped Hive model ----

    public override string[] BgMusicOptions => ["event:/music/act2_a1_v2", "event:/music/act2_a2_v2"];

    public override string[] MusicBankPaths =>
        ["res://banks/desktop/act2_a1.bank", "res://banks/desktop/act2_a2.bank"];

    public override string AmbientSfx => "event:/sfx/ambience/act2_ambience";

    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_2_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act2";

    public override string ChestSpineSkinNameStroke => "act2_stroke";

    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act2";

    public override Color MapTraveledColor => new("27221C");

    public override Color MapUntraveledColor => new("6E7750");

    public override Color MapBgColor => new("9B9562");

    public List<(string, string)>? Localization => new ActLoc("TheCity");
}
