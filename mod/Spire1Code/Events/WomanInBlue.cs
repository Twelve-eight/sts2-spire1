using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — The Woman in Blue. Buy 1/2/3 random potions for 20/30/40 gold, offered as rewards.
/// Leaving costs 5% of Max HP (rounded up) at Ascension 15+; below that, leaving is free.
/// </summary>
public class WomanInBlue : Spire1Event
{
    private const int _cost1 = 20;

    private const int _cost2 = 30;

    private const int _cost3 = 40;

    private const decimal _punchDamagePercent = 0.05m;

    protected override string ShippedPortrait => "the_future_of_potions";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HpLossVar("PunchHpLoss", 0m)];

    public override void CalculateVars()
    {
        // Damage shown on the A15+ leave option: ceil(Max HP * 0.05).
        DynamicVars["PunchHpLoss"].BaseValue = (int)Math.Ceiling(Owner.Creature.MaxHp * _punchDamagePercent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool ascensionPunch = Owner.RunState.AscensionLevel >= 15;
        return
        [
            Option(Buy1Potion),
            Option(Buy2Potions),
            Option(Buy3Potions),
            ascensionPunch ? Option(LeaveWithPunch) : Option(Leave)
        ];
    }

    private async Task BuyPotions(int cost, int count)
    {
        await PlayerCmd.LoseGold(cost, Owner, GoldLossType.Spent);
        List<PotionModel> potions = Owner.Character.PotionPool.GetUnlockedPotions(Owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(Owner.UnlockState))
            .ToList();
        List<Reward> rewards = new(count);
        for (int i = 0; i < count; i++)
        {
            PotionModel? potion = Rng.NextItem(potions);
            if (potion != null)
            {
                rewards.Add(new PotionReward(potion.ToMutable(), Owner));
            }
        }
        await RewardsCmd.OfferCustom(Owner, rewards);
        SetEventFinished(PageDescription("DONE"));
    }

    private Task Buy1Potion()
    {
        return BuyPotions(_cost1, 1);
    }

    private Task Buy2Potions()
    {
        return BuyPotions(_cost2, 2);
    }

    private Task Buy3Potions()
    {
        return BuyPotions(_cost3, 3);
    }

    private async Task LeaveWithPunch()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["PunchHpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        SetEventFinished(PageDescription("PUNCHED"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("PUNCHED"));
        return Task.CompletedTask;
    }
}
