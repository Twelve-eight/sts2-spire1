using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Connection;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 联机容错补丁（Spire1Config.IgnoreMpModDifferences 门控，默认开）。
/// <para>
/// 引擎事实（dllsrc HandshakeManager.cs:110-137）：握手按顺序判三道闸——
/// ① 游戏版本字符串不符 → VersionMismatch（保留，不绕过）；
/// ② 玩法 mod 清单差异 → ModMismatch；③ ModelIdSerializationCache.Hash 不符 → VersionMismatch。
/// 今晚实测（divergence zip #563/#249）：双方 BaseLib 来源不同（本地目录 vs 创意工坊）、
/// 各自多装非玩法 mod、Spire1 分装包名不同，全部是"清单级假阳性"，玩家状态零差异。
/// </para>
/// <para>
/// 本补丁把 ② 与"版本相同但哈希不符的 ③"改写为放行并记日志；
/// RitsuLib 的 StateDivergenceDiagnosticsPopup.ShowDeferred 前缀拦截弹窗
/// （其诊断 bundle zip 仍由独立管线写入 logs 目录）。
/// 双方都需安装本构建才完整生效——单侧放行会被对端拒绝。
/// </para>
/// </summary>
[HarmonyPatch(typeof(HandshakeManager), "TryReadHandshakeMessage")]
internal static class MpIgnoreModDiffPatch
{
    private static readonly FieldInfo? LocalVersionField =
        AccessTools.Field(typeof(HandshakeManager), "_localVersionInfo");

    [HarmonyPostfix]
    private static void AllowThrough(HandshakeResult __result, HandshakeManager __instance)
    {
        if (!Spire1Config.IgnoreMpModDifferences || __result.status == HandshakeStatus.Success)
        {
            return;
        }

        var local = (PeerVersionInfo?)LocalVersionField?.GetValue(__instance) ?? default;
        PeerVersionInfo remote = __result.remoteVersionInfo ?? default;

        if (__result.status == HandshakeStatus.ModMismatch)
        {
            MainFile.Logger.Warn(
                $"[Spire1] MP handshake: mod-list mismatch forced through "
                + $"(local={local.gameplayAffectingMods?.Count ?? -1} gameplay mods, "
                + $"remote={remote.gameplayAffectingMods?.Count ?? -1}). "
                + $"Serialization safety relies on identical gameplay-mod binaries.");
            __result.status = HandshakeStatus.Success;
            return;
        }

        // VersionMismatch 有两种成因：真版本不同（保持拦截）或哈希不符（放行）。
        if (__result.status == HandshakeStatus.VersionMismatch
            && string.Equals(local.version, remote.version, System.StringComparison.OrdinalIgnoreCase))
        {
            MainFile.Logger.Warn(
                $"[Spire1] MP handshake: ModelID hash mismatch forced through "
                + $"(same game version {local.version}; local hash={local.idDatabaseHash}, "
                + $"remote hash={remote.idDatabaseHash}).");
            __result.status = HandshakeStatus.Success;
        }
    }
}

/// <summary>
/// 抑制 RitsuLib 失同步弹窗。RitsuLib 是第三方工坊 mod，不能编译期引用，
/// 因此不走 [HarmonyPatch] 扫描（目标缺失会让注册循环每次启动报失败），
/// 由 MainFile 在扫描后显式调用 Apply；类型找不到时静默跳过。
/// </summary>
internal static class RitsuLibPopupSuppressionPatch
{
    private const string PopupType = "STS2RitsuLib.Networking.StateDivergence.StateDivergenceDiagnosticsPopup";
    private const string ShowMethod = "ShowDeferred";

    /// <summary>返回是否成功挂上（仅用于启动日志）。RitsuLib 未装时返回 false 且不报错。</summary>
    public static bool Apply(Harmony harmony)
    {
        try
        {
            var popup = AccessTools.TypeByName(PopupType);
            var show = AccessTools.Method(popup, ShowMethod);
            if (popup == null || show == null)
            {
                return false;
            }

            var prefix = typeof(RitsuLibPopupSuppressionPatch).GetMethod(
                nameof(SkipPopup), BindingFlags.Static | BindingFlags.NonPublic)!;
            harmony.Patch(show, prefix: new HarmonyMethod(prefix));
            return true;
        }
        catch
        {
            return false;
        }
    }

    // __0 = ShowDeferred 的 report 实参（第三方类型无法编译期引用，用 Harmony 索引注入 + object 接收）。
    private static bool SkipPopup(object __0)
    {
        MainFile.Logger.Info("[Spire1] RitsuLib state-divergence popup suppressed (IgnoreMpModDifferences). "
                             + "Diagnostics bundle still written to the logs directory.");
        return false; // 阻断弹窗；诊断 zip 由独立管线照常落盘
    }
}