using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// StS1 keeps the room in COMBAT during onVictory. In 0.113, IsInProgress is already
/// false before Hook.AfterCombatVictory dispatches both early and ordinary listeners.
/// Scope the bonus to that dispatch, not the reward screen or a relic's listener order.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
internal static class MagicFlowerVictoryScopePatch
{
    private static readonly AsyncLocal<Scope?> Current = new();

    internal sealed class Scope(IRunState run, ICombatState? combat, Scope? previous)
    {
        internal readonly IRunState Run = run;
        internal readonly ICombatState? Combat = combat;
        internal readonly Scope? Previous = previous;
        internal volatile bool Active = true;
    }

    internal static bool Includes(Player player)
    {
        Scope? scope = Current.Value;
        return scope is { Active: true }
            && ReferenceEquals(scope.Run, player.RunState)
            && scope.Combat is not null
            && scope.Combat.Players.Contains(player);
    }

    [HarmonyPrefix]
    private static void Enter(IRunState runState, ICombatState? combatState, CombatRoom room, out Scope __state)
    {
        __state = new Scope(runState, ReferenceEquals(combatState, room.CombatState) ? combatState : null, Current.Value);
        Current.Value = __state;
    }

    [HarmonyPostfix]
    private static void TrackCompletion(ref Task __result, Scope? __state)
    {
        if (__state is null)
            return;
        __result = Complete(__result, __state);
    }

    private static async Task Complete(Task dispatch, Scope scope)
    {
        try
        {
            await dispatch;
        }
        finally
        {
            // Also closes contexts captured by detached work after the dispatch has ended.
            scope.Active = false;
        }
    }

    [HarmonyFinalizer]
    private static Exception? Exit(Exception? __exception, Scope? __state)
    {
        if (__state is null)
            return __exception;
        if (__exception is not null)
            __state.Active = false;
        // The async body retains its captured context; unrelated caller work does not.
        Current.Value = __state.Previous;
        return __exception;
    }
}
