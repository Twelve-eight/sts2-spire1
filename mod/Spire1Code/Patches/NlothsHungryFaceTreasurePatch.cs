using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Marks the synchronous relic-generation consumer of Hook.ShouldGenerateTreasure.
///
/// TreasureRoomRelicSynchronizer.BeginRelicPicking is the only scope in which N'loth may suppress the owner's
/// relic. OneOffSynchronizer asks the same hook again for gold after this method returns, so that later query must
/// remain outside this scope and continue through the engine's original gold RNG/command path.
/// </summary>
[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
internal static class NlothsHungryFaceTreasurePatch
{
    [ThreadStatic]
    private static int _relicPickingDepth;

    internal static bool IsRelicPicking => _relicPickingDepth > 0;

    [HarmonyPrefix]
    private static void Enter(out int __state)
    {
        // Preserve the complete nested depth. A bool would lose an outer scope if the engine ever nests calls.
        __state = _relicPickingDepth;
        _relicPickingDepth++;
    }

    [HarmonyFinalizer]
    private static Exception? Exit(Exception? __exception, int __state)
    {
        // Finalizers run on both normal and exceptional exits. Return the original exception unchanged so a failed
        // relic-generation call cannot be converted into a silent success or leave a stale scope active.
        _relicPickingDepth = __state;
        return __exception;
    }
}
