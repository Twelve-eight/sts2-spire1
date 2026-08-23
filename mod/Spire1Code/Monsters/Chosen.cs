using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Chosen (<c>com.megacrit.cardcrawl.monsters.city.Chosen</c>). 官方中文名：被拣选者。
/// <para>
/// Bytecode: HP 95-99, A7 98-103; ZAP_DMG 18 (A2 21), DEBILITATE_DMG 10 (A2 12),
/// POKE_DMG 5 (A2 6), DEBILITATE_VULN 2, DRAIN_STR 3, DRAIN_WEAK 3, HEX_AMT 1.
/// getMove (non-A17): first turn POKE×2; then HEX once; then when lastMove was neither DRAIN nor
/// DEBILITATE → r&lt;50 ? DEBILITATE : DRAIN; else r&lt;40 ? ZAP : POKE×2.
/// Vanilla A17 branch moves HEX to the very first turn and drops the POKE opening — unreachable in
/// StS2's ascension mapping (max A10), so only the non-A17 script is modelled.
/// takeTurn: ZAP = 18 dmg (FIRE); DRAIN = Weak 3 on player + Strength 3 self;
/// DEBILITATE = 10 dmg + Vulnerable 2; HEX = HexPower 1 ("add a Curse to your draw pile" — see FLAG);
/// POKE = two hits of 5.
/// </para>
/// <para>
/// FLAGGED: vanilla <c>HexPower</c> shuffles a random Curse into the player's draw pile. A shipped
/// StS2 power of that name exists but its effect is the StS2-native one, not StS1's curse-adding
/// behaviour; until a curse-shuffle channel for monsters is confirmed, the move applies the shipped
/// power as the closest available stand-in. Revisit if a shipped monster demonstrates the exact
/// curse-insertion call to copy.
/// </para>
/// <para>
/// Donor: <c>damp_cultist</c> — robed humanoid cultist silhouette; closest visual match for a
/// hooded zealot casting dark magic.
/// </para>
/// </summary>
public sealed class Chosen : Spire1Monster
{
    // setHp(95, 99); ascension >= 7 -> setHp(98, 103)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 98, 95);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 103, 99);

    // zapDmg = 18; ascension >= 2 -> 21
    private int ZapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 21, 18);

    // debilitateDmg = 10; ascension >= 2 -> 12
    private int DebilitateDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    // pokeDmg = 5 x2; ascension >= 2 -> 6
    private int PokeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private const int DebilitateVulnerable = 2;
    private const int DrainStrength = 3;
    private const int DrainWeakTurns = 3;
    private const int HexAmount = 1;

    protected override string DonorId => "damp_cultist";

    // Vanilla fields: firstTurn, usedHex.
    private bool _usedHex;

    private bool _everMoved;

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Chosen",
        [
            ("ZAP_MOVE", "Zap"),
            ("DRAIN_MOVE", "Drain"),
            ("DEBILITATE_MOVE", "Debilitate"),
            ("HEX_MOVE", "Hex"),
            ("POKE_MOVE", "Poke"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState zap = new("ZAP_MOVE", ZapMove, new SingleAttackIntent(ZapDamage));
        MoveState drain = new("DRAIN_MOVE", DrainMove, new BuffIntent(), new DebuffIntent());
        MoveState debilitate = new("DEBILITATE_MOVE", DebilitateMove, new SingleAttackIntent(DebilitateDamage), new DebuffIntent());
        MoveState hex = new("HEX_MOVE", HexMove, new DebuffIntent());
        // Distinct state id so both poke turns appear in the log separately (vanilla uses one byte).
        MoveState poke1 = new("POKE_MOVE", PokeMove, new MultiAttackIntent(PokeDamage, 2));
        MoveState poke2 = new("POKE_MOVE_2", PokeMove, new MultiAttackIntent(PokeDamage, 2));

        ConditionalBranchState afterOpening = new("CHOSEN_AFTER_OPENING");
        ConditionalBranchState afterHex = new("CHOSEN_AFTER_HEX");
        poke1.FollowUpState = poke2;
        poke2.FollowUpState = afterOpening;
        hex.FollowUpState = afterHex;
        zap.FollowUpState = afterHex;
        drain.FollowUpState = afterHex;
        debilitate.FollowUpState = afterHex;

        // Opening: POKE × 2 (vanilla firstTurn latch), then exactly one HEX.
        afterOpening.AddState(hex, () => !_usedHex);
        afterOpening.AddState(afterHex, () => true);

        // Main loop: if last move was neither DRAIN nor DEBILITATE → r<50 ? DEBILITATE : DRAIN;
        // otherwise r<40 ? ZAP : POKE×2.
        afterHex.AddState(debilitate, () => !LastWas(drain) && !LastWas(debilitate) && RollHundred() < 50);
        afterHex.AddState(drain, () => !LastWas(drain) && !LastWas(debilitate));
        afterHex.AddState(zap, () => RollHundred() < 40);
        // Poke alternates between twin states purely so consecutive pokes stay distinct in the log.
        afterHex.AddState(poke1, () => !LastWas(poke2));
        afterHex.AddState(poke2, () => true);

        return new MonsterMoveStateMachine([zap, drain, debilitate, hex, poke1, poke2, afterOpening, afterHex], poke1);
    }

    private async Task ZapMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await DamageCmd.Attack(ZapDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(null);
    }

    private async Task DrainMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, DrainWeakTurns, base.Creature, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, DrainStrength, base.Creature, null);
    }

    private async Task DebilitateMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await DamageCmd.Attack(DebilitateDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DebilitateVulnerable, base.Creature, null);
    }

    private async Task HexMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        _usedHex = true;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.8f);
        // See class-level FLAG note: stand-in for vanilla's curse-shuffling HexPower.
        await PowerCmd.Apply<HexPower>(new ThrowingPlayerChoiceContext(), targets, HexAmount, base.Creature, null);
    }

    private async Task PokeMove(IReadOnlyList<Creature> targets)
    {
        _everMoved = true;
        await DamageCmd.Attack(PokeDamage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
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
}
