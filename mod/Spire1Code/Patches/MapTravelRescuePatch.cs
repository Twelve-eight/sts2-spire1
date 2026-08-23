using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
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
/// v2：挂在 autoslayer 高频轮询的 IsEnabled getter 上——只要某点已 Travelable、
/// 地图打开、无战斗、无浮层而旅行开关仍是 false，就地恢复开关并令本帧可点击。
/// 注意：IsEnabled 声明于基类 NClickableControl（NMapPoint 无自有声明），Harmony
/// 不下探基类，因此挂声明类型并以 `is NMapPoint` 过滤。仅在 AutoSlayer.IsActive 时
/// 生效，人类对局零介入。
/// </summary>
[HarmonyPatch(typeof(NClickableControl))]
[HarmonyPatch("IsEnabled", MethodType.Getter)]
internal static class MapTravelRescuePatch
{
    static void Postfix(NClickableControl __instance, ref bool __result)
    {
        if (__result || !AutoSlayer.IsActive)
        {
            return;
        }
        if (__instance is not NMapPoint mapPoint)
        {
            return;
        }
        if (mapPoint.State != MapPointState.Travelable)
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
