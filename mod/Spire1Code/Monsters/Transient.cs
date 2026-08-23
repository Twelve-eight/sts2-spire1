using System.Collections.Generic;
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

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Transient (<c>com.megacrit.cardcrawl.monsters.beyond.Transient</c>).
/// 官方中文名：倏忽魔。
/// <para>
/// Bytecode: HP 999, gold 1; startingDeathDmg 30 (A2 40); count starts 0 and increments after
/// every attack; damage[count] = startingDeathDmg + count*10, so the attack ramps
/// 30/40/50/60/70 (A2: 40/50/60/70/80). usePreBattleAction applies FadingPower(5) (A17: 6) and
/// ShiftingPower — see those types. getMove/takeTurn both set the same single ATTACK move;
/// the monster never does anything else.
/// </para>
/// <para>
/// The escape is driven by <see cref="FadingPower"/>: at the end of each enemy-side turn it
/// loses one stack, and at zero the Transient flees — matching vanilla's five attacks before
/// the vanish (6 on the DeadlyEnemies tier). <see cref="ShiftingPower"/> is Transient's
/// second pre-battle buff (lose Strength equal to HP lost until end of turn); Transient never
/// gains Strength in this port, so it is behaviourally inert, exactly like vanilla.
/// </para>
/// <para>
/// Donor: <c>phantasmal_gardener</c> — a tall spectral figure; closest visual match for the
/// wispy transient.
/// </para>
/// </summary>
public sealed class Transient : Spire1Monster
{
    // HP = 999 fixed (constructor maxHealth, no ascension tier).
    public override int MinInitialHp => 999;

    public override int MaxInitialHp => 999;

    // startingDeathDmg = 30; ascension >= 2 -> 40
    private int StartingDeathDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);

    // FadingPower amount: 5, or 6 at ascension 17+.
    private int FadingTurns => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    // Vanilla field: count (starts 0, incremented after each attack).
    private int _count;

    /// <summary>Borrows the shipped phantasmal_gardener scene.</summary>
    protected override string DonorId => "phantasmal_gardener";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: FadingPower(5/6) + ShiftingPower.
        await PowerCmd.Apply<FadingPower>(new ThrowingPlayerChoiceContext(), Creature, FadingTurns, Creature, null);
        await PowerCmd.Apply<ShiftingPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // The one and only move; damage scales with the attack counter at display time.
        MoveState attack = new("ATTACK_MOVE", AttackMove,
            new SingleAttackIntent(() => (decimal)(StartingDeathDamage + _count * 10)));
        attack.FollowUpState = attack;
        return new MonsterMoveStateMachine([attack], attack);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StartingDeathDamage + _count * 10).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        _count++;
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move titles
    // follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Transient", "倏忽魔"),
        [
            ("ATTACK_MOVE", Tr("Attack", "攻击")),
        ]);
}
