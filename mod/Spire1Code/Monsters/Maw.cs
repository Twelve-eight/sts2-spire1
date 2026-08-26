using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Maw (<c>com.megacrit.cardcrawl.monsters.beyond.Maw</c>). 官方中文名：巨口。
/// <para>
/// Bytecode: HP fixed 300 (no ascension tier); slamDmg 25 (A2 30), nomDmg 5, strUp 3 (A17 +2),
/// terrifyDur 3 (A17 +2). turnCount starts at 1 and increments on every getMove.
/// First move is always ROAR. getMove: roll&lt;50 → NOMNOMNOM (turnCount/2 hits, min 1) unless
/// last was NOMNOMNOM; otherwise last==SLAM or last==NOMNOMNOM → DROOL else SLAM.
/// takeTurn: ROAR = SFX + shout + Weak(terrifyDur) + Frail(terrifyDur) on the player;
/// SLAM = one slamDmg hit; DROOL = Strength strUp on self; NOMNOMNOM = turnCount/2 hits of 5.
/// die() plays MAW_DEATH (audio only — skipped, see Spire1Monster.HasDeathSfx).
/// </para>
/// <para>
/// Donor: <c>mawler</c> — the shipped gaping-mouth creature; closest visual match.
/// </para>
/// </summary>
public sealed class Maw : Spire1Monster
{
    // HP = 300 fixed (constructor maxHealth, no setHp range, no ascension tier).
    public override int MinInitialHp => 300;

    public override int MaxInitialHp => 300;

    // slamDmg = 25; ascension >= 2 -> 30
    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);

    // nomDmg = 5 (both tiers)
    private const int NomDamage = 5;

    // strUp = 3; ascension >= 17 -> 5
    private int StrengthUp => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    // terrifyDur = 3; ascension >= 17 -> 5
    private int TerrifyDuration => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);

    // Vanilla fields: roared (starts false), turnCount (starts 1).
    private bool _roared;

    private int _turnCount = 1;

    private int _bumpedRound = -1;

    /// <summary>Borrows the shipped mawler scene — a huge mouth creature.</summary>
    protected override string DonorId => "mawler";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState roar = new("ROAR_MOVE", RoarMove, new DebuffIntent(strong: true));
        MoveState slam = new("SLAM_MOVE", SlamMove, new SingleAttackIntent(SlamDamage));
        MoveState drool = new("DROOL_MOVE", DroolMove, new BuffIntent());
        MoveState nom = new("NOM_MOVE", NomMove, new MultiAttackIntent(NomDamage, () => _turnCount / 2));

        ConditionalBranchState opening = new("MAW_OPENING");
        ConditionalBranchState bands = new("MAW_BANDS");

        roar.FollowUpState = bands;
        slam.FollowUpState = bands;
        drool.FollowUpState = bands;
        nom.FollowUpState = bands;

        // First move is always ROAR (vanilla !roared latch). BumpTurnCount() here too:
        // vanilla's getMove increments turnCount unconditionally on EVERY call including the
        // opening one, so by enemy turn k the count is k+1 and NOM hits = floor((k+1)/2).
        // Without this the opening turn never counted and every odd turn >= 3 hit one less.
        // _bumpedRound dedup keeps this to exactly one increment per round even though the
        // state machine may re-evaluate predicates.
        opening.AddState(roar, () => BumpTurnCount() && !_roared);
        opening.AddState(bands, () => true);

        // roll < 50 -> NOMNOMNOM unless it just nommed; else DROOL when the last move was
        // SLAM or NOMNOMNOM, otherwise SLAM. turnCount++ at the top of every getMove —
        // evaluated here because opening is only visited once. NOM hits = turnCount/2.
        bands.AddState(nom, () => BumpTurnCount() && RollHundred() < 50 && !LastWas(nom));
        bands.AddState(drool, () => LastWas(slam) || LastWas(nom));
        bands.AddState(slam, () => true);

        return new MonsterMoveStateMachine([roar, slam, drool, nom, opening, bands], opening);
    }

    private async Task RoarMove(IReadOnlyList<Creature> targets)
    {
        _roared = true;
        // SFXAction(MAW_DEATH) + ShoutAction(DIALOG[0]) — audio skipped; bubble via ExtraLoc.
        TalkCmd.Play(MonsterModel.L10NMonsterLookup("SPIRE1-MAW.moves.ROAR_MOVE.shout"),
            base.Creature, VfxColor.Red, VfxDuration.Long);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, TerrifyDuration, base.Creature, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, TerrifyDuration, base.Creature, null);
    }

    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlamDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task DroolMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, StrengthUp, base.Creature, null);
    }

    private async Task NomMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn NOMNOMNOM: one hit per turnCount/2 (vanilla loops the pair VFX+damage).
        await DamageCmd.Attack(NomDamage).WithHitCount(_turnCount / 2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .Execute(null);
    }

    /// <summary>Vanilla increments turnCount at the top of every getMove (once per round).</summary>
    private bool BumpTurnCount()
    {
        int round = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_bumpedRound != round)
        {
            _bumpedRound = round;
            _turnCount++;
        }
        return true;
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
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
        new MonsterLoc(Tr("Maw", "巨口"),
        [
            ("ROAR_MOVE", Tr("Roar", "咆哮")),
            ("SLAM_MOVE", Tr("Slam", "猛击")),
            ("DROOL_MOVE", Tr("Drool", "垂涎")),
            ("NOM_MOVE", Tr("Nom Nom Nom", "咀嚼")),
        ],
        ("moves.ROAR_MOVE.shout", Tr("@ROOOAAAR!!!@", "@呜嗷嗷啊嗷！！@")));
}
