using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Spire1.Spire1Code.Acts;
using Spire1.Spire1Code.Config;
using System;
using System.Linq;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// The StS1 dungeon selector, in one patch.
/// <para>
/// StS2 has no act-sequencing API — no next-act or act-order hook exists. It does not need one:
/// <c>NGame.StartNewSingleplayerRun</c> takes the run's act list as a parameter
/// (<c>"The canonical acts that should be in the run"</c>) and hands it straight to
/// <c>RunState.CreateForNewRun</c>, which stores it as <c>RunState.Acts</c> and walks it by list
/// position via <c>CurrentActIndex</c>. So choosing a dungeon is choosing that one list, and the
/// whole selector is a prefix that rewrites the argument.
/// </para>
/// <para>
/// The StS1 acts themselves pass <c>-1</c> to <c>CustomActModel</c> (see <see cref="Spire1Act"/>),
/// which lands them on <c>Index = -2</c>. The engine reads <c>act.Index</c> in exactly one place,
/// <c>ModelDb.cs:334</c>, and guards it with <c>if (act.Index &gt;= 0)</c> — so a negative index
/// keeps an act out of <c>ActsByIndex</c> and therefore out of every natural act slot, without
/// any risk of an out-of-range access. That is precisely the "registered but never spawns
/// naturally" state a selector wants, and it is why installing this mod cannot change a vanilla
/// StS2 run.
/// </para>
/// <para>
/// Multiplayer is deliberately NOT patched yet. Co-op reaches a run through a second path
/// (<c>NCharacterSelectScreen</c> also builds a <c>RunState</c> directly at <c>:745</c> besides
/// calling <c>NGame.StartNewMultiplayerRun</c>), and substituting per-client from a local config
/// toggle would desync a lobby whose members disagree. Host-authoritative dungeon choice is M3
/// work; until then co-op runs the vanilla act sequence.
/// </para>
/// </summary>
[HarmonyPatch(typeof(NGame))]
internal static class DungeonSelectionPatch
{
    /// <summary>
    /// The StS1 dungeon in floor order. Only Exordium is ported so far; The City, The Beyond and
    /// The Ending append here as they land, and until all four exist the run is left alone rather
    /// than started on a truncated dungeon.
    /// </summary>
    private static readonly Type[] Sts1ActOrder = [typeof(Exordium)];

    /// <summary>
    /// The full StS1 dungeon is four acts. Until all four are ported, the selector refuses to
    /// substitute: a one-act "dungeon" would end the run after Exordium's boss, which is a test
    /// rig, not the feature. Remove this gate when <see cref="Sts1ActOrder"/> holds all four.
    /// </summary>
    private const int CompleteDungeonActCount = 4;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NGame.StartNewSingleplayerRun))]
    private static void SubstituteActs(ref IReadOnlyList<ActModel> acts)
    {
        if (!Spire1Config.Sts1DungeonSelected)
        {
            return;
        }

        if (Sts1ActOrder.Length < CompleteDungeonActCount)
        {
            MainFile.Logger.Warn($"StS1 dungeon selected but only {Sts1ActOrder.Length}/{CompleteDungeonActCount} acts are ported; keeping the StS2 acts.");
            return;
        }


        List<ActModel> dungeon = new(Sts1ActOrder.Length);
        foreach (Type actType in Sts1ActOrder)
        {
            ActModel? act = ModelDb.Acts.FirstOrDefault(candidate => candidate.GetType() == actType);
            if (act is null)
            {
                // An act failed to register. Fail safe: run vanilla StS2 rather than a broken dungeon.
                MainFile.Logger.Warn($"StS1 dungeon selected but {actType.Name} is not registered; keeping the StS2 acts.");
                return;
            }

            dungeon.Add(act);
        }

        acts = dungeon;
    }
}
