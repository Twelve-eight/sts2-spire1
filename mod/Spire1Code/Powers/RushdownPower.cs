using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Rushdown. Whenever you ENTER Wrath, draw cards equal to this power's amount.
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
        if (to is not WrathPower || Amount <= 0)
            return;
        Flash();
        await CardPileCmd.Draw(ctx, Amount, Owner.Player);
    }
}
