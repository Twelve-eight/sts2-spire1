using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Diagnostics;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// On-demand pool census trigger (SP1-1, 2026-09-15).
///
/// Producer: engine NMainMenu node readiness (the main menu is reached only after
/// ModManager has finished loading every mod, so this is post-registration by
/// construction). Owner: Spire1 diagnostics. First consumer: PoolCensus, one-shot per
/// session (its menu latch) - repeated menu entries are no-ops.
///
/// Gating: the body does NOTHING unless Spire1Config.PoolCensusOnMenuEnter is true
/// (default OFF). PoolCensusOnMenuEnter=true is the only way this patch performs any
/// pool read, and a pool read freezes the pool - that is exactly why the trigger is
/// config-gated and deferred to menu entry instead of running in the mod initializer.
/// Toggling the config ON mid-session fires on the next main menu entry.
///
/// Failure boundary: exception-proof postfix - a diagnostic must never break menu
/// readiness. (MainFile's per-type Harmony scan also isolates this patch class's
/// installation from every other patch.)
/// </summary>
[HarmonyPatch(typeof(NMainMenu), "_Ready")]
internal static class PoolCensusMenuPatch
{
    private static void Postfix()
    {
        try
        {
            if (Spire1Config.PoolCensusOnMenuEnter)
            {
                PoolCensus.TryRunOnceFromMenu();
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] PoolCensus menu trigger failed: {e.Message}");
        }
    }
}
