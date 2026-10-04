using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

public sealed class MantraPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mantra",
            "#At 10 Mantra, enter Divinity.",
            "At 10 Mantra, enter Divinity.");

    // C12 r5 stale cleanup: Mantra is normally wound down by StanceCmd.GainMantra, but an
    // old-save instance can survive when no further Mantra is granted. Remove it at the next
    // player turn start while the powers group is off.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            await PowerCmd.Remove(this);
        }
    }
}
