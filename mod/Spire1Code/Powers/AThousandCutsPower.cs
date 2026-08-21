using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent — A Thousand Cuts. Whenever you play a card, deal 1 damage (per stack) to ALL enemies.</summary>
public class AThousandCutsPower : CustomPowerModel
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForPlayedCards = new();
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "A Thousand Cuts",
            "#Whenever you play a card, deal {Amount} damage to ALL enemies.",
            "Whenever you play a card, deal damage to ALL enemies.");

    protected override object InitInternalData() => new Data();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner)
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
        await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, amount, ValueProp.Unpowered, Owner, null, null);
    }
}
