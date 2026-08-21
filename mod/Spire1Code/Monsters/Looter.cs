using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — Looter (<c>com.megacrit.cardcrawl.monsters.exordium.Looter</c>).
/// <para>
/// Bytecode: HP 44-48, A2 46-50; swipeDmg 10 (A2 11), lungeDmg 12 (A2 14), escapeDef 6,
/// goldAmt 15 (A17 20). usePreBattleAction applies ThieveryPower(goldAmt).
/// takeTurn: MUG = attack(swipe) + steal; after the 2nd slash, 50% Smoke Bomb else Lunge.
/// LUNGE = attack(lunge) + steal, then always Smoke Bomb. SMOKE_BOMB = gain escapeDef block,
/// queue ESCAPE. ESCAPE = mark mugged, EscapeAction. die(): if stolenGold &gt; 0, add it back
/// to the combat rewards.
/// </para>
/// <para>
/// Gold theft reuses the engine's shipped <see cref="ThieveryPower"/> exactly like
/// <c>GremlinMerc</c> does: applied in <c>AfterAddedToRoom</c> with its Target set to the player,
/// then <c>Steal()</c> invoked after each attack. The stolen amount is tracked by the power's own
/// Gold var; on death the accumulated total is returned to the loot via
/// <c>CombatRoom.AddExtraReward(new GoldReward(..., wasGoldStolenBack: true))</c>, the same call
/// the engine's HeistPower uses to hand stolen gold back.
/// </para>
/// </summary>
public sealed class Looter : Spire1Monster
{
    // setHp(44, 48); ascension >= 7 -> setHp(46, 50)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 46, 44);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);

    // swipeDmg = 10; ascension >= 2 -> 11
    private int SwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

    // lungeDmg = 12; ascension >= 2 -> 14
    private int LungeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);

    // escapeDef = 6 (Smoke Bomb block)
    private const int EscapeBlock = 6;

    // goldAmt = 15; ascension >= 17 -> 20
    private int GoldAmount => 15; // vanilla A17 tier (20) unreachable in StS2 (max A10); base value kept.

    protected override string DonorId => "thieving_hopper";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ThieveryPower(this, goldAmt) — Target must point at the player's
        // creature for Steal() to know whose purse to pick (GremlinMerc.AfterAddedToRoom pattern).
        foreach (Player player in base.CombatState.Players)
        {
            ThieveryPower thieveryPower = (ThieveryPower)ModelDb.Power<ThieveryPower>().ToMutable();
            thieveryPower.Target = player.Creature;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), thieveryPower, base.Creature, GoldAmount, base.Creature, null);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // MUG x2 -> (50% Lunge | Smoke Bomb) -> Smoke Bomb -> ESCAPE.
        MoveState mug1 = new("MUG_MOVE", MugMove, new SingleAttackIntent(SwipeDamage));
        MoveState mug2 = new("MUG_MOVE_2", MugMove, new SingleAttackIntent(SwipeDamage));
        MoveState lunge = new("LUNGE_MOVE", LungeMove, new SingleAttackIntent(LungeDamage));
        MoveState smokeBomb = new("SMOKE_BOMB_MOVE", SmokeBombMove, new DefendIntent());
        MoveState escape = new("ESCAPE_MOVE", EscapeMove, new EscapeIntent());
        RandomBranchState afterSecondSlash = new("AFTER_SLASH_2");
        mug1.FollowUpState = mug2;
        mug2.FollowUpState = afterSecondSlash;
        // aiRng.randomBoolean(0.5f): half the time he lunges again, half the time he smokes out.
        afterSecondSlash.AddBranch(lunge, 0, 50f);
        afterSecondSlash.AddBranch(smokeBomb, 0, 50f);
        lunge.FollowUpState = smokeBomb;
        smokeBomb.FollowUpState = escape;
        escape.FollowUpState = escape;
        return new MonsterMoveStateMachine([mug1, mug2, lunge, smokeBomb, escape, afterSecondSlash], mug1);
    }

    private async Task MugMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SwipeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await StealGold();
    }

    private async Task LungeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(LungeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await StealGold();
    }

    private async Task StealGold()
    {
        foreach (ThieveryPower powerInstance in base.Creature.GetPowerInstances<ThieveryPower>())
        {
            await powerInstance.Steal();
        }
    }

    private async Task SmokeBombMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, EscapeBlock, ValueProp.Move, null);
    }

    private async Task EscapeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.Escape(base.Creature);
    }

    /// <summary>
    /// StS1 die(): if stolenGold &gt; 0, addStolenGoldToRewards(stolenGold). The engine equivalent is
    /// HeistPower.BeforeDeath — an extra GoldReward flagged as stolen-back on the combat room.
    /// </summary>
    public override Task BeforeDeath(Creature creature)
    {
        if (creature != base.Creature)
        {
            return Task.CompletedTask;
        }
        int stolen = 0;
        foreach (ThieveryPower powerInstance in base.Creature.GetPowerInstances<ThieveryPower>())
        {
            stolen += powerInstance.DynamicVars.Gold.IntValue;
        }
        if (stolen > 0 && base.CombatState.RunState.CurrentRoom is CombatRoom combatRoom)
        {
            foreach (Player player in base.CombatState.Players)
            {
                combatRoom.AddExtraReward(player, new GoldReward(stolen, player, wasGoldStolenBack: true));
            }
        }
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Looter",
        [
            ("MUG_MOVE", "Mug"),
            ("MUG_MOVE_2", "Mug"),
            ("LUNGE_MOVE", "Lunge"),
            ("SMOKE_BOMB_MOVE", "Smoke Bomb"),
            ("ESCAPE_MOVE", "Escape"),
        ]);
}
