using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — JawWorm (<c>com.megacrit.cardcrawl.monsters.exordium.JawWorm</c>).
/// <para>
/// Bytecode: HP 40-44, A2 42-46; CHOMP_DMG 11 (A2 12), THRASH_DMG 7 + THRASH_BLOCK 5,
/// BELLOW_STR 3 (A2 4) + BELLOW_BLOCK 6. First move always Chomp.
/// getMove roll r: r&lt;25: last==Chomp ? 56.25% Bellow / 43.75% Thrash : Chomp;
/// 25&lt;=r&lt;55: lastTwo==Thrash ? 35.7% Chomp / 64.3% Bellow : Thrash;
/// r&gt;=55: last==Bellow ? 41.6% Chomp / 58.4% Thrash : Bellow.
/// </para>
/// <para>
/// The history guards are modelled with conditional branches on
/// <see cref="LastWas"/>/<see cref="LastTwoWere"/> over the state log; the sub-rolls share one
/// cached draw per turn via <see cref="LastSubRoll"/>.
/// </para>
/// </summary>
public sealed class JawWorm : Spire1Monster
{
    // setHp(40, 44); ascension >= 7 -> setHp(42, 46)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 40);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 44);

    // CHOMP_DMG = 11; ascension >= 2 -> 12
    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 11);

    // THRASH_DMG = 7 / THRASH_BLOCK = 5 (no ascension variants)
    private int ThrashDamage => 7;

    private int ThrashBlock => 5;

    // BELLOW_STR = 3; ascension >= 2 -> 4
    private int BellowStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    // BELLOW_BLOCK = 6 (no ascension variant)
    private int BellowBlock => 6;

    protected override string DonorId => "chomper";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState chomp = new("CHOMP_MOVE", ChompMove, new SingleAttackIntent(ChompDamage));
        MoveState bellow = new("BELLOW_MOVE", BellowMove, new DefendIntent(), new BuffIntent());
        MoveState thrash = new("THRASH_MOVE", ThrashMove, new SingleAttackIntent(ThrashDamage), new DefendIntent());

        // Bytecode getMove (jawworm.txt): first move CHOMP, then three roll bands with
        // history-dependent sub-rolls. Modelled as a conditional band picker + weighted
        // sub-branches whose weights zero out when the vanilla guard forbids the move.
        ConditionalBranchState bands = new("JAW_WORM_BANDS");
        chomp.FollowUpState = bands;
        bellow.FollowUpState = bands;
        thrash.FollowUpState = bands;

        // Band A (roll < 25, 25%): last was CHOMP -> 56.25% BELLOW / 43.75% THRASH; else CHOMP.
        ConditionalBranchState bellowBand = new("BELLOW_BAND");
        RandomBranchState afterBellow = new("AFTER_BELLOW");
        bellowBand.AddState(chomp, () => !LastWas(chomp));
        bellowBand.AddState(afterBellow, () => LastWas(chomp));
        afterBellow.AddBranch(bellow, MoveRepeatType.CanRepeatForever, () => LastSubRoll(0.5625f) ? 56.25f : 0f);
        afterBellow.AddBranch(thrash, MoveRepeatType.CanRepeatForever, () => LastSubRoll(0.5625f) ? 0f : 43.75f);

        // Band B (25 <= roll < 55, 30%): THRASH; if last two were THRASH -> 35.7% CHOMP / else BELLOW.
        ConditionalBranchState thrashBand = new("THRASH_BAND");
        RandomBranchState afterThrash = new("AFTER_THRASH");
        thrashBand.AddState(thrash, () => !LastTwoWere(thrash));
        thrashBand.AddState(afterThrash, () => LastTwoWere(thrash));
        afterThrash.AddBranch(chomp, MoveRepeatType.CanRepeatForever, () => LastSubRoll(0.357f) ? 35.7f : 0f);
        afterThrash.AddBranch(bellow, MoveRepeatType.CanRepeatForever, () => LastSubRoll(0.357f) ? 0f : 64.3f);

        // Band C (roll >= 55, 45%): BELLOW; if last was BELLOW -> 41.6% CHOMP / else THRASH.
        ConditionalBranchState chompBand = new("CHOMP_BAND");
        RandomBranchState afterChomp = new("AFTER_CHOMP");
        chompBand.AddState(bellow, () => !LastWas(bellow));
        chompBand.AddState(afterChomp, () => LastWas(bellow));
        afterChomp.AddBranch(chomp, MoveRepeatType.CanRepeatForever, () => LastSubRoll(0.416f) ? 41.6f : 0f);
        afterChomp.AddBranch(thrash, MoveRepeatType.CanRepeatForever, () => LastSubRoll(0.416f) ? 0f : 58.4f);

        // Band selection reproduces the 25/30/45 thresholds via weights on one branch state.
        RandomBranchState bandPicker = new("BAND_PICKER");
        bandPicker.AddBranch(bellowBand, MoveRepeatType.CanRepeatForever, 25f);
        bandPicker.AddBranch(thrashBand, MoveRepeatType.CanRepeatForever, 30f);
        bandPicker.AddBranch(chompBand, MoveRepeatType.CanRepeatForever, 45f);

        bands.AddState(bandPicker, () => true);
        return new MonsterMoveStateMachine(
            new List<MonsterState> { chomp, bellow, thrash, bands, bellowBand, afterBellow,
                thrashBand, afterThrash, chompBand, afterChomp, bandPicker },
            chomp);
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

    // One stable sub-roll per turn: vanilla draws aiRng.randomBoolean(p) inside getMove; we draw
    // once per RollMove and cache it so both complementary weight lambdas see the same value.
    private bool? _subRoll;
    private int _subRollTurn = -1;
    private bool LastSubRoll(float threshold)
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_subRoll == null || _subRollTurn != turn)
        {
            _subRoll = base.Rng.NextFloat() < threshold;
            _subRollTurn = turn;
        }
        return _subRoll.Value;
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task BellowMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, BellowStrength, base.Creature, null);
        await CreatureCmd.GainBlock(base.Creature, BellowBlock, ValueProp.Move, null);
    }

    private async Task ThrashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ThrashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, ThrashBlock, ValueProp.Move, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Jaw Worm",
        [
            ("CHOMP_MOVE", "Chomp"),
            ("BELLOW_MOVE", "Bellow"),
            ("THRASH_MOVE", "Thrash"),
        ]);
}
