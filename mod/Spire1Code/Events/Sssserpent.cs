using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — The Ssssserpent (Liars Game). Agreeing with the serpent first shows its approval, then a
/// Continue button pays 175 Gold and adds a Doubt curse. Disagreeing simply ends the event.
/// </summary>
public class Sssserpent : Spire1Event
{
    private const int _goldReward = 175;

    private const int _a15GoldReward = 150;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "symbiote";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(_goldReward)];

    public override void CalculateVars()
    {
        // StS1: goldReward is 150 at Ascension 15+, else 175.
        DynamicVars["Gold"].BaseValue = Owner.RunState.AscensionLevel >= 15 ? _a15GoldReward : _goldReward;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Agree, HoverTipFactory.FromCardWithCardHoverTips<Doubt>()),
            Option(Disagree),
        ];
    }

    private async Task Agree()
    {
        SetEventState(PageDescription("AGREE"), [Option(Continue, "AGREE")]);
    }

    private async Task Disagree()
    {
        SetEventFinished(PageDescription("DISAGREE"));
    }

    private async Task Continue()
    {
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner);
        await PlayerCmd.GainGold(DynamicVars["Gold"].BaseValue, Owner);
        SetEventFinished(PageDescription("GOLD_RAIN"));
    }
}
