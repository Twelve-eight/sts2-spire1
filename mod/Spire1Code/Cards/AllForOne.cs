using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(Spire1LegacyPool))]
public class AllForOne() : Spire1Card(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        CardPile discard = PileType.Discard.GetPile(Owner);
        var cards = discard.Cards
            .Where(card => ReferenceEquals(card.Owner, Owner) && LegacyRecallCost.Matches(card))
            .ToList();
        CardPile hand = PileType.Hand.GetPile(Owner);
        foreach (var card in cards)
        {
            // Match DiscardToHandAction: the selection is fixed, but membership and
            // capacity are checked again when each queued transfer is reached.
            if (!ReferenceEquals(card.Owner, Owner)
                || !discard.Cards.Contains(card)
                || hand.Cards.Count >= CardPile.MaxCardsInHand)
                continue;
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
