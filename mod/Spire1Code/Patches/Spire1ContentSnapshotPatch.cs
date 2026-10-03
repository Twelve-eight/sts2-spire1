using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Run;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 把"是否登记本 mod 奖励内容进本局"这一决定, 在新对局创建时按全局设置
/// <see cref="Spire1Config.RegisterContentNextRun"/> 锁存, 随存档持久化, 并在读档时按
/// 该存档自己的快照恢复 —— 使切换设置只影响之后新开的对局, 而任一存档始终以创建时的值运行。
/// <para>
/// 本文件按"一目标一类"拆分: 原单类多方法级目标的写法在 Harmony 2.4.2 下只会应用最后一个
/// 目标 (仓库离线复现, 见 AutoAnthonyRelics RunSeedTrackPatch.cs 注释与 C09 报告)。现在每个
/// 真实目标都有独立的 patch class, 类级 [HarmonyPatch(typeof(...), nameof(...))] 承载目标,
/// 方法级只放 [HarmonyPrefix]/[HarmonyPostfix]/[HarmonyFinalizer]; 共享逻辑在
/// <see cref="Spire1ContentSnapshotShared"/>。
/// </para>
/// <para>
/// 落点 (r3: 快照在进入 RunState 之前被过滤, 不再依赖反射 setter):
/// 1) <c>RunState.CreateForNewRun</c> 后缀 (本类): 新局锁存。
/// 2) <c>RunState.FromSerializable</c> 前缀 (Spire1ContentSnapshotRestoreOnLoadPatch): 读档恢复。
/// 3) <c>RunState.CreateShared</c> 前缀 (Spire1ContentSnapshotFilterBeforeConstructionPatch):
///    在 RunState 构造前从 modifiers 输入中过滤本 mod 快照, 使快照不进入活动 Modifiers,
///    从根上避免 hook 枚举与 NTopBar UI 看到它。
/// 4) <c>RunManager.ToSave</c> 后缀 (Spire1ContentSnapshotAttachOnSavePatch): 保存时幂等补写。
/// 5) <c>RunManager.CanonicalizeSave</c> 后缀 (Spire1ContentSnapshotAttachAfterCanonicalizePatch): 联机补写。
/// 6) <c>RunManager.CleanUp</c> 前缀+Finalizer (Spire1ContentSnapshotCleanUpPatch): 离局复位, 异常路径保持原异常。
/// 7) <c>RunState.FromSerializable</c> 后缀 (Spire1ContentSnapshotStripFromActiveRunPatch): 兼容旧引擎
///    或过滤目标缺失时的安全回退, 尝试剥离活动局快照; 失败不静默。
/// </para>
/// <para>
/// 该修正不进 Neow/每日的 Good/Bad 修正池(那是引擎硬编码列表, 不含 mod 类型)。r3 起它也不会
/// 进入活动 <c>RunState.Modifiers</c>: 构造前过滤是主路径, 旧引擎/目标缺失时由后缀反射剥离兜底。
/// 因此它除充当每局存档载体外没有玩法副作用, 也不会出现在 NTopBar 修正栏。多人时快照随
/// SerializableRun 传给对等端, 各端经 FromSerializable 采用房主的值, 保证卡池登记一致。
/// </para>
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
internal static class Spire1ContentSnapshotPatch
{
    /// <summary>新对局: 用当前全局设置锁存本局登记决定。</summary>
    [HarmonyPostfix]
    private static void LatchOnNewRun()
    {
        Spire1RunContent.LatchForNewRun(Spire1Config.RegisterContentNextRun);
        MainFile.Logger.Info(
            $"[Spire1] new-run content latch: RegisterContentNextRun={Spire1Config.RegisterContentNextRun}");
    }
}

/// <summary>读档前: 从该存档的快照恢复本局登记决定(缺快照 -> true, 保持旧存档旧行为)。</summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class Spire1ContentSnapshotRestoreOnLoadPatch
{
    [HarmonyPrefix]
    private static void RestoreOnLoad(SerializableRun save)
    {
        bool registered = Spire1ContentSnapshotShared.ReadSnapshot(save, out bool found);
        Spire1RunContent.RestoreFromSave(registered);
        MainFile.Logger.Info(
            $"[Spire1] load-run content latch: snapshot={(found ? registered.ToString() : "absent->true")}");
    }
}

/// <summary>
/// 构造前过滤: <c>RunState.CreateShared</c> 是 CreateForNewRun/CreateForTest/FromSerializable 的
/// 唯一共同构造入口, 其 modifiers 参数会原样赋给私有构造函数里的 <c>RunState.Modifiers</c>
/// (引擎 RunState.cs:319-344)。在此前缀里移除本 mod 快照, 快照就不会进入活动修正表, 因此
/// 不会被 <c>RunState.IterateHookListeners</c> 枚举, 也不会被 <c>NTopBar.Initialize</c> 渲染。
/// 这是比"构造后反射改私有 setter"更早、可证明且不依赖反射成功的边界。
/// </summary>
[HarmonyPatch]
internal static class Spire1ContentSnapshotFilterBeforeConstructionPatch
{
    private static readonly MethodInfo? Target = Spire1ContentSnapshotShared.ResolveCreateShared();

    [HarmonyPrepare]
    private static bool Prepare()
    {
        if (Target is null)
        {
            Spire1ContentSnapshotShared.ReportFilterUnavailable(
                "RunState.CreateShared target not found");
            return false;
        }
        return true;
    }

    [HarmonyCleanup]
    private static void Cleanup(Exception? __exception)
    {
        if (Target is null)
        {
            // Prepare() 已失败; 不能把缺失目标记成已安装。
            return;
        }
        if (__exception is null)
        {
            Spire1ContentSnapshotShared.RecordFilterInstalled();
            return;
        }
        Spire1ContentSnapshotShared.ReportFilterUnavailable(
            "installation threw (" + __exception.GetType().Name + ")");
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        if (Target is not null)
        {
            yield return Target;
        }
    }

    /// <summary>
    /// 只替换 modifiers 输入, 其余参数原样透传; 快照值已在 RestoreOnLoad 前缀中读入
    /// <see cref="Spire1RunContent"/>, 这里过滤不会丢失每局登记决定。
    /// </summary>
    [HarmonyPrefix]
    private static void FilterSnapshotModifiers(ref IReadOnlyList<ModifierModel> modifiers)
    {
        if (modifiers is null)
        {
            return;
        }
        modifiers = Spire1ContentSnapshotShared.FilterSnapshot(modifiers, out _);
    }
}

/// <summary>读档后: 把这枚快照修正从活动修正表剥离, 使它不作为玩法修正参与运行。
/// r3 中这是构造前过滤失败/旧引擎漂移时的安全回退; 正常路径下活动表里已经没有快照,
/// 因此该后缀是幂等的空操作。</summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class Spire1ContentSnapshotStripFromActiveRunPatch
{
    [HarmonyPostfix]
    private static void StripFromActiveRun(RunState __result) =>
        Spire1ContentSnapshotShared.StripFromActiveRun(__result);
}

/// <summary>保存后: 把当前闩锁值(幂等)写入存档的快照修正。</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.ToSave))]
internal static class Spire1ContentSnapshotAttachOnSavePatch
{
    [HarmonyPostfix]
    private static void AttachOnSave(ref SerializableRun __result) =>
        Spire1ContentSnapshotShared.AttachSnapshot(__result);
}

/// <summary>联机读档: CanonicalizeSave 内部经 FromSerializable (其后缀剥离活动局快照) 后用
/// runState.Modifiers 重建 SerializableRun, 因此剥离后的快照不会留在结果里。若不补写,
/// 联机存档 canonicalize 后会丢失每局登记决定并在后续 FromSerializable 回落 true。
/// 与 AutoAnthony 的 CanonicalizedMultiplayerSnapshotPatch 同款落点。</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CanonicalizeSave))]
internal static class Spire1ContentSnapshotAttachAfterCanonicalizePatch
{
    [HarmonyPostfix]
    private static void AttachAfterCanonicalize(ref SerializableRun __result) =>
        Spire1ContentSnapshotShared.AttachSnapshot(__result);
}

/// <summary>清理前记录是否真的有活动对局; 离开对局后复位闩锁, 异常路径保持原异常。
/// 前缀与 Finalizer 同属一个真实目标 (RunManager.CleanUp), 因此可以放在同一个单目标类中。</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class Spire1ContentSnapshotCleanUpPatch
{
    /// <summary>清理前记录是否真的有活动对局; 主菜单/错误路径的 State == null 早退不算。</summary>
    [HarmonyPrefix]
    private static void CaptureActiveRunBeforeCleanUp(RunManager __instance, out bool __state)
    {
        __state = __instance.IsInProgress;
    }

    /// <summary>离开对局后: 复位闩锁, 不把上一局的 enabled 状态留在主菜单/下一次建局之前。
    /// 用 Finalizer 保证清理抛异常时也复位; 复位幂等, 且只在清理前确实有活动对局时执行。</summary>
    [HarmonyFinalizer]
    private static Exception? ResetLatchAfterCleanUp(Exception? __exception, bool __state)
    {
        if (__state)
        {
            Spire1RunContent.ResetForMenu();
            MainFile.Logger.Info("[Spire1] run content latch reset to default (menu) after run cleanup");
        }
        return __exception;
    }
}

/// <summary>
/// 共享 helper (无 Harmony 目标): 快照读写, 构造前过滤与安全回退剥离逻辑。每个 patch class
/// 只负责绑定目标, 语义集中在这里。
/// </summary>
internal static class Spire1ContentSnapshotShared
{
    /// <summary>把当前闩锁值(幂等)写入 SerializableRun 的快照修正: 先按 id 去重再追加一个。</summary>
    internal static void AttachSnapshot(SerializableRun result)
    {
        ModelId snapshotId = ModelDb.Modifier<Spire1ContentSnapshotModifier>().Id;
        result.Modifiers.RemoveAll(m => m.Id == snapshotId);

        var snapshot = (Spire1ContentSnapshotModifier)
            ModelDb.Modifier<Spire1ContentSnapshotModifier>().ToMutable();
        snapshot.ContentRegistered = Spire1RunContent.ContentActiveThisRun;
        result.Modifiers.Add(snapshot.ToSerializable());
    }

    /// <summary>从 modifiers 输入中移除本 mod 快照, 保持其余修正的顺序与引用。
    /// <paramref name="removed"/> 报告是否确实移除了至少一项。</summary>
    internal static IReadOnlyList<ModifierModel> FilterSnapshot(
        IReadOnlyList<ModifierModel> modifiers, out bool removed)
    {
        removed = false;
        int firstSnapshot = -1;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] is Spire1ContentSnapshotModifier)
            {
                firstSnapshot = i;
                break;
            }
        }

        if (firstSnapshot < 0)
        {
            return modifiers;
        }

        var kept = new List<ModifierModel>(modifiers.Count - 1);
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i] is not Spire1ContentSnapshotModifier)
            {
                kept.Add(modifiers[i]);
            }
        }
        removed = true;
        return kept;
    }

    /// <summary>
    /// 读档后安全回退: 若活动修正表里仍有本 mod 快照(构造前过滤未安装/旧引擎), 尝试把它剥离。
    /// 正常路径下活动表已无快照, 直接返回。剥离失败时快照仍可能被 hook/UI 看到, 因此必须
    /// 明确记录失败, 不得宣称已剥离。
    /// </summary>
    internal static void StripFromActiveRun(RunState runState)
    {
        IReadOnlyList<ModifierModel> current = runState.Modifiers;
        bool hasSnapshot = current.Any(m => m is Spire1ContentSnapshotModifier);
        if (!hasSnapshot)
        {
            return;
        }

        // Reaching this point means the snapshot survived into the active list: either the
        // construction-time filter was not installed (engine drift) or the engine bypassed it.
        // Record that context with every failure so the two cases stay distinguishable.
        string filterContext = FilterInstalled
            ? "construction-time filter was installed but did not remove the snapshot"
            : "construction-time filter is NOT installed";

        IReadOnlyList<ModifierModel> kept = FilterSnapshot(current, out bool removed);
        if (!removed)
        {
            return;
        }

        if (ModifiersProp is null)
        {
            LogStripFailure(
                runState,
                "RunState.Modifiers property not found (" + filterContext + ") - snapshot left in the " +
                "active modifier list; " +
                "it may be enumerated by hooks and rendered by NTopBar (strip NOT applied).");
            return;
        }

        try
        {
            ModifiersProp.SetValue(runState, kept);
        }
        catch (Exception e)
        {
            // Fail closed on reflection/engine drift: never claim the snapshot was stripped when
            // the engine refused the assignment. The snapshot value itself is re-attached
            // idempotently on save, so a failed strip cannot lose the per-run decision; but while
            // it remains in the active list it is visible to hook enumeration and NTopBar.
            LogStripFailure(
                runState,
                $"RunState.Modifiers setter threw {e.GetType().Name}: {e.Message} ({filterContext}) - " +
                "snapshot left in the active modifier list; it may be enumerated by hooks and rendered " +
                "by NTopBar (strip NOT applied).");
            return;
        }

        // 赋值成功后再校验一次: 引擎若忽略赋值或产生新副本, 这里必须按失败处理。
        IReadOnlyList<ModifierModel> after = runState.Modifiers;
        if (after.Any(m => m is Spire1ContentSnapshotModifier))
        {
            LogStripFailure(
                runState,
                "RunState.Modifiers assignment succeeded but the snapshot is still present (" +
                filterContext + ") (strip NOT effective); it may be enumerated by hooks and rendered " +
                "by NTopBar.");
        }
    }

    /// <summary>从存档的 Modifiers 里读出本 mod 快照的 ContentRegistered。缺失时 found=false。</summary>
    internal static bool ReadSnapshot(SerializableRun save, out bool found)
    {
        found = false;
        ModelId snapshotId = ModelDb.Modifier<Spire1ContentSnapshotModifier>().Id;
        SerializableModifier? sm = save.Modifiers.FirstOrDefault(m => m.Id == snapshotId);
        if (sm is null)
        {
            return true; // 旧存档/无快照: 按内容开启处理, 不改变既有可见行为
        }

        var probe = (Spire1ContentSnapshotModifier)
            ModelDb.Modifier<Spire1ContentSnapshotModifier>().ToMutable();
        sm.Props?.Fill(probe);
        found = true;
        return probe.ContentRegistered;
    }

    /// <summary>
    /// 解析引擎私有静态构造入口 <c>RunState.CreateShared</c>。按参数类型与名称精确匹配,
    /// 目标缺失或歧义都返回 null, 由调用方显式记录并让 Prepare 失败(不静默放行)。
    /// </summary>
    internal static MethodInfo? ResolveCreateShared()
    {
        try
        {
            MethodInfo? match = null;
            // Publicizer (Spire1.csproj <Publicize>True</Publicize>) can surface the original private
            // helper as public in some build configurations; include both flags so resolution is
            // independent of that and still requires an exact signature match.
            foreach (MethodInfo candidate in typeof(RunState).GetMethods(
                         BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (candidate.Name != "CreateShared" || !IsCreateSharedSignature(candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    ReportFilterUnavailable("RunState.CreateShared is ambiguous (multiple matches)");
                    return null;
                }
                match = candidate;
            }
            return match;
        }
        catch (Exception e)
        {
            ReportFilterUnavailable(
                "RunState.CreateShared resolution threw (" + e.GetType().Name + ": " + e.Message + ")");
            return null;
        }
    }

    private static bool IsCreateSharedSignature(MethodInfo method)
    {
        if (method.ReturnType != typeof(RunState) || !method.IsStatic)
        {
            return false;
        }

        ParameterInfo[] p = method.GetParameters();
        return p.Length == 9
            && p[0].ParameterType == typeof(IReadOnlyList<Player>) && p[0].Name == "players"
            && p[1].ParameterType == typeof(IReadOnlyList<ActModel>) && p[1].Name == "acts"
            && p[2].ParameterType == typeof(IReadOnlyList<ModifierModel>) && p[2].Name == "modifiers"
            && p[3].ParameterType == typeof(GameMode) && p[3].Name == "gameMode"
            && p[4].ParameterType == typeof(int) && p[4].Name == "currentActIndex"
            && p[5].ParameterType == typeof(RunRngSet) && p[5].Name == "rng"
            && p[6].ParameterType == typeof(RunOddsSet) && p[6].Name == "odds"
            && p[7].ParameterType == typeof(RelicGrabBag) && p[7].Name == "sharedRelicGrabBag"
            && p[8].ParameterType == typeof(int) && p[8].Name == "ascensionLevel";
    }

    /// <summary>构造前过滤补丁是否经 HarmonyCleanup 确认安装。诊断用, 不参与过滤决策。</summary>
    internal static bool FilterInstalled { get; private set; }

    internal static void RecordFilterInstalled()
    {
        FilterInstalled = true;
        MainFile.Logger.Info(
            "[Spire1] snapshot filter: RunState.CreateShared prefix installed; snapshot is removed " +
            "before it can enter the active Modifiers list.");
    }

    internal static void ReportFilterUnavailable(string reason)
    {
        FilterInstalled = false;
        MainFile.Logger.Error(
            "[Spire1] snapshot filter: " + reason + " - snapshot filter NOT installed; the active " +
            "Modifiers list may expose the snapshot to hooks and NTopBar. The FromSerializable " +
            "postfix strip is the only remaining fallback.");
    }

    private static readonly PropertyInfo? ModifiersProp =
        AccessTools.Property(typeof(RunState), nameof(RunState.Modifiers));

    /// <summary>
    /// 失败日志按"读档上下文 + 失败原因"节流: 同一个 RunState 实例的同一种失败只记录一次,
    /// 不同读档或不同异常仍会记录。用 <see cref="WeakReference{T}"/> 避免把 RunState 强引用
    /// 留在静态字段里; 不产生无界刷屏。
    /// </summary>
    private static readonly object StripLogGate = new();
    private static WeakReference<RunState>? _lastFailedRun;
    private static string? _lastFailureKind;

    private static void LogStripFailure(RunState runState, string detail)
    {
        string kind = ClassifyFailure(detail);
        lock (StripLogGate)
        {
            if (_lastFailedRun is not null
                && _lastFailedRun.TryGetTarget(out RunState? last)
                && ReferenceEquals(last, runState)
                && string.Equals(_lastFailureKind, kind, StringComparison.Ordinal))
            {
                return;
            }
            _lastFailedRun = new WeakReference<RunState>(runState);
            _lastFailureKind = kind;
        }
        MainFile.Logger.Error($"[Spire1] snapshot strip: {detail}");
    }

    private static string ClassifyFailure(string detail)
    {
        if (detail.StartsWith("RunState.Modifiers property not found", StringComparison.Ordinal))
        {
            return "property-missing";
        }
        if (detail.StartsWith("RunState.Modifiers setter threw", StringComparison.Ordinal))
        {
            return "setter-threw";
        }
        if (detail.StartsWith("RunState.Modifiers assignment succeeded", StringComparison.Ordinal))
        {
            return "verify-failed";
        }
        return detail;
    }
}
