using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Runs;
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
/// StS1 The City — Mugger (<c>com.megacrit.cardcrawl.monsters.city.Mugger</c>;
/// 官方中文名「打劫的」). City thief; pairs with the Act-1 Looter in the vanilla
/// "2 Thieves" weak encounter (bytecode: <c>new MonsterGroup(new Looter, new Mugger)</c>
/// — NOT the Bandit trio; Main-agent bytecode audit 2026-08).
/// <para>
/// Bytecode: HP 48-52, A7 50-54; swipeDmg 10 (A2 11), bigSwipeDmg 16 (A2 18),
/// goldAmt 15 (A17 20), escapeDef 11 (A17 smoke bomb blocks escapeDef+6=17).
/// usePreBattleAction applies ThieveryPower(goldAmt). getMove seeds MUG.
/// takeTurn MUG: slashCount++; after the FIRST slash only, a 60% cosmetic taunt line;
/// after the SECOND slash, 50% Smoke Bomb else BIGSWIPE. BIGSWIPE then always leads to
/// Smoke Bomb. SMOKE_BOMB: gain block (A17: block+6), queue ESCAPE. ESCAPE: mark mugged,
/// EscapeAction. die(): if stolenGold &gt; 0, addStolenGoldToRewards.
/// </para>
/// <para>
/// Gold theft reuses the engine's shipped <see cref="ThieveryPower"/> exactly like our
/// Looter/GremlinMerc: applied in AfterAddedToRoom with Target set to each player, then
/// Steal() invoked after every attack. On death the accumulated total is returned via
/// CombatRoom.AddExtraReward(new GoldReward(..., wasGoldStolenBack: true)) — the same call
/// HeistPower.BeforeDeath uses. The A17 gold tier (20) is unreachable below StS2's max
/// ascension mapping and is dropped (Looter precedent); the A17 +6 smoke-bomb block maps
/// onto DeadlyEnemies like GremlinShield's A17 block bump.
/// </para>
/// <para>
/// Art: donor rig <c>thieving_hopper</c> — the shipped mugger/thief counterpart (same rig
/// our Looter borrows; Mugger is vanilla's looterAlt reskin). Attack/Steal triggers exist;
/// no animator remap needed beyond what Looter already proved works.
/// </para>
/// </summary>
public sealed class Mugger : Spire1Monster
{
    // setHp(48, 52); ascension >= 7 -> setHp(50, 54)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 48);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 52);

    // swipeDmg = 10; ascension >= 2 -> 11
    private int SwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);

    // bigSwipeDmg = 16; ascension >= 2 -> 18
    private int BigSwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);

    // escapeDef = 11; A17 smoke bomb adds +6 (DeadlyEnemies tier)
    private int SmokeBombBlock => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies) ? 17 : 11;

    // goldAmt = 15; ascension >= 17 -> 20
    private int GoldAmount => 15; // vanilla A17 tier (20) unreachable in StS2; base value kept (Looter precedent).

    protected override string DonorId => "thieving_hopper";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ThieveryPower(this, goldAmt) with Target pointing at the player
        // whose purse to pick (Looter/GremlinMerc pattern).
        foreach (Player player in base.CombatState.Players)
        {
            ThieveryPower thieveryPower = (ThieveryPower)ModelDb.Power<ThieveryPower>().ToMutable();
            thieveryPower.Target = player.Creature;
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), thieveryPower, base.Creature, GoldAmount, base.Creature, null);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // MUG -> MUG -> (50% BIGSWIPE | SMOKE_BOMB); BIGSWIPE -> SMOKE_BOMB -> ESCAPE.
        MoveState mug1 = new("MUG_MOVE", MugMove, new SingleAttackIntent(SwipeDamage));
        MoveState mug2 = new("MUG_MOVE_2", MugMove, new SingleAttackIntent(SwipeDamage));
        MoveState bigSwipe = new("BIG_SWIPE_MOVE", BigSwipeMove, new SingleAttackIntent(BigSwipeDamage));
        MoveState smokeBomb = new("SMOKE_BOMB_MOVE", SmokeBombMove, new DefendIntent());
        MoveState escape = new("ESCAPE_MOVE", EscapeMove, new EscapeIntent());
        RandomBranchState afterSecondSlash = new("AFTER_SLASH_2");
        mug1.FollowUpState = mug2;
        mug2.FollowUpState = afterSecondSlash;
        // takeTurn gate at slashCount==2: aiRng.randomBoolean(0.5f).
        afterSecondSlash.AddBranch(bigSwipe, 0, 50f);
        afterSecondSlash.AddBranch(smokeBomb, 0, 50f);
        bigSwipe.FollowUpState = smokeBomb;
        smokeBomb.FollowUpState = escape;
        escape.FollowUpState = escape;
        return new MonsterMoveStateMachine([mug1, mug2, bigSwipe, smokeBomb, escape, afterSecondSlash], mug1);
    }

    private async Task MugMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SwipeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await StealGold();
    }

    private async Task BigSwipeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BigSwipeDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
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
        await CreatureCmd.GainBlock(base.Creature, SmokeBombBlock, ValueProp.Move, null);
    }

    private async Task EscapeMove(IReadOnlyList<Creature> targets)
    {
        // Removal idiom copied from GremlinFat.EscapeMove / ThievingHopper.EscapeMove.
        await Cmd.CustomScaledWait(0.75f, 1.25f);
        NCombatRoom.Instance?.GetCreatureNode(Creature)?.ToggleIsInteractable(on: false);
        await CreatureCmd.Escape(Creature);
    }

    /// <summary>
    /// StS1 die(): if stolenGold &gt; 0, addStolenGoldToRewards(stolenGold). Engine equivalent:
    /// HeistPower.BeforeDeath — extra GoldReward flagged as stolen-back plus MarkLootReturned.
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
                player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).MarkLootReturned(stolen);
            }
        }
        return Task.CompletedTask;
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Mugger",
        [
            ("MUG_MOVE", "Mug"),
            ("MUG_MOVE_2", "Mug"),
            ("BIG_SWIPE_MOVE", "Big Swipe"),
            ("SMOKE_BOMB_MOVE", "Smoke Bomb"),
            ("ESCAPE_MOVE", "Escape"),
        ]);
}
