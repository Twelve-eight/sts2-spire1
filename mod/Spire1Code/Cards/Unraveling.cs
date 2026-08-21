using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Unraveling (Rare Skill, cost 2 / 1 upgraded). Play all of your cards from left to right with random
/// targets. Exhaust.
/// The hand is snapshotted first (auto-playing a card can add or remove cards), then each card is auto-played in hand
/// order. CardCmd.AutoPlay with a null target picks the target from Rng.CombatTargets, which is the game's own random
/// targeting for auto-plays.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Unraveling() : Spire1Card(2, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (CardModel card in hand)
        {
            if (card == this || card.Pile?.Type != PileType.Hand)
                continue;
            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
