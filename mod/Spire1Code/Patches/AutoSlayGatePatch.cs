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
