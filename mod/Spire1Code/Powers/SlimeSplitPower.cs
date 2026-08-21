using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
namespace Spire1.Spire1Code.Powers;
/// <summary>
/// StS1 Split power (slimes). While the owner is alive, the combat cannot end — this covers
/// the window where the splitting slime has died but its children have not been added yet.
/// Vanilla equivalent: <c>com.megacrit.cardcrawl.powers.SplitPower</c> on AcidSlime_L /
/// SpikeSlime_L, whose <c>die()</c> also refuses to end the encounter while a
/// SpawnMonsterAction is queued.
/// </summary>
public class SlimeSplitPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    public override bool ShouldStopCombatFromEnding() => true;
    public override List<(string, string)>? Localization =>
        new PowerLoc("Split", "Even at death's door, it divides.", "Even at death's door, it divides.");

}
