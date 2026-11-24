using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Discipline. If you end your turn with unused Energy, draw that many additional cards next turn.
/// Energy is only reset at the start of a turn (CombatManager calls PlayerCombatState.ResetEnergy there), so the
/// leftover amount is still readable in the turn-end hook. The extra draw shares the legacy
/// DrawCardNextTurnPower batch with Predator and Doppelganger, after the base hand draw.
/// </summary>
public sealed class DisciplinePower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Discipline",
            "#If you end your turn with unused *Energy*, draw that many additional cards next turn.",
            "If you end your turn with unused Energy, draw that many additional cards next turn.");

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;
        int unused = Owner.Player?.PlayerCombatState?.Energy ?? 0;
        if (unused <= 0)
            return;
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<DrawCardNextTurnPower>(choiceContext, Owner, unused, Owner, null);
    }
}
