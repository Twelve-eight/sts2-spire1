#if DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 夜间工具（仅 AutoSlayer 激活时生效）：消费 inject-queue.txt 中 "relic:<ID>" 行，
/// 在地图屏 Initialize（战斗外安全点）通过 RelicCmd.Obtain 发放给玩家。
///
/// 教训：此前在 CombatManager.SetUpCombat postfix 里 fire-and-forget Obtain——
/// 战斗建立期模型变动断言拒绝，任务静默失败（discarded task），遗物从未入包且零日志。
/// 此处改在房间之间发放，并对任务故障记录日志。行无论成败都从队列移除，避免死循环。
/// </summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Initialize))]
internal static class DebugRelicInjectPatch
{
    private const string QueuePath = "G:\\omp works\\sts2-spire1\\.tmp\\night\\inject-queue.txt";

    static void Postfix(NMapScreen __instance, RunState runState)
    {
        try
        {
            if (!AutoSlayer.IsActive)
            {
                return;
            }
            string? full = Path.GetFullPath(QueuePath);
            if (full == null || !File.Exists(full))
            {
                return;
            }
            List<string> queue = File.ReadAllLines(full)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("#"))
                .ToList();
            var relicLines = queue.Where(l => l.StartsWith("relic:", StringComparison.OrdinalIgnoreCase)).ToList();
            if (relicLines.Count == 0)
            {
                return;
            }
            var player = runState.Players.FirstOrDefault();
            if (player == null)
            {
                return;
            }
            foreach (string line in relicLines)
            {
                string rid = line.Substring(6).Trim();
                var relic = ModelDb.AllRelics.FirstOrDefault(r =>
                    r.Id.Entry.Equals(rid, StringComparison.OrdinalIgnoreCase) ||
                    r.Id.Entry.Replace("_", "").Equals(rid.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
                if (relic == null)
                {
                    MainFile.Logger.Error($"[Spire1] Relic inject: id '{rid}' not found in ModelDb.AllRelics");
                    continue;
                }
                var mutable = relic.ToMutable();
                MainFile.Logger.Info($"[Spire1] Relic inject (map screen): {mutable.Id.Entry}");
                if (mutable is MegaCrit.Sts2.Core.Models.Relics.DustyTome tome)
                {
                    tome.SetupForPlayer(player); // 原版由授予方调用；直接获得时 AncientCard 未定
                }
                RelicCmd.Obtain(mutable, player).ContinueWith(
                    tsk => MainFile.Logger.Error($"[Spire1] Obtain {relic.Id.Entry} FAILED: {tsk.Exception?.GetBaseException().Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            File.WriteAllLines(full, queue.Except(relicLines));
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Relic inject failed: {e.Message}");
        }
    }
}

#endif
