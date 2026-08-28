#if DEBUG

using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 夜间内容覆盖工具（仅 AutoSlayer 激活时生效）：从 .tmp/night/inject-queue.txt 逐行读取
/// 卡牌 id，在每次战斗SetUpCombat时把最多 6 张注入抽牌堆，让 autoslayer 自然打出并在
/// 日志留下 "Playing X" 覆盖记录。已消费的行会从队列文件移除；文件不存在则零介入。
/// </summary>
[HarmonyPatch(typeof(CombatManager))]
[HarmonyPatch(nameof(CombatManager.SetUpCombat))]
internal static class DebugCardInjectPatch
{
    // 夜间工具：绝对路径。游戏进程 CWD 是游戏目录，相对路径会静默找不到文件。
    private const string QueuePath = "G:\\omp works\\sts2-spire1\\.tmp\\night\\inject-queue.txt";

    static void Postfix(CombatState state)
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
            if (queue.Count == 0)
            {
                return;
            }
            var player = state.Players.FirstOrDefault();
            if (player == null)
            {
                return;
            }
            var batch = new List<CardModel>();
            var consumed = new List<string>();
            foreach (string id in queue)
            {
                if (batch.Count >= 6)
                {
                    break;
                }
                CardModel? model = ModelDb.AllCards.FirstOrDefault(c =>
                    c.Id.Entry.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                    c.Id.Entry.Equals("SPIRE1-" + id, StringComparison.OrdinalIgnoreCase) ||
                    c.Id.Entry.Replace("_", "").Equals(id.Replace("_", ""), StringComparison.OrdinalIgnoreCase));
                if (model == null)
                {
                    continue;
                }
                batch.Add(model);
                consumed.Add(id);
            }
            if (batch.Count == 0)
            {
                return;
            }
            File.WriteAllLines(full, queue.Except(consumed));
            MainFile.Logger.Info($"[Spire1] Injected {batch.Count} coverage cards: {string.Join(",", consumed)}");
            _ = CardPileCmd.AddGeneratedCardsToCombat(batch, PileType.Draw, player);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"Inject failed: {e.Message}");
        }
    }
}

#endif
