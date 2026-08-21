using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-1 boss "The Guardian" (<c>com.megacrit.cardcrawl.monsters.exordium.TheGuardian</c>).
/// <para>
/// Two interleaved move chains, exactly as in vanilla. Offensive Mode loops
/// Charge Up (block 9) -> Fierce Bash (32) -> Vent Steam (Weak 2 + Vulnerable 2) -> Whirlwind
/// (5 x 4) -> Charge Up ...; the boss opens on Charge Up because <c>getMove</c> sets
/// <c>CHARGE_UP</c> whenever <c>isOpen</c> is true. Accumulated HP loss (<c>dmgTaken</c>, block
/// excluded — vanilla measures <c>currentHealth</c> before/after) trips Defensive Mode at
/// <c>dmgThreshold</c>, which starts at 30 and rises by <c>dmgThresholdIncrease = 10</c> on every
/// flip. Defensive Mode runs Close Up (Sharp Hide 3) -> Roll Attack (9) -> Twin Slam (8 x 2), and
/// Twin Slam flips back to Offensive Mode before it swings, then continues into Whirlwind.
/// </para>
/// <para>
/// Ascension mapping. Vanilla splits at A4 (damage), A9 (HP + threshold) and A19 (threshold +
/// thorns). StS2's levels are cumulative, so all three tiers survive:
/// <see cref="AscensionLevel.DeadlyEnemies"/> (deadlier enemies) carries the A4 damage bump,
/// <see cref="AscensionLevel.ToughEnemies"/> (tougher enemies) carries A9's HP and threshold, and
/// <see cref="AscensionLevel.DoubleBoss"/> — the topmost level, and the only boss-scoped one —
/// carries the A19 boss tier. DoubleBoss implies the other two, matching A19 &gt; A9 &gt; A4.
/// </para>
/// </summary>
public sealed class TheGuardian : Spire1Monster
{
    /// <summary>Block gained the instant the boss curls up, from <c>DEFENSIVE_BLOCK = 20</c>.</summary>
    private const int _defensiveBlock = 20;

    /// <summary><c>blockAmount = 9</c>, the Charge Up block.</summary>
    private const int _chargeUpBlock = 9;

    private const int _whirlwindDamage = 5;
    private const int _whirlwindHits = 4;
    private const int _twinSlamDamage = 8;
    private const int _twinSlamHits = 2;

    /// <summary><c>VENT_DEBUFF = 2</c>: Vent Steam applies 2 Weak and 2 Vulnerable.</summary>
    private const int _ventDebuff = 2;

    /// <summary><c>dmgThresholdIncrease = 10</c>.</summary>
    private const int _thresholdIncrease = 10;

    private int _dmgThreshold;
    private int _dmgTaken;
    private bool _isOpen = true;
    private bool _closeUpTriggered;
    private MoveState? _closeUpState;

    /// <summary>
    /// Shipped <c>Fabricator</c>: a boss-scale mechanical construct, and the one construct rig that
    /// carries the whole default animation set (<c>idle_loop</c>/<c>cast</c>/<c>attack</c>/
    /// <c>hurt</c>/<c>die</c>) that <c>MonsterModel.GenerateAnimator</c> expects.
    /// </summary>
    protected override string DonorId => "fabricator";

    /// <summary><c>HP = 240</c>, <c>A_2_HP = 250</c> (applied from A9 up in <c>&lt;init&gt;</c>).</summary>
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 240);

    public override int MaxInitialHp => MinInitialHp;

    /// <summary><c>FIERCE_BASH_DMG = 32</c>, <c>A_2_FIERCE_BASH_DMG = 36</c>.</summary>
    private static int FierceBashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 36, 32);

    /// <summary><c>ROLL_DMG = 9</c>, <c>A_2_ROLL_DMG = 10</c>.</summary>
    private static int RollDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

    /// <summary><c>thornsDamage = 3</c>, <c>thornsDamage + 1</c> at A19.</summary>
    private static int SharpHideAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 4, 3);

    /// <summary>
    /// <c>DMG_THRESHOLD = 30</c>, <c>A_2_DMG_THRESHOLD = 35</c> (A9), <c>A_19_DMG_THRESHOLD = 40</c>.
    /// </summary>
    private static int InitialThreshold => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 40,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 35, 30));

    public override List<(string, string)>? Localization =>
        new MonsterLoc("The Guardian",
        [
            ("CHARGE_UP_MOVE", "Charging Up"),
            ("FIERCE_BASH_MOVE", "Fierce Bash"),
            ("VENT_STEAM_MOVE", "Vent Steam"),
            ("WHIRLWIND_MOVE", "Whirlwind"),
            ("CLOSE_UP_MOVE", "Defensive Mode"),
            ("ROLL_ATTACK_MOVE", "Roll Attack"),
            ("TWIN_SLAM_MOVE", "Twin Slam"),
        ]);

    /// <summary>
    /// <c>usePreBattleAction</c>: apply Mode Shift at the current threshold and zero the damage
    /// counter (its <c>ChangeStateAction("Reset Threshold")</c>).
    /// </summary>
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        _dmgThreshold = InitialThreshold;
        _dmgTaken = 0;
        _isOpen = true;
        _closeUpTriggered = false;
        await PowerCmd.Apply<ModeShiftPower>(
            new ThrowingPlayerChoiceContext(), Creature, _dmgThreshold, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState chargeUp = new("CHARGE_UP_MOVE", ChargeUpMove, new DefendIntent());
        MoveState fierceBash = new("FIERCE_BASH_MOVE", FierceBashMove, new SingleAttackIntent(FierceBashDamage));
        MoveState ventSteam = new("VENT_STEAM_MOVE", VentSteamMove, new DebuffIntent());
        MoveState whirlwind = new("WHIRLWIND_MOVE", WhirlwindMove,
            new MultiAttackIntent(_whirlwindDamage, _whirlwindHits));
        // Forced from the damage hook mid-turn, so it must survive the next roll: without this the
        // state machine would immediately walk on to its follow-up (the shipped idiom, cf.
        // WaterfallGiant's ABOUT_TO_BLOW state).
        MoveState closeUp = new("CLOSE_UP_MOVE", CloseUpMove, new BuffIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState rollAttack = new("ROLL_ATTACK_MOVE", RollAttackMove, new SingleAttackIntent(RollDamage));
        MoveState twinSlam = new("TWIN_SLAM_MOVE", TwinSlamMove,
            new MultiAttackIntent(_twinSlamDamage, _twinSlamHits), new BuffIntent());

        // Offensive chain (getMove / useChargeUp / useFierceBash / useVentSteam / useWhirlwind).
        chargeUp.FollowUpState = fierceBash;
        fierceBash.FollowUpState = ventSteam;
        ventSteam.FollowUpState = whirlwind;
        whirlwind.FollowUpState = chargeUp;

        // Defensive chain (changeState("Defensive Mode") / useCloseUp / useRollAttack / useTwinSmash).
        closeUp.FollowUpState = rollAttack;
        rollAttack.FollowUpState = twinSlam;
        twinSlam.FollowUpState = whirlwind;

        _closeUpState = closeUp;
        List<MonsterState> states = [chargeUp, fierceBash, ventSteam, whirlwind, closeUp, rollAttack, twinSlam];
        return new MonsterMoveStateMachine(states, chargeUp);
    }

    /// <summary>
    /// <c>TheGuardian.damage()</c>: while open and before the flip has been queued, add the HP
    /// actually lost to <c>dmgTaken</c>, tick the Mode Shift readout down by the same amount, and
    /// curl up once the threshold is reached. <see cref="DamageResult.UnblockedDamage"/> is the HP
    /// delta vanilla measures, so blocked damage does not count toward the shift.
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Creature || !_isOpen || _closeUpTriggered || result.UnblockedDamage <= 0 || Creature.IsDead)
            return;

        _dmgTaken += result.UnblockedDamage;
        ModeShiftPower? modeShift = Creature.GetPower<ModeShiftPower>();
        if (modeShift != null)
            await PowerCmd.ModifyAmount(choiceContext, modeShift, -result.UnblockedDamage, null, null);

        if (_dmgTaken < _dmgThreshold)
            return;

        _dmgTaken = 0;
        _closeUpTriggered = true;
        await EnterDefensiveMode();
    }

    /// <summary><c>changeState("Defensive Mode")</c>.</summary>
    private async Task EnterDefensiveMode()
    {
        await PowerCmd.Remove<ModeShiftPower>(Creature);
        await CreatureCmd.GainBlock(Creature, _defensiveBlock, ValueProp.Move, null);
        _dmgThreshold += _thresholdIncrease;
        _isOpen = false;
        if (_closeUpState != null)
            SetMoveImmediate(_closeUpState, forceTransition: true);
    }

    /// <summary><c>changeState("Offensive Mode")</c> plus its <c>"Reset Threshold"</c> follow-up.</summary>
    private async Task EnterOffensiveMode()
    {
        await PowerCmd.Apply<ModeShiftPower>(
            new ThrowingPlayerChoiceContext(), Creature, _dmgThreshold, Creature, null);
        _dmgTaken = 0;
        if (Creature.Block > 0)
            await CreatureCmd.LoseBlock(new ThrowingPlayerChoiceContext(), Creature, Creature.Block, Creature);
        _isOpen = true;
        _closeUpTriggered = false;
    }

    private async Task ChargeUpMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, _chargeUpBlock, ValueProp.Move, null);
    }

    private async Task FierceBashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FierceBashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task VentSteamMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, _ventDebuff, Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, _ventDebuff, Creature, null);
    }

    private async Task WhirlwindMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(_whirlwindDamage).WithHitCount(_whirlwindHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task CloseUpMove(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<SharpHidePower>(
            new ThrowingPlayerChoiceContext(), Creature, SharpHideAmount, Creature, null);
    }

    private async Task RollAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RollDamage).FromMonster(this).WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>
    /// <c>useTwinSmash</c> queues the mode flip first, then the two slams, then drops Sharp Hide.
    /// </summary>
    private async Task TwinSlamMove(IReadOnlyList<Creature> targets)
    {
        await EnterOffensiveMode();
        await DamageCmd.Attack(_twinSlamDamage).WithHitCount(_twinSlamHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Remove<SharpHidePower>(Creature);
    }
}
