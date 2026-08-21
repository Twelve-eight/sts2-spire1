using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Big Fish. Three floating treats on strings: the Banana heals 1/3 of Max HP, the Donut
/// grants +5 Max HP, and the Box grants a random relic but adds a Regret curse.
/// </summary>
public class BigFish : Spire1Event
{
    private const int _donutMaxHpGain = 5;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "room_full_of_cheese";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Heal", 0)];

    public override void CalculateVars()
    {
        // StS1: healAmt = AbstractDungeon.player.maxHealth / 3 (integer division).
        DynamicVars["Heal"].BaseValue = Owner.Creature.MaxHp / 3;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(EatBanana),
            Option(EatDonut),
            Option(OpenBox, HoverTipFactory.FromCardWithCardHoverTips<Regret>()),
        ];
    }

    private async Task EatBanana()
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        SetEventFinished(PageDescription("BANANA"));
    }

    private async Task EatDonut()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, _donutMaxHpGain);
        SetEventFinished(PageDescription("DONUT"));
    }

    private async Task OpenBox()
    {
        await CardPileCmd.AddCurseToDeck<Regret>(Owner);
        // StS1: returnRandomRelicTier() + returnRandomScreenlessRelic(tier) — a random relic of a random tier.
        var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("BOX"));
    }
}
