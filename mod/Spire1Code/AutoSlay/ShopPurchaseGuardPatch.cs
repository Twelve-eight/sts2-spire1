using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Spire1.Spire1Code.Patches;
using MegaCrit.Sts2.Core.Random;


/// <summary>
/// Compat patch for the official AutoSlayer shop handler.
///
/// Upstream ShopRoomHandler.HandleAsync picks any stocked slot with EnoughGold and calls
/// OnTryPurchaseWrapper. Some relics (e.g. "添水"/Ashwater-style: gain energy each turn,
/// can never gain potions) make the merchant REFUSE potion purchases while the slot still
/// reports IsStocked && EnoughGold — the loop then retries the same refused slot until
/// maxAttempts (50) burns, stalling ~1 minute in every shop.
///
/// Fix: reimplement the purchase loop with a failed-slot blacklist. A slot that fails to
/// actually spend gold / unstock is never retried. Gated on AutoSlayer.IsActive so normal
/// play uses the vanilla handler untouched.
/// </summary>
[HarmonyPatch(typeof(ShopRoomHandler), nameof(ShopRoomHandler.HandleAsync))]
public static class ShopPurchaseGuardPatch
{
    public static bool Prefix(ShopRoomHandler __instance, Rng random, CancellationToken ct)
    {
        if (!AutoSlayer.IsActive)
            return true; // vanilla path

        RunGuarded(__instance, random, ct).Wait(ct);
        return false; // skip original
    }

    private static async Task RunGuarded(ShopRoomHandler handler, Rng random, CancellationToken ct)
    {
        var drain = Traverse.Create(handler)
            .Field<Func<Task, CancellationToken, Task>>("_drainOverlayScreensUntil").Value;

        AutoSlayLog.Action("Waiting for shop room");
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        NMerchantRoom room = await WaitHelper.ForNode<NMerchantRoom>(
            root, "/root/Game/RootSceneContainer/Run/RoomContainer/MerchantRoom", ct);
        AutoSlayLog.Action("Opening merchant inventory");
        room.OpenInventory();
        await Task.Delay(500, ct);

        int maxAttempts = 50, attempts = 0;
        var failed = new HashSet<NMerchantSlot>();
        while (attempts < maxAttempts)
        {
            ct.ThrowIfCancellationRequested();
            attempts++;
            List<NMerchantSlot> list = room.Inventory.GetAllSlots()
                .Where(slot => !(slot is NMerchantCardRemoval))
                .Where(slot => !failed.Contains(slot))
                .Where(slot => slot.Entry.IsStocked && slot.Entry.EnoughGold)
                .ToList();
            if (list.Count == 0)
            {
                AutoSlayLog.Action("No more affordable items to buy");
                break;
            }
            NMerchantSlot slot = random.NextItem(list);
            AutoSlayLog.Action($"Buying {slot.GetType().Name} (cost: {slot.Entry.Cost})");
            await drain(slot.Entry.OnTryPurchaseWrapper(room.Inventory.Inventory), ct);
            await Task.Delay(300, ct);
            // A successful purchase unstocks the slot. Still stocked = purchase was
            // refused (e.g. potion-ban relic like 添水). Never retry this slot again.
            if (slot.Entry.IsStocked)
            {
                AutoSlayLog.Action($"Purchase refused for {slot.GetType().Name}; blacklisting slot");
                failed.Add(slot);
            }
        }

        var backButton = UiHelper.FindFirst<NBackButton>(room);
        if (backButton != null)
        {
            AutoSlayLog.Action("Closing inventory");
            await UiHelper.Click(backButton);
            await Task.Delay(300, ct);
        }
        AutoSlayLog.Action("Clicking proceed");
        await UiHelper.Click(room.ProceedButton);
    }

}
