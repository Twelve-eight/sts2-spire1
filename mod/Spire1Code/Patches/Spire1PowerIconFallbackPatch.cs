using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 我方力量的小图标回退：原版 PackedIconPath 指向 power_atlas.sprites/&lt;id&gt;.tres，
/// 而我们不为自研力量生成图集条目（pck 中无 power_atlas），导致战斗内小图标显示
/// missing（NOPE）。BaseLib 的 ICustomPower 前缀只在 CustomPackedIconPath 非空时接管，
/// 我们 60+ 个力量类不可能逐个 override —— 本 postfix 统一兜底：
/// Spire1 命名空间的力量一律改用已打包的 powers/&lt;id 小写&gt;.png（与 *_power.png 文件名一致）。
/// </summary>
[HarmonyPatch(typeof(PowerModel), "PackedIconPath", MethodType.Getter)]
internal static class Spire1PowerIconFallbackPatch
{
    [HarmonyPostfix]
    private static void UsePackedPng(PowerModel __instance, ref string __result)
    {
        if (__instance.GetType().Namespace?.StartsWith("Spire1.") != true)
        {
            return;
        }
        __result = ImageHelper.GetImagePath("powers/" + __instance.Id.Entry.ToLowerInvariant() + ".png");
    }
}
