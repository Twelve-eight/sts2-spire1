using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Devotion. At the start of your turn, gain Mantra equal to this power's amount.
/// Routed through StanceCmd.GainMantra so the 10-Mantra Divinity conversion (and its remainder) is handled by the
/// shared stance infrastructure. AfterSideTurnStart carries no PlayerChoiceContext, so a ThrowingPlayerChoiceContext
/// is used, exactly like the shipped PlatingPower does for its turn-start power math (no player choice can occur).
/// </summary>
public sealed class DevotionPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Devotion",
            "#At the start of your turn, gain {Amount} *Mantra*.",
            "At the start of your turn, gain Mantra.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player == null || Amount <= 0)
            return;
        Flash();
        await StanceCmd.GainMantra(new ThrowingPlayerChoiceContext(), Owner.Player, Amount, null);
    }
}
