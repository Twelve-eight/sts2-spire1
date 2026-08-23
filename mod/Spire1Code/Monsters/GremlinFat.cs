using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium "Fat Gremlin" (<c>com.megacrit.cardcrawl.monsters.exordium.GremlinFat</c>).
/// <para>
/// Bytecode values: HP 13-17 (A7+: 14-18), Blunt 4 (A2+: 5), Weak 1, and at ascension 17+ the
/// attack additionally applies Frail 1 (gate inside <c>takeTurn</c>, case BLUNT). AI:
/// <c>getMove</c> always rolls Blunt; after each Blunt the next move is Escape if
/// <c>escapeNext</c> was set by <c>deathReact</c> (ally died), otherwise RollMove re-rolls — but
/// <c>getMove</c> has no randomness, so Blunt repeats forever. Reproduced with a conditional
/// branch exactly like the other gremlins.
/// </para>
/// </summary>
public sealed class GremlinFat : Spire1Monster
{
    private bool _escapeNext;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 13);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

    private int BluntDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    /// <summary>Borrows the shipped FatGremlin scene — exact StS1 counterpart rig.</summary>
    protected override string DonorId => "fat_gremlin";

    /// <summary>
    /// The fat_gremlin rig ships <c>awake_loop</c>, <c>spawn</c>, <c>flee</c>, <c>stunned_loop</c>,
    /// <c>wake_up</c>, <c>hurt_stunned</c>, <c>hurt_awake</c> and <c>die</c> (shipped
    /// <c>FatGremlin.GenerateAnimator</c>; each also exists under a <c>_no_bag/</c> prefix). There
    /// is no <c>idle_loop</c>, <c>hurt</c> or attack animation at all — the shipped fat gremlin
    /// only ever flees — so idle/hit are remapped onto the awake variants and the attack trigger is
    /// left unmapped (it would only log a missing-animation warning).
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "awake_loop", "die", hitName: "hurt_awake");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState blunt = new("BLUNT", BluntMove, new SingleAttackIntent(BluntDamage), new DebuffIntent());
        MoveState escape = new("ESCAPE", EscapeMove, new EscapeIntent());
        ConditionalBranchState reactToAllyDeath = new("REACT_TO_ALLY_DEATH");
        blunt.FollowUpState = reactToAllyDeath;
        escape.FollowUpState = escape;
        reactToAllyDeath.AddState(escape, () => _escapeNext);
        reactToAllyDeath.AddState(blunt, () => !_escapeNext);
        List<MonsterState> states = [blunt, escape, reactToAllyDeath];
        return new MonsterMoveStateMachine(states, blunt);
    }

    private async Task BluntMove(IReadOnlyList<Creature> targets)
    {
        // No attacker anim: the fat_gremlin rig ships no attack animation (see
        // SetupCustomAnimationStates). StS1 pairs AnimateSlowAttackAction with AttackEffect.BLUNT_HEAVY,
        // so the heavy-blunt hit fx carries the impact instead.
        await DamageCmd.Attack(BluntDamage).FromMonster(this)
            .WithNoAttackerAnim()
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, 1m, Creature, null);
        // takeTurn gate: ascension >= 17 also applies Frail 1. StS1's A17 tier maps onto
        // DeadlyEnemies (A9), the highest enemy-difficulty level StS2 exposes.
        if (AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, 1m, Creature, null);
        }
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
            "StS1 - Fat Gremlin",
            [
                ("BLUNT", "Blunt"),
                ("ESCAPE", "Escape"),
            ]);
}
