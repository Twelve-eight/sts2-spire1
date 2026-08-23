using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spire1.Spire1Code.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Spire Growth (<c>com.megacrit.cardcrawl.monsters.beyond.SpireGrowth</c>,
/// internal id "Serpent"). 官方中文名：高塔之蔓。
/// <para>
/// Bytecode: HP 170, A7 190; tackleDmg 16 (A2 18), smashDmg 22 (A2 25), constrictDmg 10
/// (A17 takeTurn: 12). getMove: at A17 the CONSTRICT check runs first (player lacks Constricted
/// and last move wasn't CONSTRICT → CONSTRICT); then roll&lt;50 → QUICK_TACKLE unless the last
/// two were QUICK_TACKLE; then the same CONSTRICT check again (all tiers); then SMASH unless
/// the last two were SMASH, else QUICK_TACKLE. takeTurn: QUICK_TACKLE = one tackle hit
/// (BLUNT_HEAVY); CONSTRICT = apply ConstrictedPower to the player (10, or 12 at A17);
/// SMASH = one smash hit (BLUNT_HEAVY).
/// </para>
/// <para>
/// Note on "splitting": per bytecode SpireGrowth never spawns anything — the pack-splitting
/// monster in this act is Darkling's half-death/reincarnate cycle (see Darkling.cs).
/// </para>
/// <para>
/// FLAGGED: the shipped StS2 <see cref="ConstrictedPower"/> ticks its amount of damage after
/// every owner-side turn and persists until the applier dies, while vanilla StS1's Constricted
/// deals one lump at end of turn and clears. Repeated applications stack in both games; the
/// shipped power is applied verbatim as the closest available stand-in.
/// </para>
/// <para>
/// Donor: <c>vine_shambler</c> — the shipped plant-vine creature; closest visual match for a
/// serpentine spire growth.
/// </para>
/// </summary>
public sealed class SpireGrowth : Spire1Monster
{
    // setHp(170); ascension >= 7 -> setHp(190) — fixed single value per tier.
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 170);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 190, 170);

    // tackleDmg = 16; ascension >= 2 -> 18
    private int TackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    // smashDmg = 22; ascension >= 2 -> 25
    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 25, 22);

    // constrictDmg = 10; ascension >= 17 -> 12
    private int ConstrictAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    /// <summary>The A17 early-CONSTRICT branch maps onto DeadlyEnemies.</summary>
    private static bool IsHardMode => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);

    /// <summary>Borrows the shipped vine_shambler scene.</summary>
    protected override string DonorId => "vine_shambler";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState tackle = new("QUICK_TACKLE_MOVE", TackleMove, new SingleAttackIntent(TackleDamage));
        MoveState constrict = new("CONSTRICT_MOVE", ConstrictMove, new DebuffIntent());
        MoveState smash = new("SMASH_MOVE", SmashMove, new SingleAttackIntent(SmashDamage));

        ConditionalBranchState bands = new("SPIRE_GROWTH_BANDS");
        tackle.FollowUpState = bands;
        constrict.FollowUpState = bands;
        smash.FollowUpState = bands;

        // A17 tier checks CONSTRICT before the roll; every tier re-checks it after the
        // roll<50 band. Both gates: player must lack Constricted and last move != CONSTRICT.
        bands.AddState(constrict, () => IsHardMode && !PlayerHasConstricted() && !LastWas(constrict));
        bands.AddState(tackle, () => RollHundred() < 50 && !LastTwoWere(tackle));
        bands.AddState(constrict, () => !PlayerHasConstricted() && !LastWas(constrict));
        bands.AddState(smash, () => !LastTwoWere(smash));
        bands.AddState(tackle, () => true);

        return new MonsterMoveStateMachine([tackle, constrict, smash, bands], tackle);
    }

    private async Task TackleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TackleDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task ConstrictMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        await PowerCmd.Apply<ConstrictedPower>(new ThrowingPlayerChoiceContext(), targets, ConstrictAmount, base.Creature, null);
    }

    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SmashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>Vanilla getMove: player.hasPower("Constricted").</summary>
    private bool PlayerHasConstricted()
    {
        Player? player = base.CombatState?.Players.FirstOrDefault();
        return player?.Creature.HasPower<ConstrictedPower>() ?? true;
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

    // zh monster name is the official zhs string from .tmp/m25-zhs-names.json (vanilla id is
    // "Serpent"; the monsters.json NAME 塔内增生组织 predates the m25 naming used here); move
    // titles follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Spire Growth", "高塔之蔓"),
        [
            ("QUICK_TACKLE_MOVE", Tr("Quick Tackle", "迅捷冲撞")),
            ("CONSTRICT_MOVE", Tr("Constrict", "缠绕")),
            ("SMASH_MOVE", Tr("Smash", "粉碎")),
        ]);
}
