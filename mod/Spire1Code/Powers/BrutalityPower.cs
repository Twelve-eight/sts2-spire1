using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Ironclad - Brutality. At the start of your turn, lose 1 HP and draw 1 card (per stack).</summary>
public class BrutalityPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Brutality",
            "#At the start of your turn, lose {Amount} HP and draw {Amount:plural:card|cards}.",
            "At the start of your turn, lose HP and draw a card.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
            return;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        await CreatureCmd.Damage(ctx, Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner);
        await CardPileCmd.Draw(ctx, Amount, Owner.Player, false);
    }
}
