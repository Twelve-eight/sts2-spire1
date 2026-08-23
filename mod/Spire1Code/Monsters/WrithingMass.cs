using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Writhing Mass (<c>com.megacrit.cardcrawl.monsters.beyond.WrithingMass</c>).
/// 官方中文名：扭曲团块。
/// <para>
/// Bytecode: HP 160, A7 175; BIG_HIT 32 (A2 38), MULTI_HIT 7 x3 (A2 9), ATTACK_BLOCK 15 dmg +
/// 15 block (A2 16), ATTACK_DEBUFF 10 dmg + Weak 2 + Vulnerable 2 (A2 12), normalDebuffAmt 2;
/// usePreBattleAction applies ReactivePower + MalleablePower.
/// </para>
/// <para>
/// FLAGGED: vanilla MalleablePower (gain block when attacked, growing per proc, resetting at
/// turn start) has no shipped StS2 equivalent — same gap SnakePlant.cs documents; the closest
/// shipped defensive-on-hit behaviour in this mod's scope is <see cref="MetallicizePower"/>,
/// applied at the vanilla stack count (3). Vanilla ReactivePower (change intent when attacked)
/// is likewise unmodelled: the shipped engine keeps intent and performed move in one state
/// machine, and re-rolling from inside a damage hook would corrupt the move log the AI guards
/// read. Both substitutions are flagged rather than shipped as behaviour.
/// </para>
/// <para>
/// getMove is recursive (bands re-roll restricted ranges); each recursion draws a fresh
/// aiRng value, so this port keeps one <em>current</em> roll that <see cref="Reroll"/>
/// replaces exactly when the bytecode re-rolls. The sub-rolls (randomBoolean(0.1)/(0.4)/(0.3))
/// draw fresh values per evaluation like vanilla. First move: r&lt;33 MULTI_HIT x3, r&lt;66
/// ATTACK_BLOCK, else ATTACK_DEBUFF (MEGA_DEBUFF is never the opening move).
/// </para>
/// <para>
/// takeTurn: BIG_HIT = 1 hit (SLASH_HEAVY); MULTI_HIT = 3 hits (BLUNT_LIGHT);
/// ATTACK_BLOCK = 1 hit (BLUNT_HEAVY) + block equal to the damage;
/// ATTACK_DEBUFF = 1 hit (BLUNT_HEAVY) + Weak 2 + Vulnerable 2;
/// MEGA_DEBUFF = add a Parasite curse to the player's deck (AddCardToDeckAction).
/// </para>
/// <para>
/// Donor: <c>slithering_strangler</c> — a coiled tentacle-like creature; closest visual
/// match for a mass of writhing appendages.
/// </para>
/// </summary>
public sealed class WrithingMass : Spire1Monster
{
    // setHp(160); ascension >= 7 -> setHp(175) — fixed single value per tier.
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 160);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 160);

    // BIG_HIT = 32; ascension >= 2 -> 38
    private int BigHitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 32);

    // MULTI_HIT = 7 x3; ascension >= 2 -> 9
    private int MultiHitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);

    // ATTACK_BLOCK dmg/block = 15; ascension >= 2 -> 16
    private int AttackBlockDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

    // ATTACK_DEBUFF dmg = 10; ascension >= 2 -> 12
    private int AttackDebuffDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    // normalDebuffAmt = 2 (both tiers)
    private const int NormalDebuffAmount = 2;

    // Vanilla fields: firstMove, usedMegaDebuff.
    private bool _firstMove = true;

    private bool _usedMegaDebuff;

    /// <summary>Borrows the shipped slithering_strangler scene.</summary>
    protected override string DonorId => "slithering_strangler";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ReactivePower + MalleablePower — see FLAG note in class remarks.
        await PowerCmd.Apply<MetallicizePower>(new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        BigHitState = new("BIG_HIT_MOVE", BigHitMove, new SingleAttackIntent(BigHitDamage));
        MultiHitState = new("MULTI_HIT_MOVE", MultiHitMove, new MultiAttackIntent(MultiHitDamage, 3));
        AttackBlockState = new("ATTACK_BLOCK_MOVE", AttackBlockMove, new SingleAttackIntent(AttackBlockDamage), new DefendIntent());
        AttackDebuffState = new("ATTACK_DEBUFF_MOVE", AttackDebuffMove, new SingleAttackIntent(AttackDebuffDamage), new DebuffIntent());
        MegaDebuffState = new("MEGA_DEBUFF_MOVE", MegaDebuffMove, new DebuffIntent());

        // The bytecode resolves a move through a potentially self-reentering getMove chain
        // (restricted re-rolls re-enter the band ladder with a fresh roll). Modelling that
        // re-entry as graph edges yields intent-less conditional cycles (e.g.
        // WRITHING_REROLL_0_39 <-> WRITHING_REROLL_40_99) that native graph consumers
        // traverse without cycle protection — observed as a deterministic fatal native
        // crash (exit 0x7FFFFFFF) right after "[IntentGraph] Generating intent graph".
        // Fix: resolve eagerly in code (identical RNG draw order, see ResolveBands) so the
        // static machine is root -> 5 moves -> root; every cycle passes through a real
        // move/intent node, matching shipped-monster topology.
        ConditionalBranchState root = new("WRITHING_RESOLVE");
        root.AddState(BigHitState, () => ReferenceEquals(ResolveNext(), BigHitState));
        root.AddState(MultiHitState, () => ReferenceEquals(ResolveNext(), MultiHitState));
        root.AddState(AttackBlockState, () => ReferenceEquals(ResolveNext(), AttackBlockState));
        root.AddState(AttackDebuffState, () => ReferenceEquals(ResolveNext(), AttackDebuffState));
        root.AddState(MegaDebuffState, () => ReferenceEquals(ResolveNext(), MegaDebuffState));

        BigHitState.FollowUpState = root;
        MultiHitState.FollowUpState = root;
        AttackBlockState.FollowUpState = root;
        AttackDebuffState.FollowUpState = root;
        MegaDebuffState.FollowUpState = root;

        return new MonsterMoveStateMachine(
            [BigHitState, MultiHitState, AttackBlockState, AttackDebuffState, MegaDebuffState, root],
            root);
    }

    private async Task BigHitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BigHitDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task MultiHitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MultiHitDamage).WithHitCount(3).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task AttackBlockMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackBlockDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, AttackBlockDamage, ValueProp.Move, null);
    }

    private async Task AttackDebuffMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDebuffDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, NormalDebuffAmount, base.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, NormalDebuffAmount, base.Creature, null);
    }

    private async Task MegaDebuffMove(IReadOnlyList<Creature> targets)
    {
        _usedMegaDebuff = true;
        Player? player = targets.FirstOrDefault()?.Player;
        if (player != null)
        {
            // AddCardToDeckAction(CardLibrary.getCard("Parasite")) — permanent curse.
            await CardPileCmd.AddCurseToDeck<Parasite>(player);
        }
    }

    // Resolved move states, assigned in GenerateMoveStateMachine; the resolvers below need
    // them outside the factory's local scope.
    private MoveState BigHitState = null!;

    private MoveState MultiHitState = null!;

    private MoveState AttackBlockState = null!;

    private MoveState AttackDebuffState = null!;

    private MoveState MegaDebuffState = null!;

    private MonsterState? _resolved;

    private int _resolvedRound = -1;

    /// <summary>One resolution per round, cached — the root band's predicates consult it repeatedly.</summary>
    private MonsterState ResolveNext()
    {
        int round = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_resolvedRound != round || _resolved == null)
        {
            _resolved = _firstMove ? ResolveFirst(base.Rng.NextInt(100)) : ResolveBands(base.Rng.NextInt(100));
            _resolvedRound = round;
        }
        return _resolved;
    }

    // Opening (vanilla firstMove latch): r<33 MULTI_HIT x3, r<66 ATTACK_BLOCK, else
    // ATTACK_DEBUFF — MEGA_DEBUFF can never open. Any first resolution consumes the latch.
    private MonsterState ResolveFirst(int roll)
    {
        _firstMove = false;
        if (roll < 33)
        {
            return MultiHitState;
        }
        if (roll < 66)
        {
            return AttackBlockState;
        }
        return AttackDebuffState;
    }

    // Band ladder, byte-faithful with the EXACT RNG draw order of the lazy-predicate version
    // it replaced: each restricted re-roll draws one int in its vanilla range, each sub-roll
    // one float, evaluated in bytecode band order.
    private MonsterState ResolveBands(int roll)
    {
        if (roll < 10)
        {
            if (!LastWas(BigHitState))
            {
                return BigHitState;
            }
            return ResolveBands(10 + base.Rng.NextInt(90)); // reroll 10-99
        }
        if (roll < 20)
        {
            if (!_usedMegaDebuff && !LastWas(MegaDebuffState))
            {
                return MegaDebuffState;
            }
            if (base.Rng.NextFloat() < 0.1f) // 10% BIG_HIT, no history guard in vanilla
            {
                return BigHitState;
            }
            return ResolveBands(20 + base.Rng.NextInt(80)); // reroll 20-99
        }
        if (roll < 40)
        {
            if (!LastWas(AttackDebuffState))
            {
                return AttackDebuffState;
            }
            if (base.Rng.NextFloat() < 0.4f)
            {
                return ResolveBands(base.Rng.NextInt(20)); // reroll 0-19
            }
            return ResolveBands(40 + base.Rng.NextInt(60)); // reroll 40-99
        }
        if (roll < 70)
        {
            if (!LastWas(MultiHitState))
            {
                return MultiHitState;
            }
            if (base.Rng.NextFloat() < 0.3f) // 30% ATTACK_BLOCK, no guard in vanilla
            {
                return AttackBlockState;
            }
            return ResolveBands(base.Rng.NextInt(40)); // reroll 0-39
        }
        if (!LastWas(AttackBlockState))
        {
            return AttackBlockState;
        }
        return ResolveBands(base.Rng.NextInt(70)); // reroll 0-69
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move titles
    // follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Writhing Mass", "扭曲团块"),
        [
            ("BIG_HIT_MOVE", Tr("Big Hit", "重击")),
            ("MULTI_HIT_MOVE", Tr("Multi Hit", "连击")),
            ("ATTACK_BLOCK_MOVE", Tr("Attack & Block", "攻防一体")),
            ("ATTACK_DEBUFF_MOVE", Tr("Attack & Debuff", "攻击削弱")),
            ("MEGA_DEBUFF_MOVE", Tr("Parasite", "寄生")),
        ]);
}
