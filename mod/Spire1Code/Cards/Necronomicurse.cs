using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Relics;
using System.Threading.Tasks;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 — Necronomicurse (Curse). Unplayable, and it cannot be got rid of while you still hold the
/// Necronomicon. Granted by <see cref="Necronomicon"/> on pickup.
///
/// Verified against the jar bytecode (com.megacrit.cardcrawl.cards.curses.Necronomicurse): cost -2
/// (unplayable), CURSE type/colour, SPECIAL rarity, target NONE, empty use() and upgrade(). The whole
/// card is two callbacks, and — this is the part that is easy to miss — BOTH are gated on still owning
/// the relic (`AbstractDungeon.player.hasRelic("Necronomicon")`), after which they flash the relic:
///  * onRemoveFromMasterDeck() queues a NecronomicurseEffect holding a brand-new Necronomicurse, which
///    puts a fresh copy back into the master deck. Removing it from your deck therefore accomplishes
///    nothing while the relic is held; melt or lose the relic first.
///  * triggerOnExhaust() queues MakeTempCardInHandAction(makeCopy()), returning a temporary copy to hand
///    for the rest of the combat.
///
/// StS2 mapping (same split as <see cref="Parasite"/>, which documents it at length):
///  * removal raises the awaitable AbstractModel.BeforeCardRemoved hook for every run-state model
///    (CardPileCmd.cs:62, immediately before RemoveFromCurrentPile, so only the removed instance is
///    pulled and a copy added here survives);
///  * transformation does not raise it — the original's only callback is the synchronous
///    CardModel.AfterTransformedFrom();
///  * exhaust raises AbstractModel.AfterCardExhausted (AbstractModel.cs:447).
/// All three are overridden below, so the card is complete; no clause is approximated.
/// </summary>
public class Necronomicurse() : Spire1Curse()
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    /// <summary>
    /// The relic, or null once it has been melted or otherwise lost. StS1 gates both return paths on
    /// still holding it, so losing the Necronomicon is the intended way to be rid of this curse.
    /// </summary>
    private Necronomicon? HoldersRelic => Owner.GetRelic<Necronomicon>();

    /// <summary>Deck-removal path: card-removal services, Fountain of Cleansing, event removals.</summary>
    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (card == this)
            await Returns(PileType.Deck);
    }

    /// <summary>
    /// Combat-exhaust path. StS1 returns a temporary copy to hand, so this uses the generated-card
    /// route rather than touching the run deck.
    /// </summary>
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this)
            return;

        Necronomicon? relic = HoldersRelic;
        if (relic == null)
            return;

        relic.Flash();
        await CardPileCmd.AddGeneratedCardToCombat(Owner.RunState.CreateCard<Necronomicurse>(Owner), PileType.Hand, Owner);
    }

    /// <summary>
    /// Transform path. StS1 funnels transformation through the same onRemoveFromMasterDeck callback, so a
    /// transformed Necronomicurse also comes back. AfterTransformedFrom is synchronous while adding a card
    /// is a command, so the command is started here; a faulted continuation reports failure rather than
    /// letting the task exception go unobserved (identical handling to Parasite).
    /// </summary>
    public override void AfterTransformedFrom()
    {
        base.AfterTransformedFrom();

        Task returning = Returns(PileType.Deck);
        if (returning.IsCompleted)
        {
            returning.GetAwaiter().GetResult();
            return;
        }

        _ = returning.ContinueWith(
            static finished => MainFile.Logger.Error(
                $"Necronomicurse failed to return to the deck after being transformed: {finished.Exception}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task Returns(PileType pile)
    {
        Necronomicon? relic = HoldersRelic;
        if (relic == null)
            return;

        relic.Flash();
        await CardPileCmd.Add(Owner.RunState.CreateCard<Necronomicurse>(Owner), pile);
    }
}
