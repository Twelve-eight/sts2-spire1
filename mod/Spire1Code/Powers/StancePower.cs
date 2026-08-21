using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

public abstract class StancePower : Spire1Power
{
    public override PowerStackType StackType => PowerStackType.Single;

    public abstract string StanceName { get; }
}
