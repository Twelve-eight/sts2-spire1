using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.ValueProps;
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
/// StS1 The City — Centurion (<c>com.megacrit.cardcrawl.monsters.city.Centurion</c>). 官方中文名：百夫长。
/// <para>
/// Bytecode: HP 76-80, A7 78-83; SLASH_DMG 12 (A2 14), FURY_DMG 6 (A2 7), FURY_HITS 3,
/// BLOCK_AMOUNT 15, A_17_BLOCK_AMOUNT 20.
/// getMove: r≥65 &amp;&amp; !lastTwoMoves(FURY) &amp;&amp; !lastTwoMoves(PROTECT) →
/// livingMonsters()&gt;1 ? PROTECT : FURY; else !lastTwoMoves(SLASH) → SLASH; else
/// livingMonsters()&gt;1 ? PROTECT : FURY. takeTurn PROTECT = GainBlockRandomMonsterAction(15/20).
/// </para>
/// <para>
/// The vanilla A17 block tier (20) is unreachable in StS2's ascension mapping (max A10 → the two
/// shipped levels), so the base 15 always applies — same convention as SlaverRed's dropped A17 tier.
/// </para>
/// <para>
/// Donor: <c>guardbot</c> — the shipped shield-carrying defender robot; closest visual match for a
/// sword-and-shield bodyguard stance (GremlinShield already borrows it for the same role).
/// </para>
/// </summary>
public sealed class Centurion : Spire1Monster
{
    // setHp(76, 80); ascension >= 7 -> setHp(78, 83)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 76);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 83, 80);

    // slashDmg = 12; ascension >= 2 -> 14
    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    // furyDmg = 6; ascension >= 2 -> 7 (hits fixed at 3)
    private int FuryDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    private const int FuryHits = 3;

    // blockAmount = 15 (vanilla A17 tier of 20 unreachable in StS2; base value kept)
    private const int BlockAmount = 15;

    protected override string DonorId => "guardbot";

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Centurion",
        [
            ("SLASH_MOVE", "Slash"),
            ("PROTECT_MOVE", "Protect"),
            ("FURY_MOVE", "Fury"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState slash = new("SLASH_MOVE", SlashMove, new SingleAttackIntent(SlashDamage));
        MoveState protect = new("PROTECT_MOVE", ProtectMove, new DefendIntent());
        MoveState fury = new("FURY_MOVE", FuryMove, new MultiAttackIntent(FuryDamage, FuryHits));

        ConditionalBranchState branch = new("CENTURION_BRANCH");
        slash.FollowUpState = branch;
        protect.FollowUpState = branch;
        fury.FollowUpState = branch;

        // Vanilla priority chain: high roll with no recent Fury/Protect prefers Protect while allies
        // live; otherwise Slash unless repeated; the fallback repeats Fury only when alone.
        branch.AddState(fury, () => RollHundred() >= 65 && !LastTwoWere(fury) && !LastTwoWere(protect) && LivingMonsters() <= 1);
        branch.AddState(protect, () => RollHundred() >= 65 && !LastTwoWere(fury) && !LastTwoWere(protect));
        branch.AddState(slash, () => !LastTwoWere(slash));
        branch.AddState(protect, () => LivingMonsters() > 1);
        branch.AddState(fury, () => true);

        return new MonsterMoveStateMachine([slash, protect, fury, branch], branch);
    }

    /// <summary>StS1 counts monsters that are neither dying nor escaping.</summary>
    private int LivingMonsters() => CombatState.Enemies.Count(c => c.IsAlive);

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task ProtectMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        // GainBlockRandomMonsterAction: any other non-escaping, non-dying monster; self only as the
        // fallback when that pool is empty (GremlinShield.GuardMove idiom from Guardbot).
        List<Creature> allies = CombatState.Enemies
            .Where(c => c != Creature && c.IsAlive && !IntendsToEscape(c))
            .ToList();
        Creature target = (allies.Count > 0 ? Rng.NextItem(allies) : null) ?? Creature;
        await CreatureCmd.GainBlock(target, BlockAmount, ValueProp.Unpowered, null);
    }

    private static bool IntendsToEscape(Creature creature) =>
        creature.Monster?.NextMove.Intents.Any(intent => intent is EscapeIntent) ?? false;

    private async Task FuryMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FuryDamage).WithHitCount(FuryHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
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

    private bool LastTwoWere(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }
}
