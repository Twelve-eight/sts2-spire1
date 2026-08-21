using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Acts;

/// <summary>
/// StS1 Act 3, "The Beyond" (<c>com.megacrit.cardcrawl.dungeons.TheBeyond</c>).
/// <para>
/// Monster/elite/boss pools are empty until the Act-3 monsters are ported (M2.5+); encounters
/// attach via <see cref="Spire1Encounter.IsValidForAct"/> once they exist. The engine requires a
/// non-empty ancient list (CustomActModel throws otherwise), so this act borrows the shipped
/// act-3 ancients — StS1 has no ancients mechanic.
/// </para>
/// <para>
/// Bytecode: <c>generateMonsters()</c> = <c>generateWeakEnemies(2)</c> +
/// <c>generateStrongEnemies(12)</c> + <c>generateElites(10)</c>; map height 15.
/// </para>
/// </summary>
public sealed class TheBeyond : Spire1Act, ILocalizationProvider
{
    // ---- shipped act-3 art (glory), redirected through BaseLib's CustomActModel hooks ----

    protected override string CustomMapTopBgPath =>
        "res://images/packed/map/map_bgs/glory/map_top_glory.png";

    protected override string CustomMapMidBgPath =>
        "res://images/packed/map/map_bgs/glory/map_middle_glory.png";

    protected override string CustomMapBotBgPath =>
        "res://images/packed/map/map_bgs/glory/map_bottom_glory.png";

    protected override string CustomRestSiteBackgroundPath =>
        "res://scenes/rest_site/glory_rest_site.tscn";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng) => new("glory", rng);

    // ---- Index-dependent members that must not reach CustomActModel's switch ----

    /// <summary>Borrowed shipped act-3 ancients; see type remarks.</summary>
    public override IEnumerable<AncientEventModel> AllAncients => Act3Ancients;

    protected override int BaseNumberOfRooms => 15;

    /// <summary>TheBeyond.generateMonsters: generateWeakEnemies(2).</summary>
    protected override int NumberOfWeakEncounters => 2;

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) =>
        new(MapPointTypeCounts.StandardRandomUnknownCount(mapRng), mapRng.NextGaussianInt(7, 1, 6, 7));

    // ---- act contents ----

    /// <summary>Empty until Act-3 monsters land (M2.5+); see type remarks.</summary>
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => [];

    public override IEnumerable<EventModel> AllEvents => [];

    /// <summary>Empty until the Act-3 bosses (Awakened One / Time Eater / Donu &amp; Deca) are ported.</summary>
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => [];

    // ---- act-3 presentation, copied from the shipped Glory model ----

    public override string[] BgMusicOptions => ["event:/music/act3_a1_v1", "event:/music/act3_a2_v2"];

    public override string[] MusicBankPaths =>
        ["res://banks/desktop/act3_a1.bank", "res://banks/desktop/act3_a2.bank"];

    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";

    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act3";

    public override string ChestSpineSkinNameStroke => "act3_stroke";

    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

    public override Color MapTraveledColor => new("1D1E2F");

    public override Color MapUntraveledColor => new("60717C");

    public override Color MapBgColor => new("819A97");

    public List<(string, string)>? Localization => new ActLoc("TheBeyond");
}
