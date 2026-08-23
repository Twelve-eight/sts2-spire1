using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

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
/// <c>--autoslay</code>-only immortality for full-run verification (act-4 Heart smoke):
/// every HP-loss application on a player creature is zeroed, so the auto-player can walk the
/// whole dungeon — including lethal boss mechanics (e.g. Act4Heart's Doom kill) — without
/// dying. Normal launches never pass <c>--autoslay</c>, so gameplay is untouched.
/// <para>
/// Two entry points are covered: <c>Creature.LoseHpInternal(decimal, ValueProp)</c> is the
/// HP-mutation choke point for combat damage and most effects; direct HP sets
/// (<c>SetCurrentHpInternal</c>, used by Doom-style kills) are floored at 1 HP. Enemy
/// creatures are untouched — they must still die for runs to progress.
/// </para>
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature))]
internal static class AutoSlayImmortalityPatch
{
    internal static readonly bool Active = OS.GetCmdlineArgs().Any(
        a => a.TrimStart('-').Equals("autoslay", StringComparison.OrdinalIgnoreCase));

    [HarmonyPrefix]
    [HarmonyPatch("LoseHpInternal")]
    static void ZeroPlayerHpLoss(MegaCrit.Sts2.Core.Entities.Creatures.Creature __instance, ref decimal amount)
    {
        if (Active && __instance.IsPlayer)
        {
            amount = 0m;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetCurrentHpInternal")]
    static void FloorPlayerHpSet(MegaCrit.Sts2.Core.Entities.Creatures.Creature __instance, ref decimal amount)
    {
        if (Active && __instance.IsPlayer && amount <= 0m)
        {
            amount = 1m;
        }
    }
}
