using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Run;
using Spire1Ironclad = Spire1.Spire1Code.Character.Ironclad;
using Spire1Silent = Spire1.Spire1Code.Character.Silent;
using Spire1Defect = Spire1.Spire1Code.Character.Defect;
using EngineIroncladCardPool = MegaCrit.Sts2.Core.Models.CardPools.IroncladCardPool;
using EngineSilentCardPool = MegaCrit.Sts2.Core.Models.CardPools.SilentCardPool;
using EngineDefectCardPool = MegaCrit.Sts2.Core.Models.CardPools.DefectCardPool;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// C14 (2026-10-02): closes the LargeCapsule bypass around the Spire1 cards content gate.
/// <para>
/// ENGINE FACT: LargeCapsule.AfterObtained adds one Basic Strike and one Basic Defend to the
/// deck through the private helpers GetStrikeForCharacter / GetDefendForCharacter, which read
/// character.CardPool.AllCards directly and bypass CardPoolModel.GetUnlockedCards /
/// FilterThroughEpochs (see .tmp/dllsrc/MegaCrit.Sts2.Core.Models.Relics/LargeCapsule.cs:23-53).
/// </para>
/// <para>
/// LAYERS (Harmony 2.4.2 applies only the last method-level target of a multi-target class, so
/// every layer below is a single-target patch class):
/// 1) Spire1LargeCapsuleAfterObtainedPatch: prefix on the public virtual
///    LargeCapsule.AfterObtained. Primary layer; while mounted it replaces the whole engine
///    grant for the three Spire1 placeholder characters when the cards group is closed, so the
///    private helper names can never leak AllCards content.
/// 2) Spire1LargeCapsuleObtainFunnelPatch: prefix on RelicCmd.Obtain(RelicModel, Player, int).
///    Fail-closed backstop: while the cards group is closed, if the primary AfterObtained layer
///    is NOT proven mounted (target missing, ambiguous signature, installation failure), the
///    LargeCapsule obtain for a Spire1 placeholder character is explicitly blocked with an
///    Error log and a faulted task. It never falls through to the engine AllCards grant.
/// 3) Spire1LargeCapsuleStrikeHelperPatch / Spire1LargeCapsuleDefendHelperPatch: one class per
///    private helper, each with exactly one target. They substitute the engine-equivalent Basic
///    card while the group is closed and keep a sibling-mount check: when both the primary and
///    the funnel layer are unmounted and the sibling helper is also unmounted, the mounted
///    helper aborts the grant instead of completing the engine AllCards read.
/// 4) Spire1LargeCapsuleFinalizeStartingRelicsPatch: prefix on the public
///    RunManager.FinalizeStartingRelics() instance method. Last line for the new-run path
///    Player.PopulateStartingRelics -> AddRelicInternal -> player.Relics, which never goes
///    through RelicCmd.Obtain; while the cards group is closed, a Spire1 placeholder player
///    already holding a LargeCapsule is refused with a faulted task before the engine body can
///    call relic.AfterObtained() directly.
/// 5) Spire1LargeCapsuleAfterObtainedSafetyPatch: independent maximum-priority prefix on the
///    same LargeCapsule.AfterObtained method, with its own Prepare/Cleanup and mount state. It
///    is the shared fail-closed boundary that does not depend on the AddRelicInternal or the
///    FinalizeStartingRelics guard being mounted: while the cards group is closed, a Spire1
///    placeholder LargeCapsule whose complete replacement is not proven is refused before the
///    engine body can perform its first AllCards read.
/// 6) Spire1LargeCapsuleLifecycleFinalizeGatePatch / Spire1LargeCapsuleObtainLifecycleGatePatch:
///    independent lifecycle boundaries on RunManager.FinalizeStartingRelics() and
///    RelicCmd.Obtain(RelicModel, Player, int). They do not target LargeCapsule.AfterObtained,
///    so they still install and still refuse the two call chains even when the r7 safety target
///    is missing, ambiguous, drifted, its Prepare returns false or its installation throws. While
///    the cards group is closed and no independent replacement proof is available (the r6
///    complete replacement), a Spire1 placeholder that holds or tries to obtain a LargeCapsule
///    is refused with a faulted task before the engine body runs.
/// 7) Spire1Card.Tags total fuse (Cards/Spire1Card.cs): a model-level override that removes the
///    Strike/Defend tags from canonical Spire1 cards while the atomic Cards gate is closed. It
///    is not a Harmony patch and therefore remains effective even if every layer above fails to
///    install. LargeCapsule's own AllCards predicate then cannot select a SPIRE1-* Basic card.
/// </para>
/// <para>
/// BEHAVIOR: when the cards group is open (master + group + this-run snapshot), every layer
/// returns true and the engine method runs byte-for-byte. When the group is closed, only the
/// three Spire1 placeholder characters are intercepted; they receive the engine-equivalent
/// Basic Strike/Defend of their placeholder identity (Ironclad / Silent / Defect), never a
/// SPIRE1-* card. Vanilla characters and other mods' characters are never touched, even with
/// the group closed.
/// </para>
/// <para>
/// FAIL-CLOSED: the Spire1Card.Tags total fuse (layer 7) keeps the engine AllCards predicate
/// from selecting any SPIRE1-* Basic Strike/Defend while the cards group is closed, with or
/// without any mounted layer. The AfterObtained fallback aborts the grant (logs one Error) when
/// the engine Basic card cannot be resolved. It never calls the original helper in that state. The helper
/// fast path keeps its existing fail-closed abort. The fallback preserves the vanilla
/// TestMode/Deprived behavior for Spire1 characters by reproducing the same engine cards.
/// C14 r14: the helper fast path additionally requires the direct AfterObtained operation-scope
/// patch (layer 8) to be proven mounted, so a direct virtual call cannot complete an engine
/// AllCards read while the live gate could have been toggled open mid-call. The primary
/// replacement remains independently complete without the scope patch.
/// Mount state is only recorded from [HarmonyCleanup] after a successful install; a target that
/// was not resolved or whose installation threw is never reported as installed. A missing,
/// ambiguous or drifted AddRelicInternal early guard additionally invalidates every closed-gate
/// replacement proof, so the AfterObtained layer, the helper fast path and the RelicCmd.Obtain
/// funnel all refuse the grant instead of letting any layer fall through to the engine AllCards
/// read.
/// </para>
/// </summary>
internal static class Spire1LargeCapsuleGate
{
    internal static bool AfterObtainedGateMounted;
    internal static bool FunnelGateMounted;
    internal static bool StrikeHelperMounted;
    internal static bool DefendHelperMounted;

    /// <summary>
    /// C14 r3: set only from [HarmonyCleanup] of Spire1LargeCapsuleAddRelicInternalPatch after a
    /// successful install. While false the earliest entry guard cannot prove that the engine
    /// AllCards path is replaced, so it must refuse the whole obtain instead of relying on the
    /// four later layers.
    /// </summary>
    internal static bool AddRelicInternalGuardMounted;

    /// <summary>
    /// C14 r6: set only from [HarmonyCleanup] of Spire1LargeCapsuleFinalizeStartingRelicsPatch
    /// after a successful install. While false the closed-gate replacement cannot prove that the
    /// new-run FinalizeStartingRelics path is covered, so every other layer keeps refusing.
    /// </summary>
    internal static bool FinalizeStartingRelicsGateMounted;
    /// <summary>
    /// C14 r7: set only from [HarmonyCleanup] of Spire1LargeCapsuleAfterObtainedSafetyPatch
    /// after a successful install. This state is deliberately NOT part of
    /// HasCompleteClosedGateReplacement: the safety boundary consumes the very completeness
    /// proof it protects, so including its own mount state would be a self-trust loop. It exists
    /// for diagnostics only.
    /// </summary>
    internal static bool AfterObtainedSafetyGateMounted;

    /// <summary>
    /// C14 r8b: set only from [HarmonyCleanup] of
    /// Spire1LargeCapsuleLifecycleFinalizeGatePatch after a successful install. Diagnostics only;
    /// the lifecycle gate blocking decision does not read it.
    /// </summary>
    internal static bool LifecycleFinalizeGateMounted;

    /// <summary>
    /// C14 r8b: set only from [HarmonyCleanup] of
    /// Spire1LargeCapsuleObtainLifecycleGatePatch after a successful install. Diagnostics only;
    /// the lifecycle gate blocking decision does not read it.
    /// </summary>
    internal static bool LifecycleObtainGateMounted;

    /// <summary>
    /// C14 r4: the synchronous Player.FromSerializable scope that contains ordinary save-file
    /// relic loading. AsyncLocal keeps nested loads isolated per execution context and the
    /// Harmony finalizer restores the previous scope on both normal and exceptional exits.
    /// The scope records the exact RelicModel instances produced by RelicModel.FromSerializable;
    /// AddRelicInternal accepts only one of those instances, so a reentrant new Obtain cannot
    /// borrow the broad load marker.
    /// </summary>
    internal static bool LoadContextPatchMounted;
    internal static bool SyncContextPatchMounted;
    internal static bool DeserializedRelicPatchMounted;

    internal sealed class LoadContextScope
    {
        internal readonly LoadContext? Previous;
        internal LoadContextScope(LoadContext? previous)
        {
            Previous = previous;
        }
    }

    internal sealed class LoadContext
    {
        internal readonly object SyncRoot = new();
        internal readonly HashSet<RelicModel> DeserializedRelics = new(RelicReferenceComparer.Instance);
    }

    private sealed class RelicReferenceComparer : IEqualityComparer<RelicModel>
    {
        internal static readonly RelicReferenceComparer Instance = new();

        public bool Equals(RelicModel? x, RelicModel? y)
            => ReferenceEquals(x, y);

        public int GetHashCode(RelicModel obj)
            => RuntimeHelpers.GetHashCode(obj);
    }

    private static readonly AsyncLocal<LoadContext?> _currentLoadContext = new();

    internal static bool IsLoadContextActive
        => (LoadContextPatchMounted || SyncContextPatchMounted)
           && _currentLoadContext.Value is not null;

    internal static LoadContextScope EnterLoadContext()
    {
        LoadContext? previous = _currentLoadContext.Value;
        _currentLoadContext.Value = new LoadContext();
        return new LoadContextScope(previous);
    }

    internal static void ExitLoadContext(LoadContextScope? scope)
    {
        if (scope is not null)
        {
            _currentLoadContext.Value = scope.Previous;
        }
    }

    internal static void MarkDeserializedRelic(RelicModel relic)
    {
        if (!IsLoadContextActive || !DeserializedRelicPatchMounted)
        {
            return;
        }

        LoadContext? context = _currentLoadContext.Value;
        if (context is null)
        {
            return;
        }

        lock (context.SyncRoot)
        {
            context.DeserializedRelics.Add(relic);
        }
    }

    internal static bool TryConsumeDeserializedRelic(RelicModel relic)
    {
        if (!IsLoadContextActive || !DeserializedRelicPatchMounted)
        {
            return false;
        }

        LoadContext? context = _currentLoadContext.Value;
        if (context is null)
        {
            return false;
        }

        lock (context.SyncRoot)
        {
            return context.DeserializedRelics.Remove(relic);
        }
    }

    internal static void RecordLoadContextPatchMounted()
    {
        LoadContextPatchMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: Player.FromSerializable load-context guard installed (single target).");
    }

    internal static void RecordSyncContextPatchMounted()
    {
        SyncContextPatchMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: Player.SyncWithSerializedPlayer load-context guard installed (single target).");
    }

    internal static void RecordDeserializedRelicPatchMounted()
    {
        DeserializedRelicPatchMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: RelicModel.FromSerializable identity marker installed (single target).");
    }

    internal static bool GateAppliedLogged;
    internal static bool ResolveFailureLogged;
    private static bool _funnelBlockedLogged;
    private static bool _partialCoverageBlockedLogged;
    private static bool _earlyGuardBlockedLogged;
    private static bool _finalizeStartingRelicsBlockedLogged;
    private static bool _finalizeStartingRelicsReadFailedLogged;
    private static bool _afterObtainedSafetyBlockedLogged;
    private static bool _lifecycleFinalizeBlockedLogged;
    private static bool _lifecycleObtainBlockedLogged;

    internal static bool IsExpectedHelperSignature(MethodInfo? method)
        => method is not null
           && method.IsStatic
           && method.ReturnType == typeof(CardModel)
           && method.GetParameters() is [var parameter]
           && parameter.ParameterType == typeof(CharacterModel);

    internal static bool IsExpectedAfterObtainedSignature(MethodInfo? method)
    {
        if (method is null
            || !method.IsVirtual
            || method.ReturnType != typeof(Task)
            || method.GetParameters().Length != 0)
        {
            return false;
        }

        // Require the real override of RelicModel.AfterObtained. A hiding/new method would not
        // be reached by RelicCmd.Obtain's virtual call, so it must not count as mounted.
        MethodInfo baseDefinition = method.GetBaseDefinition();
        return baseDefinition.DeclaringType == typeof(RelicModel)
               && baseDefinition.Name == nameof(RelicModel.AfterObtained);
    }

    internal static string Describe(MethodInfo? method)
    {
        if (method is null)
        {
            return "missing";
        }
        string parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
        return $"found {method.ReturnType.Name}({parameters})";
    }

    internal static bool IsSpire1Character(CharacterModel character)
        => character is Spire1Ironclad or Spire1Silent or Spire1Defect;

    /// <summary>
    /// C14 r3/r6/r14: true when no closed-gate path can read character.CardPool.AllCards. The
    /// AddRelicInternal early guard MUST be mounted first, because it is the only layer that
    /// stops the new-run PopulateRelics path before the relic is added; while it is missing,
    /// no other layer counts as a complete replacement. Given the guard, there are exactly two
    /// sufficient proofs (r14: both are evaluated through the single-read snapshot below, so a
    /// concurrent install cannot produce a torn proof):
    /// (a) the AfterObtained prefix is mounted: it replaces the whole engine method body for the
    ///     three Spire1 placeholder characters while the cards group is closed, so
    ///     GetStrikeForCharacter / GetDefendForCharacter are never called - this holds for both
    ///     RelicCmd.Obtain (RelicCmd.cs:53) and RunManager.FinalizeStartingRelics
    ///     (RunManager.cs:735), which is why the RelicCmd.Obtain funnel alone is NOT sufficient;
    /// (b) the AfterObtained prefix is NOT mounted, but BOTH helper prefixes are AND the direct
    ///     AfterObtained operation-scope patch is mounted: the engine body runs, yet each of its
    ///     two AllCards reads is intercepted and replaced before execution, and the direct
    ///     AfterObtained call carries the frozen gate snapshot the helper prefixes read. r14: the
    ///     scope patch is part of this proof because without it a direct virtual AfterObtained
    ///     call has no EnterScoped and a mid-call toggle could make the helper prefixes observe a
    ///     live-open gate. The scope patch is NOT required for proof (a): the primary replacement
    ///     never calls the engine helper at all and is independently complete.
    /// A single mounted helper is never sufficient (the unmounted sibling still reads AllCards).
    /// </summary>
    internal static bool HasCompleteClosedGateReplacement
        => HasClosedGateReplacementProofSnapshot(out _);

    internal static bool TryReplaceBasic(
        CharacterModel character,
        CardTag tag,
        ref CardModel __result)
    {
        // Open group: original engine behavior, untouched for every character.
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        // Closed group: scope is exactly the three Spire1 placeholder characters. Vanilla
        // characters keep the engine helper, so vanilla LargeCapsule is unchanged for them.
        if (!IsSpire1Character(character))
        {
            return true;
        }

        // Last-line fail-closed check: a mounted helper may only complete the engine AllCards
        // read when some other layer already proved the closed-gate replacement complete. The
        // guard must be part of that proof (r6): without the AddRelicInternal early guard, the
        // new-run FinalizeStartingRelics path can reach the engine body before any funnel or
        // sibling helper applies. Refuse the grant instead of completing it.
        if (!HasClosedGateReplacementProofSnapshot(out string proof))
        {
            LogPartialCoverageBlockOnce(character, tag, proof);
            throw new InvalidOperationException(
                $"[Spire1] LargeCapsule gate refused to grant a card for {character.GetType().Name}: " +
                "no complete closed-gate replacement is proven mounted.");
        }

        try
        {
            CardModel? replacement = ResolveEngineBasic(character, tag);
            if (replacement is null)
            {
                string detail = "no Basic card with the required tag was resolvable";
                LogResolveFailure(character, tag, detail);
                throw new InvalidOperationException(
                    $"[Spire1] LargeCapsule gate refused to grant a card for {character.GetType().Name}: {detail}");
            }

            __result = replacement;
            LogGateAppliedOnce(character, tag);
            return false;
        }
        catch (Exception e)
        {
            LogResolveFailure(character, tag, $"{e.GetType().Name}: {e.Message}");
            throw new InvalidOperationException(
                $"[Spire1] LargeCapsule gate refused to grant a card for {character.GetType().Name} after engine Basic resolution failed.",
                e);
        }
    }

    internal static Task GrantEngineBasics(LargeCapsule instance, CharacterModel character)
    {
        return GrantEngineBasicsCore(instance, character);
    }

    private static async Task GrantEngineBasicsCore(LargeCapsule instance, CharacterModel character)
    {
        for (int i = 0; i < instance.DynamicVars["Relics"].IntValue; i++)
        {
            RelicModel relic = RelicFactory.PullNextRelicFromFront(instance.Owner).ToMutable();
            await RelicCmd.Obtain(relic, instance.Owner);
        }

        List<CardPileAddResult> results = new(2);
        results.Add(await CardPileCmd.Add(
            instance.Owner.RunState.CreateCard(ResolveEngineStrike(character), instance.Owner),
            PileType.Deck));
        results.Add(await CardPileCmd.Add(
            instance.Owner.RunState.CreateCard(ResolveEngineDefend(character), instance.Owner),
            PileType.Deck));
        CardCmd.PreviewCardPileAdd(results, 2f);
    }

    private static CardModel ResolveEngineStrike(CharacterModel character)
        => ResolveRequired(character, CardTag.Strike);

    private static CardModel ResolveEngineDefend(CharacterModel character)
        => ResolveRequired(character, CardTag.Defend);

    private static CardModel ResolveRequired(CharacterModel character, CardTag tag)
    {
        CardModel? resolved = ResolveEngineBasic(character, tag);
        if (resolved is null)
        {
            string detail = "no Basic card with the required tag was resolvable";
            LogResolveFailure(character, tag, detail);
            throw new InvalidOperationException(
                $"[Spire1] LargeCapsule gate refused to grant a card for {character.GetType().Name}: {detail}");
        }
        return resolved;
    }

    /// <summary>
    /// Engine-equivalent Basic card for the Spire1 placeholder identity, using the same rule as
    /// the engine helper (Rarity == Basic + tag) but against the engine pool. Two fallback
    /// tiers keep the closed gate from ever handing back a SPIRE1-* card: the character's
    /// StartingDeck (declared with engine starter cards) and finally the typed engine core
    /// card. A null return is only reachable if the engine content itself is missing.
    /// </summary>
    private static CardModel? ResolveEngineBasic(CharacterModel character, CardTag tag)
    {
        CardPoolModel enginePool = character switch
        {
            Spire1Ironclad => ModelDb.CardPool<EngineIroncladCardPool>(),
            Spire1Silent => ModelDb.CardPool<EngineSilentCardPool>(),
            Spire1Defect => ModelDb.CardPool<EngineDefectCardPool>(),
            _ => throw new InvalidOperationException("not a Spire1 character"),
        };

        CardModel? fromPool = enginePool.AllCards.FirstOrDefault(
            c => c.Rarity == CardRarity.Basic && c.Tags.Contains(tag));
        if (fromPool is not null)
        {
            return fromPool;
        }

        CardModel? fromDeck = character.StartingDeck.FirstOrDefault(
            c => c.Rarity == CardRarity.Basic && c.Tags.Contains(tag));
        if (fromDeck is not null)
        {
            return fromDeck;
        }

        return tag == CardTag.Strike ? EngineStrike(character) : EngineDefend(character);
    }

    private static CardModel EngineStrike(CharacterModel character) => character switch
    {
        Spire1Ironclad => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(),
        Spire1Silent => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(),
        Spire1Defect => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeDefect>(),
        _ => throw new InvalidOperationException("not a Spire1 character"),
    };

    private static CardModel EngineDefend(CharacterModel character) => character switch
    {
        Spire1Ironclad => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(),
        Spire1Silent => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(),
        Spire1Defect => ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendDefect>(),
        _ => throw new InvalidOperationException("not a Spire1 character"),
    };

    /// <summary>
    /// C14 r12: true only when the canonical Spire1 deck-grant subscriber is proven present in
    /// ModHelper._runHookSubscribers. The probe is read-only and never mutates the engine list.
    /// While this is false, no closed-gate path may rely on the model-level ShouldAddToDeck
    /// boundary, so the lifecycle gates treat the replacement proof as incomplete.
    /// </summary>
    internal static bool DeckGrantSubscriberConfirmed =>
        Spire1.Spire1Code.Cards.Spire1DeckGrantGuard.IsSubscriberConfirmed;

    internal static void RecordAfterObtainedGateMounted()
    {
        AfterObtainedGateMounted = true;
        MainFile.Logger.Info("[Spire1] LargeCapsule gate: AfterObtained primary layer installed (single target).");
    }

    internal static void RecordFunnelGateMounted()
    {
        FunnelGateMounted = true;
        MainFile.Logger.Info("[Spire1] LargeCapsule gate: RelicCmd.Obtain fail-closed funnel installed (single target).");
    }

    internal static void RecordAddRelicInternalGuardMounted()
    {
        AddRelicInternalGuardMounted = true;
        MainFile.Logger.Info("[Spire1] LargeCapsule gate: Player.AddRelicInternal early fail-closed guard installed (single target).");
    }

    internal static void RecordFinalizeStartingRelicsGateMounted()
    {
        FinalizeStartingRelicsGateMounted = true;
        MainFile.Logger.Info("[Spire1] LargeCapsule gate: RunManager.FinalizeStartingRelics fail-closed guard installed (single target).");
    }
    /// <summary>
    /// C14 r12/r14: set only from [HarmonyCleanup] of the direct AfterObtained scope patch after a
    /// successful install. Since r14 this flag is part of the helper-fallback completeness proof:
    /// without the scope patch a direct public virtual AfterObtained call has no EnterScoped and a
    /// mid-call settings toggle could make a helper prefix complete the engine AllCards read under
    /// a live-open gate. The primary AfterObtained replacement does not require this flag because
    /// it replaces the whole engine body.
    /// </summary>
    internal static bool AfterObtainedScopePatchMounted;

    internal static void RecordAfterObtainedScopePatchMounted()
    {
        AfterObtainedScopePatchMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: direct AfterObtained operation-scope patch installed (single target).");
    }

    internal static void RecordAfterObtainedSafetyGateMounted()
    {
        AfterObtainedSafetyGateMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: independent AfterObtained safety boundary installed (single target, Priority.First).");
    }

    internal static void RecordLifecycleFinalizeGateMounted()
    {
        LifecycleFinalizeGateMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: independent RunManager.FinalizeStartingRelics lifecycle boundary installed (single target).");
    }

    internal static void RecordLifecycleObtainGateMounted()
    {
        LifecycleObtainGateMounted = true;
        MainFile.Logger.Info(
            "[Spire1] LargeCapsule gate: independent RelicCmd.Obtain lifecycle boundary installed (single target).");
    }

    /// <summary>
    /// C14 r10/r14: one consistent snapshot of the closed-gate replacement proof. Each mount flag
    /// is read exactly once so a concurrent installation cannot produce a torn proof. The r7
    /// safety boundary's own mount state is deliberately NOT part of this proof (r9b supervisor
    /// SUP-C14-P0-02): a boundary cannot certify the very completeness it protects, so including
    /// it would be a self-trust loop. Safety remains an independent interceptor only.
    /// <para>
    /// C14 r14: the direct AfterObtained scope patch mount is part of the helper-fallback proof.
    /// Without that patch, a direct public virtual AfterObtained call has no EnterScoped, so a
    /// mid-call settings toggle could make a helper prefix read a live-open gate and complete the
    /// engine AllCards read. The primary replacement (AfterObtainedGateMounted) does not require
    /// the scope patch: it replaces the whole engine body and never calls the engine helpers.
    /// </para>
    /// <para>
    /// This proof is also no longer the only closed-gate guarantee. Spire1Card.Tags carries a
    /// patch-independent total fuse that reads the atomic Cards gate at the engine point of use,
    /// so even with every mount flag false the LargeCapsule AllCards predicate cannot select a
    /// SPIRE1-* Basic Strike/Defend. The lifecycle gates still refuse with a faulted task when
    /// this proof is incomplete, which keeps the failure visible instead of relying on the
    /// engine's own First() exception.
    /// </para>
    /// </summary>
    internal static bool HasClosedGateReplacementProofSnapshot(out string detail)
    {
        bool subscriber = DeckGrantSubscriberConfirmed;
        bool addGuard = Volatile.Read(ref AddRelicInternalGuardMounted);
        bool primary = Volatile.Read(ref AfterObtainedGateMounted);
        bool strike = Volatile.Read(ref StrikeHelperMounted);
        bool defend = Volatile.Read(ref DefendHelperMounted);
        bool scope = Volatile.Read(ref AfterObtainedScopePatchMounted);
        bool complete = subscriber
                        && addGuard
                        && (primary || (strike && defend && scope));
        detail =
            $"subscriber={subscriber}, earlyGuard={addGuard}, afterObtained={primary}, " +
            $"strikeHelper={strike}, defendHelper={defend}, scopePatch={scope}";
        return complete;
    }

    internal static void LogLifecycleFinalizeBlockedOnce(string detail)
    {
        if (_lifecycleFinalizeBlockedLogged)
        {
            return;
        }
        _lifecycleFinalizeBlockedLogged = true;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at RunManager.FinalizeStartingRelics (independent " +
            $"lifecycle boundary): no closed-gate replacement proof is available ({detail}) while a " +
            "Spire1 placeholder holds a LargeCapsule and the cards content group is off. The engine " +
            "AllCards grant is never reached.");
    }

    internal static void LogLifecycleObtainBlockedOnce(CharacterModel? character, string detail)
    {
        if (_lifecycleObtainBlockedLogged)
        {
            return;
        }
        _lifecycleObtainBlockedLogged = true;
        string shape = character is null ? "the target shape" : character.GetType().Name;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at RelicCmd.Obtain (independent lifecycle boundary): " +
            $"no closed-gate replacement proof is available ({detail}) while {shape} " +
            "is a Spire1 placeholder and the cards content group is off. The engine AllCards grant is " +
            "never reached.");
    }

    internal static void LogAfterObtainedSafetyBlockedOnce(CharacterModel character)
    {
        if (_afterObtainedSafetyBlockedLogged)
        {
            return;
        }
        _afterObtainedSafetyBlockedLogged = true;
        HasClosedGateReplacementProofSnapshot(out string proof);
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at LargeCapsule.AfterObtained (safety boundary): " +
            $"no complete closed-gate replacement is proven mounted ({proof}); refusing to run the " +
            $"engine body for {character.GetType().Name} while the cards content group is off. " +
            "The engine AllCards grant is never reached.");
    }

    /// <summary>
    /// C14 r12: abnormal-shape variant. Used when the relic Owner / Owner.Character cannot be
    /// read at all; the caller has already committed to a faulted Task, so this only records
    /// the one-time error and never needs a CharacterModel.
    /// </summary>
    internal static void LogAfterObtainedSafetyBlockedOnce(string detail)
    {
        if (_afterObtainedSafetyBlockedLogged)
        {
            return;
        }
        _afterObtainedSafetyBlockedLogged = true;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at LargeCapsule.AfterObtained (safety boundary): " +
            $"the relic owner shape is abnormal ({detail}); refusing to run the engine body while " +
            "the cards content group is off. The engine AllCards grant is never reached.");
    }

    internal static void LogFinalizeStartingRelicsBlockedOnce(CharacterModel character, string missing)
    {
        if (_finalizeStartingRelicsBlockedLogged)
        {
            return;
        }
        _finalizeStartingRelicsBlockedLogged = true;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at RunManager.FinalizeStartingRelics: " +
            $"the closed-gate replacement is incomplete ({missing}); " +
            $"refusing to finalize starting relics while {character.GetType().Name} holds a LargeCapsule " +
            "and the cards content group is off. The engine AllCards grant is never reached.");
    }

    internal static void LogFinalizeStartingRelicsReadFailedOnce(string detail)
    {
        if (_finalizeStartingRelicsReadFailedLogged)
        {
            return;
        }
        _finalizeStartingRelicsReadFailedLogged = true;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at RunManager.FinalizeStartingRelics: " +
            $"the run state could not be read safely ({detail}); fail closed while the cards content group is off.");
    }

    /// <summary>
    /// C14 r6: strict reflection read of the confirmed private RunManager.State property. The
    /// property getter and its RunState return type are re-verified at call time; a missing
    /// getter, a non-RunState value or a thrown getter is reported as a read failure so the
    /// caller can fail closed instead of assuming the state is safe.
    /// </summary>
    internal static bool TryGetRunManagerState(RunManager runManager, out RunState? state, out string? failure)
    {
        state = null;
        failure = null;
        try
        {
            PropertyInfo? property = AccessTools.DeclaredProperty(typeof(RunManager), "State");
            if (property is null)
            {
                failure = "private RunManager.State property was not found";
                return false;
            }

            if (property.PropertyType != typeof(RunState))
            {
                failure = $"RunManager.State type drifted to {property.PropertyType.FullName}";
                return false;
            }

            MethodInfo? getter = property.GetGetMethod(nonPublic: true);
            if (getter is null || getter.IsStatic)
            {
                failure = "RunManager.State has no usable instance getter";
                return false;
            }

            object? value = getter.Invoke(runManager, null);
            if (value is null)
            {
                // C14 r11 fail-closed: a null State is an abnormal read for the lifecycle
                // boundaries this gate protects. Treat it as a read failure instead of a
                // "no holder" result so the caller refuses the original engine path.
                state = null;
                failure = "RunManager.State was null";
                return false;
            }

            if (value is not RunState runState)
            {
                failure = $"RunManager.State returned {value.GetType().FullName} instead of RunState";
                return false;
            }

            state = runState;
            return true;
        }
        catch (Exception e)
        {
            failure = $"{e.GetType().Name}: {e.Message}";
            return false;
        }
    }

    /// <summary>
    /// C14 r6/r10: returns the first Spire1 placeholder player that already holds a LargeCapsule
    /// in the confirmed RunState.Players / Player.Relics shape, or null when no such holder
    /// exists. The state, the players list, every participating player slot and the target
    /// Spire1 player's Relics collection are validated strictly; a null player, a null Relics
    /// collection or any read exception throws so the caller fails closed instead of silently
    /// treating an abnormal run state as safe (r9b supervisor SUP-C14-P1-04).
    /// </summary>
    internal static CharacterModel? FindSpire1LargeCapsuleHolder(RunManager runManager)
    {
        if (!TryGetRunManagerState(runManager, out RunState? state, out string? failure))
        {
            LogFinalizeStartingRelicsReadFailedOnce(failure ?? "unknown RunManager.State read failure");
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to finalize starting relics: " +
                $"RunManager.State could not be read safely ({failure}).");
        }

        if (state is null)
        {
            return null;
        }

        IReadOnlyList<Player> players = state.Players;
        if (players is null)
        {
            LogFinalizeStartingRelicsReadFailedOnce("RunState.Players was null");
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to finalize starting relics: RunState.Players was null.");
        }

        for (int i = 0; i < players.Count; i++)
        {
            Player? player = players[i];
            if (player is null)
            {
                // r10 fail-closed: a null player slot is an abnormal run-state shape. Refusing
                // here is safe for every normal run (the engine never inserts null) and closes
                // the r9b supervisor SUP-C14-P1-04 gap instead of skipping the slot.
                LogFinalizeStartingRelicsReadFailedOnce($"RunState.Players[{i}] was null");
                throw new InvalidOperationException(
                    $"[Spire1] LargeCapsule gate refused to finalize starting relics: RunState.Players[{i}] was null.");
            }

            CharacterModel? character;
            try
            {
                character = player.Character;
            }
            catch (Exception e)
            {
                LogFinalizeStartingRelicsReadFailedOnce(
                    $"RunState.Players[{i}].Character threw {e.GetType().Name}: {e.Message}");
                throw new InvalidOperationException(
                    $"[Spire1] LargeCapsule gate refused to finalize starting relics: " +
                    $"RunState.Players[{i}].Character could not be read safely.", e);
            }

            // C14 r12 fail-closed: a null Character is an abnormal run-state shape. It may not
            // be treated as "no holder"; the engine body would iterate this same player's Relics.
            if (character is null)
            {
                LogFinalizeStartingRelicsReadFailedOnce($"RunState.Players[{i}].Character was null");
                throw new InvalidOperationException(
                    $"[Spire1] LargeCapsule gate refused to finalize starting relics: RunState.Players[{i}] had a null Character.");
            }

            IReadOnlyList<RelicModel> relics;
            try
            {
                relics = player.Relics;
            }
            catch (Exception e)
            {
                LogFinalizeStartingRelicsReadFailedOnce(
                    $"Spire1 player Relics threw {e.GetType().Name}: {e.Message}");
                throw new InvalidOperationException(
                    "[Spire1] LargeCapsule gate refused to finalize starting relics: the target " +
                    "Spire1 player's Relics could not be read safely.", e);
            }

            if (relics is null)
            {
                // r10 fail-closed: the engine body (RunManager.cs:733) iterates this same
                // collection, so an unreadable target holder is never allowed to pass through
                // to the original lifecycle.
                LogFinalizeStartingRelicsReadFailedOnce(
                    $"Spire1 player ({character.GetType().Name}) Relics was null");
                throw new InvalidOperationException(
                    "[Spire1] LargeCapsule gate refused to finalize starting relics: the target " +
                    "Spire1 player's Relics collection was null.");
            }

            for (int r = 0; r < relics.Count; r++)
            {
                RelicModel? relic = relics[r];
                if (relic is null)
                {
                    LogFinalizeStartingRelicsReadFailedOnce(
                        $"player ({character.GetType().Name}) Relics[{r}] was null");
                    throw new InvalidOperationException(
                        $"[Spire1] LargeCapsule gate refused to finalize starting relics: the player's Relics[{r}] was null.");
                }

                if (relic is LargeCapsule)
                {
                    if (!IsSpire1Character(character))
                    {
                        // A non-Spire1 holder does not trigger the gate; its shape is still
                        // validated above so an abnormal non-target slot can never be silently
                        // treated as safe.
                        continue;
                    }

                    return character;
                }
            }
        }

        return null;
    }

    internal static void LogEarlyGuardBlockedOnce(CharacterModel character, string missing)
    {
        if (_earlyGuardBlockedLogged)
        {
            return;
        }
        _earlyGuardBlockedLogged = true;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED at Player.AddRelicInternal: " +
            $"the closed-gate replacement is incomplete ({missing}); " +
            $"refusing to obtain LargeCapsule for {character.GetType().Name} while the cards content group is off. " +
            "The engine AllCards grant is never reached.");
    }

    internal static void RecordStrikeHelperMounted()
    {
        StrikeHelperMounted = true;
        MainFile.Logger.Info("[Spire1] LargeCapsule gate: GetStrikeForCharacter helper installed (single target).");
    }

    internal static void RecordDefendHelperMounted()
    {
        DefendHelperMounted = true;
        MainFile.Logger.Info("[Spire1] LargeCapsule gate: GetDefendForCharacter helper installed (single target).");
    }

    internal static void LogGateAppliedOnce(CharacterModel character, CardTag tag)
    {
        if (GateAppliedLogged)
        {
            return;
        }
        GateAppliedLogged = true;
        MainFile.Logger.Info(
            $"[Spire1] LargeCapsule gate active: cards content group off - {character.GetType().Name} " +
            $"receives the engine Basic {tag} card instead of SPIRE1 content.");
    }

    internal static void LogResolveFailure(CharacterModel character, CardTag tag, string detail)
    {
        if (ResolveFailureLogged)
        {
            return;
        }
        ResolveFailureLogged = true;
        MainFile.Logger.Error(
            $"[Spire1] LargeCapsule gate: could not resolve the engine Basic {tag} card for " +
            $"{character.GetType().Name} ({detail}); the grant is aborted and the original helper is not called.");
    }

    internal static void LogFunnelBlockedOnce(CharacterModel? character)
    {
        if (_funnelBlockedLogged)
        {
            return;
        }
        _funnelBlockedLogged = true;
        string shape = character is null ? "the target shape" : character.GetType().Name;
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule gate BLOCKED: no complete closed-gate replacement is proven mounted " +
            $"(afterObtained={AfterObtainedGateMounted}, funnel={FunnelGateMounted}, " +
            $"strikeHelper={StrikeHelperMounted}, defendHelper={DefendHelperMounted}, " +
            $"earlyGuard={AddRelicInternalGuardMounted}, finalizeStartingRelics={FinalizeStartingRelicsGateMounted}, " +
            $"safety={AfterObtainedSafetyGateMounted}, subscriber={DeckGrantSubscriberConfirmed}); " +
            $"refusing to obtain LargeCapsule for {shape} while the cards content group is off. " +
            "The engine AllCards grant is never reached.");
    }

    internal static void LogPartialCoverageBlockOnce(CharacterModel character, CardTag tag, string detail)
    {
        if (_partialCoverageBlockedLogged)
        {
            return;
        }
        _partialCoverageBlockedLogged = true;
        MainFile.Logger.Error(
            $"[Spire1] LargeCapsule gate BLOCKED ({tag}) for {character.GetType().Name}: " +
            $"the AddRelicInternal early guard, the AfterObtained layer, a helper prefix or the " +
            $"direct scope patch is missing; no complete closed-gate replacement is proven " +
            $"({detail}), so the engine AllCards grant is refused.");
    }
}

/// <summary>
/// C14 r4 load-context marker, single target: Player.FromSerializable(SerializablePlayer).
/// Ordinary save loading reaches AddRelicInternal with silent:false, so the C14 r3 early guard
/// needs a narrowly scoped marker that is active only while this synchronous engine loader is on
/// the call path. Prefix/finalizer symmetry restores the depth on both normal and exceptional
/// exits; when this target drifts or installation fails, no marker is ever reported as mounted.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleLoadContextPatch
{
    private static readonly MethodInfo? Target = Resolve();
    private static bool _targetVerified;

    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo[] candidates = typeof(Player)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.DeclaringType == typeof(Player)
                    && m.Name == nameof(Player.FromSerializable)
                    && !m.IsGenericMethod
                    && m.ReturnType == typeof(Player)
                    && m.GetParameters() is [var parameter]
                    && parameter.ParameterType == typeof(SerializablePlayer))
                .ToArray();

            if (candidates.Length != 1)
            {
                MainFile.Logger.Error(
                    "[Spire1] LargeCapsule load-context NOT mounted: "
                    + $"expected exactly one Player.FromSerializable(SerializablePlayer), found {candidates.Length}.");
                return null;
            }

            return candidates[0];
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule load-context target resolution threw "
                + $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule load-context NOT mounted: "
                + "Player.FromSerializable(SerializablePlayer) was missing or ambiguous; "
                + "ordinary old-save relic loading remains fail-closed when the cards group is off.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordLoadContextPatchMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule load-context NOT mounted: installation threw "
            + $"({__exception.GetType().Name}: {__exception.Message}); "
            + "ordinary old-save relic loading remains fail-closed when the cards group is off.");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPrefix]
    private static void Enter(out Spire1LargeCapsuleGate.LoadContextScope __state)
    {
        __state = Spire1LargeCapsuleGate.EnterLoadContext();
    }

    [HarmonyFinalizer]
    private static Exception? Exit(
        Exception? __exception,
        Spire1LargeCapsuleGate.LoadContextScope? __state)
    {
        Spire1LargeCapsuleGate.ExitLoadContext(__state);
        return __exception;
    }
}

/// <summary>
/// C14 r4 multiplayer-load context marker, single target:
/// Player.SyncWithSerializedPlayer(SerializablePlayer). Its relic path uses silent:true, but the
/// flag alone is not a sufficient trust boundary; the exact RelicModel identity marker below is
/// still required by AddRelicInternal.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleSyncContextPatch
{
    private static readonly MethodInfo? Target = Resolve();
    private static bool _targetVerified;

    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo[] candidates = typeof(Player)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == typeof(Player)
                    && m.Name == nameof(Player.SyncWithSerializedPlayer)
                    && !m.IsStatic
                    && !m.IsGenericMethod
                    && m.ReturnType == typeof(void)
                    && m.GetParameters() is [var parameter]
                    && parameter.ParameterType == typeof(SerializablePlayer))
                .ToArray();

            if (candidates.Length != 1)
            {
                MainFile.Logger.Error(
                    "[Spire1] LargeCapsule sync load-context NOT mounted: "
                    + $"expected exactly one Player.SyncWithSerializedPlayer(SerializablePlayer), found {candidates.Length}.");
                return null;
            }

            return candidates[0];
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule sync load-context target resolution threw "
                + $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule sync load-context NOT mounted: "
                + "Player.SyncWithSerializedPlayer(SerializablePlayer) was missing or ambiguous; "
                + "silent:true existing-relic sync remains fail-closed when the cards group is off.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordSyncContextPatchMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule sync load-context NOT mounted: installation threw "
            + $"({__exception.GetType().Name}: {__exception.Message}); "
            + "silent:true existing-relic sync remains fail-closed when the cards group is off.");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPrefix]
    private static void Enter(out Spire1LargeCapsuleGate.LoadContextScope __state)
    {
        __state = Spire1LargeCapsuleGate.EnterLoadContext();
    }

    [HarmonyFinalizer]
    private static Exception? Exit(
        Exception? __exception,
        Spire1LargeCapsuleGate.LoadContextScope? __state)
    {
        Spire1LargeCapsuleGate.ExitLoadContext(__state);
        return __exception;
    }
}

/// <summary>
/// C14 r4 identity marker, single target: RelicModel.FromSerializable(SerializableRelic).
/// The postfix records the exact mutable relic instance that the lazy save-load sequence is about
/// to pass to Player.AddRelicInternal. If this target drifts or fails to install, the early guard
/// deliberately does not accept silent:false LargeCapsule additions from the load scope.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleDeserializedRelicPatch
{
    private static readonly MethodInfo? Target = Resolve();
    private static bool _targetVerified;

    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo[] candidates = typeof(RelicModel)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.DeclaringType == typeof(RelicModel)
                    && m.Name == nameof(RelicModel.FromSerializable)
                    && !m.IsGenericMethod
                    && m.ReturnType == typeof(RelicModel)
                    && m.GetParameters() is [var parameter]
                    && parameter.ParameterType == typeof(SerializableRelic))
                .ToArray();

            if (candidates.Length != 1)
            {
                MainFile.Logger.Error(
                    "[Spire1] LargeCapsule relic identity marker NOT mounted: "
                    + $"expected exactly one RelicModel.FromSerializable(SerializableRelic), found {candidates.Length}.");
                return null;
            }

            return candidates[0];
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule relic identity target resolution threw "
                + $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule relic identity marker NOT mounted: "
                + "RelicModel.FromSerializable(SerializableRelic) was missing or ambiguous; "
                + "ordinary old-save LargeCapsule loading remains fail-closed when cards are off.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordDeserializedRelicPatchMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule relic identity marker NOT mounted: installation threw "
            + $"({__exception.GetType().Name}: {__exception.Message}); "
            + "ordinary old-save LargeCapsule loading remains fail-closed when cards are off.");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPostfix]
    private static void Mark(RelicModel __result)
    {
        Spire1LargeCapsuleGate.MarkDeserializedRelic(__result);
    }
}

/// <summary>
/// Primary layer, single target: a prefix on the public virtual LargeCapsule.AfterObtained. This
/// class has no dependency on the private helper names, so helper drift cannot disable it. If
/// this target itself drifts, [HarmonyPrepare] returns false and no mount is recorded; the
/// RelicCmd.Obtain funnel then blocks the grant for Spire1 characters while the cards group is
/// closed, so the engine AllCards path is never silently restored.
/// </summary>
[HarmonyPatch(typeof(LargeCapsule), nameof(LargeCapsule.AfterObtained))]
internal static class Spire1LargeCapsuleAfterObtainedPatch
{
    private static bool _targetVerified;

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        MethodInfo? method;
        try
        {
            method = AccessTools.DeclaredMethod(typeof(LargeCapsule), nameof(LargeCapsule.AfterObtained));
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AfterObtained gate NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}). The RelicCmd.Obtain funnel will block the grant.");
            return false;
        }

        if (!Spire1LargeCapsuleGate.IsExpectedAfterObtainedSignature(method))
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AfterObtained gate NOT mounted: expected the LargeCapsule " +
                $"override of RelicModel.AfterObtained, got {Spire1LargeCapsuleGate.Describe(method)}. " +
                "The RelicCmd.Obtain funnel will block the grant.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            // Prepare() returned false (or was never reached). Harmony still calls Cleanup with
            // no arguments in that path; never report the missing target as installed.
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordAfterObtainedGateMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule AfterObtained gate NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}). The RelicCmd.Obtain funnel will block the grant.");
    }

    [HarmonyPrefix]
    private static bool ReplaceAfterObtained(LargeCapsule __instance, ref Task __result)
    {
        // Open group: original engine behavior, untouched for every character.
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        // C14 r12: null Owner / Owner.Character are abnormal shapes. Fail closed with a faulted
        // task instead of treating them as "not the target" and letting the engine body run.
        CharacterModel? character;
        try
        {
            character = __instance.Owner?.Character;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to replace AfterObtained: the relic Owner " +
                $"could not be read safely ({e.GetType().Name}).", e);
        }

        if (character is null)
        {
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to replace AfterObtained: the relic Owner or " +
                "Owner.Character was null while the cards content group is off.");
        }

        if (!Spire1LargeCapsuleGate.IsSpire1Character(character))
        {
            return true;
        }

        // r6 fail-closed: the primary layer may only complete the engine grant when the
        // AddRelicInternal early guard is proven mounted. If the guard target drifts, the
        // new-run Player.PopulateRelics path adds the relic without any interception and
        // FinalizeStartingRelics then calls this method directly; that path must not be
        // replaced by a replacement that the guard was meant to protect.
        if (!Spire1LargeCapsuleGate.AddRelicInternalGuardMounted)
        {
            string missing =
                $"afterObtained={Spire1LargeCapsuleGate.AfterObtainedGateMounted}, " +
                $"strikeHelper={Spire1LargeCapsuleGate.StrikeHelperMounted}, " +
                $"defendHelper={Spire1LargeCapsuleGate.DefendHelperMounted}, " +
                $"funnel={Spire1LargeCapsuleGate.FunnelGateMounted}, " +
                $"earlyGuard={Spire1LargeCapsuleGate.AddRelicInternalGuardMounted}";
            Spire1LargeCapsuleGate.LogEarlyGuardBlockedOnce(character, missing);
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to replace AfterObtained: the AddRelicInternal " +
                "early guard is NOT mounted and the fresh-run relic path is unprotected.");
        }

        try
        {
            __result = Spire1LargeCapsuleGate.GrantEngineBasics(__instance, character);
            Spire1LargeCapsuleGate.LogGateAppliedOnce(character, CardTag.Strike);
            return false;
        }
        catch (Exception e)
        {
            Spire1LargeCapsuleGate.LogResolveFailure(character, CardTag.Strike, $"{e.GetType().Name}: {e.Message}");
            throw new InvalidOperationException(
                $"[Spire1] LargeCapsule gate refused to complete AfterObtained for {character.GetType().Name} after engine Basic resolution failed.",
                e);
        }
    }
}

/// <summary>
/// C14 r7 independent fail-closed boundary, single target: a maximum-priority prefix on the
/// same public virtual LargeCapsule.AfterObtained method as the primary layer, owned by a
/// separate patch class with its own [HarmonyPrepare]/[HarmonyCleanup] and mount state. It runs
/// before every other prefix on this method: [HarmonyPriority(Priority.First)] is 800 and the
/// decompiled Harmony 2.4.2 PatchSorter orders higher priority values first, so the engine body
/// cannot start when no complete closed-gate replacement is proven. This closes the r6
/// supervisor counterexample in which AddRelicInternalGuardMounted=false and
/// FinalizeStartingRelicsGateMounted=false occur together and the engine body reads
/// character.CardPool.AllCards from GetStrikeForCharacter (LargeCapsule.cs:38-45) before the
/// Defend helper is reached. While the cards group is closed and the relic owner is one of the
/// three Spire1 placeholder characters, the original body is allowed only when
/// HasCompleteClosedGateReplacement is true; otherwise a faulted Task is returned and neither
/// the engine body nor any later prefix runs. Vanilla characters, other mods' characters and
/// the cards-open path return true untouched. This class never consumes the primary
/// AfterObtained class's Prepare state, and its own mount state is deliberately not part of
/// HasCompleteClosedGateReplacement, so the completeness proof cannot trust itself.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleAfterObtainedSafetyPatch
{
    private static bool _targetResolved;
    private static MethodInfo? _target;
    private static bool _targetVerified;

    // Lazy on purpose: a throwing static initializer would abort type initialization before
    // Harmony can call [HarmonyPrepare]/[HarmonyCleanup], leaving the mount state undefined.
    private static MethodInfo? Target
    {
        get
        {
            if (!_targetResolved)
            {
                _targetResolved = true;
                _target = Resolve();
            }

            return _target;
        }
    }

    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo[] candidates = AccessTools.GetDeclaredMethods(typeof(LargeCapsule))
                .Where(m => m.Name == nameof(LargeCapsule.AfterObtained))
                .ToArray();
            if (candidates.Length != 1)
            {
                return null;
            }

            MethodInfo method = candidates[0];
            return IsExpectedSafetyTarget(method) ? method : null;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AfterObtained safety gate NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    /// <summary>
    /// Strict r7 target contract: exactly the LargeCapsule override of RelicModel.AfterObtained,
    /// non-generic, instance, virtual, returning Task with zero parameters. A hiding/new method
    /// (not reached by the virtual call) or any signature drift is rejected.
    /// </summary>
    private static bool IsExpectedSafetyTarget(MethodInfo method)
    {
        if (method.DeclaringType != typeof(LargeCapsule)
            || method.IsGenericMethod
            || method.IsStatic
            || !method.IsVirtual
            || method.ReturnType != typeof(Task)
            || method.GetParameters().Length != 0)
        {
            return false;
        }

        MethodInfo baseDefinition = method.GetBaseDefinition();
        return baseDefinition.DeclaringType == typeof(RelicModel)
               && baseDefinition.Name == nameof(RelicModel.AfterObtained);
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AfterObtained safety gate NOT mounted: " +
                "LargeCapsule.AfterObtained() was missing, ambiguous, or drifted from the strict " +
                "non-generic instance Task zero-parameter override contract; the shared fail-closed " +
                "boundary is unavailable.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordAfterObtainedSafetyGateMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule AfterObtained safety gate NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool Prefix(LargeCapsule __instance, ref Task __result)
    {
        // Open group: original engine behavior, untouched for every character.
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        // Closed group: scope is exactly the three Spire1 placeholder characters, mirroring the
        // primary layer. Vanilla characters and other mods keep the engine behavior.
        // C14 r12: null Owner / Owner.Character must fail closed, never pass as non-target.
        CharacterModel? character;
        try
        {
            character = __instance?.Owner?.Character;
        }
        catch (Exception e)
        {
            Spire1LargeCapsuleGate.LogAfterObtainedSafetyBlockedOnce("null Owner/Character");
            __result = Task.FromException(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to run the engine AfterObtained body: the " +
                $"relic Owner could not be read safely ({e.GetType().Name}).", e));
            return false;
        }

        if (character is null)
        {
            Spire1LargeCapsuleGate.LogAfterObtainedSafetyBlockedOnce("null Owner/Character");
            __result = Task.FromException(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to run the engine AfterObtained body: the " +
                "relic Owner or Owner.Character was null while the cards content group is off."));
            return false;
        }

        if (!Spire1LargeCapsuleGate.IsSpire1Character(character))
        {
            return true;
        }

        // Complete AllCards-free replacement proven (r6 proof): the primary layer replaces the
        // whole body, or both helper prefixes replace the two AllCards reads. Let the existing
        // layers finish; this boundary exists only for the combinations in which none of them
        // can intervene before the first AllCards read.
        if (Spire1LargeCapsuleGate.HasCompleteClosedGateReplacement)
        {
            return true;
        }

        // Fail closed: the engine body would run GetStrikeForCharacter first and read
        // character.CardPool.AllCards (LargeCapsule.cs:38-45) before the Defend helper could
        // intervene. Return a faulted Task and skip every later prefix and the engine body.
        Spire1LargeCapsuleGate.LogAfterObtainedSafetyBlockedOnce(character);
        __result = Task.FromException(new InvalidOperationException(
            "[Spire1] LargeCapsule gate refused to run the engine AfterObtained body: no complete " +
            "closed-gate replacement is proven mounted while a Spire1 placeholder owns a " +
            "LargeCapsule and the cards content group is off; the engine AllCards grant is never reached."));
        return false;
    }
}

/// <summary>
/// C14 r12 independent operation snapshot, single target: the same public virtual
/// LargeCapsule.AfterObtained method. This class exists only to own the gate-decision scope for
/// DIRECT callers of the public virtual method (third-party or future engine callers that never
/// pass through RunManager.FinalizeStartingRelics or RelicCmd.Obtain). Without it those callers
/// would read the live gate at every point of the body and could observe a mid-call settings
/// toggle. The scope patch runs at the highest prefix priority so the frozen decision exists
/// before the r7 safety prefix and the primary replacement prefix evaluate anything, and its
/// finalizer restores the caller context after the kickoff returns. The async body's await
/// continuations captured the entered AsyncLocal value, so the whole operation keeps one
/// decision (normal, faulted or canceled completion).
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleAfterObtainedScopePatch
{
    private static bool _targetResolved;
    private static MethodInfo? _target;
    private static bool _targetVerified;

    private static MethodInfo? Target
    {
        get
        {
            if (!_targetResolved)
            {
                _targetResolved = true;
                _target = Resolve();
            }
            return _target;
        }
    }

    private static MethodInfo? Resolve()
    {
        try
        {
            MethodInfo[] candidates = AccessTools.GetDeclaredMethods(typeof(LargeCapsule))
                .Where(m => m.Name == nameof(LargeCapsule.AfterObtained))
                .ToArray();
            if (candidates.Length != 1)
            {
                return null;
            }

            MethodInfo method = candidates[0];
            return Spire1LargeCapsuleGate.IsExpectedAfterObtainedSignature(method) ? method : null;
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AfterObtained scope patch NOT mounted: target resolution " +
                $"threw ({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AfterObtained scope patch NOT mounted: target was missing, " +
                "ambiguous or drifted; direct AfterObtained calls have no frozen gate decision.");
            return false;
        }
        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordAfterObtainedScopePatchMounted();
            return;
        }
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule AfterObtained scope patch NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    // Priority.First (800) runs before the r7 safety boundary's Priority.First because this
    // patch is applied later and Harmony orders equal priorities by patch index (the later
    // index sorts first). The scope must exist before any gate prefix reads the snapshot.
    // Only the three Spire1 placeholder identities enter the operation scope; vanilla and
    // other-mod characters keep the original engine behavior byte-for-byte. 900 is above
    // Priority.First (800) so the scope prefix always runs before the r7 safety boundary's
    // Priority.First prefix regardless of patch index tie-breaking.
    [HarmonyPriority(900)]
    [HarmonyPrefix]
    private static void EnterScope(LargeCapsule __instance, out Spire1CardsGateSnapshot.SnapshotToken __state)
    {
        bool entered = false;
        try
        {
            CharacterModel? character = __instance?.Owner?.Character;
            entered = character is not null && Spire1LargeCapsuleGate.IsSpire1Character(character);
        }
        catch (Exception)
        {
            // An abnormal owner shape is handled (fail closed) by the gate prefixes; the scope
            // itself stays inactive so the exception is reported there, not masked here.
            entered = false;
        }

        __state = entered
            ? Spire1CardsGateSnapshot.EnterScoped(Spire1Config.LiveCardsGateClosed)
            : default;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyFinalizer]
    private static Exception? ExitScope(Exception? __exception, Spire1CardsGateSnapshot.SnapshotToken __state)
    {
        // The kickoff prefix entered the scope in the caller's execution context. The async
        // body captured that context; restoring here removes the nested depth from the caller
        // so no other call on the same context observes the operation scope. A default token
        // (scope not entered, e.g. vanilla owner) restores to a no-op.
        Spire1CardsGateSnapshot.Restore(__state);
        return __exception;
    }
}

/// <summary>
/// Secondary layer, single target: prefix on the private GetStrikeForCharacter helper. Split
/// from the Defend helper on purpose: Harmony 2.4.2 applies only the last method-level target of
/// a multi-target class (repository offline reproduction), so each helper owns its own class and
/// its own [HarmonyPrepare]/[HarmonyCleanup] install state. A drift or install failure in this
/// class can never disable the Defend class or the primary layer.
/// </summary>
[HarmonyPatch(typeof(LargeCapsule), "GetStrikeForCharacter")]
internal static class Spire1LargeCapsuleStrikeHelperPatch
{
    private static bool _targetVerified;

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        MethodInfo? strike;
        try
        {
            strike = AccessTools.DeclaredMethod(typeof(LargeCapsule), "GetStrikeForCharacter");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule strike helper gate NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}).");
            return false;
        }

        if (!Spire1LargeCapsuleGate.IsExpectedHelperSignature(strike))
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule strike helper gate NOT mounted: engine target drifted " +
                $"(GetStrikeForCharacter={Spire1LargeCapsuleGate.Describe(strike)}; " +
                "expected private static CardModel GetStrikeForCharacter(CharacterModel)).");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordStrikeHelperMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule strike helper gate NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    // __0 = the verified character parameter of the engine helper. Positional binding keeps
    // working if the engine only renames the parameter; a type or order change fails the mount
    // above (fail closed) instead of binding a wrong value.
    [HarmonyPrefix]
    private static bool ReplaceStrike(CharacterModel __0, ref CardModel __result)
        => Spire1LargeCapsuleGate.TryReplaceBasic(
            __0, CardTag.Strike, ref __result);
}

/// <summary>
/// Secondary layer, single target: prefix on the private GetDefendForCharacter helper. See the
/// Strike class for why the two helpers are separate classes. A drift or install failure in this
/// class can never disable the Strike class or the primary layer.
/// </summary>
[HarmonyPatch(typeof(LargeCapsule), "GetDefendForCharacter")]
internal static class Spire1LargeCapsuleDefendHelperPatch
{
    private static bool _targetVerified;

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        MethodInfo? defend;
        try
        {
            defend = AccessTools.DeclaredMethod(typeof(LargeCapsule), "GetDefendForCharacter");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule defend helper gate NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}).");
            return false;
        }

        if (!Spire1LargeCapsuleGate.IsExpectedHelperSignature(defend))
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule defend helper gate NOT mounted: engine target drifted " +
                $"(GetDefendForCharacter={Spire1LargeCapsuleGate.Describe(defend)}; " +
                "expected private static CardModel GetDefendForCharacter(CharacterModel)).");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordDefendHelperMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule defend helper gate NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyPrefix]
    private static bool ReplaceDefend(CharacterModel __0, ref CardModel __result)
        => Spire1LargeCapsuleGate.TryReplaceBasic(
            __0, CardTag.Defend, ref __result);
}

/// <summary>
/// Fail-closed backstop, single target: prefix on the non-generic RelicCmd.Obtain. While the
/// cards group is closed, obtaining a LargeCapsule for one of the three Spire1 placeholder
/// characters is blocked with an Error and a faulted task unless the primary AfterObtained layer
/// is proven mounted. This closes every "primary target drifted / signature ambiguous / install
/// failed" path before the engine can reach character.CardPool.AllCards; the blocked relic is
/// never reported as granted. Vanilla characters, other mods' characters and every
/// non-LargeCapsule relic pass through untouched.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleObtainFunnelPatch
{
    private static readonly MethodInfo? Target = Resolve();
    private static bool _targetVerified;

    private static MethodInfo? Resolve()
    {
        try
        {
            return typeof(RelicCmd)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(m => m.Name == nameof(RelicCmd.Obtain)
                    && m.DeclaringType == typeof(RelicCmd)
                    && !m.IsGenericMethod
                    && m.IsPublic
                    && m.ReturnType == typeof(Task<RelicModel>)
                    && m.GetParameters() is [var a, var b, var c]
                    && a.ParameterType == typeof(RelicModel)
                    && b.ParameterType == typeof(Player)
                    && c.ParameterType == typeof(int));
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule Obtain funnel NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule Obtain funnel NOT mounted: " +
                "RelicCmd.Obtain(RelicModel, Player, int) was not found or was ambiguous; " +
                "the fail-closed backstop is unavailable.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordFunnelGateMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule Obtain funnel NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPrefix]
    private static bool Prefix(RelicModel relic, Player player, ref Task<RelicModel> __result)
    {
        // Open group: original engine behavior, untouched for every relic and character.
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        if (relic is not LargeCapsule)
        {
            return true;
        }

        // C14 r12: a null player or an unreadable/null Character is an abnormal shape for a
        // LargeCapsule obtain; fail closed instead of letting the engine dereference it.
        if (player is null)
        {
            Spire1LargeCapsuleGate.LogFunnelBlockedOnce(null);
            __result = Task.FromException<RelicModel>(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic: the player argument was " +
                "null while the cards content group is off."));
            return false;
        }

        CharacterModel? character;
        try
        {
            character = player.Character;
        }
        catch (Exception e)
        {
            Spire1LargeCapsuleGate.LogFunnelBlockedOnce(null);
            __result = Task.FromException<RelicModel>(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic: the player Character " +
                $"could not be read safely ({e.GetType().Name}) while the cards content group is off.", e));
            return false;
        }

        if (character is null)
        {
            Spire1LargeCapsuleGate.LogFunnelBlockedOnce(null);
            __result = Task.FromException<RelicModel>(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic: the player Character was " +
                "null while the cards content group is off."));
            return false;
        }

        if (!Spire1LargeCapsuleGate.IsSpire1Character(character))
        {
            return true;
        }

        // Complete closed-gate replacement proven mounted: the AfterObtained layer replaces the
        // whole engine grant and the AddRelicInternal early guard also won the new-run race, so
        // let the obtain continue. Any runtime failure of those layers throws and is never a
        // silent AllCards fallback.
        if (Spire1LargeCapsuleGate.HasCompleteClosedGateReplacement)
        {
            return true;
        }

        // Fail closed: no complete replacement is proven, so the engine AfterObtained body would
        // be free to read character.CardPool.AllCards. Block the obtain explicitly.
        Spire1LargeCapsuleGate.LogFunnelBlockedOnce(character);
        __result = Task.FromException<RelicModel>(new InvalidOperationException(
            "[Spire1] LargeCapsule gate refused to obtain the relic: no complete closed-gate " +
            "replacement is proven mounted and the cards content group is off; the engine " +
            "AllCards grant is never reached."));
        return false;
    }
}
/// <summary>
/// C14 r6 final line, single target: prefix on the public RunManager.FinalizeStartingRelics()
/// instance method. The new-run chain Player.CreateForNewRun -> PopulateStartingRelics ->
/// PopulateRelics -> AddRelicInternal adds starting relics directly, without going through
/// RelicCmd.Obtain; FinalizeStartingRelics then iterates player.Relics and calls
/// relic.AfterObtained() directly (RunManager.cs:725-738). While the cards group is closed and
/// no complete closed-gate replacement is proven, a Spire1 placeholder player holding a
/// LargeCapsule is refused with a faulted task before the engine body runs, so the original
/// AllCards read is never reached. The private RunManager.State property is read strictly by
/// reflection; a missing getter, type drift or read exception fails closed rather than assuming
/// the run is safe. When no Spire1 placeholder holds a LargeCapsule, or the cards group is open,
/// the original method runs untouched. Target resolution and [HarmonyCleanup] follow the same
/// mount discipline as the other layers: missing, ambiguous, drifted or throwing installs never
/// record mounted=true.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleFinalizeStartingRelicsPatch
{
    private static bool _targetResolved;
    private static MethodInfo? _target;
    private static bool _targetVerified;

    // Lazy: a throwing static initializer would abort the type's initialization and Harmony
    // would never call [HarmonyPrepare]/[HarmonyCleanup], so a target-resolution failure could
    // not be logged and mount state could not stay false in a well-defined way.
    private static MethodInfo? Target
    {
        get
        {
            if (!_targetResolved)
            {
                _targetResolved = true;
                _target = Resolve();
            }

            return _target;
        }
    }

    private static MethodInfo? Resolve()
    {
        try
        {
            return typeof(RunManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(m => m.Name == nameof(RunManager.FinalizeStartingRelics)
                    && m.DeclaringType == typeof(RunManager)
                    && !m.IsStatic
                    && !m.IsGenericMethod
                    && m.IsPublic
                    && m.ReturnType == typeof(Task)
                    && m.GetParameters().Length == 0);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule FinalizeStartingRelics gate NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule FinalizeStartingRelics gate NOT mounted: " +
                "RunManager.FinalizeStartingRelics() was not found or was ambiguous; " +
                "the final fail-closed guard is unavailable.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordFinalizeStartingRelicsGateMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule FinalizeStartingRelics gate NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPrefix]
    private static bool Prefix(ref Task __result)
    {
        // Open group: original engine behavior, untouched for every run and character.
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        // The guard above already proves the closed-gate replacement for every other layer;
        // while it is mounted, FinalizeStartingRelics may run the original body.
        if (Spire1LargeCapsuleGate.HasCompleteClosedGateReplacement)
        {
            return true;
        }

        CharacterModel? holder;
        try
        {
            holder = Spire1LargeCapsuleGate.FindSpire1LargeCapsuleHolder(RunManager.Instance);
        }
        catch (Exception e)
        {
            Spire1LargeCapsuleGate.LogFinalizeStartingRelicsReadFailedOnce(
                $"{e.GetType().Name}: {e.Message}");
            __result = Task.FromException(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to finalize starting relics: the run state " +
                "could not be read safely while the cards content group is off; the engine " +
                "AllCards grant is never reached.", e));
            return false;
        }

        if (holder is null)
        {
            return true;
        }

        string missing =
            $"afterObtained={Spire1LargeCapsuleGate.AfterObtainedGateMounted}, " +
            $"strikeHelper={Spire1LargeCapsuleGate.StrikeHelperMounted}, " +
            $"defendHelper={Spire1LargeCapsuleGate.DefendHelperMounted}, " +
            $"funnel={Spire1LargeCapsuleGate.FunnelGateMounted}, " +
            $"earlyGuard={Spire1LargeCapsuleGate.AddRelicInternalGuardMounted}, " +
            $"finalizeStartingRelics={Spire1LargeCapsuleGate.FinalizeStartingRelicsGateMounted}";
        Spire1LargeCapsuleGate.LogFinalizeStartingRelicsBlockedOnce(holder, missing);
        __result = Task.FromException(new InvalidOperationException(
            "[Spire1] LargeCapsule gate refused to finalize starting relics: no complete " +
            "closed-gate replacement is proven mounted while a Spire1 placeholder holds a " +
            "LargeCapsule and the cards content group is off; the engine AllCards grant is never reached."));
        return false;
    }
}

/// <summary>
/// C14 r3 earliest entry guard, single target: prefix on
/// Player.AddRelicInternal(RelicModel, int, bool). RelicCmd.Obtain calls this before
/// relic.AfterObtained (RelicCmd.cs:40 before :53), so throwing here stops the obtain before the
/// relic is added to the player's relic list and before the grab-bag / UI / save / floor /
/// AfterObtained steps run; character.CardPool.AllCards is therefore never read. Note the map
/// point history entry at RelicCmd.cs:39 is written before this call and is not rolled back.
/// The guard only refuses when the cards group is closed, the relic is LargeCapsule, the player
/// is one of the three Spire1 placeholder characters, the call is not a proven existing-relic
/// load (the exact RelicModel instance marked by the FromSerializable or
/// SyncWithSerializedPlayer path), and no complete AllCards-free replacement is proven (neither
/// the AfterObtained prefix alone nor both helper prefixes together). Vanilla characters, other
/// mods' characters and every non-LargeCapsule relic pass through untouched.
/// <para>
/// If this class itself cannot be installed the four later layers still cover their own drift
/// cases, but this earliest guard is then unavailable; the r3 report records that boundary
/// explicitly instead of claiming a closed gate.
/// </para>
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleAddRelicInternalPatch
{
    private static readonly MethodInfo? Target = Resolve();
    private static bool _targetVerified;

    private static MethodInfo? Resolve()
    {
        try
        {
            return AccessTools.GetDeclaredMethods(typeof(Player))
                .SingleOrDefault(m => m.Name == nameof(Player.AddRelicInternal)
                    && !m.IsStatic
                    && m.ReturnType == typeof(void)
                    && m.GetParameters() is [var a, var b, var c]
                    && a.ParameterType == typeof(RelicModel)
                    && b.ParameterType == typeof(int)
                    && c.ParameterType == typeof(bool));
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AddRelicInternal early guard NOT mounted: target resolution threw " +
                $"({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule AddRelicInternal early guard NOT mounted: " +
                "Player.AddRelicInternal(RelicModel, int, bool) was not found or was ambiguous; " +
                "the earliest fail-closed entry is unavailable.");
            return false;
        }

        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }

        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordAddRelicInternalGuardMounted();
            return;
        }

        MainFile.Logger.Error(
            "[Spire1] LargeCapsule AddRelicInternal early guard NOT mounted: installation threw " +
            $"({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    // __0 = relic, __2 = silent (positional binding keeps working if the engine only renames the
    // parameters; a type or order change fails the mount above instead of binding a wrong value).
    [HarmonyPrefix]
    private static bool Prefix(Player __instance, RelicModel __0, bool __2)
    {
        // Open group: original engine behavior, untouched for every relic and character.
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        // Both existing-relic paths are identified by an exact RelicModel instance emitted by
        // RelicModel.FromSerializable while either Player.FromSerializable or
        // Player.SyncWithSerializedPlayer is active. Do not trust the public silent flag by
        // itself: a reentrant/new direct AddRelicInternal call with silent:true must not borrow
        // the multiplayer load boundary.
        if (__0 is not LargeCapsule)
        {
            return true;
        }

        if (Spire1LargeCapsuleGate.TryConsumeDeserializedRelic(__0))
        {
            return true;
        }

        CharacterModel? character;
        try
        {
            character = __instance?.Character;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic at Player.AddRelicInternal: " +
                $"the player Character could not be read safely ({e.GetType().Name}).", e);
        }

        if (character is null)
        {
            throw new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic at Player.AddRelicInternal: " +
                "the player Character was null while the cards content group is off.");
        }

        if (!Spire1LargeCapsuleGate.IsSpire1Character(character))
        {
            return true;
        }

        // Complete closed replacement proven mounted: the AfterObtained prefix removes the whole
        // engine method body and both helper prefixes are installed, so the obtain may continue.
        if (Spire1LargeCapsuleGate.HasCompleteClosedGateReplacement)
        {
            return true;
        }

        // Fail closed before the relic is added and long before AfterObtained: throw from this
        // void prefix. Harmony emits the prefix call directly, so the exception propagates out
        // of AddRelicInternal; RelicCmd.Obtain then faults before its first await and the engine
        // AllCards grant is never reached. This also closes the "four layers all drifted" case.
        string missing =
            $"afterObtained={Spire1LargeCapsuleGate.AfterObtainedGateMounted}, " +
            $"strikeHelper={Spire1LargeCapsuleGate.StrikeHelperMounted}, " +
            $"defendHelper={Spire1LargeCapsuleGate.DefendHelperMounted}, " +
            $"funnel={Spire1LargeCapsuleGate.FunnelGateMounted}, " +
            $"earlyGuard={Spire1LargeCapsuleGate.AddRelicInternalGuardMounted}";
        Spire1LargeCapsuleGate.LogEarlyGuardBlockedOnce(character, missing);
        throw new InvalidOperationException(
            "[Spire1] LargeCapsule gate refused to obtain the relic at Player.AddRelicInternal: " +
            "the closed-gate replacement is incomplete and the engine AllCards grant is never reached.");
    }
}

/// <summary>
/// C14 r8b independent lifecycle boundary, single target: the public
/// RunManager.FinalizeStartingRelics() instance method. This class does NOT target
/// LargeCapsule.AfterObtained and does not depend on the r7 safety patch's target resolving: it
/// targets a different engine method, so it still installs (and still blocks) when the safety
/// target is missing, ambiguous, drifted, its Prepare returns false or its installation throws.
/// When the cards group is closed and a Spire1 placeholder player already holds a LargeCapsule,
/// the new-run finalize is refused unless the r6 closed-gate replacement proof is complete.
/// (r10: the Spire1Card.Tags total fuse additionally makes the engine's own AllCards predicate
/// fail closed even when this lifecycle boundary is also unmounted.) That covers the supervisor's
/// Y=0,A=0,F=0,P=0,S=0,D=1/0 counterexamples at the direct FinalizeStartingRelics call chain:
/// even with the primary, both helpers, the AddRelicInternal guard and the existing Finalize
/// guard all unmounted, this prefix returns a faulted Task before the engine body can call
/// relic.AfterObtained(). Vanilla characters, other mods' characters and the cards-open path
/// return true untouched. The reflection read of the private RunManager.State is the same strict
/// read used by the r6 layer; a read failure is fail-closed.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleLifecycleFinalizeGatePatch
{
    private static bool _targetResolved;
    private static MethodInfo? _target;
    private static bool _targetVerified;

    // Lazy: a throwing static initializer would abort type initialization before Harmony can call
    // [HarmonyPrepare]/[HarmonyCleanup], leaving the mount state undefined.
    private static MethodInfo? Target
    {
        get
        {
            if (!_targetResolved)
            {
                _targetResolved = true;
                _target = Resolve();
            }
            return _target;
        }
    }

    private static MethodInfo? Resolve()
    {
        try
        {
            return typeof(RunManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(m => m.Name == nameof(RunManager.FinalizeStartingRelics)
                    && m.DeclaringType == typeof(RunManager)
                    && !m.IsStatic
                    && !m.IsGenericMethod
                    && m.IsPublic
                    && m.ReturnType == typeof(Task)
                    && m.GetParameters().Length == 0);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule lifecycle FinalizeStartingRelics gate NOT mounted: " +
                $"target resolution threw ({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule lifecycle FinalizeStartingRelics gate NOT mounted: " +
                "RunManager.FinalizeStartingRelics() was not found or was ambiguous; the " +
                "independent new-run fail-closed boundary is unavailable.");
            return false;
        }
        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordLifecycleFinalizeGateMounted();
            return;
        }
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule lifecycle FinalizeStartingRelics gate NOT mounted: " +
            $"installation threw ({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    // Priority.First: this independent boundary runs before the r6 Finalize guard and before any
    // other prefix, so its refusal cannot be pre-empted by a later prefix returning true.
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool Prefix(RunManager __instance, ref Task __result, out bool __state)
    {
        // C14 r11: open the operation scope and take the ONE gate decision for this call before
        // anything else. AsyncLocal flows into await continuations, so the engine body after an
        // await reads the same value and a concurrent settings toggle cannot split the call.
        Spire1CardsGateSnapshot.EnterLive();
        __state = true;
        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        if (__instance is null)
        {
            const string detail = "instance=null";
            Spire1LargeCapsuleGate.LogLifecycleFinalizeBlockedOnce(detail);
            __result = Task.FromException(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to finalize starting relics: the RunManager " +
                "instance was null while the cards content group is off."));
            return false;
        }

        CharacterModel? holder;
        try
        {
            holder = Spire1LargeCapsuleGate.FindSpire1LargeCapsuleHolder(__instance);
        }
        catch (Exception e)
        {
            string detail = $"{e.GetType().Name}: {e.Message}";
            Spire1LargeCapsuleGate.LogLifecycleFinalizeBlockedOnce(detail);
            __result = Task.FromException(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to finalize starting relics: the run state " +
                "could not be read safely while the cards content group is off.", e));
            return false;
        }

        if (holder is null)
        {
            return true;
        }

        if (Spire1LargeCapsuleGate.HasClosedGateReplacementProofSnapshot(out string proof))
        {
            return true;
        }

        Spire1LargeCapsuleGate.LogLifecycleFinalizeBlockedOnce(proof);
        __result = Task.FromException(new InvalidOperationException(
            "[Spire1] LargeCapsule gate refused to finalize starting relics: no independent " +
            $"closed-gate replacement proof is available ({proof}) while {holder.GetType().Name} " +
            "holds a LargeCapsule and the cards content group is off; the engine AllCards grant is " +
            "never reached."));
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyFinalizer]
    private static Exception? ExitSnapshot(Exception? __exception, bool __state)
    {
        if (__state)
        {
            Spire1CardsGateSnapshot.Exit();
        }
        return __exception;
    }
}


/// <summary>
/// C14 r8b independent lifecycle boundary, single target: RelicCmd.Obtain(RelicModel, Player, int).
/// Like the Finalize lifecycle gate above, this class targets a different method than the r7 safety
/// boundary and does not depend on LargeCapsule.AfterObtained resolving: it still installs and still
/// blocks when the safety target is missing, ambiguous, drifted, its Prepare returns false or its
/// installation throws. When the cards group is closed and a Spire1 placeholder player tries to
/// obtain a LargeCapsule, the obtain is refused unless the r6 closed-gate replacement proof is
/// complete. This covers the
/// Y=0,A=0,F=0,P=0,S=0,D=1/0 counterexamples on the RelicCmd.Obtain call chain (including mid-run
/// obtains in old saves and multiplayer runs). Vanilla characters, other mods' characters, every
/// non-LargeCapsule relic and the cards-open path return true untouched.
/// </summary>
[HarmonyPatch]
internal static class Spire1LargeCapsuleObtainLifecycleGatePatch
{
    private static bool _targetResolved;
    private static MethodInfo? _target;
    private static bool _targetVerified;

    private static MethodInfo? Target
    {
        get
        {
            if (!_targetResolved)
            {
                _targetResolved = true;
                _target = Resolve();
            }
            return _target;
        }
    }

    private static MethodInfo? Resolve()
    {
        try
        {
            return typeof(RelicCmd)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(m => m.Name == nameof(RelicCmd.Obtain)
                    && m.DeclaringType == typeof(RelicCmd)
                    && !m.IsGenericMethod
                    && m.IsPublic
                    && m.ReturnType == typeof(Task<RelicModel>)
                    && m.GetParameters() is [var a, var b, var c]
                    && a.ParameterType == typeof(RelicModel)
                    && b.ParameterType == typeof(Player)
                    && c.ParameterType == typeof(int));
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule lifecycle RelicCmd.Obtain gate NOT mounted: " +
                $"target resolution threw ({e.GetType().Name}: {e.Message}).");
            return null;
        }
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        _targetVerified = false;
        if (Target is null)
        {
            MainFile.Logger.Error(
                "[Spire1] LargeCapsule lifecycle RelicCmd.Obtain gate NOT mounted: " +
                "RelicCmd.Obtain(RelicModel, Player, int) was not found or was ambiguous; the " +
                "independent obtain fail-closed boundary is unavailable.");
            return false;
        }
        _targetVerified = true;
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (!_targetVerified)
        {
            return;
        }
        if (__exception is null)
        {
            Spire1LargeCapsuleGate.RecordLifecycleObtainGateMounted();
            return;
        }
        MainFile.Logger.Error(
            "[Spire1] LargeCapsule lifecycle RelicCmd.Obtain gate NOT mounted: " +
            $"installation threw ({__exception.GetType().Name}: {__exception.Message}).");
    }

    [HarmonyTargetMethod]
    private static MethodInfo TargetMethod() => Target!;

    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool Prefix(RelicModel relic, Player player, ref Task<RelicModel> __result, out bool __state)
    {
        __state = false;

        // Hot path: every relic obtain. Non-LargeCapsule obtains pay only this type check and
        // never touch the snapshot scope; the config and character checks run only for the one
        // relic type this gate protects.
        if (relic is not LargeCapsule)
        {
            return true;
        }

        // C14 r12: a null player is an abnormal shape for a LargeCapsule obtain. The engine
        // would dereference it immediately; fail closed with a faulted task instead.
        if (player is null)
        {
            Spire1LargeCapsuleGate.LogLifecycleObtainBlockedOnce(null, "player=null");
            __result = Task.FromException<RelicModel>(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic at RelicCmd.Obtain: the " +
                "player argument was null while the cards content group is off."));
            return false;
        }

        // C14 r11: freeze one gate decision for this whole obtain. The scope also covers the
        // awaited engine continuation (AsyncLocal); __state makes the finalizer exit only when
        // this invocation actually entered, so a nested non-LargeCapsule obtain inside an outer
        // lifecycle scope cannot decrement the outer scope.
        Spire1CardsGateSnapshot.EnterLive();
        __state = true;

        if (!Spire1Config.CardsGateClosedThisRun)
        {
            return true;
        }

        CharacterModel? character;
        try
        {
            character = player.Character;
        }
        catch (Exception e)
        {
            Spire1LargeCapsuleGate.LogLifecycleObtainBlockedOnce(null, $"character threw {e.GetType().Name}");
            __result = Task.FromException<RelicModel>(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic at RelicCmd.Obtain: the " +
                "player Character could not be read safely while the cards content group is off.", e));
            return false;
        }

        if (character is null)
        {
            Spire1LargeCapsuleGate.LogLifecycleObtainBlockedOnce(null, "player.Character was null");
            __result = Task.FromException<RelicModel>(new InvalidOperationException(
                "[Spire1] LargeCapsule gate refused to obtain the relic at RelicCmd.Obtain: the " +
                "player Character was null while the cards content group is off."));
            return false;
        }

        if (!Spire1LargeCapsuleGate.IsSpire1Character(character))
        {
            return true;
        }

        if (Spire1LargeCapsuleGate.HasClosedGateReplacementProofSnapshot(out string proof))
        {
            return true;
        }

        Spire1LargeCapsuleGate.LogLifecycleObtainBlockedOnce(character, proof);
        __result = Task.FromException<RelicModel>(new InvalidOperationException(
            "[Spire1] LargeCapsule gate refused to obtain the relic at RelicCmd.Obtain: no " +
            $"independent closed-gate replacement proof is available ({proof}) while " +
            $"{character.GetType().Name} is a Spire1 placeholder and the cards content group is off; " +
            "the engine AllCards grant is never reached."));
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyFinalizer]
    private static Exception? ExitSnapshot(Exception? __exception, bool __state)
    {
        if (__state)
        {
            Spire1CardsGateSnapshot.Exit();
        }
        return __exception;
    }
}
