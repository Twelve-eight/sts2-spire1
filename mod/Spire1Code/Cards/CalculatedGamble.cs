using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Silent — Calculated Gamble (Uncommon Skill). Discard your hand, then draw that many cards.
/// Exhaust (the upgrade removes Exhaust).
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class CalculatedGamble() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    // LocalKeywords caches CanonicalKeywords on first access, so Exhaust must be removed dynamically on upgrade.
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count > 0)
        {
            await CardCmd.DiscardAndDraw(choiceContext, hand, hand.Count);
        }
    }
}
