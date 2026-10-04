using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

public class StaticDischargePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Static Discharge",
            "#Whenever you receive unblocked attack damage, Channel {Amount} Lightning.",
            "Whenever you receive unblocked attack damage, Channel Lightning.");

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || dealer == null || !props.IsPoweredAttack() || result.UnblockedDamage <= 0 || Amount <= 0)
            return;

        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: the disabled instance must not linger on the creature forever;
            // its only reachable hook is this damage reaction, so wind it down here.
            await PowerCmd.Remove(this);
            return;
        }

        Flash();
        for (int i = 0; i < Amount; i++)
        {
            await OrbCmd.Channel<LightningOrb>(choiceContext, Owner.Player);
        }
    }
}
