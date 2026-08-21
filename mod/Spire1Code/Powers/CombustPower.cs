using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Ironclad - Combust. At the end of your turn, lose 1 HP (per stack) and deal 5 damage to ALL enemies (per stack).</summary>
public class CombustPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Unpowered)];

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Combust",
            "#At the end of your turn, lose {Amount} HP and deal {Amount} times {Damage} damage to ALL enemies.",
            "At the end of your turn, lose HP and deal damage to ALL enemies.");

    /// <summary>Set the damage this power deals per stack (the card's damage value, 5 or 7 upgraded).</summary>
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
        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner);
        await CreatureCmd.Damage(choiceContext, Owner.CombatState.HittableEnemies, Amount * DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner);
    }
}
