using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Ending — Spire Shield (<c>com.megacrit.cardcrawl.monsters.ending.SpireShield</c>).
/// 官方中文名：高塔之盾（<c>.tmp/m25-zhs-names.json</c>）。
/// <para>
/// Bytecode (<c>ending_SpireShield.txt</c>): HP 110, A8 125; BASH_DMG 12 (A3 14) + debuff,
/// FORTIFY_BLOCK 30 (all monsters), SMASH_DMG 34 (A3 38) + self-block equal to damage (A18 99).
/// usePreBattleAction: SurroundedPower on the player + ArtifactPower(1, A18 2).
/// getMove: moveCount%3 0→50/50 FORTIFY/BASH; 1→last BASH ? FORTIFY : BASH; 2→SMASH.
/// takeTurn BASH: attack + if player has orbs &amp;&amp; random → FocusPower(-1) else StrengthPower(-1).
/// FORTIFY: all monsters GainBlock 30. SMASH: attack + GainBlock = damage[1].output (A18 99).
/// die(): remove Surrounded from player and BackAttack from survivors.
/// </para>
/// <para>
/// FLAG: SurroundedPower is the engine's Kaiser Crab flanking power; BackAttackLeftPower is
/// applied to self so the engine's <see cref="SurroundedPower"/>ModifyDamageMultiplicative hook
/// works (the Spear receives BackAttackRightPower). AfterDeath removes both when either dies.
/// </para>
/// <para>
/// The vanilla HP 38-42 range cited in the design ticket is incorrect — the bytecode calls
/// setHp(110) / setHp(125 at A8), not the 38-42 range of the Act-1 Sentry.
/// </para>
/// <para>
/// Donor: <c>guardbot</c> — the shipped shield-bearing construct (Centurion, GremlinShield);
/// its idle_loop/attack/hurt/die tracks match the vanilla animator defaults.
/// </para>
/// </summary>
public sealed class SpireShield : Spire1Monster
{
    // setHp(110); ascension >= 8 -> setHp(125)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 125, 110);

    public override int MaxInitialHp => MinInitialHp;

    // BASH_DMG = 12; ascension >= 3 -> 14
    private int BashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    // SMASH_DMG = 34; ascension >= 3 -> 38
    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 34);

    // FORTIFY_BLOCK = 30 (no ascension variant)
    private const int FortifyBlock = 30;

    // Vanilla BASH tail: if player has orbs && aiRng.randomBoolean() -> FocusPower(-1) else StrengthPower(-1)
    private const int DebuffAmount = -1;

    protected override string DonorId => "guardbot";

    // Vanilla fields: moveCount (starts 0).
    private int _moveCount;

    private int MoveNum => _moveCount;

    // Cached 50/50 roll for move selection (one roll per turn).
    private bool? _coin;
    private int _coinTurn = -1;

    private bool RollFifty()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_coin == null || _coinTurn != turn)
        {
            _coin = base.Rng.NextFloat() < 0.5f;
            _coinTurn = turn;
        }
        return _coin.Value;
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: SurroundedPower on the player (flanking marker).
        foreach (Player player in base.CombatState.Players)
        {
            await PowerCmd.Apply<SurroundedPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, base.Creature, null);
        }
        // BackAttackLeftPower on self — the engine SurroundedPower's flanking check needs the marker.
        await PowerCmd.Apply<BackAttackLeftPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1, base.Creature, null);
        // ArtifactPower(1, A18 2).
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            AscensionHelper.HasAscension(AscensionLevel.DoubleBoss) ? 2 : 1, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState bash = new("BASH_MOVE", BashMove, new SingleAttackIntent(BashDamage), new DebuffIntent());
        MoveState fortify = new("FORTIFY_MOVE", FortifyMove, new DefendIntent());
        MoveState smash = new("SMASH_MOVE", SmashMove, new SingleAttackIntent(SmashDamage), new DefendIntent());

        ConditionalBranchState branch = new("SPIRE_SHIELD_BRANCH");
        bash.FollowUpState = branch;
        fortify.FollowUpState = branch;
        smash.FollowUpState = branch;

        // getMove: moveCount%3 0→50/50; 1→last BASH ? FORTIFY : BASH; 2→SMASH.
        branch.AddState(fortify, () => MoveNum % 3 == 0 ? RollFifty() : (MoveNum % 3 == 1 ? LastWas(bash) : false));
        branch.AddState(bash, () => MoveNum % 3 == 0 ? !RollFifty() : (MoveNum % 3 == 1 ? !LastWas(bash) : false));
        branch.AddState(smash, () => true);
        return new MonsterMoveStateMachine([bash, fortify, smash, branch], branch);
    }

    private async Task BashMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn BASH: ChangeState(ATTACK) + Wait(0.35) + DamageAction(damage[0], BLUNT_HEAVY).
        await DamageCmd.Attack(BashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.35f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        // BASH tail: if player has orbs && random → FocusPower(-1) else StrengthPower(-1).
        bool anyPlayerHasOrbs = targets.Any(t =>
            t.Player is { } p && p.PlayerCombatState is { } cs && cs.OrbQueue.Orbs.Count > 0);
        if (anyPlayerHasOrbs && base.Rng.NextFloat() < 0.5f)
        {
            await PowerCmd.Apply<FocusPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        }
        else
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        }
        _moveCount++;
    }

    private async Task FortifyMove(IReadOnlyList<Creature> targets)
    {
        _ = targets;
        // takeTurn FORTIFY: GainBlockAction(monster, this, 30) for every monster in the group.
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        foreach (Creature ally in base.CombatState.Enemies.Where(c => c.IsAlive))
        {
            await CreatureCmd.GainBlock(ally, FortifyBlock, ValueProp.Move, null);
        }
        _moveCount++;
    }

    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn SMASH: ChangeState(OLD_ATTACK) + Wait(0.5) + DamageAction(damage[1], BLUNT_HEAVY)
        // + GainBlockAction(this, this, damage[1].output) — A18: 99 flat.
        await DamageCmd.Attack(SmashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        // NOTE: vanilla block = damage[1].output (includes strength scaling); port uses base value + A18 99.
        int block = AscensionHelper.HasAscension(AscensionLevel.DoubleBoss) ? 99 : SmashDamage;
        await CreatureCmd.GainBlock(base.Creature, block, ValueProp.Move, null);
        _moveCount++;
    }

    /// <summary>
    /// die(): once one flanker dies, the player is no longer Surrounded and the survivor loses
    /// its BackAttack marker (vanilla removes both in a pass over all alive monsters).
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature)
        {
            return;
        }
        foreach (Player player in base.CombatState.Players)
        {
            SurroundedPower? surrounded = player.Creature.GetPower<SurroundedPower>();
            if (surrounded != null)
            {
                await PowerCmd.Remove(surrounded);
            }
        }
        foreach (Creature survivor in base.CombatState.Enemies.Where(c => c.IsAlive).ToList())
        {
            PowerModel? back = survivor.GetPower<BackAttackLeftPower>()
                ?? (PowerModel?)survivor.GetPower<BackAttackRightPower>();
            if (back != null)
            {
                await PowerCmd.Remove(back);
            }
        }
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Spire Shield",
        [
            ("BASH_MOVE", "Bash"),
            ("FORTIFY_MOVE", "Fortify"),
            ("SMASH_MOVE", "Smash"),
        ]);
}