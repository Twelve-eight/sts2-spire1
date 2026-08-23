using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-2 boss "The Champ" (<c>com.megacrit.cardcrawl.monsters.city.Champ</c>).
/// 官方中文名：第一勇士（<c>.tmp/m25-zhs-names.json</c>）。
/// <para>
/// Bytecode (<c>city_Champ.txt</c>): HP 420 flat (A9+ 440); HEAVY_SLASH 16 (A4+ 18);
/// EXECUTE 10, two hits; FACE_SLAP 12 (A4+ 14) + 2 Frail + 2 Vulnerable; DEFENSIVE_STANCE
/// GainBlock(15; A9+ 18, A19+ 20) + Metallicize(5; A9+ 6, A19+ 7); ANGER Strength(2; A4+ 3,
/// A19+ 4); GLOAT 2 Weak + 2 Vulnerable; LIMIT_BREAK remove all debuffs + Strength(strAmt*3).
/// </para>
/// <para>
/// <c>getMove</c> is a priority chain over one 0-99 roll. First: below half HP and not yet
/// triggered → LIMIT_BREAK (latch <c>thresholdReached</c>). Then, once triggered, EXECUTE on
/// any roll whose last two moves are not EXECUTE (with a death-quote talk). Then, before the
/// threshold: GLOAT on the 4th turn (<c>numTurns</c> resets). Then DEFENSIVE_STANCE while
/// <c>forgeTimes &lt; 2</c> and the roll ≤ 15 (A19+ ≤ 30) — the A19 branch also widens the
/// GLOAT roll band, but GLOAT itself is turn-latched, not rolled. Then ANGER (roll ≤ 30,
/// not last move, not after stance), FACE_SLAP (roll ≤ 55, not last move), and finally
/// HEAVY_SLASH unless it was the last move, else FACE_SLAP.
/// </para>
/// <para>
/// Ascension mapping, same scheme as the other ported bosses: A4's damage/Strength tier →
/// <see cref="AscensionLevel.DeadlyEnemies"/>, A9's HP/forge/block tier →
/// <see cref="AscensionLevel.ToughEnemies"/>, A19's boss tier → <see cref="AscensionLevel.DoubleBoss"/>
/// (topmost, boss-scoped, cumulative with the other two; nested lookups below).
/// </para>
/// <para>
/// Vanilla details intentionally not reproduced: <c>usePreBattleAction</c> (music, BGM
/// unsilence, <c>markBossAsSeen</c>) and <c>die()</c> (shake, VO, boss-victory/unlock calls)
/// are scene/audio/unlock plumbing owned by the encounter and act layers — same call as the
/// other ported bosses (Deca remarks). The first-turn Champion Belt relic dialog is dropped
/// (the relic is not ported). The EXECUTE death quote plays at the start of the move body
/// instead of at roll time, which is the only hook the move state machine exposes.
/// </para>
/// </summary>
public sealed class Champ : Spire1Monster
{
    /// <summary><c>HP = 420</c>, <c>A_9_HP = 440</c> (applied from A9 up).</summary>
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 440, 420);

    public override int MaxInitialHp => MinInitialHp;

    /// <summary><c>SLASH_DMG = 16</c>, <c>A_2_SLASH_DMG = 18</c>.</summary>
    private static int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    /// <summary><c>EXECUTE_DMG = 10</c>, no ascension tier; dealt twice per Execute.</summary>
    private const int ExecuteDamage = 10;

    private const int ExecuteHits = 2;

    /// <summary><c>SLAP_DMG = 12</c>, <c>A_2_SLAP_DMG = 14</c>.</summary>
    private static int SlapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    /// <summary><c>STR_AMT = 2</c>, <c>A_4_STR_AMT = 3</c>, <c>A_19_STR_AMT = 4</c>.</summary>
    private static int StrAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 4,
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2));

    /// <summary><c>FORGE_AMT = 5</c>, <c>A_9_FORGE_AMT = 6</c>, <c>A_19_FORGE_AMT = 7</c>.</summary>
    private static int ForgeAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 7,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 6, 5));

    /// <summary><c>BLOCK_AMT = 15</c>, <c>A_9_BLOCK_AMT = 18</c>, <c>A_19_BLOCK_AMT = 20</c>.</summary>
    private static int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 20,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 15));

    /// <summary><c>DEBUFF_AMT = 2</c> (Frail/Vulnerable on Face Slap, Weak/Vulnerable on Gloat).</summary>
    private const int DebuffAmount = 2;

    /// <summary>
    /// Shipped <c>FlailKnight</c>: an armored knight swinging a heavy flail — the closest shipped
    /// rig to the Champ's golden armor, shield and flail silhouette. Its rig ships
    /// <c>idle_loop</c>/<c>buff</c>/<c>attack_flail</c>/<c>attack_ram</c>/<c>hurt</c>/<c>die</c>
    /// (per <c>FlailKnight.GenerateAnimator</c>), so the engine-default trigger set is remapped
    /// below. Fallback if it reads too lanky in-game: <c>brute_ruby_raider</c> (BanditBear's donor).
    /// </summary>
    protected override string DonorId => "flail_knight";

    /// <summary>
    /// Remaps the engine's default animation triggers onto the tracks this donor rig actually has
    /// (<c>FlailKnight.GenerateAnimator</c> is the authority for the names): Attack → attack_flail
    /// (Heavy Slash / Execute / Face Slap swings), Cast → buff (Anger / Gloat / Limit Break).
    /// </summary>
    public override CreatureAnimator GenerateAnimator(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt",
            attackName: "attack_flail", castName: "buff");

    /// <summary>
    /// Copied from the donor model (<c>FlailKnight.TakeDamageSfxType</c>): the generic hit sound
    /// bank is chosen by this enum rather than by a derived path, so the Champ takes clanking
    /// armor hits instead of the default flesh ones.
    /// </summary>
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    // Vanilla fields: numTurns (GLOAT cadence), forgeTimes / forgeThreshold = 2 (stance budget),
    // thresholdReached (half-HP latch). firstTurn exists only to gate the dropped relic dialog.
    private int _numTurns;

    private int _forgeTimes;

    private const int ForgeThreshold = 2;

    private bool _thresholdReached;

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

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // Vanilla !lastMove(EXECUTE) && !lastMoveBefore(EXECUTE): neither of the last two moves was it.
    private bool NeitherOfLastTwoWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return !(log.Count > 0 && ReferenceEquals(log[^1], state))
            && !(log.Count >= 2 && ReferenceEquals(log[^2], state));
    }

    // Vanilla picks a quote with MathUtils.random (unseeded cosmetic draw); the AI rng is fine.
    private LocString PickQuote(string keyBase, int variants) =>
        MonsterModel.L10NMonsterLookup("SPIRE1-CHAMP.moves." + keyBase + (base.Rng.NextInt(variants) + 1));

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState heavySlash = new("HEAVY_SLASH_MOVE", HeavySlashMove, new SingleAttackIntent(SlashDamage));
        MoveState defensiveStance = new("DEFENSIVE_STANCE_MOVE", DefensiveStanceMove,
            new DefendIntent(), new BuffIntent());
        MoveState execute = new("EXECUTE_MOVE", ExecuteMove, new MultiAttackIntent(ExecuteDamage, ExecuteHits));
        MoveState faceSlap = new("FACE_SLAP_MOVE", FaceSlapMove,
            new SingleAttackIntent(SlapDamage), new DebuffIntent());
        MoveState anger = new("ANGER_MOVE", AngerMove, new BuffIntent());
        MoveState gloat = new("GLOAT_MOVE", GloatMove, new DebuffIntent());
        MoveState limitBreak = new("LIMIT_BREAK_MOVE", LimitBreakMove, new BuffIntent());

        // Every move cycles back through the priority chain — bytecode getMove, in order.
        ConditionalBranchState branch = new("CHAMP_BRANCH");
        heavySlash.FollowUpState = branch;
        defensiveStance.FollowUpState = branch;
        execute.FollowUpState = branch;
        faceSlap.FollowUpState = branch;
        anger.FollowUpState = branch;
        gloat.FollowUpState = branch;
        limitBreak.FollowUpState = branch;

        // getMove: currentHealth < maxHealth / 2 (integer division, strict) forces LIMIT_BREAK once.
        branch.AddState(limitBreak, () => !_thresholdReached && base.Creature.CurrentHp < base.Creature.MaxHp / 2);
        // Then EXECUTE on any roll whose last two moves are not EXECUTE.
        branch.AddState(execute, () => _thresholdReached && NeitherOfLastTwoWas(execute));
        // Pre-threshold, the 4th turn (numTurns == 4) is GLOAT, overriding the roll.
        branch.AddState(gloat, () => !_thresholdReached && _numTurns == 4);
        // DEFENSIVE_STANCE: twice per fight, not twice in a row, roll <= 15 (A19+ <= 30).
        branch.AddState(defensiveStance, () => !LastWas(defensiveStance) && _forgeTimes < ForgeThreshold
            && RollHundred() <= (AscensionHelper.HasAscension(AscensionLevel.DoubleBoss) ? 30 : 15));
        // ANGER: roll <= 30, not after Anger and not after a stance.
        branch.AddState(anger, () => !LastWas(anger) && !LastWas(defensiveStance) && RollHundred() <= 30);
        // FACE_SLAP: roll <= 55, not twice in a row.
        branch.AddState(faceSlap, () => !LastWas(faceSlap) && RollHundred() <= 55);
        // Fallback: HEAVY_SLASH unless it was the last move, then FACE_SLAP.
        branch.AddState(heavySlash, () => !LastWas(heavySlash));
        branch.AddState(faceSlap, () => true);

        return new MonsterMoveStateMachine(
            [heavySlash, defensiveStance, execute, faceSlap, anger, gloat, limitBreak, branch], branch);
    }

    private async Task HeavySlashMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn HEAVY_SLASH: ChangeState("ATTACK") + golden slash + damage[0].
        await DamageCmd.Attack(SlashDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        CountTurn();
    }

    private async Task DefensiveStanceMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn DEFENSIVE_STANCE: GainBlockAction(blockAmt) + MetallicizePower(forgeAmt).
        // Vanilla bumps forgeTimes inside getMove's chosen branch; performing the bump in the move
        // body is equivalent (the next roll always happens after this body ran).
        await CreatureCmd.GainBlock(base.Creature, BlockAmount, ValueProp.Move, null);
        await PowerCmd.Apply<MetallicizePower>(new ThrowingPlayerChoiceContext(), base.Creature, ForgeAmount, base.Creature, null);
        _forgeTimes++;
        CountTurn();
    }

    private async Task ExecuteMove(IReadOnlyList<Creature> targets)
    {
        // Vanilla queues the death-quote TalkAction addToTop at roll time (getMove); the move body
        // is the only hook the state machine exposes, so the quote plays as the move starts.
        TalkCmd.Play(PickQuote("EXECUTE_MOVE.deathQuote", 2), base.Creature, VfxColor.Gold, VfxDuration.Standard);
        // AnimateJump + Wait(0.5) wind-up beat from the vanilla turn.
        await Cmd.CustomScaledWait(0.25f, 0.5f);
        // takeTurn EXECUTE: two golden slashes, each dealing damage[1].
        await DamageCmd.Attack(ExecuteDamage).WithHitCount(ExecuteHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        CountTurn();
    }

    private async Task FaceSlapMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn FACE_SLAP: damage[2] (BLUNT_LIGHT) + 2 Frail + 2 Vulnerable on the player.
        await DamageCmd.Attack(SlapDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        CountTurn();
    }

    private async Task AngerMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn ANGER: StrengthPower(strAmt) on self.
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, StrAmount, base.Creature, null);
        CountTurn();
    }

    private async Task GloatMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn GLOAT: taunt talk + 2 Weak + 2 Vulnerable on the player.
        _numTurns = 0;
        TalkCmd.Play(PickQuote("GLOAT_MOVE.taunt", 4), base.Creature, VfxColor.Gold, VfxDuration.Standard);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DebuffAmount, base.Creature, null);
    }

    private async Task LimitBreakMove(IReadOnlyList<Creature> targets)
    {
        // getMove latches thresholdReached when the half-HP branch fires; the latch lives here so
        // branch predicates stay side-effect free (BookOfStabbing's re-evaluation caveat).
        _thresholdReached = true;
        // takeTurn LIMIT_BREAK: charge shout + 3x InflameEffect (Cast anim stands in), then
        // RemoveDebuffsAction. Vanilla also removes "Shackled" specifically; nothing else is needed
        // here because the debuff sweep below already removes every debuff-type power, and the StS1
        // Shackled power is not ported.
        TalkCmd.Play(PickQuote("LIMIT_BREAK_MOVE.limitBreak", 2), base.Creature, VfxColor.Gold, VfxDuration.VeryLong);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.75f);
        foreach (PowerModel debuff in base.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList())
        {
            await PowerCmd.Remove(debuff);
        }
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, StrAmount * 3, base.Creature, null);
        CountTurn();
    }

    /// <summary>
    /// Vanilla increments numTurns at the top of every getMove; counting performed moves here is
    /// equivalent (each pre-threshold turn performs exactly one move). GloatMove resets instead.
    /// </summary>
    private void CountTurn() => _numTurns++;

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move titles and
    // dialog lines follow the same localization style (BanditLeader precedent for in-code lines).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("The Champ", "第一勇士"),
        [
            ("HEAVY_SLASH_MOVE", Tr("Heavy Slash", "重斩")),
            ("DEFENSIVE_STANCE_MOVE", Tr("Defensive Stance", "防御姿态")),
            ("EXECUTE_MOVE", Tr("Execute", "处决")),
            ("FACE_SLAP_MOVE", Tr("Face Slap", "掌掴")),
            ("ANGER_MOVE", Tr("Anger", "愤怒")),
            ("GLOAT_MOVE", Tr("Gloat", "得意洋洋")),
            ("LIMIT_BREAK_MOVE", Tr("Limit Break", "极限突破")),
        ],
        ("moves.GLOAT_MOVE.taunt1", Tr("You call that a weapon?", "你就管那玩意叫武器？")),
        ("moves.GLOAT_MOVE.taunt2", Tr("Come at me!", "放马过来！")),
        ("moves.GLOAT_MOVE.taunt3", Tr("Do your worst! NL @HAHAHA!@", "尽管出手！NL @哈哈哈！@")),
        ("moves.GLOAT_MOVE.taunt4", Tr("Have a free shot! NL Futile weakling!", "送你一次免费出手的机会！NL 徒劳的弱者！")),
        ("moves.LIMIT_BREAK_MOVE.limitBreak1", Tr("~You've~ ~done~ ~it~ ~now...~", "你这下闯大祸了……")),
        ("moves.LIMIT_BREAK_MOVE.limitBreak2", Tr("@DEFEAT??@ NL @IMPOSSIBLE!!@", "失败？？NL 不可能！！")),
        ("moves.EXECUTE_MOVE.deathQuote1", Tr("~DIE~ ~.~ ~.~ ~.~", "去死吧……")),
        ("moves.EXECUTE_MOVE.deathQuote2", Tr("Face my wrath!", "承受我的怒火！")));
}
