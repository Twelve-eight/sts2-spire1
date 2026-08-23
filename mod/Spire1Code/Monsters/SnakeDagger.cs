using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The Beyond — Snake Dagger (<c>com.megacrit.cardcrawl.monsters.beyond.SnakeDagger</c>),
/// the Reptomancer's summoned minion. 官方中文名：蛇匕首。
/// <para>
/// Bytecode: HP monsterHpRng.random(20, 25) per instance (no ascension variant); STAB_DMG 9,
/// SACRIFICE_DMG 25. getMove: firstMove → WOUND, else EXPLODE forever.
/// takeTurn WOUND: ChangeState ATTACK + DamageAction(damage[0] = 9, SLASH_HORIZONTAL) +
/// MakeTempCardInDiscardAction(new Wound(), 1). takeTurn EXPLODE: ChangeState SUICIDE +
/// DamageAction(damage[1] = 25, SLASH_HEAVY), then LoseHPAction(this, this, currentHealth) —
/// the dagger spends its whole remaining HP and dies right after the strike.
/// </para>
/// <para>
/// The suicide uses <c>CreatureCmd.Kill</c> (the game's normal death path, ExplosivePower /
/// SlimeBoss pattern) standing in for StS1's full-current-health LoseHPAction. Wound is our
/// ported status card (<see cref="Wound"/>), added to the discard pile via
/// <c>CardPileCmd.AddToCombatAndPreview</c> — the same call shipped monsters use.
/// Donor: <c>stabbot</c> — a small pointy stabbing construct with a standard rig; closest
/// silhouette among the shipped scenes for a floating dagger.
/// </summary>
public sealed class SnakeDagger : Spire1Monster
{
    // constructor: monsterHpRng.random(20, 25) — the engine draws uniformly from these bounds.
    public override int MinInitialHp => 20;

    public override int MaxInitialHp => 25;

    // STAB_DMG = 9 / SACRIFICE_DMG = 25 (no ascension variants)
    private const int WoundStrikeDamage = 9;

    private const int ExplodeDamage = 25;

    protected override string DonorId => "stabbot";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState wound = new("WOUND_MOVE", WoundMove,
            new SingleAttackIntent(WoundStrikeDamage), new StatusIntent(1));
        MoveState explode = new("EXPLODE_MOVE", ExplodeMove, new SingleAttackIntent(ExplodeDamage));

        // getMove: first move always WOUND; afterwards EXPLODE (which also ends this dagger).
        wound.FollowUpState = explode;
        explode.FollowUpState = explode;
        return new MonsterMoveStateMachine([wound, explode], wound);
    }

    private async Task WoundMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn WOUND: DamageAction(SLASH_HORIZONTAL) + one Wound into the discard pile.
        await DamageCmd.Attack(WoundStrikeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Discard, 1, null);
    }

    private async Task ExplodeMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn EXPLODE: ChangeState SUICIDE + DamageAction(SLASH_HEAVY), then
        // LoseHPAction(this, this, currentHealth) -> kill through the normal death path.
        await DamageCmd.Attack(ExplodeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.Kill(base.Creature);
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json).
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Snake Dagger", "蛇匕首"),
        [
            ("WOUND_MOVE", Tr("Wound", "创伤")),
            ("EXPLODE_MOVE", Tr("Sacrifice", "牺牲")),
        ]);
}
