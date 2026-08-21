using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Mushrooms. Eating the hypnotic mushrooms heals 25% of Max HP. The alternative (Stomp)
/// starts a fight against "The Mushroom Lair", which is not implemented yet (FLAGGED).
/// </summary>
public class Mushrooms : Spire1Event
{
    private const float _healPercent = 0.25f;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "hungry_for_mushrooms";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Heal", 0)];

    public override void CalculateVars()
    {
        // StS1: healAmt = (int)(AbstractDungeon.player.maxHealth * 0.25f).
        DynamicVars["Heal"].BaseValue = (int)(Owner.Creature.MaxHp * _healPercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // FLAGGED: StS1's first option is "[Stomp] Anger the Mushrooms." which starts a fight against
        // "The Mushroom Lair" (with 20-30 gold and the Odd Mushroom relic as rewards). StS1 encounters
        // are not ported yet, so the option is omitted.
        return [Option(Eat)];
    }

    private async Task Eat()
    {
        // StS1: eating heals 25% of Max HP and adds one Parasite curse. Parasite is a mod card
        // (SPIRE1-PARASITE, Spire1Curse + Unplayable + MaxHpVar(3)).
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        await CardPileCmd.AddCurseToDeck<Parasite>(Owner);
        SetEventFinished(PageDescription("HEALED"));
    }
}
