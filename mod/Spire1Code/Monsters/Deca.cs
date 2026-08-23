using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Ascension;
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
/// StS1 Act-3 boss "Deca" (<c>com.megacrit.cardcrawl.monsters.beyond.Deca</c>). 官方中文名：八体。
/// <para>
/// Bytecode: HP 250, A9 265; BEAM_DMG 10 (A4 12), BEAM_DAZE 2, PROTECT_BLOCK 16.
/// Alternates between BEAM (2× beamDmg + 2 Dazed into discard) and SQUARE_OF_PROTECTION
/// (all monsters +16 Block; A19 also +3 Plated Armor). isAttacking starts true — first turn BEAM.
/// </para>
/// <para>
/// Donor: <c>cubex_construct</c> — a cubic construct with a central eye; closest visual match for
/// Deca's box shape.
/// </para>
/// </summary>
public sealed class Deca : Spire1Monster
{
    // HP 250, A9 → 265
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 265, 250);
    public override int MaxInitialHp => MinInitialHp;

    // BEAM_DMG = 10; ascension >= 4 → 12
    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    // BEAM_DAZE_AMT = 2 (no ascension variant)
    private const int BeamDazeCount = 2;

    // PROTECT_BLOCK = 16 (no ascension variant)
    private const int ProtectBlock = 16;

    // A19 → Plated Armor 3 on all monsters
    private const int ProtectPlatedArmor = 3;

    // ARTIFACT_AMT = 2 (A19 → 3)
    private int ArtifactAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 3, 2);

    protected override string DonorId => "cubex_construct";

    // Vanilla field: isAttacking (starts true — first turn is BEAM).
    private bool _isAttacking;

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Deca",
        [
            ("BEAM_MOVE", "Beam"),
            ("SQUARE_MOVE", "Square of Protection"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // BEAM: 2 hits × beamDmg + 2 Dazed. Intent is ATTACK_DEBUFF in vanilla.
        MoveState beam = new("BEAM_MOVE", BeamMove,
            new MultiAttackIntent(BeamDamage, 2), new StatusIntent(BeamDazeCount));
        // SQUARE_OF_PROTECTION: all monsters +16 Block (A19 +3 Plated Armor). Intent DEFEND / DEFEND_BUFF.
        MoveState square = new("SQUARE_MOVE", SquareMove, new DefendIntent(), new BuffIntent());

        // Alternating: isAttacking latch
        ConditionalBranchState picker = new("DECA_PICKER");
        beam.FollowUpState = picker;
        square.FollowUpState = picker;

        picker.AddState(beam, () => _isAttacking);
        picker.AddState(square, () => !_isAttacking);

        // Bytecode opens with BEAM (isAttacking starts true), then alternates.
        return new MonsterMoveStateMachine([beam, square, picker], beam);
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BeamDamage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, BeamDazeCount, null);
        _isAttacking = false;
    }

    private async Task SquareMove(IReadOnlyList<Creature> targets)
    {
        var allies = base.CombatState.Enemies;
        foreach (var ally in allies)
        {
            await CreatureCmd.GainBlock(ally, ProtectBlock, ValueProp.Move, null);
            if (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss))
            {
                await PowerCmd.Apply<PlatingPower>(
                    new ThrowingPlayerChoiceContext(), ally, ProtectPlatedArmor, base.Creature, null);
            }
        }
        _isAttacking = true;
    }

    public override async Task BeforeCombatStart()
    {
        // usePreBattleAction: unsilence BGM + fadeOutAmbiance (skipped, audio paths are StS1-only)
        // + ArtifactPower(2), A19→3. MarkBossAsSeen("DONUT") is an unlock-tracking call, skipped.
        await PowerCmd.Apply<ArtifactPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, ArtifactAmount, base.Creature, null);
    }
}