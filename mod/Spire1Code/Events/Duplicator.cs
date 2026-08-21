using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Duplicator. Pray to duplicate a card (any card, curse included, upgrades preserved),
/// or leave.
/// </summary>
public class Duplicator : Spire1Event
{
    protected override string ShippedPortrait => "amalgamator";

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
        // StS1 lets the player duplicate any deck card (no filter, no cancel).
        CardModel card = (await CardSelectCmd.FromDeckGeneric(Owner, new CardSelectorPrefs(new LocString("events", "SPIRE1-DUPLICATOR.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (card != null)
        {
            CardModel copy = Owner.RunState.CloneCard(card);
            await CardPileCmd.Add(copy, PileType.Deck);
        }
        SetEventFinished(PageDescription("DONE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("IGNORE"));
        return Task.CompletedTask;
    }
}
