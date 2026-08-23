using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Gremlin Leader elite (<c>com.megacrit.cardcrawl.monsters.city.GremlinLeader</c>).
/// 官方中文名：地精首领。
/// <para>
/// Bytecode: HP 140-148, A8 145-155; STAB_DMG 6, STAB_AMT 3 (fixed); strAmt 3 / blockAmt 6,
/// A3+ strAmt 4, A18+ strAmt 5 / blockAmt 10. usePreBattleAction fills <c>gremlins[0..1]</c>
/// with the two encounter-spawned minions and applies MinionPower to each (visual marker —
/// not ported; the engine derives primary-ness itself, see <see cref="TheCollector"/> remarks).
/// </para>
/// <para>
/// getMove is gated on <c>numAliveGremlins()</c> (living non-self monsters): 0 alive → summon
/// (RALLY); 1 alive → mixed; 2+ → no summoning, encourage/stab only. Recursive rerolls
/// (<c>getMove(aiRng.random(...))</c>) are approximated by falling through to the next band,
/// the established Darkling/JawWorm precedent.
/// </para>
/// <para>
/// RALLY (byte 2, UNKNOWN intent): two <c>SummonGremlinAction</c> calls, each filling the first
/// empty gremlin slot with one randomly picked gremlin. The summon pool — Fat / Sneaky /
/// Shield / Warrior — follows the City gremlin pack (<c>SummonGremlinAction</c> is not in the
/// local dumps; the roll is uniform over 4 types, aiRng.random(3), and Wizard/Tsundere are not
/// part of the pack). Slot tracking mirrors <see cref="TheCollector"/>'s minion-slot map.
/// ENCOURAGE (byte 3, DEFEND_BUFF): the leader buffs herself with Strength only, and every
/// other living monster with Strength + Block. STAB (byte 4, ATTACK): three fast slashes of
/// STAB_DMG.
/// </para>
/// <para>
/// die(): every surviving gremlin escapes (EscapeAction per survivor; the DIALOG shouts are
/// cosmetic and omitted). Ascension mapping: A8 HP tier → ToughEnemies, A3 str tier →
/// DeadlyEnemies, A18 tier → DoubleBoss (top tier, TheCollector A19 precedent).
/// Donor: <c>gremlin_merc</c> — the shipped battle-gremlin rig (idle_loop/attack_single/
/// attack_double/hurt/die); the attack trigger is remapped onto attack_single.
/// </para>
/// </summary>
public sealed class GremlinLeader : Spire1Monster
{
    // setHp(140, 148); ascension >= 8 -> setHp(145, 155)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 145, 140);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 155, 148);

    // STAB_DMG = 6 / STAB_AMT = 3 (no ascension variants)
    private const int StabDamage = 6;

    private const int StabAmount = 3;

    // strAmt 3, A3+ 4, A18+ 5; blockAmt 6, A18+ 10.
    private int StrAmt => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 5,
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3));

    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 10, 6);

    // Vanilla gremlins[3] slot array: [encounter minion 0, encounter minion 1, null].
    private readonly Dictionary<int, Creature> _minionSlots = new();

    protected override string DonorId => "gremlin_merc";

    /// <summary>
    /// The gremlin_merc rig ships attack_single/attack_double (no plain "attack" track), so the
    /// engine-default animator would silently drop the attack animation; remap it.
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack_single");

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: gremlins[0..1] = the two encounter-spawned minions (slot 2 empty).
        int slot = 0;
        foreach (Creature enemy in base.CombatState.Enemies)
        {
            if (ReferenceEquals(enemy, base.Creature) || !enemy.IsAlive || slot >= 2)
            {
                continue;
            }
            _minionSlots[slot++] = enemy;
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState rally = new("RALLY_MOVE", RallyMove, new UnknownIntent());
        MoveState encourage = new("ENCOURAGE_MOVE", EncourageMove, new DefendIntent(), new BuffIntent());
        MoveState stab = new("STAB_MOVE", StabMove, new MultiAttackIntent(StabDamage, StabAmount));

        ConditionalBranchState top = new("GREMLIN_LEADER_TOP");
        ConditionalBranchState noneAlive = new("GREMLIN_LEADER_NONE_ALIVE");
        ConditionalBranchState oneAlive = new("GREMLIN_LEADER_ONE_ALIVE");
        ConditionalBranchState packAlive = new("GREMLIN_LEADER_PACK_ALIVE");

        rally.FollowUpState = top;
        encourage.FollowUpState = top;
        stab.FollowUpState = top;

        top.AddState(noneAlive, () => AliveGremlins() == 0);
        top.AddState(oneAlive, () => AliveGremlins() == 1);
        top.AddState(packAlive, () => true);

        // numAliveGremlins() == 0: r < 75 && !last(RALLY) -> RALLY; r < 75 -> STAB;
        // r >= 75 && !last(STAB) -> STAB; else RALLY.
        noneAlive.AddState(rally, () => RollHundred() < 75 && !LastWas(rally));
        noneAlive.AddState(stab, () => RollHundred() < 75);
        noneAlive.AddState(rally, () => !LastWas(stab));
        noneAlive.AddState(stab, () => true);

        // numAliveGremlins() == 1: r < 50 && !last(RALLY) -> RALLY; r < 80 && !last(ENCOURAGE)
        // -> ENCOURAGE; (r < 80 && last(ENCOURAGE)) || (r >= 80 && !last(STAB)) -> STAB;
        // r >= 80 && last(STAB) -> ENCOURAGE (vanilla rerolls 0-80; approximated).
        oneAlive.AddState(rally, () => RollHundred() < 50 && !LastWas(rally));
        oneAlive.AddState(encourage, () => RollHundred() < 80 && !LastWas(encourage));
        oneAlive.AddState(stab, () => RollHundred() < 80 || !LastWas(stab));
        oneAlive.AddState(encourage, () => true);

        // numAliveGremlins() >= 2: r < 66 && !last(ENCOURAGE) -> ENCOURAGE;
        // (r < 66 && last(ENCOURAGE)) || (r >= 66 && !last(STAB)) -> STAB; else ENCOURAGE.
        packAlive.AddState(encourage, () => RollHundred() < 66 && !LastWas(encourage));
        packAlive.AddState(stab, () => RollHundred() < 66 || !LastWas(stab));
        packAlive.AddState(encourage, () => true);

        return new MonsterMoveStateMachine([rally, encourage, stab, noneAlive, oneAlive, packAlive, top], top);
    }

    private int AliveGremlins()
    {
        int count = 0;
        foreach (Creature enemy in base.CombatState.Enemies)
        {
            if (!ReferenceEquals(enemy, base.Creature) && enemy.IsAlive)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>takeTurn RALLY: two SummonGremlinActions, each one gremlin into the first empty slot.</summary>
    private async Task RallyMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        for (int i = 0; i < 2; i++)
        {
            if (!await TrySummonOne())
            {
                break;
            }
        }
    }

    private async Task<bool> TrySummonOne()
    {
        int slot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (!_minionSlots.TryGetValue(i, out Creature? occupant) || occupant == null
                || !occupant.IsAlive)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0)
        {
            return false;
        }
        // Vanilla SummonGremlinAction: aiRng.random(3) over the City pack quartet.
        Spire1Monster model = base.Rng.NextInt(4) switch
        {
            0 => (Spire1Monster)ModelDb.Monster<GremlinFat>().ToMutable(),
            1 => (Spire1Monster)ModelDb.Monster<GremlinThief>().ToMutable(),
            2 => (Spire1Monster)ModelDb.Monster<GremlinShield>().ToMutable(),
            _ => (Spire1Monster)ModelDb.Monster<GremlinWarrior>().ToMutable(),
        };
        Creature minion = await CreatureCmd.Add(model, CombatState, base.Creature.Side);
        _minionSlots[slot] = minion;
        return true;
    }

    /// <summary>
    /// takeTurn ENCOURAGE: ShoutAction (quote — cosmetic, omitted); the leader gets Strength,
    /// every other living monster gets Strength + Block.
    /// </summary>
    private async Task EncourageMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        foreach (Creature ally in base.CombatState.Enemies.Where(c => c.IsAlive))
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), ally, StrAmt, base.Creature, null);
            if (!ReferenceEquals(ally, base.Creature))
            {
                await CreatureCmd.GainBlock(ally, BlockAmount, ValueProp.Move, null);
            }
        }
    }

    private async Task StabMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn STAB: three DamageActions (SLASH_HORIZONTAL / SLASH_VERTICAL / SLASH_HEAVY).
        await DamageCmd.Attack(StabDamage).WithHitCount(StabAmount).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>die(): the surviving gremlins escape instead of fighting on.</summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature)
        {
            return;
        }
        foreach (Creature survivor in base.CombatState.Enemies
            .Where(c => c != base.Creature && c.IsAlive)
            .ToList())
        {
            await CreatureCmd.Escape(survivor);
        }
    }

    /// <summary>StS1 gremlins[] slot clearing: a dead minion frees its slot for the next RALLY.</summary>
    public override Task BeforeDeath(Creature creature)
    {
        if (creature == base.Creature)
        {
            return Task.CompletedTask;
        }
        foreach (int slot in _minionSlots.Keys.ToList())
        {
            if (ReferenceEquals(_minionSlots[slot], creature))
            {
                _minionSlots.Remove(slot);
            }
        }
        return Task.CompletedTask;
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // One stable 0-99 draw per move selection (vanilla passes one aiRng roll into getMove).
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

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Gremlin Leader", "地精首领"),
        [
            ("RALLY_MOVE", Tr("Rally", "号召")),
            ("ENCOURAGE_MOVE", Tr("Encourage", "鼓舞")),
            ("STAB_MOVE", Tr("Stab", "戳刺")),
        ]);
}
