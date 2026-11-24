using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect - Scrape (Uncommon Attack, 1 cost).
/// StS1 cards/Scrape.txt::&lt;init&gt; @0-52: cost 1, ATTACK, BLUE, UNCOMMON, ENEMY, baseDamage 7,
/// baseMagicNumber 4; ::upgrade @91-98 raises the damage by 3 and the draw count by 1.
///
/// <para>
/// The shipped v0.113 Scrape (Models.Cards/Scrape.cs:26-34) keeps the numbers but drifts in its
/// filter: one batched discard of every drawn card where
/// `GetWithModifiers(CostModifiers.All) != 0 || CostsX`. StS1 cards/Scrape.txt::use @69-88 instead
/// queues DrawCardAction(magicNumber, ScrapeFollowUpAction), and ScrapeFollowUpAction::update
/// @26-84 iterates exactly DrawCardAction.drawnCards and discards a card only when
/// `card.costForTurn != 0 &amp;&amp; !card.freeToPlayOnce`, moving each card immediately before the next
/// query. This class needs a FieldDriftTwin entry (SharedCardReuse) so the default pool also offers
/// the StS1 rule; in PureSts1Pools mode the authored class is injected directly.
/// </para>
/// <list type="bullet">
/// <item>Membership is the real draw result only (CardPileCmd.cs:1010-1069 returns exactly the cards
/// it moved): the hand and both piles are never scanned, so an already-held card is untouched and a
/// kept 0-cost draw does not truncate later discards (the cost-consumer-integration-probe
/// mutant-first-drawn-only failure set is S01/S03/S06/S07/U02).</item>
/// <item>Each card is queried immediately before its own CardCmd.Discard, so a cost change made by an
/// earlier draw/discard hook is honoured (probe cases S02/S03).</item>
/// <item>CardCmd.Discard (CardCmd.cs:172-206) performs the StS1 moveToDiscardPile +
/// triggerOnManualDiscard + incrementDiscard(false) chain via the real pile move, CardDiscarded
/// history and AfterCardDiscarded hook.</item>
/// <item>X keeps the StS1 -1 sentinel inside LegacyRecallCost.DiscardsOnScrape: the engine stores 0
/// for X (CardEnergyCost.cs:105-108), so an X card is discarded, never kept as if it cost 0.</item>
/// <item>Untracked third-party cards keep the shipped All-cost filter because their StS1 provenance
/// is unknown (probe case U02); only known StS1/vanilla cards use the legacy projection.</item>
/// </list>
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class Scrape() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move), new CardsVar(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // StS1 Scrape.txt::use @38-66: one DamageAction for `damage` with the SLASH_HEAVY effect.
        await CommonActions.CardAttack(this, play).Execute(choiceContext);

        // StS1 Scrape.txt::use @69-88 queues DrawCardAction(magicNumber, ScrapeFollowUpAction); the
        // returned list is the only membership source. Materialize it before any discard can change
        // piles (DrawInternal already returns a snapshot List of exactly what it moved).
        var drawn = (await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner)).ToList();

        foreach (var card in drawn)
        {
            // A card that no longer belongs to this player is not ours to move (the draw is the
            // owner's own draw pile; this mirrors AllForOne's per-transfer owner recheck).
            if (!ReferenceEquals(card.Owner, Owner))
                continue;

            // StS1 ScrapeFollowUpAction::update @52-56 queries costForTurn/freeToPlayOnce per card,
            // immediately before that card's own moveToDiscardPile call.
            if (!LegacyRecallCost.DiscardsOnScrape(card))
                continue;

            // CardCmd.Discard's own entry guard (CardCmd.cs:174-177) drops every later call once the
            // combat is over; stop before querying costs of a torn-down combat.
            if (CombatManager.Instance.IsOverOrEnding)
                break;

            await CardCmd.Discard(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        // StS1 Scrape.txt::upgrade @91-98: upgradeDamage(3) then upgradeMagicNumber(1).
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
