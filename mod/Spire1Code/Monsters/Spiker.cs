// Port of vanilla StS1 com.megacrit.cardcrawl.monsters.beyond.Spiker.
// All numbers below are transcribed from the javap dump; nothing invented.
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Vanilla StS1 <c>Spiker</c> ported to the StS2 engine.
/// <para>
/// Bytecode: HP 42-56, A7 44-60; attackDmg 7 (A2 9), startingThorns 3 (A2 4), BUFF_AMT 2.
/// thornsCount starts at 0 and increments on every BUFF_THORNS use.
/// getMove: thornsCount &gt; 5 → ATTACK; else roll &lt; 50 &amp;&amp; !lastMove(ATTACK) → ATTACK;
/// else → BUFF_THORNS. usePreBattleAction applies ThornsPower(startingThorns); the A17 tier
/// (startingThorns + 3) is not modelled — StS2 has no AscensionLevel mapping for A17.
/// takeTurn ATTACK = AnimateSlowAttack + SLASH_HORIZONTAL; BUFF_THORNS = thornsCount++ +
/// ApplyPower(ThornsPower, 2).
/// </para>
/// <para>
/// The donor rig <c>spiny_toad</c> ships the protrude sfx and <c>Spiked</c> anim (used by the
/// shipped <c>SpinyToad.SpikesMove</c>), so the thorns buff borrows that presentation verbatim.
/// </para>
/// </summary>
public sealed class Spiker : Spire1Monster
{
    // setHp(42, 56); ascension >= 7 -> setHp(44, 60)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 44, 42);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 56);

    // attackDmg = 7; ascension >= 2 -> 9
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);

    // startingThorns = 3; ascension >= 2 -> 4 (A17 tier +3 not modelled, see class remarks)
    private int StartingThorns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    // BUFF_AMT = 2 (no ascension variant)
    private int BuffAmount => 2;

    /// <summary>
    /// Vanilla <c>thornsCount</c>: starts at 0, incremented once per BUFF_THORNS use; the
    /// getMove ATTACK guard at &gt; 5 reads it.
    /// </summary>
    private int _thornsCount;

    protected override string DonorId => "spiny_toad";

    /// <summary>
    /// usePreBattleAction: ApplyPowerAction(new ThornsPower(this, startingThorns)) — the
    /// shipped <see cref="ThornsPower"/> is applied verbatim.
    /// </summary>
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            StartingThorns, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState attack = new("ATTACK_MOVE", AttackMove, new SingleAttackIntent(AttackDamage));
        MoveState buffThorns = new("BUFF_THORNS_MOVE", ThornsMove, new BuffIntent());

        // Bytecode getMove: thornsCount > 5 -> ATTACK; else roll < 50 && !lastMove(ATTACK) ->
        // ATTACK; else -> BUFF_THORNS. Modelled as one conditional picker; the roll is a stable
        // per-turn sub-roll so the guard reads a single draw, like vanilla's one aiRng roll.
        ConditionalBranchState picker = new("SPIKER_PICKER");
        attack.FollowUpState = picker;
        buffThorns.FollowUpState = picker;
        picker.AddState(attack, () => _thornsCount > 5 || (LastSubRoll(0.5f) && !LastWas(attack)));
        picker.AddState(buffThorns, () => true);

        return new MonsterMoveStateMachine(
            new List<MonsterState> { attack, buffThorns, picker }, picker);
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // One stable sub-roll per turn: vanilla draws aiRng.random(100) < 50 inside getMove; we draw
    // once per turn and cache it so repeated evaluations of the guard see the same value.
    private bool? _subRoll;
    private int _subRollTurn = -1;
    private bool LastSubRoll(float threshold)
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_subRoll == null || _subRollTurn != turn)
        {
            _subRoll = base.Rng.NextFloat() < threshold;
            _subRollTurn = turn;
        }
        return _subRoll.Value;
    }

    // takeTurn ATTACK: AnimateSlowAttackAction + DamageAction(SLASH_HORIZONTAL) → slower
    // attack anim + slash hit vfx.
    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // takeTurn BUFF_THORNS: thornsCount++ + ApplyPowerAction(new ThornsPower(this, 2), 2).
    // Donor spiny_toad buff presentation: protrude sfx + "Spiked" anim (SpinyToad.SpikesMove).
    private async Task ThornsMove(IReadOnlyList<Creature> targets)
    {
        _thornsCount++;
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/spiny_toad/spiny_toad_protrude");
        await CreatureCmd.TriggerAnim(base.Creature, "Spiked", 0.5f);
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            BuffAmount, base.Creature, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Spiker",
        [
            ("ATTACK_MOVE", "Spike"),
            ("BUFF_THORNS_MOVE", "Thorns"),
        ]);
}
