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
/// StS1 shrine — Upgrade Shrine. Pray to upgrade a card (locked when no card is upgradable), or leave.
/// </summary>
public class UpgradeShrine : Spire1Event
{
    protected override string ShippedPortrait => "tinker_time";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool hasUpgradable = PileType.Deck.GetPile(Owner).Cards.Any(c => c.IsUpgradable);
        return
        [
            hasUpgradable
                ? Option(Pray)
                : LockedOption("PRAY_LOCKED"),
            Option(Leave)
        ];
    }

    private async Task Pray()
    {
        CardModel card = (await CardSelectCmd.FromDeckForUpgrade(Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("PRAY"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
