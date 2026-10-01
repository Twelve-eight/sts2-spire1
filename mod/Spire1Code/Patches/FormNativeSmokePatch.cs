using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Spire1.Spire1Code.Run;

namespace Spire1.Spire1Code.Patches;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
internal static class FormNativeSmokePatch
{
    [HarmonyPostfix]
    private static void Postfix(NGame __instance)
    {
        FormNativeSmokeRunner.TryStart(__instance);
    }
}
