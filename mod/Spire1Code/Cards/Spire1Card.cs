using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Run;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

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
        !Spire1PowersGate.ContentUnavailableActive
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);

    /// <summary>
    /// Prevent modifier-driven card generation from reintroducing a disabled Spire1 card.
    /// </summary>
    public override bool CanBeGeneratedByModifiers =>
        !Spire1PowersGate.ContentUnavailableActive
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);

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
            // r8d: 独立不可用状态优先, 不读取 Spire1Config (静态构造失败时不得把异常传播到引擎).
            if (!IsCanonical || (!Spire1PowersGate.ContentUnavailableActive && !Spire1CardsGateSnapshot.Closed))
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
        if (card is Spire1Card && (Spire1PowersGate.ContentUnavailableActive || Spire1CardsGateSnapshot.Closed))
        {
            return false;
        }
        return base.ShouldAddToDeck(card);
    }
}

/// <summary>
/// C14 r11/r14 (2026-10-04): keeps a canonical Spire1 card subscribed as a run-state hook
/// listener.
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
/// <para>
/// C14 r14: the engine silently ignores a duplicate subscription id (ModHelper.cs:100-112), so
/// the probe must not treat "an item with the same id exists" as success. The subscription now
/// uses the named callback <see cref="ConfirmRepresentatives"/>, and the read-only probe verifies
/// the stored delegate's Method, declaring type, static target and signature are exactly this
/// mod's callback. A same-id subscriber owned by another mod, a missing delegate field, a type
/// drift or any reflection failure stays unconfirmed and the closed-gate path fails closed.
/// </para>
/// </summary>
internal static class Spire1DeckGrantGuard
{
    private const string SubscriptionId = "Spire1.DeckGrantGuard";

    private static readonly object Sync = new();
    private static bool _subscribed;
    private static bool _failureLogged;
    private static AbstractModel? _representative;
    private static bool _resolveFailureLogged;
    private static FieldInfo? _subscribersField;
    private static bool _subscribersFieldResolved;

    /// <summary>
    /// C14 r12: the registration is only reported as successful after the engine subscriber list
    /// has been probed and this mod's named callback is actually present. The engine's
    /// SubscribeForRunStateHooks silently ignores a duplicate id (it logs and returns without a
    /// result), so a normal return is NOT proof of registration. While this is false the
    /// lifecycle proof treats the closed-gate replacement as incomplete and refuses the grant.
    /// </summary>
    internal static bool IsSubscriberConfirmed
    {
        get
        {
            if (TryProbeSubscriberPresent())
            {
                return true;
            }

            // The engine list only changes through ModHelper itself; attempt one idempotent
            // re-subscribe before declaring the boundary unavailable.
            EnsureSubscribed();
            return TryProbeSubscriberPresent();
        }
    }

    internal static void EnsureSubscribed()
    {
        if (_subscribed && TryProbeSubscriberPresent())
        {
            return;
        }

        lock (Sync)
        {
            if (_subscribed && TryProbeSubscriberPresent())
            {
                return;
            }

            // Retry a bounded number of times: a duplicate id is ignored by the engine, so
            // re-issuing the same id is idempotent and cannot create a second listener.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    // C14 r14: a named callback (not a lambda) gives the probe a stable identity
                    // to verify; the delegate still returns exactly the canonical representative.
                    ModHelper.SubscribeForRunStateHooks(SubscriptionId, ConfirmRepresentatives);
                }
                catch (Exception e)
                {
                    if (!_failureLogged)
                    {
                        _failureLogged = true;
                        MainFile.Logger.Error(
                            "[Spire1] deck-grant guard subscription threw " +
                            $"{e.GetType().Name}: {e.Message}; the canonical listener is not proven present.");
                    }
                }

                if (TryProbeSubscriberPresent())
                {
                    _subscribed = true;
                    _failureLogged = false;
                    MainFile.Logger.Info(
                        "[Spire1] deck-grant guard confirmed: this mod's named callback is present " +
                        "in ModHelper run-state hook subscribers.");
                    return;
                }
            }

            _subscribed = false;
            if (!_failureLogged)
            {
                _failureLogged = true;
                MainFile.Logger.Error(
                    "[Spire1] deck-grant guard NOT confirmed: no subscriber with id " +
                    $"'{SubscriptionId}' and this mod's callback delegate was found in " +
                    "ModHelper._runHookSubscribers after retries. " +
                    "Closed-gate LargeCapsule grants fail closed until the subscription is confirmed.");
            }
        }
    }

    /// <summary>
    /// C14 r14: the single named run-state callback this mod subscribes. Kept as a method group
    /// so the stored delegate can be identified by Method/DeclaringType/Target/signature; the
    /// engine only passes the RunState through, and the representative resolution below stays
    /// the same canonical-model lookup as before.
    /// </summary>
    private static IEnumerable<AbstractModel> ConfirmRepresentatives(RunState runState)
        => ResolveRepresentatives();

    /// <summary>
    /// Read-only probe of the engine subscriber list. Returns true only when the list field can
    /// be resolved, an item exposes the exact subscription id, AND that item's delegate is this
    /// mod's named callback (Method, declaring type, static target, return type and single
    /// RunState parameter all verified). A same-id item owned by another mod, a missing or
    /// drifted delegate field, or any reflection failure returns false (fail-closed), never true.
    /// </summary>
    private static bool TryProbeSubscriberPresent()
    {
        try
        {
            FieldInfo? field = ResolveSubscribersField();
            if (field is null)
            {
                return false;
            }

            object? listValue = field.GetValue(null);
            if (listValue is not IEnumerable subscribers)
            {
                return false;
            }

            foreach (object? item in subscribers)
            {
                if (item is null)
                {
                    continue;
                }

                Type itemType = item.GetType();
                FieldInfo? idField = itemType.GetField(
                    "id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (idField is null || idField.FieldType != typeof(string))
                {
                    continue;
                }

                if (idField.GetValue(item) is not string id || id != SubscriptionId)
                {
                    continue;
                }

                // C14 r14: the id matches, but that alone does not prove this mod owns the
                // subscriber (ModHelper ignores a duplicate id). The delegate must be present,
                // have the exact engine delegate type and be this mod's named callback.
                FieldInfo? delField = itemType.GetField(
                    "del", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (delField is null || delField.FieldType != typeof(RunHookSubscriptionDelegate))
                {
                    return false;
                }

                if (delField.GetValue(item) is not RunHookSubscriptionDelegate subscriberDelegate)
                {
                    return false;
                }

                return IsThisModCallback(subscriberDelegate);
            }

            return false;
        }
        catch (Exception)
        {
            // A probe failure is an unconfirmed state, never a success.
            return false;
        }
    }

    /// <summary>
    /// C14 r14: verifies a stored delegate is exactly this mod's named callback. A delegate from
    /// another mod (even with the same subscription id) fails one of these checks and is never
    /// reported as confirmed; any reflection failure returns false.
    /// </summary>
    private static bool IsThisModCallback(RunHookSubscriptionDelegate subscriberDelegate)
    {
        try
        {
            MethodInfo? method = subscriberDelegate.Method;
            if (method is null
                || !method.IsStatic
                || subscriberDelegate.Target is not null
                || method.DeclaringType != typeof(Spire1DeckGrantGuard)
                || method.Name != nameof(ConfirmRepresentatives)
                || method.IsGenericMethod
                || method.ReturnType != typeof(IEnumerable<AbstractModel>))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 1
                   && parameters[0].ParameterType == typeof(RunState);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static FieldInfo? ResolveSubscribersField()
    {
        if (_subscribersFieldResolved)
        {
            return _subscribersField;
        }

        lock (Sync)
        {
            if (_subscribersFieldResolved)
            {
                return _subscribersField;
            }

            _subscribersFieldResolved = true;
            try
            {
                FieldInfo? exact = typeof(ModHelper).GetField(
                    "_runHookSubscribers",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (exact is not null && exact.FieldType.IsGenericType)
                {
                    _subscribersField = exact;
                    return _subscribersField;
                }

                // Shape fallback: any static generic collection field whose element type
                // exposes a string id field. Keeps the probe working if the engine only
                // renames the private list field.
                foreach (FieldInfo candidate in typeof(ModHelper).GetFields(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Type type = candidate.FieldType;
                    if (!type.IsGenericType)
                    {
                        continue;
                    }

                    Type[] arguments = type.GetGenericArguments();
                    if (arguments.Length != 1)
                    {
                        continue;
                    }

                    FieldInfo? idField = arguments[0].GetField(
                        "id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (idField is not null && idField.FieldType == typeof(string))
                    {
                        _subscribersField = candidate;
                        return _subscribersField;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
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
