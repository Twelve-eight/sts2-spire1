using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
/// The battle-ending guard maps to the engine's <c>ShouldStopCombatFromEnding</c> hook: while
/// this power is on a dying creature the combat cannot finish before AfterDeath runs, which is
/// exactly the window StS1's early-return was protecting (no spores after the fight is over).
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
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), Owner.Player.Creature, Amount, Owner, null);
    }

    // Keeps the combat open until the death-triggered Vulnerable has been applied,
    // mirroring StS1's isBattleEnding() guard in reverse.
    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }
}
