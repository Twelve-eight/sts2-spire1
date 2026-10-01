using BaseLib.Abstracts;
using BaseLib.Extensions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 我方力量的小图标回退:原版 <c>PackedIconPath</c> 指向
/// <c>res://images/atlases/power_atlas.sprites/&lt;id&gt;.tres</c>,
/// 而我们不为自研力量生成图集条目(pck 中无 power_atlas),导致战斗内小图标显示
/// missing(NOPE).
/// <para>
/// 资源根修正(2026-09-22,R02-01):旧实现走
/// <c>ImageHelper.GetImagePath("powers/...")</c>,它固定返回
/// <c>res://images/...</c> -- 那是原版资源根.我们的力量图在
/// <c>res://Spire1/images/powers/</c>(<c>MainFile.ResPath</c> =
/// <c>res://Spire1</c>),所以旧路径永远不存在,图标必然退化为缺失.现在统一改走
/// <c>Spire1.Spire1Code.Extensions.StringExtensions.PowerImagePath()</c>,
/// 由它拼 <c>MainFile.ResPath + /images/powers</c>,资源缺失时再退到通用
/// <c>power.png</c>,而不是一个不存在的路径.
/// </para>
/// <para>
/// 前缀规则:与 <c>Spire1Power</c> 的显式覆盖保持一致 -- 先对 <c>Id.Entry</c> 应用
/// BaseLib 的 <c>RemovePrefix()</c>(在第一个 <c>'-'</c> 处截断,
/// <c>TypePrefix.PrefixSplitChar</c> = <c>'-'</c>),再 <c>ToLowerInvariant()</c>,
/// 最后补 <c>.png</c>.例如 <c>SPIRE1-A_THOUSAND_CUTS_POWER</c> -&gt;
/// <c>a_thousand_cuts_power.png</c>.
/// </para>
/// <para>
/// 显式覆盖优先:BaseLib 的 <c>ICustomPower</c> 前缀只在 <c>CustomPackedIconPath</c>
/// 非空时接管并跳过原方法,此时 <c>__result</c> 已是正确路径.所以本 postfix 必须先读
/// <c>ICustomPower.CustomPackedIconPath</c>,非空则原样返回,绝不覆盖.
/// 覆盖者包括 <c>Spire1Power</c> 全家族(8 个具体直接子类 + <c>StancePower</c>
/// 的 3 个具体子类)与继承 BaseLib <c>CustomTemporaryPowerModelWrapper</c> 的
/// FlexPower / PiercingWailPower;只有直接继承 <c>CustomPowerModel</c>(默认返回
/// null)的其余 47 个具体力量类才真正需要本回退.
/// </para>
/// </summary>
[HarmonyPatch(typeof(PowerModel), "PackedIconPath", MethodType.Getter)]
internal static class Spire1PowerIconFallbackPatch
{
    [HarmonyPostfix]
    private static void UsePackedPng(PowerModel __instance, ref string __result)
    {
        // 1) 仅 Spire1 命名空间的力量:原版与其它 mod 的力量一律不碰.
        if (__instance.GetType().Namespace?.StartsWith("Spire1.") != true)
        {
            return;
        }

        // 2) 显式覆盖优先:BaseLib 已按 CustomPackedIconPath 算出正确路径,
        //    保持 __result 逐字节不变.
        if (__instance is ICustomPower customPower && customPower.CustomPackedIconPath is not null)
        {
            return;
        }

        // 3) 无覆盖:用与 Spire1Power 相同的前缀规则从模组资源根取图;
        //    PowerImagePath 在资源缺失时退到 power.png.
        string entry = __instance.Id.Entry.RemovePrefix().ToLowerInvariant();
        __result = $"{entry}.png".PowerImagePath();
    }
}
