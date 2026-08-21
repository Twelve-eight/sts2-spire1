using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Act-1 boss "Slime Boss" (<c>com.megacrit.cardcrawl.monsters.exordium.SlimeBoss</c>).
/// <para>
/// Three-move loop, no randomness at all: <c>getMove</c> opens on <c>STICKY</c> (Goop Spray) via the
/// <c>firstTurn</c> latch, Goop Spray queues <c>PREP_SLAM</c> (Preparing, Unknown intent), Preparing
/// queues <c>SLAM</c>, and Slam queues <c>STICKY</c> again. <c>tackleDmg</c> exists in the class but
/// no branch of <c>takeTurn</c> ever reads it — only the split children tackle — so it is not
/// modelled here.
/// </para>
/// <para>
/// The split is driven from <c>damage()</c>: as soon as
/// <c>currentHealth &lt;= maxHealth / 2f</c> (and the boss is not already about to split) it forces
/// its next move to <c>SPLIT</c> and shows "Interrupted". The <c>SPLIT</c> turn itself spawns
/// <em>one</em> <c>SpikeSlime_L</c> and <em>one</em> <c>AcidSlime_L</c>, each constructed with
/// <c>currentHealth</c> as its HP, and the boss removes itself with <c>SuicideAction</c>. Vanilla
/// suicides first and brackets the whole turn in <c>CannotLoseAction</c>/<c>CanLoseAction</c> so the
/// combat does not end during the gap; spawning before the boss dies achieves the same thing
/// without needing that guard, and is otherwise indistinguishable.
/// </para>
/// <para>
/// Ascension mapping matches the other Exordium bosses: A4 damage on
/// <see cref="AscensionLevel.DeadlyEnemies"/>, A9 HP on <see cref="AscensionLevel.ToughEnemies"/>,
/// A19's boss tier on <see cref="AscensionLevel.DoubleBoss"/>.
/// </para>
/// <para>
/// One cosmetic omission: vanilla adds a display-only <c>SplitPower</c> in its constructor whose only
/// job is the "will split at half HP" tooltip. There is no shipped StS2 equivalent and authoring one
/// is outside this port's power list, so the tooltip is absent; the mechanic itself is intact.
/// </para>
/// </summary>
public sealed class SlimeBoss : Spire1Monster
{
    private MoveState? _splitState;

    /// <summary>
    /// Shipped <c>SlimedBerserker</c>: the largest slime-flesh creature that ships. Its rig has
    /// <c>idle_loop</c>/<c>attack</c>/<c>hurt</c>/<c>die</c> plus <c>hug</c> and <c>vomit</c> tracks,
    /// but no <c>cast</c>, which <see cref="MonsterModel.GenerateAnimator"/> assumes.
    /// </summary>
    protected override string DonorId => "slimed_berserker";

    /// <summary>
    /// Remaps the engine's default animation triggers onto the tracks this donor rig actually has
    /// (<c>SlimedBerserker.GenerateAnimator</c> is the authority for the names). BaseLib's helper
    /// folds a trigger with no matching track back onto idle, so the absent <c>cast</c> track cannot
    /// produce a broken state.
    /// </summary>
    public override CreatureAnimator GenerateAnimator(MegaSprite controller) =>
        SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt", attackName: "attack");

    /// <summary>
    /// Copied from the donor model (<c>SlimedBerserker.TakeDamageSfxType</c>): the generic hit sound
    /// bank is chosen by this enum rather than by a derived path, so a slime takes squishy hits
    /// instead of the default flesh ones. Death sfx stays off (see <see cref="Spire1Monster"/>).
    /// </summary>
    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Slime;

    /// <summary><c>HP = 140</c>, <c>A_2_HP = 150</c> (applied from A9 up).</summary>
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 150, 140);

    public override int MaxInitialHp => MinInitialHp;

    /// <summary><c>SLAM_DAMAGE = 35</c>, <c>A_2_SLAM_DAMAGE = 38</c>.</summary>
    private static int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 35);

    /// <summary>Goop Spray makes 3 Slimed, 5 from A19 up.</summary>
    private static int SlimedCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 5, 3);

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Slime Boss",
        [
            ("GOOP_SPRAY_MOVE", "Goop Spray"),
            ("PREPARING_MOVE", "Preparing"),
            ("SLAM_MOVE", "Slam"),
            ("SPLIT_MOVE", "Split"),
        ]);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // Vanilla's STICKY intent is STRONG_DEBUFF, from before StS2 had a dedicated
        // "adds status cards" intent; StatusIntent is the shipped equivalent and previews the count.
        MoveState goopSpray = new("GOOP_SPRAY_MOVE", GoopSprayMove, new StatusIntent(SlimedCount));
        MoveState preparing = new("PREPARING_MOVE", PreparingMove, new UnknownIntent());
        MoveState slam = new("SLAM_MOVE", SlamMove, new SingleAttackIntent(SlamDamage));
        // Vanilla shows UNKNOWN for SPLIT, not a summon intent. Forced from the damage hook
        // mid-turn, so it must survive the next roll - otherwise the machine walks straight on to
        // its follow-up (shipped idiom: WaterfallGiant's ABOUT_TO_BLOW state).
        MoveState split = new("SPLIT_MOVE", SplitMove, new UnknownIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };

        goopSpray.FollowUpState = preparing;
        preparing.FollowUpState = slam;
        slam.FollowUpState = goopSpray;
        // Never reached by the chain: the split turn ends with the boss dead. Present so the state is
        // registered and can be forced from the damage hook.
        split.FollowUpState = goopSpray;

        _splitState = split;
        List<MonsterState> states = [goopSpray, preparing, slam, split];
        return new MonsterMoveStateMachine(states, goopSpray);
    }

    /// <summary>
    /// <c>SlimeBoss.damage()</c>: at or below half HP, interrupt whatever was queued and split next
    /// turn. Vanilla compares against <c>maxHealth / 2f</c>, hence the decimal halving.
    /// </summary>
    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Creature || Creature.IsDead || _splitState == null || NextMove.Id == _splitState.Id)
            return Task.CompletedTask;
        if (Creature.CurrentHp > (decimal)Creature.MaxHp / 2m)
            return Task.CompletedTask;
        SetMoveImmediate(_splitState, forceTransition: true);
        return Task.CompletedTask;
    }

    private async Task GoopSprayMove(IReadOnlyList<Creature> targets)
    {
        await CardPileCmd.AddToCombatAndPreview<Slimed>(targets, PileType.Discard, SlimedCount, null);
    }

    /// <summary>
    /// <c>PREP_SLAM</c> is pure telegraphing in vanilla too — a shout and a screen shake, no game
    /// effect — so the move body only spends the beat that the shout occupied.
    /// </summary>
    private async Task PreparingMove(IReadOnlyList<Creature> targets)
    {
        await Cmd.CustomScaledWait(0.25f, 0.5f);
    }

    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlamDamage).FromMonster(this).WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    /// <summary>
    /// <c>SPLIT</c>: one Spike Slime (L) and one Acid Slime (L), each constructed with the boss's
    /// current HP as both max and current HP (vanilla passes <c>currentHealth</c> straight into the
    /// child constructors), then the boss removes itself.
    /// <para>
    /// HP is handed over through the mod's own <see cref="ISlimeSplitSpawn.SpawnHp"/> preset, the
    /// same channel the L slimes use when they split into M slimes, so it is applied while the
    /// creature is being built instead of being corrected afterwards. The model has to be a mutable
    /// clone - <c>CreatureCmd.Add</c> asserts mutability, and <c>ModelDb</c> hands out canonical
    /// instances.
    /// </para>
    /// </summary>
    private async Task SplitMove(IReadOnlyList<Creature> targets)
    {
        int inheritedHp = Creature.CurrentHp;
        var side = Creature.Side;

        var spikeSlime = (SpikeSlimeL)ModelDb.Monster<SpikeSlimeL>().ToMutable();
        spikeSlime.SpawnHp = inheritedHp;
        await CreatureCmd.Add(spikeSlime, CombatState, side);

        var acidSlime = (AcidSlimeL)ModelDb.Monster<AcidSlimeL>().ToMutable();
        acidSlime.SpawnHp = inheritedHp;
        await CreatureCmd.Add(acidSlime, CombatState, side);

        await CreatureCmd.Kill(Creature);
    }
}
