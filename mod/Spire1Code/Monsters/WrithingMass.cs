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
        MoveState bigHit = new("BIG_HIT_MOVE", BigHitMove, new SingleAttackIntent(BigHitDamage));
        MoveState multiHit = new("MULTI_HIT_MOVE", MultiHitMove, new MultiAttackIntent(MultiHitDamage, 3));
        MoveState attackBlock = new("ATTACK_BLOCK_MOVE", AttackBlockMove, new SingleAttackIntent(AttackBlockDamage), new DefendIntent());
        MoveState attackDebuff = new("ATTACK_DEBUFF_MOVE", AttackDebuffMove, new SingleAttackIntent(AttackDebuffDamage), new DebuffIntent());
        MoveState megaDebuff = new("MEGA_DEBUFF_MOVE", MegaDebuffMove, new DebuffIntent());

        ConditionalBranchState firstBands = new("WRITHING_FIRST");
        ConditionalBranchState bands = new("WRITHING_BANDS");
        // Reroll fallthrough targets: the bytecode re-enters getMove with a fresh restricted
        // roll; each Reroll branch re-enters "bands" with the current roll replaced.
        ConditionalBranchState reroll10_99 = new("WRITHING_REROLL_10_99");
        ConditionalBranchState reroll20_99 = new("WRITHING_REROLL_20_99");
        ConditionalBranchState reroll40_99 = new("WRITHING_REROLL_40_99");
        ConditionalBranchState reroll0_19 = new("WRITHING_REROLL_0_19");
        ConditionalBranchState reroll0_39 = new("WRITHING_REROLL_0_39");
        ConditionalBranchState reroll0_69 = new("WRITHING_REROLL_0_69");
        // Each reroll re-enters bands with the fresh restricted roll (bytecode re-invokes
        // getMove); without an exit ConditionalBranchState throws "No valid next state".
        reroll10_99.AddState(bands, () => true);
        reroll20_99.AddState(bands, () => true);
        reroll40_99.AddState(bands, () => true);
        reroll0_19.AddState(bands, () => true);
        reroll0_39.AddState(bands, () => true);
        reroll0_69.AddState(bands, () => true);

        bigHit.FollowUpState = bands;
        multiHit.FollowUpState = bands;
        attackBlock.FollowUpState = bands;
        attackDebuff.FollowUpState = bands;
        megaDebuff.FollowUpState = bands;
        // Opening (vanilla firstMove latch): r<33 MULTI_HIT x3, r<66 ATTACK_BLOCK, else
        // ATTACK_DEBUFF — MEGA_DEBUFF can never open.
        firstBands.AddState(multiHit, () => FirstMoveRoll(33));
        firstBands.AddState(attackBlock, () => FirstMoveRoll(66));
        firstBands.AddState(attackDebuff, () => ConsumeFirstMove());
        firstBands.AddState(bands, () => true);

        // roll < 10: BIG_HIT unless last was BIG_HIT, then reroll 10-99.
        bands.AddState(bigHit, () => CurrentRoll() < 10 && !LastWas(bigHit));
        bands.AddState(reroll10_99, () => CurrentRoll() < 10 && Reroll(10, 99));
        // roll < 20: MEGA_DEBUFF once (unless it was the last move); else 10% BIG_HIT (no
        // history guard in vanilla), 90% reroll 20-99.
        bands.AddState(megaDebuff, () => CurrentRoll() < 20 && !_usedMegaDebuff && !LastWas(megaDebuff));
        bands.AddState(bigHit, () => CurrentRoll() < 20 && SubRoll(0.1f));
        bands.AddState(reroll20_99, () => CurrentRoll() < 20 && Reroll(20, 99));
        // roll < 40: ATTACK_DEBUFF unless last was ATTACK_DEBUFF; else 40% reroll 0-19,
        // 60% reroll 40-99.
        bands.AddState(attackDebuff, () => CurrentRoll() < 40 && !LastWas(attackDebuff));
        bands.AddState(reroll0_19, () => CurrentRoll() < 40 && SubRoll(0.4f) && Reroll(0, 19));
        bands.AddState(reroll40_99, () => CurrentRoll() < 40 && Reroll(40, 99));
        // roll < 70: MULTI_HIT x3 unless last was MULTI_HIT; else 30% ATTACK_BLOCK (no guard
        // in vanilla), 70% reroll 0-39.
        bands.AddState(multiHit, () => CurrentRoll() < 70 && !LastWas(multiHit));
        bands.AddState(attackBlock, () => CurrentRoll() < 70 && SubRoll(0.3f));
        bands.AddState(reroll0_39, () => CurrentRoll() < 70 && Reroll(0, 39));
        // roll >= 70: ATTACK_BLOCK unless last was ATTACK_BLOCK; else reroll 0-69.
        bands.AddState(attackBlock, () => !LastWas(attackBlock));
        bands.AddState(reroll0_69, () => Reroll(0, 69));

        return new MonsterMoveStateMachine(
            [bigHit, multiHit, attackBlock, attackDebuff, megaDebuff,
                firstBands, bands, reroll10_99, reroll20_99, reroll40_99, reroll0_19, reroll0_39, reroll0_69],
            firstBands);
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

    private bool FirstMoveRoll(int threshold)
    {
        if (!_firstMove)
        {
            return false;
        }
        if (CurrentRoll() >= threshold)
        {
            return false;
        }
        _firstMove = false;
        return true;
    }

    private bool ConsumeFirstMove()
    {
        if (!_firstMove)
        {
            return false;
        }
        _firstMove = false;
        return true;
    }

    // The "current" roll of this getMove chain. Drawn once per round on first use; the
    // bytecode re-rolls inside recursion, which Reroll replaces in place.
    private int _roll = -1;
    private int _rollTurn = -1;
    private int CurrentRoll()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_rollTurn != turn)
        {
            _roll = base.Rng.NextInt(100);
            _rollTurn = turn;
        }
        return _roll;
    }

    private bool Reroll(int minInclusive, int maxInclusive)
    {
        _roll = minInclusive + base.Rng.NextInt(maxInclusive - minInclusive + 1);
        return true;
    }

    private bool SubRoll(float threshold) => base.Rng.NextFloat() < threshold;

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
