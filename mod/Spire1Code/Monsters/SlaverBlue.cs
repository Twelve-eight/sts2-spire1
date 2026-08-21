using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
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
/// StS1 Exordium — Blue Slaver (<c>com.megacrit.cardcrawl.monsters.exordium.SlaverBlue</c>).
/// <para>
/// Bytecode: HP 46-50, A2 48-52; STAB_DMG 12 (A2 13), RAKE_DMG 7 (A2 8), weakAmt 1.
/// getMove roll r: r&gt;=40 &amp;&amp; !lastTwo(STAB) -&gt; STAB; else (A17: last!=RAKE ? RAKE : STAB)
/// / (!lastTwo(RAKE) ? RAKE : STAB). takeTurn RAKE = attack(rake) + Weak(weakAmt) [A17: +1].
/// </para>
/// </summary>
public sealed class SlaverBlue : Spire1Monster
{
    // setHp(46, 50); ascension >= 7 -> setHp(48, 52)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 48, 46);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 50);

    // STAB_DMG = 12; ascension >= 2 -> 13
    private int StabDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);

    // RAKE_DMG = 7; ascension >= 2 -> 8
    private int RakeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    // weakAmt = 1 (A17 applies weakAmt+1 = 2)
    private int WeakAmount => 1; // vanilla A17 tier (2) unreachable in StS2; base value kept.

    protected override string DonorId => "stabbot";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // Bytecode getMove (slaverblue.txt), roll 0-99:
        //   num>=40 (60%) -> STAB unless lastTwo(STAB)
        //   else RAKE unless lastTwo(RAKE) at base (A17+ lastMove(RAKE) tier dropped)
        // First move random 60/40 via initial state roll.
        MoveState stab = new("STAB_MOVE", StabMove, new SingleAttackIntent(StabDamage));
        MoveState rake = new("RAKE_MOVE", RakeMove, new SingleAttackIntent(RakeDamage), new DebuffIntent());
        RandomBranchState roll = new("ROLL");
        stab.FollowUpState = roll;
        rake.FollowUpState = roll;
        roll.AddBranch(stab, 2, () => 60f);
        roll.AddBranch(rake, 2, () => 40f);
        return new MonsterMoveStateMachine([stab, rake, roll], roll);
    }

    private async Task StabMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StabDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task RakeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RakeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, WeakAmount, base.Creature, null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Blue Slaver",
        [
            ("STAB_MOVE", "Stab"),
            ("RAKE_MOVE", "Rake"),
        ]);
}
