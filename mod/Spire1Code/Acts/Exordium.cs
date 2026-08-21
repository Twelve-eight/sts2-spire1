using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Encounters;

namespace Spire1.Spire1Code.Acts;

/// <summary>
/// StS1 Act 1, "Exordium" (<c>com.megacrit.cardcrawl.dungeons.Exordium</c>).
/// <para>
/// The act ships no art of its own. Every asset path below is a real shipped StS2 path, verified
/// present in <c>SlayTheSpire2.pck</c>, and all of them belong to <c>Overgrowth</c> — the shipped
/// StS2 act 1 — because that is the act whose position in the run Exordium takes. The base
/// <see cref="ActModel"/> builds these four paths from <c>Id.Entry.ToLowerInvariant()</c>, which
/// would resolve to a non-existent <c>exordium</c> folder, so BaseLib's four <c>Custom…Path</c>
/// hooks redirect them.
/// </para>
/// <para>
/// <see cref="Spire1Act"/> passes <c>-1</c> to <c>CustomActModel</c>, so <c>Index</c> is <c>-2</c>
/// and the act never spawns naturally (<c>DungeonSelectionPatch</c> places it). Every
/// <c>CustomActModel</c> member that switches on <c>Index</c> is therefore overridden here:
/// <see cref="AllAncients"/> would throw outright, and <see cref="BaseNumberOfRooms"/> /
/// <see cref="GetMapPointTypes"/> would silently fall through to a non-act-1 default.
/// </para>
/// <para>
/// The encounter list is deliberately empty: BaseLib's <c>AddActContent</c> postfixes
/// <see cref="GenerateAllEncounters"/> and appends every <c>CustomEncounterModel</c> whose
/// <c>IsValidForAct</c> accepts this act, which is exactly what <c>Spire1Encounter</c> does.
/// The method must still be *declared* here — <c>AddActContent</c> patches
/// <c>AccessTools.DeclaredMethod(type, "GenerateAllEncounters")</c> and skips types that only
/// inherit it.
/// </para>
/// </summary>
public sealed class Exordium : Spire1Act, ILocalizationProvider
{
    // ---- shipped act-1 art, redirected through BaseLib's CustomActModel hooks ----

    protected override string CustomMapTopBgPath =>
        "res://images/packed/map/map_bgs/overgrowth/map_top_overgrowth.png";

    protected override string CustomMapMidBgPath =>
        "res://images/packed/map/map_bgs/overgrowth/map_middle_overgrowth.png";

    protected override string CustomMapBotBgPath =>
        "res://images/packed/map/map_bgs/overgrowth/map_bottom_overgrowth.png";

    protected override string CustomRestSiteBackgroundPath =>
        "res://scenes/rest_site/overgrowth_rest_site.tscn";

    /// <summary>
    /// Combat backgrounds. <c>CustomActModel</c> defaults to the act-3 <c>"glory"</c> set, which
    /// would clash with the act-1 map above; <c>overgrowth</c> is the shipped act-1 layer set
    /// (<c>res://scenes/backgrounds/overgrowth/layers/*.tscn</c>).
    /// </summary>
    protected override BackgroundAssets CustomGenerateBackgroundAssets(Rng rng) => new("overgrowth", rng);

    // ---- Index-dependent members that must not reach CustomActModel's switch ----

    /// <summary>
    /// <c>CustomActModel.AllAncients</c> throws for any non-basegame <c>Index</c>. StS1 Act 1
    /// opens on Neow, which is what <c>Act1Ancients</c> holds.
    /// </summary>
    public override IEnumerable<AncientEventModel> AllAncients => Act1Ancients;

    /// <summary>
    /// <c>AbstractDungeon.MAP_HEIGHT = 15</c>; both shipped StS2 act-1 models agree
    /// (<c>Overgrowth</c>/<c>Underdocks</c> use 15). Excludes the boss and ancient floors.
    /// </summary>
    protected override int BaseNumberOfRooms => 15;

    /// <summary>
    /// <c>Exordium.generateMonsters()</c> is <c>generateWeakEnemies(3)</c> +
    /// <c>generateStrongEnemies(12)</c>, so 3 of the 15 rooms draw from the weak pool.
    /// </summary>
    protected override int NumberOfWeakEncounters => 3;

    /// <summary>
    /// Act-1 rest/unknown counts, copied from <c>CustomActModel.GetMapPointTypes</c>'s
    /// <c>Index == 0</c> branch; the fallthrough default would fix rests at 6.
    /// </summary>
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng) =>
        new(MapPointTypeCounts.StandardRandomUnknownCount(mapRng), mapRng.NextGaussianInt(7, 1, 6, 7));

    // ---- act contents ----

    /// <summary>
    /// Empty by design; see the type remarks. Encounters attach themselves via
    /// <c>Spire1Encounter.IsValidForAct</c>.
    /// </summary>
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => [];

    /// <summary>
    /// Empty by design, same pattern: BaseLib postfixes this getter and appends every
    /// <c>CustomEventModel</c> that lists this act in <c>Acts</c>, plus
    /// <c>ModelDb.AllSharedEvents</c> is concatenated by <c>ActModel.GenerateRooms</c>.
    /// </summary>
    public override IEnumerable<EventModel> AllEvents => [];

    /// <summary>
    /// StS1's <c>Exordium.initializeBoss()</c> shows the three act-1 bosses in a fixed order until
    /// each has been seen — <c>isBossSeen("GUARDIAN")</c>, then <c>"GHOST"</c>, then <c>"SLIME"</c>,
    /// and only once all three are seen does it shuffle. That is precisely what StS2's
    /// <c>BossDiscoveryOrder</c> + <c>ApplyDiscoveryOrderModifications</c> do.
    /// </summary>
    public override IEnumerable<EncounterModel> BossDiscoveryOrder =>
    [
        ModelDb.Encounter<TheGuardianEncounter>(),
        ModelDb.Encounter<HexaghostEncounter>(),
        ModelDb.Encounter<SlimeBossEncounter>(),
    ];

    // ---- act-1 presentation, all values copied verbatim from the shipped Overgrowth model ----

    public override string[] BgMusicOptions => ["event:/music/act1_a1_v1", "event:/music/act1_a2_v2"];

    public override string[] MusicBankPaths =>
        ["res://banks/desktop/act1_a1.bank", "res://banks/desktop/act1_a2.bank"];

    public override string AmbientSfx => "event:/sfx/ambience/act1_ambience";

    public override string ChestSpineResourcePath =>
        "res://animations/backgrounds/treasure_room/chest_room_act_1_skel_data.tres";

    public override string ChestSpineSkinNameNormal => "act1";

    public override string ChestSpineSkinNameStroke => "act1_stroke";

    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act1";

    public override Color MapTraveledColor => new("28231D");

    public override Color MapUntraveledColor => new("877256");

    public override Color MapBgColor => new("A78A67");

    /// <summary><c>ActModel.Title</c> reads <c>acts:&lt;Id.Entry&gt;.title</c>.</summary>
    public List<(string, string)>? Localization => new ActLoc("Exordium");
}
