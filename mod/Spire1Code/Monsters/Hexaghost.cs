using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-1 boss "Hexaghost" (<c>com.megacrit.cardcrawl.monsters.exordium.Hexaghost</c>).
/// <para>
/// Fully deterministic AI. <c>getMove</c> fires <c>ACTIVATE</c> once (<c>activated</c> latch,
/// Unknown intent), and <c>takeTurn</c>'s ACTIVATE branch reads
/// <c>AbstractDungeon.player.currentHealth / 12 + 1</c> into <c>damage[2]</c> and queues
/// <c>DIVIDER</c> for six hits of that value. Every later move is chosen purely by
/// <c>orbActiveCount</c>, which each of Sear/Tackle/Inflame raises by one
/// (<c>changeState("Activate Orb")</c>) and Inferno resets to zero
/// (<c>changeState("Deactivate")</c>), giving the fixed seven-move loop
/// Sear -&gt; Tackle -&gt; Sear -&gt; Inflame -&gt; Tackle -&gt; Sear -&gt; Inferno.
/// </para>
/// <para>
/// Ascension mapping, same scheme as the other Exordium bosses: A4's damage bumps hang off
/// <see cref="AscensionLevel.DeadlyEnemies"/>, A9's HP off <see cref="AscensionLevel.ToughEnemies"/>,
/// and A19's boss tier off <see cref="AscensionLevel.DoubleBoss"/> (topmost, boss-scoped, and
/// cumulative with the other two).
/// </para>
/// <para>
/// Two vanilla details are intentionally absent. The six orbiting flames
/// (<c>HexaghostOrb</c>/<c>HexaghostBody</c>) are pure decoration drawn by the boss itself in StS1 —
/// they carry no HP and no mechanics, only the visual orb counter — so nothing is lost by omitting
/// them; the move order they encode is reproduced exactly by the state chain. Inferno's
/// <c>BurnIncreaseAction</c> (upgrade every Burn in play to Burn+, 4 damage) cannot be reproduced:
/// both our <see cref="Burn"/> and the shipped StS2 <c>Burn</c> declare
/// <c>MaxUpgradeLevel =&gt; 0</c>, i.e. StS2 statuses do not upgrade at all. Inferno therefore deals
/// its damage without the escalation, and Sear keeps adding un-upgraded Burns.
/// </para>
/// </summary>
public sealed class Hexaghost : Spire1Monster
{
    /// <summary><c>SEAR_DMG = 6</c>; not ascension-scaled in vanilla.</summary>
    private const int _searDamage = 6;

    /// <summary><c>strengthenBlockAmt = 12</c>.</summary>
    private const int _inflameBlock = 12;

    /// <summary><c>fireTackleCount = 2</c>.</summary>
    private const int _fireTackleHits = 2;

    /// <summary><c>infernoHits = 6</c>, and the same six hits Divider uses.</summary>
    private const int _infernoHits = 6;

    /// <summary>The <c>/ 12 + 1</c> divisor from the ACTIVATE branch of <c>takeTurn</c>.</summary>
    private const int _dividerHpDivisor = 12;

    private int _dividerDamage;

    /// <summary>
    /// Shipped <c>TorchHeadAmalgam</c>: a fire-headed boss-scale creature. Its rig has
    /// <c>idle_loop</c>/<c>attack</c>/<c>hurt</c>/<c>die</c> plus a <c>debuff</c> track, but no
    /// <c>cast</c>, which is one of the five tracks <see cref="MonsterModel.GenerateAnimator"/>
    /// assumes.
    /// </summary>
    protected override string DonorId => "torch_head_amalgam";

    /// <summary>
    /// Remaps the engine's default animation triggers onto the tracks this donor rig actually has
    /// (<c>TorchHeadAmalgam.GenerateAnimator</c> is the authority for the names). BaseLib's helper
    /// folds any trigger with no matching track back onto idle, so the missing <c>cast</c> track
    /// cannot produce a broken state.
    /// </summary>
    public override CreatureAnimator GenerateAnimator(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack");

    /// <summary><c>HP = 250</c>, <c>A_2_HP = 264</c> (applied from A9 up).</summary>
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 264, 250);

    public override int MaxInitialHp => MinInitialHp;

    /// <summary><c>FIRE_TACKLE_DMG = 5</c>, <c>A_4_FIRE_TACKLE_DMG = 6</c>.</summary>
    private static int FireTackleDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);

    /// <summary><c>INFERNO_DMG = 2</c>, <c>A_4_INFERNO_DMG = 3</c>.</summary>
    private static int InfernoDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    /// <summary><c>BURN_COUNT = 1</c>, <c>A_19_BURN_COUNT = 2</c>.</summary>
    private static int SearBurnCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 2, 1);

    /// <summary><c>STR_AMT = 2</c>, <c>A_19_STR_AMT = 3</c>.</summary>
    private static int InflameStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 3, 2);

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Hexaghost",
        [
            ("ACTIVATE_MOVE", "Activate"),
            ("DIVIDER_MOVE", "Divider"),
            ("SEAR_MOVE", "Sear"),
            ("TACKLE_MOVE", "Tackle"),
            ("SEAR_MOVE_2", "Sear"),
            ("INFLAME_MOVE", "Inflame"),
            ("TACKLE_MOVE_2", "Tackle"),
            ("SEAR_MOVE_3", "Sear"),
            ("INFERNO_MOVE", "Inferno"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState activate = new("ACTIVATE_MOVE", ActivateMove, new UnknownIntent());
        // Divider's per-hit damage is only known once Activate resolves, and MultiAttackIntent's
        // public constructors bake the damage in, so the intent reads the snapshot through a Func.
        MoveState divider = new("DIVIDER_MOVE", DividerMove,
            new DividerIntent(() => _dividerDamage, _infernoHits));
        MoveState sear1 = new("SEAR_MOVE", SearMove,
            new SingleAttackIntent(_searDamage), new StatusIntent(SearBurnCount));
        MoveState tackle1 = new("TACKLE_MOVE", FireTackleMove,
            new MultiAttackIntent(FireTackleDamage, _fireTackleHits));
        MoveState sear2 = new("SEAR_MOVE_2", SearMove,
            new SingleAttackIntent(_searDamage), new StatusIntent(SearBurnCount));
        MoveState inflame = new("INFLAME_MOVE", InflameMove, new DefendIntent(), new BuffIntent());
        MoveState tackle2 = new("TACKLE_MOVE_2", FireTackleMove,
            new MultiAttackIntent(FireTackleDamage, _fireTackleHits));
        MoveState sear3 = new("SEAR_MOVE_3", SearMove,
            new SingleAttackIntent(_searDamage), new StatusIntent(SearBurnCount));
        MoveState inferno = new("INFERNO_MOVE", InfernoMove,
            new MultiAttackIntent(InfernoDamage, _infernoHits));

        activate.FollowUpState = divider;
        divider.FollowUpState = sear1;      // orbActiveCount 0
        sear1.FollowUpState = tackle1;      // 1
        tackle1.FollowUpState = sear2;      // 2
        sear2.FollowUpState = inflame;      // 3
        inflame.FollowUpState = tackle2;    // 4
        tackle2.FollowUpState = sear3;      // 5
        sear3.FollowUpState = inferno;      // 6
        inferno.FollowUpState = sear1;      // Deactivate -> back to 0

        List<MonsterState> states =
            [activate, divider, sear1, tackle1, sear2, inflame, tackle2, sear3, inferno];
        return new MonsterMoveStateMachine(states, activate);
    }

    /// <summary>
    /// <c>takeTurn</c>'s ACTIVATE branch: no damage, no block — it only lights the orbs and locks in
    /// Divider's damage from the player's CURRENT HP at this instant,
    /// <c>currentHealth / 12 + 1</c> (integer division). Vanilla reads the single
    /// <c>AbstractDungeon.player</c>; the first player creature in the target list is that player in
    /// a solo run, and a deterministic pick in co-op.
    /// </summary>
    private Task ActivateMove(IReadOnlyList<Creature> targets)
    {
        Creature? player = targets.Count > 0 ? targets[0] : null;
        _dividerDamage = player == null ? 1 : player.CurrentHp / _dividerHpDivisor + 1;
        return Task.CompletedTask;
    }

    private async Task DividerMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(_dividerDamage).WithHitCount(_infernoHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task SearMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(_searDamage).FromMonster(this).WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, SearBurnCount, null);
    }

    private async Task FireTackleMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FireTackleDamage).WithHitCount(_fireTackleHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary><c>INFLAME</c>: <c>GainBlockAction(12)</c> plus <c>StrengthPower(strAmount)</c>.</summary>
    private async Task InflameMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, _inflameBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, InflameStrength, Creature, null);
    }

    private async Task InfernoMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(InfernoDamage).WithHitCount(_infernoHits).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>
    /// Multi-hit attack intent whose per-hit damage is evaluated live, which the shipped
    /// <see cref="MultiAttackIntent"/> constructors cannot do (they capture a fixed int).
    /// <see cref="AttackIntent.DamageCalc"/> is settable from a subclass, so nothing else changes:
    /// label, texture and total-damage maths are all inherited.
    /// </summary>
    private sealed class DividerIntent : MultiAttackIntent
    {
        public DividerIntent(Func<int> damageCalc, int repeat)
            : base(0, repeat)
        {
            DamageCalc = () => damageCalc();
        }
    }
}
