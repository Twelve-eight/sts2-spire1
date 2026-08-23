using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Darkling (<c>com.megacrit.cardcrawl.monsters.beyond.Darkling</c>).
/// 官方中文名：小黑。
/// <para>
/// Bytecode: HP 48-56, A7 50-59; chompDmg 8 (A2 9), nipDmg monsterHpRng.random(7,11) per
/// instance (A2 9-13), HARDEN block 12 + Strength 2 at A17; firstMove starts true.
/// takeTurn: CHOMP = two hits of chompDmg (BLUNT_HEAVY); HARDEN = 12 block (A17: +2 Str);
/// NIP = one hit of nipDmg (BLUNT_LIGHT); COUNT = dialog only (DIALOG[0]); REINCARNATE =
/// SFX, heal maxHealth/2, ChangeState REVIVE (halfDead=false), re-apply RegrowPower(1),
/// relics.onSpawnMonster, then roll again.
/// </para>
/// <para>
/// Half-death (the "split" the wiki describes): dropping to 0 HP does NOT kill a Darkling
/// while any other Darkling is still alive — it enters the half-dead cycle instead. Its next
/// move becomes COUNT (shows "..." dialog, unknown intent), then REINCARNATE heals it to 50%
/// max HP. Only when every Darkling is half-dead at once do they all die for real
/// (vanilla cannotLose=false + die() on each).
/// </para>
/// <para>
/// getMove: halfDead → REINCARNATE; firstMove → r&lt;50 HARDEN else NIP; then roll&lt;40 →
/// CHOMP×2 only when last move wasn't CHOMP and the pack slot is even (else vanilla rerolls
/// 40-99, modelled by falling through to the next band); roll&lt;70 → HARDEN unless last was
/// HARDEN (else NIP); roll≥70 → NIP unless the last two were NIP (vanilla rerolls 0-99 —
/// approximated by preferring CHOMP when legal, NIP otherwise).
/// </para>
/// <para>
/// StS2 port notes: the engine's death pipeline has no "0 HP but keep fighting" state, so a
/// half-dead Darkling sits at 1 HP (healed by <see cref="AfterPreventingDeath"/>, which is
/// also required to stop the Kill pipeline from re-entering) instead of vanilla's 0 HP; its
/// move script never attacks while half-dead, so the difference is visual only.
/// <see cref="RegrowPower"/> is a display buff exactly like vanilla's (the mechanic lives in
/// the damage/ShouldDie path and the REINCARNATE move, per bytecode).
/// </para>
/// <para>
/// Donor: <c>inklet</c> — a small dark inky creature; closest visual match for a compact
/// quadruped.
/// </para>
/// </summary>
public sealed class Darkling : Spire1Monster
{
    // setHp(48, 56); ascension >= 7 -> setHp(50, 59)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 59, 56);

    // chompDmg = 8; ascension >= 2 -> 9
    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    // nipDmg = monsterHpRng.random(7,11); ascension >= 2 -> random(9,13), rolled per instance.
    private int _nipDmg;

    // HARDEN block is flat 12 in both tiers.
    private const int HardenBlock = 12;

    // A17 HARDEN additionally applies Strength 2 (mapped onto DeadlyEnemies).
    private int HardenStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 0);

    // Vanilla fields: firstMove, halfDead.
    private bool _firstMove = true;

    private bool _halfDead;

    // Slot parity of this Darkling in the pack (vanilla getMove: lastIndexOf(this) % 2 == 0).
    private bool _slotEven;

    // The COUNT move state, forced when a Darkling drops to 0 HP mid-round (vanilla SetMoveAction).
    private MoveState? _countState;

    /// <summary>Borrows the shipped inklet scene — a small dark creature.</summary>
    protected override string DonorId => "inklet";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: cannotLose=true (modelled by ShouldDie/AfterPreventingDeath) +
        // ApplyPowerAction(new RegrowPower(this)).
        await PowerCmd.Apply<RegrowPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        // nipDmg is rolled per instance at construction (monsterHpRng).
        _nipDmg = base.Rng.NextInt(
            AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7),
            AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12));
        // getMove(firstMove): lastIndexOf(this) % 2 == 0 gates the double-Chomp band.
        IReadOnlyList<Creature> enemies = base.CombatState.Enemies;
        int idx = -1;
        for (int i = 0; i < enemies.Count; i++)
            if (ReferenceEquals(enemies[i], base.Creature)) { idx = i; break; }
        _slotEven = idx % 2 == 0;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState chomp = new("CHOMP_MOVE", ChompMove, new MultiAttackIntent(ChompDamage, 2));
        MoveState harden = new("HARDEN_MOVE", HardenMove, new DefendIntent());
        MoveState nip = new("NIP_MOVE", NipMove, new SingleAttackIntent(() => (decimal)_nipDmg));
        MoveState count = new("COUNT_MOVE", CountMove, new UnknownIntent());
        MoveState reincarnate = new("REINCARNATE_MOVE", ReincarnateMove, new BuffIntent());
        _countState = count;

        ConditionalBranchState firstBands = new("DARKLING_FIRST");
        ConditionalBranchState bands = new("DARKLING_BANDS");

        chomp.FollowUpState = bands;
        harden.FollowUpState = bands;
        nip.FollowUpState = bands;
        count.FollowUpState = bands;
        reincarnate.FollowUpState = bands;

        // Opening (vanilla firstMove latch): roll < 50 -> HARDEN, else NIP.
        firstBands.AddState(harden, () => FirstMoveRoll(50));
        firstBands.AddState(nip, () => ConsumeFirstMove());
        firstBands.AddState(bands, () => true);

        // Half-dead latch: REINCARNATE (heals to 50% max HP) until revived.
        bands.AddState(reincarnate, () => _halfDead);
        // roll < 40: CHOMP x2 unless last was CHOMP or the slot is odd — vanilla rerolls
        // 40-99, which falls through to the bands below (see class remarks).
        bands.AddState(chomp, () => RollHundred() < 40 && !LastWas(chomp) && _slotEven);
        bands.AddState(harden, () => RollHundred() < 70 && !LastWas(harden));
        bands.AddState(nip, () => !LastTwoWere(nip));
        // roll >= 70 with the last two NIP: vanilla rerolls 0-99; approximate that recursion
        // by preferring CHOMP when legal, NIP otherwise.
        bands.AddState(chomp, () => !LastWas(chomp));
        bands.AddState(nip, () => true);

        return new MonsterMoveStateMachine([chomp, harden, nip, count, reincarnate, firstBands, bands], firstBands);
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompDamage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task HardenMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, HardenBlock, ValueProp.Move, null);
        int strength = HardenStrength;
        if (strength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, strength, base.Creature, null);
        }
    }

    private async Task NipMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(_nipDmg).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private Task CountMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn COUNT: TextAboveCreatureAction(DIALOG[0]) only, then RollMove.
        TalkCmd.Play(MonsterModel.L10NMonsterLookup("SPIRE1-DARKLING.moves.COUNT_MOVE.dialog"),
            base.Creature, VfxColor.DarkGray, VfxDuration.Standard);
        return Task.CompletedTask;
    }

    private async Task ReincarnateMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn REINCARNATE: heal maxHealth/2, ChangeState REVIVE (halfDead=false),
        // re-apply RegrowPower(1), relics.onSpawnMonster (no StS2 equivalent — skipped).
        await CreatureCmd.Heal(base.Creature, base.Creature.MaxHp / 2);
        _halfDead = false;
        await PowerCmd.Apply<RegrowPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1, base.Creature, null);
    }

    /// <summary>
    /// Vanilla damage(): a Darkling only truly dies when every Darkling in the pack is
    /// half-dead at once; otherwise death is prevented and the half-dead cycle begins.
    /// Once half-dead, further damage never kills it (vanilla halfDead guard + cannotLose).
    /// </summary>
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Creature)
        {
            return true;
        }
        if (_halfDead)
        {
            return false;
        }
        return AllSiblingsHalfDead();
    }

    /// <summary>
    /// Vanilla half-death transition: mark half-dead, cleanse nothing (engine keeps powers),
    /// swap the pending move to COUNT (vanilla SetMoveAction + createIntent).
    /// Heal 1 HP so the engine's Kill pipeline does not re-enter (see class remarks).
    /// </summary>
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature)
        {
            return;
        }
        _halfDead = true;
        await CreatureCmd.Heal(Creature, 1m, playAnim: false);
        // SetMoveImmediate mirrors vanilla's SetMoveAction(COUNT, UNKNOWN) + createIntent:
        // the darkling's pending move (and its intent display) becomes COUNT.
        if (_countState != null)
        {
            SetMoveImmediate(_countState, forceTransition: true);
        }
    }

    /// <summary>
    /// When the last Darkling drops, vanilla sets cannotLose=false and calls die() on every
    /// Darkling. Here the pack-kill is issued from the dying one's death path (force bypasses
    /// the half-dead ShouldDie guard of the siblings).
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != Creature || wasRemovalPrevented)
        {
            return;
        }
        foreach (Creature sibling in base.CombatState.Enemies.ToList())
        {
            if (sibling != Creature && sibling.IsAlive && sibling.Monster is Darkling)
            {
                await CreatureCmd.Kill(sibling, force: true);
            }
        }
    }

    private bool AllSiblingsHalfDead()
    {
        foreach (Creature c in base.CombatState.Enemies)
        {
            if (c != Creature && c.Monster is Darkling sibling && !sibling._halfDead && c.IsAlive)
            {
                return false;
            }
        }
        return true;
    }

    private bool FirstMoveRoll(int threshold)
    {
        if (!_firstMove)
        {
            return false;
        }
        if (RollHundred() >= threshold)
        {
            return false;
        }
        _firstMove = false;
        return true;
    }

    private bool ConsumeFirstMove()
    {
        if (!_firstMove)
        {
            return false;
        }
        _firstMove = false;
        return true;
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
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
    // follow the same localization style (vanilla Darkling.MOVES[0] = 啊呜！啊呜！).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Darkling", "小黑"),
        [
            ("CHOMP_MOVE", Tr("Chomp", "啊呜！啊呜！")),
            ("HARDEN_MOVE", Tr("Harden", "硬化")),
            ("NIP_MOVE", Tr("Nip", "轻咬")),
            ("COUNT_MOVE", Tr("Count", "计数")),
            ("REINCARNATE_MOVE", Tr("Reincarnate", "重生")),
        ],
        ("moves.COUNT_MOVE.dialog", Tr("...", "重生中……")));
}
