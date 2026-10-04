using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
/// 该存档自己的快照恢复 -- 使切换设置只影响之后新开的对局, 而任一存档始终以创建时的值运行.
/// <para>
/// 本文件按"一目标一类"拆分: 原单类多方法级目标的写法在 Harmony 2.4.2 下只会应用最后一个
/// 目标 (仓库离线复现, 见 AutoAnthonyRelics RunSeedTrackPatch.cs 注释与 C09 报告).现在每个
/// 真实目标都有独立的 patch class, 类级 [HarmonyPatch(typeof(...), nameof(...))] 承载目标,
/// 方法级只放 [HarmonyPrefix]/[HarmonyPostfix]/[HarmonyFinalizer]; 共享逻辑在
/// <see cref="Spire1ContentSnapshotShared"/>.
/// </para>
/// <para>
/// 落点 (r3: 快照在进入 RunState 之前被过滤, 不再依赖反射 setter):
/// 1) <c>RunState.CreateForNewRun</c> 后缀 (本类): 新局锁存.
/// 2) <c>RunState.FromSerializable</c> 前缀+后缀 (Spire1ContentSnapshotRestoreOnLoadPatch /
///    Spire1ContentSnapshotStripFromActiveRunPatch): 读档恢复, 并把该存档自己的登记值绑定到具体
///    RunState 实例, 供后续 ToSave 按被保存实例读取.
/// 3) <c>RunState.CreateShared</c> 前缀 (Spire1ContentSnapshotFilterBeforeConstructionPatch):
///    在 RunState 构造前从 modifiers 输入中过滤本 mod 快照, 使快照不进入活动 Modifiers,
///    从根上避免 hook 枚举与 NTopBar UI 看到它.
/// 4) <c>RunManager.ToSave</c> 前缀+后缀 (Spire1ContentSnapshotAttachOnSavePatch): 保存时按
///    ToSave body 开始时捕获的 RunState 实例幂等补写.
/// 5) <c>RunManager.CanonicalizeSave</c> 前缀+后缀 (Spire1ContentSnapshotAttachAfterCanonicalizePatch): 联机补写.
/// 6) <c>RunManager.CleanUp</c> 前缀+Finalizer (Spire1ContentSnapshotCleanUpPatch): 离局复位, 异常路径保持原异常.
/// 7) <c>RunState.FromSerializable</c> 后缀 (Spire1ContentSnapshotStripFromActiveRunPatch): 过滤已安装
///    但快照仍进入活动表时的 fail-closed 剥离; 过滤未安装时读档前缀直接拒绝, 后缀不会运行.
/// </para>
/// <para>
/// 该修正不进 Neow/每日的 Good/Bad 修正池(那是引擎硬编码列表, 不含 mod 类型).构造前过滤
/// 成功时它不会进入活动 <c>RunState.Modifiers</c>, 因此没有玩法副作用, 也不会出现在 NTopBar
/// 修正栏; 构造前过滤未安装时, 读档前缀直接 fail-closed 拒绝该次读档, 后缀剥离不会运行;
/// 过滤已安装但未移除快照时, 后缀尝试剥离, 剥离失败则显式抛出 fail-closed 异常, 不宣称已隔离.
/// 多人时快照随 SerializableRun 传给对等端, 各端经 FromSerializable 采用房主的值, 保证卡池登记一致.
/// </para>
/// </summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
internal static class Spire1ContentSnapshotPatch
{
    /// <summary>新对局: 用当前全局设置锁存本局登记决定.</summary>
    [HarmonyPostfix]
    private static void LatchOnNewRun(RunState __result)
    {
        bool registerContent = Spire1Config.RegisterContentNextRun;
        Spire1RunContent.LatchForNewRun(__result, registerContent);
        MainFile.Logger.Info(
            $"[Spire1] new-run content latch: RegisterContentNextRun={registerContent}");
    }
}

/// <summary>读档前: 从该存档的快照恢复本局登记决定(缺快照 -> true, 保持旧存档旧行为);
/// 过滤补丁未确认安装时直接 fail-closed 拒绝, 不构造 RunState.</summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class Spire1ContentSnapshotRestoreOnLoadPatch
{
    [HarmonyPrefix]
    private static void RestoreOnLoad(SerializableRun save, out object? __state)
    {
        // Capture before the fail-closed check: if this prefix throws, the finalizer must still
        // restore exactly the caller's context instead of clearing an existing ambient binding.
        __state = Spire1RunContent.CaptureAmbient();
        if (!Spire1ContentSnapshotShared.FilterInstalled)
        {
            throw new InvalidOperationException(
                "[Spire1] snapshot load refused: the construction-time filter is NOT installed, " +
                "so the snapshot could survive into the active Modifiers list and become visible " +
                "to hooks/NTopBar (fail closed).");
        }
        bool registered = Spire1ContentSnapshotShared.ReadSnapshot(save, out bool found);
        Spire1RunContent.RestoreFromSave(registered);
        MainFile.Logger.Info(
            $"[Spire1] load-run content latch: snapshot={(found ? registered.ToString() : "absent->true")}");
    }

    /// <summary>
    /// C14 r13: a successful FromSerializable is an active normal load, so it keeps the value it
    /// latched and only releases the capture marker (no rewind). A failed load (engine throws
    /// after construction, or the strip postfix refuses) must not leave this load's temporary
    /// latch value behind: the finalizer rewinds exactly this call's own write, and only while
    /// that write is still the latest publish. The run's own value is re-bound in the postfix.
    /// </summary>
    [HarmonyFinalizer]
    private static Exception? FinalizeLatchOnLoad(Exception? __exception, object? __state)
    {
        if (__exception is null)
        {
            Spire1RunContent.CommitAmbient(__state);
            return null;
        }

        Spire1RunContent.RestoreAmbient(__state);
        MainFile.Logger.Error(
            "[Spire1] load-run content latch: FromSerializable failed; pre-load latch context restored (fail closed).");
        return __exception;
    }
}

/// <summary>
/// 构造前过滤: <c>RunState.CreateShared</c> 是 CreateForNewRun/CreateForTest/FromSerializable 的
/// 唯一共同构造入口, 其 modifiers 参数会原样赋给私有构造函数里的 <c>RunState.Modifiers</c>
/// (引擎 RunState.cs:319-344).前缀成功安装并执行时移除本 mod 快照, 快照就不会进入活动修正表,
/// 因此不会被 <c>RunState.IterateHookListeners</c> 枚举, 也不会被 <c>NTopBar.Initialize</c> 渲染;
/// 前缀未安装时由读档前缀的 fail-closed 拒绝路径拦截(后缀不会运行), 过滤已安装但未移除快照时
/// 由读档后缀的 fail-closed 剥离兜底, 不宣称无条件隔离.
/// 这是比"构造后反射改私有 setter"更早, 可证明且不依赖反射成功的边界.
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
            // Prepare() 已失败; 不能把缺失目标记成已安装.
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
    /// <see cref="Spire1RunContent"/>, 这里过滤不会丢失每局登记决定.
    /// </summary>
    [HarmonyPrefix]
    private static void FilterSnapshotModifiers(ref IReadOnlyList<ModifierModel> modifiers)
    {
        if (!Spire1ContentSnapshotShared.FilterInstalled)
        {
            throw new InvalidOperationException(
                "[Spire1] snapshot filter refused to construct RunState: the construction-time " +
                "filter was not confirmed installed, so the snapshot could enter the active " +
                "Modifiers list and become visible to hooks/NTopBar (fail closed).");
        }
        if (modifiers is null)
        {
            return;
        }
        modifiers = Spire1ContentSnapshotShared.FilterSnapshot(modifiers, out _);
    }
}

/// <summary>读档后: 把这枚快照修正从活动修正表剥离, 使它不作为玩法修正参与运行; 同时把该存档
/// 自己的登记值绑定到具体 RunState 实例, 供后续 ToSave 按被保存实例读取.
/// 剥离只在过滤已安装但快照仍进入活动表时才会实际执行(过滤未安装时读档前缀已 fail-closed 拒绝,
/// 后缀不会运行); 实例绑定部分是每次读档都必须完成的正常路径.</summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class Spire1ContentSnapshotStripFromActiveRunPatch
{
    [HarmonyPostfix]
    private static void StripAndBindFromActiveRun(RunState __result, SerializableRun __0)
    {
        // 先完成 fail-closed 剥离: 只有剥离成功(或本就没有快照)才承认这枚 RunState 可玩.
        Spire1ContentSnapshotShared.StripFromActiveRun(__result);
        // 再把该存档自己的登记值绑定到具体实例; ToSave 只信这个实例绑定, 不信全局闩锁.
        Spire1ContentSnapshotShared.BindLoadedRun(__result, __0);
    }
}

/// <summary>保存后: 只把 ToSave body 开始时捕获的那个 RunState 实例的绑定值(幂等)写入存档快照.
/// 绝不读全局闩锁/Ambient, 也不在 postfix 重读 RunManager.State: 交错保存两个 RunState 时,
/// 被捕获实例的绑定是唯一权威来源; 捕获失败或实例没有绑定(异常状态)时明确 fail closed,
/// 不猜测, 不写入其它局的值.</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.ToSave))]
internal static class Spire1ContentSnapshotAttachOnSavePatch
{
    /// <summary>ToSave body 开始时捕获它将要序列化的确切 RunState 引用; State 为 null 时
    /// 直接 fail closed, 不进入 body, 也不在 postfix 猜测.</summary>
    [HarmonyPrefix]
    private static bool CaptureStateOnSave(RunManager __instance, out RunState? __state)
    {
        RunState? state = __instance.DebugOnlyGetState();
        if (state is null)
        {
            __state = null;
            throw new InvalidOperationException(
                "[Spire1] snapshot save refused: RunManager.State was null when ToSave started, " +
                "so the per-run content decision could not be attributed to a saved RunState " +
                "(fail closed).");
        }
        __state = state;
        return true;
    }

    [HarmonyPostfix]
    private static void AttachOnSave(ref SerializableRun __result, RunState? __state)
    {
        if (__state is null)
        {
            throw new InvalidOperationException(
                "[Spire1] snapshot save refused: ToSave started without a captured RunState " +
                "(fail closed).");
        }

        // Only the binding attached to the RunState captured when ToSave body started is
        // authoritative. Never read the ambient/process carrier or re-read RunManager.State
        // here: an interleaved load, settings write or run switch could otherwise persist
        // another run's value into this save.
        bool? bound = Spire1RunContent.TryReadInstanceBinding(__state);
        if (bound is null)
        {
            throw new InvalidOperationException(
                "[Spire1] snapshot save refused: the RunState being saved has no bound content " +
                "decision, so writing the global latch value could persist a foreign run's value " +
                "(fail closed).");
        }
        Spire1ContentSnapshotShared.AttachSnapshot(__result, bound.Value);
    }
}

/// <summary>联机读档: CanonicalizeSave 内部经 FromSerializable (其后缀剥离活动局快照并绑定临时实例)
/// 后用 runState.Modifiers 重建 SerializableRun, 因此剥离后的快照不会留在结果里.若不补写,
/// 联机存档 canonicalize 后会丢失每局登记决定并在后续 FromSerializable 回落 true.
/// 补写值只读输入参数 __0 自己的快照, 不读全局闩锁; prefix/finalizer 成对捕获调用方的
/// Ambient/process fallback/Cards gate, body 抛异常时按版本回退本次写入, 成功时 postfix 先提交
/// 标记再由 finalizer 释放, 不把 canonicalize 的临时值留给调用方.回退只在本次写入仍是最新
/// 发布时执行, 不覆盖其它线程或设置开关的新值.
/// 与 AutoAnthony 的 CanonicalizedMultiplayerSnapshotPatch 同款落点.</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CanonicalizeSave))]
internal static class Spire1ContentSnapshotAttachAfterCanonicalizePatch
{
    /// <summary>
    /// C14 r13: capture the caller's ambient context before the engine body runs, so the
    /// finalizer can restore it on every exit path (success, fault, or postfix refusal).
    /// </summary>
    [HarmonyPrefix]
    private static void CaptureCanonicalizeContext(out object? __state)
    {
        __state = Spire1RunContent.CaptureAmbient();
    }

    [HarmonyPostfix]
    private static void AttachAfterCanonicalize(SerializableRun __0, ref SerializableRun __result)
    {
        bool registered = Spire1ContentSnapshotShared.ReadSnapshot(__0, out _);
        Spire1ContentSnapshotShared.AttachSnapshot(__result, registered);
    }

    /// <summary>
    /// C14 r13: CanonicalizeSave is a static method and may run outside a run context. Its latch
    /// write is temporary: the finalizer restores the caller's ambient context and rewinds the
    /// process fallback and combined Cards gate only while this call's own write is still the
    /// latest publish, so a newer write from another thread or a settings toggle is preserved.
    /// This runs on success and on every fault path, and rethrows the original exception.
    /// </summary>
    [HarmonyFinalizer]
    private static Exception? RestoreCanonicalizeContext(Exception? __exception, object? __state)
    {
        Spire1RunContent.RestoreAmbient(__state);
        return __exception;
    }
}

/// <summary>测试局: 以 true 绑定测试 RunState 实例, 不继承上一局进程级值.该绑定经
/// <see cref="Spire1RunContent.LatchForNewRun"/> 发布, 因此与正常新局一样会改写全局闩锁与
/// combined Cards gate(true), 并依赖同一条 RunManager.CleanUp 路径复位, 不是只读实例绑定.</summary>
[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForTest))]
internal static class Spire1ContentSnapshotCreateForTestPatch
{
    [HarmonyPostfix]
    private static void BindTestRunContext(RunState __result)
    {
        // LatchForNewRun publishes the combined Cards gate and binds this exact RunState to true;
        // a test run must neither inherit the previous run's latch nor leave that latch behind.
        Spire1RunContent.LatchForNewRun(__result, true);
    }
}

/// <summary>清理前记录是否真的有活动对局; 离开对局后复位闩锁, 异常路径保持原异常.
/// 前缀与 Finalizer 同属一个真实目标 (RunManager.CleanUp), 因此可以放在同一个单目标类中.</summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class Spire1ContentSnapshotCleanUpPatch
{
    /// <summary>清理前记录是否真的有活动对局; 主菜单/错误路径的 State == null 早退不算.</summary>
    [HarmonyPrefix]
    private static void CaptureActiveRunBeforeCleanUp(RunManager __instance, out bool __state)
    {
        __state = __instance.IsInProgress;
    }

    /// <summary>离开对局后: 复位闩锁, 不把上一局的 enabled 状态留在主菜单/下一次建局之前.
    /// 用 Finalizer 保证清理抛异常时也复位; 复位幂等, 且只在清理前确实有活动对局时执行.</summary>
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
/// 共享 helper (无 Harmony 目标): 快照读写, 构造前过滤与安全回退剥离逻辑.每个 patch class
/// 只负责绑定目标, 语义集中在这里.
/// </summary>
internal static class Spire1ContentSnapshotShared
{
    /// <summary>
    /// Write the caller-supplied per-run decision (idempotent) into the SerializableRun snapshot:
    /// remove any previous entry by id, then append one. The value MUST come from the concrete
    /// RunState being saved, never from a process-wide latch, so interleaved saves cannot swap
    /// two runs' decisions.
    /// </summary>
    internal static void AttachSnapshot(SerializableRun result, bool contentRegistered)
    {
        ModelId snapshotId = ModelDb.Modifier<Spire1ContentSnapshotModifier>().Id;
        result.Modifiers.RemoveAll(m => m.Id == snapshotId);

        var snapshot = (Spire1ContentSnapshotModifier)
            ModelDb.Modifier<Spire1ContentSnapshotModifier>().ToMutable();
        snapshot.ContentRegistered = contentRegistered;
        result.Modifiers.Add(snapshot.ToSerializable());
    }

    /// <summary>从 modifiers 输入中移除本 mod 快照, 保持其余修正的顺序与引用.
    /// <paramref name="removed"/> 报告是否确实移除了至少一项.</summary>
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
    /// 读档后安全回退: 若活动修正表里仍有本 mod 快照(构造前过滤已安装但没有移除, 或引擎绕过),
    /// 尝试把它剥离.正常路径下活动表已无快照, 直接返回.剥离失败时快照仍可能被 hook/UI 看到,
    /// 因此必须明确记录失败, 不得宣称已剥离.
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
            throw FailClosedStrip(
                runState,
                "property-missing",
                "RunState.Modifiers property not found (" + filterContext + ") - the snapshot is " +
                "still in the active modifier list and may be enumerated by hooks and rendered by " +
                "NTopBar; refusing to continue (strip NOT applied).");
        }

        try
        {
            ModifiersProp.SetValue(runState, kept);
        }
        catch (Exception e)
        {
            throw FailClosedStrip(
                runState,
                "setter-threw",
                $"RunState.Modifiers setter threw {e.GetType().Name}: {e.Message} ({filterContext}) - " +
                "the snapshot is still in the active modifier list and may be enumerated by hooks " +
                "and rendered by NTopBar; refusing to continue (strip NOT applied).");
        }

        // 赋值成功后再校验一次: 引擎若忽略赋值或产生新副本, 这里必须按失败处理.
        IReadOnlyList<ModifierModel> after = runState.Modifiers;
        if (after.Any(m => m is Spire1ContentSnapshotModifier))
        {
            throw FailClosedStrip(
                runState,
                "verify-failed",
                "RunState.Modifiers assignment succeeded but the snapshot is still present (" +
                filterContext + ") (strip NOT effective); it may be enumerated by hooks and " +
                "rendered by NTopBar; refusing to continue.");
        }
    }

    /// <summary>
    /// P1-A fail-closed helper: log the failure once per RunState instance and explicit failure
    /// kind, then return the exception the caller must throw. The kind is passed by the call site
    /// instead of being re-derived from the detail string, so the throttle key stays stable when
    /// a detail embeds an exception message. Never returns normally by accident.
    /// </summary>
    private static InvalidOperationException FailClosedStrip(
        RunState runState, string failureKind, string detail)
    {
        LogStripFailure(runState, failureKind, detail);
        return new InvalidOperationException("[Spire1] snapshot strip failed: " + detail);
    }

    /// <summary>从存档的 Modifiers 里读出本 mod 快照的 ContentRegistered.缺失时 found=false.</summary>
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
    /// Bind the concrete RunState produced by FromSerializable to the value carried by that
    /// save's own snapshot (absent -> true). This closes the load/save lifecycle gap: before
    /// this binding existed, a loaded run had no instance binding and AttachOnSave refused to
    /// save it. Re-reading the save argument here keeps the value tied to the save being loaded,
    /// never to the last global latch write.
    /// </summary>
    internal static void BindLoadedRun(RunState runState, SerializableRun save)
    {
        bool registered = ReadSnapshot(save, out _);
        Spire1RunContent.BindToInstance(runState, registered);
    }

    /// <summary>
    /// 解析引擎私有静态构造入口 <c>RunState.CreateShared</c>.按参数类型与名称精确匹配,
    /// 目标缺失或歧义都返回 null, 由调用方显式记录并让 Prepare 失败(不静默放行).
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

    /// <summary>构造前过滤补丁是否经 HarmonyCleanup 确认安装.诊断用, 不参与过滤决策.</summary>
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
            "Modifiers list may expose the snapshot to hooks and NTopBar. FromSerializable refuses " +
            "loads fail-closed while the filter is not confirmed installed; the postfix strip only " +
            "runs when the filter was installed but did not remove the snapshot.");
    }

    private static readonly PropertyInfo? ModifiersProp =
        AccessTools.Property(typeof(RunState), nameof(RunState.Modifiers));

    /// <summary>
    /// 失败日志按"RunState 实例 + 失败类型"独立节流: 每个实例的每种失败只记录一次,
    /// 交错多个实例或多种失败时互不覆盖.ConditionalWeakTable 以弱键持有实例, 实例被回收后
    /// 记录随之消失, 不产生静态强引用泄漏, 也不产生无界刷屏.
    /// </summary>
    private static readonly object StripLogGate = new();
    private static readonly ConditionalWeakTable<RunState, HashSet<string>> StripFailureLogs = new();

    private static void LogStripFailure(RunState runState, string failureKind, string detail)
    {
        lock (StripLogGate)
        {
            if (StripFailureLogs.TryGetValue(runState, out HashSet<string>? kinds))
            {
                if (kinds.Contains(failureKind))
                {
                    return;
                }
            }
            else
            {
                kinds = new HashSet<string>(StringComparer.Ordinal);
                StripFailureLogs.Add(runState, kinds);
            }
            kinds.Add(failureKind);
        }
        MainFile.Logger.Error($"[Spire1] snapshot strip ({failureKind}): {detail}");
    }
}
