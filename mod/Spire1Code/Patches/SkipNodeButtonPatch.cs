using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 地图页"跳过当前节点"救援按钮（Spire1Config.EnableSkipNodeButton 门控，默认开）。
/// <para>
/// 用途：火堆等房间进入即黑屏死锁时，打开地图（顶栏地图键本地可用，
/// 见 NTopBarMapButton.Open(isOpenedFromTopBar:true) 先例）后点此按钮解锁选点，
/// 再直接点目标节点——走引擎原生 VoteForMapCoordAction 投票管线，全端一致移动。
/// </para>
/// <para>
/// 引擎事实（SkipApiScout 取证）：RunState 无"房间完成"字段；放行完全由各端
/// NMapScreen.IsTravelEnabled 本地门控（战斗胜利路径就是 SetTravelEnabled(true)），
/// 因此本补丁零状态改动、零新增网络类型，不构成失同步源。
/// </para>
/// </summary>
[HarmonyPatch(typeof(NMapScreen), "Open")]
internal static class SkipNodeButtonPatch
{
    private const string ButtonName = "Spire1SkipNodeButton";

    [HarmonyPostfix]
    private static void AddSkipButton(NMapScreen __instance)
    {
        var button = __instance.GetNodeOrNull<Button>(ButtonName);
        if (!Spire1Config.EnableSkipNodeButton)
        {
            button?.QueueFree();
            return;
        }

        if (button == null)
        {
            button = new Button
            {
                Name = ButtonName,
                TooltipText = TrFallback("SPIRE1_UI_SKIP_NODE_TOOLTIP", "卡在房间出不去时使用：解锁地图选点，然后点击下一个要去的节点。")
            };
            button.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
            button.OffsetLeft = -250;
            button.OffsetTop = -74;
            button.OffsetRight = -16;
            button.OffsetBottom = -20;
            button.Pressed += () => OnSkipPressed(__instance, button);
            __instance.AddChild(button);
        }

        button.Text = TrFallback("SPIRE1_UI_SKIP_NODE", "跳过当前节点");

        // 每次打开地图都复位可用性——上次按下后的禁用不能延续到下一次救援（Critic P2）。
        button.Disabled = false;
    }

    private static void OnSkipPressed(NMapScreen screen, Button button)
    {
        try
        {
            screen.SetTravelEnabled(true);
            // 私有方法：重算各点位可点性（与引擎内部状态变化后的刷新等价）
            AccessTools.Method(typeof(NMapScreen), "RecalculateTravelability")
                ?.Invoke(screen, null);
            MainFile.Logger.Info("[Spire1] skip-node: travel force-enabled from map button");
            button.Disabled = true;
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error("[Spire1] skip-node failed: " + e.Message);
        }
    }

    /// <summary>TranslationServer.Translate with key-missing detection: when the key is absent
    /// the engine returns the key itself, in which case we fall back to the hardcoded string.</summary>
    private static string TrFallback(string key, string fallback)
    {
        var localized = TranslationServer.Translate(key);
        return localized == key ? fallback : (string)localized;
    }
}