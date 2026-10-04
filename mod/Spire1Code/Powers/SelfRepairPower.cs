using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Entities.Powers;
using Spire1.Spire1Code.Config;

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
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: wind the disabled instance down at its own next relevant hook.
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        await CreatureCmd.Heal(Owner, Amount);
    }
}
