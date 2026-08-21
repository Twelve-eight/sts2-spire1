using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Establishment. Whenever a card is Retained, reduce its cost by this power's amount this combat.
/// Retention is decided by CombatManager.FlushPlayerHand, which hands the retained-card list to every combat hook
/// listener through AfterFlush; that list is exactly "the cards that were Retained this turn".
/// The reduction uses CardEnergyCost.AddThisCombat, the same combat-scoped cost modifier the shipped Kingly Kick
/// and Up My Sleeve use, so it stacks per retain and expires with the combat.
/// </summary>
public sealed class EstablishmentPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Establishment",
            "#Whenever a card is Retained, reduce its cost by {Amount} this combat.",
            "Whenever a card is Retained, reduce its cost this combat.");

    public override Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player != Owner.Player || Amount <= 0 || retainedCards.Count == 0)
            return Task.CompletedTask;
        Flash();
        foreach (CardModel card in retainedCards)
        {
            card.EnergyCost.AddThisCombat(-Amount);
        }
        return Task.CompletedTask;
    }
}
