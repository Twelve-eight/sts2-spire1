using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Scrap Ooze. A repeatable dig: each attempt costs HP (3, +1 per failed attempt) and has a
/// chance to find a random relic (starting at 25%, +10% per failed attempt). Leaving ends the event.
/// </summary>
public class ScrapOoze : Spire1Event
{
    private const int _startingChance = 25;

    private const int _chanceIncrement = 10;

    private const int _startingDamage = 3;

    private const int _a15StartingDamage = 5;

    private const int _damageIncrement = 1;

    private int _relicObtainChance = _startingChance;

    private int _damage = _startingDamage;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "trash_heap";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Damage", _startingDamage),
        new IntVar("Chance", _startingChance),
    ];

    public override void CalculateVars()
    {
        // StS1: starting damage is 5 at Ascension 15+, else 3.
        _damage = Owner.RunState.AscensionLevel >= 15 ? _a15StartingDamage : _startingDamage;
        DynamicVars["Damage"].BaseValue = _damage;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(ReachInside).ThatDoesDamage(_damage),
            Option(Leave),
        ];
    }

    private async Task ReachInside()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, _damage,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);

        // StS1: success when miscRng.random(0, 99) >= 99 - relicObtainChance — i.e. probability
        // relicObtainChance/100. The port rolls Rng.NextInt(0, 100) < chance for the exact same odds.
        if (Rng.NextInt(0, 100) < _relicObtainChance)
        {
            var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
            await RelicCmd.Obtain(relic, Owner);
            SetEventFinished(PageDescription("SUCCESS"));
            return;
        }

        _relicObtainChance += _chanceIncrement;
        _damage += _damageIncrement;
        DynamicVars["Chance"].BaseValue = _relicObtainChance;
        DynamicVars["Damage"].BaseValue = _damage;
        SetEventState(PageDescription("FAIL"),
        [
            Option(Deeper, "FAIL").ThatDoesDamage(_damage),
            Option(Leave, "FAIL"),
        ]);
    }

    private async Task Deeper()
    {
        await ReachInside();
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("ESCAPE"));
    }
}
