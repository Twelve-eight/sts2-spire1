using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Acts;

/// <summary>
/// StS1 Act 4, "The Ending" (<c>com.megacrit.cardcrawl.dungeons.TheEnding</c>).
/// <para>
/// Bytecode: <c>generateMonsters()</c> is EMPTY — vanilla The Ending has no normal monster,
/// elite or chest rooms; its fights are the two Spire Heart phases plus forced encounters.
/// The StS2 map generator (<c>StandardActMap</c>) always assigns Monster points and
/// <c>GenerateRooms</c> fills them from the encounter pools, so a faithful empty pool would
/// produce empty combat rooms. Until the Ending-specific content is ported (Spire Heart, the
/// Death/Obeloth/Face encounters), this act therefore reuses the standard act-3 room mix as a
/// placeholder shell — explicitly NOT vanilla-faithful, flagged here and in DEVLOG.
/// </para>
/// <para>
/// No dedicated art ships for a fourth act (the unused <c>factory</c> background set has no
/// map_bgs/rest_site variants), so presentation borrows the shipped act-3 (glory) assets. The
/// engine requires a non-empty ancient list; borrowed shipped act-3 ancients.
/// </para>
/// </summary>
public sealed class TheEnding : Spire1Act, ILocalizationProvider
{

    public override int Sts1ActNumber => 4;
    // ---- borrowed shipped act-3 art (glory); no fourth-act art ships ----

    protected override string CustomMapTopBgPath =>
        "res://Spire1/images/map_bgs/map_top_the_ending.png";

    protected override string CustomMapMidBgPath =>
        "res://Spire1/images/map_bgs/map_middle_the_ending.png";

    protected override string CustomMapBotBgPath =>
        "res://Spire1/images/map_bgs/map_bottom_the_ending.png";

    protected override string CustomRestSiteBackgroundPath =>
        "res://scenes/rest_site/glory_rest_site.tscn";

    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng) => new("glory", rng);

    // ---- Index-dependent members that must not reach CustomActModel's switch ----

    /// <summary>Borrowed shipped act-3 ancients; see type remarks.</summary>
    public override IEnumerable<AncientEventModel> AllAncients => Act3Ancients;

    protected override int BaseNumberOfRooms => 15;

    /// <summary>
    /// Vanilla The Ending has NO weak encounters (empty generateMonsters). Kept at 0: the
    /// placeholder normal pool below still feeds the regular rooms.
    /// </summary>
    protected override int NumberOfWeakEncounters => 0;

    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) =>
        new(MapPointTypeCounts.StandardRandomUnknownCount(mapRng), mapRng.NextGaussianInt(7, 1, 6, 7));

    // ---- act contents ----

    /// <summary>Empty until the Ending encounters land; see type remarks.</summary>
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => [];

    public override IEnumerable<EventModel> AllEvents => [];

    /// <summary>Empty until Spire Heart is ported.</summary>
    public override IEnumerable<EncounterModel> BossDiscoveryOrder => [];

    // ---- borrowed act-3 presentation ----

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

    public List<(string, string)>? Localization => new ActLoc("TheEnding");
}
