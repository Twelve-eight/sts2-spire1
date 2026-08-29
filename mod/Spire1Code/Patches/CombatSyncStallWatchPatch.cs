using System.Threading.Tasks;
using HarmonyLib;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// MP 房间过渡死等的观察哨（kb v3 卷四 V4-R6 的防御补丁）。
/// <para>
/// 引擎事实（engine-dllsrc CombatStateSynchronizer.cs L152-163）：WaitForSync 是
/// <c>await _syncCompletionSource.Task</c>，无超时无取消；唯一解除条件 = 收齐全部
/// peer 的 SyncPlayerDataMessage，或 peer 断线（OnPeerDisconnected L103-111）。
/// 只要对端"不发 sync 也不断线"（典型：对端先死在另一个等待点/异常把任务链炸断），
/// 本端就永久黑屏等待——2026-08-28 家族C黑屏的机制（只能强退）。
/// </para>
/// <para>
/// 本补丁不改任何行为：Postfix 把原方法返回的 Task 包一层"60 秒后仍未完成则打
/// Warn"的观察任务。超时不取消、不注入结果——原 Task 的命运完全不变。
/// </para>
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Multiplayer.CombatStateSynchronizer), "WaitForSync")]
internal static class CombatSyncStallWatchPatch
{
    [HarmonyPostfix]
    private static void Watch(ref Task __result)
    {
        try
        {
            var original = __result;
            __result = WatchAsync(original);
        }
        catch
        {
            // 观察哨绝不引入新故障——异常时 __result 保持原样
        }
    }

    private static async Task WatchAsync(Task original)
    {
        var completed = await Task.WhenAny(original, Task.Delay(60_000));
        if (completed != original)
        {
            MainFile.Logger.Warn(
                "[Spire1] combat-sync stall: WaitForSync unresolved after 60s — " +
                "a peer likely froze before its own sync point (kb v3 V4-R6). " +
                "If the screen is black, check the OTHER peer's log for an exception or another stall; " +
                "the only engine-native release is that peer disconnecting.");
        }
        await original; // 保持原语义：调用方 await 到的仍是最初的任务结果/异常
    }
}
