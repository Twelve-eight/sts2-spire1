using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.Entities.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-3 "Shape" — Repulsor (<c>com.megacrit.cardcrawl.monsters.beyond.Repulsor</c>).
/// <para>
/// Bytecode: HP 29-35, A7 31-38; attackDmg 11 (A2 13); dazeAmt 2 (no ascension tier).
/// getMove: <c>roll &lt; 20 &amp;&amp; !lastMove(ATTACK)</c> -&gt; ATTACK, else DAZE — i.e. a 20%
/// attack roll that is suppressed right after an attack (so attacks can never repeat), and
/// Daze takes every remaining outcome.
/// takeTurn ATTACK: AnimateSlowAttack + DamageAction(SLASH_HORIZONTAL). takeTurn DAZE:
/// <c>MakeTempCardInDrawPileAction(new Dazed(), 2, sameUUID: true, randomPosition: true)</c> —
/// two Dazed shuffled into the player's draw pile.
/// </para>
/// <para>
/// Ascension mapping follows the shipped StS2 monster convention (HP → ToughEnemies,
/// damage → DeadlyEnemies). Dazed is our ported status card (<see cref="Dazed"/>), added to the
/// draw pile via <c>CardPileCmd.AddToCombatAndPreview</c> — the same call shipped monsters use,
/// and the same one our Sentry port uses for its discard-pile Dazed.
/// </para>
/// </summary>
public sealed class Repulsor : Spire1Monster
{
    // setHp(29, 35); ascension >= 7 -> setHp(31, 38)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 31, 29);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 38, 35);

    // attackDmg = 11; ascension >= 2 -> 13
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);

    // dazeAmt = 2 (no ascension variant)
    private const int DazedAmount = 2;

    protected override string DonorId => "globe_head";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState daze = new("DAZE_MOVE", DazeMove, new CardDebuffIntent());
        MoveState attack = new("ATTACK_MOVE", AttackMove, new SingleAttackIntent(AttackDamage));

        // Bytecode getMove (repulsor.txt): roll < 20 && !lastMove(ATTACK) -> ATTACK, else DAZE.
        // After an attack the roll band collapses to DAZE, so attacks never repeat. Modelled as
        // a history gate onto a single weighted 20/80 roll, cached once per turn like JawWorm's.
        ConditionalBranchState gate = new("REPULSOR_GATE");
        RandomBranchState roll = new("REPULSOR_ROLL");
        gate.AddState(roll, () => !LastWas(attack));
        gate.AddState(daze, () => LastWas(attack));
        roll.AddBranch(attack, MoveRepeatType.CanRepeatForever, () => AttackSubRoll() ? 20f : 0f);
        roll.AddBranch(daze, MoveRepeatType.CanRepeatForever, () => AttackSubRoll() ? 0f : 80f);

        daze.FollowUpState = gate;
        attack.FollowUpState = gate;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { daze, attack, gate, roll }, gate);
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // One stable sub-roll per turn: vanilla draws aiRng.random(99) inside getMove; we draw once
    // per RollMove and cache it so both complementary weight lambdas see the same value.
    private bool? _subRoll;
    private int _subRollTurn = -1;
    private bool AttackSubRoll()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_subRoll == null || _subRollTurn != turn)
        {
            _subRoll = base.Rng.NextFloat() < 0.2f;
            _subRollTurn = turn;
        }
        return _subRoll.Value;
    }

    private async Task DazeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        // MakeTempCardInDrawPileAction(new Dazed(), dazeAmt, true, true): two Dazed shuffled
        // into the player's draw pile.
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, DazedAmount, null);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Repulsor",
        [
            ("DAZE_MOVE", "Daze"),
            ("ATTACK_MOVE", "Attack"),
        ]);
}
