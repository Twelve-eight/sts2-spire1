using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Snake Plant (<c>com.megacrit.cardcrawl.monsters.city.SnakePlant</c>). 官方中文名：蛇花。
/// <para>
/// Bytecode: HP 75-79, A7 78-82; CHOMPY_DMG 7 (A2 8) × 3 hits; SPORES = Frail 2 + Weak 2.
/// usePreBattleAction: MalleablePower(3). getMove: A17 branch unreachable in StS2 (max A10);
/// base script — r&lt;65 → lastTwoMoves(CHOMPY) ? SPORES : CHOMPY;
/// else lastMove(SPORES) || lastMoveBefore(SPORES) ? CHOMPY : SPORES
/// (vanilla intent label MOVES[0] "Chomp" is reused for the debuff turn).
/// </para>
/// <para>
/// FLAGGED: vanilla <c>MalleablePower</c> (gain block per unblocked hit taken) has no shipped StS2
/// equivalent under that name; the closest shipped defensive-on-hit behaviour in this mod's scope is
/// the ported <see cref="MetallicizePower"/> used by Lagavulin, applied here at the vanilla stack
/// count. If a shipped Malleable-style power ships later, swap the Apply call.
/// </para>
/// <para>
/// Donor: <c>fogmog</c> — a rooted, plant-like creature with snapping maw tracks; closest visual
/// match among shipped scenes for a stationary carnivorous plant.
/// </para>
/// </summary>
public sealed class SnakePlant : Spire1Monster
{

    public override List<(string, string)>? Localization => new MonsterLoc(
        "Snake Plant",
        new[]
        {
            ("BITE", "Bite"),
            ("CHOMP", "Chomp")
        });
    // setHp(75, 79); ascension >= 7 -> setHp(78, 82)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 75);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 82, 79);

    // chompyDmg = 7 x3; ascension >= 2 -> 8
    private int ChompyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    private const int ChompyHits = 3;

    private const int SporesFrailTurns = 2;
    private const int SporesWeakTurns = 2;

    // Vanilla MalleablePower stacks (see FLAG note).
    private const int MalleableAmount = 3;

    protected override string DonorId => "fogmog";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new MalleablePower(this)) — see FLAG note.
        await PowerCmd.Apply<MetallicizePower>(new ThrowingPlayerChoiceContext(), base.Creature, MalleableAmount, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState chompy = new("CHOMPY_CHOMPS_MOVE", ChompyChompsMove, new MultiAttackIntent(ChompyDamage, ChompyHits));
        MoveState spores = new("SPORES_MOVE", SporesMove, new DebuffIntent());

        ConditionalBranchState branch = new("SNAKE_PLANT_BRANCH");
        chompy.FollowUpState = branch;
        spores.FollowUpState = branch;

        // r<65 → two Chompy turns in a row force Spores; otherwise Spores never repeats within the
        // previous two turns (lastMove || lastMoveBefore guard), so it alternates into Chompy.
        branch.AddState(spores, () => RollHundred() < 65 && LastTwoWere(chompy));
        branch.AddState(chompy, () => RollHundred() < 65);
        branch.AddState(chompy, () => LastWas(spores) || LastTwoAgoWas(spores));
        branch.AddState(spores, () => true);

        return new MonsterMoveStateMachine([chompy, spores, branch], chompy);
    }

    private async Task ChompyChompsMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompyDamage).WithHitCount(ChompyHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SporesMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, SporesFrailTurns, base.Creature, null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, SporesWeakTurns, base.Creature, null);
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    private bool LastTwoAgoWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^2], state);
    }

    private bool LastTwoWere(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }

    // One stable 0-99 draw per move selection (vanilla passes one aiRng roll through getMove).
    private int? _roll;
    private int _rollTurn = -1;
    private int RollHundred()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_roll == null || _rollTurn != turn)
        {
            _roll = base.Rng.NextInt(100);
            _rollTurn = turn;
        }
        return _roll.Value;
    }
}
