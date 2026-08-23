using MegaCrit.Sts2.Core.Entities.Creatures;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.DrawReductionPower</c>. Reduces the owner player's
/// hand size by Amount on their next turn, then removes itself — the engine's
/// <c>ModifyHandDraw</c> hook is the direct equivalent of vanilla's <c>onPlayerDraw</c> cut.
/// </summary>
public sealed class DrawReductionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player == base.Owner.Player)
        {
            return count - Amount;
        }
        return count;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Draw Reduction",
            "Draw {Amount} fewer card(s) next turn.",
            "Draw {Amount} fewer card(s) next turn.");
}
