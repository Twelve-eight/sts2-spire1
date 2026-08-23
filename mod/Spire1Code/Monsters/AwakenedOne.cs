using System.Collections.Generic;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Powers;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Spire1.Spire1Code.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Models.Powers;
 
        // Remove every debuff (vanilla removes DEBUFF-type powers plus Curiosity/Unawakened/Shackled).

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-3 boss "Awakened One" (<c>com.megacrit.cardcrawl.monsters.beyond.AwakenedOne</c>).
/// 官方中文名：觉醒者。
/// <para>
/// Bytecode: HP 300, A9 320; SLASH_DMG 20, SS_DMG 6 ×4, ECHO_DMG 40, SLUDGE_DMG 18 + 1 MegaCrit.Sts2.Core.Models.Cards.Void
/// (draw pile), TACKLE_DMG 10 ×3. usePreBattleAction: Regen 10 (A19 15), custom
/// <see cref="CuriosityPower"/> 1 (A19 2), Unawakened (display-only, omitted), Strength 2 from
/// <see cref="AscensionLevel.DeadlyEnemies"/> (vanilla A4).
/// </para>
/// <para>
/// Two-phase boss. Form 1 (firstTurn → SLASH, then last-move guards over SLASH/Soul Strike);
/// at 0 HP <c>damage()</c> flips <c>halfDead</c>, strips its debuffs, forces the REBIRTH intent
/// and switches <c>form1 = false</c>; the REBIRTH turn then heals to full and form 2 opens with
/// DARK_ECHO, alternating Sludge (18 + MegaCrit.Sts2.Core.Models.Cards.Void) and Tackle (3×10) under the same last-move guards.
/// The whole handoff is driven from <see cref="AfterDamageReceived"/> — with the engine's
/// <c>SetMoveImmediate</c> (same idiom as SlimeBoss' split) — and the boss is healed to 1 HP
/// on the killing blow so the engine never sees it dead before REBIRTH resolves.
/// </para>
/// <para>
/// Donor: <c>owl_magistrate</c> — a large winged boss-scale creature; closest visual match for
/// the crow-like Awakened One.
/// </para>
/// </summary>
public sealed class AwakenedOne : Spire1Monster
{
    // HP 300, A9 → 320
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);
    public override int MaxInitialHp => MinInitialHp;

    // SLASH_DMG = 20 (no ascension variant)
    private const int SlashDamage = 20;

    // SS_DMG = 6 ×4 (no ascension variant)
    private const int SoulStrikeDamage = 6;
    private const int SoulStrikeHits = 4;

    // ECHO_DMG = 40 (no ascension variant)
    private const int DarkEchoDamage = 40;

    // SLUDGE_DMG = 18 + 1 MegaCrit.Sts2.Core.Models.Cards.Void (no ascension variant)
    private const int SludgeDamage = 18;
    private const int SludgeVoidCount = 1;

    // TACKLE_DMG = 10 ×3 (no ascension variant)
    private const int TackleDamage = 10;
    private const int TackleHits = 3;

    // usePreBattleAction: Regen 10 (A19 15), Curiosity 1 (A19 2), Strength 2 (A4).
    private int RegenAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 15, 10);
    private int CuriosityAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 2, 1);
    private const int StrengthAmount = 2;

    protected override string DonorId => "owl_magistrate";

    // Vanilla fields: form1 (starts true), firstTurn (starts true), halfDead, saidPower.
    private bool _form1 = true;
    private bool _firstTurn = true;
    private bool _halfDead;

    private MoveState? _rebirthState;

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Awakened One",
        [
            ("SLASH_MOVE", "Slash"),
            ("SOUL_STRIKE_MOVE", "Soul Strike"),
            ("REBIRTH_MOVE", "Rebirth"),
            ("DARK_ECHO_MOVE", "Dark Echo"),
            ("SLUDGE_MOVE", "Sludge"),
            ("TACKLE_MOVE", "Tackle"),
        ],
        ("moves.REBIRTH_LINE", "~Grrraaah...~"));

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState slash = new("SLASH_MOVE", SlashMove, new SingleAttackIntent(SlashDamage));
        MoveState soulStrike = new("SOUL_STRIKE_MOVE", SoulStrikeMove,
            new MultiAttackIntent(SoulStrikeDamage, SoulStrikeHits));
        // REBIRTH: vanilla intent is UNKNOWN; forced from the damage hook, so it must survive the
        // next roll — same MustPerformOnceBeforeTransitioning idiom as SlimeBoss' split state.
        MoveState rebirth = new("REBIRTH_MOVE", RebirthMove, new UnknownIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState darkEcho = new("DARK_ECHO_MOVE", DarkEchoMove, new SingleAttackIntent(DarkEchoDamage));
        MoveState sludge = new("SLUDGE_MOVE", SludgeMove,
            new SingleAttackIntent(SludgeDamage), new StatusIntent(SludgeVoidCount));
        MoveState tackle = new("TACKLE_MOVE", TackleMove, new MultiAttackIntent(TackleDamage, TackleHits));

        // Form 1 picker: firstTurn → SLASH; turn < 25 → last Soul Strike ? SLASH : Soul Strike;
        // else last two SLASH ? Soul Strike : SLASH.
        ConditionalBranchState form1 = new("AWAKENED_FORM_1");
        // Form 2 picker: firstTurn → DARK_ECHO; turn < 50 → last two Sludge ? Tackle : Sludge;
        // else last two Tackle ? Sludge : Tackle.
        ConditionalBranchState form2 = new("AWAKENED_FORM_2");
        ConditionalBranchState main = new("AWAKENED_MAIN");

        slash.FollowUpState = main;
        soulStrike.FollowUpState = main;
        rebirth.FollowUpState = main;
        darkEcho.FollowUpState = main;
        sludge.FollowUpState = main;
        tackle.FollowUpState = main;

        // Form 1: firstTurn → SLASH; roll<25 → last Soul Strike ? SLASH : Soul Strike;
        // else → last two SLASH ? Soul Strike : SLASH.
        form1.AddState(slash, () => _firstTurn || (RollHundred() < 25 ? LastWas(soulStrike) : !LastTwoWere(slash)));
        form1.AddState(soulStrike, () => true);
        // Form 2: firstTurn → DARK_ECHO; roll<50 → last two Sludge ? Tackle : Sludge;
        // else → last two Tackle ? Sludge : Tackle.
        form2.AddState(darkEcho, () => _firstTurn);
        form2.AddState(tackle, () => RollHundred() < 50 ? LastTwoWere(sludge) : !LastTwoWere(tackle));
        form2.AddState(sludge, () => true);

        main.AddState(form1, () => _form1);
        main.AddState(form2, () => true);

        _rebirthState = rebirth;
        return new MonsterMoveStateMachine(
            [slash, soulStrike, rebirth, darkEcho, sludge, tackle, form1, form2, main],
            main);
    }

    private int Turn => base.Creature?.CombatState?.RoundNumber ?? 0;

    private int? _roll;
    private int _rollTurn = -1;

    // One cached 0-99 roll per round — vanilla getMove takes a single random(100) per call.
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

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        _firstTurn = false;
        await DamageCmd.Attack(SlashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task SoulStrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SoulStrikeDamage).WithHitCount(SoulStrikeHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    /// <summary>
    /// <c>changeState("REBIRTH")</c>: re-assert the max HP (A9 tier), heal to full, and re-enable
    /// losing the fight. Vanilla also restarts the flame particles — cosmetic, omitted.
    /// </summary>
    private async Task RebirthMove(IReadOnlyList<Creature> targets)
    {
        _halfDead = false;
        await CreatureCmd.Heal(Creature, Creature.MaxHp);
    }

    private async Task DarkEchoMove(IReadOnlyList<Creature> targets)
    {
        _firstTurn = false;
        await DamageCmd.Attack(DarkEchoDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task SludgeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SludgeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        // MakeTempCardInDrawPileAction(VoidCard, 1, randomSpot: true, autoPosition: true).
        await CardPileCmd.AddToCombatAndPreview<MegaCrit.Sts2.Core.Models.Cards.Void>(targets, PileType.Draw, SludgeVoidCount, null);
    }

    private async Task TackleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TackleDamage).WithHitCount(TackleHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    /// <summary>
    /// <c>AwakenedOne.damage()</c>: the killing blow flips to the REBIRTH sequence instead of
    /// dying — vanilla gates this on the room's <c>cannotLose</c> flag, i.e. the Awakened One
    /// encounter. Heal to 1 HP here so the engine never observes the creature dead, strip every
    /// debuff (vanilla also removes Curiosity/Unawakened/Shackled; Unawakened is not ported),
    /// switch to form 2, and force the REBIRTH intent for the next turn. The rebirth turn itself
    /// heals to full via <see cref="RebirthMove"/>.
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Creature || _halfDead || Creature.CurrentHp > 0 || _rebirthState == null)
            return;
        _halfDead = true;

        // Remove every debuff (vanilla removes DEBUFF-type powers plus Curiosity/Unawakened/Shackled).
        foreach (var power in base.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList())
        {
            await PowerCmd.Remove(power);
        }
        var curiosity = base.Creature.GetPower<CuriosityPower>();
        if (curiosity != null)
        {
            await PowerCmd.Remove(curiosity);
        }

        // Keep the creature alive until REBIRTH resolves (vanilla: room.cannotLose).
        await CreatureCmd.Heal(Creature, 1m);

        _form1 = false;
        _firstTurn = true;
        SetMoveImmediate(_rebirthState, forceTransition: true);
    }

    public override async Task BeforeCombatStart()
    {
        // usePreBattleAction: unsilenceBGM/playBgmInstantly/markBossAsSeen skipped (StS1-only
        // audio/unlock calls). UnawakenedPower is display-only in vanilla too, so it is omitted.
        await PowerCmd.Apply<RegenPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, RegenAmount, base.Creature, null);
        await PowerCmd.Apply<CuriosityPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, CuriosityAmount, base.Creature, null);
        if (AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies))
        {
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), base.Creature, StrengthAmount, base.Creature, null);
        }
    }
}
