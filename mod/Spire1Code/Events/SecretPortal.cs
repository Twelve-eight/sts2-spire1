using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Secret Portal. Entering the portal teleports the player straight to the
/// Act-4 boss room (StS1: nextRoom = MonsterRoomBoss + nextRoomTransitionStart). FLAGGED: there is no
/// act-jump / boss-room API — <c>MegaCrit.Sts2.Core.Commands.MapCmd</c> only exposes
/// <c>SetBossEncounter(IRunState, EncounterModel)</c>, and the only act-entry flow is the run-internal
/// <c>RunManager.EnterAct</c> (dev-console / act-transition machinery, not event-safe). The "[Leave]"
/// branch is implemented.
/// </summary>
public class SecretPortal : Spire1Event
{
    protected override string ShippedPortrait => "doors_of_light_and_dark";

    public override ActModel[] Acts => Act3;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO: "[Enter the Portal]" (FLAGGED, omitted), "[Leave]".
        return [Option(Leave)];
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
