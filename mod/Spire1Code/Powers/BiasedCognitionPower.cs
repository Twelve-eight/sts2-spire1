using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using Spire1.Spire1Code.Config;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spire1.Spire1Code.Powers;

public class BiasedCognitionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Biased Cognition",
            "#At the start of your turn, lose 1 Focus.",
            "At the start of your turn, lose Focus.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        // C16: the per-turn Focus drain is an effect triggered by this Spire1 power; when the powers
        // group is off, remove the stale instance at this first relevant hook instead of leaving it
        // on the creature forever. Removal does not touch vanilla Focus.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            await PowerCmd.Remove(this);
            return;
        }
        Flash();
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.FocusPower>(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
            Owner,
            -Amount,
            Owner,
            null);
    }
}
