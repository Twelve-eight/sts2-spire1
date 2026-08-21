using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Debug mode: appends the localization key to every mod string shown in-game,
/// e.g. "打击 (SPIRE1-STRIKE_SILENT.title)", so cards/relics can be identified and
/// spawned by id from the dev console while testing. Only keys with the SPIRE1-
/// prefix are touched; vanilla text is never modified.
/// </summary>
[HarmonyPatch(typeof(LocTable))]
internal static class LocDebugPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(LocTable.GetRawText))]
    static void AppendKey(string key, ref string __result)
    {
        if (!Spire1Config.LocDebug)
        {
            return;
        }

        if (!key.StartsWith("SPIRE1-", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        __result = $"{__result} ({key})";
    }
}
