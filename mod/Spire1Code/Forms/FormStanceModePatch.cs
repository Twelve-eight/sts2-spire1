using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Forms;

// MainFile scans class-level attributes. ModelDb.Init runs after all mod DLLs have loaded.
// No AssemblyLoad event, background Godot access, or changes to the existing bootstrap are needed.
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
internal static class FormStanceModePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void AfterModelDbInit() => FormStanceWatcherBridge.TryBind();
}
