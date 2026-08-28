using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 删类旧存档兼容层（LEAN-CODE 去重批 2e405f8 的善后）。
/// <para>
/// 背景：该提交硬删了 8 个遗物（Anchor/BagOfMarbles/BloodVial/BronzeScales/Lantern/
/// LetterOpener/OrnamentalFan/Akabeko——官方 StS2 均已内置同效果版本）与 6 个药水
/// （BlockPotion/StrengthPotion/DexterityPotion/EnergyPotion/FirePotion/WeakPotion）。
/// 引擎加载旧存档时，Player.FromSerializable → LoadInventory → PopulateRelics/LoadPotions
/// 会把未知 id 解析为 DeprecatedRelic/DeprecatedPotion 占位模型（dllsrc Player.cs:362-365,
/// RelicModel.FromSerializable → SaveUtil.RelicOrDeprecated）——不崩溃，但背包里会留下
/// 无图标无效果的幽灵条目。
/// </para>
/// <para>
/// 修复：Prefix（非 postfix——LoadInventory 在 FromSerializable 方法体内部执行，必须赶在
/// 它之前）把已删 id 从 SerializablePlayer 的 relics/potions 列表静默剔除。每个被剔除的
/// id 记一行日志，方便玩家核对背包差异。官方同名遗物不受影响：它们的 id 是
/// RELIC.ANCHOR 等原生 Category.Entry，与 SPIRE1- 前缀条目天然不冲突。
/// MP 重连同步（SyncWithSerializedPlayer）同样消费这两个列表，但 MP 双方装同一构建时
/// 存档里早已不含已删 id，本补丁在 MP 路径上是纯空转。
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

    private static void Prefix(ref SerializablePlayer save)
    {
        try
        {
            StripRelics(save.Relics);
            StripPotions(save.Potions);
        }
        catch (System.Exception e)
        {
            // 兼容层绝不允许把存档加载整个打断——异常时退回引擎原生 deprecated 占位行为。
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
            if (r.Id != null && RemovedRelicIds.Contains(r.Id.Entry))
            {
                dropped.Add(r.Id.Entry);
                return true;
            }
            return false;
        });
        if (dropped.Count > 0)
        {
            MainFile.Logger.Info(
                $"[Spire1] legacy save compat: removed {dropped.Count} deleted relic(s): {string.Join(", ", dropped)}");
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
