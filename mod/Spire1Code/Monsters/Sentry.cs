using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 Exordium — Sentry (<c>com.megacrit.cardcrawl.monsters.exordium.Sentry</c>).
/// <para>
/// Bytecode: HP 38-42, A8 39-45; beamDmg 9 (A3 10), dazedAmt 2 (A18 3 — dropped, see remarks).
/// usePreBattleAction: ArtifactPower(1). getMove: first move — even slot index BOLT, odd BEAM
/// (the two Sentries alternate so one opens with each); afterwards strict BEAM/BOLT alternation
/// (lastMove(BEAM) ? BOLT : BEAM). takeTurn BOLT = MakeTempCardInDiscard(Dazed, dazedAmt);
/// BEAM = attack(beamDmg).
/// </para>
/// <para>
/// Ascension mapping follows the shipped StS2 monster convention (HP → ToughEnemies,
/// damage → DeadlyEnemies); the vanilla A18 dazedAmt=3 tier is unreachable below the mapping
/// threshold and is intentionally not modelled — base value 2 always applies.
/// </para>
/// <para>
/// Artifact is the engine's shipped <see cref="ArtifactPower"/>; Dazed is our ported status card
/// (<see cref="Dazed"/>), shuffled into the discard pile via
/// <c>CardPileCmd.AddToCombatAndPreview</c> — the same call shipped monsters (Chomper,
/// EyeWithTeeth) use to put Dazed into the discard.
/// </para>
/// </summary>
public sealed class Sentry : Spire1Monster
{
    // setHp(38, 42); ascension >= 8 -> setHp(39, 45)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 39, 38);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 42);

    // beamDmg = 9; ascension >= 3 -> 10
    private int BeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 9);

    // dazedAmt = 2 (vanilla A18 tier of 3 is not modelled — see type remarks)
    private const int DazedAmount = 2;

    // Vanilla first move: slot parity decides which Sentry opens with Bolt.
    private bool _opensWithBolt;

    private bool _firstMoveDecided;

    // True after Beam has been played; drives strict alternation.
    private bool _lastWasBeam;

    protected override string DonorId => "stabbot";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // usePreBattleAction: ApplyPowerAction(new ArtifactPower(this, 1))
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        // getMove(firstMove): lastIndexOf(this) % 2 == 0 ? BOLT : BEAM
        IReadOnlyList<Creature> enemies = base.CombatState.Enemies;
        int idx = -1;
        for (int i = 0; i < enemies.Count; i++)
            if (ReferenceEquals(enemies[i], base.Creature)) { idx = i; break; }
        _opensWithBolt = idx % 2 == 0;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState bolt = new("BOLT_MOVE", BoltMove, new CardDebuffIntent());
        MoveState beam = new("BEAM_MOVE", BeamMove, new SingleAttackIntent(BeamDamage));
        ConditionalBranchState branch = new("SENTRY_BRANCH");
        bolt.FollowUpState = branch;
        beam.FollowUpState = branch;
        // ConditionalBranchState picks the FIRST satisfied predicate, so order matters:
        // opening turn honours slot parity; afterwards strict alternation — last was BEAM
        // means play BOLT next, otherwise BEAM.
        branch.AddState(bolt, () =>
            !_firstMoveDecided ? _opensWithBolt : _lastWasBeam);
        branch.AddState(beam, () =>
            !_firstMoveDecided ? !_opensWithBolt : !_lastWasBeam);
        return new MonsterMoveStateMachine([bolt, beam, branch], bolt);
    }

    private async Task BoltMove(IReadOnlyList<Creature> targets)
    {
        _firstMoveDecided = true;
        _lastWasBeam = false;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        // MakeTempCardInDiscardAction(new Dazed(), dazedAmt)
        await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, DazedAmount, null);
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        _firstMoveDecided = true;
        _lastWasBeam = true;
        await DamageCmd.Attack(BeamDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(null);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Sentry",
        [
            ("BOLT_MOVE", "Bolt"),
            ("BEAM_MOVE", "Beam"),
        ]);
}
