using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;

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
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return Task.CompletedTask;
        }
        if (cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Power)
            GetInternalData<Data>().AmountsForPowerCards[cardPlay.Card] = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!GetInternalData<Data>().AmountsForPowerCards.Remove(cardPlay.Card, out int amount) || amount <= 0)
            return;
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: wind the disabled instance down at its own next relevant hook.
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        await CardPileCmd.Draw(choiceContext, amount, Owner.Player);
    }
}
