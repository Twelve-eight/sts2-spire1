using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Collect. At the start of each of your next Amount turns, put an upgraded Miracle into your
/// hand and tick one turn off the counter. The counter is set by Collect to its X value (+1 when upgraded).
/// </summary>
public class CollectPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Collect",
            "#At the start of your next {Amount} turns, put a *Miracle+* into your hand.",
            "At the start of your turn, put an upgraded Miracle into your hand.");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;
        CardModel? miracle = Owner.CombatState?.CreateCard<Miracle>(player);
        if (miracle == null)
            return;
        Flash();
        CardCmd.Upgrade(miracle);
        await CardPileCmd.AddGeneratedCardToCombat(miracle, PileType.Hand, player);
        await PowerCmd.Decrement(this);
    }
}
