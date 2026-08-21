using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace Spire1.Spire1Code.Powers;

public class StormPower : CustomPowerModel
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForPlayedCards = new();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Storm",
            "#Whenever you play a Power card, Channel {Amount} Lightning.",
            "Whenever you play a Power card, Channel Lightning.");

    protected override object InitInternalData() => new Data();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Power)
            GetInternalData<Data>().AmountsForPlayedCards[cardPlay.Card] = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner ||
            !GetInternalData<Data>().AmountsForPlayedCards.Remove(cardPlay.Card, out int amount) ||
            amount <= 0)
            return;

        Flash();
        for (int i = 0; i < amount; i++)
            await OrbCmd.Channel<LightningOrb>(choiceContext, Owner.Player);
    }
}
