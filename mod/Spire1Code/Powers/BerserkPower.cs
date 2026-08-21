using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Ironclad - Berserk. At the start of your turn, gain 1 Energy.</summary>
public class BerserkPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Berserk",
            "#At the start of your turn, gain {Amount} *Energy*.",
            "At the start of your turn, gain Energy.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
    }
}
