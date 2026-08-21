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

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — JawWorm (<c>com.megacrit.cardcrawl.monsters.exordium.JawWorm</c>).
/// <para>
/// Bytecode: HP 40-44, A2 42-46; CHOMP_DMG 11 (A2 12), THRASH_DMG 7 + THRASH_BLOCK 5,
/// BELLOW_STR 3 (A2 4) + BELLOW_BLOCK 6. First move always Chomp.
/// getMove roll r: r&lt;25: last==Chomp ? 45/55 Bellow/Thrash : Chomp; r&lt;55:
/// lastTwo==Thrash ? 35/65 Chomp/Bellow : Thrash; else last==Bellow ? 40/60 Chomp/Thrash : Bellow.
/// </para>
/// <para>
/// The StS1 "never thrice in a row" behaviour is expressed with the engine's
/// <see cref="RandomBranchState"/> repeat limits: Bellow maxRepeats 2, Thrash maxRepeats 1,
/// Chomp unbounded — the same shape shipped <c>Flyconid</c> uses.
/// </para>
/// </summary>
public sealed class JawWorm : Spire1Monster
{
    // setHp(40, 44); ascension >= 7 -> setHp(42, 46)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 40);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 44);

    // CHOMP_DMG = 11; ascension >= 2 -> 12
    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

    // THRASH_DMG = 7 / THRASH_BLOCK = 5 (no ascension variants)
    private int ThrashDamage => 7;

    private int ThrashBlock => 5;

    // BELLOW_STR = 3; ascension >= 2 -> 4
    private int BellowStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    // BELLOW_BLOCK = 6 (no ascension variant)
    private int BellowBlock => 6;

    protected override string DonorId => "chomper";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState chomp = new("CHOMP_MOVE", ChompMove, new SingleAttackIntent(ChompDamage));
        MoveState bellow = new("BELLOW_MOVE", BellowMove, new DefendIntent(), new BuffIntent());
        MoveState thrash = new("THRASH_MOVE", ThrashMove, new SingleAttackIntent(ThrashDamage), new DefendIntent());
        RandomBranchState roll = new("ROLL");
        chomp.FollowUpState = roll;
        bellow.FollowUpState = roll;
        thrash.FollowUpState = roll;
        // Weights 45/25/30 from the vanilla thresholds; repeat caps encode the
        // lastMove/lastTwoMoves guards (Bellow never 3x, Thrash never back-to-back).
        roll.AddBranch(chomp, 0, 45f);
        roll.AddBranch(bellow, 0, 2, 25f);
        roll.AddBranch(thrash, 0, 1, 30f);
        return new MonsterMoveStateMachine([chomp, bellow, thrash, roll], chomp);
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task BellowMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, BellowStrength, base.Creature, null);
        await CreatureCmd.GainBlock(base.Creature, BellowBlock, ValueProp.Move, null);
    }

    private async Task ThrashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ThrashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, ThrashBlock, ValueProp.Move, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Jaw Worm",
        [
            ("CHOMP_MOVE", "Chomp"),
            ("BELLOW_MOVE", "Bellow"),
            ("THRASH_MOVE", "Thrash"),
        ]);
}
