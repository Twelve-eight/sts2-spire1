using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.AngryPower</c> — "Whenever this creature receives
/// attack damage, gain <see cref="Amount"/> Strength."
/// <para>
/// Bytecode gate (<c>onAttacked(DamageInfo, int damageAmount)</c>, re-verified directly against
/// <c>javap com.megacrit.cardcrawl.powers.AngryPower</c>): the damage must have a non-null owner,
/// <c>damageAmount &gt; 0</c>, and a type that is neither HP_LOSS nor THORNS. StS2 equivalent: the
/// damage must be a powered attack (<c>props.IsPoweredAttack()</c>, i.e. <c>ValueProp.Move</c>
/// without <c>Unpowered</c> - HP-loss and thorns sources never carry <c>Move</c>) and it must have
/// landed unblocked damage (<c>UnblockedDamage &gt; 0</c> is StS1's <c>damageAmount &gt; 0</c>).
/// The shipped engine has no Angry power (checked against all 268 shipped power models), so this is
/// a new custom power.
/// </para>
/// </summary>
public class AngryPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Angry",
            $"#Whenever this creature receives attack damage, gain {{Amount}} Strength.",
            "Whenever this creature receives attack damage, gain Strength.");

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || !props.IsPoweredAttack() || result.UnblockedDamage <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
