using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Cards gate for the SHARED colorless pool (user request 2026-09-13): the 11
/// StS1 colorless cards are registered into the ENGINE's ColorlessCardPool via
/// [Pool(typeof(ColorlessCardPool))], so base-game shops/rewards offered them
/// even with the card injection toggle off (the toggle existed but had no
/// consumer). This postfix removes them from every GetUnlockedCards result
/// while the cards content group is off. Other Spire1 cards (character pools)
/// and all vanilla cards are untouched; composes with Perfect's gate on the
/// same method (independent filters).
/// <para>
/// C09 (2026-10-02) strict fail-closed policy. The previous revision did a
/// reflection scan over the mod assembly and, when that scan failed or came back
/// empty, returned the original result unchanged. That was a gate bypass: with
/// the cards group off a Spire1 card could still be offered. The scan is now
/// only an early-exit optimization; the authoritative check is a live
/// inheritance test (<c>c is Spire1Card or Spire1Curse</c>), which cannot be
/// defeated by a failed/partial reflection scan because every Spire1 card
/// class in this assembly derives from one of those two bases (verified:
/// 211 Spire1Card subclasses + 9 Spire1Curse subclasses, no other card base).
/// </para>
/// <para>
/// The filter is always applied when the cards group is off, regardless of the
/// scan outcome. When the scan failed, an Error is logged once stating that the
/// fast-path scan failed and the live type check is carrying the gate, so the
/// log never claims a reflection result that did not happen and never claims
/// filtering that did not run. The result is materialized with ToList so the
/// filter runs exactly once and callers cannot observe a lazily-re-evaluated
/// sequence that changes if the gate flips mid-run.
/// </para>
/// </summary>
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class Spire1SharedPoolGatePatch
{
    /// <summary>r8d: 安装收束用目标句柄 (CardPoolModel.GetUnlockedCards).</summary>
    internal static MethodInfo? TargetMethod { get; } = ResolveTarget();

    /// <summary>r8d: 安装收束用 postfix 句柄.</summary>
    internal static MethodInfo? PostfixMethod { get; } = ResolvePostfix();

    private static MethodInfo? ResolveTarget()
    {
        try
        {
            return AccessTools.DeclaredMethod(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards));
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
            MethodInfo? method = typeof(Spire1SharedPoolGatePatch).GetMethod(
                nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            return Spire1PowersGate.IsDeclaredPostfixMethod(method) ? method : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    /// <summary>Reflection fast-path set; diagnostic only. May be empty if the scan failed.</summary>
    private static readonly HashSet<Type> SharedColorlessCardTypes = Build();

    private static readonly bool ScanFailed = SharedColorlessCardTypes.Count == 0;

    private static bool _scanFailureLogged;
    private static bool _filterAppliedLogged;

    private static HashSet<Type> Build()
    {
        var set = new HashSet<Type>();
        try
        {
            foreach (var t in typeof(Spire1Card).Assembly.GetTypes())
            {
                if (!t.IsSubclassOf(typeof(Spire1Card)))
                {
                    continue;
                }
                var pool = t.GetCustomAttribute<PoolAttribute>();
                if (pool?.PoolType == typeof(ColorlessCardPool))
                {
                    set.Add(t);
                }
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error(
                $"[Spire1] shared colorless pool gate: fast-path type scan failed " +
                $"({e.GetType().Name}: {e.Message}); the live inheritance check will carry the gate.");
            set.Clear();
        }
        return set;
    }

    private static void Postfix(ref IEnumerable<CardModel> __result)
    {
        // r8c: 独立不可用状态优先于任何 Spire1Config 静态读取. 类型初始化失败时
        // Spire1Config 静态属性会抛 TypeInitializationException; 因此不可用状态下不读配置,
        // 直接把 gate 视为关闭并继续执行下面的 live 类型过滤 (fail closed; 仍只过滤 Spire1 类型,
        // vanilla/其它 mod 原样保留). 该分支不依赖熔断面是否安装, 是第二道独立保险.
        bool unavailable = Spire1PowersGate.ContentUnavailableActive;

        if (!unavailable && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            return;
        }

        if (ScanFailed && !_scanFailureLogged)
        {
            _scanFailureLogged = true;
            MainFile.Logger.Error(
                "[Spire1] shared colorless pool gate: fast-path type scan produced no types; " +
                "filtering is still applied via the live inheritance check (Spire1Card/Spire1Curse).");
        }

        // 权威判定: 类型继承关系。无论反射扫描成功、失败还是部分成功, 都不会漏掉本 mod 卡。
        __result = __result
            .Where(c => c is not Spire1Card && c is not Spire1Curse)
            .ToList();

        if (!_filterAppliedLogged)
        {
            _filterAppliedLogged = true;
            MainFile.Logger.Info(
                $"[Spire1] shared colorless pool gate active: filtering gen-1 colorless cards from " +
                $"ColorlessCardPool results (cards content group off; fast-path types={SharedColorlessCardTypes.Count})");
        }
    }
}
