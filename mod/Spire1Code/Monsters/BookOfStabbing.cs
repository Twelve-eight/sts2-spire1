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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Book of Stabbing (<c>com.megacrit.cardcrawl.monsters.city.BookOfStabbing</c>).
/// 官方中文名：扎人的书。
/// <para>
/// Bytecode: HP 160-164, A8 168-172; STAB_DMG 6 (A3 7), BIG_STAB_DMG 21 (A3 24); stabCount
/// starts at 1. usePreBattleAction: ApplyPowerAction(PainfulStabsPower) — the shipped
/// <see cref="PainfulStabsPower"/> carries the same keyword and Wound-per-unblocked-hit effect,
/// so it is applied verbatim. getMove(num): num&lt;15 → lastMove(BIG_STAB) ? stabCount++ +
/// STAB×stabCount : BIG_STAB (+A18 stabCount++); else → lastTwoMoves(STAB) ? BIG_STAB :
/// stabCount++ + STAB×stabCount. takeTurn STAB: one DamageAction per hit
/// (SLASH_VERTICAL); BIG_STAB: single hit. Consecutive limits: STAB at most twice in a row
/// (lastTwoMoves guard), BIG_STAB never twice in a row (lastMove guard).
/// </para>
/// <para>
/// Ascension mapping: HP A8 tier → <see cref="AscensionLevel.ToughEnemies"/>; damage A3 tiers →
/// <see cref="AscensionLevel.DeadlyEnemies"/>; the A18 "Big Stab grows the next Stab" behavioural
/// tier maps onto DeadlyEnemies like GremlinNob's deterministic branch.
/// </para>
/// <para>
/// Donor: <c>scroll_of_biting</c> — the shipped animated biting scroll; closest visual match for
/// a floating, attacking tome among the shipped scenes.
/// </para>
/// </summary>
public sealed class BookOfStabbing : Spire1Monster
{

    protected override string DonorId => "scroll_of_biting";
    // setHp(160, 164); ascension >= 8 -> setHp(168, 172)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 168, 160);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 172, 164);

    // stabDmg = 6; ascension >= 3 -> 7
    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    // bigStabDmg = 21; ascension >= 3 -> 24
    private int BigStabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 21);

    // Vanilla field stabCount = 1; every rolled Multi Stab grows the next one (see GrowStab).
    private int _stabCount = 1;

    private int _bumpedRound = -1;

    /// <summary>StS1 A18+ also grows stabCount when Big Stab is rolled; mapped onto DeadlyEnemies.</summary>
    private static bool IsHardMode => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);

    /// <summary>
    /// Vanilla bumps stabCount inside getMove, i.e. once per RollMove before the intent renders
    /// (MultiAttackIntent's dynamic repeat shows it immediately). The round-keyed guard keeps the
    /// bump idempotent if the engine re-evaluates branch predicates.
    /// </summary>
    private bool GrowStab()
    {
        int round = Creature?.CombatState?.RoundNumber ?? 0;
        if (_bumpedRound != round)
        {
            _bumpedRound = round;
            _stabCount++;
        }
        return true;
    }

    private bool GrowStabIfHardMode() => !IsHardMode || GrowStab();

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new PainfulStabsPower(this)) — shipped power, stack 1.
        await PowerCmd.Apply<PainfulStabsPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState stab = new("STAB_MOVE", StabMove, new MultiAttackIntent(StabDamage, () => _stabCount));
        MoveState bigStab = new("BIG_STAB_MOVE", BigStabMove, new SingleAttackIntent(BigStabDamage));

        ConditionalBranchState branch = new("BOOK_OF_STABBING_BRANCH");
        stab.FollowUpState = branch;
        bigStab.FollowUpState = branch;

        // Bytecode getMove bands: roll < 15 (15%) prefers Big Stab unless it just used it; the
        // remaining 85% rolls Multi Stab unless it already did so twice in a row. Each selected
        // branch performs vanilla's stabCount growth at selection time via its predicate tail.
        branch.AddState(stab, () => RollHundred() < 15 && LastWas(bigStab) && GrowStab());
        branch.AddState(bigStab, () => RollHundred() < 15 && !LastWas(bigStab) && GrowStabIfHardMode());
        branch.AddState(bigStab, () => LastTwoWere(stab) && GrowStabIfHardMode());
        branch.AddState(stab, () => GrowStab());

        return new MonsterMoveStateMachine([stab, bigStab, branch], branch);
    }

    // takeTurn STAB: ChangeState ATTACK + one SLASH_VERTICAL hit per stabCount.
    private async Task StabMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StabDamage).WithHitCount(_stabCount).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // takeTurn BIG_STAB: ChangeState ATTACK_2 + one SLASH_VERTICAL hit.
    private async Task BigStabMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BigStabDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
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
    // follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Book of Stabbing", "扎人的书"),
        [
            ("STAB_MOVE", Tr("Multi Stab", "扎击")),
            ("BIG_STAB_MOVE", Tr("Big Stab", "重扎")),
        ]);
}
