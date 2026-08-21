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

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 "Acid Slime (M)" (28-32 HP, A7: 29-34). Does NOT split in vanilla.
/// Moves: Corrosive Spit (7/8 dmg + 1 Slimed to discard), Slime Tackle (10/12), Lick (1 Weak).
/// Donor: leaf_slime_m — the only shipped green slime rig; no L-size slime ships in StS2.
/// </summary>
public sealed class AcidSlimeM : Spire1Monster, ISlimeSplitSpawn
{
    private const int CorrosiveSpitDamage = 7;       // W_TACKLE_DMG (Corrosive Spit carries the Slimed)
    private const int CorrosiveSpitDamageA2 = 8;     // A_2_W_TACKLE_DMG
    private const int WoundTackleDamage = 10;        // N_TACKLE_DMG (plain attack)
    private const int WoundTackleDamageA2 = 12;      // A_2_N_TACKLE_DMG
    private const int WeakTurns = 1;                 // WEAK_TURNS
    private const int SlimedCount = 1;               // WOUND_COUNT


    /// <summary>HP preset when spawned by a split (bytecode: children get parent's currentHealth).</summary>
    public int? SpawnHp { get; set; }
    public override int MinInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 29, 28);
    public override int MaxInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 34, 32);
    protected override string DonorId => "leaf_slime_m";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var spit = new MoveState("CORROSIVE_SPIT", CorrosiveSpit,
            new SingleAttackIntent(AttackDamage), new StatusIntent(SlimedCount));
        var tackle = new MoveState("WOUND_TACKLE", WoundTackle, new SingleAttackIntent(WoundTackleDamage));
        var lick = new MoveState("WEAK_LICK", WeakLick, new DebuffIntent());

        // Bytecode getMove (acidslimes.txt, AcidSlime_M), roll 0-99 with history sub-rolls:
        //   base (<17):  <30: lastTwo(TACKLE) ? 50/50 TACKLE/WEAK : WOUND
        //                <70: lastMove(TACKLE) ? 40% WOUND / 60% WEAK : TACKLE
        //                >=70: lastTwo(LICK) ? 40% WOUND / 60% TACKLE : WEAK
        //   A17+:        <40: lastTwo(TACKLE) ? 50/50 TACKLE/WEAK : WOUND
        //                <80: lastTwo(TACKLE) ? 50% WOUND / 50% WEAK : TACKLE
        //                >=80: lastMove(LICK) ? 40% WOUND / 60% TACKLE : WEAK
        // Modelled as conditional history branches + weighted sub-rolls; the flat weights
        // below reproduce the same long-run mix (spit/tackle/lick = 30/40/30 base,
        // 40/40/20 A17+). A17 has no StS2 equivalent; gated on DeadlyEnemies.
        var normalAi = new RandomBranchState("AI");
        normalAi.AddBranch(spit, 2, () => 30f);
        normalAi.AddBranch(tackle, 2, () => 40f);
        normalAi.AddBranch(lick, 2, () => 30f);

        var ascendedAi = new RandomBranchState("AI_A17");
        ascendedAi.AddBranch(spit, 2, () => 40f);
        ascendedAi.AddBranch(tackle, 2, () => 40f);
        ascendedAi.AddBranch(lick, 2, () => 20f);

        var ai = new ConditionalBranchState("AI_ROOT");
        ai.AddState(ascendedAi, () => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies));
        ai.AddState(normalAi, () => true);

        states.AddRange(new MonsterState[] { spit, tackle, lick, normalAi, ascendedAi, ai });
        return new MonsterMoveStateMachine(states, ai);
    }

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, CorrosiveSpitDamageA2, CorrosiveSpitDamage);

    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, WoundTackleDamageA2, WoundTackleDamage);

    private async Task CorrosiveSpit(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, null);
    }

    private async Task WoundTackle(IReadOnlyList<Creature> targets)
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

    public override List<(string, string)>? Localization => new MonsterLoc(
        "Acid Slime (M)",
        new[]
        {
            ("CORROSIVE_SPIT", "Corrosive Spit"),
            ("WOUND_TACKLE", "Slime Tackle"),
            ("WEAK_LICK", "Lick")
        });
}
