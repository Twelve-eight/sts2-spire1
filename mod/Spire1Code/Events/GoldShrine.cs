using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Golden Shrine. Pray for gold (100 gold, 50 at Ascension 15+), desecrate for 275 gold
/// plus the Regret curse, or leave.
/// </summary>
public class GoldShrine : Spire1Event
{
    private const int _prayGold = 100;

    private const int _a15PrayGold = 50;

    private const int _desecrateGold = 275;

    protected override string ShippedPortrait => "sunken_treasury";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(_prayGold)];

    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = Owner.RunState.AscensionLevel >= 15 ? _a15PrayGold : _prayGold;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Pray),
            Option(Desecrate, HoverTipFactory.FromCardWithCardHoverTips<Regret>()),
            Option(Leave)
        ];
    }

    private async Task Pray()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
        SetEventFinished(PageDescription("PRAY"));
    }

    private async Task Desecrate()
    {
        await PlayerCmd.GainGold(_desecrateGold, Owner);
        await CardPileCmd.AddCurseToDeck<Regret>(Owner);
        SetEventFinished(PageDescription("DESECRATE"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
