using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — Taskmaster (<c>com.megacrit.cardcrawl.monsters.city.Taskmaster</c>; vanilla
/// reuses the <c>SlaverBoss</c> localization id). 官方中文名：监工。
/// <para>
/// Bytecode: ELITE; HP monsterHpRng 54-60, A8 57-64; woundCount 1 (A3 2, A18 3); damage list
/// [4, 7] with only [1] referenced. Single move SCOURING_WHIP (byte 2, ATTACK_DEBUFF, 7):
/// takeTurn = slow SLASH_HEAVY hit + MakeTempCardInDiscardAction(Wound ×woundCount), then on
/// A18+ ApplyPowerAction(StrengthPower +1) — the whip both litters the discard pile and hardens
/// the Taskmaster itself at the top tier.
/// </para>
/// <para>
/// Ascension mapping: HP A8 tier → <see cref="AscensionLevel.ToughEnemies"/>; the wound tiers
/// split across the two shipped levers (A3 → ToughEnemies, A18 → DeadlyEnemies) and the A18
/// self-strength maps onto <see cref="AscensionLevel.DeadlyEnemies"/> via
/// <see cref="AscensionHelper.HasAscension"/>, matching GremlinNob's deterministic-branch idiom.
/// The whip damage is tier-free in the bytecode (7 in every band) so no damage lever is used.
/// </para>
/// <para>
/// Donor: <c>flail_knight</c> — the shipped armored humanoid wielding a long-reach weapon;
/// closest silhouette among the shipped scenes for a slaver master cracking a scourge.
/// </para>
/// </summary>
public sealed class Taskmaster : Spire1Monster
{

    protected override string DonorId => "flail_knight";
    // monsterHpRng.random(54, 60); ascension >= 8 -> setHp(57, 64)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 57, 54);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 64, 60);

    // SCOURING_WHIP hits for 7 in every ascension band (damage[1]; damage[0]=4 is never used).
    private const int WhipDamage = 7;

    // woundCount = 1; ascension >= 3 -> 2; ascension >= 18 -> 3. Mid tier rides ToughEnemies,
    // top tier DeadlyEnemies (same span split as Mystic's strength tiers).
    private int WoundCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3,
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 2, 1));

    /// <summary>StS1 A18+ also grants 1 Strength after each whip; mapped onto DeadlyEnemies.</summary>
    private static bool IsHardMode => AscensionHelper.HasAscension(AscensionLevel.DeadlyEnemies);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // Bytecode getMove ignores its roll: setMove(SCOURING_WHIP, ATTACK_DEBUFF, 7) every turn.
        MoveState whip = new("SCOURING_WHIP_MOVE", ScouringWhipMove,
            new SingleAttackIntent(WhipDamage), new DebuffIntent());
        whip.FollowUpState = whip;
        return new MonsterMoveStateMachine([whip], whip);
    }

    // takeTurn SCOURING_WHIP: slow hit, Wound ×woundCount into the player's discard pile, then
    // Strength +1 on the A18 tier.
    private async Task ScouringWhipMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(WhipDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Wound>(targets, PileType.Discard, WoundCount, null);
        if (IsHardMode)
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        }
    }

    // zh monster name is the official StS1 zhs string (.tmp/m25-zhs-names.json); move title
    // follows the same localization style.
    private static string Tr(string eng, string zhs) =>
        LocManager.Instance != null && LocManager.Instance.Language == "zhs" ? zhs : eng;

    public override List<(string, string)>? Localization =>
        new MonsterLoc(Tr("Taskmaster", "监工"),
        [
            ("SCOURING_WHIP_MOVE", Tr("Scouring Whip", "惩戒之鞭")),
        ]);
}
