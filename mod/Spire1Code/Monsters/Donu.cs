using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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
/// StS1 Act-3 boss "Donu" (<c>com.megacrit.cardcrawl.monsters.beyond.Donu</c>). 官方中文名：甜圈。
/// <para>
/// Bytecode: HP 250, A9 265; BEAM_DMG 10 (A4 12), BEAM_AMT 1 (not used in block), CIRCLE_STR 3.
/// Alternates between BEAM (2× beamDmg) and CIRCLE_OF_PROTECTION (all monsters +3 Strength).
/// isAttacking latch flips each turn: BEAM → isAttacking=false → CIRCLE → isAttacking=true → BEAM.
/// </para>
/// <para>
/// Donor: <c>globe_head</c> — floating sphere with a central eye; closest visual match for Donu.
/// </para>
/// </summary>
public sealed class Donu : Spire1Monster
{
    // HP 250, A9 → 265
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 265, 250);
    public override int MaxInitialHp => MinInitialHp;

    // BEAM_DMG = 10; ascension >= 4 → 12
    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    // BEAM_AMT = 1 (not used for block in StS1; StS1 Donu's Beam doesn't block)
    // CIRCLE_STR_AMT = 3 (no ascension variant)
    private const int CircleStrength = 3;

    // ARTIFACT_AMT = 2 (A19 → 3)
    private int ArtifactAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 3, 2);

    protected override string DonorId => "globe_head";

    // Vanilla field: isAttacking (starts false — first turn is CIRCLE).
    // But bytecode getMove: isAttacking ? BEAM : CIRCLE. Initial isAttacking = false → first turn CIRCLE.
    // However vanilla TESTS show Donu opens with CIRCLE? Let me re-check...
    // Bytecode Donu constructor: isAttacking = false. getMove: isAttacking? BEAM : CIRCLE.
    // So first move is CIRCLE (buff all monsters). Then BEAM. Then CIRCLE. etc.
    private bool _isAttacking;

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Donu",
        [
            ("BEAM_MOVE", "Beam"),
            ("CIRCLE_MOVE", "Circle of Protection"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // BEAM: 2 hits × beamDmg, FIRE effect
        MoveState beam = new("BEAM_MOVE", BeamMove, new MultiAttackIntent(BeamDamage, 2));
        // CIRCLE_OF_PROTECTION: all monsters +3 Strength
        MoveState circle = new("CIRCLE_MOVE", CircleMove, new BuffIntent());

        // Alternating: isAttacking latch
        ConditionalBranchState picker = new("DONU_PICKER");
        beam.FollowUpState = picker;
        circle.FollowUpState = picker;

        picker.AddState(circle, () => !_isAttacking);
        picker.AddState(beam, () => _isAttacking);

        // Bytecode opens with CIRCLE (isAttacking starts false), then alternates.
        return new MonsterMoveStateMachine([beam, circle, picker], circle);
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BeamDamage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
        _isAttacking = false;
    }

    private async Task CircleMove(IReadOnlyList<Creature> targets)
    {
        // Buff all monsters on the enemy side (vanilla iterates AbstractDungeon.getMonsters()).
        var allies = base.CombatState.Enemies;
        foreach (var ally in allies)
        {
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), ally, CircleStrength, base.Creature, null);
        }
        _isAttacking = true;
    }

    public override async Task BeforeCombatStart()
    {
        // usePreBattleAction: ArtifactPower(2), A19→3
        await PowerCmd.Apply<ArtifactPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, ArtifactAmount, base.Creature, null);
    }
}