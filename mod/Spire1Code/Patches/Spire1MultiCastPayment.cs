using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using OwnMultiCast = Spire1.Spire1Code.Cards.MultiCast;

namespace Spire1.Spire1Code.Patches;

/// <summary>Own MultiCast only: no orb means no energy payment, not cancellation of the card play.</summary>
internal static class Spire1MultiCastPayment
{
    private const string OwnerId = "Spire1.MultiCastPayment";
    private const BindingFlags Methods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly object InstallationLock = new();
    private static readonly Task<(int, int)> NoResources = Task.FromResult((0, 0));
    private static Func<CardModel, int, Task>? _spendStars;
    private static bool _available;
    private static bool _failed;

    internal static bool EnsureInstalled()
    {
        lock (InstallationLock)
        {
            if (_failed)
                return false;
            var harmony = new Harmony(OwnerId);
            MethodInfo? spend = null, chemical = null;
            MethodInfo paymentPrefix = Own(nameof(PayIfOrb));
            MethodInfo chemicalPrefix = Own(nameof(FlashIfOrb));
            try
            {
                spend = AccessTools.DeclaredMethod(typeof(CardModel), nameof(CardModel.SpendResources), Type.EmptyTypes);
                chemical = AccessTools.DeclaredMethod(typeof(ChemicalX), nameof(ChemicalX.BeforeCardPlayed), new[] { typeof(CardPlay) });
                MethodInfo? stars = AccessTools.DeclaredMethod(typeof(CardModel), "SpendStars", new[] { typeof(int) });
                if (spend == null || spend.IsStatic || spend.ReturnType != typeof(Task<(int, int)>)
                    || chemical == null || chemical.IsStatic || chemical.ReturnType != typeof(Task)
                    || stars == null || stars.IsStatic || !stars.IsPrivate || stars.ReturnType != typeof(Task))
                    throw new InvalidOperationException("MultiCast payment signatures changed");
                _spendStars ??= stars.CreateDelegate<Func<CardModel, int, Task>>();
                if (!_available)
                {
                    harmony.Patch(spend, prefix: new HarmonyMethod(paymentPrefix) { priority = Priority.First });
                    harmony.Patch(chemical, prefix: new HarmonyMethod(chemicalPrefix) { priority = Priority.First });
                }
                Verify(spend, paymentPrefix);
                Verify(chemical, chemicalPrefix);
                _available = true;
                return true;
            }
            catch (Exception error)
            {
                _available = false;
                _failed = true;
                string? paymentCleanup = RemoveExact(harmony, spend, paymentPrefix);
                string? chemicalCleanup = RemoveExact(harmony, chemical, chemicalPrefix);
                string reason = "MultiCast payment unavailable: " + error.Message;
                if (paymentCleanup != null)
                    reason += "; payment rollback: " + paymentCleanup;
                if (chemicalCleanup != null)
                    reason += "; ChemicalX rollback: " + chemicalCleanup;
                Spire1PowersGate.MarkDeterministicUnavailable(reason);
                return false;
            }
        }
    }

    private static string? RemoveExact(Harmony harmony, MethodInfo? target, MethodInfo prefix)
    {
        if (target == null)
            return null;
        try
        {
            harmony.Unpatch(target, prefix);
            return Harmony.GetPatchInfo(target)?.Prefixes.Any(p => p.owner == OwnerId && p.PatchMethod == prefix) == true
                ? "exact prefix remains installed" : null;
        }
        catch (Exception error)
        {
            return error.GetType().Name + ": " + error.Message;
        }
    }

    private static MethodInfo Own(string name) => typeof(Spire1MultiCastPayment).GetMethod(name, Methods)!;

    private static void Verify(MethodInfo target, MethodInfo prefix)
    {
        var patches = Harmony.GetPatchInfo(target)?.Prefixes;
        if (patches == null || patches.Count(p => p.owner == OwnerId) != 1
            || !patches.Any(p => p.owner == OwnerId && p.PatchMethod == prefix && p.priority == Priority.First))
            throw new InvalidOperationException("Missing exact MultiCast payment prefix: " + target.Name);
    }

    internal static void RequireAvailable()
    {
        if (!_available || Spire1PowersGate.ContentUnavailableActive)
            throw new InvalidOperationException("Spire1 MultiCast payment unavailable; restart with compatible game and mod versions.");
    }

    internal static bool HasOrb(CardModel card) => card.Owner.PlayerCombatState!.OrbQueue.Orbs.Count != 0;

    private static bool PayIfOrb(CardModel __instance, ref Task<(int, int)> __result)
    {
        if (__instance.GetType() != typeof(OwnMultiCast))
            return true;
        RequireAvailable();
        if (HasOrb(__instance))
            return true;

        int rawX = __instance.EnergyCost.GetAmountToSpend();
        int stars = Math.Max(0, __instance.GetStarCostWithModifiers());
        __instance.EnergyCost.CapturedXValue = rawX;
        Task payment = _spendStars!(__instance, stars);
        __result = stars == 0 && payment.IsCompletedSuccessfully ? NoResources : FinishStars(payment, stars);
        return false;
    }

    private static async Task<(int, int)> FinishStars(Task payment, int amount)
    {
        await payment;
        return (0, amount);
    }

    private static bool FlashIfOrb(ChemicalX __instance, CardPlay cardPlay, ref Task __result)
    {
        CardModel card = cardPlay.Card;
        if (card.GetType() != typeof(OwnMultiCast) || !ReferenceEquals(__instance.Owner, card.Owner))
            return true;
        RequireAvailable();
        if (HasOrb(card))
            return true;
        __result = Task.CompletedTask;
        return false;
    }
}
