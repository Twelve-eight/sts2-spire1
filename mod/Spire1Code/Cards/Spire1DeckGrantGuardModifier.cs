using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Run;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// Owner-free run hook carrier for the final Spire1 deck-grant boundary.
/// It is never added to RunState.Modifiers or any custom modifier pool; the subscriber
/// returns one mutable clone only while RunState enumerates hook listeners.
/// </summary>
public sealed class Spire1DeckGrantGuardModifier : ModifierModel
{
    public override bool ShouldReceiveCombatHooks => false;

    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card is Spire1Card && Spire1CardsGateSnapshot.Closed)
        {
            return false;
        }

        return base.ShouldAddToDeck(card);
    }
}
