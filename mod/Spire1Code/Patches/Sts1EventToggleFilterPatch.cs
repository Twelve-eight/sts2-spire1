using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Events;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// StS1 事件开关的 RUNTIME 落点 (2026-09-27 修复: EnableSts1Events 开关此前结构性失效).
/// <para>
/// 失效根因: MainFile 初始化器 (Phase2) 里对 CustomContentDictionary.ActCustomEvents /
/// SharedCustomEvents 做 RemoveAll(e is Spire1Event) -- 但这些事件实例是在 ModelDb.Init
/// (晚于所有 mod 初始化器, 引擎 ModManager 明确保证的时序) 才由 CustomEventModel(autoAdd:true)
/// 构造并入池的.初始化器阶段池里还没有任何 Spire1Event, RemoveAll 命中 0 项 = 空操作;
/// 随后 ModelDb.Init 照常把事件加进池 -- 于是开关真假都一样.RegisterType 的 once-guard
/// 只挡"重复添加", 挡不了"先删后加"(删发生在加之前, 没东西可删).
/// </para>
/// <para>
/// 正确落点: 与 <see cref="LegacyActSharedEventFilterPatch"/> 同款--在 ActModel.GenerateRooms
/// 的 Postfix 里读 _rooms.events (RoomSet.events 为 public readonly List, 可原位 RemoveAll),
/// 当事件组关闭时把本 mod 的 Spire1Event 从该幕事件池移除.GenerateRooms 是每次进幕
/// 运行期调用, 此刻配置早已加载 (晚于 ModelDb.Init), 所以开关立即生效.
/// </para>
/// <para>
/// 读档路径 (2026-10-02 补强): RunState.FromSerializable -> ActModel.FromSave 直接恢复
/// 存档里的 RoomSet (不重跑 GenerateRooms), 因此旧存档事件池可能仍含 Spire1Event.
/// 本补丁同时对 ActModel.PullNextEvent 做 Prefix, 在每次抽取事件前再次过滤, 使关闭状态
/// 对 load/save 与读档后的继续游玩同样生效.
/// </para>
/// <para>
/// 作用范围: 全部幕 (Spire1Event 经 ActCustomEvents 挂到 Overgrowth/Underdocks/Hive;
/// 未声明 Acts 的 FountainOfCurseRemoval/NoteForYourself 经 BaseLib 的 SharedCustomEvents
/// transpiler 进入 ModelDb.AllSharedEvents, 由 GenerateRooms 的 concat 覆盖到任意幕).
/// 按类型判定 (e is Spire1Event), 只影响本 mod 事件, 不碰官方 shared 事件 (那条由
/// LegacyActSharedEventFilterPatch 处理) 也不碰 AFTP/其它 mod 的事件.
/// SpireHeart 用编译期 autoAdd:false, 本就不入池, 不受影响.
/// </para>
/// <para>
/// C09 (2026-10-02) 严格 fail-closed 策略:
/// 1) 关闭时严格移除全部 Spire1Event, 不保留禁用事件.
/// 2) 若移除会让池变空 (池 100% 为 Spire1Event), 用真实引擎 API 解析出的 vanilla fallback
///    替换: 优先 <see cref="SelfHelpBook"/> 与 <see cref="ThisOrThat"/> (引擎 AllSharedEvents
///    成员, 均未 override IsAllowed, 即无条件允许); 若该查询失败, 退回 engine assembly 的
///    shared events (排除 DeprecatedEvent).这样 RoomSet.NextEvent 的
///    events[eventsVisited % events.Count] 不会除零, 且不会把禁用内容放回池中.
/// 3) fallback 解析不到时抛出带上下文的 InvalidOperationException (显式 fail closed),
///    绝不把"仍含禁用事件"或"空池"的状态交给引擎.
/// 4) If the _rooms field drifts or cannot be read, throw before entering the engine PullNextEvent path.
///    This keeps the events gate fail-closed and records an Error for investigation.
/// </para>
/// <para>
/// HARMONY 结构约束 (仓库既有事故记录, 见 AutoAnthonyRelics RunSeedTrackPatch 与
/// ChaosRelicPoolReplacementPatch 注释): 一个补丁类带两个方法级
/// [HarmonyPatch(typeof(...))] 目标时, Harmony 2.4.2 只应用最后一个目标 (离线对 sts2.dll
/// 复现过).因此本文件按目标拆成三个单目标补丁类, 全部调用本 helper:
/// GenerateRooms 池级过滤,PullNextEvent 前缀池级过滤,PullNextEvent 后缀返回值兜底.
/// </para>
/// </summary>
internal static class Sts1EventFilter
{
    private static readonly FieldInfo? RoomsField =
        AccessTools.Field(typeof(ActModel), "_rooms");

    private static bool _fieldMissingLogged;
    private static bool _allGen1ReplacedLogged;
    private static bool _pullBackstopLogged;

    private static List<EventModel>? _vanillaFallbackEvents;

    internal static void OnRoomsGenerated(ActModel act)
    {
        // r8d: 独立不可用状态优先于任何 Spire1Config 静态读取 (类型初始化失败时不得把异常传播到引擎).
        if (!Spire1PowersGate.ContentUnavailableActive
            && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Events))
        {
            return;
        }
        FilterEvents(act, "GenerateRooms");
    }

    internal static void BeforePull(ActModel act)
    {
        // r8d: 独立不可用状态优先于任何 Spire1Config 静态读取.
        if (!Spire1PowersGate.ContentUnavailableActive
            && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Events))
        {
            return;
        }
        FilterEvents(act, "PullNextEvent");
    }

    /// <summary>
    /// 兜底落点: 即使 ActModel._rooms 字段漂移导致池级过滤无法运行, 也不把关闭状态下的
    /// Spire1Event 交给事件房.Postfix 观察最终返回值, 命中禁用类型时替换为 vanilla fallback,
    /// 并记录一次 Error 说明池级过滤漏掉了该事件 (需要调查字段/时序漂移).
    /// </summary>
    internal static void AfterPull(ref EventModel __result)
    {
        // r8d: 独立不可用状态优先于任何 Spire1Config 静态读取.
        if (!Spire1PowersGate.ContentUnavailableActive
            && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Events))
        {
            return;
        }
        if (__result is not Spire1Event)
        {
            return;
        }

        List<EventModel> fallback = GetVanillaFallbackEvents();
        if (fallback.Count == 0)
        {
            throw new InvalidOperationException(
                "[Spire1] StS1 event filter: PullNextEvent produced a gen-1 event while the events group " +
                "is off, and no vanilla fallback event could be resolved. Refusing to enter disabled " +
                "gen-1 content.");
        }

        __result = fallback[0];
        if (!_pullBackstopLogged)
        {
            _pullBackstopLogged = true;
            MainFile.Logger.Error(
                "[Spire1] StS1 event filter: PullNextEvent returned a gen-1 event even though the events " +
                "group is off; replaced the result with a vanilla fallback event. The pool-level filter " +
                "did not run or missed the event (investigate _rooms/timing drift).");
        }
    }

    private static void FilterEvents(ActModel act, string source)
    {
        RoomSet rooms = GetRoomsOrThrow(act, source);

        int before = rooms.events.Count;
        if (before == 0)
        {
            // 关闭状态下不能把空池交给 RoomSet.NextEvent (其实现执行 eventsVisited % events.Count).
            // 使用同一确定性 vanilla fallback 保持池非空; fallback 解析失败则显式 fail closed.
            List<EventModel> emptyPoolFallback = GetVanillaFallbackEvents();
            if (emptyPoolFallback.Count == 0)
            {
                throw new InvalidOperationException(
                    $"[Spire1] StS1 event filter: {act.GetType().Name} event pool was empty while the " +
                    "events content group is off, and no vanilla fallback event could be resolved.");
            }

            rooms.events.AddRange(emptyPoolFallback);
            MainFile.Logger.Warn(
                $"[Spire1] StS1 event filter ({source}): {act.GetType().Name} event pool was empty; " +
                $"added {emptyPoolFallback.Count} vanilla fallback event(s) to prevent an empty RoomSet.NextEvent pool.");
            return;
        }

        int toRemove = rooms.events.Count(e => e is Spire1Event);
        if (toRemove == 0)
        {
            return;
        }

        if (toRemove < before)
        {
            rooms.events.RemoveAll(e => e is Spire1Event);
            MainFile.Logger.Info(
                $"[Spire1] StS1 event filter ({source}): removed {toRemove} gen-1 events from " +
                $"{act.GetType().Name}; remaining {rooms.events.Count} (events content group off)");
            return;
        }

        // 池 100% 是 Spire1Event: 保留它们违反关闭 gate; 直接清空又会让 RoomSet.NextEvent
        // The next line would divide by events.Count, so an empty pool must be filled before the engine reads it.
        // Resolve the same deterministic vanilla fallback; if resolution fails, throw fail-closed.
        // This never passes a disabled event or an empty pool to the engine.
        List<EventModel> allGen1Fallback = GetVanillaFallbackEvents();
        if (allGen1Fallback.Count == 0)
        {
            throw new InvalidOperationException(
                $"[Spire1] StS1 event filter: {act.GetType().Name} event pool contained only gen-1 events, " +
                "and no vanilla fallback event could be resolved. Refusing to pull from a pool that would " +
                "otherwise contain only disabled gen-1 events (events content group off).");
        }

        rooms.events.Clear();
        rooms.events.AddRange(allGen1Fallback);
        if (!_allGen1ReplacedLogged)
        {
            _allGen1ReplacedLogged = true;
            MainFile.Logger.Warn(
                $"[Spire1] StS1 event filter ({source}): {act.GetType().Name} event pool was 100% gen-1 " +
                $"events; replaced all {before} with {allGen1Fallback.Count} vanilla fallback event(s) to keep " +
                "the pool non-empty (events content group off).");
        }
    }

    private static RoomSet GetRoomsOrThrow(ActModel act, string source)
    {
        if (RoomsField is null)
        {
            if (!_fieldMissingLogged)
            {
                _fieldMissingLogged = true;
                MainFile.Logger.Error(
                    $"[Spire1] StS1 event filter ({source}): ActModel._rooms field not found; " +
                    "refusing to call the engine event pull while the events content group is off.");
            }
            throw new InvalidOperationException(
                "[Spire1] StS1 event filter cannot fail closed because ActModel._rooms drifted; " +
                "refusing to enter the engine event-pull path.");
        }

        try
        {
            if (RoomsField.GetValue(act) is RoomSet rooms)
            {
                return rooms;
            }

            throw new InvalidOperationException("ActModel._rooms value is not a RoomSet");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                $"[Spire1] StS1 event filter ({source}): cannot read ActModel._rooms; " +
                $"refusing to enter the engine event-pull path ({e.GetType().Name}: {e.Message}).");
            throw new InvalidOperationException(
                "[Spire1] StS1 event filter cannot fail closed because ActModel._rooms is unreadable; " +
                "refusing to enter the engine event-pull path.",
                e);
        }
    }

    /// <summary>
    /// 通过真实引擎 API 解析确定性 vanilla fallback, 结果缓存且只含引擎事件:
    /// 优先 SelfHelpBook/ThisOrThat (二者 IsAllowed 为基类无条件 true, 见 engine-dllsrc);
    /// 若该查询失败, 退回 engine assembly 的 AllSharedEvents 并排除 DeprecatedEvent.
    /// 解析失败时不缓存空结果, 让下一次调用可以重试; 调用方必须把空结果转成显式 fail-closed 异常.
    /// </summary>
    private static List<EventModel> GetVanillaFallbackEvents()
    {
        if (_vanillaFallbackEvents is not null)
        {
            return _vanillaFallbackEvents;
        }

        var fallback = new List<EventModel>(2);
        try
        {
            fallback.Add(ModelDb.Event<SelfHelpBook>());
            fallback.Add(ModelDb.Event<ThisOrThat>());
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                $"[Spire1] StS1 event filter: always-allowed vanilla fallback lookup failed " +
                $"({e.GetType().Name}: {e.Message}); trying the engine shared-event list.");
        }

        if (fallback.Count > 0)
        {
            _vanillaFallbackEvents = fallback;
            return fallback;
        }

        try
        {
            fallback.AddRange(ModelDb.AllSharedEvents.Where(e =>
                e.GetType().Assembly == typeof(ModelDb).Assembly &&
                e is not DeprecatedEvent));
            if (fallback.Count > 0)
            {
                _vanillaFallbackEvents = fallback;
                return fallback;
            }
            MainFile.Logger.Error(
                "[Spire1] StS1 event filter: engine shared-event fallback resolved zero vanilla events.");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                $"[Spire1] StS1 event filter: engine shared-event fallback enumeration failed " +
                $"({e.GetType().Name}: {e.Message}).");
        }

        return fallback;
    }
}

/// <summary>
/// 单目标补丁类: 进幕生成事件池后过滤 (ActModel.GenerateRooms postfix).
/// 目标写在类级属性上, 方法级只放 [HarmonyPostfix] -- 与仓库已验证可用的
/// PerfectPoolGatePatch / RunSeedEarlyTrackSingleplayerPatch 同款, 不依赖
/// "类级空属性 + 方法级目标"这一行为有争议的组合.
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class Sts1EventToggleGenerateRoomsPatch
{
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();
    internal static MethodInfo? PostfixMethod { get; } = ResolvePostfix();

    private static MethodInfo? ResolveTarget()
    {
        try { return AccessTools.DeclaredMethod(typeof(ActModel), nameof(ActModel.GenerateRooms)); }
        catch (Exception) { return null; }
    }

    private static MethodInfo? ResolvePostfix()
    {
        try
        {
            MethodInfo? method = typeof(Sts1EventToggleGenerateRoomsPatch).GetMethod(
                nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPostfixMethod(method) ? method : null;
        }
        catch (Exception) { return null; }
    }
    [HarmonyPostfix]
    private static void Postfix(ActModel __instance) => Sts1EventFilter.OnRoomsGenerated(__instance);
}

/// <summary>
/// 单目标补丁类: 读档恢复的事件池在抽取前过滤 (ActModel.PullNextEvent prefix).
/// 与下面的 Postfix 类指向同一方法, 但彼此独立 (Harmony 允许同一方法有多个补丁类).
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
internal static class Sts1EventTogglePullNextEventPrefixPatch
{
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();
    internal static MethodInfo? PrefixMethod { get; } = ResolvePrefix();

    private static MethodInfo? ResolveTarget()
    {
        try { return AccessTools.DeclaredMethod(typeof(ActModel), nameof(ActModel.PullNextEvent)); }
        catch (Exception) { return null; }
    }

    private static MethodInfo? ResolvePrefix()
    {
        try
        {
            MethodInfo? method = typeof(Sts1EventTogglePullNextEventPrefixPatch).GetMethod(
                nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPatchMethod(method) ? method : null;
        }
        catch (Exception) { return null; }
    }
    [HarmonyPrefix]
    private static void Prefix(ActModel __instance) => Sts1EventFilter.BeforePull(__instance);
}

/// <summary>单目标补丁类: 抽取结果兜底替换 (ActModel.PullNextEvent postfix).</summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
internal static class Sts1EventTogglePullNextEventPostfixPatch
{
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();
    internal static MethodInfo? PostfixMethod { get; } = ResolvePostfix();

    private static MethodInfo? ResolveTarget()
    {
        try { return AccessTools.DeclaredMethod(typeof(ActModel), nameof(ActModel.PullNextEvent)); }
        catch (Exception) { return null; }
    }

    private static MethodInfo? ResolvePostfix()
    {
        try
        {
            MethodInfo? method = typeof(Sts1EventTogglePullNextEventPostfixPatch).GetMethod(
                nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPostfixMethod(method) ? method : null;
        }
        catch (Exception) { return null; }
    }
    [HarmonyPostfix]
    private static void Postfix(ref EventModel __result) => Sts1EventFilter.AfterPull(ref __result);
}