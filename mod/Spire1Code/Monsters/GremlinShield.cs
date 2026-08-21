using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium "Shield Gremlin" (<c>com.megacrit.cardcrawl.monsters.exordium.GremlinTsundere</c>).
/// <para>
/// Bytecode values: HP 12-15 (A7+: 13-17), Protect block 7 (A7+: 8, A17+: 11), Bash 6 (A2+: 8).
/// Exact <c>takeTurn</c>:
/// <list type="bullet">
/// <item>PROTECT: <c>GainBlockRandomMonsterAction(this, blockAmt)</c>, then count monsters that are
/// neither dying nor escaping; if <c>escapeNext</c> → Escape, else if that count is greater than 1
/// → PROTECT again, else → BASH.</item>
/// <item>BASH: slow attack for <c>bashDmg</c> (AttackEffect.BLUNT_LIGHT); if <c>escapeNext</c> →
/// Escape, else BASH again — it never returns to PROTECT.</item>
/// </list>
/// <c>GainBlockRandomMonsterAction</c> bytecode: the pool is every monster that is not the source,
/// does not intend to Escape and is not dying; only when that pool is empty does the source shield
/// itself. Ally-targeting idiom copied from shipped <c>Guardbot.GuardMove</c>.
/// </para>
/// <para>
/// Ascension mapping: HP and the A7 block bump → <see cref="AscensionLevel.ToughEnemies"/> (A8);
/// Bash damage and the A17 block bump → <see cref="AscensionLevel.DeadlyEnemies"/> (A9). StS2
/// exposes exactly these two enemy-difficulty levels, so all three vanilla block tiers survive.
/// </para>
/// </summary>
public sealed class GremlinShield : Spire1Monster
{
    private bool _escapeNext;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 13, 12);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 17, 15);

    /// <summary>7 base, 8 at the A7 tier, 11 at the A17 tier.</summary>
    private int BlockAmount => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies)
        ? 11
        : AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 7);

    private int BashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    /// <summary>
    /// Borrows the shipped Guardbot scene — the shipped shield-bearer, and the closest match for a
    /// gremlin whose whole job is blocking for allies. Guardbot ships no animator override, so its
    /// rig follows the engine default names (<c>idle_loop</c>/<c>cast</c>/<c>hurt</c>/<c>die</c>,
    /// with <c>Cast</c> proven by <c>Guardbot.GuardMove</c>) and no remap is needed here.
    /// </summary>
    protected override string DonorId => "guardbot";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState protect = new("PROTECT", ProtectMove, new DefendIntent());
        MoveState bash = new("BASH", BashMove, new SingleAttackIntent(BashDamage));
        MoveState escape = new("ESCAPE", EscapeMove, new EscapeIntent());

        // takeTurn/PROTECT tail: escapeNext wins, then the living-monster count decides.
        ConditionalBranchState afterProtect = new("AFTER_PROTECT");
        afterProtect.AddState(escape, () => _escapeNext);
        afterProtect.AddState(protect, () => LivingMonsters() > 1);
        afterProtect.AddState(bash, () => true);

        // takeTurn/BASH tail: escapeNext wins, otherwise Bash repeats forever.
        ConditionalBranchState afterBash = new("AFTER_BASH");
        afterBash.AddState(escape, () => _escapeNext);
        afterBash.AddState(bash, () => true);

        protect.FollowUpState = afterProtect;
        bash.FollowUpState = afterBash;
        escape.FollowUpState = escape;

        // Branch states must be registered too, or FindNextMoveState throws "no valid state found".
        List<MonsterState> states = [protect, bash, escape, afterProtect, afterBash];
        return new MonsterMoveStateMachine(states, protect);
    }

    /// <summary>
    /// StS1 counts monsters that are neither dying nor escaping. <c>CreatureCmd.Escape</c> removes a
    /// creature from <c>CombatState.Enemies</c> outright, and dying creatures fail
    /// <see cref="Creature.IsAlive"/>, so the alive filter covers both StS1 conditions - except for
    /// an ally that has already telegraphed Escape but not yet acted, which is excluded explicitly.
    /// </summary>
    private int LivingMonsters() => CombatState.Enemies.Count(c => c.IsAlive && !IntendsToEscape(c));

    private static bool IntendsToEscape(Creature creature) =>
        creature.Monster?.NextMove.Intents.Any(intent => intent is EscapeIntent) ?? false;

    private async Task ProtectMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.6f);
        // GainBlockRandomMonsterAction: any other non-escaping, non-dying monster; self only as the
        // fallback when that pool is empty. Idiom from shipped Guardbot.GuardMove.
        List<Creature> allies = CombatState.Enemies
            .Where(c => c != Creature && c.IsAlive && !IntendsToEscape(c))
            .ToList();
        Creature target = (allies.Count > 0 ? Rng.NextItem(allies) : null) ?? Creature;
        await CreatureCmd.GainBlock(target, BlockAmount, ValueProp.Unpowered, null);
    }

    private async Task BashMove(IReadOnlyList<Creature> targets)
    {
        // StS1 pairs AnimateSlowAttackAction with AttackEffect.BLUNT_LIGHT.
        await DamageCmd.Attack(BashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
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
            "Shield Gremlin",
            [
                ("PROTECT", "Protect"),
                ("BASH", "Bash"),
                ("ESCAPE", "Escape"),
            ]);
}
