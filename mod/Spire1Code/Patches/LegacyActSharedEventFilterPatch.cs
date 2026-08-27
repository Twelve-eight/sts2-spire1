using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 一代楼层事件纯净性过滤（2026-08-27，用户点名的"AFTP 二代事件乱入一代楼层"修复）。
/// <para>
/// 引擎事实（dllsrc ActModel.cs:334）：<c>GenerateRooms</c> 无条件
/// <c>AllEvents.Concat(ModelDb.AllSharedEvents)</c>——官方 18 个二代 shared 事件
/// （BrainLeech / WelcomeToWongos / ThisOrThat …）会进入每一个幕的事件池，
/// 包括 AFTP 的一代幕与我们的 StS1 幕。这就是玩家在一代楼层撞见二代专属事件的根因。
/// shared 事件不挂在任何幕上（静态集合），ActToggler2 的勾选/注册池移除都影响不到它，
/// 唯一有效落点是这条 concat 的结果。
/// </para>
/// <para>
/// 实现：GenerateRooms 的 Postfix 反射读 <c>_rooms.events</c>（RoomSet.events 为
/// public readonly List，可原位 RemoveAll），把引擎 shared 白名单之外的二代事件清掉。
/// 仅对一代幕生效：我们的四幕（Spire1Act 子类）与 AFTP 三幕（类型全名前缀
/// "ActsFromThePast.Acts."，字符串判定无编译耦合）。官方二代幕不碰。
/// </para>
/// <para>
/// 保留范围（StS1 忠实）：BaseLib postfix 追加的 ActCustomEvents（一代事件，按幕声明）
/// 与幕自身 AllEvents 一律不动——只删官方 shared 拼接。AFTP 的 SharedEvents（Duplicator
/// 等 IShrineEvent）以 CustomEventModel 身份经 ActCustomEvents 通道追加，不经 shared
/// concat，天然不受影响。
/// </para>
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class LegacyActSharedEventFilterPatch
{
    private static readonly FieldInfo RoomsField =
        AccessTools.Field(typeof(ActModel), "_rooms");

    /// <summary>引擎官方 shared 事件清单（dllsrc ModelDb.cs:157-175，18 个）。
    /// 在一代幕中全部移除——StS1 的对应事件（我们与 AFTP 均有移植）走
    /// ActCustomEvents 通道，不在此列。</summary>
    private static readonly HashSet<string> OfficialSharedEventIds = BuildSharedIds();

    private static HashSet<string> BuildSharedIds()
    {
        // ModelDb.AllSharedEvents 是引擎静态属性——运行时读取以保证与版本同步，
        // 不在编译期硬编码 18 个类名。
        try
        {
            var shared = typeof(ModelDb)
                .GetProperty("AllSharedEvents", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as IEnumerable<EventModel>;
            if (shared != null)
            {
                var ids = new HashSet<string>(shared.Select(e => e.Id.Entry));
                if (ids.Count > 0)
                {
                    return ids;
                }
            }
        }
        catch
        {
            // fall through to reflection failure → empty set → patch no-ops safely
        }
        return [];
    }

    [HarmonyPostfix]
    private static void RemoveGen2SharedEvents(ActModel __instance)
    {
        // 仅一代幕：我们的四幕（Spire1Act 子类）或 AFTP 三幕（命名空间前缀判定）
        bool isLegacyAct = __instance is Spire1.Spire1Code.Acts.Spire1Act
            || __instance.GetType().FullName?.StartsWith("ActsFromThePast.Acts.", StringComparison.Ordinal) == true;
        if (!isLegacyAct || OfficialSharedEventIds.Count == 0)
        {
            return;
        }

        if (RoomsField?.GetValue(__instance) is not MegaCrit.Sts2.Core.Rooms.RoomSet rooms)
        {
            return;
        }

        int removed = rooms.events.RemoveAll(e => OfficialSharedEventIds.Contains(e.Id.Entry));
        if (removed > 0)
        {
            MainFile.Logger.Info(
                $"[Spire1] legacy-act shared-event filter: removed {removed} gen-2 shared events " +
                $"from {__instance.GetType().Name} (StS1 act purity)");
        }
    }
}
