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

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 — Plated Armor (<c>com.megacrit.cardcrawl.powers.PlatedArmorPower</c>):
/// at the end of your turn gain Block equal to this power's amount; lose 1 stack whenever you
/// take unblocked damage. Applied with 14 stacks by <see cref="Monsters.ShelledParasite"/>,
/// whose shell-break stun triggers once the stacks run out.
/// <para>
/// Deliberately NOT the shipped <see cref="MegaCrit.Sts2.Core.Models.Powers.PlatingPower"/>:
/// that one grants Block every round and decays by itself over time, while StS1's Plated Armor
/// only decays when actual unblocked damage lands. The block tick reuses the Metallicize idiom
/// (<c>AfterSideTurnEnd</c>); the stack loss hooks <c>AfterDamageReceived</c>, gated on
/// unblocked damage like StS1's non-HP-loss/non-thorns check.
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

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
        {
            return;
        }
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0)
        {
            return;
        }
        Flash();
        await PowerCmd.Decrement(this);
    }
}
