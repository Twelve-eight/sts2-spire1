// Port of vanilla StS1 com.megacrit.cardcrawl.monsters.exordium.LouseDefensive ("Green Louse").
// All numbers below are transcribed from the javap dump in .tmp/lice.txt; nothing invented.
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
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Vanilla StS1 <c>LouseDefensive</c> (ID <c>FuzzyLouseDefensive</c>) ported to the StS2 engine.
/// <para>
/// Dumped values (<c>.tmp/lice.txt</c>): HP 11-17, A7+ 12-18. Bite damage is rolled ONCE at
/// spawn from <c>monsterHpRng</c>: 5-7, or 6-8 at ascension 2+ (same as the red louse). Curl Up
/// block 3-7 (4-8 at A7+, 9-12 at A17+), applied pre-battle and spent on first hit. Spit Web:
/// Weak 2 to the player, no ascension split (WEAK_AMT = 2). AI: 75/25 bite/web roll where
/// neither move may appear three times in a row.
/// </para>
/// </summary>
public sealed class LouseDefensive : Spire1Monster
{
    // Spawn-time bite roll bounds (constructor: monsterHpRng.random(5,7), A2+: random(6,8)).
    private int BiteMin => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    /// <summary>Inclusive upper bound of the vanilla bite roll.</summary>
    private int BiteMax => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    // Curl Up block range (usePreBattleAction): 3-7, A7+ 4-8, A17+ 9-12.
    private int CurlUpMin => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 4, 3));

    /// <summary>Inclusive upper bound of the vanilla Curl Up roll.</summary>
    private int CurlUpMax => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 8, 7));

    // Spit Web (WEAKEN): WeakPower 2 to the player; WEAK_AMT has no ascension variant.
    private const int WeakAmount = 2;

    /// <summary>
    /// Rolled once per combat at spawn, mirroring the vanilla constructor's single
    /// <c>monsterHpRng</c> draw; reused for every bite instead of re-rolling per attack.
    /// </summary>
    private int _biteDamage;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 11);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 17);

    // Byrdpip is a small critter whose shipped model only overrides SetupSkins to pick a
    // relic-driven skin — defaulting to "version1" when no owner exists — so the engine's
    // default animator and visuals work for a borrowed scene with no extra overrides.
    protected override string DonorId => "byrdpip";

    /// <summary>
    /// Runs before the engine's first <c>RollMove</c> (CombatManager.AddCreatureToCombat),
    /// so this is the faithful spot for the vanilla spawn-time rolls.
    /// </summary>
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // StS1 monsterHpRng ≈ StS2 run-level Niche stream (one-off per-monster rolls).
        Rng spawnRng = base.RunRng.Niche;
        _biteDamage = spawnRng.NextInt(BiteMin, BiteMax + 1);
        await PowerCmd.Apply<CurlUpPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            spawnRng.NextInt(CurlUpMin, CurlUpMax + 1), base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // Damage is read at execution time so the intent always shows the spawn-rolled value.
        MoveState bite = new MoveState("BITE_MOVE", BiteMove, new SingleAttackIntent(() => _biteDamage));
        MoveState spitWeb = new MoveState("SPIT_WEB_MOVE", SpitWebMove, new DebuffIntent());

        // Bytecode getMove (lice.txt, LouseDefensive), roll 0-99; both branches are
        // deterministic history maps (num only picks the branch):
        //   base (<17):  <25: lastTwo(WEB) ? BITE : WEB
        //                >=25: lastTwo(BITE) ? WEB : BITE
        //   A17+:        <25: lastMove(WEB) ? BITE : WEB   (rest identical)
        // Long-run ~80% BITE / 20% WEB. A17 gated on DeadlyEnemies.
        _spitWebState = spitWeb;
        ConditionalBranchState root = new("GREEN_LOUSE_ROOT");
        ConditionalBranchState lowRoll = new("LOW_ROLL");
        ConditionalBranchState highRoll = new("HIGH_ROLL");
        lowRoll.AddState(bite, () => WebGuard());
        lowRoll.AddState(spitWeb, () => true);
        highRoll.AddState(spitWeb, () => LastTwoWere(bite));
        highRoll.AddState(bite, () => true);
        root.AddState(lowRoll, () => LastSubRoll(0.25f));
        root.AddState(highRoll, () => true);
        bite.FollowUpState = root;
        spitWeb.FollowUpState = root;
        return new MonsterMoveStateMachine(
            new List<MonsterState> { root, lowRoll, highRoll, bite, spitWeb }, root);
    }

    private MoveState? _spitWebState;

    private bool LastTwoWere(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }

    // roll<25 history guard: lastTwo(WEB) at base, lastMove(WEB) at A17+ (DeadlyEnemies).
    private bool WebGuard()
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        if (log.Count == 0 || !ReferenceEquals(log[^1], _spitWebState))
        {
            return false;
        }
        return RunManager.Instance.HasAscension(AscensionLevel.DeadlyEnemies)
            || (log.Count >= 2 && ReferenceEquals(log[^2], _spitWebState));
    }

    // One stable sub-roll per turn: vanilla draws aiRng.randomBoolean(0.25) inside getMove;
    // cached so both complementary weight lambdas see the same value.
    private bool? _subRoll;
    private int _subRollTurn = -1;
    private bool LastSubRoll(float threshold)
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_subRoll == null || _subRollTurn != turn)
        {
            _subRoll = base.Rng.NextFloat() < threshold;
            _subRollTurn = turn;
        }
        return _subRoll.Value;
    }

    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        // Vanilla: AnimateSlowAttackAction + DamageAction(BLUNT_LIGHT) → blunt hit vfx.
        await DamageCmd.Attack(_biteDamage).FromMonster(this).WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task SpitWebMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Web", 0.25f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets,
            WeakAmount, base.Creature, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Green Louse",
        [
            ("BITE_MOVE", "Chomp"),
            ("SPIT_WEB_MOVE", "Spit Web")
        ]);
}
