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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Mystic (<c>com.megacrit.cardcrawl.monsters.city.Healer</c>; the vanilla class
/// id is <c>Healer</c>, the display name "Mystic"). 官方中文名：神秘术士。
/// <para>
/// Bytecode: HP 48-56, A7 50-58; magicDmg 8 (A2/A17 9), strAmt 2 (A2 3, A17 4), healAmt 16
/// (A17 20). getMove: healPotential = Σ(maxHealth − currentHealth) over monsters neither dying
/// nor escaping; healPotential &gt; (A17 ? 20 : 15) &amp;&amp; !lastTwoMoves(HEAL) → HEAL;
/// else roll &gt; 40 &amp;&amp; !(A17 ? lastMove(ATTACK) : lastTwoMoves(ATTACK)) → ATTACK;
/// else !lastTwoMoves(BUFF) → BUFF; else ATTACK. takeTurn: ATTACK = slow hit + Frail 2;
/// HEAL = HealAction(healAmt) on every living non-escaping monster (self included);
/// BUFF = StrengthPower(strAmt) on the same pool.
/// </para>
/// <para>
/// Ascension mapping: HP A7 tier → <see cref="AscensionLevel.ToughEnemies"/>; damage A2 tier and
/// the A17 tiers (strAmt 4 / healAmt 20 / the single-attack guard) map onto
/// <see cref="AscensionLevel.DeadlyEnemies"/> like GremlinNob's A18 branch; the intermediate
/// strAmt 3 tier rides ToughEnemies so all three vanilla values stay reachable.
/// </para>
/// <para>
/// Donor: <c>kin_priest</c> — the shipped robed staff-casting priest; closest healer archetype
/// among the shipped scenes.
/// </para>
/// </summary>
public sealed class Healer : Spire1Monster
{

    protected override string DonorId => "kin_priest";
    // setHp(48, 56); ascension >= 7 -> setHp(50, 58)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 58, 56);

    // magicDmg = 8; ascension >= 2 -> 9 (A17 stays 9)
    private int MagicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

    // strAmt = 2; ascension >= 2 -> 3; ascension >= 17 -> 4. The two shipped enemy-scaling
    // levers split the span: mid tier -> ToughEnemies, top tier -> DeadlyEnemies.
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 3, 2));

    // healAmt = 16; ascension >= 17 -> 20
    private int HealAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 16);

    private const int FrailTurns = 2;

    /// <summary>StS1 A17+ tightens the attack-repeat guard and the heal threshold.</summary>
    private static bool IsHardMode => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attack = new("ATTACK_MOVE", AttackMove, new SingleAttackIntent(MagicDamage), new DebuffIntent());
        MoveState heal = new("HEAL_MOVE", HealMove, new BuffIntent());
        MoveState buff = new("BUFF_MOVE", BuffMove, new BuffIntent());

        ConditionalBranchState branch = new("MYSTIC_BRANCH");
        attack.FollowUpState = branch;
        heal.FollowUpState = branch;
        buff.FollowUpState = branch;

        // Bytecode priority chain: heal when allies are missing enough HP, then the 60% attack
        // band with its repeat guard, then buff unless repeated, else attack.
        branch.AddState(heal, () => MissingAllyHp() > (IsHardMode ? 20 : 15) && !LastTwoWere(heal));
        branch.AddState(attack, () => RollHundred() > 40 && !(IsHardMode ? LastWas(attack) : LastTwoWere(attack)));
        branch.AddState(buff, () => !LastTwoWere(buff));
        branch.AddState(attack, () => true);

        return new MonsterMoveStateMachine([attack, heal, buff, branch], attack);
    }

    /// <summary>Vanilla sums maxHealth − currentHealth over monsters not dying and not escaping.</summary>
    private int MissingAllyHp() => CombatState.Enemies
        .Where(c => c.IsAlive && !IntendsToEscape(c))
        .Sum(c => (int)(c.MaxHp - c.CurrentHp));

    private static bool IntendsToEscape(Creature creature) =>
        creature.Monster?.NextMove.Intents.Any(intent => intent is EscapeIntent) ?? false;

    // takeTurn ATTACK: AnimateSlowAttackAction + SLASH_DIAGONAL hit + ApplyPowerAction(Frail, 2).
    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MagicDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, FrailTurns, base.Creature, null);
    }

    // takeTurn HEAL (byte 2): STAFF_RAISE + HealAction(healAmt) on every living ally.
    private async Task HealMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        foreach (Creature ally in LivingAllies())
        {
            await CreatureCmd.Heal(ally, HealAmount);
        }
    }

    // takeTurn BUFF (byte 3): STAFF_RAISE + StrengthPower(strAmt) on every living ally.
    private async Task BuffMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        foreach (Creature ally in LivingAllies())
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), ally, StrengthAmount,
                base.Creature, null);
        }
    }

    private List<Creature> LivingAllies() => CombatState.Enemies
        .Where(c => c.IsAlive && !IntendsToEscape(c))
        .ToList();

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
        new MonsterLoc(Tr("Mystic", "神秘术士"),
        [
            ("ATTACK_MOVE", Tr("Attack", "攻击")),
            ("HEAL_MOVE", Tr("Heal", "治疗")),
            ("BUFF_MOVE", Tr("Buff", "增益")),
        ]);
}
