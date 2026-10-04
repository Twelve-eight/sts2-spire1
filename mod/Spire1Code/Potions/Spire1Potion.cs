using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Potions;

/// <summary>
/// Base class for Spire1 potions and their packed images.
///
/// Content registration remains unconditional so ModelDb can construct canonical models
/// and preserve old save IDs. The C01 group gate is applied to the engine's potion-pool
/// query, which is the common source for rewards, shops, events, and random generation.
/// </summary>
[Pool(typeof(Spire1PotionPool))]
public abstract class Spire1Potion : CustomPotionModel
{
    public override string? CustomPackedImagePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
    public override string? CustomPackedOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}

/// <summary>
/// PotionModel has no IsAllowed hook. Its GetUnlockedPotions query is the supported
/// common filter point for PotionFactory, merchant inventory, rewards, events, and
/// UnlockState.Potions enumeration, so filter only Spire1Potion instances here.
/// </summary>
[HarmonyPatch(typeof(PotionPoolModel), nameof(PotionPoolModel.GetUnlockedPotions))]
internal static class Spire1PotionPoolGatePatch
{
    /// <summary>r8d: 安装收束用目标句柄 (PotionPoolModel.GetUnlockedPotions).</summary>
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();

    /// <summary>r8d: 安装收束用 postfix 句柄.</summary>
    internal static MethodInfo? PostfixMethod { get; } = ResolvePostfix();

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(typeof(PotionPoolModel), nameof(PotionPoolModel.GetUnlockedPotions));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolvePostfix()
    {
        try
        {
            MethodInfo? method = typeof(Spire1PotionPoolGatePatch).GetMethod(
                nameof(FilterDisabledPotions), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPostfixMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    [HarmonyPostfix]
    private static void FilterDisabledPotions(ref IEnumerable<PotionModel> __result)
    {
        // r8d: 独立不可用状态优先于任何 Spire1Config 静态读取. 类型初始化失败时
        // Spire1Config 静态属性会抛 TypeInitializationException; 不可用状态下不读配置,
        // 直接把 gate 视为关闭并强制过滤本 mod 药水 (vanilla/其它 mod 原样保留).
        bool unavailable = Spire1PowersGate.ContentUnavailableActive;

        if (!unavailable && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Potions))
        {
            return;
        }

        __result = __result.Where(potion => potion is not Spire1Potion);
    }
}