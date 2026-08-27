using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using System.Collections.Generic;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 第三方 mod 本地化修复（2026-08-27）。
/// <para>
/// 引擎 loc 合并顺序（dllsrc LocManager.LoadTablesFromPath L454-480）：每个 mod 的同名表按
/// <c>_mods</c> 加载顺序依次 <c>LocTable.MergeWith</c>（键级覆盖，后者胜）。用户目录的
/// localization_override 在 mod 表之前合并，覆盖不了 mod 文本。加载顺序由依赖图决定，
/// 我们无法保证排在目标 mod 之后——所以对跨 mod 文本缺陷，唯一顺序无关的落点是
/// <c>SetLanguageInternal</c>（全部合并完成后）的 Postfix 直改表。
/// </para>
/// <para>
/// 当前修复：AFTP 工坊版 1.0.5 的 zhs events.json 用了中文变量名 <c>{离开Cost}</c>（fork 源码
/// fbb87dc 已改为 <c>{LeaveCost}</c>，但工坊 pck 从未重发）——SmartFormat 把中文字符判为
/// invalid selector，知悉头骨事件的离开选项文本渲染失败（日志 3 处/局 + 玩家看到原始串）。
/// </para>
/// </summary>
[HarmonyPatch(typeof(LocManager), "SetLanguageInternal")]
internal static class ThirdPartyLocFixPatch
{
    [HarmonyPostfix]
    private static void FixAftpKnowingSkull(LocManager __instance, string language)
    {
        if (language != "zhs")
        {
            return;
        }

        // 与 AFTP 事件类 KnowingSkull.UpdateDynamicVars 注册的变量名一致：
        // LeaveCost / CardCost / GoldCost / PotionCost。仅修中文变量名缺陷键。
        var fixes = new Dictionary<string, string>
        {
            ["ACTSFROMTHEPAST-KNOWING_SKULL.pages.ASK.options.LEAVE.description"] =
                "失去 [red]{LeaveCost}[/red] 点生命。",
        };

        LocTable? events = __instance.GetTable("events");
        if (events == null)
        {
            return;
        }

        events.MergeWith(fixes);
        MainFile.Logger.Info("[Spire1] third-party loc fix applied: AFTP KNOWING_SKULL zhs LEAVE description (中文变量名 {离开Cost} → {LeaveCost})");
    }
}
