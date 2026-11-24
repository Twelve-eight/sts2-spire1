using System.Threading;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// Rest-site "Lift" option for our own <see cref="Girya"/>. The engine's LiftRestSiteOption is hard-bound to the
/// vanilla StS2 Girya (TimesLifted, ModelDb.Relic&lt;Girya&gt;()), so this is the minimal copy that touches ours instead.
/// </summary>
/// <remarks>
/// Identity: <see cref="OptionId"/> is "SPIRE1_LIFT", deliberately not the engine's "LIFT". The base
/// <c>RestSiteOption.Equals</c> compares (OptionId, Owner) only and ignores the concrete type
/// (RestSiteOption.cs:100-108), and the engine resolves a click through
/// <c>NRestSiteRoom.Options.IndexOf(option)</c> plus <c>GetButtonForOption</c>'s <c>item.Option == option</c>.
/// With a shared id our option and a vanilla <c>LiftRestSiteOption</c> of the same player compare equal in
/// the vanilla-to-ours direction, so the vanilla option's <c>OnSelect</c> (which increments the vanilla
/// relic's TimesLifted) could answer our button. A distinct id makes the base equality false in both
/// directions, so no Equals/GetHashCode override is needed.
///
/// Localisation: the base <c>Title</c> is non-virtual and always resolves
/// <c>rest_site_ui:OPTION_&lt;OptionId&gt;.name</c>, so this option needs its own table entries
/// <c>OPTION_SPIRE1_LIFT.name</c> / <c>OPTION_SPIRE1_LIFT.description</c> in both eng and zhs
/// (mod/Spire1/localization/{eng,zhs}/rest_site_ui.json). Only <c>LiftsLeft</c> is supplied here,
/// exactly as LiftRestSiteOption does.
///
/// Icon: the base <c>IconPath</c> derives <c>ui/rest_site/option_spire1_lift.png</c> from the id, which
/// ships nowhere. <see cref="CustomIconPath"/> points at the vanilla lift art through the same
/// <c>ImageHelper</c> call the engine composes for OptionId "LIFT"; BaseLib's prefix on that getter
/// replaces the path, and <c>AssetPaths</c> reads the same getter so preloading follows.
/// </remarks>
public class GiryaRestSiteOption : CustomRestSiteOption
{
    public GiryaRestSiteOption(Player owner)
        : base(owner)
    {
    }

    /// <summary>Distinct from the engine's "LIFT" so the base (OptionId, Owner) equality cannot collide.</summary>
    public override string OptionId => "SPIRE1_LIFT";

    /// <summary>
    /// The vanilla lift image: the base IconPath would ask for <c>option_spire1_lift.png</c>, which no pack
    /// ships. Derived through <see cref="ImageHelper"/> rather than a hand-written <c>res://</c> literal so
    /// the path stays identical to what the engine builds for the vanilla option.
    /// </summary>
    public override string? CustomIconPath => ImageHelper.GetImagePath("ui/rest_site/option_lift.png");

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("LiftsLeft", LiftsLeft);
            return description;
        }
    }

    private int LiftsLeft => Girya.MaxLifts - (Owner.GetRelic<Girya>()?.StrengthBonus ?? 0);

    public override Task<bool> OnSelect()
    {
        Girya? relic = Owner.GetRelic<Girya>();
        return Task.FromResult(relic is not null && relic.TryLift());
    }

    public override Task DoLocalPostSelectVfx(CancellationToken ct = default(CancellationToken))
    {
        // Same feedback as the vanilla lift (LiftRestSiteOption.cs:46-50); the relic flash comes from Girya.TryLift.
        NGame.Instance?.ScreenShake(ShakeStrength.Strong, ShakeDuration.Short);
        return Task.CompletedTask;
    }
}
