using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Ancient Writing (Back to Basics).
/// Upgrade all Strikes and Defends, or remove one card from the deck.
/// StS1 matches on the STARTER_STRIKE / STARTER_DEFEND card tags; the mod's Strike/Defend
/// starter cards carry <see cref="CardTag.Strike"/> / <see cref="CardTag.Defend"/>.
/// </summary>
public class BackToBasics : Spire1Event
{
    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "tablet_of_truth";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(RemoveCard),
            Option(UpgradeCards),
        ];
    }

    private async Task RemoveCard()
    {
        CardSelectorPrefs prefs = new(L10NLookup($"{Id.Entry}.pages.ELEGANCE.selectionScreenPrompt"), 1)
        {
            Cancelable = false,
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        foreach (CardModel card in selected)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
        SetEventFinished(PageDescription("ELEGANCE"));
    }

    private async Task UpgradeCards()
    {
        List<CardModel> cards = PileType.Deck.GetPile(Owner).Cards
            .Where(c => (c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend)) && c.IsUpgradable)
            .ToList();
        foreach (CardModel card in cards)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("SIMPLICITY"));
        await Task.CompletedTask;
    }
}
