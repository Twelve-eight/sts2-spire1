using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

public sealed class CalmPower : StancePower
{
    public override PowerType Type => PowerType.Buff;

    public override string StanceName => "Calm";

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Calm",
            "#When Calm ends, gain 2 *Energy*.",
            "When Calm ends, gain Energy.");

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player != null)
        {
            await PlayerCmd.GainEnergy(2m, oldOwner.Player);
        }
    }
}
