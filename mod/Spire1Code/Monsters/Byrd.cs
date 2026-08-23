using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
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
/// StS1 The City — Byrd (<c>com.megacrit.cardcrawl.monsters.city.Byrd</c>). 官方中文名：异鸟。
/// <para>
/// Bytecode: HP 25-31, A7 26-33; PECK_DMG 1, PECK_COUNT 5 (A2 6), SWOOP_DMG 12 (A2 14),
/// HEADBUTT_DMG 3 (damage[2], only referenced by the intent — the stunned turn deals no damage),
/// CAW_STR 1, flightAmt 3 (vanilla A17 tier 4 unreachable in StS2 — dropped).
/// usePreBattleAction: FlightPower(flightAmt). getMove: first move — 37.5% CAW else PECK.
/// While flying: r&lt;50 → lastTwoMoves(SWOOP) ? (40% SWOOP : CAW) : PECK;
/// 50≤r&lt;70 → lastMove(SWOOP) ? (37.5% CAW : PECK) : SWOOP;
/// r≥70 → lastMove(CAW) ? (28.57% SWOOP : PECK) : CAW.
/// When Flight depletes (mid player turn), changeState("GROUNDED") forces the STUNNED move;
/// after the wasted stunned turn the grounded bird SWOOPs every turn.
/// </para>
/// <para>
/// FLAGGED: vanilla <c>FlightPower</c> (each hit deals at most 1 while stacks last) has no shipped
/// StS2 equivalent under that name; the closest shipped behaviour is <see cref="IntangiblePower"/>
/// (damage reduced to 1 per hit, one stack consumed per hit), which is applied here with the same
/// stack count. Our own counter mirrors the depletion trigger: once unblocked damage has landed
/// <c>flightAmt</c> times the bird lands and the stunned turn is forced via
/// <see cref="Spire1Monster.SetMoveImmediate"/> (Lagavulin wake idiom).
/// </para>
/// <para>
/// Donor: <c>byrdpip</c> — the shipped small bird creature; closest visual match among the 121
/// shipped scenes for a flapping nuisance bird.
/// </para>
/// </summary>
public sealed class Byrd : Spire1Monster
{
    // setHp(25, 31); ascension >= 7 -> setHp(26, 33)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 33, 31);

    // peckDmg = 1 (both tiers)
    private const int PeckDamage = 1;

    // peckCount = 5; ascension >= 2 -> 6
    private int PeckCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    // swoopDmg = 12; ascension >= 2 -> 14
    private int SwoopDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    // HEADBUTT (STUNNED) turn deals no damage; the 3 lives only in the intent preview.
    private const int HeadbuttPreviewDamage = 3;

    // caw strength = 1
    private const int CawStrength = 1;

    // flightAmt = 3 (vanilla A17 tier of 4 unreachable in StS2; base value kept)
    private const int FlightAmount = 3;

    protected override string DonorId => "byrdpip";

    // Vanilla fields: isFlying (starts true), firstMove.
    private bool _grounded;

    private bool _landStunPerformed;

    private bool _everMoved;

    private MoveState? _landStunState;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new FlightPower(this, flightAmt)) — see FLAG note.
        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), base.Creature, FlightAmount, base.Creature, null);
    }

    /// <summary>
    /// Vanilla FlightPower depletion → changeState("GROUNDED"): setMove(STUNNED) + createIntent,
    /// i.e. the landing is telegraphed immediately and the next monster turn is wasted.
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (target != base.Creature || _grounded || result.UnblockedDamage <= 0)
        {
            return;
        }
        if (--_flightRemaining > 0)
        {
            return;
        }
        _grounded = true;
        _landStunPerformed = false;
        if (_landStunState != null)
        {
            SetMoveImmediate(_landStunState, forceTransition: true);
        }
    }

    private int _flightRemaining = FlightAmount;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState peck = new("PECK_MOVE", PeckMove, new MultiAttackIntent(PeckDamage, PeckCount));
        MoveState swoop = new("SWOOP_MOVE", SwoopMove, new SingleAttackIntent(SwoopDamage));
        MoveState caw = new("CAW_MOVE", CawMove, new BuffIntent());
        MoveState landStun = new("HEADBUTT_MOVE", LandStunMove, new StunIntent())
        {
            // Mirrors Creature.StunInternal (Lagavulin idiom): the forced post-landing turn must be
            // performed once before the next RollMove transitions away from it.
            MustPerformOnceBeforeTransitioning = true,
        };
        _landStunState = landStun;

        ConditionalBranchState branch = new("BYRD_BRANCH");
        peck.FollowUpState = branch;
        swoop.FollowUpState = branch;
        caw.FollowUpState = branch;
        landStun.FollowUpState = branch;

        // Predicate order reproduces vanilla priority: landed → forced stun, then permanent ground
        // swooping, then the opening roll, then the three flying roll bands.
        branch.AddState(landStun, () => _grounded && !_landStunPerformed);
        branch.AddState(swoop, () => _grounded);
        branch.AddState(caw, () => !_everMoved && TurnDraw(0.375f));
        branch.AddState(peck, () => !_everMoved);
        // Band A (roll < 50): last two were SWOOP → 40% SWOOP else CAW; otherwise PECK.
        branch.AddState(swoop, () => RollHundred() < 50 && LastTwoWere(swoop) && TurnDraw(0.4f));
        branch.AddState(caw, () => RollHundred() < 50 && LastTwoWere(swoop));
        branch.AddState(peck, () => RollHundred() < 50);
        // Band B (50 ≤ roll < 70): last was SWOOP → 37.5% CAW else PECK; otherwise SWOOP.
        branch.AddState(caw, () => RollHundred() < 70 && LastWas(swoop) && TurnDraw(0.375f));
        branch.AddState(peck, () => RollHundred() < 70 && LastWas(swoop));
        branch.AddState(swoop, () => RollHundred() < 70);
        // Band C (roll ≥ 70): last was CAW → 28.57% SWOOP else PECK; otherwise CAW.
        branch.AddState(swoop, () => LastWas(caw) && TurnDraw(0.2857f));
        branch.AddState(peck, () => LastWas(caw));
        branch.AddState(caw, () => true);

        return new MonsterMoveStateMachine([peck, swoop, caw, landStun, branch], peck);
    }

    private async Task PeckMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await DamageCmd.Attack(PeckDamage).WithHitCount(PeckCount).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task SwoopMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await DamageCmd.Attack(SwoopDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task CawMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, CawStrength, base.Creature, null);
    }

    private Task LandStunMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn HEADBUTT/STUNNED: head_lift animation + "Stunned!" text only — the turn is wasted.
        _everMoved = true;
        _landStunPerformed = true;
        return Task.CompletedTask;
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
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

    // One stable boolean draw per turn; vanilla executes at most one randomBoolean per getMove.
    private bool? _draw;
    private int _drawTurn = -1;
    private bool TurnDraw(float threshold)
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_draw == null || _drawTurn != turn)
        {
            _draw = base.Rng.NextFloat() < threshold;
            _drawTurn = turn;
        }
        return _draw.Value;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Byrd",
        [
            ("PECK_MOVE", "Peck"),
            ("SWOOP_MOVE", "Swoop"),
            ("CAW_MOVE", "Caw"),
            ("HEADBUTT_MOVE", "Head Butt"),
        ]);
}
