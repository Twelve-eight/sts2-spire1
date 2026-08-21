using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Face Of Cleric (event relic, from FaceTrader). At the end of every combat won, +1 Max HP — which
/// also heals 1, because StS1's increaseMaxHp never reads its boolean and heals unconditionally.
///
/// StS1 (face-relics-and-madness.json "FaceOfCleric"): onVictory() -> flash + player.increaseMaxHp(1, true).
/// The boolean parameter is never read (no iload_2 in the method body), so the true effect is +1 Max HP AND
/// +1 current HP per combat won. The hook is driven by AbstractPlayer.onVictory(), which is guarded by
/// !isDying, so it fires for normal, elite and boss wins but never on death.
///
/// StS2 port: AfterCombatVictory (AbstractModel.cs:556) is the exact analogue of onVictory() — deliberately
/// NOT AfterCombatEnd (AbstractModel.cs:520), which also fires when the combat was lost. CreatureCmd.GainMaxHp
/// (CreatureCmd.cs:841) calls SetMaxHp and then `await Heal(creature, num)` at CreatureCmd.cs:853, so the one
/// call reproduces StS1's increaseMaxHp exactly — no separate heal is added.
/// </summary>
public class FaceOfCleric : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Face Of Cleric",
            "#At the end of combat, raise your Max HP by 1.",
            "Everyone loves Cleric.");

    // StS1's onVictory() only runs when the player is not dying; in multiplayer a combat can be won while
    // another player's creature is already down, so the dead-owner guard is the faithful translation of the
    // StS1 guard. (CreatureCmd.Heal would otherwise play a revive animation on a dead creature —
    // CreatureCmd.cs:744,772-775.)
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner.Creature.IsDead)
            return;

        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, 1m);
    }
}
