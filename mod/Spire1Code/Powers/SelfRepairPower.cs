using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

public class SelfRepairPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Self Repair",
            "#At the end of combat, heal {Amount} HP.",
            "At the end of combat, heal HP.");

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.IsDead)
            return;
        Flash();
        await CreatureCmd.Heal(Owner, Amount);
    }
}
