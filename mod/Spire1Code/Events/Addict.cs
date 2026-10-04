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
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City - Pleading Vagrant (Addict).
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
        // The Shame hover tip would advertise a card the cards gate refuses to grant, so it is only
        // attached while the cards content group is on; the option itself stays available because Rob
        // still hands over its random relic reward (C13, 2026-10-02).
        options.Add(Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards)
            ? Option(Rob, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Shame>().ToArray())
            : Option(Rob, "INITIAL"));
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
        // Cards content group gate (C13, 2026-10-02): Shame is a Spire1 curse created directly by
        // CardPileCmd.AddCurseToDeck, so Spire1Curse.CanBeGenerated* cannot stop this grant. Fail closed
        // before any card is constructed, then continue with the unchanged relic reward so the event's
        // order and its ROB page are preserved. A stale page or a direct debug invocation lands here too.
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            await CardPileCmd.AddCurseToDeck<Shame>(Owner);
        }
        else
        {
            MainFile.Logger.Warn("[Spire1] Addict Rob invoked while cards content group is off; Shame grant skipped");
        }
        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        SetEventFinished(PageDescription("ROB"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("INITIAL"));
        return Task.CompletedTask;
    }
}
