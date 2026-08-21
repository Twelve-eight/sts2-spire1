using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

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

    public override bool ShouldClearBlock(Creature creature) => Owner != creature;

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
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
