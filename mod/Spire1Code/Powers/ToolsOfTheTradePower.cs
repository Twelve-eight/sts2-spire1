using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent — Tools of the Trade. At the start of your turn, draw 1 card and discard 1 card (per stack).</summary>
public class ToolsOfTheTradePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Tools of the Trade",
            "#At the start of your turn, draw {Amount:plural:card|cards} and discard {Amount:plural:card|cards}.",
            "At the start of your turn, draw a card and discard a card.");

    // Hooks copied verbatim from the decompiled game's ToolsOfTheTradePower: the extra draw is part of the
    // turn-start hand draw, then the discard happens after the turn starts (needs a real choice context).
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player)
            return count;
        return count + Amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;
        var picked = (await CardSelectCmd.FromHandForDiscard(choiceContext, player, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, Amount), null, this)).ToList();
        if (picked.Count != 0)
        {
            await CardCmd.Discard(choiceContext, picked);
        }
    }
}
