using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Spire Heart. The final encounter with the heart: an attack cinematic, then
/// either death ("[Sleep]") or the door to the final act ("[Approach Door]").
/// FLAGGED: "[Approach Door]" needs the StS1 key/ending system (ruby/emerald/sapphire keys and the
/// DoorUnlockScreen); StS2 has no key system and no door API, so the door branch is omitted and the
/// event always resolves to the sleep (death) branch, exactly as StS1 does without the keys.
/// FLAGGED: the "You deal X damage!" number is StS1's GameOverScreen.calcScore(true) (a score quirk);
/// this port shows the run's actual damage-dealt stat (<see cref="ExtraPlayerFields.DamageDealt"/>),
/// the only in-run aggregate available. The "total dealt by all who have challenged it" sentences need
/// cross-run / global stats that StS2 does not expose, so they are omitted.
/// NOTE: this jar's SpireHeart has no blessing (max HP / upgrade / relic) choices — the bytecode only
/// contains the Continue / Attack / Continue / (Sleep | Approach Door) cinematic flow.
/// </summary>
public class SpireHeart : Spire1Event
{
    protected override string ShippedPortrait => "the_legends_were_true";

    public override ActModel[] Acts => Act3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DamageDealt", 0m)];

    public override void CalculateVars()
    {
        DynamicVars["DamageDealt"].BaseValue = Owner.ExtraFields.DamageDealt;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO: "[Continue]".
        return [Option(Continue)];
    }

    private async Task Continue()
    {
        // StS1: body = player.getSpireHeartText() — character-specific weapon line, then "[Attack] ???".
        string page = Owner.Character switch
        {
            Silent => "MIDDLE_DAGGERS",
            Defect => "MIDDLE_CORE",
            Watcher => "MIDDLE_STAFF",
            _ => "MIDDLE_BLADE", // Ironclad; also the fallback for characters without a specific line
        };
        SetEventState(PageDescription(page), [Option(Attack, "MIDDLE")]);
    }

    private async Task Attack()
    {
        // StS1: "You deal {damageDealt} damage! ..." then "[Continue]".
        SetEventState(PageDescription("MIDDLE_2"), [Option(ContinueAfterAttack, "MIDDLE_2")]);
    }

    private async Task ContinueAfterAttack()
    {
        // StS1 checks the three keys here: with all keys + final act available it shows "[Approach Door]"
        // (FLAGGED, omitted), otherwise "[Sleep]" and death.
        SetEventState(PageDescription("DEATH"), [Option(Sleep, "DEATH")]);
    }

    private async Task Sleep()
    {
        // StS1: death screen. Kill is the sanctioned event-death path (see TabletOfTruth).
        await CreatureCmd.Kill(Owner.Creature);
    }
}
