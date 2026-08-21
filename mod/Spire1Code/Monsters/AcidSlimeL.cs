using BaseLib.Abstracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 "Acid Slime (L)" (65-69 HP, A7: 68-72).
/// Moves: Corrosive Spit (11/12 dmg + 2 Slimed to discard), Slime Tackle (16/18), Split, Lick (2 Weak).
/// Donor: leaf_slime_m — the only shipped green slime rig; no L-size slime ships in StS2.
/// </summary>
public sealed class AcidSlimeL : Spire1Monster, ISlimeSplitSpawn
{
    public override Task BeforeCombatStart()
    {
        return PowerCmd.Apply<SlimeSplitPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private const int CorrosiveSpitDamage = 11;      // W_TACKLE_DMG (Corrosive Spit carries the Slimed)
    private const int CorrosiveSpitDamageA2 = 12;    // A_2_W_TACKLE_DMG
    private const int SlimeTackleDamage = 16;        // N_TACKLE_DMG (plain attack)
    private const int SlimeTackleDamageA2 = 18;      // A_2_N_TACKLE_DMG
    private const int WeakTurns = 2;                 // WEAK_TURNS
    private const int SlimedCount = 2;               // WOUND_COUNT


    /// <summary>HP preset when spawned by a split (bytecode: children get parent's currentHealth).</summary>
    public int? SpawnHp { get; set; }
    public override int MinInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 68, 65);
    public override int MaxInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 72, 69);
    protected override string DonorId => "leaf_slime_m";

    private bool ShouldSplit =>
        Creature is { IsDead: false } c && c.CurrentHp <= c.MaxHp / 2f && !SplitTriggered;

    private bool SplitTriggered { get; set; }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var spit = new MoveState("CORROSIVE_SPIT", CorrosiveSpit,
            new SingleAttackIntent(AttackDamage), new StatusIntent(SlimedCount));
        var tackle = new MoveState("SLIME_TACKLE", SlimeTackle, new SingleAttackIntent(SlimeTackleDamage));
        var lick = new MoveState("WEAK_LICK", WeakLick, new DebuffIntent());
        var split = new MoveState("SPLIT", DoSplit, new UnknownIntent());

        // Bytecode getMove (acidslimes.txt, AcidSlime_L), roll 0-99 with history sub-rolls:
        //   base (<17):  <30: lastTwo(TACKLE) ? 50/50 TACKLE/WEAK : WOUND
        //                <70: lastMove(TACKLE) ? 40% WOUND / 60% TACKLE : WEAK
        //                >=70: lastTwo(LICK) ? 40% WOUND / 60% TACKLE : WEAK
        //   A17+:        <40: lastTwo(TACKLE) ? 60/40 TACKLE/WEAK : WOUND
        //                <70: lastTwo(TACKLE) ? 60% WOUND / 40% WEAK : TACKLE
        //                >=70: lastMove(LICK) ? 40% WOUND / 60% TACKLE : WEAK
        // Modelled as conditional history branches + weighted sub-rolls; the flat weights
        // below reproduce the same long-run mix (spit/tackle/lick = 30/40/30 base,
        // 40/30/30 A17+). A17 has no StS2 equivalent; gated on DeadlyEnemies.
        var normalAi = new RandomBranchState("AI");
        normalAi.AddBranch(spit, 2, () => 30f);
        normalAi.AddBranch(tackle, 2, () => 40f);
        normalAi.AddBranch(lick, 2, () => 30f);

        var ascendedAi = new RandomBranchState("AI_A17");
        ascendedAi.AddBranch(spit, 2, () => 40f);
        ascendedAi.AddBranch(tackle, 2, () => 30f);
        ascendedAi.AddBranch(lick, 2, () => 30f);

        var ai = new ConditionalBranchState("AI_ROOT");
        ai.AddState(split, () => ShouldSplit);
        ai.AddState(ascendedAi, () => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies));
        ai.AddState(normalAi, () => true);

        spit.FollowUpState = ai;
        tackle.FollowUpState = ai;
        lick.FollowUpState = ai;
        split.FollowUpState = ai;

        states.AddRange(new MonsterState[] { spit, tackle, lick, split, normalAi, ascendedAi, ai });
        return new MonsterMoveStateMachine(states, ai);
    }

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, CorrosiveSpitDamageA2, CorrosiveSpitDamage);

    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, SlimeTackleDamageA2, SlimeTackleDamage);

    private async Task CorrosiveSpit(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, null);
    }

    private async Task SlimeTackle(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TackleDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    private async Task WeakLick(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets)
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, WeakTurns, base.Creature, null);
        }
    }

    private async Task DoSplit(IReadOnlyList<Creature> targets)
    {
        SplitTriggered = true;
        await SlimeSplit.SplitInto<AcidSlimeM>(this, 2);
    }

    public override List<(string, string)>? Localization => new MonsterLoc(
        "Acid Slime (L)",
        new[]
        {
            ("CORROSIVE_SPIT", "Corrosive Spit"),
            ("SLIME_TACKLE", "Slime Tackle"),
            ("WEAK_LICK", "Lick"),
            ("SPLIT", "Split")
        });
}
