using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Ironclad - Evolve. Whenever you draw a Status card, draw 1 card.</summary>
public class EvolvePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Evolve",
            "#Whenever you draw a Status card, draw {Amount:plural:card|cards}.",
            "Whenever you draw a Status card, draw a card.");

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner || card.Type != CardType.Status)
            return;
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: wind the disabled instance down at its own next relevant hook.
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}
