using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Autoslayer 竞态保险：当事件流程以"浮层+奖励"路径收尾时，AutoSlayer 的
/// EventRoomHandler 可能在点击"继续"（NEventRoom.Proced→SetTravelEnabled(true)）
/// 之前就宣布事件完成，导致下一张地图 IsTravelEnabled 永远为 false，
/// Watchdog 以 "Map point never became travelable" 退出（P1SMOKE4 Act3:F12 实测）。
///
/// 本补丁在每次地图打开后检查：无战斗进行、无待处理浮层、且旅行未被合法锁止时，
/// 恢复旅行开关。对正常流程是无操作（该标志本应为 true）；
/// 对 LordsParasol 商店锁定等合法 false 态不触发（其上下文中地图未打开）。
/// </summary>
[HarmonyPatch(typeof(NMapScreen))]
[HarmonyPatch("Open")]
internal static class MapTravelRescuePatch
{
    static void Postfix(NMapScreen __instance)
    {
        try
        {
            if (!__instance.IsOpen || __instance.IsTravelEnabled || __instance.IsTraveling)
            {
                return;
            }
            if (CombatManager.Instance?.IsInProgress == true)
            {
                return;
            }
            var overlay = NOverlayStack.Instance;
            if (overlay != null && overlay.ScreenCount > 0)
            {
                return;
            }
            __instance.SetTravelEnabled(enabled: true);
        }
        catch
        {
            // 绝不让救援逻辑破坏地图打开本身
        }
    }
}
