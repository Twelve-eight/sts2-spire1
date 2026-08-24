using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Hooks;
namespace Spire1.Spire1Code.AutoSlay;

/// <summary>
/// Compat fix for the official AutoSlayer shop handler.
///
/// Upstream ShopRoomHandler.HandleAsync buys any stocked slot whose Entry.EnoughGold is
/// true. Two refusal reasons are invisible to that filter and made it spin until
/// maxAttempts (50), stalling ~1 minute per shop:
///   1. potion-ban relics (e.g. Sozu "添水": Hook.ShouldProcurePotion == false) — the
///      merchant refuses with FailureForbidden while the slot still reports EnoughGold;
///   2. a full potion bar — FailureSpace, same loop stall (vanilla-reachable).
///
/// Fix: teach EnoughGold itself about these cases. When the entry is a shop POTION the
/// player could not actually receive right now, EnoughGold reports false, so the vanilla
/// loop's existing filter skips the slot and proceeds ("No more affordable items").
/// No async code, no main-thread blocking — safe on the Godot main thread.
///
/// NOTE on the earlier attempt: replacing HandleAsync with a Prefix that blocked on
/// .Wait() deadlocked the game at "Entering Shop room" (main thread must pump the
/// awaited continuations). Never block the Unity/Godot main thread on game tasks.
/// </summary>
[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.EnoughGold), MethodType.Getter)]
internal static class ShopEnoughGoldGuardPatch
{
    [HarmonyPostfix]
    private static void HideUnbuyablePotions(MerchantEntry __instance, ref bool __result)
    {
        if (!__result || __instance is not MerchantPotionEntry potionEntry || potionEntry.Model == null)
        {
            return;
        }
        Player? player = Traverse.Create(__instance).Field<Player>("_player").Value;
        bool banned;
        string reason;
        if (player == null)
        {
            banned = true; reason = "player-null";
        }
        else
        {
            banned = !Hook.ShouldProcurePotion(player.RunState, player.Creature?.CombatState, potionEntry.Model, player);
            reason = banned ? "sozu-ban" : (player.HasOpenPotionSlots ? "none" : "bar-full");
        }
        Spire1.Spire1Code.MainFile.Logger.Info(
            $"[Spire1] EnoughGoldGuard: {potionEntry.Model.Id.Entry} result={__result} reason={reason}");
        if (banned || (reason == "bar-full"))
        {
            __result = false;
        }
    }
}
