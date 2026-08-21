using BaseLib.Abstracts;
using BaseLib.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Foresight. At the start of your turn, Scry this power's amount.
/// Scry opens a selection screen, so this must run on AfterPlayerTurnStart (the turn-start hook that carries a
/// PlayerChoiceContext); AfterSideTurnStart has none and must not trigger player choices.
/// </summary>
public class ForesightPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Foresight",
            "#At the start of your turn, Scry {Amount}.",
            "At the start of your turn, Scry.");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;
        Flash();
        await ScryCmd.Execute(choiceContext, player, Amount);
    }
}
