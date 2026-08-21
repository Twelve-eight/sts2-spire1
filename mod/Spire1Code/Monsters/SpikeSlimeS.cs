using BaseLib.Abstracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 "Spike Slime (S)" (10-14 HP, A7: 11-15). Single move: Tackle (5/6 dmg).
/// Donor: twig_slime_s — the only shipped small brown/spiky slime rig.
/// </summary>
public sealed class SpikeSlimeS : Spire1Monster, ISlimeSplitSpawn
{
    private const int TackleDamage = 5;      // TACKLE_DAMAGE
    private const int TackleDamageA2 = 6;    // A_2_TACKLE_DAMAGE

    public override int MinInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 11, 10);
    public override int MaxInitialHp => SpawnHp ?? AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 14);

    /// <summary>HP preset when spawned by a split (bytecode: children get parent's currentHealth).</summary>
    public int? SpawnHp { get; set; }

    protected override string DonorId => "twig_slime_s";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var states = new List<MonsterState>();

        var tackle = new MoveState("TACKLE", Tackle, new SingleAttackIntent(TackleDamage));
        tackle.FollowUpState = tackle; // vanilla getMove always sets Tackle

        states.Add(tackle);
        return new MonsterMoveStateMachine(states, tackle);
    }

    private async Task Tackle(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, TackleDamageA2, TackleDamage))
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    public override List<(string, string)>? Localization => new MonsterLoc(
        "Spike Slime (S)",
        new[]
        {
            ("TACKLE", "Tackle")
        });
}
