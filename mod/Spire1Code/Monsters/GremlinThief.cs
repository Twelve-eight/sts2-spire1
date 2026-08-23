using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
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
/// StS1 Exordium "Sneaky Gremlin" (<c>com.megacrit.cardcrawl.monsters.exordium.GremlinThief</c>).
/// <para>
/// Bytecode values: HP 10-14 (A7+: 11-15), Puncture 9 (A2+: 10). AI: <c>getMove</c> always rolls
/// Puncture; after each Puncture, if <c>escapeNext</c> was set the next move becomes Escape
/// (byte 99), otherwise Puncture repeats forever. <c>escapeNext</c> is only ever set by
/// <c>deathReact</c> when an ally dies — same rule as Mad Gremlin.
/// </para>
/// </summary>
public sealed class GremlinThief : Spire1Monster
{
    private bool _escapeNext;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 14);

    private int PunctureDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

    /// <summary>Borrows the shipped SneakyGremlin scene — exact StS1 counterpart rig.</summary>
    protected override string DonorId => "sneaky_gremlin";

    /// <summary>
    /// The sneaky_gremlin rig ships <c>awake_loop</c>, <c>spawn</c>, <c>attack</c>,
    /// <c>stunned_loop</c>, <c>wake_up</c>, <c>hurt_stunned</c>, <c>hurt_awake</c> and <c>die</c>
    /// (shipped <c>SneakyGremlin.GenerateAnimator</c>) — it has no <c>idle_loop</c> or <c>hurt</c>,
    /// which is what <see cref="MonsterModel.GenerateAnimator"/> asks for by default, so the idle
    /// and hit states are remapped onto the awake variants. This gremlin is never asleep in StS1.
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "awake_loop", "die", hitName: "hurt_awake", attackName: "attack");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState puncture = new("PUNCTURE", PunctureMove, new SingleAttackIntent(PunctureDamage));
        MoveState escape = new("ESCAPE", EscapeMove, new EscapeIntent());
        ConditionalBranchState reactToAllyDeath = new("REACT_TO_ALLY_DEATH");
        puncture.FollowUpState = reactToAllyDeath;
        escape.FollowUpState = escape;
        reactToAllyDeath.AddState(escape, () => _escapeNext);
        reactToAllyDeath.AddState(puncture, () => !_escapeNext);
        List<MonsterState> states = [puncture, escape, reactToAllyDeath];
        return new MonsterMoveStateMachine(states, puncture);
    }

    private async Task PunctureMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(PunctureDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.1f)
            .WithHitFx("vfx/vfx_attack_slash")
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
            "StS1 - Sneaky Gremlin",
            [
                ("PUNCTURE", "Puncture"),
                ("ESCAPE", "Escape"),
            ]);
}
