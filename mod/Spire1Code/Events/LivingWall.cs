using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Living Wall. The wall demands a tribute of one card: remove it, transform it, or upgrade
/// it. The Grow choice is locked while no card in the deck is upgradeable.
/// </summary>
public class LivingWall : Spire1Event
{
    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "morphic_grove";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canGrow = PileType.Deck.GetPile(Owner).Cards.Any(c => c.IsUpgradable);
        return
        [
            Option(Forget),
            Option(Change),
            canGrow ? Option(Grow) : LockedOption("LOCKED_GROW"),
        ];
    }

    private async Task Forget()
    {
        var card = (await CardSelectCmd.FromDeckGeneric(Owner,
            new CardSelectorPrefs(L10NLookup($"{Id.Entry}.pages.FORGET.selectionScreenPrompt"), 1),
            c => c.IsRemovable)).FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
        SetEventFinished(PageDescription("RESULT"));
    }

    private async Task Change()
    {
        var card = (await CardSelectCmd.FromDeckForTransformation(Owner,
            new CardSelectorPrefs(L10NLookup($"{Id.Entry}.pages.CHANGE.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (card != null)
        {
            await CardCmd.TransformToRandom(card, Rng);
        }
        SetEventFinished(PageDescription("RESULT"));
    }

    private async Task Grow()
    {
        var card = (await CardSelectCmd.FromDeckForUpgrade(Owner,
            new CardSelectorPrefs(L10NLookup($"{Id.Entry}.pages.GROW.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (card != null)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("RESULT"));
    }
}
