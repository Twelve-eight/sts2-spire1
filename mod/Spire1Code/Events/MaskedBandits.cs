using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Masked Bandits.
/// Pay all of your gold and listen to the bandits gloat, or fight them.
///
/// FLAG: [Fight!] starts the StS1 encounter "Masked Bandits" (rewards: 25-35 gold, or 30 in
/// daily runs, plus the Red Mask relic — Circlet if already owned). Both relics are available now
/// (StS2 ships RedMask and Circlet), so the only blocker is the unported encounter itself; [Fight!]
/// stays a locked placeholder until StS1 monsters land.
/// </summary>
public class MaskedBandits : Spire1Event
{
    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "punch_off";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Pay),
            // FLAG: starts StS1 encounter "Masked Bandits"; encounters are not ported.
            LockedOption("FIGHT"),
        ];
    }

    private async Task Pay()
    {
        // StS1: stealGold() then loseGold(player.gold) — the player keeps nothing.
        if (Owner.Gold > 0)
        {
            await PlayerCmd.LoseGold(Owner.Gold, Owner, GoldLossType.Stolen);
        }
        SetEventState(PageDescription("PAID_1"), [Option(ContinuePage1, "PAID_1")]);
    }

    private Task ContinuePage1()
    {
        SetEventState(PageDescription("PAID_2"), [Option(ContinuePage2, "PAID_2")]);
        return Task.CompletedTask;
    }

    private Task ContinuePage2()
    {
        SetEventState(PageDescription("PAID_3"), [Option(Leave, "PAID_3")]);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("PAID_4"));
        return Task.CompletedTask;
    }
}
