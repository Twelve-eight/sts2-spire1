using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Exploder (<c>com.megacrit.cardcrawl.monsters.beyond.Exploder</c>).
/// 官方中文名：爆炸机。
/// <para>
/// Bytecode: HP 30/30, A7 30-35; attackDmg 9 (A2 11). usePreBattleAction applies
/// <see cref="ExplosivePower"/> with 3 stacks — the countdown ticks at the start of each of its
/// turns and at 0 it deals 30 damage to every player and dies (<c>EXPLODE_BASE</c> 30, THORNS
/// semantics; implemented by our existing <see cref="ExplosivePower"/> port).
/// </para>
/// <para>
/// Vanilla counts <c>turnCount++</c> at the top of every takeTurn and getMove reads it:
/// turns 1-2 are ATTACK (<c>turnCount &lt; 2</c> when rolled), afterwards BLOCK (byte 2, UNKNOWN
/// intent — an empty turn while the fuse burns down). The state machine reproduces this with
/// the completed-move count: decisions happen after exactly that many performed moves.
/// </para>
/// <para>
/// Donor: <c>gas_bomb</c> — the shipped round bomb creature that also explodes; closest
/// silhouette among all shipped scenes.
/// </para>
/// </summary>
public sealed class Exploder : Spire1Monster
{
    // setHp(30, 30); ascension >= 7 -> setHp(30, 35)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 30, 30);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 35, 30);

    // attackDmg = 9; ascension >= 2 -> 11
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 9);

    protected override string DonorId => "gas_bomb";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new ExplosivePower(this, 3)).
        await PowerCmd.Apply<ExplosivePower>(new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attack = new("ATTACK_MOVE", AttackMove, new SingleAttackIntent(AttackDamage));
        // Byte 2 (BLOCK): UNKNOWN intent, takeTurn case does nothing but RollMove.
        MoveState block = new("BLOCK_MOVE", BlockMove, new UnknownIntent());

        ConditionalBranchState decide = new("EXPLODER_DECIDE");
        attack.FollowUpState = decide;
        block.FollowUpState = decide;

        // getMove: turnCount < 2 -> ATTACK else BLOCK; turnCount equals completed moves here.
        decide.AddState(block, () => base.MoveStateMachine.StateLog.Count >= 2);
        decide.AddState(attack, () => true);

        return new MonsterMoveStateMachine([attack, block, decide], decide);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn ATTACK: AnimateSlowAttackAction + DamageAction(FIRE).
        await DamageCmd.Attack(AttackDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private Task BlockMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn BLOCK (byte 2): nothing — the turn is spent waiting out the fuse.
        return Task.CompletedTask;
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Exploder", "爆炸机"),
        [
            ("ATTACK_MOVE", Tr("Attack", "攻击")),
            ("BLOCK_MOVE", Tr("Block", "防御")),
        ]);
}
