using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad - Girya (Rare). At the start of each combat, gain !StrengthPower!.</summary>
/// <remarks>
/// StS1 Girya gains its Strength from rest-site lifts: <see cref="StrengthBonus"/> is that lift count, capped at
/// <see cref="MaxLifts"/>, and is also the amount applied at combat start. The rest-site option we offer is our own
/// <see cref="GiryaRestSiteOption"/>; the engine's <c>LiftRestSiteOption</c> cannot be reused because it is hard-bound
/// to the vanilla StS2 Girya (it increments that relic's <c>TimesLifted</c>, LiftRestSiteOption.cs:41).
///
/// Once acquired, possession is the authority: the offer, the lift and the combat Strength below gate only on this
/// instance still being in <c>Owner.Relics</c> and not melted, exactly like a vanilla relic. They do not read the
/// local content switches or any per-run/ambient content latch. Pool and shop eligibility stays a separate,
/// acquisition-time question in <see cref="Spire1Relic"/> (<c>IsAllowedInternal</c>/<c>IsAllowedInShops</c>), so
/// turning a local switch off stops future obtainment but does not retroact on a relic the player already owns.
/// </remarks>
public class Girya : Spire1Relic
{
    /// <summary>StS1 Girya caps at three rest-site lifts.</summary>
    public const int MaxLifts = 3;

    private int _strengthBonus;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => StrengthBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(0)];

    // Localisation is the mod JSON only: mod/Spire1/localization/{eng,zhs}/relics.json `SPIRE1-GIRYA.*`
    // (title/description/flavor, both languages). Deliberately no ILocalizationProvider override here:
    // BaseLib's ModelLocPatch (postfix on ModelDb.Init) writes the in-code pair over the already-merged
    // mod table for the ACTIVE language, and OneTimeInitialization.ExecuteEssential calls
    // LocManager.Initialize() (which merges the mod JSON, LocManager.LoadTablesFromPath) before
    // ModelDb.Init(). A C# description would therefore replace the Chinese text with English on a zhs
    // client, and BaseLib re-applies nothing after a locale change. Keeping the text out of code is what
    // keeps the lift instruction in both languages; see GiryaRestSiteOption for the rest-site wording.

    /// <summary>
    /// Rest-site lifts taken; also the Strength applied at combat start. Persisted through <see cref="SavedPropertyAttribute"/>,
    /// so a lifted Girya keeps its value across save/load. The setter mirrors the stored value into the description's
    /// <c>!StrengthPower!</c> variable (the same pattern GeneticAlgorithm uses for its [SavedProperty] counter).
    /// </summary>
    [SavedProperty]
    public int StrengthBonus
    {
        get => _strengthBonus;
        set
        {
            AssertMutable();
            _strengthBonus = value;
            DynamicVars.Strength.BaseValue = value;
            InvokeDisplayAmountChanged();
        }
    }

    /// <summary>
    /// True only while <paramref name="player"/> is this relic's owner, this exact instance is still in that
    /// player's relic list, and it has not been melted. Removal takes the instance out of <c>Player.Relics</c>
    /// (<c>Player.RemoveRelicInternal</c>); melting leaves it in the list and only sets
    /// <c>IsMelted</c> (<c>Player.MeltRelicInternal</c>), so both signals are required.
    /// </summary>
    private bool IsHeldBy(Player? player)
        => player != null
           && Owner == player
           && !IsMelted
           && player.Relics.Contains(this);

    /// <summary>
    /// Applies one rest-site lift. Returns false once a lift would exceed the StS1 cap, or when the option
    /// outlived the relic (instance removed from the owner's relic list, or melted) so a stale rest-site option
    /// cannot mutate it. Reads only the relic's own possession state; local content switches are deliberately not
    /// consulted (see the type remarks).
    /// </summary>
    public bool TryLift()
    {
        if (!IsHeldBy(Owner) || StrengthBonus >= MaxLifts)
        {
            return false;
        }

        StrengthBonus = StrengthBonus + 1;
        Flash();
        return true;
    }

    /// <summary>
    /// Offers our own Lift option for the owning player while this relic is held and still has lifts left.
    /// Engine dispatch: <c>RestSiteOption.Generate</c> calls <c>Hook.ModifyRestSiteOptions</c> for every hook
    /// listener of the run (<c>RestSiteOption.cs:53-74</c>), so the owner check keeps other players' relics from
    /// adding a duplicate button. The offer follows possession, never a local content switch or the run latch:
    /// option generation is replayed per client and only the chosen index travels over the network, so one
    /// client's configuration must not add or remove an entry. This model applies vanilla
    /// <see cref="StrengthPower"/> with a null card source, outside EnableSts1Powers and outside
    /// Spire1PowersGatePatch.
    /// </summary>
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (!IsHeldBy(player) || StrengthBonus >= MaxLifts)
        {
            return false;
        }

        options.Add(new GiryaRestSiteOption(player));
        return true;
    }

    public override async Task BeforeCombatStart()
    {
        // Possession is the authority, like a vanilla relic: the hook only iterates relics that are still in the
        // owner's list and not melted, and the explicit guard keeps a stale or melted instance from applying.
        Player owner = Owner;
        if (StrengthBonus <= 0 || !IsHeldBy(owner))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), owner.Creature, StrengthBonus, owner.Creature, null);
    }
}
