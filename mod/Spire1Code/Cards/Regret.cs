using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Regret (Curse). Unplayable. At the end of your turn, lose 1 HP for each card in your hand.
/// Mirror of the base-game Regret (BeforeSideTurnEnd hand snapshot + OnTurnEndInHand).
/// </summary>
public class Regret() : Spire1Curse()
{
    private int _cardsInHand;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool HasTurnEndInHandEffect => true;

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return Task.CompletedTask;
        }
        if (Pile is not { Type: PileType.Hand })
        {
            return Task.CompletedTask;
        }
        _cardsInHand = Pile.Cards.Count;
        return Task.CompletedTask;
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, _cardsInHand,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, null);
        _cardsInHand = 0;
    }
}
