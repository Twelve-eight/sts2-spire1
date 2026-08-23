using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Shelled Parasite (<c>com.megacrit.cardcrawl.monsters.city.ShelledParasite</c>).
/// 官方中文名：带壳寄生怪。
/// <para>
/// Bytecode: HP 68-72, A7 70-75; doubleStrikeDmg 6 (A2 7), fellDmg 18 (A2 21), suckDmg 10 (A2 12);
/// PLATED_ARMOR_AMT 14, FELL_FRAIL_AMT 2. usePreBattleAction applies PlatedArmorPower(14) AND
/// grants 14 Block up front.
/// </para>
/// <para>
/// Shell-break: StS1's plated armor loses 1 stack per unblocked hit; when it reaches 0 the
/// monster changes to ARMOR_BREAK — three hops, then its pending move becomes STUNNED
/// (a wasted turn showing the Stunned text; the next pending move after it is FELL, which
/// vanilla's takeTurn case 4 sets directly). Ported via <see cref="AfterDamageReceivedLate"/>
/// (runs after the power's decrement) forcing <see cref="SetMoveImmediate"/> onto the stunned
/// state, the Lagavulin wake-up idiom; the hop VFX is cosmetic and omitted.
/// </para>
/// <para>
/// getMove: firstMove → A17+ FELL, else randomBoolean ? DOUBLE_STRIKE : LIFE_SUCK; afterwards
/// r&lt;20: last(FELL) ? reroll 20-99 : FELL; 20&lt;=r&lt;60: lastTwo(DOUBLE_STRIKE) ? LIFE_SUCK :
/// DOUBLE_STRIKE; r&gt;=60: lastTwo(LIFE_SUCK) ? DOUBLE_STRIKE : LIFE_SUCK. Recursive rerolls are
/// approximated by falling through to the next band (Darkling precedent).
/// </para>
/// <para>
/// LIFE_SUCK uses StS1's VampireDamageAction — the parasite heals for the unblocked damage it
/// deals, read back from the attack's <see cref="DamageResult"/>s. Ascension mapping: A7 HP tier
/// → ToughEnemies, A2 damage tier → DeadlyEnemies, A17 first-move tier → DoubleBoss (top tier;
/// Darkling maps its A17 tier onto DeadlyEnemies, but DoubleBoss is used here for uniformity
/// with the other A17+/A18 tiers in this batch).
/// Donor: <c>phrog_parasite</c> — a shipped parasite creature with a standard rig.
/// </para>
/// </summary>
public sealed class ShelledParasite : Spire1Monster
{
    // setHp(68, 72); ascension >= 7 -> setHp(70, 75)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 70, 68);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 75, 72);

    // doubleStrikeDmg = 6; ascension >= 2 -> 7
    private int DoubleStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    // fellDmg = 18; ascension >= 2 -> 21
    private int FellDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);

    // suckDmg = 10; ascension >= 2 -> 12
    private int SuckDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    // PLATED_ARMOR_AMT = 14, FELL_FRAIL_AMT = 2 (no ascension variants)
    private const int PlatedArmorAmount = 14;

    private const int FellFrailAmount = 2;

    // Vanilla fields: firstMove (starts true), shell-broken latch (one-shot stun).
    private bool _firstMove = true;

    private bool _shellBroken;

    // The STUNNED move state, forced when the plated armor runs out (vanilla changeState ARMOR_BREAK).
    private MoveState? _stunState;

    protected override string DonorId => "phrog_parasite";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(PlatedArmorPower(14)) + GainBlockAction(14).
        await PowerCmd.Apply<PlatedArmorPower>(new ThrowingPlayerChoiceContext(), Creature, PlatedArmorAmount, Creature, null);
        await CreatureCmd.GainBlock(Creature, PlatedArmorAmount, ValueProp.Unpowered, null);
    }

    /// <summary>
    /// Shell-break detection. <c>AfterDamageReceivedLate</c> runs after every model's
    /// <c>AfterDamageReceived</c>, so PlatedArmorPower has already decremented by now.
    /// Vanilla's ARMOR_BREAK also plays three hops (cosmetic — omitted) and forces STUNNED.
    /// </summary>
    public override async Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceivedLate(choiceContext, target, result, props, dealer, cardSource);
        if (target != Creature || _shellBroken || result.UnblockedDamage <= 0)
        {
            return;
        }
        PlatedArmorPower? armor = Creature.GetPower<PlatedArmorPower>();
        if (armor != null && armor.Amount > 0)
        {
            return;
        }
        _shellBroken = true;
        if (_stunState != null)
        {
            SetMoveImmediate(_stunState, forceTransition: true);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState fell = new("FELL_MOVE", FellMove,
            new SingleAttackIntent(FellDamage), new DebuffIntent());
        MoveState doubleStrike = new("DOUBLE_STRIKE_MOVE", DoubleStrikeMove,
            new MultiAttackIntent(DoubleStrikeDamage, 2));
        MoveState lifeSuck = new("LIFE_SUCK_MOVE", LifeSuckMove,
            new SingleAttackIntent(SuckDamage), new BuffIntent());
        MoveState stunned = new("STUNNED_MOVE", StunnedMove, new StunIntent())
        {
            // Lagavulin/Byrd idiom: the forced stunned turn must be performed once before the
            // next RollMove transitions away from it.
            MustPerformOnceBeforeTransitioning = true
        };
        _stunState = stunned;

        ConditionalBranchState decide = new("SHELLED_PARASITE_DECIDE");
        fell.FollowUpState = decide;
        doubleStrike.FollowUpState = decide;
        lifeSuck.FollowUpState = decide;

        // takeTurn STUNNED ends with setMove(FELL) — the post-stun move is fixed, not rolled.
        stunned.FollowUpState = fell;

        // Opening (vanilla firstMove latch): A17+ -> FELL, else 50/50 DOUBLE_STRIKE / LIFE_SUCK.
        decide.AddState(fell, () => FirstMovePick(AscensionHelper.HasAscension(AscensionLevel.DoubleBoss)));
        decide.AddState(doubleStrike, () => FirstMovePick(RollHundred() < 50));
        decide.AddState(lifeSuck, () => ConsumeFirstMove());
        // roll < 20: FELL unless the last move was FELL (vanilla rerolls 20-99).
        decide.AddState(fell, () => RollHundred() < 20 && !LastWas(fell));
        // 20-59: DOUBLE_STRIKE unless the last two were DOUBLE_STRIKE (then LIFE_SUCK).
        decide.AddState(doubleStrike, () => RollHundred() < 60 && !LastTwoWere(doubleStrike));
        // >= 60: LIFE_SUCK unless the last two were LIFE_SUCK (then DOUBLE_STRIKE).
        decide.AddState(lifeSuck, () => !LastTwoWere(lifeSuck));
        decide.AddState(doubleStrike, () => true);

        return new MonsterMoveStateMachine([fell, doubleStrike, lifeSuck, stunned, decide], decide);
    }

    private async Task FellMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn FELL: AnimateSlowAttackAction + Wait + DamageAction(BLUNT_HEAVY) +
        // FrailPower(2) on the player.
        await DamageCmd.Attack(FellDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, FellFrailAmount, Creature, null);
    }

    private async Task DoubleStrikeMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn DOUBLE_STRIKE: two hops, each DamageAction(damage[0], BLUNT_LIGHT).
        await DamageCmd.Attack(DoubleStrikeDamage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task LifeSuckMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn LIFE_SUCK: ChangeState ATTACK + BiteEffect VFX + VampireDamageAction —
        // the parasite heals for the unblocked damage it deals.
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.6f);
        var attack = DamageCmd.Attack(SuckDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt");
        await attack.Execute(null);
        decimal dealt = attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage);
        if (dealt > 0)
        {
            await CreatureCmd.Heal(Creature, dealt);
        }
    }

    private Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn STUNNED: TextAboveCreatureAction(STUNNED) only — the turn is wasted.
        return Task.CompletedTask;
    }

    private bool FirstMovePick(bool wanted)
    {
        if (!_firstMove || !wanted)
        {
            return false;
        }
        _firstMove = false;
        return true;
    }

    private bool ConsumeFirstMove()
    {
        if (!_firstMove)
        {
            return false;
        }
        _firstMove = false;
        return true;
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
        new MonsterLoc(Tr("Shelled Parasite", "带壳寄生怪"),
        [
            ("FELL_MOVE", Tr("Fell", "坠落")),
            ("DOUBLE_STRIKE_MOVE", Tr("Double Strike", "双重打击")),
            ("LIFE_SUCK_MOVE", Tr("Life Suck", "生命吸取")),
            ("STUNNED_MOVE", Tr("Stunned", "晕眩")),
        ]);
}
