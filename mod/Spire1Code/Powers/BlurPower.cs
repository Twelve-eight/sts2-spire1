using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Silent - Blur. Block is not removed at the start of your next turn (one-turn block retention).
/// Mirrors the game's own BlurPower hook pair: ShouldClearBlock prevents the block clear while this power is
/// present, and AfterSideTurnStart consumes one stack (removing the power at 0).
/// </summary>
public class BlurPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Blur",
            "#Block is not removed at the start of your next turn.",
            "Block is not removed at the start of your next turn.");

    public override bool ShouldClearBlock(Creature creature) =>
        !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers) || Owner != creature;

    public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (this == preventer)
        {
            Flash();
        }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: disabled Blur must not keep preventing block clear; remove
            // the stale instance at its own next relevant hook.
            await PowerCmd.Remove(this);
            return;
        }
        await PowerCmd.Decrement(this);
    }
}
