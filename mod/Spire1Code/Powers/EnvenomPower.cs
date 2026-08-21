using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent — Envenom. Whenever an Attack deals unblocked damage, apply 1 Poison (per stack).</summary>
public class EnvenomPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Envenom",
            "#Whenever an Attack deals unblocked damage, apply {Amount} *Poison*.",
            "Whenever an Attack deals unblocked damage, apply Poison.");

    // Hook + condition copied verbatim from the decompiled game's EnvenomPower/ConcoctPower.AfterDamageGiven.
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner && props.IsPoweredAttack() && result.UnblockedDamage > 0)
        {
            Flash();
            await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Owner, null);
        }
    }
}
