using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Forms;

#if SPIRE1_FORM_MOD && !GODOT
// The isolated form probe has a metadata-only Harmony surface and no real Harmony assembly.
// The production build resolves [HarmonyTargetMethod] from the referenced Harmony 2.4.2 assembly.
[AttributeUsage(AttributeTargets.Method)]
internal sealed class HarmonyTargetMethodAttribute : Attribute { }
#endif

/// <summary>
/// Carries one real card payment from SpendResources to OnPlayWrapper.
///
/// CardModel.SpendResources is async. Harmony runs a postfix when that method returns its Task,
/// not when the payment Task completes. A reservation therefore cannot be released by the raw
/// postfix. Native PlayCardAction.ExecuteAction is used as the explicit action owner: its async
/// execution context carries the exact SpendToken into the later OnPlayWrapper call.
/// </summary>
internal static class VoidFormPlayTransaction
{
    internal sealed class ActionState
    {
        internal required object Action { get; init; }
        internal CardModel? ExpectedCard { get; set; }
        internal SpendToken? Spend { get; set; }
        internal ActionState? PreviousAction { get; init; }
        internal bool Closed { get; set; }
    }

    internal sealed class SpendToken
    {
        internal required CardModel Card { get; init; }
        internal VoidFormEffectPower? PowerAtStart { get; init; }
        internal VoidFormEffectPower? BlockedPower { get; set; }
        internal ActionState? Action { get; set; }
        internal bool Reserved { get; set; }
        internal bool SpendCompleted { get; set; }
        internal bool OnPlayStarted { get; set; }
        internal bool BeforePlayStarted { get; set; }
        internal bool Cancelled { get; set; }
        internal SpendToken? PreviousSpend { get; set; }
        internal bool SpendInvocationClosed { get; set; }
    }

    internal sealed class PlayState
    {
        internal required CardModel Card { get; init; }
        internal SpendToken? Spend { get; init; }
        internal ActionState? Action { get; init; }
        internal bool IsAutoPlay { get; init; }
        internal VoidFormEffectPower? BlockedPower { get; set; }
        internal PlayState? PreviousPlay { get; init; }
        internal CardPlay? BeforeCardPlay { get; set; }
        internal bool ContextClosed { get; set; }
    }

    private sealed class TokenBucket
    {
        internal readonly List<SpendToken> Items = new();
    }

    private sealed class ProbeActionLease
    {
        internal required ActionState State { get; init; }
        internal bool OwnsState { get; init; }
    }

#if SPIRE1_FORM_MOD && !GODOT
    // Probe-only identity accepted by the production action bridge. The probe calls the same
    // CancelNativeActionForPatch/ObserveNativeActionCompletionForPatch methods instead of copying
    // their cleanup algorithm or pretending that ProbeEndAction is a native action callback.
    private sealed class ProbeNativeActionIdentity { }
#endif

    private static readonly ConditionalWeakTable<CardModel, TokenBucket> Tokens = new();
    private static readonly ConditionalWeakTable<Creature, TokenBucket> OwnerTokens = new();
    private static readonly ConditionalWeakTable<VoidFormEffectPower, TokenBucket> PowerTokens = new();
    private static readonly ConditionalWeakTable<object, ActionState> Actions = new();
    private static readonly AsyncLocal<SpendToken?> CurrentSpend = new();
    private static readonly AsyncLocal<ActionState?> CurrentAction = new();
    private static readonly AsyncLocal<PlayState?> CurrentPlay = new();

    internal static SpendToken BeginSpend(CardModel card)
    {
        VoidFormEffectPower? power = card.Owner.Creature.GetPower<VoidFormEffectPower>();
        ActionState? action = GetCurrentActionFor(card);
        var token = new SpendToken
        {
            Card = card,
            PowerAtStart = power,
            Reserved = false,
            PreviousSpend = CurrentSpend.Value,
            Action = action
        };
        CurrentSpend.Value = token;

        // The native PlayCardAction path is one action -> one payment. If an unrelated nested
        // spend happens inside that action, refuse to attach it to the action instead of
        // guessing which payment the later wrapper belongs to.
        if (action != null && action.Spend == null)
        {
            action.ExpectedCard = card;
            action.Spend = token;
        }
        else
        {
            token.Action = null;
        }

        if (power != null)
        {
            token.Reserved = power.TryReserveFor(card, token);
        }
        TokenBucket bucket = Tokens.GetValue(card, static _ => new TokenBucket());
        lock (bucket)
        {
            bucket.Items.Add(token);
        }

        TokenBucket ownerBucket = OwnerTokens.GetValue(card.Owner.Creature, static _ => new TokenBucket());
        lock (ownerBucket)
        {
            ownerBucket.Items.Add(token);
        }

        if (power != null)
        {
            TokenBucket powerBucket = PowerTokens.GetValue(power, static _ => new TokenBucket());
            lock (powerBucket)
            {
                powerBucket.Items.Add(token);
            }
        }
        return token;
    }

    internal static void EndSpendInvocation(SpendToken token)
    {
        if (token.SpendInvocationClosed)
        {
            return;
        }

        token.SpendInvocationClosed = true;
        if (ReferenceEquals(CurrentSpend.Value, token))
        {
            CurrentSpend.Value = token.PreviousSpend;
        }
    }

    internal static bool HasCurrentSpendFor(CardModel card)
    {
        SpendToken? token = CurrentSpend.Value;
        return token != null && ReferenceEquals(token.Card, card);
    }

    internal static bool IsCurrentSpendFor(CardModel card, VoidFormEffectPower power)
    {
        SpendToken? token = CurrentSpend.Value;
        return token != null
            && ReferenceEquals(token.Card, card)
            && ReferenceEquals(token.PowerAtStart, power)
            && token.Reserved;
    }

    internal static Task<(int, int)> ObserveSpendCompletion(Task<(int, int)> result, SpendToken token)
        => ObserveSpendCompletionAsync(result, token);

    private static async Task<(int, int)> ObserveSpendCompletionAsync(
        Task<(int, int)> result,
        SpendToken token)
    {
        try
        {
            (int, int) resources = await result;
            MarkSpendCompleted(token);
            return resources;
        }
        catch
        {
            Cancel(token);
            throw;
        }
    }

    internal static void Cancel(SpendToken token)
    {
        if (Tokens.TryGetValue(token.Card, out TokenBucket? bucket))
        {
            lock (bucket)
            {
                token.Cancelled = true;
            }
        }
        else
        {
            token.Cancelled = true;
        }
        token.PowerAtStart?.ClearFailedPlay(token.Card, token, rollbackConsumed: false);
        token.BlockedPower?.ClearFailedPlay(token.Card, token, rollbackConsumed: false);
        Remove(token);
    }

    internal static PlayState BeginPlay(CardModel card, bool isAutoPlay)
    {
        ActionState? action = GetCurrentActionFor(card);
        SpendToken? token = ClaimForPlay(card, action, isAutoPlay);
        bool ownsReservation = token?.Reserved == true
            && token.PowerAtStart != null
            && token.PowerAtStart.IsReservationFor(card, token);
        var state = new PlayState
        {
            Card = card,
            Spend = token,
            Action = action,
            IsAutoPlay = isAutoPlay,
            PreviousPlay = GetActivePlay()
        };
        CurrentPlay.Value = state;

        if (isAutoPlay)
        {
            if (token != null)
            {
                Cancel(token);
            }
            return state;
        }

        if (ownsReservation)
        {
            return state;
        }

        // A manual wrapper without the exact action handoff is not allowed to consume a Void
        // power gained during payment, or a power already reserved by another card. Completed
        // unclaimed generations are cancelled here so a stale token cannot block the next one.
        if (token?.PowerAtStart != null && token.Reserved)
        {
            token.PowerAtStart.ReleaseReservation(card, token);
        }
        VoidFormEffectPower? current = card.Owner.Creature.GetPower<VoidFormEffectPower>();
        if (current != null)
        {
            current.BlockCard(card, token);
            state.BlockedPower = current;
        }
        return state;
    }

    internal static Task ObservePlayCompletion(Task result, PlayState state)
        => ObservePlayCompletionAsync(result, state);

    private static async Task ObservePlayCompletionAsync(Task result, PlayState state)
    {
        try
        {
            await result;
            CompletePlay(state);
        }
        catch
        {
            AbortForPatch(state);
            throw;
        }
        finally
        {
            ClosePlayContext(state);
        }
    }

    internal static SpendToken? ClaimForBeforeCardPlayed(
        CardPlay cardPlay,
        VoidFormEffectPower power)
    {
        PlayState? state = GetCurrentPlayFor(cardPlay.Card);
        if (state == null
            || state.IsAutoPlay
            || state.Action == null
            || state.Spend == null
            || state.BeforeCardPlay != null)
        {
            return null;
        }

        SpendToken token = state.Spend;
        if (token.Cancelled
            || !token.SpendCompleted
            || !token.OnPlayStarted
            || token.BeforePlayStarted
            || !token.Reserved
            || !ReferenceEquals(token.Action, state.Action)
            || !ReferenceEquals(token.PowerAtStart, power)
            || !power.IsReservationFor(cardPlay.Card, token))
        {
            return null;
        }

        token.BeforePlayStarted = true;
        state.BeforeCardPlay = cardPlay;
        return token;
    }

    private static ActionState? GetActiveAction()
    {
        ActionState? state = CurrentAction.Value;
        while (state != null && state.Closed)
        {
            state = state.PreviousAction;
        }
        return state;
    }

    private static ActionState? GetCurrentActionFor(CardModel card)
    {
        ActionState? state = GetActiveAction();
        if (state == null)
        {
            return null;
        }
        return state.ExpectedCard == null || ReferenceEquals(state.ExpectedCard, card)
            ? state
            : null;
    }

    private static PlayState? GetActivePlay()
    {
        PlayState? state = CurrentPlay.Value;
        while (state != null && state.ContextClosed)
        {
            state = state.PreviousPlay;
        }
        return state;
    }

    private static PlayState? GetCurrentPlayFor(CardModel card)
    {
        PlayState? state = GetActivePlay();
        return state != null && ReferenceEquals(state.Card, card) ? state : null;
    }

    internal static void ClosePlayContext(PlayState state)
    {
        state.ContextClosed = true;
        if (ReferenceEquals(CurrentPlay.Value, state))
        {
            CurrentPlay.Value = state.PreviousPlay;
        }
        if (state.Action != null)
        {
            CloseAction(state.Action);
        }
    }

    internal static void AdoptBlockedToken(VoidFormEffectPower power, SpendToken token)
    {
        if (ReferenceEquals(token.BlockedPower, power))
        {
            return;
        }

        if (token.BlockedPower != null)
        {
            RemoveFromPowerBucket(token.BlockedPower, token);
        }

        TokenBucket powerBucket = PowerTokens.GetValue(power, static _ => new TokenBucket());
        lock (powerBucket)
        {
            if (!powerBucket.Items.Contains(token))
            {
                powerBucket.Items.Add(token);
            }
        }
        token.BlockedPower = power;
    }

    internal static void CancelForPower(VoidFormEffectPower power)
    {
        if (!PowerTokens.TryGetValue(power, out TokenBucket? bucket))
        {
            return;
        }

        SpendToken[] tokens;
        lock (bucket)
        {
            tokens = bucket.Items.ToArray();
        }

        foreach (SpendToken token in tokens)
        {
            Cancel(token);
        }
    }

    internal static void CancelOrphanTokensForOwner(Creature owner)
    {
        if (!OwnerTokens.TryGetValue(owner, out TokenBucket? bucket))
        {
            return;
        }

        SpendToken[] tokens;
        lock (bucket)
        {
            tokens = bucket.Items.ToArray();
        }

        foreach (SpendToken token in tokens)
        {
            if (token.PowerAtStart == null
                && token.BlockedPower == null
                && !token.OnPlayStarted
                && !token.Cancelled)
            {
                Cancel(token);
            }
        }
    }

    private static SpendToken? ClaimForPlay(
        CardModel card,
        ActionState? action,
        bool isAutoPlay)
    {
        if (action?.Spend is SpendToken actionToken
            && ReferenceEquals(actionToken.Card, card)
            && !actionToken.Cancelled
            && actionToken.SpendCompleted
            && !actionToken.OnPlayStarted
            && ReferenceEquals(actionToken.Action, action))
        {
            actionToken.OnPlayStarted = true;
            return actionToken;
        }

        if (!isAutoPlay)
        {
            CancelUnclaimedCompletedTokens(card);
        }
        return null;
    }

    private static void CancelUnclaimedCompletedTokens(CardModel card)
    {
        if (!Tokens.TryGetValue(card, out TokenBucket? bucket))
        {
            return;
        }

        SpendToken[] tokens;
        lock (bucket)
        {
            tokens = bucket.Items.ToArray();
        }
        foreach (SpendToken token in tokens)
        {
            if (token.SpendCompleted && !token.OnPlayStarted && !token.Cancelled)
            {
                Cancel(token);
            }
        }
    }

    private static void CompletePlay(PlayState state)
    {
        if (state.Spend != null)
        {
            VoidFormEffectPower? power = state.Spend.PowerAtStart;
            power?.ClearFailedPlay(state.Card, state.Spend, rollbackConsumed: false);
            if (state.BlockedPower != null && !ReferenceEquals(state.BlockedPower, power))
            {
                state.BlockedPower.ClearFailedPlay(state.Card, state.Spend, rollbackConsumed: false);
            }
            Remove(state.Spend);
        }
        else if (state.BlockedPower != null)
        {
            state.BlockedPower.ClearFailedPlay(state.Card, null, rollbackConsumed: false);
        }
    }

    internal static void AbortForPatch(PlayState state)
    {
        if (state.Spend != null)
        {
            state.Spend.PowerAtStart?.ClearFailedPlay(state.Card, state.Spend, rollbackConsumed: false);
            if (state.BlockedPower != null
                && !ReferenceEquals(state.BlockedPower, state.Spend.PowerAtStart))
            {
                state.BlockedPower.ClearFailedPlay(state.Card, state.Spend, rollbackConsumed: false);
            }
            Cancel(state.Spend);
        }
        else if (state.BlockedPower != null)
        {
            state.BlockedPower.ClearFailedPlay(state.Card, null, rollbackConsumed: false);
        }
    }

    private static void MarkSpendCompleted(SpendToken token)
    {
        if (!Tokens.TryGetValue(token.Card, out TokenBucket? bucket))
        {
            return;
        }

        lock (bucket)
        {
            if (!token.Cancelled && bucket.Items.Contains(token))
            {
                token.SpendCompleted = true;
            }
        }
    }

    private static void Remove(SpendToken token)
    {
        if (Tokens.TryGetValue(token.Card, out TokenBucket? bucket))
        {
            lock (bucket)
            {
                bucket.Items.Remove(token);
            }
        }

        if (OwnerTokens.TryGetValue(token.Card.Owner.Creature, out TokenBucket? ownerBucket))
        {
            lock (ownerBucket)
            {
                ownerBucket.Items.Remove(token);
            }
        }

        RemoveFromPowerBucket(token.PowerAtStart, token);
        if (token.BlockedPower != null
            && !ReferenceEquals(token.BlockedPower, token.PowerAtStart))
        {
            RemoveFromPowerBucket(token.BlockedPower, token);
        }
        if (token.Action != null && ReferenceEquals(token.Action.Spend, token))
        {
            token.Action.Spend = null;
        }
        token.Action = null;
        token.BlockedPower = null;
    }

    private static void RemoveFromPowerBucket(VoidFormEffectPower? power, SpendToken token)
    {
        if (power == null
            || !PowerTokens.TryGetValue(power, out TokenBucket? powerBucket))
        {
            return;
        }

        lock (powerBucket)
        {
            powerBucket.Items.Remove(token);
        }
    }

    internal static void EnterNativeActionForPatch(object action)
    {
        if (!IsNativePlayCardAction(action))
        {
            return;
        }

        ActionState state = Actions.GetValue(
            action,
            key => new ActionState
            {
                Action = key,
                PreviousAction = CurrentAction.Value
            });
        CurrentAction.Value = state;
    }

    internal static void CancelNativeActionForPatch(object action)
    {
        if (!IsNativePlayCardAction(action)
            || !Actions.TryGetValue(action, out ActionState? state))
        {
            return;
        }

        if (state.Spend != null)
        {
            Cancel(state.Spend);
        }
        CloseAction(state);
    }

    internal static Task ObserveNativeActionCompletionForPatch(Task result, object action)
    {
        if (!IsNativePlayCardAction(action)
            || !Actions.TryGetValue(action, out ActionState? state))
        {
            return result;
        }
        return ObserveNativeActionCompletionAsync(result, state);
    }

    private static async Task ObserveNativeActionCompletionAsync(Task result, ActionState state)
    {
        try
        {
            await result;
        }
        catch
        {
            if (state.Spend != null)
            {
                Cancel(state.Spend);
            }
            throw;
        }
        finally
        {
            if (state.Spend != null)
            {
                Cancel(state.Spend);
            }
            CloseAction(state);
        }
    }

    private static void CloseAction(ActionState state)
    {
        state.Closed = true;
        if (ReferenceEquals(CurrentAction.Value, state))
        {
            CurrentAction.Value = state.PreviousAction;
        }
    }

    // Probe-only explicit action bridge. It is deliberately not used by production code; the
    // native Harmony target above supplies the equivalent PlayCardAction owner at runtime.
    internal static object ProbeBeginAction(CardModel card)
    {
        ActionState? existing = GetCurrentActionFor(card);
        if (existing != null)
        {
            return new ProbeActionLease { State = existing, OwnsState = false };
        }

        object key = new object();
        ActionState state = new ActionState
        {
            Action = key,
            ExpectedCard = card,
            PreviousAction = CurrentAction.Value
        };
        Actions.Add(key, state);
        CurrentAction.Value = state;
        return new ProbeActionLease { State = state, OwnsState = true };
    }

    internal static void ProbeEndAction(object leaseObject)
    {
        if (leaseObject is not ProbeActionLease lease || !lease.OwnsState)
        {
            return;
        }

        if (lease.State.Spend != null && !lease.State.Spend.Cancelled)
        {
            Cancel(lease.State.Spend);
        }
        CloseAction(lease.State);
    }

#if SPIRE1_FORM_MOD && !GODOT
    internal static object ProbeBeginNativeAction(CardModel card)
    {
        object key = new ProbeNativeActionIdentity();
        ActionState state = new ActionState
        {
            Action = key,
            ExpectedCard = card,
            PreviousAction = CurrentAction.Value
        };
        Actions.Add(key, state);
        CurrentAction.Value = state;
        return key;
    }
#endif

    private static bool IsNativePlayCardAction(object? action)
    {
#if SPIRE1_FORM_MOD && !GODOT
        if (action is ProbeNativeActionIdentity)
        {
            return true;
        }
#endif
        return action?.GetType().FullName == "MegaCrit.Sts2.Core.GameActions.PlayCardAction";
    }

    internal static MethodBase? ResolveNativePlayCardMethodForPatch(string methodName)
    {
        Type? actionType = null;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            actionType = assembly.GetType("MegaCrit.Sts2.Core.GameActions.PlayCardAction", throwOnError: false);
            if (actionType != null)
            {
                break;
            }
        }

        return actionType?.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}

/// <summary>
/// Holds a per-card token across the actual asynchronous resource payment.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class VoidFormReserveBeforeSpendPatch
{
    [HarmonyPrefix]
    private static void Prefix(CardModel __instance, out VoidFormPlayTransaction.SpendToken __state)
    {
        __state = VoidFormPlayTransaction.BeginSpend(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(
        ref Task<(int, int)> __result,
        VoidFormPlayTransaction.SpendToken __state)
    {
        VoidFormPlayTransaction.EndSpendInvocation(__state);
        __result = VoidFormPlayTransaction.ObserveSpendCompletion(__result, __state);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        VoidFormPlayTransaction.SpendToken __state,
        Exception? __exception)
    {
        VoidFormPlayTransaction.EndSpendInvocation(__state);
        if (__exception != null)
        {
            VoidFormPlayTransaction.Cancel(__state);
        }
        return __exception;
    }
}

/// <summary>
/// Transfers a successful payment into the exact manual play action, or releases it for auto-play.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
internal static class VoidFormOnPlayWrapperTransactionPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        CardModel __instance,
        bool isAutoPlay,
        out VoidFormPlayTransaction.PlayState __state)
    {
        __state = VoidFormPlayTransaction.BeginPlay(__instance, isAutoPlay);
    }

    [HarmonyPostfix]
    private static void Postfix(
        ref Task __result,
        VoidFormPlayTransaction.PlayState __state)
    {
        __result = VoidFormPlayTransaction.ObservePlayCompletion(__result, __state);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        VoidFormPlayTransaction.PlayState __state,
        Exception? __exception)
    {
        if (__exception != null)
        {
            VoidFormPlayTransaction.AbortForPatch(__state);
            VoidFormPlayTransaction.ClosePlayContext(__state);
        }
        return __exception;
    }
}

/// <summary>
/// Dynamic compatibility target for the native PlayCardAction.ExecuteAction method. The
/// CardModel target in the attribute is a compile-only fallback for the isolated probe build;
/// Harmony uses TargetMethod when the native action type is present. If the target is absent, the
/// prefix is harmless and the production path fails closed because no action handoff exists.
/// </summary>
[HarmonyPatch(typeof(CardModel), (string?)null)]
internal static class VoidFormPlayCardActionExecutePatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
        => VoidFormPlayTransaction.ResolveNativePlayCardMethodForPatch("ExecuteAction");

    [HarmonyPrefix]
    private static void Prefix(object __instance)
        => VoidFormPlayTransaction.EnterNativeActionForPatch(__instance);

    [HarmonyPostfix]
    private static void Postfix(ref Task __result, object __instance)
        => __result = VoidFormPlayTransaction.ObserveNativeActionCompletionForPatch(__result, __instance);
}

/// <summary>
/// Dynamic compatibility target for native PlayCardAction.CancelAction. Cancellation is the
/// dedicated cleanup boundary for a successful payment that never reaches OnPlayWrapper.
/// </summary>
[HarmonyPatch(typeof(CardModel), (string?)null)]
internal static class VoidFormPlayCardActionCancelPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
        => VoidFormPlayTransaction.ResolveNativePlayCardMethodForPatch("CancelAction");

    [HarmonyPrefix]
    private static void Prefix(object __instance)
        => VoidFormPlayTransaction.CancelNativeActionForPatch(__instance);
}
