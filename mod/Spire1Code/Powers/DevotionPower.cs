using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Config;

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
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: at the first relevant hook, wind down the disabled instance
            // instead of letting it call into the stance/Mantra pipeline every turn.
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        await StanceCmd.GainMantra(new ThrowingPlayerChoiceContext(), Owner.Player, Amount, null);
    }
}
