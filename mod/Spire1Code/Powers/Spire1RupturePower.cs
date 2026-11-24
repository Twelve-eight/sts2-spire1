using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.RupturePower</c> - "Whenever you lose HP from a card,
/// gain Amount Strength."
/// <para>
/// Owned replacement for the shipped <c>MegaCrit.Sts2.Core.Models.Powers.RupturePower</c>. The
/// shipped power only gates on target, positive unblocked damage and the owner's side, so an
/// enemy-sourced hit on your turn also grants Strength. StS1 <c>wasHPLost</c> additionally requires
/// <c>DamageInfo.owner</c> to be the power owner; this power applies the reviewed self-source
/// predicate from the rupture fidelity candidate
/// (<c>spire1-official/rupture-fidelity-probe/Spire1RupturePower.cs</c>): exact owner as target and
/// as <c>DamageResult.Receiver</c>, actual unblocked HP loss above zero, exact owner as dealer, and
/// no current-side gate.
/// </para>
/// <para>
/// The grant is the same real command the candidate consumed for its 20 main and 10 safety cases:
/// <c>PowerCmd.Apply&lt;StrengthPower&gt;</c> on the owner, applied by the owner, with no card
/// argument. It runs in the hook the shipped Rupture power already uses; this power adds no global
/// hook, scheduler or config switch.
/// </para>
/// </summary>
public class Spire1RupturePower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse the shipped canonical power art; this mod authors no separate power icon.
    public override string CustomPackedIconPath =>
        ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.RupturePower>().PackedIconPath;

    public override string CustomBigIconPath =>
        ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.RupturePower>().ResolvedBigIconPath;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override List<(string, string)>? Localization => new PowerLoc(
        "Rupture",
        "Whenever you lose HP from a card, gain {Amount} Strength.",
        "Whenever you lose HP from a card, gain Strength.");

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // StS1 wasHPLost gate: positive HP loss, and the damage owner is this power's owner.
        // result.Receiver guards redirected damage (e.g. Osty) so only real owner HP loss counts;
        // a null or other-creature dealer never matches, so enemy attacks and Thorns grant nothing.
        if (!ReferenceEquals(target, Owner) || !ReferenceEquals(result.Receiver, Owner)
            || result.UnblockedDamage <= 0 || !ReferenceEquals(dealer, Owner))
        {
            return;
        }

        // C12: a Spire1-triggered effect is inert while the powers group is off; an existing
        // instance still winds down normally because the central gate allows removals.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
