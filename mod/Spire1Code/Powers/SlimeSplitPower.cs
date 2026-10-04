using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Config;
namespace Spire1.Spire1Code.Powers;
/// <summary>
/// StS1 Split power (slimes). While the owner is alive, the combat cannot end - this covers
/// the window where the splitting slime has died but its children have not been added yet.
/// Vanilla equivalent: <c>com.megacrit.cardcrawl.powers.SplitPower</c> on AcidSlime_L /
/// SpikeSlime_L, whose <c>die()</c> also refuses to end the encounter while a
/// SpawnMonsterAction is queued.
/// <para>
/// r6 (2026-10-03): this predicate is gated on the powers content group. It is a pure query
/// evaluated while <c>Hook.ShouldStopCombatFromEnding</c> iterates combat hook listeners, so it
/// must not remove the power or mutate combat state. With the group off it returns false so a
/// stale old-save instance can no longer hold combat open (same shape as
/// <see cref="SporeCloudPower.ShouldStopCombatFromEnding"/>); with the group on the original
/// safety valve is unchanged. Repeated calls are idempotent: the method reads one atomic config
/// value and holds no state.
/// </para>
/// </summary>
public class SlimeSplitPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    public override bool ShouldStopCombatFromEnding() =>
        Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers);
    public override List<(string, string)>? Localization =>
        new PowerLoc("Split", "Even at death's door, it divides.", "Even at death's door, it divides.");

}
