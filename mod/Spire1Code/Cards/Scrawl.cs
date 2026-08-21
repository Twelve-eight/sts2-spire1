using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Scrawl (Rare Skill, cost 1 / 0 upgraded). Draw cards until your hand is full. Exhaust.
/// Hand limit is the game's CardPile.MaxCardsInHand (10); the shipped StS2 Scrawl uses the same expression.
/// Our own class is required because the shipped Scrawl also gains Retain on upgrade.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Scrawl() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int count = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
        if (count <= 0)
            return;
        await CardPileCmd.Draw(choiceContext, count, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
