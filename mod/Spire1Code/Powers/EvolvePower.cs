using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

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
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }
}
