using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Pain (Curse). Unplayable. While in hand, lose 1 HP whenever you play a card.
/// Not present in StS2 (no base-game mirror); implemented via the AbstractModel.AfterCardPlayed
/// combat hook (fires for every card play; cards receive it while in a combat pile).
/// </summary>
public class Pain() : Spire1Curse()
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
        {
            return;
        }
        if (Pile is not { Type: PileType.Hand })
        {
            return;
        }
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 1,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, null);
    }
}
