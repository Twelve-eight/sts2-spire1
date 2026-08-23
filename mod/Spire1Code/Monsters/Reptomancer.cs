using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Reptomancer elite (<c>com.megacrit.cardcrawl.monsters.beyond.Reptomancer</c>).
/// 官方中文名：拜蛇术士。
/// <para>
/// Bytecode: HP 180-190, A8 190-200; biteDmg 13 (A3 16), snakeStrikeDmg 30 (A3 34);
/// daggersPerSpawn 1 (A18 2). <c>firstMove</c> starts true. usePreBattleAction applies
/// MinionPower to every non-self monster (cosmetic) and registers the encounter-spawned
/// <see cref="SnakeDagger"/>s into the daggers array slots 0,1.
/// </para>
/// <para>
/// getMove: firstMove → SPAWN_DAGGER; else r&lt;33: last(SNAKE_STRIKE) ? reroll 33-99 (approximated
/// by falling through) : SNAKE_STRIKE; 33&lt;=r&lt;66: lastTwo(SPAWN_DAGGER) ? SNAKE_STRIKE :
/// (canSpawn() ? SPAWN_DAGGER : SNAKE_STRIKE); r&gt;=66: last(BIG_BITE) ? reroll 0-64 (fall
/// through) : BIG_BITE. <c>canSpawn()</c> returns true when ≤3 non-self enemies are alive.
/// Recursive rerolls are approximated by falling through to the next band (Darkling precedent).
/// </para>
/// <para>
/// SNAKE_STRIKE: 2 hits of biteDmg + Weak 1 (fire-like BiteEffect VFX omitted; the "vfx_attack_blunt"
/// hit vfx is a placeholder). SPAWN_DAGGER: spawns up to <c>daggersPerSpawn</c> SnakeDaggers into
/// free slots (4 max). BIG_BITE: single heavy hit of snakeStrikeDmg.
/// die(): every surviving SnakeDagger is killed (vanilla SuicideAction); the boss is the primary
/// enemy so combat ends when it dies, but the kill ensures no lingering rewards.
/// Ascension mapping: A8 HP tier → ToughEnemies, A3 damage tier → DeadlyEnemies, A18 dagger-spawn
/// tier → DoubleBoss (top tier, TheCollector A19 precedent).
/// Donor: <c>entomancer</c> — the shipped caster-summoner with full track set (idle_loop/cast/
/// attack/hurt/die), a perfect match for a robed mage who summons minions.
/// </para>
/// </summary>
public sealed class Reptomancer : Spire1Monster
{
    // setHp(180, 190); ascension >= 8 -> setHp(190, 200)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 180);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 190);

    // biteDmg = 13; ascension >= 3 -> 16
    private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 13);

    // snakeStrikeDmg = 30; ascension >= 3 -> 34
    private int SnakeStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 34, 30);

    // daggersPerSpawn = 1; ascension >= 18 -> 2
    private int DaggersPerSpawn => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 2, 1);

    // Vanilla fields: firstMove, daggers[4] slot array.
    private bool _firstMove = true;

    private readonly Dictionary<int, Creature> _daggerSlots = new();

    protected override string DonorId => "entomancer";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: iterate the room's monsters; assign SnakeDaggers to slots 0,1
        // in encounter order. Vanilla fills daggers[] by index order vs self.
        int slot = 0;
        int selfIndex = -1;
        // Determine self index in the enemy list.
        IReadOnlyList<Creature> enemies = base.CombatState.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (ReferenceEquals(enemies[i], base.Creature))
            {
                selfIndex = i;
                break;
            }
        }
        // Vanilla: daggers after self → daggers[0], before → daggers[1] (usePreBattleAction
        // indexOf comparison).
        for (int i = 0; i < enemies.Count; i++)
        {
            Creature c = enemies[i];
            if (ReferenceEquals(c, base.Creature) || !(c.Monster is SnakeDagger) || !c.IsAlive)
            {
                continue;
            }
            if (i >= selfIndex && slot < 4)
            {
                _daggerSlots[slot++] = c;
            }
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            Creature c = enemies[i];
            if (ReferenceEquals(c, base.Creature) || !(c.Monster is SnakeDagger) || !c.IsAlive)
            {
                continue;
            }
            if (i < selfIndex && slot < 4)
            {
                _daggerSlots[slot++] = c;
            }
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState snakeStrike = new("SNAKE_STRIKE_MOVE", SnakeStrikeMove,
            new MultiAttackIntent(BiteDamage, 2), new DebuffIntent());
        MoveState spawnDagger = new("SPAWN_DAGGER_MOVE", SpawnDaggerMove, new UnknownIntent());
        MoveState bigBite = new("BIG_BITE_MOVE", BigBiteMove, new SingleAttackIntent(SnakeStrikeDamage));

        ConditionalBranchState decide = new("REPTOMANCER_DECIDE");
        snakeStrike.FollowUpState = decide;
        spawnDagger.FollowUpState = decide;
        bigBite.FollowUpState = decide;

        // First move always SPAWN_DAGGER (vanilla firstMove latch).
        decide.AddState(spawnDagger, () => FirstMoveSpawn());
        // r < 33: SNAKE_STRIKE unless last was SNAKE_STRIKE.
        decide.AddState(snakeStrike, () => RollHundred() < 33 && !LastWas(snakeStrike));
        // 33 <= r < 66: SPAWN_DAGGER unless lastTwo were SPAWN_DAGGER or can't spawn.
        decide.AddState(spawnDagger, () => RollHundred() < 66 && !LastTwoWere(spawnDagger) && CanSpawn());
        // r >= 66: BIG_BITE unless last was BIG_BITE (vanilla rerolls 0-64; fall through).
        // The fallback after the last two failed bands lands here.
        decide.AddState(bigBite, () => RollHundred() >= 66 && !LastWas(bigBite));
        // Terminals: never-repeat fallback for the "recursion" cases.
        decide.AddState(snakeStrike, () => !LastWas(snakeStrike));
        decide.AddState(bigBite, () => true);

        return new MonsterMoveStateMachine([snakeStrike, spawnDagger, bigBite, decide], decide);
    }

    private bool FirstMoveSpawn()
    {
        if (!_firstMove)
        {
            return false;
        }
        _firstMove = false;
        return true;
    }

    /// <summary>Vanilla canSpawn(): living non-self monsters > 3 → false.</summary>
    private bool CanSpawn()
    {
        int count = 0;
        foreach (Creature enemy in base.CombatState.Enemies)
        {
            if (!ReferenceEquals(enemy, base.Creature) && enemy.IsAlive)
            {
                count++;
            }
        }
        return count <= 3;
    }

    /// <summary>takeTurn SNAKE_STRIKE: 2 hits of biteDmg + Weak 1 on the player.</summary>
    private async Task SnakeStrikeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.3f);
        await DamageCmd.Attack(BiteDamage).WithHitCount(2).FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, 1, base.Creature, null);
    }

    /// <summary>
    /// takeTurn SPAWN_DAGGER: spawn up to DaggersPerSpawn SnakeDaggers into free slots.
    /// Vanilla iterates daggers[0..3] and spawns where the slot is null or its occupant is dead.
    /// </summary>
    private async Task SpawnDaggerMove(IReadOnlyList<Creature> targets)
    {
        int spawned = 0;
        for (int slot = 0; slot < 4 && spawned < DaggersPerSpawn; slot++)
        {
            if (_daggerSlots.TryGetValue(slot, out Creature? existing) && existing != null && existing.IsAlive)
            {
                continue;
            }
            var dagger = (SnakeDagger)ModelDb.Monster<SnakeDagger>().ToMutable();
            Creature minion = await CreatureCmd.Add(dagger, base.CombatState, base.Creature.Side);
            _daggerSlots[slot] = minion;
            spawned++;
        }
    }

    /// <summary>takeTurn BIG_BITE: single heavy hit of snakeStrikeDmg.</summary>
    private async Task BigBiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SnakeStrikeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>die(): kill every remaining dagger (vanilla SuicideAction on each).</summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature)
        {
            return;
        }
        foreach (Creature survivor in base.CombatState.Enemies
            .Where(c => c != base.Creature && c.IsAlive)
            .ToList())
        {
            await CreatureCmd.Kill(survivor);
        }
    }

    /// <summary>StS1 daggers[] slot clearing: a dead dagger frees its slot.</summary>
    public override Task BeforeDeath(Creature creature)
    {
        if (creature == base.Creature)
        {
            return Task.CompletedTask;
        }
        foreach (int slot in _daggerSlots.Keys.ToList())
        {
            if (ReferenceEquals(_daggerSlots[slot], creature))
            {
                _daggerSlots.Remove(slot);
            }
        }
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
        new MonsterLoc(Tr("Reptomancer", "拜蛇术士"),
        [
            ("SNAKE_STRIKE_MOVE", Tr("Snake Strike", "蛇咬")),
            ("SPAWN_DAGGER_MOVE", Tr("Summon", "召唤")),
            ("BIG_BITE_MOVE", Tr("Bite", "噬咬")),
        ]);
}