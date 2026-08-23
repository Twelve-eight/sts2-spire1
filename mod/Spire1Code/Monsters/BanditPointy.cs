using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — BanditPointy (<c>com.megacrit.cardcrawl.monsters.city.BanditPointy</c>,
/// internal id "BanditChild"; 官方中文名「尖头强盗」). Masked Bandits trio member —
/// event-spawned only, never a regular encounter (Main-agent bytecode audit 2026-08).
/// <para>
/// Bytecode: HP 30 flat, A7 34; attackDmg 5 (A2 6), POINTY_SPECIAL hits twice
/// (SetMoveAction multi 2x1). getMove always seeds the same move; takeTurn slashes twice
/// (SLASH_VERTICAL then SLASH_HORIZONTAL) and re-sets the same multi attack forever.
/// </para>
/// <para>
/// Ascension mapping: A7 HP -> ToughEnemies, A2 damage -> DeadlyEnemies (shipped
/// convention). Cosmetic spine SLASH state juggling not ported.
/// </para>
/// <para>
/// Art: donor rig <c>nibbit</c> — small scrappy rodent-with-a-blade (Nibbit, HP 42-48,
/// slash moves); the shipped rig closest to a pint-sized stab-happy bandit. NOTE
/// (FLAGGED): nibbit's animator maps Cast->hiss and has no separate hurt track issue;
/// default triggers work. Alternative if it reads too beast-like: <c>vantom</c>.
/// </para>
/// </summary>
public sealed class BanditPointy : Spire1Monster
{
    // setHp(30); ascension >= 7 -> setHp(34)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 34, 30);

    public override int MaxInitialHp => MinInitialHp;

    // attackDmg = 5; ascension >= 2 -> 6
    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    private const int Hits = 2;

    protected override string DonorId => "nibbit";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState pointySpecial = new("POINTY_SPECIAL", PointySpecialMove,
            new MultiAttackIntent(AttackDamage, Hits));
        pointySpecial.FollowUpState = pointySpecial;
        return new MonsterMoveStateMachine([pointySpecial], pointySpecial);
    }

    private async Task PointySpecialMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage).WithHitCount(Hits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .OnlyPlayAnimOnce()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Pointy",
        [
            ("POINTY_SPECIAL", "Pointy Special"),
        ]);
}
