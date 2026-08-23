using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.MonsterMoves;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-3 elite "Nemesis" (<c>com.megacrit.cardcrawl.monsters.beyond.Nemesis</c>). 官方中文名：天罚。
/// <para>
/// Bytecode: HP 185, A8 200; SCYTHE_DMG 45, FIRE_DMG 6 (A3 7), FIRE_TIMES 3, BURN_AMT 3 (A18 5).
/// Elite type. Every turn ends by granting itself IntangiblePower(1) if absent — the engine's
/// <see cref="IntangiblePower"/> caps incoming damage at 1, exactly like vanilla's <c>damage()</c>
/// override (output &gt; 0 &amp;&amp; hasPower("Intangible") → output = 1).
/// </para>
/// <para>
/// getMove: <c>scytheCooldown--</c> once per roll; first move is roll &lt; 50 → TRI_ATTACK else
/// TRI_BURN. Later bands follow the bytecode: r&lt;30 → SCYTHE if not last and cooldown ≤ 0
/// (cooldown = 2) else 50/50 coin; coin heads → TRI_ATTACK unless last two, tails → TRI_BURN
/// unless last. 30≤r&lt;65 → TRI_ATTACK unless last two; else coin heads → SCYTHE if cooldown ≤ 0
/// else TRI_BURN, tails → TRI_BURN. r≥65 → TRI_BURN unless last; else coin heads → SCYTHE if
/// cooldown ≤ 0 else TRI_ATTACK, tails → TRI_ATTACK. The one roll and one coin flip are drawn once
/// per round (StS1 draws aiRng once per getMove); the cooldown tick is folded into the same latch.
/// </para>
/// <para>
/// Donor: <c>spectral_knight</c> — a ghostly armored knight wielding a greatsword; closest visual
/// match for the wraith-like _scytheState wielder.
/// </para>
/// </summary>
public sealed class Nemesis : Spire1Monster
{
    // HP 185, A8 → 200
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 185);
    public override int MaxInitialHp => MinInitialHp;

    // SCYTHE_DMG = 45 (no ascension variant)
    private const int ScytheDamage = 45;

    // FIRE_DMG = 6; ascension >= 3 → 7
    private int FireDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    // FIRE_TIMES = 3
    private const int FireTimes = 3;

    // BURN_AMT = 3; A18 → 5 (mapped to the topmost available tier)
    private int BurnAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 5, 3);

    protected override string DonorId => "spectral_knight";

    // Vanilla fields: scytheCooldown (starts 0), firstMove (starts true).
    private int _scytheCooldown;
    private MoveState? _scytheState;


    private bool _firstMove = true;

    // One roll + one coin per round (StS1: aiRng.random in getMove + one randomBoolean).
    private int _rollTurn = -1;
    private bool CoinHeads;

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Nemesis",
        [
            ("TRI_ATTACK_MOVE", "Fire x3"),
            ("SCYTHE_MOVE", "Scythe"),
            ("TRI_BURN_MOVE", "Burn"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _scytheState = new("SCYTHE_MOVE", ScytheMove, new SingleAttackIntent(ScytheDamage));
        MoveState triAttack = new("TRI_ATTACK_MOVE", TriAttackMove,
            new MultiAttackIntent(FireDamage, FireTimes));
        MoveState triBurn = new("TRI_BURN_MOVE", TriBurnMove, new DebuffIntent());

        // Opening (first move only): roll < 50 → TRI_ATTACK else TRI_BURN.
        RandomBranchState opening = new("NEMESIS_OPENING");
        opening.AddBranch(triAttack, MoveRepeatType.CanRepeatForever, () => FirstMoveRoll() < 50 ? 100f : 0f);
        opening.AddBranch(triBurn, MoveRepeatType.CanRepeatForever, () => FirstMoveRoll() < 50 ? 0f : 100f);

        // Band A (r < 30): SCYTHE when ready, else coin (heads → TRI_ATTACK unless last two,
        // tails → TRI_BURN unless last).
        ConditionalBranchState bandA = new("NEMESIS_BAND_A");
        RandomBranchState bandACoin = new("NEMESIS_BAND_A_COIN");
        bandA.AddState(_scytheState, () => ScytheReady());
        bandA.AddState(bandACoin, () => true);
        ConditionalBranchState coinHeadsA = new("NEMESIS_BAND_A_HEADS");
        ConditionalBranchState coinTailsA = new("NEMESIS_BAND_A_TAILS");
        bandACoin.AddBranch(coinHeadsA, MoveRepeatType.CanRepeatForever, () => CoinHeads ? 100f : 0f);
        bandACoin.AddBranch(coinTailsA, MoveRepeatType.CanRepeatForever, () => CoinHeads ? 0f : 100f);
        coinHeadsA.AddState(triAttack, () => !LastTwoWere(triAttack));
        coinHeadsA.AddState(triBurn, () => true);
        coinTailsA.AddState(triBurn, () => !LastWas(triBurn));
        coinTailsA.AddState(triAttack, () => true);

        // Band B (30 <= r < 65): TRI_ATTACK unless last two; else coin → SCYTHE if cooldown ≤ 0
        // else TRI_BURN; tails → TRI_BURN.
        ConditionalBranchState bandB = new("NEMESIS_BAND_B");
        RandomBranchState bandBCoin = new("NEMESIS_BAND_B_COIN");
        bandB.AddState(triAttack, () => !LastTwoWere(triAttack));
        bandB.AddState(bandBCoin, () => true);
        ConditionalBranchState coinHeadsB = new("NEMESIS_BAND_B_HEADS");
        bandBCoin.AddBranch(coinHeadsB, MoveRepeatType.CanRepeatForever, () => CoinHeads ? 100f : 0f);
        bandBCoin.AddBranch(triBurn, MoveRepeatType.CanRepeatForever, () => CoinHeads ? 0f : 100f);
        coinHeadsB.AddState(_scytheState, () => _scytheCooldown <= 0);
        coinHeadsB.AddState(triBurn, () => true);

        // Band C (r >= 65): TRI_BURN unless last; else coin → SCYTHE if cooldown ≤ 0 else
        // TRI_ATTACK; tails → TRI_ATTACK.
        ConditionalBranchState bandC = new("NEMESIS_BAND_C");
        RandomBranchState bandCCoin = new("NEMESIS_BAND_C_COIN");
        bandC.AddState(triBurn, () => !LastWas(triBurn));
        bandC.AddState(bandCCoin, () => true);
        ConditionalBranchState coinHeadsC = new("NEMESIS_BAND_C_HEADS");
        bandCCoin.AddBranch(coinHeadsC, MoveRepeatType.CanRepeatForever, () => CoinHeads ? 100f : 0f);
        bandCCoin.AddBranch(triAttack, MoveRepeatType.CanRepeatForever, () => CoinHeads ? 0f : 100f);
        coinHeadsC.AddState(_scytheState, () => _scytheCooldown <= 0);
        coinHeadsC.AddState(triAttack, () => true);

        // Band picker: 30 / 35 / 35 (roll < 30, 30-64, 65+).
        RandomBranchState bandPicker = new("NEMESIS_BAND_PICKER");
        bandPicker.AddBranch(bandA, MoveRepeatType.CanRepeatForever, () => BandRoll() < 30 ? 100f : 0f);
        bandPicker.AddBranch(bandB, MoveRepeatType.CanRepeatForever, () => BandRoll() < 65 ? 100f : 0f);
        bandPicker.AddBranch(bandC, MoveRepeatType.CanRepeatForever);

        // First roll opens with the 50/50, every later roll goes to the band picker.
        ConditionalBranchState main = new("NEMESIS_MAIN");
        triAttack.FollowUpState = main;
        _scytheState.FollowUpState = main;
        triBurn.FollowUpState = main;
        main.AddState(opening, () => _firstMove);
        main.AddState(bandPicker, () => true);

        return new MonsterMoveStateMachine(
            [triAttack, _scytheState, triBurn, opening, bandA, bandACoin, coinHeadsA, coinTailsA,
                bandB, bandBCoin, coinHeadsB, bandC, bandCCoin, coinHeadsC, bandPicker, main],
            main);
    }

    /// <summary>
    /// One cached roll + coin per round, mirroring vanilla's single <c>getMove(int num)</c> draw
    /// and its one <c>randomBoolean()</c> per branch. Also ticks the cooldown once per round
    /// (vanilla decrements it at the top of getMove).
    /// </summary>
    private void TickRound()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_rollTurn != turn)
        {
            _rollTurn = turn;
            _roll = base.Rng.NextInt(100);
            CoinHeads = base.Rng.NextFloat() < 0.5f;
            _scytheCooldown -= 1;
        }
    }

    private int _roll;
    private int FirstMoveRoll() { TickRound(); return _roll; }
    private int BandRoll() { TickRound(); return _roll; }

    private bool ScytheReady()
    {
        TickRound();
        return !LastWas(_scytheState) && _scytheCooldown <= 0;
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

    private async Task TriAttackMove(IReadOnlyList<Creature> targets)
    {
        _firstMove = false;
        await DamageCmd.Attack(FireDamage).WithHitCount(FireTimes).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
        await EnsureIntangible();
    }

    private async Task ScytheMove(IReadOnlyList<Creature> targets)
    {
        _firstMove = false;
        _scytheCooldown = 2;
        await DamageCmd.Attack(ScytheDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await EnsureIntangible();
    }

    private async Task TriBurnMove(IReadOnlyList<Creature> targets)
    {
        _firstMove = false;
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, BurnAmount, null);
        await EnsureIntangible();
    }

    private async Task EnsureIntangible()
    {
        // takeTurn epilogue: if !hasPower("Intangible") → ApplyPower(IntangiblePower, 1).
        if (!base.Creature.HasPower<IntangiblePower>())
        {
            await PowerCmd.Apply<IntangiblePower>(
                new ThrowingPlayerChoiceContext(), base.Creature, 1, base.Creature, null);
        }
    }
}