using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
/// StS1 The Ending — Corrupt Heart (<c>com.megacrit.cardcrawl.monsters.ending.CorruptHeart</c>).
/// 官方中文名：腐化之心（<c>.tmp/m25-zhs-names.json</c>）。
/// <para>
/// Bytecode (<c>ending_CorruptHeart.txt</c>): HP 750, A9 800; BLOOD_SHOT_DMG 2 × bloodHitCount
/// (12, A4 15); ECHO_ATTACK_DMG 40 (A4 45); DEBILITATE: Vulnerable 2 + Weak 2 + Frail 2 + 5
/// status cards to draw pile (Dazed, Slimed, Wound, Burn, Void); BUFF: recover strength debuff
/// + Strength 2, then cycle through Artifact 2 / BeatOfDeath 1 (MISSING — see FLAG) /
/// PainfulStabs / Strength 10 / Strength 50+.
/// usePreBattleAction: InvinciblePower(300, A19 200) + BeatOfDeathPower(1, A19 2) — BOTH MISSING
/// from the StS2 engine, so the Heart is a plain boss in this port.
/// getMove: firstTurn → DEBILITATE; then moveCount%3 0→50/50 BLOOD_SHOTS/ECHO;
/// 1→last ECHO ? BLOOD_SHOTS : ECHO; 2→BUFF.
/// die(): onBossVictoryLogic + onFinalBossVictoryLogic + stopClock — the engine handles boss
/// victory; no StS1-specific hooks are ported.
/// </para>
/// <para>
/// FLAG (unimplementable): InvinciblePower and BeatOfDeathPower are not present in the
/// StS2 engine. The vanilla usePreBattleAction applies both (Invincible 300, BeatOfDeath 1);
/// with neither available, the Heart has no per-turn damage cap or on-play damage in this port.
/// The BUFF cycle's BeatOfDeath slot (buffCount % 4 == 1) is also skipped.
/// The "phase mechanism + Shield/Spear summons" noted in the design ticket does not exist in
/// the bytecode — the Heart is a solo boss in vanilla StS1; the Shield/Spear fight is a
/// separate elite encounter.
/// Vanilla HP 300 (design ticket) is incorrect: bytecode calls setHp(750) / setHp(800 at A9).
/// </para>
/// <para>
/// Donor: <c>mawler</c> — the shipped huge maw creature (Maw); largest silhouette available
/// for a final-boss-scale monster; idle_loop/attack/hurt/die tracks fit the default animator.
/// </para>
/// </summary>
public sealed class CorruptHeart : Spire1Monster
{
    // setHp(750); ascension >= 9 -> setHp(800)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 800, 750);

    public override int MaxInitialHp => MinInitialHp;

    // ECHO_ATTACK_DMG = 40; ascension >= 4 -> 45
    private int EchoDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 45, 40);

    // BLOOD_SHOT_DMG = 2 (no ascension variant)
    private const int BloodShotDamage = 2;

    // bloodHitCount: 12; ascension >= 4 -> 15
    private int BloodHitCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);

    // Debuff values for DEBILITATE: Vulnerable 2, Weak 2, Frail 2.
    private const int DebuffAmount = 2;

    // Status card count per type (1 each).
    private const int StatusCount = 1;

    protected override string DonorId => "mawler";

    // Vanilla fields: isFirstMove (starts true), moveCount (starts 0), buffCount (starts 0).
    private bool _firstMove = true;
    private int _moveCount;
    private int _buffCount;

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

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState bloodShots = new("BLOOD_SHOTS_MOVE", BloodShotsMove,
            new MultiAttackIntent(BloodShotDamage, () => BloodHitCount));
        MoveState echo = new("ECHO_ATTACK_MOVE", EchoAttackMove, new SingleAttackIntent(EchoDamage));
        MoveState debilitate = new("DEBILITATE_MOVE", DebilitateMove, new DebuffIntent(strong: true));
        MoveState buff = new("BUFF_MOVE", BuffMove, new BuffIntent());

        ConditionalBranchState branch = new("CORRUPT_HEART_BRANCH");
        bloodShots.FollowUpState = branch;
        echo.FollowUpState = branch;
        debilitate.FollowUpState = branch;
        buff.FollowUpState = branch;

        // getMove: isFirstMove → DEBILITATE; then moveCount%3 0→50/50; 1→last ECHO ? BLOOD_SHOTS : ECHO; 2→BUFF.
        branch.AddState(debilitate, () => _firstMove);
        branch.AddState(bloodShots, () => MoveNum % 3 == 0 ? RollFifty() : (MoveNum % 3 == 1 ? LastWas(echo) : false));
        branch.AddState(echo, () => MoveNum % 3 == 0 ? !RollFifty() : (MoveNum % 3 == 1 ? !LastWas(echo) : false));
        branch.AddState(buff, () => true);
        return new MonsterMoveStateMachine([bloodShots, echo, debilitate, buff, branch], branch);
    }

    private async Task BloodShotsMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn BLOOD_SHOTS: VFX (cosmetic, skipped) + bloodHitCount × DamageAction(damage[1], BLUNT_HEAVY).
        await DamageCmd.Attack(BloodShotDamage).WithHitCount(BloodHitCount).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        _moveCount++;
    }

    private async Task EchoAttackMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn ECHO_ATTACK: VFX (cosmetic, skipped) + DamageAction(damage[0], BLUNT_HEAVY).
        await DamageCmd.Attack(EchoDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        _moveCount++;
    }

    private async Task DebilitateMove(IReadOnlyList<Creature> targets)
    {
        _firstMove = false;
        // takeTurn DEBILITATE: VFX (cosmetic, skipped) + Vulnerable 2 + Weak 2 + Frail 2 +
        // 5 status cards (Dazed, Slimed, Wound, Burn, Void) ×1 each into the draw pile.
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, StatusCount, null);
        await CardPileCmd.AddToCombatAndPreview<MegaCrit.Sts2.Core.Models.Cards.Slimed>(targets, PileType.Draw, StatusCount, null);
        await CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Draw, StatusCount, null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Draw, StatusCount, null);
        await CardPileCmd.AddToCombatAndPreview<Spire1.Spire1Code.Cards.Void>(targets, PileType.Draw, StatusCount, null);
        // Vanilla getMove returns early on the first move WITHOUT incrementing moveCount —
        // DEBILITATE sits outside the %3 cycle.
    }

    private async Task BuffMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn GAIN_ONE_STRENGTH: recover any strength debuff, then +2.
        // Then rotate buff by buffCount: Artifact 2 / BeatOfDeath 1 [MISSING] / PainfulStabs / Strength 10 / Strength 50+.
        _ = targets;
        decimal strengthDebuff = 0;
        StrengthPower? strPower = base.Creature.GetPower<StrengthPower>();
        if (strPower != null && strPower.Amount < 0)
        {
            strengthDebuff = -strPower.Amount;
        }
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, strengthDebuff + 2, base.Creature, null);
        switch (_buffCount)
        {
            case 0:
                await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature, 2, base.Creature, null);
                break;
            case 1:
                // FLAG: vanilla applies BeatOfDeathPower(1) here; the StS2 engine ships no
                // BeatOfDeath power, so this slot is skipped (cadence preserved).
                break;
            case 2:
                await PowerCmd.Apply<PainfulStabsPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1, base.Creature, null);
                break;
            case 3:
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, 10, base.Creature, null);
                break;
            default:
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, 50, base.Creature, null);
                break;
        }
        _buffCount++;
        _moveCount++;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Corrupt Heart",
        [
            ("BLOOD_SHOTS_MOVE", "Blood Shots"),
            ("ECHO_ATTACK_MOVE", "Echo Attack"),
            ("DEBILITATE_MOVE", "Debilitate"),
            ("BUFF_MOVE", "Buff"),
        ]);
}