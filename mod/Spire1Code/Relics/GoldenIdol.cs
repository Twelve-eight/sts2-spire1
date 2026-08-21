using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 — Golden Idol (Event). Enemies drop 25% more Gold.</summary>
public class GoldenIdol : Spire1Relic
{
    /// <summary>StS1 <c>GoldenIdol.MULTIPLIER</c>. The relic class itself declares nothing else.</summary>
    private const decimal _multiplier = 0.25m;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Golden Idol",
            "#Enemies drop 25% more *Gold*.",
            "Made of solid gold, you feel richer just holding it.");

    // StS1 puts none of this in the relic. RewardItem's gold path does:
    //     if (!(AbstractDungeon.getCurrRoom() instanceof TreasureRoom)
    //         && AbstractDungeon.player.hasRelic("Golden Idol"))
    //         this.bonusGold += MathUtils.round(this.goldAmt * 0.25f);
    // and the claim path pays out player.gainGold(goldAmt + bonusGold).
    //
    // TryModifyRewards (AbstractModel.cs:2140) is the 1:1 StS2 counterpart: it runs once per generated
    // RewardsSet, after every Reward.Populate() (RewardsSet.cs:132-136), and it receives both the reward
    // list and the room those rewards belong to — exactly the two things StS1's check needs.
    //
    // Deliberately NOT ModifyGoldGained (AbstractModel.cs:1635): that hook fires for every
    // PlayerCmd.GainGold (PlayerCmd.cs:144), which would also boost Hand of Greed, Heist, Maw Bank and
    // every event gold payout. Vanilla boosts none of those — only RewardItem gold.
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        // A null room means the rewards are not room completion (an event choice or a relic pickup).
        // Requiring a combat room reproduces StS1's "not a TreasureRoom" predicate exactly, because the
        // only rooms that ever produce a gold RewardItem in StS1 are combat rooms and Treasure rooms.
        // StS2 Treasure rooms cannot leak either way: their gold is paid straight through
        // PlayerCmd.GainGold (OneOffSynchronizer.cs:138) and is never a GoldReward.
        // "?" rooms that resolve into a fight do count, since the pushed room is a combat room in both
        // games (RoomType.cs:19-27).
        if (player != Owner || room == null || !room.RoomType.IsCombatRoom())
            return false;

        bool modified = false;

        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is not GoldReward gold)
                continue;

            int bonus = BonusGoldFor(gold.Amount);
            if (bonus <= 0)
                continue;

            // GoldReward.Amount has a private setter (GoldReward.cs:38), so the reward has to be
            // replaced rather than edited — the same move shipped Midas makes (Midas.cs:19). The
            // replacement is already populated (Amount >= 0), so RewardsSet.cs:137-143 leaves it alone.
            // ToSerializable() is the only public read of the "stolen back" flag, which selects the
            // COMBAT_REWARD_GOLD_STOLEN description (GoldReward.cs:44), so it is carried over instead
            // of being silently dropped.
            rewards[i] = new GoldReward(gold.Amount + bonus, player, gold.ToSerializable().WasGoldStolenBack);
            modified = true;
        }

        return modified;
    }

    /// <summary>
    /// Only invoked for models that returned true from a modify-rewards hook (Hook.cs:841-850), so the
    /// flash is tied to an actual gold boost.
    /// </summary>
    public override Task AfterModifyingRewards()
    {
        Flash();
        return Task.CompletedTask;
    }

    /// <summary>
    /// StS1: <c>bonusGold += MathUtils.round(goldAmt * 0.25f)</c>. libGDX's round is
    /// <c>(int)(value + 16384.5d) - 16384</c>, i.e. floor(value + 0.5) for non-negative input, so the
    /// bonus rounds half up and is truncated exactly once — 10 gold gives +3, 9 gold gives +2. This is
    /// not the same as paying out 1.25x the gold, hence the explicit floor rather than a bare multiply.
    /// </summary>
    private static int BonusGoldFor(int goldAmount) =>
        (int)decimal.Floor(goldAmount * _multiplier + 0.5m);
}
