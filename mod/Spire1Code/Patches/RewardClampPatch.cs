using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Runs;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 纯一代池模式（PureSts1Pools）的保险丝：把 CreateForReward 的请求张数钳制到
/// "候选池经 options 过滤后实际可用的卡数"。官方各角色池永远远大于事件需求，
/// 此钳制对原版行为零影响；仅当自定义小池（如纯一代 Defect 的 4 张 Common）
/// 面对 ROOM_FULL_OF_CHEESE 这类"要 8 张不重复"的事件时生效——从"必然抛
/// InvalidOperationException"变为"优雅地给到池内全部可用卡"。
/// 仅在 PureSts1Pools 开启时激活，默认构建路径零开销。
/// </summary>
[HarmonyPatch(typeof(CardFactory))]
[HarmonyPatch("CreateForReward", new[] { typeof(Player), typeof(int), typeof(CardCreationOptions) })]
internal static class RewardClampPatch
{
    static bool Prefix(ref int cardCount, Player player, CardCreationOptions options)
    {
        if (!Spire1Config.PureSts1Pools || cardCount <= 0)
        {
            return true;
        }
        int available = options.GetPossibleCards(player).Count();
        if (available < cardCount)
        {
            cardCount = available;
        }
        return true;
    }
}
