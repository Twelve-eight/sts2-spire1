using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Defect - Electrodynamics power (StS1 <c>ElectroPower</c>, ID "Electro"). A marker
/// power: while attached, <see cref="Spire1.Spire1Code.Patches.ElectrodynamicsLightning"/>
/// turns Lightning passive/evoke damage into an all-enemies hit.
/// <para>
/// <see cref="PowerStackType.Single"/> keeps the amount hidden and at 1. Reapplying the card
/// still runs the full native Given/Received modifier chain; the received clamp in
/// <see cref="Spire1.Spire1Code.Patches.ElectrodynamicsAmount"/> only zeroes the positive
/// increment so repeated plays do not stack, while a final negative offset still removes it.
/// </para>
/// </summary>
public sealed class ElectrodynamicsPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
