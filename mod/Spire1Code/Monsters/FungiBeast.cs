using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — FungiBeast (<c>com.megacrit.cardcrawl.monsters.exordium.FungiBeast</c>).
/// <para>
/// Bytecode: HP 22-28, A2 24-28; BITE_DMG 6 (no ascension variant), GROW_STR 3 (A2 4).
/// getMove roll r: r&lt;60: lastTwo==Bite ? Grow : Bite; else last==Grow ? Bite : Grow.
/// usePreBattleAction applies SporeCloudPower(2): on death, if the battle is not ending,
/// apply Vulnerable 2 to the player.
/// </para>
/// <para>
/// Spore Cloud is ported as <see cref="SporeCloudPower"/> (our own CustomPowerModel — the engine
/// ships no equivalent) hooked through the engine's <c>AfterDeath</c> power hook, the same
/// mechanism shipped powers like SteamEruptionPower use for death triggers. The "battle not
/// ending" guard is expressed with <c>ShouldStopCombatFromEnding => true</c>, which keeps the
/// combat alive until the spores resolve — the engine-side equivalent of StS1's
/// <c>isBattleEnding</c> check.
/// </para>
/// </summary>
public sealed class FungiBeast : Spire1Monster
{
    // setHp(22, 28); ascension >= 7 -> setHp(24, 28)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 24, 22);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 28, 28);

    // BITE_DMG = 6 (no ascension variant)
    private int BiteDamage => 6;

    // GROW_STR = 3; ascension >= 2 -> 4
    private int GrowStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    // VULN_AMT = 2 (SporeCloudPower amount)
    private const int SporeVulnerableAmount = 2;

    protected override string DonorId => "flyconid";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new SporeCloudPower(this, 2))
        await PowerCmd.Apply<SporeCloudPower>(new ThrowingPlayerChoiceContext(), base.Creature, SporeVulnerableAmount, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // getMove: r<60 -> lastTwo(Bite) ? Grow : Bite ; else last(Grow) ? Bite : Grow.
        // Expressed as weighted branches (60/40) with Grow capped at 1 repeat and Bite at 2,
        // which reproduces the vanilla alternation without a hand-rolled history check.
        MoveState bite = new("BITE_MOVE", BiteMove, new SingleAttackIntent(BiteDamage));
        MoveState grow = new("GROW_MOVE", GrowMove, new BuffIntent());
        RandomBranchState roll = new("ROLL");
        bite.FollowUpState = roll;
        grow.FollowUpState = roll;
        roll.AddBranch(bite, 0, 2, 60f);
        roll.AddBranch(grow, 0, 1, 40f);
        return new MonsterMoveStateMachine([bite, grow, roll], bite);
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task GrowMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, GrowStrength, base.Creature, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Fungi Beast",
        [
            ("BITE_MOVE", "Bite"),
            ("GROW_MOVE", "Grow"),
        ]);
}
