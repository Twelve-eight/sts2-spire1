using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Watcher - Mark. Pressure Points deals HP loss equal to this persistent counter.</summary>
public sealed class MarkPower : Spire1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mark",
            "#Pressure Points deals damage equal to this amount.",
            "Pressure Points deals damage equal to this amount.");
}
