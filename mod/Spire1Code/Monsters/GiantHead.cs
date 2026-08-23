using MegaCrit.Sts2.Core.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Giant Head elite (<c>com.megacrit.cardcrawl.monsters.beyond.GiantHead</c>).
/// 官方中文名：大脑袋。
/// <para>
/// Bytecode: HP 500/500, A8 520/520; COUNT_DMG 13 (no ascension variant); DEATH_DMG
/// (<c>startingDeathDmg</c>) 30, A3 40; GLARE_WEAK 1. <c>count</c> starts at 5, A18 at 4.
/// <c>damage</c> table: [0]=13, [1]=death, [2]=death+5 … [7]=death+30.
/// </para>
/// <para>
/// usePreBattleAction applies <see cref="SlowPower"/> to itself, then A18 decrements the count.
/// getMove: <c>count &gt; 1</c> → count-- then roll r&lt;50: last(GLARE) ? COUNT(13) : GLARE,
/// r&gt;=50: lastTwo(COUNT) ? GLARE : COUNT; <c>-6 &lt; count &lt;= 1</c> → count--, IT_IS_TIME;
/// <c>count &lt;= -6</c> → IT_IS_TIME (no further decrement). takeTurn IT_IS_TIME hits with
/// <c>damage[min(1 - count, 7)]</c> (SMASH), i.e. death +5 per elapsed turn after the first,
/// capped at death+30. Vanilla's intent label for late loops resets to the base death damage
/// while still dealing the capped value; we always display what will actually be dealt.
/// </para>
/// <para>
/// The counting shouts (<c>#r~N...~</c> / random DIALOG quotes) are cosmetic ShoutActions and
/// are omitted, consistent with <see cref="Spire1Monster.HasDeathSfx"/>.
/// Ascension mapping: A8 HP tier → ToughEnemies, A3 death-damage tier → DeadlyEnemies, the A18
/// head-start tier → DoubleBoss (top StS2 tier, the TheCollector A19 precedent).
/// Donor: <c>waterfall_giant</c> — a colossal stone figure whose rig ships every default track
/// (idle_loop/cast/attack/hurt/die), the closest shipped silhouette for a giant stone head.
/// </para>
/// </summary>
public sealed class GiantHead : Spire1Monster
{
    // setHp(500, 500); ascension >= 8 -> setHp(520, 520)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 520, 500);

    public override int MaxInitialHp => MinInitialHp;

    // startingDeathDmg = 30; ascension >= 3 -> 40
    private int DeathDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);

    // COUNT_DMG = 13 (no ascension variant)
    private const int CountDamage = 13;

    // Vanilla field: count (starts 5; A18 -> 4).
    private int _count = 5;

    protected override string DonorId => "waterfall_giant";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new SlowPower(this, 0)) on itself.
        await PowerCmd.Apply<SlowPower>(new ThrowingPlayerChoiceContext(), Creature, 0, Creature, null);
        // usePreBattleAction: ascension >= 18 -> count--.
        if (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss))
        {
            _count--;
        }
    }

    /// <summary>takeTurn IT_IS_TIME: damage[min(1 - count, 7)] from {13, d, d+5 … d+30}.</summary>
    private decimal ItIsTimeDamage
    {
        get
        {
            int index = Math.Min(1 - _count, 7);
            return DeathDamage + (index - 1) * 5;
        }
    }

    /// <summary>Intent-time view of ItIsTimeDamage: vanilla decrements count in getMove before
    /// setMove, so the shown value includes this turn's decrement; the mod decrements in the
    /// move body instead, so preview applies the same guarded decrement up front.</summary>
    private decimal ItIsTimePreview
    {
        get
        {
            int c = _count > -6 ? _count - 1 : _count;
            int index = Math.Min(1 - c, 7);
            return DeathDamage + (index - 1) * 5;
        }
    }

    /// <summary>Vanilla decrements in getMove while -6 &lt; count &lt;= 1; each move body runs once
    /// per turn, so performing the decrement there keeps branch predicates side-effect free.</summary>
    private void TickCount()
    {
        if (_count > -6)
        {
            _count--;
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState glare = new("GLARE_MOVE", GlareMove, new DebuffIntent());
        MoveState count = new("COUNT_MOVE", CountMove, new SingleAttackIntent(CountDamage));
        MoveState itIsTime = new("IT_IS_TIME_MOVE", ItIsTimeMove,
            new SingleAttackIntent(() => ItIsTimePreview));

        ConditionalBranchState decide = new("GIANT_HEAD_DECIDE");
        glare.FollowUpState = decide;
        count.FollowUpState = decide;
        itIsTime.FollowUpState = decide;

        // getMove gate: count <= 1 forces IT_IS_TIME regardless of the roll.
        decide.AddState(itIsTime, () => _count <= 1);
        // roll < 50: lastTwo(GLARE) ? COUNT : GLARE; roll >= 50: lastTwo(COUNT) ? GLARE : COUNT.
        decide.AddState(glare, () => RollHundred() < 50 ? !LastTwoWere(glare) : LastTwoWere(count));
        decide.AddState(count, () => true);

        return new MonsterMoveStateMachine([glare, count, itIsTime, decide], decide);
    }

    private async Task GlareMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn GLARE: ApplyPowerAction(player, this, WeakPower(1)).
        TickCount();
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, 1, Creature, null);
    }

    private async Task CountMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn COUNT: DamageAction(damage[0] = 13, FIRE).
        TickCount();
        await DamageCmd.Attack(CountDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private async Task ItIsTimeMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn IT_IS_TIME: DamageAction(damage[min(1-count,7)], SMASH).
        TickCount();
        await DamageCmd.Attack(ItIsTimeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
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

    // One stable 0-99 draw per move selection (vanilla passes one aiRng roll into getMove).
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

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;
    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Giant Head", "大脑袋"),
        [
            ("GLARE_MOVE", Tr("Glare", "瞪眼")),
            ("COUNT_MOVE", Tr("Count", "计数")),
            ("IT_IS_TIME_MOVE", Tr("It Is Time", "时机已到")),
        ]);
}
