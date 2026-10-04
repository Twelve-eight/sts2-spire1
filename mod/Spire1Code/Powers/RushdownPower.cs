using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Rushdown. Whenever you ENTER Wrath, draw cards equal to this power's amount.
/// Only the transition INTO Wrath counts, so leaving Wrath (or moving between other stances) draws nothing.
/// </summary>
public class RushdownPower : CustomPowerModel, IOnStanceChanged
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Rushdown",
            "#Whenever you enter Wrath, draw {Amount} cards.",
            "Whenever you enter Wrath, draw cards.");

    public async Task OnStanceChanged(PlayerChoiceContext ctx, StancePower? from, StancePower? to)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        if (to?.StanceName != "Wrath" || Amount <= 0)
            return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}
