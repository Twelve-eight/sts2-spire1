using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Mysterious Sphere. Opening the sphere starts a fight against "2 Orb Walkers"
/// (reward: gold 45-54 and a rare relic) — FLAGGED: the StS1 encounter is not ported, so the
/// "[Open Sphere]" option is omitted. The "[Leave]" branch is implemented.
/// </summary>
public class MysteriousSphere : Spire1Event
{
    protected override string ShippedPortrait => "crystal_sphere";

    public override ActModel[] Acts => Act3;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO: "[Open Sphere]" (FLAGGED, omitted), "[Leave]".
        return [Option(Leave)];
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
