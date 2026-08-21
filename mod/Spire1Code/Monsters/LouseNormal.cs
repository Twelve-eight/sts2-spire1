// Port of vanilla StS1 com.megacrit.cardcrawl.monsters.exordium.LouseNormal ("Red Louse").
// All numbers below are transcribed from the javap dump in .tmp/lice.txt; nothing invented.
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
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Random;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Vanilla StS1 <c>LouseNormal</c> (ID <c>FuzzyLouseNormal</c>) ported to the StS2 engine.
/// <para>
/// Dumped values (<c>.tmp/lice.txt</c>): HP 10-15, A7+ 11-16. Bite damage is rolled ONCE at
/// spawn from <c>monsterHpRng</c>: 5-7, or 6-8 at ascension 2+. Curl Up block 3-7 (4-8 at A7+,
/// 9-12 at A17+), applied pre-battle and spent on first hit. Grow: +3 Strength (+4 at A17+).
/// AI: 75/25 bite/grow split resolved by a stable per-turn sub-roll; each branch is a
/// deterministic history map over the recent move log (no move three times in a row),
/// with the A17+ variant tightening the low-roll Grow guard to the last move.
/// </para>
/// </summary>
public sealed class LouseNormal : Spire1Monster
{
    // Spawn-time bite roll bounds (constructor: monsterHpRng.random(5,7), A2+: random(6,8)).
    private int BiteMin => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    /// <summary>Inclusive upper bound of the vanilla bite roll.</summary>
    private int BiteMax => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    // Curl Up block range (usePreBattleAction): 3-7, A7+ 4-8, A17+ 9-12.
    private int CurlUpMin => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 4, 3));

    /// <summary>Inclusive upper bound of the vanilla Curl Up roll.</summary>
    private int CurlUpMax => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 7));

    // Grow (STRENGTHEN): StrengthPower 3, A17+ 4.
    private int GrowAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    /// <summary>
    /// Rolled once per combat at spawn, mirroring the vanilla constructor's single
    /// <c>monsterHpRng</c> draw; reused for every bite instead of re-rolling per attack.
    /// </summary>
    private int _biteDamage;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 16, 15);

    protected override string DonorId => "louse_progenitor";

    /// <summary>
    /// Runs before the engine's first <c>RollMove</c> (CombatManager.AddCreatureToCombat),
    /// so this is the faithful spot for the vanilla spawn-time rolls.
    /// </summary>
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // StS1 monsterHpRng ≈ StS2 run-level Niche stream (one-off per-monster rolls).
        Rng spawnRng = base.RunRng.Niche;
        _biteDamage = spawnRng.NextInt(BiteMin, BiteMax + 1);
        await PowerCmd.Apply<CurlUpPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            spawnRng.NextInt(CurlUpMin, CurlUpMax + 1), base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // Damage is read at execution time so the intent always shows the spawn-rolled value.
        MoveState bite = new MoveState("BITE_MOVE", BiteMove, new SingleAttackIntent(() => _biteDamage));
        MoveState grow = new MoveState("GROW_MOVE", GrowMove, new BuffIntent());

        // Bytecode getMove (lice.txt, LouseNormal), roll 0-99; both branches are
        // deterministic history maps (num only picks the branch):
        //   base (<17):  <25: lastTwo(GROW) ? BITE : GROW
        //                >=25: lastTwo(BITE) ? GROW : BITE
        //   A17+:        <25: lastMove(GROW) ? BITE : GROW   (rest identical)
        // Long-run ~80% BITE / 20% GROW. A17 gated on DeadlyEnemies.
        _growState = grow;
        ConditionalBranchState root = new("RED_LOUSE_ROOT");
        ConditionalBranchState lowRoll = new("LOW_ROLL");
        ConditionalBranchState highRoll = new("HIGH_ROLL");
        lowRoll.AddState(bite, () => GrowGuard());
        lowRoll.AddState(grow, () => true);
        highRoll.AddState(grow, () => LastTwoWere(bite));
        highRoll.AddState(bite, () => true);
        root.AddState(lowRoll, () => LastSubRoll(0.25f));
        root.AddState(highRoll, () => true);
        bite.FollowUpState = root;
        grow.FollowUpState = root;
        return new MonsterMoveStateMachine(
            new List<MonsterState> { root, lowRoll, highRoll, bite, grow }, root);
    }

    private MoveState? _growState;

    private bool LastTwoWere(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }

    // roll<25 history guard: lastTwo(GROW) at base, lastMove(GROW) at A17+ (DeadlyEnemies).
    private bool GrowGuard()
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        if (log.Count == 0 || !ReferenceEquals(log[^1], _growState))
        {
            return false;
        }
        return RunManager.Instance.HasAscension(AscensionLevel.DeadlyEnemies)
            || (log.Count >= 2 && ReferenceEquals(log[^2], _growState));
    }

    // One stable sub-roll per turn: vanilla draws aiRng.randomBoolean(0.25) inside getMove;
    // cached so both complementary weight lambdas see the same value.
    private bool? _subRoll;
    private int _subRollTurn = -1;
    private bool LastSubRoll(float threshold)
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_subRoll == null || _subRollTurn != turn)
        {
            _subRoll = base.Rng.NextFloat() < threshold;
            _subRollTurn = turn;
        }
        return _subRoll.Value;
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        // Vanilla: AnimateSlowAttackAction + DamageAction(BLUNT_LIGHT) → blunt hit vfx.
        await DamageCmd.Attack(_biteDamage).FromMonster(this).WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task GrowMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Curl", 0.25f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            GrowAmount, base.Creature, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Red Louse",
        [
            ("BITE_MOVE", "Chomp"),
            ("GROW_MOVE", "Grow")
        ]);
}
