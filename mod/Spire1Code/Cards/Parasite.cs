using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 — Parasite (Curse). Unplayable. If transformed or removed from your deck, lose 3 Max HP.
/// Added by the Mushrooms event (mod/Spire1Code/Events/Mushrooms.cs).
///
/// Verified against the jar bytecode (com.megacrit.cardcrawl.cards.curses.Parasite): cost -2 (unplayable),
/// CURSE type/colour/rarity, target NONE, use() is empty, upgrade() is empty, and the whole card is the
/// single override onRemoveFromMasterDeck() -&gt; AbstractDungeon.player.decreaseMaxHealth(3).
///
/// StS1 funnels both "removed" and "transformed" through that one callback. StS2 splits them:
///  * removal goes through CardPileCmd.RemoveFromDeck, which raises the awaitable
///    AbstractModel.BeforeCardRemoved hook for every run-state model — including the deck cards themselves
///    (.tmp/dllsrc/MegaCrit.Sts2.Core.Commands/CardPileCmd.cs:62 and
///    .tmp/dllsrc/MegaCrit.Sts2.Core.Runs/RunState.cs:554, which puts every deck card in the listener list).
///    The shipped SpoilsMap card overrides the same hook with the same `card != this` filter.
///  * transformation does NOT raise it: CardCmd.Transform yanks the original out of its pile directly and the
///    only callback the original still gets is the synchronous CardModel.AfterTransformedFrom()
///    (.tmp/dllsrc/MegaCrit.Sts2.Core.Commands/CardCmd.cs:404 and :451).
/// Both are overridden below, so the clause is fully implemented; there is no missing API.
///
/// isFromCard is false because StS1's decreaseMaxHealth() is a plain max-HP reduction, not card damage, so
/// StS2 effects that react to card-sourced HP loss (Rupture and friends) must not fire — the same choice the
/// mod's events already make for CreatureCmd.LoseMaxHp.
/// </summary>
public class Parasite() : Spire1Curse()
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(3)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    /// <summary>Removal path: card-removal services, Fountain of Cleansing, event card removals, etc.</summary>
    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (card == this)
            await LoseMaxHp();
    }

    /// <summary>
    /// Transform path. AfterTransformedFrom is synchronous while max-HP loss is a command, so the command is
    /// started here; if it has not already finished, a faulted continuation reports it instead of letting the
    /// task exception go unobserved.
    /// </summary>
    public override void AfterTransformedFrom()
    {
        base.AfterTransformedFrom();

        Task loss = LoseMaxHp();
        if (loss.IsCompleted)
        {
            loss.GetAwaiter().GetResult();
            return;
        }

        _ = loss.ContinueWith(
            static finished => MainFile.Logger.Error(
                $"Parasite max-HP loss on transform failed: {finished.Exception}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task LoseMaxHp() => await CreatureCmd.LoseMaxHp(
        new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.MaxHp.BaseValue, isFromCard: false);
}
