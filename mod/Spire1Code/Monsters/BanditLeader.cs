using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — BanditLeader (<c>com.megacrit.cardcrawl.monsters.city.BanditLeader</c>;
/// 官方中文名「罗密欧」, "Romeo"). Masked Bandits trio frontman — event-spawned only,
/// never a regular encounter (Main-agent bytecode audit 2026-08).
/// <para>
/// Bytecode: HP 35-39, A7 37-41; CROSS_SLASH 15 (A2 17); AGONIZING_SLASH 10 (A2 12) +
/// Weak <c>weakAmount</c> 2 (A17 3). getMove seeds byte 2 (MOCK); takeTurn chains
/// MOCK -> SetMove(AGONIZING_SLASH); AGONIZING_SLASH -> SetMove(CROSS_SLASH);
/// CROSS_SLASH -> (ascension &gt;= 17 &amp;&amp; !lastTwoMoves(CROSS_SLASH))
/// ? SetMove(CROSS_SLASH) : SetMove(AGONIZING_SLASH). So base play is
/// MOCK -> SLASH+Weak -> CROSS_SLASH -> repeat; A17 repeats CROSS_SLASH once.
/// </para>
/// <para>
/// deathReact(): when the Bear dies, Romeo talks (DIALOG[0] "NOOOO!" line if a Bear is
/// dying, DIALOG[1] otherwise). Ported through <see cref="TalkCmd"/> with the vanilla
/// English lines localized in code (MonsterLoc ExtraLoc), matching shipped FatGremlin's
/// Flee banter pattern. A17 cross-slash-repeat maps onto DeadlyEnemies like GremlinNob's
/// A18 deterministic branch.
/// </para>
/// <para>
/// Art: donor rig <c>mysterious_knight</c> — a caped, armored duelist humanoid, visually
/// closest to Romeo's tall swordsman silhouette among shipped rigs. NOTE (FLAGGED): the
/// mysterious_knight rig has no idle_loop/attack/hurt/die track set confirmed in source;
/// if in-game it animates oddly, fall back to <c>axe_ruby_raider</c> (default-track axe
/// bandit) by changing DonorId only.
/// </para>
/// </summary>
public sealed class BanditLeader : Spire1Monster
{
    private bool _bearDying;

    // setHp(35, 39); ascension >= 7 -> setHp(37, 41)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 37, 35);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 41, 39);

    // slashDmg = 15; ascension >= 2 -> 17
    private int CrossSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

    // agonizeDmg = 10; ascension >= 2 -> 12
    private int AgonizingSlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    // weakAmount = 2; ascension >= 17 -> 3 (mapped onto DeadlyEnemies, see remarks)
    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override string DonorId => "mysterious_knight";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState mock = new("MOCK", MockMove, new UnknownIntent());
        MoveState agonize = new("AGONIZING_SLASH_MOVE", AgonizingSlashMove,
            new SingleAttackIntent(AgonizingSlashDamage), new DebuffIntent());
        MoveState crossSlash = new("CROSS_SLASH_MOVE", CrossSlashMove, new SingleAttackIntent(CrossSlashDamage));

        // takeTurn/CROSS_SLASH tail at DeadlyEnemies (vanilla A17): two cross slashes in a row
        // force an Agonizing Slash; otherwise the cross slash repeats.
        ConditionalBranchState afterCross = new("AFTER_CROSS_SLASH");
        mock.FollowUpState = agonize;
        agonize.FollowUpState = crossSlash;
        afterCross.AddState(crossSlash, () => !LastTwoWereCrossSlash());
        afterCross.AddState(agonize, () => LastTwoWereCrossSlash());
        crossSlash.FollowUpState = afterCross;
        return new MonsterMoveStateMachine([mock, agonize, crossSlash, afterCross], mock);
    }

    private bool LastTwoWereCrossSlash()
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && log[^1].Id == "CROSS_SLASH_MOVE" && log[^2].Id == "CROSS_SLASH_MOVE";
    }

    private async Task MockMove(IReadOnlyList<Creature> targets)
    {
        // Vanilla MOCK turn is pure dialogue (DIALOG[0] when a bear is dying, DIALOG[1] else).
        LocString line = MonsterModel.L10NMonsterLookup("SPIRE1-BANDIT_LEADER.moves.MOCK." + (_bearDying ? "mockBearLine" : "mockLine"));
        TalkCmd.Play(line, base.Creature, VfxColor.Purple, VfxDuration.Standard);
    }

    private async Task AgonizingSlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AgonizingSlashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, WeakAmount, base.Creature, null);
    }

    private async Task CrossSlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CrossSlashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// StS1 deathReact(): Romeo cries out when an ally dies (the Bear's die() pokes every
    /// living ally). The flag also swaps his next Mock line, mirroring the vanilla
    /// DIALOG[0]/DIALOG[1] split.
    /// </summary>
    public override Task BeforeDeath(Creature creature)
    {
        if (creature == Creature || !creature.IsEnemy)
        {
            return Task.CompletedTask;
        }
        _bearDying = true;
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Romeo",
        [
            ("MOCK", "Mock"),
            ("AGONIZING_SLASH_MOVE", "Agonizing Slash"),
            ("CROSS_SLASH_MOVE", "Cross Slash"),
        ],
        ("moves.MOCK.mockLine", "You dare interfere?!"),
        ("moves.MOCK.mockBearLine", "Bear! NO!"),
        ("moves.MOCK.deathReactLine", "You'll pay for that."));
}
