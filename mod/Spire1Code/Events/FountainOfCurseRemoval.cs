using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — The Divine Fountain. Drink to remove every curse from the deck (Ascender's Bane is
/// kept, exactly as in StS1, which also kept Curse of the Bell and Necronomicurse — neither of those
/// two cards exists in this mod), or leave.
/// </summary>
public class FountainOfCurseRemoval : Spire1Event
{
    protected override string ShippedPortrait => "wellspring";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Drink),
            Option(Leave)
        ];
    }

    private async Task Drink()
    {
        // StS1 removes every deck card of type CURSE except the unremovable ones (AscendersBane,
        // Curse of the Bell, Necronomicurse — the latter two do not exist in this mod). The Eternal
        // keyword is the StS2 equivalent of "cannot be removed".
        List<CardModel> curses = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Curse && c.IsRemovable)
            .ToList();
        await CardPileCmd.RemoveFromDeck(curses);
        SetEventFinished(PageDescription("DRINK"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
