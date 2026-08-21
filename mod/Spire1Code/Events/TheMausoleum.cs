using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — The Mausoleum.
/// Open the coffin for a random relic; 50% of the time you also gain the Writhe curse
/// (100% at Ascension 15+).
///
/// Writhe is the card StS2 already ships, reused rather than reimplemented (see OpenCoffin).
/// StS1 constants: PERCENT = 50 (A_2: 100).
/// </summary>
public class TheMausoleum : Spire1Event
{
    private const string _percentKey = "Percent";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "grave_of_the_forgotten";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(_percentKey, 50)];

    public override void CalculateVars()
    {
        DynamicVars[_percentKey].BaseValue = Owner.RunState.AscensionLevel >= 15 ? 100 : 50;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(OpenCoffin),
            Option(Leave),
        ];
    }

    private async Task OpenCoffin()
    {
        // StS1: Random.randomBoolean() (50/50), forced true at Ascension 15+.
        bool cursed = Owner.RunState.AscensionLevel >= 15 || Rng.NextBool();
        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        if (cursed)
        {
            // Writhe is not reimplemented here: StS2 ships an identical one
            // (MegaCrit.Sts2.Core.Models.Cards.Writhe, cost -1 Curse, Innate + Unplayable,
            // MaxUpgradeLevel 0) and it is already registered in CurseCardPool, so per the
            // lean-code rule we grant the shipped card.
            await CardPileCmd.AddCurseToDeck<Writhe>(Owner);
            SetEventFinished(PageDescription("CURSED"));
        }
        else
        {
            SetEventFinished(PageDescription("NORMAL"));
        }
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("NOPE"));
        return Task.CompletedTask;
    }
}
