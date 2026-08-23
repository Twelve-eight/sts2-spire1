using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — The Collector (<c>com.megacrit.cardcrawl.monsters.city.TheCollector</c>;
/// 官方中文名「收藏家」). Boss encounter.
/// <para>
/// Bytecode: HP 282 (A9+: 300), rakeDmg (Fireball) 18 (A4+: 21), strAmt 3 (A4+: 4, A19+: 5),
/// megaDebuffAmt 3 (A4+: 3, A19+: 5), blockAmt 15 (A9+: 18, A19+: +5).
/// getMove: <c>initialSpawn</c> → SPAWN (1, UNKNOWN); <c>turnsTaken >= 3 &amp;&amp; !ultUsed</c> →
/// MEGA_DEBUFF (4, STRONG_DEBUFF); else a single MonsterAi roll (0-99) gates:
/// ≤25 &amp;&amp; minion dying &amp;&amp; last != REVIVE → REVIVE (5, UNKNOWN);
/// 26-70 &amp;&amp; !lastTwo(FIREBALL) → FIREBALL (2, ATTACK);
/// &gt;70 or lastMove != BUFF → BUFF (3, DEFEND_BUFF); else FIREBALL.
/// takeTurn: SPAWN spawns 2 TorchHead + SFX + enemySlots map; FIREBALL → DamageAction(FIRE);
/// BUFF → GainBlock + StrengthPower on all living monsters; MEGA_DEBUFF → Talk(DIALOG[0]) +
/// Weak/Vulnerable/Frail × megaDebuffAmt; REVIVE → respawn dying TorchHead into their slots.
/// </para>
/// <para>
/// Ascension mapping: vanilla A9 HP/block tier → <see cref="AscensionLevel.ToughEnemies"/>;
/// A4 rakeDmg/strAmt tier → <see cref="AscensionLevel.DeadlyEnemies"/>;
/// A19 strAmt/block/megaDebuff tier → <see cref="AscensionLevel.DoubleBoss"/>.
/// </para>
/// <para>
/// Donor: <c>the_obscura</c> — the shipped occult summoner boss (floating robed figure with
/// glowing eyes, full idle_loop/cast/attack/hurt/die track set, and a Summon trigger). Closest
/// silhouette among the 121 shipped scenes for a soul-collecting robed caster.
/// </para>
/// <para>
/// NOTE: The vanilla SPAWN-first-turn SFX, the MEGA_DEBUFF SFX + CollectorCurseEffect VFX,
/// and the per-frame eye-fire particle emitter are cosmetic and are omitted (consistent with
/// <see cref="Spire1Monster.HasDeathSfx"/> policy). The <c>usePreBattleAction</c> music/bgm
/// and <c>die()</c> minion health-bar cleanup are also not ported — the engine ends combat
/// when the last <c>IsPrimaryEnemy</c> dies (the spawned TorchHeads are not primary), and
/// no modded FMOD event path exists for the collector.
/// </para>
/// <para>
/// The talk line during MEGA_DEBUFF is the canonical StS1 Collector line; the exact text
/// is not bytecode-verifyable from the local workspace (DIALOG[0] comes from the game's
/// localization files). The line is in English only — the StS2 loc system falls back to
/// English for missing zh locale keys.
/// </para>
/// </summary>
public sealed class TheCollector : Spire1Monster
{
    /// <summary>Vanilla HP 282 (A9+ 300).</summary>
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 282);

    public override int MaxInitialHp => MinInitialHp;

    /// <summary>Vanilla Fireball/rakeDmg 18 (A4+ 21).</summary>
    private static int RakeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);

    /// <summary>Vanilla strAmt 3 (A4+ 4, A19+ 5).</summary>
    private static int StrAmt => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 5,
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3));

    /// <summary>Vanilla megaDebuffAmt 3 (A4+ 3, A19+ 5).</summary>
    private static int MegaDebuffAmt => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 5,
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 3));

    /// <summary>Vanilla blockAmt 15 (A9+ 18); A19+ adds 5 (DoubleBoss).</summary>
    private int BlockAmount => (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss) ? 5 : 0)
        + AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 15);

    /// <summary>
    /// Shipped <c>the_obscura</c>: a floating occult summoner with hooded robe and glowing
    /// eyes — the closest silhouette among the shipped StS2 scenes for a soul-collecting
    /// robed caster. The rig has idle_loop/cast/attack/hurt/die; the default engine animator
    /// works without a custom override.
    /// </summary>
    protected override string DonorId => "the_obscura";

    // ── Vanilla fields ──────────────────────────────────────────────────────

    /// <summary>Vanilla <c>enemySlots</c>: slot index → current minion creature.</summary>
    private readonly Dictionary<int, Creature> _minionSlots = new();

    /// <summary>Slots whose occupant is in the dying state (marked by <see cref="BeforeDeath"/>).</summary>
    private readonly HashSet<int> _deadSlots = new();

    /// <summary>
    /// Vanilla <c>ultUsed</c>: the MEGA_DEBUFF once-per-combat latch. Read by the
    /// <c>turnsTaken >= 3 && !ultUsed</c> gate in <c>getMove</c>.
    /// </summary>
    private bool _ultUsed;

    // ── Localization ────────────────────────────────────────────────────────

    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("The Collector", "收藏家"),
        [
            ("SPAWN_MOVE", Tr("Summon", "召唤")),
            ("FIREBALL_MOVE", Tr("Fireball", "火球")),
            ("BUFF_MOVE", Tr("Buff", "增益")),
            ("MEGA_DEBUFF_MOVE", Tr("Mega Debuff", "强效诅咒")),
            ("REVIVE_MOVE", Tr("Revive", "复活")),
        ],
        // Vanilla DIALOG[0] — the canonical Collector line; text not locally
        // verifiable from bytecode, follows established StS1 community knowledge.
        ("moves.MEGA_DEBUFF_MOVE.taunt", "You will make a fine addition to my collection."));

    // ── State machine ───────────────────────────────────────────────────────

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState spawn = new("SPAWN_MOVE", SpawnMove, new UnknownIntent());
        MoveState fireball = new("FIREBALL_MOVE", FireballMove, new SingleAttackIntent(RakeDamage));
        MoveState buff = new("BUFF_MOVE", BuffMove, new DefendIntent(), new BuffIntent());
        MoveState megaDebuff = new("MEGA_DEBUFF_MOVE", MegaDebuffMove, new DebuffIntent(strong: true));
        MoveState revive = new("REVIVE_MOVE", ReviveMove, new UnknownIntent());

        // Vanilla tail: last move was BUFF → force fireball (no double buff).
        ConditionalBranchState buffTail = new("BUFF_TAIL");
        buffTail.AddState(buff, () => !LastWas(buff));
        buffTail.AddState(fireball, () => true);

        // 45% band (26-70): fireball unless the last two moves were both fireballs.
        ConditionalBranchState fireballOrTail = new("FIREBALL_OR_TAIL");
        fireballOrTail.AddState(fireball, () => !LastTwoWere(fireball));
        fireballOrTail.AddState(buffTail, () => true);

        // 25% band (0-25): revive a dying minion, unless the last move was already revive.
        ConditionalBranchState reviveGate = new("REVIVE_GATE");
        reviveGate.AddState(revive, () => AnyMinionDying() && !LastWas(revive));
        reviveGate.AddState(fireballOrTail, () => true);

        // Single MonsterAi roll per turn, split 25/45/30 across the three vanilla bands.
        RandomBranchState roll = new("COLLECTOR_ROLL");
        roll.AddBranch(reviveGate, MoveRepeatType.CanRepeatForever, 26f);
        roll.AddBranch(fireballOrTail, MoveRepeatType.CanRepeatForever, 45f);
        roll.AddBranch(buffTail, MoveRepeatType.CanRepeatForever, 29f);

        // Priority: the forced mega-debuff on turn 4 (turnsTaken >= 3) beats the roll.
        // StateLog.Count == number of completed turns at decision time.
        ConditionalBranchState decide = new("COLLECTOR_DECIDE");
        decide.AddState(megaDebuff, () => TurnCount >= 3 && !_ultUsed);
        decide.AddState(roll, () => true);

        spawn.FollowUpState = decide;
        fireball.FollowUpState = decide;
        buff.FollowUpState = decide;
        megaDebuff.FollowUpState = decide;
        revive.FollowUpState = decide;

        List<MonsterState> states = [spawn, fireball, buff, megaDebuff, revive, buffTail, fireballOrTail, reviveGate, roll, decide];
        return new MonsterMoveStateMachine(states, spawn);
    }

    // ── Turn / history helpers ─────────────────────────────────────────────

    /// <summary>Vanilla <c>turnsTaken</c>: number of completed turns.</summary>
    private int TurnCount => base.MoveStateMachine.StateLog.Count;

    /// <summary>Vanilla <c>lastMove(byte)</c>: the most recent executed move.</summary>
    private bool LastWas(MoveState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    /// <summary>Vanilla <c>lastTwoMoves(byte)</c>: the two most recent moves were both <paramref name="state"/>.</summary>
    private bool LastTwoWere(MoveState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }

    /// <summary>Vanilla <c>isMinionDead()</c>: any tracked minion slot is dying.</summary>
    private bool AnyMinionDying() => _deadSlots.Count > 0;

    // ── Move bodies ─────────────────────────────────────────────────────────

    /// <summary>
    /// SPAWN (byte 1, UNKNOWN intent): spawn 2 TorchHeads at indexed slots,
    /// matching vanilla's loop i=1..2.
    /// </summary>
    private async Task SpawnMove(IReadOnlyList<Creature> targets)
    {
        for (int slot = 1; slot <= 2; slot++)
        {
            var torchHead = (TorchHead)ModelDb.Monster<TorchHead>().ToMutable();
            Creature minion = await CreatureCmd.Add(torchHead, CombatState, Creature.Side);
            _minionSlots[slot] = minion;
        }
    }

    /// <summary>
    /// FIREBALL (byte 2, ATTACK intent): single hit with <c>AttackEffect.FIRE</c>.
    /// Vanilla uses <c>damage[0]</c> = rakeDmg; the "vfx/vfx_attack_blunt" hit vfx is
    /// the closest shipped generic impact (no fire-specific vfx exists in the engine).
    /// </summary>
    private async Task FireballMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RakeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>
    /// BUFF (byte 3, DEFEND_BUFF intent): <c>GainBlockAction(blockAmt)</c> then
    /// <c>StrengthPower(strAmt)</c> on every living monster (including self).
    /// A19+ variant adds 5 to blockAmt via <see cref="AscensionLevel.DoubleBoss"/>.
    /// </summary>
    private async Task BuffMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.6f);
        foreach (Creature ally in CombatState.Enemies.Where(c => c.IsAlive))
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), ally, StrAmt, Creature, null);
        }
    }

    /// <summary>
    /// MEGA_DEBUFF (byte 4, STRONG_DEBUFF intent): <c>TalkAction(DIALOG[0])</c>,
    /// <c>CollectorCurseEffect</c> VFX, then <c>WeakPower</c> + <c>VulnerablePower</c> +
    /// <c>FrailPower</c> each for <c>megaDebuffAmt</c> turns. <c>ultUsed</c> latch
    /// prevents a second use.
    /// </summary>
    private async Task MegaDebuffMove(IReadOnlyList<Creature> targets)
    {
        LocString line = MonsterModel.L10NMonsterLookup("SPIRE1-THE_COLLECTOR.moves.MEGA_DEBUFF_MOVE.taunt");
        TalkCmd.Play(line, Creature, VfxColor.Purple, VfxDuration.Standard);
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.6f);
        // Apply all three debuffs (vanilla applies Weak + Vulnerable + Frail). The order
        // matches the bytecode: Weak first, then Vulnerable, then Frail.
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, MegaDebuffAmt, Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, MegaDebuffAmt, Creature, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, MegaDebuffAmt, Creature, null);
        _ultUsed = true;
    }

    /// <summary>
    /// REVIVE (byte 5, UNKNOWN intent): iterate tracked minion slots; for each slot whose
    /// occupant is dying, spawn a fresh TorchHead and update the map (vanilla loop over
    /// <c>enemySlots.entrySet()</c> respawning <c>isDying</c> entries).
    /// </summary>
    private async Task ReviveMove(IReadOnlyList<Creature> targets)
    {
        foreach (int slot in _deadSlots.ToList())
        {
            var torchHead = (TorchHead)ModelDb.Monster<TorchHead>().ToMutable();
            Creature minion = await CreatureCmd.Add(torchHead, CombatState, Creature.Side);
            _minionSlots[slot] = minion;
            _deadSlots.Remove(slot);
        }
    }

    // ── Hooks ───────────────────────────────────────────────────────────────

    /// <summary>
    /// StS1 <c>isMinionDead()</c> detection: watch for ally deaths via
    /// <see cref="BeforeDeath"/>. When a tracked minion enters the dying state,
    /// its slot is recorded in <c>_deadSlots</c>. REVIVE reads and clears those slots.
    /// </summary>
    public override Task BeforeDeath(Creature creature)
    {
        if (creature == Creature || !creature.IsEnemy)
            return Task.CompletedTask;
        foreach (var (slot, minion) in _minionSlots)
        {
            if (ReferenceEquals(minion, creature))
            {
                _deadSlots.Add(slot);
            }
        }
        return Task.CompletedTask;
    }
}