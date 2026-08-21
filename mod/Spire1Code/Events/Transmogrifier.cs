using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Transmogrifier. Pray to transform a card into a random card, or leave.
/// </summary>
public class Transmogrifier : Spire1Event
{
    protected override string ShippedPortrait => "morphic_grove";

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
        CardModel card = (await CardSelectCmd.FromDeckForTransformation(Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            await CardCmd.TransformToRandom(card, Rng, CardPreviewStyle.EventLayout);
        }
        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
