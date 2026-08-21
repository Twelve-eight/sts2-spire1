using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 — Spore Cloud (FungiBeast death power, <c>com.megacrit.cardcrawl.powers.SporeCloudPower</c>).
/// Bytecode: onDeath() returns early if <c>AbstractRoom.isBattleEnding()</c>, otherwise applies
/// Vulnerable (amount) to the player. The engine ships no equivalent power, so this is our own
/// CustomPowerModel.
/// <para>
/// Death-trigger shape copied from the shipped <c>SteamEruptionPower</c>: <c>AfterDeath</c> hook
/// plus <c>ShouldPowerBeRemovedAfterOwnerDeath => false</c> so the power survives its owner's
/// death long enough to fire, and <c>ShouldStopCombatFromEnding => true</c> so the combat cannot
/// finish before the spores resolve — together these are the engine-side equivalent of StS1's
/// isBattleEnding guard (spores never apply after the fight is already over).
/// </para>
/// </summary>
public class SporeCloudPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Spore Cloud",
            "#When this enemy dies, apply {Amount} *Vulnerable* to you.",
            "When this enemy dies, apply Vulnerable.");

    // Death hook signature copied from the decompiled game powers (e.g. SteamEruptionPower.AfterDeath).
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner)
            return;
        Flash();
        // Owner.Player is null for enemy-side creatures; resolve the target through the
        // combat state's players (GremlinMerc / Looter pattern).
          foreach (Player player in CombatState.Players)
        {
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), player.Creature, Amount, Owner, null);
        }
    }

    // Keeps the combat open until the death-triggered Vulnerable has been applied.
    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }

    // Survive the owner's death so AfterDeath can run (SteamEruptionPower pattern).
    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return false;
    }
}
