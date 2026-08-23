using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Spheric Guardian (<c>com.megacrit.cardcrawl.monsters.city.SphericGuardian</c>).
/// 官方中文名：圆球守护者。
/// <para>
/// Bytecode: HP fixed 20 (constructor maxHealth, no setHp range and no ascension HP tier);
/// dmg 10 (A2 11). usePreBattleAction: BarricadePower + ArtifactPower(3) + GainBlockAction(40)
/// — the "shell": barricade keeps block between turns and 3 artifact absorbs the first debuffs.
/// getMove: first move ACTIVATE (DEFEND); second move FRAIL_ATTACK (ATTACK_DEBUFF);
/// afterwards lastMove(SLAM) ? HARDEN : SLAM. takeTurn: SLAM = 2×BLUNT_HEAVY hits;
/// ACTIVATE = GainBlock(A17+ ? 35 : 25); HARDEN = GainBlock(15) + hit;
/// FRAIL_ATTACK = hit + Frail 5 on the player.
/// </para>
/// <para>
/// Ascension mapping: damage A2 tier → <see cref="AscensionLevel.DeadlyEnemies"/>; the A17
/// Activate-block tier (35) maps onto DeadlyEnemies like GremlinNob's A18 branch; HP has no
/// vanilla ascension tier so ToughEnemies is intentionally unused here.
/// </para>
/// <para>
/// Donor: <c>globe_head</c> — the shipped floating sphere creature; closest spherical silhouette
/// among the shipped scenes for a hovering guardian orb.
/// </para>
/// </summary>
public sealed class SphericGuardian : Spire1Monster
{

    protected override string DonorId => "globe_head";
    // AbstractMonster(NAME, ID, 20, ...) — fixed 20 HP, no setHp call, no ascension HP branch.
    public override int MinInitialHp => 20;

    public override int MaxInitialHp => 20;

    // dmg = 10; ascension >= 2 -> 11
    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

    // ACTIVATE: A17+ gains 35 instead of 25 (mapped onto DeadlyEnemies).
    private int ActivateBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 35, 25);

    private const int HardenBlock = 15;

    private const int FrailTurns = 5;

    // Pre-battle shell: BarricadePower + ArtifactPower(3) + GainBlockAction(this, this, 40).
    private const int ArtifactAmount = 3;

    private const int StartingBlock = 40;

    // Vanilla fields firstMove / secondMove gate the scripted opening pair; flipped at perform
    // time, which yields the same visible sequence as flipping them inside getMove.
    private bool _firstMoveDone;

    private bool _secondMoveDone;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction order: Barricade, Artifact 3, then the 40 starting block.
        await PowerCmd.Apply<BarricadePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Creature, ArtifactAmount, Creature, null);
        await CreatureCmd.GainBlock(Creature, StartingBlock, ValueProp.Unpowered, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState activate = new("ACTIVATE_MOVE", ActivateMove, new DefendIntent());
        MoveState frailAttack = new("FRAIL_ATTACK_MOVE", FrailAttackMove,
            new SingleAttackIntent(Damage), new DebuffIntent());
        MoveState slam = new("SLAM_MOVE", SlamMove, new MultiAttackIntent(Damage, 2));
        MoveState harden = new("HARDEN_MOVE", HardenMove, new SingleAttackIntent(Damage), new DefendIntent());

        ConditionalBranchState branch = new("SPHERIC_GUARDIAN_BRANCH");
        activate.FollowUpState = branch;
        frailAttack.FollowUpState = branch;
        slam.FollowUpState = branch;
        harden.FollowUpState = branch;

        // Bytecode getMove: scripted ACTIVATE then FRAIL_ATTACK opening, then alternate — a Slam
        // is always followed by Harden, anything else rolls Slam again.
        branch.AddState(activate, () => !_firstMoveDone);
        branch.AddState(frailAttack, () => !_secondMoveDone);
        branch.AddState(harden, () => LastWas(slam));
        branch.AddState(slam, () => true);

        return new MonsterMoveStateMachine([activate, frailAttack, slam, harden, branch], activate);
    }

    // takeTurn ACTIVATE (byte 2): GainBlockAction(25 / 35 on the A17 tier) + detect VO.
    private async Task ActivateMove(IReadOnlyList<Creature> targets)
    {
        _firstMoveDone = true;
        await CreatureCmd.GainBlock(base.Creature, ActivateBlock, ValueProp.Move, null);
    }

    // takeTurn SLAM (byte 1): two BLUNT_HEAVY hits.
    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // takeTurn HARDEN (byte 3): GainBlockAction(15) + fast BLUNT_HEAVY hit.
    private async Task HardenMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, HardenBlock, ValueProp.Move, null);
        await DamageCmd.Attack(Damage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // takeTurn FRAIL_ATTACK (byte 4): slow BLUNT_LIGHT hit + ApplyPowerAction(FrailPower, 5).
    private async Task FrailAttackMove(IReadOnlyList<Creature> targets)
    {
        _secondMoveDone = true;
        await DamageCmd.Attack(Damage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, FrailTurns, base.Creature, null);
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move titles
    // follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Spheric Guardian", "圆球守护者"),
        [
            ("SLAM_MOVE", Tr("Slam", "猛击")),
            ("ACTIVATE_MOVE", Tr("Activate", "激活")),
            ("HARDEN_MOVE", Tr("Harden", "硬化")),
            ("FRAIL_ATTACK_MOVE", Tr("Debuff Attack", "虚弱打击")),
        ]);
}
