using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// This is the base class for your mod's relics, which is set up to load the relic's images from your mod's resources.
/// When creating a relic, right click the Relics folder and create a new file with the Custom Relic template.
/// This will generate a class that extends this one.
/// You can also just create the class manually; just make sure to inherit from this class.
///
/// The [Pool] annotation marks this relic as being tied to your specific character. Inheriting from this class means
/// that your relics don't need to individually say which pool they should be in.
///
/// Content registration remains unconditional so ModelDb can construct canonical models and preserve old save IDs.
/// The runtime gate is applied at every supported pool and obtainment query instead of removing model types at load time.
/// </summary>
[Pool(typeof(Spire1RelicPool))]
public abstract class Spire1Relic : CustomRelicModel
{
    public override string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
    protected override string PackedIconOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
    protected override string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();

    /// <summary>
    /// Relic pool pulls, Neow checks, and RelicGrabBag cleanup all use this engine hook.
    /// Keep the per-run C01 gate here so every Spire1 relic, including relics assigned to
    /// an engine character pool, fails closed when the relic group is disabled.
    /// </summary>
    public override bool IsAllowed(global::MegaCrit.Sts2.Core.Runs.IRunState runState)
    {
        // r8d: 独立不可用状态优先, 不读 Spire1Config (静态构造失败时不得把异常传播到引擎).
        if (Spire1PowersGate.ContentUnavailableActive)
        {
            return false;
        }
        return base.IsAllowed(runState)
            && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics);
    }

    /// <summary>
    /// MerchantRelicEntry uses IsAllowedInShops rather than IsAllowed, so the same gate
    /// must cover shop generation explicitly.
    /// </summary>
    public override bool IsAllowedInShops
        => !Spire1PowersGate.ContentUnavailableActive
            && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics);
}

/// <summary>
/// RelicPoolModel.GetUnlockedRelics is the shared query used by unlock enumeration,
/// character pools, and several reward/event helpers. The engine does not call
/// RelicModel.IsAllowed from this query, so filter Spire1 models here as well.
/// </summary>
[HarmonyPatch(typeof(RelicPoolModel), nameof(RelicPoolModel.GetUnlockedRelics))]
internal static class Spire1RelicPoolGatePatch
{
    /// <summary>r8d: 安装收束用目标句柄 (RelicPoolModel.GetUnlockedRelics).</summary>
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();

    /// <summary>r8d: 安装收束用 postfix 句柄.</summary>
    internal static MethodInfo? PostfixMethod { get; } = ResolvePostfix();

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(typeof(RelicPoolModel), nameof(RelicPoolModel.GetUnlockedRelics));
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
            MethodInfo? method = typeof(Spire1RelicPoolGatePatch).GetMethod(
                nameof(FilterDisabledRelics), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPostfixMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    [HarmonyPostfix]
    private static void FilterDisabledRelics(ref IEnumerable<RelicModel> __result)
    {
        // r8d: 独立不可用状态优先于任何 Spire1Config 静态读取. 类型初始化失败时
        // Spire1Config 静态属性会抛 TypeInitializationException; 不可用状态下不读配置,
        // 直接把 gate 视为关闭并强制过滤本 mod 遗物 (vanilla/其它 mod 原样保留).
        bool unavailable = Spire1PowersGate.ContentUnavailableActive;

        if (!unavailable && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics))
        {
            return;
        }

        __result = __result.Where(relic => relic is not Spire1Relic);
    }
}