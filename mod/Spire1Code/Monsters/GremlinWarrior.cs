using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium "Mad Gremlin" (<c>com.megacrit.cardcrawl.monsters.exordium.GremlinWarrior</c>).
/// <para>
/// Bytecode values: HP 20-24 (A7+: 21-25), Scratch 4 (A2+: 5), Angry 1 (A17+: 2) applied in
/// <c>usePreBattleAction</c>. AI: <c>getMove</c> always rolls Scratch; <c>deathReact</c> switches
/// the next move to Escape (byte 99) when an ally dies, unless already escaping. Reproduced here
/// with a <see cref="ConditionalBranchState"/> flipped by the <see cref="BeforeDeath"/> hook —
/// the branch is evaluated at the next roll, matching StS1's "next move becomes Escape" timing.
/// </para>
/// <para>
/// Ascension mapping (StS2 exposes exactly two enemy-difficulty levels, ToughEnemies = A8 and
/// DeadlyEnemies = A9, and <c>HasLevel</c> is <c>runLevel &gt;= (int)level</c>): StS1's low-tier
/// bumps (A2 damage, A7 HP) map to those two shipped levels by kind — HP to
/// <see cref="AscensionLevel.ToughEnemies"/>, damage to <see cref="AscensionLevel.DeadlyEnemies"/>,
/// matching shipped monster convention — and StS1's A17 tier (Angry 2) maps to
/// <see cref="AscensionLevel.DeadlyEnemies"/>, the highest enemy-difficulty level that exists.
/// </para>
/// </summary>
public sealed class GremlinWarrior : Spire1Monster
{
    private bool _escapeNext;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 21, 20);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 25, 24);

    private int ScratchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    /// <summary>Borrows the shipped GremlinMerc scene — the largest of the three gremlin rigs.</summary>
    protected override string DonorId => "gremlin_merc";

    /// <summary>
    /// The gremlin_merc rig ships <c>idle_loop</c>, <c>attack_single</c>, <c>attack_double</c>,
    /// <c>hurt</c> and <c>die</c> (see shipped <c>GremlinMerc.GenerateAnimator</c>) — it has no
    /// plain <c>attack</c> animation, which is what <see cref="MonsterModel.GenerateAnimator"/>
    /// would ask for by default, so the "Attack" trigger is remapped onto <c>attack_single</c>.
    /// Without this the attack animation silently degrades to a logged warning.
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack_single");

    public override async Task AfterAddedToRoom()
    {
        // usePreBattleAction: Angry 1, or 2 at ascension 17+.
        await PowerCmd.Apply<AngryPower>(
            new ThrowingPlayerChoiceContext(),
            Creature,
            AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1),
            Creature,
            null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState scratch = new("SCRATCH", ScratchMove, new SingleAttackIntent(ScratchDamage));
        MoveState escape = new("ESCAPE", EscapeMove, new EscapeIntent());
        ConditionalBranchState reactToAllyDeath = new("REACT_TO_ALLY_DEATH");
        scratch.FollowUpState = reactToAllyDeath;
        escape.FollowUpState = escape;
        reactToAllyDeath.AddState(escape, () => _escapeNext);
        reactToAllyDeath.AddState(scratch, () => !_escapeNext);
        // Branch states must be registered too, or the machine throws "no valid state found".
        List<MonsterState> states = [scratch, escape, reactToAllyDeath];
        return new MonsterMoveStateMachine(states, scratch);
    }

    private async Task ScratchMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ScratchDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
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
            "Mad Gremlin",
            [
                ("SCRATCH", "Scratch"),
                ("ESCAPE", "Escape"),
            ]);
}
