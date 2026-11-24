using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 删类 / 分拆内容旧存档兼容层。
/// <para>
/// 1) LEAN-CODE（2e405f8）硬删了 8 个遗物与 6 个药水（官方 StS2 均已内置），在 Player.FromSerializable 前静默剔除，避免 DeprecatedRelic/Potion 占位。
/// 2) 1.3.0（2d3f817）将 MutagenicStrength、J.A.X. 与 6 个一代事件分拆到独立模组 Spire1Events（沿用原 SPIRE1-* ID）。
/// 当未安装 Spire1Events（ModelDb 未注册对应 ID）时，静默剔除这批已迁出条目；若已安装 Spire1Events 则完整保留。
/// </para>
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.FromSerializable))]
internal static class LegacySaveCompatPatch
{
    /// <summary>已删遗物的 ModelId.Entry（SPIRE1- 前缀，含 Akabeko 共 8 个）。</summary>
    private static readonly HashSet<string> RemovedRelicIds = new()
    {
        "SPIRE1-ANCHOR",
        "SPIRE1-BAG_OF_MARBLES",
        "SPIRE1-BLOOD_VIAL",
        "SPIRE1-BRONZE_SCALES",
        "SPIRE1-LANTERN",
        "SPIRE1-LETTER_OPENER",
        "SPIRE1-ORNAMENTAL_FAN",
        "SPIRE1-AKABEKO",
    };

    /// <summary>已删药水的 ModelId.Entry（共 6 个）。</summary>
    private static readonly HashSet<string> RemovedPotionIds = new()
    {
        "SPIRE1-BLOCK_POTION",
        "SPIRE1-STRENGTH_POTION",
        "SPIRE1-DEXTERITY_POTION",
        "SPIRE1-ENERGY_POTION",
        "SPIRE1-FIRE_POTION",
        "SPIRE1-WEAK_POTION",
    };

    private static readonly HashSet<string> MovedRelicIds = new() { "SPIRE1-MUTAGENIC_STRENGTH" };
    private static readonly HashSet<string> MovedCardIds = new() { "SPIRE1-J_A_X" };
    internal static readonly HashSet<string> MovedEventIds = new()
    {
        "SPIRE1-ADDICT",
        "SPIRE1-BACK_TO_BASICS",
        "SPIRE1-DRUG_DEALER",
        "SPIRE1-FOUNTAIN_OF_CURSE_REMOVAL",
        "SPIRE1-NOTE_FOR_YOURSELF",
        "SPIRE1-SPIRE_HEART",
    };

    private static void Prefix(ref SerializablePlayer save)
    {
        try
        {
            StripRelics(save.Relics);
            StripPotions(save.Potions);
            StripMovedCards(save.Deck);
        }
        catch (System.Exception e)
        {
            // 兼容层绝不允许把存档加载整个打断--异常时退回引擎原生 deprecated 占位行为。
            MainFile.Logger.Error($"[Spire1] legacy save strip failed (falling back to deprecated placeholders): {e.Message}");
        }
    }

    private static void StripRelics(List<SerializableRelic> relics)
    {
        if (relics.Count == 0)
        {
            return;
        }
        List<string> dropped = new();
        relics.RemoveAll(r =>
        {
            if (r.Id == null)
            {
                return false;
            }
            bool remove = RemovedRelicIds.Contains(r.Id.Entry)
                || (MovedRelicIds.Contains(r.Id.Entry) && ModelDb.GetByIdOrNull<RelicModel>(r.Id) is null);
            if (remove)
            {
                dropped.Add(r.Id.Entry);
            }
            return remove;
        });
        if (dropped.Count > 0)
        {
            MainFile.Logger.Info(
                $"[Spire1] legacy save compat: removed {dropped.Count} deleted relic(s): {string.Join(", ", dropped)}");
        }
    }

    private static void StripMovedCards(List<SerializableCard> deck)
    {
        if (deck.Count == 0)
        {
            return;
        }
        List<string> dropped = new();
        deck.RemoveAll(c =>
        {
            if (c.Id != null && MovedCardIds.Contains(c.Id.Entry) && ModelDb.GetByIdOrNull<CardModel>(c.Id) is null)
            {
                dropped.Add(c.Id.Entry);
                return true;
            }
            return false;
        });
        if (dropped.Count > 0)
        {
            MainFile.Logger.Info(
                $"[Spire1] legacy save compat: removed {dropped.Count} moved card(s) (Spire1Events absent): {string.Join(", ", dropped)}");
        }
    }

    private static void StripPotions(List<SerializablePotion> potions)
    {
        if (potions.Count == 0)
        {
            return;
        }
        List<string> dropped = new();
        potions.RemoveAll(p =>
        {
            if (p.Id != null && RemovedPotionIds.Contains(p.Id.Entry))
            {
                dropped.Add(p.Id.Entry);
                return true;
            }
            return false;
        });
        if (dropped.Count > 0)
        {
            MainFile.Logger.Info(
                $"[Spire1] legacy save compat: removed {dropped.Count} deleted potion(s): {string.Join(", ", dropped)}");
        }
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class LegacyRunSaveCompatPatch
{
    private static void Prefix(SerializableRun save)
    {
        try
        {
            if (save.EventsSeen.Count == 0)
            {
                return;
            }
            List<string> dropped = new();
            save.EventsSeen.RemoveAll(id =>
            {
                if (id != null && LegacySaveCompatPatch.MovedEventIds.Contains(id.Entry)
                    && ModelDb.GetByIdOrNull<EventModel>(id) is null)
                {
                    dropped.Add(id.Entry);
                    return true;
                }
                return false;
            });
            if (dropped.Count > 0)
            {
                MainFile.Logger.Info(
                    $"[Spire1] legacy run save compat: removed {dropped.Count} moved event id(s) from EventsSeen (Spire1Events absent): {string.Join(", ", dropped)}");
            }
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error($"[Spire1] legacy run save strip failed: {e.Message}");
        }
    }
}
