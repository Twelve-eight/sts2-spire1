using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// R1（2026-09-06 审阅，docs/CODE-REVIEW-20260904.md §R1）：生态栈下三幕 boss 零奖励。
///
/// 根因链（全程实证）：
/// - 引擎 <c>RewardsSet.WithRewardsFromRoom</c> 对 boss 房且
///   <c>CurrentActIndex >= Acts.Count-1</c> 早退返回空奖励（发奖口本意："最后一幕
///   boss 不发奖励"，StS1 同语义：三幕 boss 无奖励，四幕 Heart 战终局）。
/// - Act4Heart 的 <c>FixAct3Boss_IL_</c> 把三处 <c>Acts.Count-1</c> IL 改写为
///   <c>Count-2</c>（RunManager.GenerateRooms / 本方法 / AmethystAubergine），
///   意图让三幕在"房间生成/遗物调整"侧按非终幕处理。
/// - 其 <c>ModelDb.get_Acts</c> 钩子无条件把 TheEnding 追加进全局幕表 →
///   生态栈开局 <c>RunState.Acts.Count=4</c>（旁证：Act4Hooks.cs:60 钥匙门
///   <c>state.Acts[CurrentActIndex+1] is TheEnding</c>）。
/// - 结果：三幕 boss（index 2）落 <c>2 >= 4-2</c> 早退 → 零奖励，与钥匙无关；
///   Act4Heart 全栈无补发钩子。原版表达式在四幕栈下本来会发（<c>2>=3</c> 为假）。
///
/// 修复（遵守 DEVELOP §0"生态补丁进本仓"指令）：postfix 补发。触发条件取窄集——
/// boss 房、引擎已按改写后条件早退（Rewards 为空）、当前幕恰为倒数第二幕
/// （<c>CurrentActIndex == Acts.Count-2</c>）、且下一幕存在并是第四幕 mod 的
/// TheEnding（Act4Heart 的或本仓 fallback 的）。按引擎原始构成
/// （RewardsSet.cs GenerateRewardsFor 的 Boss 分支）补发金币 + 药水 roll + 3 张卡。
///
/// 语义边界：
/// - 纯原版 / 纯 AFTP / 无 Act4Heart：三幕 boss 时 <c>Acts.Count=3</c>，
///   原版表达式 <c>2>=2</c> 早退、Rewards 为空，但 <c>CurrentActIndex==Count-2</c>
///   为 <c>2==1</c> 假，且无 TheEnding 后继——不触发。原版行为不变。
/// - 四幕 boss（TheEnding，index 3）：<c>3==2</c> 假——不触发，终局保持零奖励。
/// - Act4Heart 已修好其改写的未来版本：三幕 boss 不再早退、Rewards 非空——不触发。
/// - 教程奖励路径（TryGenerateTutorialRewards）：真 return 时 Rewards 非空——不触发；
///   假 return 落入正常分支早退——补发与正常路径等价。
/// - 多人：WithRewardsFromRoom 逐玩家实例调用，补发构成与引擎逐玩家分支一致，
///   MP 奖励语义不变（GoldReward/PotionReward/CardReward 均为引擎 public 构造器）。
/// </summary>
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
internal static class Act3BossRewardPatch
{
    private static void Postfix(RewardsSet __instance, AbstractRoom room)
    {
        try
        {
            if (room.RoomType != RoomType.Boss
                || __instance.Rewards.Count != 0
                || __instance.Room is not CombatRoom combatRoom)
            {
                return;
            }
            IRunState run = __instance.Player.RunState;
            // 恰为倒数第二幕，且下一幕存在并是第四幕（TheEnding 全限定名两种：
            // Act4Heart 的 Act4Heart.TheEnding / 本仓 fallback 的 Spire1…TheEnding）。
            if (run.CurrentActIndex != run.Acts.Count - 2
                || run.CurrentActIndex + 1 >= run.Acts.Count
                || !IsFourthAct(run.Acts[run.CurrentActIndex + 1]))
            {
                return;
            }

            Player player = __instance.Player;
            __instance.Rewards.Add(new GoldReward(
                combatRoom.Encounter.MinGoldReward, combatRoom.Encounter.MaxGoldReward, player));
            RollForPotion(__instance, player, room.RoomType);
            __instance.Rewards.Add(new CardReward(
                CardCreationOptions.ForRoom(player, RoomType.Boss)
                    .WithFlags(CardCreationFlags.IsFromCombat), 3, player));
            MainFile.Logger.Info(
                $"[Spire1] Act3BossRewardPatch: restored boss rewards for act index {run.CurrentActIndex} (Acts.Count={run.Acts.Count}).");
        }
        catch (Exception e)
        {
            // 补丁自身故障不得炸掉发奖路径——放行原结果（该 boss 无奖励，可接受降级）。
            MainFile.Logger.Error($"[Spire1] Act3BossRewardPatch failed: {e}");
        }
    }

    private static bool IsFourthAct(ActModel act)
        => act.GetType().Name == "TheEnding";

    /// <summary>引擎 RewardsSet.RollForPotionAndAddTo 的等价（私有成员不走 AccessTools，
    /// 语义相同：PotionRewardOdds roll，命中加 PotionReward）。</summary>
    private static void RollForPotion(RewardsSet set, Player player, RoomType roomType)
    {
        if (player.PlayerOdds.PotionReward.Roll(player, roomType))
        {
            set.Rewards.Add(new PotionReward(player));
        }
    }
}
