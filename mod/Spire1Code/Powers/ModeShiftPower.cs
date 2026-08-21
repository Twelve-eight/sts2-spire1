using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.ModeShiftPower</c>. Pure readout: the power carries no
/// behaviour of its own in vanilla either — <c>TheGuardian.damage()</c> decrements
/// <c>getPower("Mode Shift").amount</c> by the HP lost and flips the boss to Defensive Mode once
/// its own <c>dmgTaken</c> counter reaches <c>dmgThreshold</c>. The accounting therefore lives in
/// <see cref="Spire1.Spire1Code.Monsters.TheGuardian"/>; this type only shows the remaining
/// damage. Counter stacking matches the vanilla behaviour of re-applying the power at the (raised)
/// threshold when the boss shifts back to Offensive Mode, since the previous instance is removed
/// first.
/// </summary>
public sealed class ModeShiftPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mode Shift",
            "#After taking {Amount} more damage, this enemy switches to a defensive mode.",
            "After taking damage, this enemy switches to a defensive mode.");
}
