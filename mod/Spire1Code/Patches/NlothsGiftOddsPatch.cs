using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// N'loth's Gift changes only the state-changing rarity roll used by the ordinary and elite
/// combat-card reward path. CardRarityOdds.Roll is also used by non-combat generation when a
/// caller forces rarity odds, so the companion context patch marks the actual CardFactory
/// RollForRarity call before this prefix can opt in. A missing context always leaves the vanilla
/// method untouched.
/// </summary>
[HarmonyPatch(typeof(CardRarityOdds), nameof(CardRarityOdds.Roll),
    new[] { typeof(CardRarityOddsType), typeof(Rng) })]
internal static class NlothsGiftOddsPatch
{
    private const float VanillaRareReset = -0.05f;
    private const float VanillaMaxRarityOffset = 0.4f;

    private static readonly MethodInfo? TargetMethod = ResolveTarget();
    private static readonly MethodInfo? PrefixMethod = ResolvePrefix();
    private static readonly AsyncLocal<NlothsGiftRollContext?> CurrentContext = new();

    /// <summary>
    /// The relic pool uses this exact owner+prefix probe as its fail-closed installation gate.
    /// Both the odds prefix and its source context/finalizer must be installed, otherwise the
    /// relic is unavailable instead of silently becoming an inert item.
    /// </summary>
    internal static bool IsInstalled => VerifyInstalled() && NlothsGiftRewardContextPatch.IsInstalled;

    [HarmonyPrefix]
    private static bool Prefix(
        CardRarityOdds __instance,
        CardRarityOddsType type,
        Rng? rngOverride,
        ref CardRarity __result)
    {
        if (__instance is null || !TryGetCombatGift(__instance, type, out float baseRareOdds))
        {
            // This is the only fallback point. All checks above are state-only and happen before
            // any replacement RNG call, so returning true runs vanilla exactly once.
            return true;
        }

        // Vanilla's effective rare threshold is baseRareOdds + CurrentValue. The replacement
        // offset makes that threshold three times as large while preserving the vanilla draw.
        float offset = (baseRareOdds * 2f) + (__instance.CurrentValue * 3f);
        CardRarity result;
        try
        {
            // This is the first potentially state-changing operation. If it consumes RNG and
            // then fails, the exception is logged and propagated; vanilla Roll is never retried.
            result = __instance.RollWithoutChangingFutureOdds(type, offset, rngOverride);

            // RollWithoutChangingFutureOdds intentionally leaves pity untouched. Keep vanilla's
            // state transition in the same operation, with no second roll on update failure.
            float nextCurrentValue = result == CardRarity.Rare
                ? VanillaRareReset
                : Math.Min(__instance.CurrentValue + __instance.RarityGrowth, VanillaMaxRarityOffset);
            __instance.OverrideCurrentValue(nextCurrentValue);
        }
        catch (Exception error)
        {
            MainFile.Logger.Error(
                "[Spire1] N'loth's Gift replacement failed after the roll began; "
                + "propagating without a vanilla retry ("
                + error.GetType().Name + ": " + error.Message + ").");
            throw;
        }

        __result = result;
        return false;
    }

    /// <summary>
    /// Captures the exact CardFactory reward owner and source context. The returned value is the
    /// previous context and is restored by the companion Harmony finalizer on both success and
    /// exception paths.
    /// </summary>
    internal static NlothsGiftRollContext? EnterRollContext(Player player, CardCreationOptions? options)
    {
        NlothsGiftRollContext? previous = CurrentContext.Value;
        CardRarityOddsType? combatOddsType = null;
        if (options is not null
            && options.Source == CardCreationSource.Encounter
            && options.Flags.HasFlag(CardCreationFlags.IsFromCombat)
            && (options.RarityOdds == CardRarityOddsType.RegularEncounter
                || options.RarityOdds == CardRarityOddsType.EliteEncounter))
        {
            combatOddsType = options.RarityOdds;
        }

        CurrentContext.Value = combatOddsType.HasValue
            ? new NlothsGiftRollContext(player, combatOddsType)
            : null;
        return previous;
    }

    internal static void RestoreRollContext(NlothsGiftRollContext? previous)
    {
        CurrentContext.Value = previous;
    }

    private static bool TryGetCombatGift(
        CardRarityOdds odds,
        CardRarityOddsType type,
        out float baseRareOdds)
    {
        baseRareOdds = 0f;
        if (type != CardRarityOddsType.RegularEncounter && type != CardRarityOddsType.EliteEncounter)
        {
            return false;
        }

        NlothsGiftRollContext? context = CurrentContext.Value;
        if (context is null || context.CombatOddsType != type)
        {
            return false;
        }

        try
        {
            baseRareOdds = type == CardRarityOddsType.RegularEncounter
                ? CardRarityOdds.RegularRareOdds
                : CardRarityOdds.EliteRareOdds;
            Player owner = context.Owner;
            RunState? runState = RunManager.Instance.DebugOnlyGetState();
            if (runState is null
                || !ReferenceEquals(owner.RunState, runState)
                || !runState.Players.Contains(owner)
                || runState.CurrentRoom is not CombatRoom combatRoom
                || !IsMatchingCombatRoom(combatRoom, type)
                || !ReferenceEquals(owner.PlayerOdds.CardRarity, odds))
            {
                return false;
            }

            NlothsGift? gift = owner.GetRelic<NlothsGift>();
            return gift is not null && !gift.IsMelted;
        }
        catch (Exception error)
        {
            // This branch is still preflight: no replacement RNG call has started. Keep vanilla
            // behavior, but make an unexpected API/state failure visible to the run log.
            MainFile.Logger.Error(
                "[Spire1] N'loth's Gift preflight failed before RNG; retaining vanilla roll ("
                + error.GetType().Name + ": " + error.Message + ").");
            return false;
        }
    }

    private static bool IsMatchingCombatRoom(CombatRoom room, CardRarityOddsType type) =>
        type switch
        {
            CardRarityOddsType.RegularEncounter => room.RoomType == RoomType.Monster,
            CardRarityOddsType.EliteEncounter => room.RoomType == RoomType.Elite,
            _ => false
        };

    private static bool VerifyInstalled()
    {
        if (TargetMethod is null || PrefixMethod is null)
        {
            return false;
        }

        try
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(TargetMethod);
            return info is not null
                && info.Prefixes.Count(patch =>
                    patch.owner == MainFile.ModId
                    && patch.PatchMethod is { } patchMethod
                    && (patchMethod == PrefixMethod || patchMethod.Equals(PrefixMethod))) == 1;
        }
        catch (Exception error)
        {
            MainFile.Logger.Error(
                "[Spire1] N'loth's Gift odds Harmony verification failed ("
                + error.GetType().Name + ": " + error.Message + ").");
            return false;
        }
    }

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(
                typeof(CardRarityOdds),
                nameof(CardRarityOdds.Roll),
                new[] { typeof(CardRarityOddsType), typeof(Rng) });
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolvePrefix()
    {
        try
        {
            return typeof(NlothsGiftOddsPatch).GetMethod(
                nameof(Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

internal sealed class NlothsGiftRollContext
{
    internal NlothsGiftRollContext(Player owner, CardRarityOddsType? combatOddsType)
    {
        Owner = owner;
        CombatOddsType = combatOddsType;
    }

    internal Player Owner { get; }

    internal CardRarityOddsType? CombatOddsType { get; }
}

/// <summary>
/// Marks only CardFactory's actual reward rarity call. CardRarityOdds.Roll has no source or
/// owner argument, so checking only the current room would incorrectly modify event/relic card
/// generation that happens while a CombatRoom is current.
/// </summary>
[HarmonyPatch(typeof(CardFactory), "RollForRarity",
    new[] { typeof(Player), typeof(CardCreationOptions), typeof(HashSet<CardRarity>) })]
internal static class NlothsGiftRewardContextPatch
{
    internal static readonly MethodInfo? TargetMethod = ResolveTarget();
    internal static readonly MethodInfo? PrefixMethod = ResolvePrefix();
    internal static readonly MethodInfo? FinalizerMethod = ResolveFinalizer();

    internal static bool IsInstalled => VerifyInstalled();

    [HarmonyPrefix]
    private static void Prefix(
        Player player,
        CardCreationOptions options,
        out NlothsGiftRollContext? __state)
    {
        __state = NlothsGiftOddsPatch.EnterRollContext(player, options);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        Exception? __exception,
        NlothsGiftRollContext? __state)
    {
        NlothsGiftOddsPatch.RestoreRollContext(__state);
        return __exception;
    }

    private static bool VerifyInstalled()
    {
        if (TargetMethod is null || PrefixMethod is null || FinalizerMethod is null)
        {
            return false;
        }

        try
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(TargetMethod);
            return info is not null
                && info.Prefixes.Count(patch =>
                    patch.owner == MainFile.ModId
                    && patch.PatchMethod is { } patchMethod
                    && (patchMethod == PrefixMethod || patchMethod.Equals(PrefixMethod))) == 1
                && info.Finalizers.Count(patch =>
                    patch.owner == MainFile.ModId
                    && patch.PatchMethod is { } patchMethod
                    && (patchMethod == FinalizerMethod || patchMethod.Equals(FinalizerMethod))) == 1;
        }
        catch (Exception error)
        {
            MainFile.Logger.Error(
                "[Spire1] N'loth's Gift reward-context Harmony verification failed ("
                + error.GetType().Name + ": " + error.Message + ").");
            return false;
        }
    }

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(
                typeof(CardFactory),
                "RollForRarity",
                new[] { typeof(Player), typeof(CardCreationOptions), typeof(HashSet<CardRarity>) });
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolvePrefix()
    {
        try
        {
            return typeof(NlothsGiftRewardContextPatch).GetMethod(
                nameof(Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolveFinalizer()
    {
        try
        {
            return typeof(NlothsGiftRewardContextPatch).GetMethod(
                nameof(Finalizer),
                BindingFlags.Static | BindingFlags.NonPublic);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
