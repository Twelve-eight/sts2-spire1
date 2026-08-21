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
/// StS1 Watcher — Battle Hymn. At the start of each turn, add a Smite into your hand (one per stack).
/// Uses AfterPlayerTurnStart (not AfterSideTurnStart) because adding cards to hand can hand off to a
/// player-choice-driven pile add, and AfterPlayerTurnStart is the hook that carries a PlayerChoiceContext.
/// </summary>
public class BattleHymnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Battle Hymn",
            "#At the start of each turn, add {Amount} *Smite* into your hand.",
            "At the start of each turn, add a Smite into your hand.");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;
        Flash();
        await CardPileCmd.AddToCombatAndPreview<Smite>(Owner, PileType.Hand, Amount, player);
    }
}
