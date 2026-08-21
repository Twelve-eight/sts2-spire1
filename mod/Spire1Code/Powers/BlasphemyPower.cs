using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Blasphemy. The owner dies at the start of their next turn.
/// The death goes through CreatureCmd.Kill with force=false, which is the game's normal death path
/// (it drops HP through LoseHpInternal and still runs BeforeDeath/ShouldDie, so death-prevention effects apply).
/// No HP field is written directly. The power is intentionally NOT removed after firing: if the death is prevented,
/// vanilla keeps the effect and it triggers again on the following turn.
/// </summary>
public sealed class BlasphemyPower : Spire1Power
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Blasphemy",
            "#Die at the start of your next turn.",
            "Die at the start of your next turn.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Owner.IsDead)
            return;
        Flash();
        await CreatureCmd.Kill(Owner);
    }
}
