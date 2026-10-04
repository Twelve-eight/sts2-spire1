using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

public sealed class CalmPower : StancePower
{
    public override PowerType Type => PowerType.Buff;

    public override string StanceName => "Calm";

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Calm",
            "#When Calm ends, gain 2 *Energy*.",
            "When Calm ends, gain Energy.");

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player != null && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            await PlayerCmd.GainEnergy(2m, oldOwner.Player);
        }
    }

    // C12 r5 stale cleanup: an old-save Calm instance must not linger after the powers group is
    // switched off. Next relevant hook = the owner's turn start; removal pays no exit energy.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player && !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            await PowerCmd.Remove(this);
        }
    }
}
