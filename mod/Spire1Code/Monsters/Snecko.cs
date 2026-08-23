using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Snecko (<c>com.megacrit.cardcrawl.monsters.city.Snecko</c>). 官方中文名：异蛇。
/// <para>
/// Bytecode: HP 114-120, A7 120-125; BITE_DMG 15 (A2 18), TAIL_DMG 8 (A2 10), VULNERABLE_AMT 2.
/// getMove: first turn GLARE (STRONG_DEBUFF); then r&lt;40 → TAIL; else lastTwoMoves(TAIL) → BITE,
/// otherwise GLARE. takeTurn: GLARE = ConfusionPower on the player (vanilla "your hand is
/// randomized"); TAIL = 8 dmg + Weak 2 (A17) + Vulnerable 2; BITE = 15 dmg.
/// </para>
/// <para>
/// The vanilla A17 extra Weak on Tail is unreachable in StS2's ascension mapping (max A10) and is
/// dropped, matching the convention used by SlaverRed/Sentry for their A17/A18 tiers.
/// </para>
/// <para>
/// FLAGGED: vanilla <c>ConfusionPower</c> randomizes the cost of cards drawn while afflicted. StS2
/// ships <c>ConfusedPower</c> (note the different name) with the same hand-cost-randomization
/// behaviour, so it is applied directly; no custom power is created.
/// </para>
/// <para>
/// Donor: <c>terror_eel</c> — the shipped serpentine creature; closest visual match among shipped
/// scenes for a long coiling snake body.
/// </para>
/// </summary>
public sealed class Snecko : Spire1Monster
{
    // setHp(114, 120); ascension >= 7 -> setHp(120, 125)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 120, 114);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 125, 120);

    // biteDmg = 15; ascension >= 2 -> 18
    private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);

    // tailDmg = 8; ascension >= 2 -> 10
    private int TailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    private const int TailVulnerable = 2;

    protected override string DonorId => "terror_eel";

    // Vanilla field: firstTurn (GLARE latch).
    private bool _usedGlare;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState glare = new("GLARE_MOVE", GlareMove, new DebuffIntent(strong: true));
        MoveState bite = new("BITE_MOVE", BiteMove, new SingleAttackIntent(BiteDamage));
        MoveState tail = new("TAIL_MOVE", TailMove, new SingleAttackIntent(TailDamage), new DebuffIntent());

        ConditionalBranchState branch = new("SNECKO_BRANCH");
        glare.FollowUpState = branch;
        bite.FollowUpState = branch;
        tail.FollowUpState = branch;

        // Priority chain mirrors getMove: opening GLARE; roll<40 → TAIL; last two were
        // BITE → TAIL; otherwise BITE.
        branch.AddState(glare, () => !_usedGlare);
        branch.AddState(tail, () => RollHundred() < 40);
        branch.AddState(tail, () => LastTwoWere(bite));
        branch.AddState(bite, () => true);

        return new MonsterMoveStateMachine([glare, bite, tail, branch], glare);
    }

    private async Task GlareMove(IReadOnlyList<Creature> targets)
    {
        _usedGlare = true;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        // takeTurn GLARE: ApplyPowerAction(new ConfusionPower(player)) — see FLAG note on naming.
        await PowerCmd.Apply<ConfusedPower>(new ThrowingPlayerChoiceContext(), targets, 1, base.Creature, null);
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task TailMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TailDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        // Vanilla applies Weak 2 only from A17 (unreachable in StS2 — dropped); always Vulnerable 2.
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, TailVulnerable, base.Creature, null);
    }

    private bool LastTwoWere(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }

    // One stable 0-99 draw per move selection (vanilla passes one aiRng roll through getMove).
    private int? _roll;
    private int _rollTurn = -1;
    private int RollHundred()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_roll == null || _rollTurn != turn)
        {
            _roll = base.Rng.NextInt(100);
            _rollTurn = turn;
        }
        return _roll.Value;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Snecko",
        [
            ("GLARE_MOVE", "Glare"),
            ("BITE_MOVE", "Bite"),
            ("TAIL_MOVE", "Tail Whack"),
        ]);
}
