using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 - Plated Armor (<c>com.megacrit.cardcrawl.powers.PlatedArmorPower</c>):
/// at the end of your turn gain Block equal to this power's amount; lose 1 stack whenever you
/// take unblocked damage. Applied with 14 stacks by <see cref="Monsters.ShelledParasite"/>,
/// whose shell-break stun triggers once the stacks run out.
/// <para>
/// Deliberately NOT the shipped <see cref="MegaCrit.Sts2.Core.Models.Powers.PlatingPower"/>:
/// that one grants Block every round and decays by itself over time, while StS1's Plated Armor
/// only decays when actual unblocked damage lands. The player block tick runs in
/// <c>BeforeSideTurnEnd</c> (the pre-hand-end point; enemy owners keep <c>AfterSideTurnEnd</c>) and
/// grants unpowered Block, so Dexterity does not modify it; the stack loss hooks
/// <c>AfterDamageReceived</c>, gated exactly like
/// StS1's <c>wasHPLost</c>: unblocked damage AND a powered attack, which excludes HP loss,
/// thorns and other unpowered sources (see the gate comment on the method).
/// </para>
/// </summary>
public class PlatedArmorPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Plated Armor",
            "#At the end of your turn, gain {Amount} *Block*. Lose 1 Stack whenever you take unblocked damage.",
            "At the end of your turn, gain Block. Lose a Stack whenever you take unblocked damage.");

    // StS1's Plated Armor is a preEndTurnCards power: GameActionManager gives it its Block before the
    // hand's end-of-turn cards (Burn, etc.) resolve. The host's BeforeSideTurnEnd is that point; the
    // player tick must not also run in AfterSideTurnEnd, or Burn damage would land first and the
    // power would tick twice. Enemy owners keep the existing AfterSideTurnEnd timing because that is
    // the host lifecycle their current behaviour uses.
    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        return side == CombatSide.Player
            ? GrantBlock(participants)
            : Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        return side == CombatSide.Player
            ? Task.CompletedTask
            : GrantBlock(participants);
    }

    private async Task GrantBlock(IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
        {
            return;
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // StS1 gate (javap com.megacrit.cardcrawl.powers.PlatedArmorPower#wasHPLost, authoritative
        // jar): the decrement requires ALL of - the damage source is non-null, the source is not
        // the power's own owner (no self-inflicted decay), DamageType is neither HP_LOSS nor
        // THORNS, and the lost amount is > 0. StS2's equivalent of the two excluded damage types
        // is "not a powered attack" (ValuePropExtensions.IsPoweredAttack: Move without Unpowered);
        // the same gate the shipped FlutterPower uses. So thorns, HP loss and other unpowered
        // sources must NOT strip a stack.
        if (target != Owner || result.UnblockedDamage <= 0 || !props.IsPoweredAttack()
            || dealer is null || ReferenceEquals(dealer, Owner))
        {
            return;
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        Flash();
        await PowerCmd.Decrement(this);
    }
}
