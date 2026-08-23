using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Constricted (<c>com.megacrit.cardcrawl.powers.ConstrictedPower</c>, Spire Growth):
/// at the end of the victim's side turn it takes blockable, strength-immune damage
/// (StS1 DamageType.THORNS) equal to stacks. No shipped StS2 power matches (PlatingPower
/// regenerates block instead), so this is a custom power.
/// </summary>
public class ConstrictedPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Constricted",
            "At the end of its turn, lose HP equal to Constricted.",
            "At the end of this turn, lose HP equal to Constricted.");

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
        {
            return;
        }
        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, null, null);
    }
}
