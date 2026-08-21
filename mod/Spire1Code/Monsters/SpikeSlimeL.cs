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
/// StS1 "Spike Slime (L)" (64-70 HP, A7: 67-73).
/// Moves: Flame Tackle (16/18 dmg + 2 Slimed to discard), Split, Lick (2 Frail).
/// Donor: twig_slime_m — the only shipped brown/spiky slime rig; no L-size slime ships in StS2.
/// </summary>
public sealed class SpikeSlimeL : Spire1Monster, ISlimeSplitSpawn
{
    private const int FlameTackleDamage = 16;      // TACKLE_DAMAGE
    private const int FlameTackleDamageA2 = 18;    // A_2_TACKLE_DAMAGE
    private int FrailTurns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2); // FRAIL_TURNS (A17+: 3)
    private const int SlimedCount = 2;             // WOUND_COUNT


    /// <summary>HP preset when spawned by a split (bytecode: children get parent's currentHealth).</summary>
    public int? SpawnHp { get; set; }

    protected override string DonorId => "twig_slime_m";
    public override int MinInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 67, 64);
    public override int MaxInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 73, 70);
    public override Task BeforeCombatStart()
    {
        return PowerCmd.Apply<SlimeSplitPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private bool ShouldSplit =>
        Creature is { IsDead: false } c && c.CurrentHp <= c.MaxHp / 2f && !SplitTriggered;

    private bool SplitTriggered { get; set; }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackle = new MoveState("FLAME_TACKLE", FlameTackle,
            new SingleAttackIntent(AttackDamage), new StatusIntent(SlimedCount));
        var lick = new MoveState("FRAIL_LICK", FrailLick, new DebuffIntent());
        var split = new MoveState("SPLIT", DoSplit, new UnknownIntent());

        // Vanilla getMove (roll 0-99):
        //   base (<17): <30: lastTwo(TACKLE) ? FRAIL : TACKLE; >=30: lastTwo(LICK) ? TACKLE : FRAIL
        //   A17+:       <30: lastTwo(TACKLE) ? FRAIL : TACKLE; >=30: lastMove(LICK) ? TACKLE : FRAIL
        // => base tackle max2 / lick max2; A17+ tackle max2 / lick max1
        // A17 has no StS2 equivalent; gated on DeadlyEnemies as the nearest higher-difficulty tier.
        var normalAi = new RandomBranchState("AI");
        normalAi.AddBranch(tackle, 2, () => 30f);
        normalAi.AddBranch(lick, 2, () => 70f);

        var ascendedAi = new RandomBranchState("AI_A17");
        ascendedAi.AddBranch(tackle, 2, () => 30f);
        ascendedAi.AddBranch(lick, 1, () => 70f);

        var ai = new ConditionalBranchState("AI_ROOT");
        ai.AddState(split, () => ShouldSplit);
        ai.AddState(ascendedAi, () => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies));
        ai.AddState(normalAi, () => true);

        tackle.FollowUpState = ai;
        lick.FollowUpState = ai;

        split.FollowUpState = ai;

        states.AddRange(new MonsterState[] { tackle, lick, split, normalAi, ascendedAi, ai });
        return new MonsterMoveStateMachine(states, ai);
    }

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, FlameTackleDamageA2, FlameTackleDamage);

    private async Task FlameTackle(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, null);
    }

    private async Task FrailLick(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets)
        {
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, FrailTurns, base.Creature, null);
        }
    }

    private async Task DoSplit(IReadOnlyList<Creature> targets)
    {
        SplitTriggered = true;
        await SlimeSplit.SplitInto<SpikeSlimeM>(this, 2);
    }

    public override List<(string, string)>? Localization => new MonsterLoc(
        "Spike Slime (L)",
        new[]
        {
            ("FLAME_TACKLE", "Flame Tackle"),
            ("FRAIL_LICK", "Lick"),
            ("SPLIT", "Split")
        });
}
