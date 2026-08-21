using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium "Gremlin Wizard" (<c>com.megacrit.cardcrawl.monsters.exordium.GremlinWizard</c>).
/// <para>
/// Bytecode values: HP 21-25 (A7+: 22-26), Dopey Magic 25 (A2+: 30), <c>CHARGE_LIMIT</c> 3, and the
/// constructor seeds <c>currentCharge = 1</c>. Exact <c>takeTurn</c> rule (CHARGE is move byte 2,
/// DOPE_MAGIC is byte 1, and <c>getMove</c> always opens on CHARGE):
/// <list type="bullet">
/// <item>CHARGE: <c>currentCharge++</c>; if <c>escapeNext</c> → Escape; else if
/// <c>currentCharge == 3</c> → DOPE_MAGIC; else CHARGE again.</item>
/// <item>DOPE_MAGIC: deal damage and reset <c>currentCharge = 0</c>; if <c>escapeNext</c> → Escape;
/// else at ascension 17+ cast again immediately, otherwise return to CHARGE.</item>
/// </list>
/// Because the counter starts at 1, the first blast lands after two charge turns and every later
/// blast after three, exactly as in StS1.
/// </para>
/// <para>
/// Ascension mapping: HP → <see cref="AscensionLevel.ToughEnemies"/> (A8); damage and the StS1 A17
/// repeat-cast tier → <see cref="AscensionLevel.DeadlyEnemies"/> (A9), the highest enemy-difficulty
/// level StS2 exposes.
/// </para>
/// </summary>
public sealed class GremlinWizard : Spire1Monster
{
    private const int ChargeLimit = 3;

    private int _currentCharge = 1;
    private bool _escapeNext;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 21);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 26, 25);

    private int MagicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);

    private static bool RepeatsCast => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);

    /// <summary>Borrows the shipped DampCultist scene (robed caster rig).</summary>
    protected override string DonorId => "damp_cultist";

    /// <summary>
    /// The damp_cultist rig ships <c>idle_loop</c>, <c>buff</c>, <c>attack</c>, <c>hurt</c> and
    /// <c>die</c> (shipped <c>DampCultist.GenerateAnimator</c>). Only the cast animation is named
    /// differently from the engine default (<c>cast</c>), so the Cast trigger is remapped onto
    /// <c>buff</c> and everything else keeps the default names.
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack", castName: "buff");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState charge = new("CHARGE", ChargeMove, new UnknownIntent());
        MoveState dopeMagic = new("DOPE_MAGIC", DopeMagicMove, new SingleAttackIntent(MagicDamage));
        MoveState escape = new("ESCAPE", EscapeMove, new EscapeIntent());

        // takeTurn/CHARGE tail: escapeNext wins, then the charge counter decides.
        ConditionalBranchState afterCharge = new("AFTER_CHARGE");
        afterCharge.AddState(escape, () => _escapeNext);
        afterCharge.AddState(dopeMagic, () => _currentCharge >= ChargeLimit);
        afterCharge.AddState(charge, () => true);

        // takeTurn/DOPE_MAGIC tail: escapeNext wins, then the A17 repeat-cast gate.
        ConditionalBranchState afterCast = new("AFTER_CAST");
        afterCast.AddState(escape, () => _escapeNext);
        afterCast.AddState(dopeMagic, () => RepeatsCast);
        afterCast.AddState(charge, () => true);

        charge.FollowUpState = afterCharge;
        dopeMagic.FollowUpState = afterCast;
        escape.FollowUpState = escape;

        // Branch states must be registered too, or FindNextMoveState throws "no valid state found".
        List<MonsterState> states = [charge, dopeMagic, escape, afterCharge, afterCast];
        return new MonsterMoveStateMachine(states, charge);
    }

    private async Task ChargeMove(IReadOnlyList<Creature> targets)
    {
        _currentCharge++;
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.45f);
        await Cmd.CustomScaledWait(0.25f, 0.5f);
    }

    private async Task DopeMagicMove(IReadOnlyList<Creature> targets)
    {
        _currentCharge = 0;
        // StS1 uses AttackEffect.FIRE for this blast.
        await DamageCmd.Attack(MagicDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private async Task EscapeMove(IReadOnlyList<Creature> targets)
    {
        // Removal idiom copied from shipped FatGremlin.FleeMove / ThievingHopper.EscapeMove.
        await Cmd.CustomScaledWait(0.75f, 1.25f);
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.ToggleIsInteractable(on: false);
        await CreatureCmd.Escape(Creature);
    }

    /// <summary>StS1 <c>deathReact</c>: an ally dying sets this gremlin's next move to Escape.</summary>
    public override Task BeforeDeath(Creature creature)
    {
        if (creature == Creature || !creature.IsEnemy)
        {
            return Task.CompletedTask;
        }

        _escapeNext = true;
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc(
            "Gremlin Wizard",
            [
                ("CHARGE", "Charging"),
                ("DOPE_MAGIC", "Dopey Magic"),
                ("ESCAPE", "Escape"),
            ]);
}
