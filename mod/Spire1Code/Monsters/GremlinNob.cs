using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium elite "Gremlin Nob" (<c>com.megacrit.cardcrawl.monsters.exordium.GremlinNob</c>).
/// <para>
/// Bytecode values: HP 82-86 (A8+: 85-90), Bull Rush 14 (A3+: 16), Skull Bash 6 (A3+: 8) plus
/// Vulnerable <c>DEBUFF_AMT</c> = 2, Bellow applies Anger 2 (A18+: 3). <c>canVuln</c> is true for
/// the two-arg constructor, which is the one <c>MonsterHelper</c> uses.
/// </para>
/// <para>
/// Exact <c>getMove</c>, reproduced state-for-state:
/// <list type="number">
/// <item>First turn (<c>!usedBellow</c>) is always BELLOW.</item>
/// <item>A18+: if neither of the last two moves was SKULL_BASH → SKULL_BASH; otherwise fall into
/// the shared tail.</item>
/// <item>Below A18: <c>roll &lt; 33</c> → SKULL_BASH; otherwise fall into the shared tail.</item>
/// <item>Shared tail: if the last two moves were both BULL_RUSH → SKULL_BASH, else BULL_RUSH.</item>
/// </list>
/// The 33% roll is a <see cref="RandomBranchState"/> with weights 33/67 and no repeat limit - StS1
/// imposes none, the anti-repeat behaviour comes solely from the shared tail.
/// StS1's Anger power ("whenever the player plays a Skill, gain Strength") is exactly the shipped
/// <see cref="EnragePower"/>, so that power is reused rather than recreated.
/// </para>
/// <para>
/// Ascension mapping: HP → <see cref="AscensionLevel.ToughEnemies"/> (A8); damage plus the StS1 A18
/// tier (Bellow 3 and the deterministic Skull Bash branch) → <see cref="AscensionLevel.DeadlyEnemies"/>
/// (A9), the highest enemy-difficulty level StS2 exposes.
/// </para>
/// </summary>
public sealed class GremlinNob : Spire1Monster
{
    private const int DebuffAmount = 2;

    private const string SkullBashId = "SKULL_BASH";
    private const string BullRushId = "BULL_RUSH";

    /// <summary><c>MonsterHelper</c> builds Nob through <c>GremlinNob(float, float)</c>, which passes true.</summary>
    private const bool CanVuln = true;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 85, 82);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 90, 86);

    private int BashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    private int RushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);

    /// <summary>StS1 A18+ makes the Skull Bash choice deterministic; mapped onto DeadlyEnemies.</summary>
    private static bool IsHardMode => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);

    /// <summary>Borrows the shipped GremlinMerc scene - the largest, most brutish gremlin rig.</summary>
    protected override string DonorId => "gremlin_merc";

    /// <summary>
    /// The gremlin_merc rig ships <c>idle_loop</c>, <c>attack_single</c>, <c>attack_double</c>,
    /// <c>hurt</c> and <c>die</c> (shipped <c>GremlinMerc.GenerateAnimator</c>). It has no plain
    /// <c>attack</c> animation, which the engine default animator would ask for, so Attack is
    /// remapped onto <c>attack_single</c>; the rig has no buff/cast animation at all, so Bellow
    /// intentionally plays no animation (see <see cref="BellowMove"/>).
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack_single");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState bellow = new("BELLOW", BellowMove, new BuffIntent());
        MoveState bullRush = new(BullRushId, BullRushMove, new SingleAttackIntent(RushDamage));
        MoveState skullBash = new(SkullBashId, SkullBashMove, new SingleAttackIntent(BashDamage), new DebuffIntent());

        // Shared tail: two Bull Rushes in a row force a Skull Bash.
        ConditionalBranchState rushRepeatCheck = new("RUSH_REPEAT_CHECK");
        rushRepeatCheck.AddState(skullBash, () => LastTwoMovesWere(BullRushId));
        rushRepeatCheck.AddState(bullRush, () => true);

        // Below A18: 33% Skull Bash, else the shared tail.
        RandomBranchState roll = new("BASH_ROLL");
        roll.AddBranch(skullBash, MoveRepeatType.CanRepeatForever, 33f);
        roll.AddBranch(rushRepeatCheck, MoveRepeatType.CanRepeatForever, 67f);

        ConditionalBranchState decide = new("DECIDE");
        decide.AddState(skullBash, () => IsHardMode && !EitherOfLastTwoMovesWas(SkullBashId));
        decide.AddState(rushRepeatCheck, () => IsHardMode);
        decide.AddState(roll, () => true);

        bellow.FollowUpState = decide;
        bullRush.FollowUpState = decide;
        skullBash.FollowUpState = decide;

        // Branch states must be registered too, or FindNextMoveState throws "no valid state found".
        List<MonsterState> states = [bellow, bullRush, skullBash, rushRepeatCheck, roll, decide];
        return new MonsterMoveStateMachine(states, bellow);
    }

    /// <summary>StS1 <c>lastTwoMoves(byte)</c>: both of the two most recent moves were <paramref name="moveId"/>.</summary>
    private bool LastTwoMovesWere(string moveId)
    {
        List<MonsterState>? log = MoveStateMachine?.StateLog;
        if (log == null || log.Count < 2)
        {
            return false;
        }

        return log[^1].Id == moveId && log[^2].Id == moveId;
    }

    /// <summary>StS1 <c>lastMove(byte) || lastMoveBefore(byte)</c>.</summary>
    private bool EitherOfLastTwoMovesWas(string moveId)
    {
        List<MonsterState>? log = MoveStateMachine?.StateLog;
        if (log == null || log.Count == 0)
        {
            return false;
        }

        if (log[^1].Id == moveId)
        {
            return true;
        }

        return log.Count >= 2 && log[^2].Id == moveId;
    }

    private async Task BellowMove(IReadOnlyList<Creature> targets)
    {
        // No animation: the gremlin_merc rig has no buff/cast animation to play.
        await Cmd.CustomScaledWait(0.25f, 0.5f);
        await PowerCmd.Apply<EnragePower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3m, 2m),
            Creature,
            null);
    }

    private async Task BullRushMove(IReadOnlyList<Creature> targets)
    {
        // StS1 uses AttackEffect.BLUNT_HEAVY for both Nob attacks.
        await DamageCmd.Attack(RushDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(null);
    }

    private async Task SkullBashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(null);
        if (CanVuln)
        {
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, Creature, null);
        }
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc(
            "Gremlin Nob",
            [
                ("BELLOW", "Bellow"),
                (BullRushId, "Bull Rush"),
                (SkullBashId, "Skull Bash"),
            ]);
}
