using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Simmering Fury (the card is named Simmering Fury, the StS1 power is "Wrath Next Turn").
/// At the start of the owner's next turn, enter Wrath and draw cards equal to this power's amount, then expire.
/// </summary>
public class SimmeringFuryPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Simmering Fury",
            "#At the start of your next turn, enter Wrath and draw {Amount} cards.",
            "At the start of your next turn, enter Wrath and draw cards.");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: this one-shot "next turn" power must not draw or enter Wrath
            // while the powers group is off; remove it at its own next relevant hook.
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        int toDraw = Amount;
        await StanceCmd.Enter<WrathPower>(choiceContext, player, null);
        // C12 r5: only pay the "enter Wrath and draw" effect when Wrath is actually mounted.
        // A drifted/blocked mount returns without attaching, and this one-shot power must not
        // draw from a request that did not happen; it is still wound down so it cannot retry
        // every turn.
        if (player.Creature.GetPower<WrathPower>() == null)
        {
            await PowerCmd.Remove(this);
            return;
        }
        if (toDraw > 0)
            await CardPileCmd.Draw(choiceContext, toDraw, player);
        await PowerCmd.Remove(this);
    }
}
