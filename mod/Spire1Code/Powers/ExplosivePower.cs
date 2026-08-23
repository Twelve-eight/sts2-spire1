using MegaCrit.Sts2.Core.Entities.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 — Explosive (Exploder / gas_bomb countdown, <c>com.megacrit.cardcrawl.powers.ExplosivePower</c>).
/// Applied with 3 stacks in Exploder's usePreBattleAction. At the start of the owner's (enemy-side)
/// turn the counter ticks down; at 0 it deals 30 damage to every player and then the owner dies.
/// <para>
/// The 30 damage keeps StS1's THORNS-type semantics (<c>DamageInfo.createDamageMatrix(30, true)</c>):
/// blockable but power-immune, i.e. ValueProp.Unpowered (DamageProps.nonCardUnpowered). AfterSideTurnStart
/// carries no PlayerChoiceContext, so a ThrowingPlayerChoiceContext is used (DevotionPower pattern), and the
/// player targets come from CombatState.Players since Owner.Player is null enemy-side (SporeCloudPower pattern).
/// The suicide goes through CreatureCmd.Kill — the game's normal death path (SlimeBoss / BlasphemyPower
/// pattern; it drops HP and still runs BeforeDeath/ShouldDie) — standing in for StS1's LoseHPAction.
/// </para>
/// </summary>
public class ExplosivePower : CustomPowerModel
{
    public const decimal ExplodeDamage = 30m;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Explosive",
            "Explodes after {Amount} turns, dealing 30 damage.",
            "Explodes after {Amount} turns, dealing 30 damage.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Enemy || !participants.Contains(Owner) || Owner.IsDead)
            return;
        await PowerCmd.Decrement(this);
        if (Amount > 0)
            return;
        Flash();
        foreach (Player player in CombatState.Players)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), player.Creature, ExplodeDamage,
                ValueProp.Unpowered, Owner, null, null);
        }
        await CreatureCmd.Kill(Owner);
    }
}
