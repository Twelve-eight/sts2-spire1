using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-3 boss "Time Eater" (<c>com.megacrit.cardcrawl.monsters.beyond.TimeEater</c>).
/// 官方中文名：时间吞噬者。
/// <para>
/// Bytecode: HP 456, A9 480; REVERB_DMG 7 (A4 8), RIPPLE_BLOCK 20, HEAD_SLAM_DMG 26 (A4 32),
/// plus the A19 tier (Frail on Ripple, 2 Slimed on Head Slam, block on Haste) mapped onto
/// <see cref="AscensionLevel.DoubleBoss"/>. usePreBattleAction applies the custom
/// <see cref="TimeWarpPower"/> (12) — the player's turn is forcibly ended after 12 played cards.
/// </para>
/// <para>
/// getMove: below half HP (integer <c>maxHealth / 2</c>) and Haste unused → HASTE (once);
/// r&lt;45 → REVERBERATE unless last two, else re-roll; 45≤r&lt;80 → HEAD SLAM unless last, else
/// 66% REVERBERATE / 34% RIPPLE; r≥80 → RIPPLE unless last, else re-roll. The two recursive
/// re-rolls fold into weighted branches with identical distributions:
/// random(50,99) = 60/40 band2/band3; random(74) = 60/40 band1/band2.
/// </para>
/// <para>
/// takeTurn: opening turn speaks DIALOG[0] (the Watcher line DIALOG[2] has no StS2 class to map
/// to, so it is omitted). HASTE clears all debuffs (incl. Shackled), heals to half max HP, and
/// gains headSlamDmg block at the top tier. The first-turn talk and HASTE's shout are ported via
/// <see cref="TalkCmd"/> with the vanilla English lines localized in code.
/// </para>
/// <para>
/// Donor: <c>waterfall_giant</c> — a colossal boss-scale creature; closest visual match for the
/// Time Eater's bulk.
/// </para>
/// </summary>
public sealed class TimeEater : Spire1Monster
{
    // HP 456, A9 → 480
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 480, 456);
    public override int MaxInitialHp => MinInitialHp;

    // REVERB_DMG = 7; ascension >= 4 → 8
    private int ReverbDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    // RIPPLE_BLOCK = 20 (no ascension variant)
    private const int RippleBlock = 20;

    // HEAD_SLAM_DMG = 26; ascension >= 4 → 32
    private int HeadSlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 26);

    // HEAD_SLAM_STICKY = 1 (Draw Reduction, no ascension variant)
    private const int HeadSlamDrawReduction = 1;

    // RIPPLE_DEBUFF_TURNS = 1 (Vulnerable + Weak, no ascension variant)
    private const int RippleDebuffTurns = 1;

    // A19: Ripple gains Frail 1; Head Slam adds 2 Slimed; Haste gains headSlamDmg block.
    private const int RippleFrailTurns = 1;
    private const int SlamSlimedCount = 2;

    protected override string DonorId => "waterfall_giant";

    // Vanilla fields: usedHaste, firstTurn.
    private bool _usedHaste;
    private bool _firstTurn = true;

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Time Eater",
        [
            ("REVERB_MOVE", "Reverberate"),
            ("RIPPLE_MOVE", "Ripple"),
            ("SLAM_MOVE", "Head Slam"),
            ("HASTE_MOVE", "Haste"),
        ],
        ("moves.OPENING_LINE", "~You...~ NL ~...came...~"),
        ("moves.HASTE_LINE", "~Foolish...~ NL @How foolish!@"));

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState reverb = new("REVERB_MOVE", ReverbMove, new MultiAttackIntent(ReverbDamage, 3));
        MoveState ripple = new("RIPPLE_MOVE", RippleMove, new DefendIntent(), new DebuffIntent());
        MoveState slam = new("SLAM_MOVE", SlamMove,
            new SingleAttackIntent(HeadSlamDamage), new DebuffIntent());
        MoveState haste = new("HASTE_MOVE", HasteMove, new BuffIntent());

        // Haste fires once at/below half max HP (integer division, like vanilla's maxHealth / 2).
        ConditionalBranchState main = new("TIME_EATER_MAIN");
        reverb.FollowUpState = main;
        ripple.FollowUpState = main;
        slam.FollowUpState = main;
        haste.FollowUpState = main;

        // Band A (r < 45): REVERBERATE unless last two; else re-roll random(50,99) → band2/band3.
        // Band B (45 <= r < 80): HEAD SLAM unless last; else 66% REVERBERATE / 34% RIPPLE.
        // Band C (r >= 80): RIPPLE unless last; else re-roll random(74) → band1/band2.
        RandomBranchState bandA = new("TIME_EATER_BAND_A");
        RandomBranchState bandAReroll = new("TIME_EATER_BAND_A_REROLL");
        RandomBranchState bandB = new("TIME_EATER_BAND_B");
        RandomBranchState bandBCoin = new("TIME_EATER_BAND_B_COIN");
        RandomBranchState bandC = new("TIME_EATER_BAND_C");
        RandomBranchState bandCReroll = new("TIME_EATER_BAND_C_REROLL");
        RandomBranchState bandPicker = new("TIME_EATER_BAND_PICKER");

        bandA.AddBranch(reverb, MoveRepeatType.CanRepeatForever, () => LastTwoWere(reverb) ? 0f : 100f);
        bandA.AddBranch(bandAReroll, MoveRepeatType.CanRepeatForever);
        bandAReroll.AddBranch(bandB, MoveRepeatType.CanRepeatForever, 60f);
        bandAReroll.AddBranch(bandC, MoveRepeatType.CanRepeatForever, 40f);

        bandB.AddBranch(slam, MoveRepeatType.CanRepeatForever, () => LastWas(slam) ? 0f : 100f);
        bandB.AddBranch(bandBCoin, MoveRepeatType.CanRepeatForever);
        bandBCoin.AddBranch(reverb, MoveRepeatType.CanRepeatForever, 66f);
        bandBCoin.AddBranch(ripple, MoveRepeatType.CanRepeatForever, 34f);

        bandC.AddBranch(ripple, MoveRepeatType.CanRepeatForever, () => LastWas(ripple) ? 0f : 100f);
        bandC.AddBranch(bandCReroll, MoveRepeatType.CanRepeatForever);
        bandCReroll.AddBranch(bandA, MoveRepeatType.CanRepeatForever, 60f);
        bandCReroll.AddBranch(bandB, MoveRepeatType.CanRepeatForever, 40f);

        // Band picker: 45 / 35 / 20 (roll 0-99 thresholds 45 and 80).
        bandPicker.AddBranch(bandA, MoveRepeatType.CanRepeatForever, 45f);
        bandPicker.AddBranch(bandB, MoveRepeatType.CanRepeatForever, 35f);
        bandPicker.AddBranch(bandC, MoveRepeatType.CanRepeatForever, 20f);

        main.AddState(haste, () => !_usedHaste && Creature.CurrentHp < Creature.MaxHp / 2);
        main.AddState(bandPicker, () => true);

        return new MonsterMoveStateMachine(
            [reverb, ripple, slam, haste, bandA, bandAReroll, bandB, bandBCoin, bandC, bandCReroll, bandPicker, main],
            main);
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

    private async Task SayOpeningIfFirstTurn()
    {
        if (_firstTurn)
        {
            _firstTurn = false;
            LocString line = MonsterModel.L10NMonsterLookup("SPIRE1-TIME_EATER.moves.OPENING_LINE");
            TalkCmd.Play(line, base.Creature, VfxColor.Purple, VfxDuration.Standard);
        }
        await Task.CompletedTask;
    }

    private async Task ReverbMove(IReadOnlyList<Creature> targets)
    {
        await SayOpeningIfFirstTurn();
        await DamageCmd.Attack(ReverbDamage).WithHitCount(3).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private async Task RippleMove(IReadOnlyList<Creature> targets)
    {
        await SayOpeningIfFirstTurn();
        await CreatureCmd.GainBlock(Creature, RippleBlock, ValueProp.Move, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, RippleDebuffTurns, base.Creature, null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, RippleDebuffTurns, base.Creature, null);
        if (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss))
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, RippleFrailTurns, base.Creature, null);
        }
    }

    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await SayOpeningIfFirstTurn();
        await DamageCmd.Attack(HeadSlamDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<DrawReductionPower>(new ThrowingPlayerChoiceContext(), targets, HeadSlamDrawReduction, base.Creature, null);
        if (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss))
        {
            await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlamSlimedCount, null);
        }
    }

    private async Task HasteMove(IReadOnlyList<Creature> targets)
    {
        _usedHaste = true;
        LocString line = MonsterModel.L10NMonsterLookup("SPIRE1-TIME_EATER.moves.HASTE_LINE");
        TalkCmd.Play(line, base.Creature, VfxColor.Purple, VfxDuration.Standard);
        // RemoveDebuffsAction + RemoveSpecificPowerAction("Shackled"): strip every debuff on self.
        foreach (var power in base.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList())
        {
            await PowerCmd.Remove(power);
        }
        // Heal(maxHealth/2 - currentHealth): up to half max HP.
        int heal = Math.Max(0, Creature.MaxHp / 2 - Creature.CurrentHp);
        if (heal > 0)
        {
            await CreatureCmd.Heal(Creature, heal);
        }
        if (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss))
        {
            await CreatureCmd.GainBlock(Creature, HeadSlamDamage, ValueProp.Move, null);
        }
    }

    public override async Task BeforeCombatStart()
    {
        // usePreBattleAction: playBgmInstantly + markBossAsSeen skipped (StS1-only audio/unlock
        // calls), then ApplyPowerAction(new TimeWarpPower(this)).
        await PowerCmd.Apply<TimeWarpPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, TimeWarpPower.ResetAmount, base.Creature, null);
    }
}
