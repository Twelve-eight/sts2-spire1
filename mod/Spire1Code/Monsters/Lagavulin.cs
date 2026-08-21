using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — Lagavulin (<c>com.megacrit.cardcrawl.monsters.exordium.Lagavulin</c>).
/// <para>
/// Bytecode (asleep elite variant): HP 109-111, A8 112-115; attackDmg 18 (A3 20);
/// debuff -1 (A18 -2, dropped — see remarks); pre-battle GainBlock(8) + Metallicize(8).
/// getMove: <c>!isOut</c> -&gt; SLEEP(5); <c>isOut</c> -&gt; <c>debuffTurnCount &gt;= 2</c> ? DEBUFF
/// : (<c>lastTwoMoves(ATTACK)</c> ? DEBUFF : ATTACK).
/// takeTurn SLEEP(5): <c>idleCount++</c>; when <c>idleCount &gt;= 3</c> set <c>isOutTriggered</c>,
/// changeState("OPEN") and <c>SetMoveAction(ATTACK)</c> — so the first waking action is ATTACK,
/// not DEBUFF. takeTurn ATTACK(3): <c>debuffTurnCount++</c> then 18 damage (BLUNT_HEAVY).
/// takeTurn DEBUFF(1): <c>debuffTurnCount = 0</c> then Dexterity(-1) + Strength(-1) on the player.
/// takeTurn STUN(4): nothing but the "Stunned!" text. changeState("OPEN"): <c>isOut = true</c> and
/// <c>ReducePower(Metallicize, 8)</c>. damage(): if HP actually dropped and <c>!isOutTriggered</c>
/// -&gt; <c>setMove(STUN)</c>, <c>isOutTriggered = true</c>, changeState("OPEN").
/// </para>
/// <para>
/// Sleep/wake is our own state machine, not the engine's <c>AsleepPower</c>: vanilla wakes on the
/// 3rd idle turn (or the instant unblocked damage lands), which no shipped power reproduces.
/// The shell armour is our ported <see cref="MetallicizePower"/> (8), stripped in
/// <see cref="OpenShell"/> exactly like vanilla's ReducePower. Damage waking uses the engine's
/// <c>AfterDamageReceived</c> hook plus <c>SetMoveImmediate</c> (the engine's own SetMoveAction
/// equivalent — it refreshes the shown intent), giving the vanilla wasted "Stunned!" turn.
/// </para>
/// <para>
/// Ascension mapping: vanilla A8 HP tier -&gt; <c>ToughEnemies</c>, A3 damage tier -&gt;
/// <c>DeadlyEnemies</c> (the shipped StS2 convention). The vanilla A18 debuff tier (-2) has no
/// StS2 level above DeadlyEnemies to map onto without inventing one, so it is dropped: the debuff
/// is always -1.
/// </para>
/// </summary>
public sealed class Lagavulin : Spire1Monster
{
    // setHp(109, 111); ascension >= 8 -> setHp(112, 115)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 112, 109);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 115, 111);

    // attackDmg = 18; ascension >= 3 -> 20
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);

    // debuff = -1 (vanilla A18 tier of -2 is not modelled — see type remarks)
    private const int DebuffAmount = -1;

    // GainBlockAction(this, this, 8) + MetallicizePower(this, 8)
    private const int ArmorAmount = 8;

    // Vanilla fields: isOut, isOutTriggered, idleCount, debuffTurnCount.
    private bool _isOut;

    private bool _isOutTriggered;

    private int _idleCount;

    private int _debuffTurnCount;

    // Vanilla lastTwoMoves(ATTACK): true when the previous two performed moves were both ATTACK.
    private bool _previousWasAttack;

    private bool _twoAgoWasAttack;

    // Move states kept so damage-waking can force the stunned turn (vanilla SetMoveAction).
    private MoveState? _stunState;

    protected override string DonorId => "lagavulin_matriarch";

    // Donor LagavulinMatriarch overrides SetupSkins to start its eyes-closed track; keep the
    // borrowed scene sleeping-looking until it wakes.
    public override void SetupSkins(MegaSprite spine, MegaSkeleton skeleton)
    {
        spine.GetAnimationState().SetAnimation("_tracks/eyes_closed_loop", loop: true, 1);
    }

    /// <summary>
    /// The lagavulin_matriarch rig has no plain "attack" track (it ships attack_heavy and
    /// attack_double), so the engine-default animator would silently drop our attack animation.
    /// Map the engine triggers onto the tracks the donor rig actually has.
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt",
            attackName: "attack_heavy", castName: "cast");
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction (asleep): GainBlockAction(8) + ApplyPower(MetallicizePower, 8).
        await CreatureCmd.GainBlock(base.Creature, ArmorAmount, ValueProp.Unpowered, null);
        await PowerCmd.Apply<MetallicizePower>(new ThrowingPlayerChoiceContext(), base.Creature, ArmorAmount, base.Creature, null);
    }

    /// <summary>
    /// Vanilla changeState("OPEN"): isOut = true and ReducePowerAction(Metallicize, 8) — the shell
    /// armour it gained pre-battle goes away the moment it comes out.
    /// </summary>
    private async Task OpenShell()
    {
        if (_isOut)
        {
            return;
        }
        _isOut = true;
        _isOutTriggered = true;
        PowerModel? metallicize = base.Creature.GetPower<MetallicizePower>();
        if (metallicize != null)
        {
            await PowerCmd.Remove(metallicize);
        }
    }

    /// <summary>
    /// Vanilla damage(): HP actually dropped (block absorbs everything -> no wake) while still
    /// shelled forces the STUN move and opens the shell immediately, mid player turn.
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (target != base.Creature || _isOutTriggered || result.UnblockedDamage <= 0)
        {
            return;
        }
        await OpenShell();
        if (_stunState != null)
        {
            SetMoveImmediate(_stunState, forceTransition: true);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState sleep = new("SLEEP_MOVE", SleepMove, new SleepIntent());
        // MustPerformOnceBeforeTransitioning mirrors Creature.StunInternal: without it the
        // next RollMove transitions away before the stunned turn is ever performed.
        MoveState stun = new("STUN_MOVE", StunMove, new StunIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState debuff = new("DEBUFF_MOVE", DebuffMove, new DebuffIntent());
        MoveState attack = new("ATTACK_MOVE", AttackMove, new SingleAttackIntent(AttackDamage));
        _stunState = stun;
        ConditionalBranchState branch = new("LAGAVULIN_BRANCH");
        sleep.FollowUpState = branch;
        stun.FollowUpState = branch;
        debuff.FollowUpState = branch;
        attack.FollowUpState = branch;
        // getMove: still shelled -> SLEEP; out of the shell -> two attacks then a debuff.
        branch.AddState(sleep, () => !_isOut);
        branch.AddState(debuff, () => _debuffTurnCount >= 2 || (_previousWasAttack && _twoAgoWasAttack));
        branch.AddState(attack, () => true);
        return new MonsterMoveStateMachine([sleep, stun, debuff, attack, branch], sleep);
    }

    private async Task SleepMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn SLEEP: idleCount++; the 3rd idle turn opens the shell and queues ATTACK.
        _idleCount++;
        RecordMove(wasAttack: false);
        if (_idleCount < 3)
        {
            return;
        }
        await OpenShell();
    }

    private Task StunMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn STUN: TextAboveCreatureAction(STUNNED) only — the turn is wasted.
        RecordMove(wasAttack: false);
        return Task.CompletedTask;
    }

    private async Task DebuffMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn DEBUFF: debuffTurnCount = 0, then Dexterity(debuff) + Strength(debuff).
        _debuffTurnCount = 0;
        RecordMove(wasAttack: false);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn ATTACK: debuffTurnCount++, then attack for 18 with BLUNT_HEAVY.
        _debuffTurnCount++;
        RecordMove(wasAttack: true);
        await DamageCmd.Attack(AttackDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private void RecordMove(bool wasAttack)
    {
        _twoAgoWasAttack = _previousWasAttack;
        _previousWasAttack = wasAttack;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Lagavulin",
        [
            ("SLEEP_MOVE", "Sleep"),
            ("STUN_MOVE", "Stunned"),
            ("DEBUFF_MOVE", "Siphon Soul"),
            ("ATTACK_MOVE", "Attack"),
        ]);
}
