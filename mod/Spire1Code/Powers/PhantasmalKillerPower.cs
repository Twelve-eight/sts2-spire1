using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent — Phantasmal Killer. Next turn, your Attacks deal double damage.</summary>
public class PhantasmalKillerPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Phantasmal Killer",
            "#Next turn, your Attacks deal double damage.",
            "Next turn, your Attacks deal double damage.");

    // The game's DoubleDamagePower ticks down at the end of the side's turn, so applying it directly mid-turn
    // would expire it the same turn. Deferring the apply to the next turn's start mirrors the game's own
    // ShadowStepPower, giving exactly StS1's "next turn" timing.
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        Flash();
        await PowerCmd.Apply<DoubleDamagePower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}
