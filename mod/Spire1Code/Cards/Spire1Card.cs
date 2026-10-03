using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Run;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// Base class for all Ironclad (Spire1) cards. Carries the pool tag so concrete cards need no [Pool].
/// M1: every card uses the shipped placeholder art (card.png); real per-card art is a later wave.
/// </summary>
[Pool(typeof(Spire1CardPool))]
public abstract class Spire1Card(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
    /// <summary>
    /// The card model remains loadable for ModelId and old-save compatibility, but it must not
    /// be selected by combat card generation while the cards content group is disabled.
    /// </summary>
    public override bool CanBeGeneratedInCombat =>
        Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);

    /// <summary>
    /// Prevent modifier-driven card generation from reintroducing a disabled Spire1 card.
    /// </summary>
    public override bool CanBeGeneratedByModifiers =>
        Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);

    /// <summary>
    /// C14 r10 (2026-10-03) total fuse, independent of every Harmony mounted flag.
    /// <para>
    /// ENGINE FACT: LargeCapsule.GetStrikeForCharacter / GetDefendForCharacter select a card from
    /// <c>character.CardPool.AllCards</c> by <c>Rarity == CardRarity.Basic &amp;&amp;
    /// Tags.Contains(CardTag.Strike/Defend)</c>
    /// (see .tmp/dllsrc/MegaCrit.Sts2.Core.Models.Relics/LargeCapsule.cs:38-54). When the cards
    /// content group is closed, a canonical Spire1 Basic Strike/Defend must therefore stop
    /// satisfying that predicate. The engine then finds no match and its own
    /// <c>First</c> throws <see cref="System.InvalidOperationException"/> before any SPIRE1-*
    /// card can be granted - a fail-closed outcome that needs no patch to be installed.
    /// </para>
    /// <para>
    /// Scope is deliberately the canonical pool instances only
    /// (<see cref="MegaCrit.Sts2.Core.Models.AbstractModel.IsCanonical"/>).
    /// <c>CardPoolModel.AllCards</c> resolves its entries through <c>ModelDb.GetById</c> (which
    /// stores the canonical instance created by <c>ModelDb.Init</c>), so this override covers
    /// exactly the set LargeCapsule reads. Old-save deck/combat instances are mutable clones
    /// (<c>CardModel.FromSerializable</c> / <c>ToMutable</c>); they are NOT canonical, so they
    /// keep their Strike/Defend tags and preserve display, upgrade and combat-tag semantics
    /// (PerfectedStrike counts, IsBasicStrikeOrDefend, enchantment and relic filters).
    /// Canonical tag consumers that operate on pool/deck references (Pandora's Box,
    /// GhostSeed, Tezcatara, Spiral, ...) intentionally see no Strike/Defend tag for SPIRE1
    /// cards while the group is closed; that is the same closed-gate degradation the group
    /// already applies to generation and modifiers.
    /// </para>
    /// <para>
    /// The decision reads <see cref="Spire1CardsGateSnapshot.Closed"/> exactly once per
    /// access: that value is published atomically by the config/latch setters (publish-before-store),
    /// so a concurrent master/cards/per-run toggle can no longer produce a torn AND chain, and no
    /// prefix-local boolean is treated as protection for the rest of an engine call.
    /// </para>
    /// <para>
    /// C14 r11 (2026-10-03) total fuse, independent of every Harmony mounted flag: this class also
    /// overrides <see cref="CardModel.ShouldAddToDeck"/>. The engine's deck grant path
    /// (<c>CardPileCmd.Add(..., PileType.Deck)</c>) calls <c>Hook.ShouldAddToDeck</c> and consults
    /// every listener model, so a canonical Spire1 Basic Strike/Defend refuses its own deck grant
    /// while the cards group is closed. LargeCapsule's two grants necessarily pass this check, so
    /// even when every lifecycle patch is missing or drifted the original engine cannot complete
    /// the grant. Open-group and mutable (old-save/combat clone) instances return the base result
    /// untouched.
    /// </para>
    /// <para>
    /// AllCards / AllCardIds are untouched: the pool content, order and ids stay identical, so
    /// SPIRE1-* ModelId resolution for old saves is unaffected. Only the tag predicate result
    /// changes, and only while the group is closed.
    /// </para>
    /// </summary>
    public override IEnumerable<CardTag> Tags
    {
        get
        {
            IEnumerable<CardTag> tags = base.Tags;
            if (!IsCanonical || !Spire1CardsGateSnapshot.Closed)
            {
                return tags;
            }

            // Only Strike/Defend tags matter to the LargeCapsule predicate. Most cards have
            // neither, so the common path stays allocation-free.
            if (!tags.Contains(CardTag.Strike) && !tags.Contains(CardTag.Defend))
            {
                return tags;
            }

            // Filter only the tags LargeCapsule selects on; every other tag is preserved, so
            // PerfectedStrike counts, IsBasicStrikeOrDefend, enchantment and relic filters keep
            // their canonical semantics for the card models themselves.
            return tags.Where(tag => tag != CardTag.Strike && tag != CardTag.Defend).ToArray();
        }
    }

    /// <summary>
    /// C14 r11 (2026-10-03) final deck-grant boundary, independent of every Harmony patch.
    /// <para>
    /// ENGINE FACT: the engine grants deck cards through
    /// <c>CardPileCmd.Add(card, PileType.Deck)</c>. Its deck branch calls
    /// <c>Hook.ShouldAddToDeck(runState, card, out preventer)</c>, which iterates every listener
    /// model (RunState.IterateHookListeners) plus every mod run-state subscriber and asks each one
    /// <c>AbstractModel.ShouldAddToDeck(card)</c>. A false result marks the add as failed, runs
    /// <c>AfterAddToDeckPrevented</c> and never inserts the card into the deck. LargeCapsule's two
    /// grants therefore necessarily pass through this virtual method before any Spire1 Basic card
    /// can land in a deck.
    /// </para>
    /// <para>
    /// The incoming card is checked by argument (<c>card is Spire1Card</c>), not by <c>this</c>:
    /// the newly created card is a mutable clone that is not yet in the run, so it can never be its
    /// own listener. <see cref="Spire1DeckGrantGuard"/> keeps one canonical Spire1 card subscribed
    /// to every run-state hook list so this override is always consulted. While the cards content
    /// group is closed, a Spire1 deck grant is refused even when every lifecycle patch is missing,
    /// drifted or unmounted; other cards (engine cards, other mods) and the open-group path return
    /// the base result untouched, and old-save loading never goes through this hook
    /// (Player.PopulateDeck uses Deck.AddInternal directly).
    /// </para>
    /// </summary>
    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card is Spire1Card && Spire1CardsGateSnapshot.Closed)
        {
            return false;
        }
        return base.ShouldAddToDeck(card);
    }
}

/// <summary>
/// C14 r11 (2026-10-03): keeps a canonical Spire1 card subscribed as a run-state hook listener.
/// <para>
/// ENGINE FACT: <c>Hook.ShouldAddToDeck</c> enumerates the run's existing deck cards, relics,
/// potions, modifiers and <c>ModHelper.IterateAllRunStateSubscribers</c>. A brand-new card that is
/// about to be added is not yet in that set, so its own override cannot be the boundary by itself.
/// This subscription guarantees the <see cref="Spire1Card.ShouldAddToDeck"/> override is present in
/// every run's listener set and inspects the incoming card argument.
/// </para>
/// <para>
/// Subscription is attempted once from the <see cref="Spire1Config"/> static constructor, i.e.
/// during MainFile Phase 1 before any run or pool consumer exists; the representative canonical
/// model is resolved lazily on first hook use (after ModelDb.Init). If resolution ever fails the
/// guard logs an error and yields nothing - the gate is then still enforced by the lifecycle
/// patches and the Tags fuse, and the failure is never reported as protected.
/// </para>
/// </summary>
internal static class Spire1DeckGrantGuard
{
    private const string SubscriptionId = "Spire1.DeckGrantGuard";

    private static readonly object Sync = new();
    private static bool _subscribed;
    private static AbstractModel? _representative;
    private static bool _resolveFailureLogged;

    internal static void EnsureSubscribed()
    {
        if (_subscribed)
        {
            return;
        }
        lock (Sync)
        {
            if (_subscribed)
            {
                return;
            }
            try
            {
                ModHelper.SubscribeForRunStateHooks(SubscriptionId, _ => ResolveRepresentatives());
                _subscribed = true;
                MainFile.Logger.Info(
                    "[Spire1] deck-grant guard subscribed: canonical Spire1 card is a run-state " +
                    "hook listener so Spire1 deck grants can fail closed without any Harmony patch.");
            }
            catch (Exception e)
            {
                MainFile.Logger.Error(
                    "[Spire1] deck-grant guard NOT subscribed: ModHelper.SubscribeForRunStateHooks " +
                    $"threw {e.GetType().Name}: {e.Message}. Spire1 deck grants fall back to the " +
                    "lifecycle patches and the Tags fuse.");
            }
        }
    }

    private static IEnumerable<AbstractModel> ResolveRepresentatives()
    {
        AbstractModel? representative = _representative;
        if (representative is null)
        {
            try
            {
                representative = ModelDb.Card<Strike>();
                _representative = representative;
            }
            catch (Exception e)
            {
                if (!_resolveFailureLogged)
                {
                    _resolveFailureLogged = true;
                    MainFile.Logger.Error(
                        "[Spire1] deck-grant guard could not resolve the canonical SPIRE1-STRIKE " +
                        $"representative ({e.GetType().Name}: {e.Message}); no Spire1 deck-grant " +
                        "listener is present this run.");
                }
                yield break;
            }
        }

        if (representative is not null)
        {
            yield return representative;
        }
    }
}