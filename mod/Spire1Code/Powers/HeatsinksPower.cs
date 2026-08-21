using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Powers;

public class HeatsinksPower : CustomPowerModel
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForPowerCards = new();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Heatsinks",
            "#Whenever you play a Power card, draw {Amount} card(s).",
            "#Whenever you play a Power card, draw cards.");

    protected override object InitInternalData() => new Data();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Power)
            GetInternalData<Data>().AmountsForPowerCards[cardPlay.Card] = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!GetInternalData<Data>().AmountsForPowerCards.Remove(cardPlay.Card, out int amount) || amount <= 0)
            return;
        Flash();
        await CardPileCmd.Draw(choiceContext, amount, Owner.Player);
    }
}
