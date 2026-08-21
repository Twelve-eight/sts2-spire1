using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Ominous Forge (<c>com.megacrit.cardcrawl.events.shrines.AccursedBlacksmith</c>).
/// Forge upgrades a card in the deck; Rummage hands over the Warped Tongs relic plus the Pain curse.
///
/// Rummage is a fixed reward, not a roll: <c>buttonEffect</c> case 1 obtains a <c>Pain</c> and then calls
/// <c>spawnRelicAndObtain(new WarpedTongs())</c> — no relic-tier lookup, no already-owned check and no
/// Circlet fallback — and takes nothing in return beyond the curse.
/// </summary>
public class AccursedBlacksmith : Spire1Event
{
    protected override string ShippedPortrait => "tinker_time";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool hasUpgradable = PileType.Deck.GetPile(Owner).Cards.Any(c => c.IsUpgradable);
        // StS1 shows both rewards on the option itself:
        // setDialogOption(OPTIONS[1], new Pain(), new WarpedTongs()) — card preview first, then the relic.
        var rummageTips = HoverTipFactory.FromCardWithCardHoverTips<Pain>()
            .Concat(HoverTipFactory.FromRelic<WarpedTongs>());
        return
        [
            hasUpgradable
                ? Option(Forge)
                : LockedOption("FORGE_LOCKED"),
            Option(Rummage, rummageTips),
            Option(Leave)
        ];
    }

    private async Task Forge()
    {
        CardModel card = (await CardSelectCmd.FromDeckForUpgrade(Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("FORGE"));
    }

    private async Task Rummage()
    {
        // StS1 buttonEffect case 1 obtains the Pain curse first and only then spawns the relic, so a
        // mid-flow failure cannot hand out the relic for free.
        await CardPileCmd.AddCurseToDeck<Pain>(Owner);
        await RelicCmd.Obtain<WarpedTongs>(Owner);
        SetEventFinished(PageDescription("RUMMAGE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
