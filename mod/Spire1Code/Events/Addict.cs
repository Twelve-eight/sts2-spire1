using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Pleading Vagrant (Addict).
/// Offer 85 gold for a random relic, rob him (Shame curse + random relic), or leave.
/// StS1 constants: GOLD_COST = 85.
/// </summary>
public class Addict : Spire1Event
{
    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "ranwid_the_elder";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(85)];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> options = [];
        if (Owner.Gold >= 85)
        {
            options.Add(Option(OfferGold));
        }
        else
        {
            options.Add(LockedOption("OFFER_GOLD_LOCKED"));
        }
        options.Add(Option(Rob, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Shame>().ToArray()));
        options.Add(Option(Leave));
        return options;
    }

    private async Task OfferGold()
    {
        await PlayerCmd.LoseGold(85, Owner, GoldLossType.Spent);
        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        SetEventFinished(PageDescription("OFFER"));
    }

    private async Task Rob()
    {
        await CardPileCmd.AddCurseToDeck<Shame>(Owner);
        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        SetEventFinished(PageDescription("ROB"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("INITIAL"));
        return Task.CompletedTask;
    }
}
