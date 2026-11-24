using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad - Perfected Strike (Common Attack, cost 2). Deal 6 damage; deals 2 additional
/// damage for ALL your cards containing "Strike" (3 upgraded).
/// <para>
/// AUTHORITY (StS1 bytecode, .tmp/audit/javap/PerfectedStrike.txt):
/// &lt;init&gt; @0-63 -> cost 2, baseDamage 6, baseMagicNumber 2, magicNumber 2, CardTags.STRIKE;
/// countCards @0-138 -> hand + drawPile + discardPile only; isStrike @0-7 -> hasTag(STRIKE);
/// calculateCardDamage @0-52 / applyPowers @0-51 -> baseDamage + magicNumber * countCards();
/// upgrade @0-30 -> upgradeMagicNumber(1), i.e. magicNumber 2 -> 3 while baseDamage stays 6.
/// </para>
/// <para>
/// COUNT DOMAIN: the shipped StS2 PerfectedStrike sums PlayerCombatState.AllCards, which is all
/// five piles (Hand/Draw/Discard/Exhaust/Play - PlayerCombatState.cs:69-79). That is not the
/// legacy three-pile domain. <see cref="LegacyStrikeCount"/> reads only Hand/DrawPile/DiscardPile
/// and projects the played card back when StS1 would still see it in hand:
/// AbstractPlayer.javap-asc useCard @4242-4311 calls calculateCardDamage @16 before
/// hand.removeCard @109. The projection is bound to the exact CardPlay executing this OnPlay (not
/// to a captured snapshot) and is limited to its first non-auto play; draw-top auto-plays and
/// CreateDupe copies start outside the hand and are not projected. Native same-object Replay and
/// the full damage pipeline stay outside the verified boundary (see
/// reports/PerfectedProductionContent.md and the integration hand-off).
/// </para>
/// </summary>
// FieldDriftTwin registration: BaseLib CustomContentDictionary.AddModel (called by the
// CustomCardModel ctor) consumes this attribute and calls ModHelper.AddModelToPool at
// construction; SharedCardReuse.InjectTwin then adds the same own type to Spire1CardPool for
// non-pure mode. ModHelper.ConcatModelsFromMods concatenates without dedup, so a
// Spire1CardPool attribute here would put the canonical card twice in
// Spire1CardPool.AllCards. Spire1LegacyPool is the retired authored-card pool; the
// Spire1Card.Pool override resolves it back to Spire1CardPool for UI/old-save paths. This is
// the same pattern as BodySlam/PommelStrike/TwinStrike/Reboot (all FieldDriftTwins).
[Pool(typeof(Spire1LegacyPool))]
public class PerfectedStrike() : Spire1Card(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private CardPlay? _activePlay;

    internal CardPlay? ActivePlay => _activePlay;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // StS1 fields: CalculationBaseVar mirrors baseDamage 6; ExtraDamageVar mirrors the
        // per-Strike magicNumber 2 (OnUpgrade applies upgradeMagicNumber(1) -> 3).
        ..CustomCardModel.MakeCalculatedDamage(6,
            static (card, _) => LegacyStrikeCount.Count(card, (card as PerfectedStrike)?.ActivePlay),
            mult: 2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // CalculatedDamageVar.Calculate runs inside AttackCommand.Execute (AttackCommand.cs:669),
        // after CardModel.OnPlayWrapper has moved the card to the Play pile (CardModel.cs:1862-1876)
        // and created this exact CardPlay (CardModel.cs:1915-1925). Bind that play so the count uses
        // its IsAutoPlay/PlayIndex instead of guessing the origin or caching a stale snapshot.
        _activePlay = play;
        try
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
        }
        finally
        {
            _activePlay = null;
        }
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1m); // 2 -> 3
}
