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
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Ending — Spire Spear (<c>com.megacrit.cardcrawl.monsters.ending.SpireSpear</c>).
/// 官方中文名：高塔之矛（<c>.tmp/m25-zhs-names.json</c>）。
/// <para>
/// Bytecode (<c>ending_SpireSpear.txt</c>): HP 160, A8 180; BURN_STRIKE_DMG 5 (A3 6) ×2 +
/// 2×Burn (A18: into the draw pile, else discard), PIERCER: all monsters Strength 2,
/// SKEWER_DMG 10 × skewerCount (3, A3 4). usePreBattleAction: ArtifactPower(1, A18 2).
/// getMove: moveCount%3 0→last BURN_STRIKE ? PIERCER : BURN_STRIKE; 1→SKEWER; 2→50/50.
/// die(): remove Surrounded from player and BackAttack from survivors.
/// </para>
/// <para>
/// FLAG: BackAttackRightPower is applied to self so the engine's <see cref="SurroundedPower"/>
/// flanking hook works (the Shield carries BackAttackLeftPower); AfterDeath removes both markers.
/// </para>
/// <para>
/// Donor: <c>stabbot</c> — the shipped pointy stabby construct (Sentry, SnakeDagger, Slavers);
/// idle_loop/attack/hurt/die tracks match the vanilla animator defaults.
/// </para>
/// </summary>
public sealed class SpireSpear : Spire1Monster
{
    // setHp(160); ascension >= 8 -> setHp(180)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 180, 160);

    public override int MaxInitialHp => MinInitialHp;

    // BURN_STRIKE_DMG = 5; ascension >= 3 -> 6
    private int BurnStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    // SKEWER_DMG = 10 (no ascension variant)
    private const int SkewerDamage = 10;

    // BURN_STRIKE_COUNT = 2 (fixed)
    private const int BurnStrikeHits = 2;

    // Burn cards added per BURN_STRIKE (A18: draw pile, else discard).
    private const int BurnAmount = 2;

    // PIERCER: all monsters Strength 2
    private const int PiercerStrength = 2;

    // skewerCount: 3; ascension >= 3 -> 4
    private int SkewerCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    protected override string DonorId => "stabbot";

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
        // BackAttackRightPower on self — flanking marker for the engine SurroundedPower.
        await PowerCmd.Apply<BackAttackRightPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1, base.Creature, null);
        // usePreBattleAction: ArtifactPower(1, A18 2).
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            AscensionHelper.HasAscension(AscensionLevel.DoubleBoss) ? 2 : 1, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState burnStrike = new("BURN_STRIKE_MOVE", BurnStrikeMove,
            new MultiAttackIntent(BurnStrikeDamage, () => BurnStrikeHits), new DebuffIntent());
        MoveState piercer = new("PIERCER_MOVE", PiercerMove, new BuffIntent());
        MoveState skewer = new("SKEWER_MOVE", SkewerMove,
            new MultiAttackIntent(SkewerDamage, () => SkewerCount));

        ConditionalBranchState branch = new("SPIRE_SPEAR_BRANCH");
        burnStrike.FollowUpState = branch;
        piercer.FollowUpState = branch;
        skewer.FollowUpState = branch;

        // getMove: moveCount%3 0→last BURN_STRIKE ? PIERCER : BURN_STRIKE; 1→SKEWER; 2→50/50.
        branch.AddState(burnStrike, () => MoveNum % 3 == 0 ? !LastWas(burnStrike) : (MoveNum % 3 == 2 ? !RollFifty() : false));
        branch.AddState(piercer, () => MoveNum % 3 == 0 ? LastWas(burnStrike) : (MoveNum % 3 == 2 ? RollFifty() : false));
        branch.AddState(skewer, () => true);
        return new MonsterMoveStateMachine([burnStrike, piercer, skewer, branch], branch);
    }

    private async Task BurnStrikeMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn BURN_STRIKE: two ChangeState(ATTACK) + Wait(0.15) + DamageAction(damage[0], FIRE)
        // each, then Burn ×2 — A18: draw pile, else discard.
        await DamageCmd.Attack(BurnStrikeDamage).WithHitCount(BurnStrikeHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.15f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets,
            AscensionHelper.HasAscension(AscensionLevel.DoubleBoss) ? PileType.Draw : PileType.Discard,
            BurnAmount, null);
        _moveCount++;
    }

    private async Task PiercerMove(IReadOnlyList<Creature> targets)
    {
        _ = targets;
        // takeTurn PIERCER: ApplyPowerAction(monster, this, StrengthPower(monster, 2)) for every monster.
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        foreach (Creature ally in base.CombatState.Enemies.Where(c => c.IsAlive))
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), ally, PiercerStrength, base.Creature, null);
        }
        _moveCount++;
    }

    private async Task SkewerMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn SKEWER: skewerCount × (ChangeState(ATTACK) + Wait(0.05) +
        // DamageAction(damage[1], SLASH_DIAGONAL, deadOn)).
        await DamageCmd.Attack(SkewerDamage).WithHitCount(SkewerCount).FromMonster(this)
            .WithAttackerAnim("Attack", 0.05f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
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
        new MonsterLoc("Spire Spear",
        [
            ("BURN_STRIKE_MOVE", "Burn Strike"),
            ("PIERCER_MOVE", "Piercer"),
            ("SKEWER_MOVE", "Skewer"),
        ]);
}