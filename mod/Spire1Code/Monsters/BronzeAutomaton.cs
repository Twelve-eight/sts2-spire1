using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City boss — Bronze Automaton (<c>com.megacrit.cardcrawl.monsters.city.BronzeAutomaton</c>).
/// 官方中文名：铜制机械人偶（<c>.tmp/m25-zhs-names.json</c>）。
/// <para>
/// Bytecode (<c>city_BronzeAutomaton.txt</c>): setHp(300), A9 320; blockAmt 9 (A9 12);
/// flailDmg 7 (A4 8); beamDmg 45 (A4 50); strAmt 3 (A4 4). usePreBattleAction: ArtifactPower(3).
/// Moves: SPAWN_ORBS(4) summons two <see cref="BronzeOrb"/>s with UNKNOWN intent; FLAIL(1) is a
/// two-hit flailDmg attack (SLASH_DIAGONAL each); HYPER_BEAM(2) hits for beamDmg after a
/// LaserBeamEffect windup (AttackEffect.NONE); STUNNED(3) wastes the turn ("Stunned!" text only,
/// STUN intent); BOOST(5) gains blockAmt block and strAmt strength (DEFEND_BUFF intent).
/// </para>
/// <para>
/// getMove is fully deterministic — there is no roll input at all:
/// firstTurn → SPAWN_ORBS; then <c>numTurns == 4</c> → HYPER_BEAM (numTurns reset);
/// lastMove(HYPER_BEAM) → A19 ? BOOST : STUNNED; lastMove(STUNNED|BOOST|SPAWN_ORBS) → FLAIL;
/// else BOOST. numTurns increments on every FLAIL/BOOST selection, so between hyper beams the
/// cycle is Flail→Boost→Flail→Boost (four counted turns). Modelled as one conditional branch in
/// vanilla predicate order, with the counter maintained at perform time — nothing can force or
/// re-roll this boss's moves mid-flight, and branch predicates must stay side-effect-free
/// (BookOfStabbing idiom). All five move states chain back into the same branch.
/// </para>
/// <para>
/// die(): screen-shake cosmetics plus onBossVictoryLogic() are engine-side here; the mechanical
/// part — every survivor left in the room suicides (HideHealthBar + SuicideAction + Inflame VFX)
/// so killing the boss ends the fight instantly even with orbs alive — is reproduced in
/// <see cref="AfterDeath"/> via <see cref="CreatureCmd.Kill"/>, the engine's normal death path.
/// The orbs are primary enemies (no MinionPower port-side), so the engine's own
/// secondary-enemy cascade does not cover them and the explicit sweep is required.
/// BGM handling, markBossAsSeen and the AUTOMATON unlock/achievement have no mod surface and are
/// dropped. Spawn positions/slots (<c>(-300f,200f)</c>/<c>(200f,130f)</c>) do not map onto
/// CreatureCmd.Add's side-based layout, and the per-orb random spawn SFX is cosmetic — both are
/// not ported (SlimeBoss split precedent).
/// </para>
/// <para>
/// Ascension mapping follows the shipped boss convention (cf. SlimeBoss): A9 HP/block tier →
/// <see cref="AscensionLevel.ToughEnemies"/>, A4 damage tiers →
/// <see cref="AscensionLevel.DeadlyEnemies"/>, and the A19 post-beam "BOOST instead of STUNNED"
/// deterministic branch → <see cref="AscensionHelper.HasAscension"/><c>(</c><see cref="AscensionLevel.DoubleBoss"/><c>)</c>.
/// Donor: <c>mecha_knight</c> — the shipped giant armored mech rig (idle_loop/hurt/die plus
/// attack_flame/attack_cleave/charge/wind_up tracks); closest silhouette among the shipped scenes
/// for the city's towering bronze robot boss, whose shipped model even sits on the same
/// 300/320 HP tier.
/// </para>
/// </summary>
public sealed class BronzeAutomaton : Spire1Monster
{
    // setHp(300); ascension >= 9 -> setHp(320)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 320, 300);

    public override int MaxInitialHp => MinInitialHp;

    // FLAIL_DMG = 7; ascension >= 4 -> 8
    private int FlailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);

    // BEAM_DMG = 45; ascension >= 4 -> 50
    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 50, 45);

    // BLOCK_AMT = 9; ascension >= 9 -> 12 (rides the HP tier, not the damage tier)
    private int BoostBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 9);

    // STR_AMT = 3; ascension >= 4 -> 4
    private int BoostStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    /// <summary>A19+ skips the stunned recovery turn after Hyper Beam (BOOST instead).</summary>
    private static bool SkipsStunRecovery => AscensionHelper.HasAscension(AscensionLevel.DoubleBoss);

    /// <summary>Vanilla field numTurns: FLAIL/BOOST selections since the last HYPER_BEAM.</summary>
    private int _numTurns;

    protected override string DonorId => "mecha_knight";

    /// <summary>
    /// Remaps the engine's default animation triggers onto the tracks this donor rig actually has
    /// (MechaKnight.GenerateAnimator is the authority for the names): Attack → attack_cleave,
    /// Cast → attack_flame (the flamethrower track doubles as the hyper-beam burn), Hit → hurt.
    /// The rig has no cast track of its own; without this mapping the engine defaults would drop
    /// both attack animations silently (Lagavulin precedent).
    /// </summary>
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return SetupAnimationState(controller, "idle_loop", "die", hitName: "hurt",
            attackName: "attack_cleave", castName: "attack_flame");
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new ArtifactPower(this, 3)).
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState spawnOrbs = new("SPAWN_ORBS_MOVE", SpawnOrbsMove, new UnknownIntent());
        MoveState flail = new("FLAIL_MOVE", FlailMove, new MultiAttackIntent(FlailDamage, 2));
        MoveState hyperBeam = new("HYPER_BEAM_MOVE", HyperBeamMove, new SingleAttackIntent(BeamDamage));
        MoveState boost = new("BOOST_MOVE", BoostMove, new DefendIntent(), new BuffIntent());
        MoveState stunned = new("STUNNED_MOVE", StunnedMove, new StunIntent())
        {
            // Shipped stun idiom (CeremonialBeast/Lagavulin): never transition away unperformed.
            MustPerformOnceBeforeTransitioning = true
        };

        ConditionalBranchState branch = new("BRONZE_AUTOMATON_BRANCH");
        spawnOrbs.FollowUpState = branch;
        flail.FollowUpState = branch;
        hyperBeam.FollowUpState = branch;
        boost.FollowUpState = branch;
        stunned.FollowUpState = branch;

        // getMove priority order, verbatim: beam timer, post-beam recovery, post-idle flail,
        // default boost.
        branch.AddState(hyperBeam, () => _numTurns == 4);
        branch.AddState(boost, () => LastWas(hyperBeam) && SkipsStunRecovery);
        branch.AddState(stunned, () => LastWas(hyperBeam));
        branch.AddState(flail, () => LastWas(stunned) || LastWas(boost) || LastWas(spawnOrbs));
        branch.AddState(boost, () => true);
        return new MonsterMoveStateMachine([spawnOrbs, flail, hyperBeam, boost, stunned, branch], spawnOrbs);
    }

    /// <summary>
    /// takeTurn SPAWN_ORBS: two SpawnMonsterAction calls, one BronzeOrb each (UNKNOWN intent —
    /// vanilla shows no summon icon). Vanilla passes aiRng-randomized spawn SFX per orb; cosmetic.
    /// </summary>
    private async Task SpawnOrbsMove(IReadOnlyList<Creature> targets)
    {
        _ = targets;
        var side = Creature.Side;

        var orbLeft = (BronzeOrb)ModelDb.Monster<BronzeOrb>().ToMutable();
        await CreatureCmd.Add(orbLeft, CombatState, side);

        var orbRight = (BronzeOrb)ModelDb.Monster<BronzeOrb>().ToMutable();
        await CreatureCmd.Add(orbRight, CombatState, side);
    }

    private async Task FlailMove(IReadOnlyList<Creature> targets)
    {
        // numTurns++ lives in getMove in vanilla (roll time); nothing can force or re-roll this
        // boss between roll and perform, so counting here keeps the branch predicates pure while
        // producing the identical sequence.
        _numTurns++;
        // takeTurn FLAIL: AnimateFastAttackAction + two DamageActions (SLASH_DIAGONAL).
        await DamageCmd.Attack(FlailDamage).WithHitCount(2).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task HyperBeamMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn HYPER_BEAM resets numTurns to 0 (vanilla does it inside getMove).
        _numTurns = 0;
        // LaserBeamEffect windup above the hitbox, then DamageAction(NONE) — no impact fx.
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await DamageCmd.Attack(BeamDamage).FromMonster(this)
            .Execute(null);
    }

    private async Task BoostMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn BOOST: GainBlockAction(blockAmt) + ApplyPowerAction(StrengthPower(strAmt)).
        // No animation action in vanilla either.
        _numTurns++;
        await CreatureCmd.GainBlock(base.Creature, BoostBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature,
            BoostStrength, base.Creature, null);
    }

    private Task StunnedMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn STUNNED: TextAboveCreatureAction(STUNNED) only — the turn is wasted.
        return Task.CompletedTask;
    }

    /// <summary>
    /// die(): after the boss itself dies, every survivor left in the room suicides so the boss
    /// kill ends the fight outright (vanilla iterates room monsters that are neither dead nor
    /// dying and queues HideHealthBar + SuicideAction + Inflame VFX on each).
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature)
        {
            return;
        }
        foreach (Creature survivor in CombatState.Enemies
            .Where(c => c != base.Creature && c.IsAlive)
            .ToList())
        {
            await CreatureCmd.Kill(survivor);
        }
    }

    private bool LastWas(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count > 0 && ReferenceEquals(log[^1], state);
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move titles
    // follow the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Bronze Automaton", "铜制机械人偶"),
        [
            ("SPAWN_ORBS_MOVE", Tr("Summon", "召唤")),
            ("FLAIL_MOVE", Tr("Flail", "乱击")),
            ("HYPER_BEAM_MOVE", Tr("Hyper Beam", "超级光束")),
            ("STUNNED_MOVE", Tr("Stunned", "晕眩")),
            ("BOOST_MOVE", Tr("Boost", "增强")),
        ]);
}
