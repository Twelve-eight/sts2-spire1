using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Carries one real card payment from SpendResources to OnPlayWrapper.
///
/// CardModel.SpendResources is async. Harmony runs a postfix when that method returns its Task,
/// not when the payment Task completes. A reservation therefore cannot be released by the raw
/// postfix. Each card has an independent token which remains live until its play wrapper claims
/// it, or until the payment or play fails.
/// </summary>
internal static class VoidFormPlayTransaction
{
    internal sealed class SpendToken
    {
        internal required CardModel Card { get; init; }
        internal VoidFormEffectPower? PowerAtStart { get; init; }
        internal bool Reserved { get; init; }
        internal bool SpendCompleted { get; set; }
        internal bool OnPlayStarted { get; set; }
    }

    internal sealed class PlayState
    {
        internal required CardModel Card { get; init; }
        internal SpendToken? Spend { get; init; }
        internal bool IsAutoPlay { get; init; }
        internal bool BlockedCurrentPower { get; set; }
    }

    private sealed class TokenBucket
    {
        internal readonly List<SpendToken> Items = new();
    }

    private static readonly ConditionalWeakTable<CardModel, TokenBucket> Tokens = new();

    internal static SpendToken BeginSpend(CardModel card)
    {
        VoidFormEffectPower? power = card.Owner.Creature.GetPower<VoidFormEffectPower>();
        bool reserved = power != null && power.CanReserveFor(card);
        if (reserved)
        {
            power!.ReserveFor(card);
        }

        var token = new SpendToken
        {
            Card = card,
            PowerAtStart = power,
            Reserved = reserved
        };
        TokenBucket bucket = Tokens.GetValue(card, static _ => new TokenBucket());
        lock (bucket)
        {
            bucket.Items.Add(token);
        }
        return token;
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
            token.SpendCompleted = true;
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
        if (token.PowerAtStart != null && token.Reserved)
        {
            token.PowerAtStart.ReleaseReservation(token.Card);
        }
        Remove(token);
    }

    internal static PlayState BeginPlay(CardModel card, bool isAutoPlay)
    {
        SpendToken? token = ClaimForPlay(card);
        var state = new PlayState
        {
            Card = card,
            Spend = token,
            IsAutoPlay = isAutoPlay
        };

        if (isAutoPlay)
        {
            if (token != null)
            {
                Cancel(token);
            }
            return state;
        }

        if (token?.Reserved == true
            && token.PowerAtStart != null
            && token.PowerAtStart.IsReservationFor(card))
        {
            return state;
        }

        // A manual wrapper without the matching reservation is not allowed to consume a Void
        // power gained during payment, or a power that was already reserved by another card.
        if (token?.PowerAtStart != null && token.Reserved)
        {
            token.PowerAtStart.ReleaseReservation(card);
        }
        VoidFormEffectPower? current = card.Owner.Creature.GetPower<VoidFormEffectPower>();
        if (current != null)
        {
            current.BlockCard(card);
            state.BlockedCurrentPower = true;
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
    }

    internal static bool IsManaged(CardModel card)
    {
        if (!Tokens.TryGetValue(card, out TokenBucket? bucket))
        {
            return false;
        }

        lock (bucket)
        {
            return bucket.Items.Count != 0;
        }
    }

    private static SpendToken? ClaimForPlay(CardModel card)
    {
        if (!Tokens.TryGetValue(card, out TokenBucket? bucket))
        {
            return null;
        }

        lock (bucket)
        {
            for (int i = bucket.Items.Count - 1; i >= 0; i--)
            {
                SpendToken token = bucket.Items[i];
                if (!token.OnPlayStarted)
                {
                    token.OnPlayStarted = true;
                    return token;
                }
            }
        }
        return null;
    }

    private static void CompletePlay(PlayState state)
    {
        if (state.Spend != null)
        {
            VoidFormEffectPower? power = state.Spend.PowerAtStart;
            if (power != null && !power.HasPendingFor(state.Card))
            {
                power.ReleaseReservation(state.Card);
                power.ClearBlockedCard(state.Card);
            }
            Remove(state.Spend);
        }
        else if (state.BlockedCurrentPower)
        {
            state.Card.Owner.Creature.GetPower<VoidFormEffectPower>()?.ClearBlockedCard(state.Card);
        }
    }

    internal static void AbortForPatch(PlayState state)
    {
        if (state.Spend != null)
        {
            Cancel(state.Spend);
        }
        else if (state.BlockedCurrentPower)
        {
            state.Card.Owner.Creature.GetPower<VoidFormEffectPower>()?.ClearBlockedCard(state.Card);
        }
    }

    private static void Remove(SpendToken token)
    {
        if (!Tokens.TryGetValue(token.Card, out TokenBucket? bucket))
        {
            return;
        }

        lock (bucket)
        {
            bucket.Items.Remove(token);
        }
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
        __result = VoidFormPlayTransaction.ObserveSpendCompletion(__result, __state);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        VoidFormPlayTransaction.SpendToken __state,
        Exception? __exception)
    {
        if (__exception != null)
        {
            VoidFormPlayTransaction.Cancel(__state);
        }
        return __exception;
    }
}

/// <summary>
/// Transfers a successful payment into the matching manual play, or releases it for auto-play.
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
        }
        return __exception;
    }
}
