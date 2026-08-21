using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — World of Goop (Goop Puddle). Fishing the gold out of the slime costs 11 HP but pays 75
/// Gold; leaving without it costs a random 20-50 Gold (capped at the player's current gold).
/// </summary>
public class GoopPuddle : Spire1Event
{
    private const int _damage = 11;

    private const int _gold = 75;

    private const int _minGoldLoss = 20;

    private const int _maxGoldLoss = 50;

    private const int _a15MinGoldLoss = 35;

    private const int _a15MaxGoldLoss = 75;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "spiraling_whirlpool";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("GoldLoss", 0)];

    public override void CalculateVars()
    {
        // StS1: goldLoss = miscRng.random(20, 50) — 35-75 at Ascension 15+ — clamped to current gold.
        bool ascension15 = Owner.RunState.AscensionLevel >= 15;
        int rolled = Rng.NextInt(ascension15 ? _a15MinGoldLoss : _minGoldLoss, (ascension15 ? _a15MaxGoldLoss : _maxGoldLoss) + 1);
        DynamicVars["GoldLoss"].BaseValue = Math.Min(rolled, Owner.Gold);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(GatherGold).ThatDoesDamage(_damage),
            Option(LeaveIt),
        ];
    }

    private async Task GatherGold()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, _damage,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        await PlayerCmd.GainGold(_gold, Owner);
        SetEventFinished(PageDescription("GATHERED"));
    }

    private async Task LeaveIt()
    {
        await PlayerCmd.LoseGold(DynamicVars["GoldLoss"].BaseValue, Owner);
        SetEventFinished(PageDescription("LEFT"));
    }
}
