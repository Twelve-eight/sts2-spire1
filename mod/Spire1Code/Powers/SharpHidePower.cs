using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.SharpHidePower</c>: <c>onUseCard</c> checks
/// <c>card.type == ATTACK</c> and then deals <c>amount</c> THORNS damage to the player.
/// <para>
/// Deliberately NOT the shipped <see cref="MegaCrit.Sts2.Core.Models.Powers.ThornsPower"/>: Thorns
/// retaliates per damage instance received (<c>BeforeDamageReceived</c>), so a multi-hit attack
/// triggers it several times and a non-damaging attack not at all. Sharp Hide fires exactly once
/// per Attack card played, hit or miss, which is what <c>AfterCardPlayed</c> reproduces — the same
/// hook the shipped <c>SneakyPower</c> uses for its "whenever an enemy plays an Attack" trigger.
/// </para>
/// </summary>
public sealed class SharpHidePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Sharp Hide",
            "#Whenever you play an Attack, take {Amount} damage.",
            "Whenever you play an Attack, take damage.");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        Creature? player = cardPlay.Card.Owner.Creature;
        if (player == null || player == Owner || player.IsDead)
            return;
        Flash();
        // ValueProp mirrors the shipped ThornsPower retaliation: unpowered (Strength must not
        // scale it) and no hurt anim, since the player is mid-card-play.
        await CreatureCmd.Damage(
            choiceContext, player, Amount, ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner, null, null);
    }
}
