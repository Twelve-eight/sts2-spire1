using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using Spire1.Spire1Code.Interop;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Fail-closed save-load guard for old saves that carry the standalone Forms form-stance modifier.
/// <para>
/// 背景 (r5 P1): 引擎加载存档时, <see cref="ModifierModel.FromSerializable(SerializableModifier)"/>
/// 会先调用 <c>SaveUtil.ModifierOrDeprecated(serializable.Id)</c>; 当 ModelDb 里查不到该 id 时
/// (典型场景: 独立 Forms 程序集未加载) 它返回 <c>DeprecatedModifier</c>, 于是原始
/// <c>SPIRE1-FORM_STANCE_MODIFIER</c> 身份在进入 <c>RunState</c> 之前就丢失了. 之后任何
/// 基于已解析 ModifierModel 类型的形态判定都只能看到 DeprecatedModifier, 会静默降为普通规则.
/// 本 prefix 在解析之前用 raw <see cref="SerializableModifier.Id"/> 精确比对, 命中时要求 Forms
/// 桥真实可用 (Forms 程序集存在 + entry point 签名通过 + 运行期 IsAvailable), 否则明确抛错,
/// 绝不把它降级成 DeprecatedModifier 后按普通规则继续.
/// </para>
/// <para>
/// r8: 本 guard 是独立 save 安全面, 不依赖 powers gate 的 ContentUnavailableActive. 由
/// MainFile.Initialize 最早安全阶段调用 <see cref="EnsureInstalled"/> 显式安装, 并用
/// Harmony.GetPatchInfo 精确证明 owner+prefix+priority; Phase3 属性扫描精确跳过本类型.
/// </para>
/// <para>
/// 保证范围 (明确, 不夸大): 本保护只在 Spire1 程序集内代码可执行时生效. 若 Spire1 与 Forms
/// 两个 mod 都缺失, 则本 prefix 不存在, 该环境不受本保护覆盖; 本文件不宣称覆盖那种环境.
/// </para>
/// <para>
/// 非目标: 其它 vanilla/mod modifier 与 deprecated 旧内容 (Id.Entry 不等于本常量) 一律直接放行,
/// 不受本 prefix 影响; 本文件不引用 Forms 或 Watcher 的任何编译期类型, 只经反射桥判定可用性.
/// </para>
/// </summary>
[HarmonyPatch(typeof(ModifierModel), nameof(ModifierModel.FromSerializable), new[] { typeof(SerializableModifier) })]
internal static class FormsMissingModifierSaveGuardPatch
{
    /// <summary>
    /// 独立 Forms 生产侧 <c>FormStanceModifier</c> 保留的历史 CustomID, 也是旧档 raw
    /// <see cref="SerializableModifier.Id"/> 的 <see cref="ModelId.Entry"/> 值.
    /// </summary>
    internal const string LegacyFormStanceModifierId = "SPIRE1-FORM_STANCE_MODIFIER";

    private static readonly object InstallLock = new();
    private static bool _installProven;
    private static readonly MethodInfo? TargetMethod = ResolveTarget();
    private static readonly MethodInfo? PrefixMethod = ResolvePrefix();

    /// <summary>r8: True 仅当本 guard 的精确 prefix 已由 Harmony.GetPatchInfo 证明存在.</summary>
    internal static bool InstallProven => _installProven;

    /// <summary>
    /// r8: 独立 save 安全面的幂等显式安装/复核入口. 不读取 Spire1Config, 不受
    /// ContentUnavailableActive 支配; 已有同 owner+prefix 条目时只复核不重复挂载.
    /// 目标/prefix 解析失败, 读取异常, 安装失败或精确证明失败时返回 false (fail closed).
    /// </summary>
    internal static bool EnsureInstalled(Harmony harmony)
    {
        lock (InstallLock)
        {
            if (TargetMethod is null || PrefixMethod is null)
            {
                _installProven = false;
                MainFile.Logger.Error(
                    "[Spire1] Forms save guard: target/prefix reflection unresolved - guard NOT installed; " +
                    "raw SPIRE1-FORM_STANCE_MODIFIER save protection is incomplete.");
                return false;
            }

            try
            {
                HarmonyLib.Patches? info = Harmony.GetPatchInfo(TargetMethod);
                bool ownerPrefixExists = info is not null
                    && info.Prefixes.Any(patch => IsOwnedGuardPrefix(patch, harmony));

                if (!ownerPrefixExists)
                {
                    harmony.CreateClassProcessor(typeof(FormsMissingModifierSaveGuardPatch)).Patch();
                }

                _installProven = VerifyInstalled(harmony);
            }
            catch (Exception e)
            {
                _installProven = false;
                MainFile.Logger.Error(
                    "[Spire1] Forms save guard: Harmony install/probe failed (" + e.GetType().Name + ": " +
                    e.Message + ") - guard NOT proven; raw SPIRE1-FORM_STANCE_MODIFIER save protection " +
                    "is incomplete.");
                return false;
            }

            if (_installProven)
            {
                MainFile.Logger.Info(
                    "[Spire1] Forms save guard: ModifierModel.FromSerializable prefix verified installed " +
                    "(owner=" + harmony.Id + ", priority=First).");
            }
            else
            {
                MainFile.Logger.Error(
                    "[Spire1] Forms save guard: exact owner+prefix+priority=First NOT proven; raw " +
                    "SPIRE1-FORM_STANCE_MODIFIER save protection is incomplete.");
            }

            return _installProven;
        }
    }

    /// <summary>
    /// r8: 精确证明目标方法上存在本 Harmony id 的 guard prefix, 且 priority 为 First.
    /// 任何解析异常/owner 漂移/prefix 漂移/priority 漂移都返回 false (fail closed), 不抛出.
    /// </summary>
    private static bool VerifyInstalled(Harmony harmony)
    {
        MethodInfo? target = TargetMethod;
        MethodInfo? prefix = PrefixMethod;
        if (target is null || prefix is null)
        {
            return false;
        }

        try
        {
            HarmonyLib.Patches? info = Harmony.GetPatchInfo(target);
            return info is not null
                && info.Prefixes.Any(patch =>
                    IsOwnedGuardPrefix(patch, harmony)
                    && patch.priority == (int)Priority.First);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsOwnedGuardPrefix(HarmonyLib.Patch patch, Harmony harmony)
    {
        return patch.owner == harmony.Id
            && patch.PatchMethod is { } patchMethod
            && PrefixMethod is { } prefix
            && (patchMethod == prefix || patchMethod.Equals(prefix));
    }

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(
                typeof(ModifierModel),
                nameof(ModifierModel.FromSerializable),
                new[] { typeof(SerializableModifier) });
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static MethodInfo? ResolvePrefix()
    {
        try
        {
            MethodInfo? method = typeof(FormsMissingModifierSaveGuardPatch).GetMethod(
                nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            return method is not null
                && method.GetCustomAttributes(typeof(HarmonyPrefix), false).Length == 1
                ? method
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(SerializableModifier serializable)
    {
        if (!IsLegacyFormStanceModifierId(serializable))
        {
            // vanilla / 其它 mod modifier / 普通 deprecated 旧内容: 保持引擎原有解析路径.
            return true;
        }

        // 命中已选形态身份: 解析之前先证明 Forms 桥可用 (存在 + 签名 + 运行期 IsAvailable).
        if (!FormsCompatibilityBridge.IsAvailable)
        {
            throw new InvalidOperationException(
                "[Spire1] refusing to load a save that selected the standalone Forms form-stance modifier "
                + "while the Forms mod is absent, signature-incompatible, or not runtime-available: "
                + FormsCompatibilityBridge.DisabledReason
                + " The save was not converted to DeprecatedModifier and was not loaded under ordinary "
                + "stance rules; load it with a working Forms mod, or remove the form-stance modifier.");
        }

        return true;
    }

    /// <summary>
    /// raw 身份判定: 只读 <see cref="SerializableModifier.Id"/> 的 <see cref="ModelId.Entry"/>,
    /// 精确序数比较, 不触碰 ModelDb, 不反射 Forms 类型. null / 其它 entry 一律 false.
    /// </summary>
    private static bool IsLegacyFormStanceModifierId(SerializableModifier? serializable)
    {
        ModelId? id = serializable?.Id;
        return id is not null
            && string.Equals(id.Entry, LegacyFormStanceModifierId, StringComparison.Ordinal);
    }
}