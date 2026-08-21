using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Purifier. Pray to remove a card from your deck, or leave.
/// </summary>
public class PurificationShrine : Spire1Event
{
    protected override string ShippedPortrait => "whispering_hollow";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Pray),
            Option(Leave)
        ];
    }

    private async Task Pray()
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
