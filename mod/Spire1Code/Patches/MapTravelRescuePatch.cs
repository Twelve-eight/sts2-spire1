using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Autoslayer 竞态保险（v2）：当事件以"浮层+奖励"路径收尾时，EventRoomHandler 可能在
/// 点击"继续"（NEventRoom.Proceed→SetTravelEnabled(true)）之前就宣布事件完成，
/// 导致地图 IsTravelEnabled 永远 false，Watchdog 以 "Map point never became travelable"
/// 退出（P1SMOKE4 Act3:F12 与 P1SMOKE6 Act3:F12(0,11) 两次实测；Open() 钩子版本因
/// 触发时子浮层仍在栈上而漏救）。
///
/// v2：挂在 autoslayer 高频轮询的 NMapPoint.IsEnabled getter 上——只要某点已
/// Travelable、屏幕打开、无战斗、无浮层而旅行开关仍是 false，就地恢复开关。
/// 仅在 AutoSlayer.IsActive 时生效，人类对局零介入。
/// </summary>
[HarmonyPatch(typeof(NMapPoint))]
[HarmonyPatch("IsEnabled", MethodType.Getter)]
internal static class MapTravelRescuePatch
{
    static void Postfix(NMapPoint __instance, ref bool __result)
    {
        if (__result || !AutoSlayer.IsActive)
        {
            return;
        }
        if (__instance.State != MapPointState.Travelable)
        {
            return;
        }
        var screen = NMapScreen.Instance;
        if (screen == null || !screen.IsOpen || screen.IsTraveling || screen.IsTravelEnabled)
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
        screen.SetTravelEnabled(enabled: true);
        __result = true; // 本帧即可点击，省一轮等待
    }
}
