using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Relics;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City - Augmenter (Drug Dealer).
/// Three trades: gain a J.A.X. card, transform 2 cards, or gain the Mutagenic Strength relic.
///
/// All three trades are free. <c>com.megacrit.cardcrawl.events.city.DrugDealer.buttonEffect</c> hands
/// over the reward and nothing else in each of its three cases: there is no damage, gold or max-HP
/// call anywhere in the method.
/// </summary>
public class DrugDealer : Spire1Event
{
    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "the_future_of_potions";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> options =
        [
            // Slugify("TestJax") == "TEST_JAX", matching the existing loc keys.
            Option(TestJax),
        ];
        if (PileType.Deck.GetPile(Owner).Cards.Count(c => c.IsRemovable) >= 2)
        {
            options.Add(Option(TransformCards));
        }
        else
        {
            options.Add(LockedOption("TRANSFORM_CARDS_LOCKED"));
        }
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics))
        {
            options.Add(Option(IngestMutagens, HoverTipFactory.FromRelic<MutagenicStrength>()));
        }
        return options;
    }

    private async Task TestJax()
    {
        // Cards content group gate (C13, 2026-10-02): J.A.X. is a Spire1 card created directly here, so
        // Spire1Card.CanBeGeneratedInCombat/CanBeGeneratedByModifiers cannot stop this grant. Fail closed
        // before any card is constructed. The option stays visible on purpose: TRANSFORM_CARDS can be a
        // locked option and the mutagen option is hidden while the relics group is off, so removing this
        // option would leave an event with no clickable option. A stale page or a direct debug invocation
        // therefore lands on the same no-grant finish the relic gate already uses below.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] DrugDealer J.A.X. option invoked while cards content group is off; grant skipped");
            SetEventFinished(PageDescription("INITIAL"));
            return;
        }

        await CardPileCmd.Add(Owner.RunState.CreateCard<JAX>(Owner), PileType.Deck);
        SetEventFinished(PageDescription("JAX"));
    }

    private async Task TransformCards()
    {
        CardSelectorPrefs prefs = new(L10NLookup($"{Id.Entry}.pages.TRANSFORM.selectionScreenPrompt"), 2)
        {
            Cancelable = false,
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForTransformation(Owner, prefs);
        foreach (CardModel card in selected)
        {
            await CardCmd.TransformToRandom(card, Rng);
        }
        SetEventFinished(PageDescription("TRANSFORM"));
    }

    private async Task IngestMutagens()
    {
        // A stale event page or a direct debug invocation must not bypass the relic group gate.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics))
        {
            MainFile.Logger.Warn("[Spire1] DrugDealer mutagen option invoked while relics content group is off; grant skipped");
            SetEventFinished(PageDescription("INITIAL"));
            return;
        }

        // StS1 buttonEffect case 2 is exactly:
        //   if (!player.hasRelic("MutagenicStrength")) spawnRelicAndObtain(new MutagenicStrength());
        //   else                                       spawnRelicAndObtain(new Circlet());
        // and nothing more - the trade is free, like [Test J.A.X.].
        //
        // Circlet is not reimplemented: StS2 ships it (MegaCrit.Sts2.Core.Models.Relics/Circlet.cs,
        // RelicRarity.None + IsStackable), which is precisely StS1's "you already own the reward relic"
        // consolation prize, so the shipped relic is granted.
        if (Owner.GetRelic<MutagenicStrength>() == null)
        {
            await RelicCmd.Obtain<MutagenicStrength>(Owner);
        }
        else
        {
            await RelicCmd.Obtain<Circlet>(Owner);
        }
        SetEventFinished(PageDescription("MUTAGENS"));
    }
}
