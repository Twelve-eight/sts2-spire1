using BaseLib.Abstracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 "Acid Slime (S)" (8-12 HP, A7: 9-13). Does NOT split in vanilla.
/// Moves: Tackle (3/4 dmg) and Lick (1 Weak). getMove alternates: 50/50 roll (A17+: strict
/// lastTwoMoves alternation). takeTurn self-alternates after each move.
/// Donor: leaf_slime_s — the only shipped small green slime rig.
/// </summary>
public sealed class AcidSlimeS : Spire1Monster, ISlimeSplitSpawn
{
    private const int TackleDamage = 3;      // TACKLE_DAMAGE
    private const int TackleDamageA2 = 4;    // A_2_TACKLE_DAMAGE
    private const int WeakTurns = 1;         // WEAK_TURNS

    public override int MinInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 9, 8);
    public override int MaxInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 13, 12);
    public int? SpawnHp { get; set; }

    protected override string DonorId => "leaf_slime_s";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackle = new MoveState("TACKLE", Tackle, new SingleAttackIntent(TackleDamage));
        var lick = new MoveState("LICK", Lick, new DebuffIntent());

        // Vanilla base: 50/50 roll between Tackle and Lick (both repeatable).
        // A17+: strict alternation — lastTwoMoves(Tackle) forces Lick, else Tackle.
        // A17 has no StS2 equivalent; gated on DeadlyEnemies as the nearest higher-difficulty tier.
        var normalAi = new RandomBranchState("AI");
        normalAi.AddBranch(tackle, 1, () => 1f);
        normalAi.AddBranch(lick, 1, () => 1f);

        var ascendedAi = new RandomBranchState("AI_A17");
        ascendedAi.AddBranch(tackle, 1, () => 1f);
        ascendedAi.AddBranch(lick, 1, () => 1f);

        var ai = new ConditionalBranchState("AI_ROOT");
        ai.AddState(ascendedAi, () => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies));
        ai.AddState(normalAi, () => true);

        states.AddRange(new MonsterState[] { tackle, lick, normalAi, ascendedAi, ai });
        return new MonsterMoveStateMachine(states, ai);
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, TackleDamageA2, TackleDamage))
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    private async Task Lick(IReadOnlyList<Creature> targets)
    {
        foreach (var target in targets)
        {
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, WeakTurns, base.Creature, null);
        }
    }

    public override List<(string, string)>? Localization => new MonsterLoc(
        "Acid Slime (S)",
        new[]
        {
            ("TACKLE", "Tackle"),
            ("LICK", "Lick")
        });
}
