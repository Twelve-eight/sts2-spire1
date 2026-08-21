using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Lab. Search the lab for 3 random potions (2 at Ascension 15+), offered as rewards.
/// </summary>
public class Lab : Spire1Event
{
    private const int _potionCount = 3;

    private const int _a15PotionCount = 2;

    protected override string ShippedPortrait => "potion_courier";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Search)
        ];
    }

    private async Task Search()
    {
        // StS1's PotionHelper.getRandomPotion() rolls each potion independently (duplicates allowed).
        List<PotionModel> potions = Owner.Character.PotionPool.GetUnlockedPotions(Owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(Owner.UnlockState))
            .ToList();
        int count = Owner.RunState.AscensionLevel >= 15 ? _a15PotionCount : _potionCount;
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
        SetEventFinished(PageDescription("INITIAL"));
    }
}
