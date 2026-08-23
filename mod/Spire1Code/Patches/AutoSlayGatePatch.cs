using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Opens the engine's built-in AutoSlay smoke-test path (NGame line ~694: the
/// <c>--autoslay</c> branch is gated behind <c>!IsReleaseGame()</c>, and the shipped build
/// hardcodes <c>IsReleaseGame() => true</c>). When the process was launched with
/// <c>--autoslay</c>, this prefix makes that check pass so the engine itself launches the
/// main menu, starts a run with <c>--seed</c>, auto-plays every combat/event/reward through
/// <see cref="AutoSlayer"/>, and exits with a status code. Normal launches never pass
/// <c>--autoslay</c>, so gameplay is untouched.
/// </summary>
[HarmonyPatch(typeof(NGame))]
internal static class AutoSlayGatePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(NGame.IsReleaseGame))]
    static bool ForceDevBehavior(ref bool __result)
    {
        if (!OS.GetCmdlineArgs().Any(a => a.TrimStart('-').Equals("autoslay", StringComparison.OrdinalIgnoreCase)))
        {
            return true; // normal release behavior
        }
        __result = false;
        return false; // skip original — report "not a release game" for this launch
    }
}

/// <summary>
/// <c>--autoslay</c>-only immortality, SCOPED TO COMBAT ROOMS: HP loss on a player creature is
/// zeroed only while the current room is Monster/Elite/Boss, so the auto-player survives every
/// fight yet out-of-combat real damage stays lethal. That scoping matters: the vanilla victory
/// sequence executes the player for real (<c>CreatureCmd.cs:533</c> —
/// <c>LoseHpInternal(currentHp, Unblockable|Unpowered)</c> during TheArchitect dialogue), and an
/// unscoped patch blocked that execution, so no game-over screen ever appeared and AutoSlayer
/// stalled at the very end of a won run (seed P1SMOKE1, Act 4 Floor 5).
/// Normal launches never pass <c>--autoslay</c>, so gameplay is untouched.
/// <para>
/// Entry points covered: <c>Creature.LoseHpInternal(decimal, ValueProp)</c> (the HP-mutation
/// choke point) and direct sets via <c>SetCurrentHpInternal</c> (floored at 1). Enemy creatures
/// are untouched — they must still die for runs to progress.
/// </para>
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature))]
internal static class AutoSlayImmortalityPatch
{
    internal static readonly bool Active = OS.GetCmdlineArgs().Any(
        a => a.TrimStart('-').Equals("autoslay", StringComparison.OrdinalIgnoreCase));

    /// <summary>Fail-safe default is IMMORTAL (state unreadable mid-combat must not kill the run).</summary>
    internal static bool InCombatRoom()
    {
        try
        {
            return RunManager.Instance.DebugOnlyGetState().CurrentRoom?.RoomType
                is MegaCrit.Sts2.Core.Rooms.RoomType.Monster
                    or MegaCrit.Sts2.Core.Rooms.RoomType.Elite
                    or MegaCrit.Sts2.Core.Rooms.RoomType.Boss;
        }
        catch
        {
            return true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("LoseHpInternal")]
    static void ZeroPlayerHpLoss(MegaCrit.Sts2.Core.Entities.Creatures.Creature __instance, ref decimal amount)
    {
        if (Active && __instance.IsPlayer && InCombatRoom())
        {
            amount = 0m;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetCurrentHpInternal")]
    static void FloorPlayerHpSet(MegaCrit.Sts2.Core.Entities.Creatures.Creature __instance, ref decimal amount)
    {
        if (Active && __instance.IsPlayer && amount <= 0m && InCombatRoom())
        {
            amount = 1m;
        }
    }
}
