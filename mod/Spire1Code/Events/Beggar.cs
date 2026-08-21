using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Old Beggar (Beggar).
/// Give 75 gold to remove a card from your deck (he is secretly a Cleric), or walk away.
/// StS1 constant: GOLD_COST = 75.
/// </summary>
public class Beggar : Spire1Event
{
    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "zen_weaver";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(75)];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> options = [];
        if (Owner.Gold >= 75)
        {
            options.Add(Option(OfferGold));
        }
        else
        {
            options.Add(LockedOption("OFFER_GOLD_LOCKED"));
        }
        options.Add(Option(Leave));
        return options;
    }

    private async Task OfferGold()
    {
        await PlayerCmd.LoseGold(75, Owner, GoldLossType.Spent);
        SetEventState(PageDescription("PURGE"), [Option(Continue, "PURGE")]);
    }

    private async Task Continue()
    {
        CardSelectorPrefs prefs = new(L10NLookup($"{Id.Entry}.pages.PURGE.selectionScreenPrompt"), 1)
        {
            Cancelable = false,
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        foreach (CardModel card in selected)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
        SetEventFinished(PageDescription("POST_PURGE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
