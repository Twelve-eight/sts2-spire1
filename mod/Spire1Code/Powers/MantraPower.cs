using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

public sealed class MantraPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mantra",
            "#At 10 Mantra, enter Divinity.",
            "At 10 Mantra, enter Divinity.");
}
