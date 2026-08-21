using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Deva Form. At the start of your turn, gain Energy, then increase that gain by this power's amount.
/// The escalating gain lives in the power's own EnergyVar (not a private field) so the tooltip always shows the
/// current value; Amount is the per-turn increase, so two copies of Deva Form escalate twice as fast, as in vanilla.
/// </summary>
public sealed class DevaFormPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(0)];

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Deva Form",
            "#At the start of your turn, gain {Energy} *Energy*, then increase this gain by {Amount}.",
            "At the start of your turn, gain Energy, then increase this gain.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.Player == null)
            return;
        Flash();
        DynamicVars.Energy.BaseValue += Amount;
        InvokeDisplayAmountChanged();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner.Player);
    }
}
