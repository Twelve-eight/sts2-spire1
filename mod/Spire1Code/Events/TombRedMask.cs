using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Tomb of Lord Red Mask.
/// Two mutually exclusive trades plus a Leave, reproduced from the jar bytecode
/// (com.megacrit.cardcrawl.events.beyond.TombRedMask):
///  * "[Don the Red Mask] Gain 222 Gold." — offered only while the player already holds the Red Mask;
///    otherwise StS1 shows "[Locked] Requires: Red Mask." in that slot. GOLD_AMT = 222 (three separate
///    sipush 222 sites: the option title, the gainGold call and the locked-slot text).
///  * "[Offer: {gold} Gold] Lose all Gold. Obtain a Relic." — loseGold(all) then spawnRelicAndObtain of a
///    RedMask. The option title splices the player's current gold, which is why StS1's OPTIONS[2]/[3] are
///    two fragments around it.
///
/// The Red Mask relic is NOT reimplemented here: StS2 ships an identical one
/// (MegaCrit.Sts2.Core.Models.Relics/RedMask.cs — at the start of each combat apply 1 Weak to ALL
/// enemies), so per the lean-code rule the shipped relic is granted.
/// </summary>
public class TombRedMask : Spire1Event
{
    private const string _goldKey = "Gold";

    /// <summary>StS1 <c>GOLD_AMT = 222</c>.</summary>
    private const int _goldReward = 222;

    protected override string ShippedPortrait => "mirror_mask3";

    public override ActModel[] Acts => Act3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(0)];

    public override void CalculateVars()
    {
        // The offer option's title shows the gold it will cost, i.e. everything the player has.
        DynamicVars.Gold.BaseValue = Owner.Gold;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> options =
        [
            Owner.GetRelic<RedMask>() != null ? Option(DonTheMask) : LockedOption("DON_THE_MASK_LOCKED"),
        ];

        // StS1 always shows the offer slot; with 0 gold it is a trade of nothing for the relic, which is
        // what the bytecode does — there is no gold-amount guard on the option.
        options.Add(Option(OfferGold));
        options.Add(Option(Leave));
        return options;
    }

    private async Task DonTheMask()
    {
        await PlayerCmd.GainGold(_goldReward, Owner);
        SetEventFinished(PageDescription("DON_MASK"));
    }

    private async Task OfferGold()
    {
        // StS1 loses the gold FIRST and then spawns the relic, so a mid-flow failure cannot leave the
        // player holding both. GoldLossType.Spent matches the repo's other paid-gold events (Beggar.cs:44)
        // and the shipped SilkenTress.cs:44 "lose everything" precedent.
        if (Owner.Gold > 0)
        {
            await PlayerCmd.LoseGold(Owner.Gold, Owner, GoldLossType.Spent);
        }

        await RelicCmd.Obtain<RedMask>(Owner);
        SetEventFinished(PageDescription("OFFER"));
    }

    private Task Leave()
    {
        // StS1: leave with the intro body unchanged, then open the map.
        SetEventFinished(InitialDescription);
        return Task.CompletedTask;
    }
}
