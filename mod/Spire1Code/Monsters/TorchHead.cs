using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — TorchHead (<c>com.megacrit.cardcrawl.monsters.city.TorchHead</c>;
/// 官方中文名「火炬头」). City weak-encounter single (vanilla pool "Torch Head" =
/// <c>new MonsterGroup(new TorchHead)</c>; the Act-1 encounter worker owns that file —
/// this class is the monster itself).
/// <para>
/// Bytecode: HP 38-40, A9 40-45; ATTACK_DMG 7 flat, TACKLE only move
/// (getMove always setMove(ATTACK 7); takeTurn re-sets the same). No ascension damage
/// variant exists in vanilla. The per-frame TorchHeadFireEffect particle emitter is a
/// cosmetic spine-bone effect and is not ported.
/// </para>
/// <para>
/// Ascension mapping: vanilla A9 HP tier -> ToughEnemies (shipped convention; there is no
/// damage bump to map).
/// </para>
/// <para>
/// Art: donor rig <c>torch_head_amalgam</c> — literally the shipped StS2 fire-headed
/// creature (idle_loop/attack/hurt/die tracks; Hexaghost already borrows it at boss scale,
/// proving the rig works with default triggers).
/// </para>
/// </summary>
public sealed class TorchHead : Spire1Monster
{
    // setHp(38, 40); ascension >= 9 -> setHp(40, 45)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 38);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 40);

    // ATTACK_DMG = 7 (no ascension variant)
    private const int AttackDamage = 7;

    protected override string DonorId => "torch_head_amalgam";

    /// <summary>
    /// The torch_head_amalgam rig ships idle_loop/attack/hurt/die plus a debuff track but no
    /// cast track (Hexaghost.GenerateAnimator precedent) — remap so missing triggers fold onto
    /// existing tracks instead of warning-spamming.
    /// </summary>
    public override CreatureAnimator GenerateAnimator(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState tackle = new("TACKLE_MOVE", TackleMove, new SingleAttackIntent(AttackDamage));
        tackle.FollowUpState = tackle;
        return new MonsterMoveStateMachine([tackle], tackle);
    }

    private async Task TackleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Torch Head",
        [
            ("TACKLE_MOVE", "Tackle"),
        ]);
}
