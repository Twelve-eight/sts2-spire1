using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — The Moai Head. Jumping inside heals you to full HP at the cost of max HP
/// (12.5% of max HP; the Ascension 15+ variant of 18% is not applied because StS2's ascension levels
/// do not map 1:1 onto StS1's numbered ladder). Feeding the statue the Golden Idol smashes it for
/// 333 Gold; StS1 shows that slot as "[Locked] Requires: Golden Idol." while the relic is not held.
/// </summary>
public class MoaiHead : Spire1Event
{
    /// <summary>StS1 <c>MoaiHead.goldAmount = 333</c>, inlined at all three of its bytecode sites: the
    /// metric log, the RainingGoldEffect and the gainGold call.</summary>
    private const int _goldAmount = 333;

    protected override string ShippedPortrait => "sunken_statue";

    public override ActModel[] Acts => Act3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HpLoss", 0m)];

    public override void CalculateVars()
    {
        // StS1: MathUtils.round(maxHealth * HP_LOSS_PERCENT), HP_LOSS_PERCENT = 0.125f (0.18f at Ascension 15+).
        DynamicVars["HpLoss"].BaseValue = Math.Round(Owner.Creature.MaxHp * 0.125m, MidpointRounding.AwayFromZero);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO: "[Jump Inside]", the idol slot, "[Leave]". The idol slot is always shown; only its
        // text and clickability change. Holding the relic gives
        // setDialogOption(OPTIONS[2], !hasRelic("Golden Idol")) — the second argument is `disabled`, so a
        // held idol makes the option live — and otherwise setDialogOption(OPTIONS[3], true), OPTIONS[3]
        // being "[Locked] Requires: Golden Idol.".
        return
        [
            Option(JumpInside),
            Owner.GetRelic<GoldenIdol>() != null ? Option(OfferIdol) : LockedOption("OFFER_IDOL_LOCKED"),
            Option(Leave),
        ];
    }

    private async Task JumpInside()
    {
        // StS1: maxHealth -= hpAmt; clamp current HP to the new max (min max 1); heal to full.
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["HpLoss"].BaseValue, isFromCard: false);
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        SetEventFinished(PageDescription("JUMP"));
    }

    private async Task OfferIdol()
    {
        // StS1: loseRelic("Golden Idol") first, then gainGold(333) behind a RainingGoldEffect(333).
        // Remove, not Melt: StS1's loseRelic drops the relic from the list outright, which is what
        // Vampires.cs:88 already uses for offering up the Blood Vial.
        GoldenIdol? idol = Owner.GetRelic<GoldenIdol>();
        if (idol != null)
        {
            await RelicCmd.Remove(idol);
        }
        await PlayerCmd.GainGold(_goldAmount, Owner);
        SetEventFinished(PageDescription("OFFER"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
