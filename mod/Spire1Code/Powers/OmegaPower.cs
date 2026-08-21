using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Watcher - Omega. At the end of your turn, deal 50 damage per stack to ALL enemies.</summary>
public class OmegaPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(50, ValueProp.Unpowered)];

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Omega",
            "#At the end of your turn, deal {Amount} times {Damage} damage to ALL enemies.",
            "At the end of your turn, deal damage to ALL enemies.");

    /// <summary>Sets the damage dealt by each Omega stack (50 or 60 upgraded).</summary>
    public void SetDamage(decimal damage)
    {
        AssertMutable();
        DynamicVars.Damage.BaseValue = damage;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner.CombatState.HittableEnemies,
            Amount * DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner, null, null);
    }
}
