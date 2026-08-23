using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Orb Walker (<c>com.megacrit.cardcrawl.monsters.beyond.OrbWalker</c>).
/// 官方中文名：圆球行者。
/// <para>
/// Bytecode: HP monsterHpRng.random(90,96), A7 setHp(92,102); clawDmg 15 (A2 16),
/// laserDmg 10 (A2 11). usePreBattleAction applies GenericStrengthUpPower(MOVES[0], 3)
/// (A17: 5) — "gains N Strength at the start of its turn". getMove: roll&lt;40 → CLAW unless
/// the last two moves were CLAW (then LASER); else LASER unless the last two were LASER
/// (then CLAW). takeTurn: CLAW = one clawDmg hit (SLASH_HEAVY); LASER = one laserDmg hit
/// (FIRE) + MakeTempCardInDiscardAndDeckAction(Burn).
/// </para>
/// <para>
/// FLAGGED: vanilla GenericStrengthUpPower has no shipped StS2 equivalent. The shipped
/// <see cref="RitualPower"/> gains its amount of Strength at the end of the owner side's turn
/// and skips the turn it was applied; pre-granting one stack in
/// <see cref="AfterAddedToRoom"/> aligns the ramp with vanilla's start-of-turn timing
/// (turn 1 already buffed, +N at every later turn end).
/// </para>
/// <para>
/// Donor: <c>globe_head</c> — the shipped floating orb-headed creature (same donor as Donu).
/// </para>
/// </summary>
public sealed class OrbWalker : Spire1Monster
{
    // HP monsterHpRng.random(90,96); ascension >= 7 -> setHp(92,102)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 92, 90);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 102, 96);

    // clawDmg = 15; ascension >= 2 -> 16
    private int ClawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

    // laserDmg = 10; ascension >= 2 -> 11
    private int LaserDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

    // GenericStrengthUpPower amount: 3; ascension >= 17 -> 5
    private int StrengthGain => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    /// <summary>Borrows the shipped globe_head scene.</summary>
    protected override string DonorId => "globe_head";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: GenericStrengthUpPower(MOVES[0], 3/5) — see FLAG note.
        await PowerCmd.Apply<RitualPower>(new ThrowingPlayerChoiceContext(), Creature, StrengthGain, Creature, null);
        // Pre-grant one stack so turn 1 matches vanilla's start-of-turn buff (see class remarks).
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, StrengthGain, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState claw = new("CLAW_MOVE", ClawMove, new SingleAttackIntent(ClawDamage));
        MoveState laser = new("LASER_MOVE", LaserMove, new SingleAttackIntent(LaserDamage), new DebuffIntent());

        ConditionalBranchState bands = new("ORB_WALKER_BANDS");
        claw.FollowUpState = bands;
        laser.FollowUpState = bands;

        // roll < 40 -> CLAW unless the last two were CLAW; else LASER unless the last two
        // were LASER; the two guards cross so no move can appear three times in a row.
        bands.AddState(claw, () => RollHundred() < 40 && !LastTwoWere(claw));
        bands.AddState(laser, () => RollHundred() < 40);
        bands.AddState(laser, () => !LastTwoWere(laser));
        bands.AddState(claw, () => true);

        return new MonsterMoveStateMachine([claw, laser, bands], claw);
    }

    private async Task ClawMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ClawDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task LaserMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(LaserDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
        // MakeTempCardInDiscardAndDeckAction(new Burn()): one copy into the discard pile and
        // one into the draw pile (shipped Noisebot pattern).
        Player? player = targets.FirstOrDefault()?.Player;
        if (player != null)
        {
            CardModel discardCard = base.CombatState.CreateCard<Burn>(player);
            await CardPileCmd.AddGeneratedCardToCombat(discardCard, PileType.Discard, null);
            CardModel drawCard = base.CombatState.CreateCard<Burn>(player);
            await CardPileCmd.AddGeneratedCardToCombat(drawCard, PileType.Draw, null, CardPilePosition.Random);
        }
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

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move titles
    // follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Orb Walker", "圆球行者"),
        [
            ("CLAW_MOVE", Tr("Claw", "爪击")),
            ("LASER_MOVE", Tr("Orb Laser", "圆球激光")),
        ]);
}
