using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 火堆黑屏通用救援（对所有幕生效，含原版/AFTP/自研）。
/// <para>
/// 引擎事实（dllsrc）：NRestSiteRoom._Ready 调 ActModel.CreateRestSiteBackground() 后立刻
/// <c>control.GetNode&lt;Control&gt;("%RestSiteLighting")</c> —— 非 OrNull，场景实例化抛异常或缺
/// "%RestSiteLighting" 节点都会让整个火堆房间初始化失败 = 进入黑屏。AFTP 三幕使用自定义
/// tscn（overgrowth/hive/glory_rest_site.tscn），在多人资产同步竞态下正是触发面。
/// </para>
/// <para>
/// 双保险：Finalizer 捕获创建异常→用纯色暗底替代并记日志；Postfix 保证灯光节点必然存在。
/// 全部为确定性视觉节点操作，无状态改动、无双端分歧风险。
/// </para>
/// </summary>
[HarmonyPatch(typeof(ActModel), "CreateRestSiteBackground")]
internal static class RestSiteLightingRescuePatch
{
    [HarmonyFinalizer]
    private static Control OnCreateFailed(System.Exception __exception, ActModel __instance, ref Control __result)
    {
        if (__exception == null)
        {
            return null; // 原方法成功，走 Postfix 校验
        }

        MainFile.Logger.Error(
            "[Spire1] rest-site background creation threw on "
            + __instance.GetType().Name + " → substituting flat fallback. Cause: " + __exception.Message);

        __result = BuildFallback();
        return null; // 吞掉异常，房间继续初始化
    }

    [HarmonyPostfix]
    private static void EnsureLighting(Control __result)
    {
        if (__result == null || __result.GetNodeOrNull<Control>("%RestSiteLighting") != null)
        {
            return;
        }

        MainFile.Logger.Warn("[Spire1] rest-site background missing %RestSiteLighting → injecting");
        __result.AddChild(new Control { Name = "RestSiteLighting" });
    }

    /// <summary>极简可用背景：全屏深色底 + 必备灯光节点。</summary>
    private static Control BuildFallback()
    {
        var root = new Control { Name = "Spire1RestFallback" };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var dim = new ColorRect { Color = new Color(0.05f, 0.04f, 0.03f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(dim);

        root.AddChild(new Control { Name = "RestSiteLighting" });
        return root;
    }
}
