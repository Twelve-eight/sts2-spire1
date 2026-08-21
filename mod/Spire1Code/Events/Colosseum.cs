using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — The Colosseum.
/// You are thrown into a gladiator arena. The only branch leads to two consecutive fights:
/// 1. "Colosseum Slavers" (no rewards), then
/// 2. "Colosseum Nobs" (rewards: Rare relic + Uncommon relic + 100 gold).
///
/// FLAG: StS1 encounters are NOT ported, and this event has no non-combat branch, so it is
/// disabled via <see cref="IsAllowed"/> until the "Colosseum Slavers" / "Colosseum Nobs"
/// encounters exist. The [Fight] option is a locked placeholder.
/// </summary>
public class Colosseum : Spire1Event
{
    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "trial";

    // FLAG: combat-only event; keep it out of the pool until the StS1 encounters
    // "Colosseum Slavers" and "Colosseum Nobs" are ported, otherwise the player would
    // soft-lock on the locked [Fight] option.
    public override bool IsAllowed(IRunState runState) => false;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        // StS1: page shows "Groggy and with a throbbing head, ...  WE NOW BEGIN THE 4200TH COMBAT!!!! A gate on the opposite side opens..."
        // then the only option is [Fight], which starts encounter "Colosseum Slavers".
        SetEventState(PageDescription("FIGHT"), [LockedOption("FIGHT", "FIGHT")]);
        return Task.CompletedTask;
    }
}
