using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Talk to the Hand. Debuff on ONE enemy: whenever a player attacks that enemy, the attacker gains
/// Block equal to this power's amount. Block is granted even when the hit is fully blocked and on the killing blow,
/// matching StS1's onAttacked timing, which is why this listens on AfterDamageGiven (fires for every damage result)
/// rather than AfterDamageReceived (skipped when the target dies). Hook shape copied from the mod's EnvenomPower.
/// </summary>
public class TalkToTheHandPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Talk to the Hand",
            "#Whenever the player attacks this enemy, they gain {Amount} Block.",
            "Whenever the player attacks this enemy, they gain Block.");

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer.Player == null || !props.IsPoweredAttack() || Amount <= 0)
            return;
        Flash();
        await CreatureCmd.GainBlock(dealer, Amount, ValueProp.Unpowered, null);
    }
}
