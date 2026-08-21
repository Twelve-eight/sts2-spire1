using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Watcher — Like Water. At the end of your turn, if you are in Calm, gain Amount Block.</summary>
public class LikeWaterPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Like Water",
            "#At the end of your turn, if you are in *Calm*, gain {Amount} *Block*.",
            "At the end of your turn, if you are in Calm, gain Block.");

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        Player? owner = Owner.Player;
        if (owner == null || !participants.Contains(Owner) || Amount <= 0)
            return;
        if (!StanceCmd.IsIn<CalmPower>(owner))
            return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
