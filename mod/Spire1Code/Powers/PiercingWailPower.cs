using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Silent - Piercing Wail's temporary Strength loss.
/// The card stores a POSITIVE amount so the card text reads "lose 6 Strength"; this wrapper inverts it
/// (<see cref="InvertInternalPowerAmount"/>) so the real StrengthPower is applied as -6 and restored
/// when the power expires. Inverting also flips the displayed PowerType to Debuff.
/// </summary>
public class PiercingWailPower : CustomTemporaryPowerModelWrapper<PiercingWail, StrengthPower>
{
    protected override bool InvertInternalPowerAmount => true;
}
