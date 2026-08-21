using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Bonfire Spirits. Offer a card to the bonfire; the reward scales with the card's
/// rarity (from the StS1 bytecode rarity switch):
///   Basic   -> nothing
///   Special -> heal 5
///   Common  -> heal 5
///   Uncommon-> heal to full
///   Rare    -> +10 Max HP and heal to full
///   Curse   -> Spirit Poop relic (shipped Circlet if Spirit Poop is already owned)
/// </summary>
public class Bonfire : Spire1Event
{
    protected override string ShippedPortrait => "luminous_choir";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Continue)
        ];
    }

    private Task Continue()
    {
        SetEventState(PageDescription("INTRO"), [Option(Offer, "INTRO")]);
        return Task.CompletedTask;
    }

    private async Task Offer()
    {
        // StS1 lets the player offer ANY purgeable card, curses included: the CURSE arm of the rarity
        // switch grants Spirit Poop, or a Circlet when Spirit Poop is already owned
        // (bytecode: hasRelic("Spirit Poop") ? new Circlet() : new SpiritPoop(), then spawnRelicAndObtain).
        // Both relics exist now, so nothing is filtered out of the selection.
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(Owner,
            new CardSelectorPrefs(new LocString("events", "SPIRE1-BONFIRE.selectionScreenPrompt"), 1))).ToList();
        if (cards.Count == 0)
        {
            // No eligible cards: StS1 shows "Nothing happens..." without removing anything.
            SetEventFinished(PageDescription("NOTHING"));
            return;
        }
        CardModel offered = cards[0];
        await CardPileCmd.RemoveFromDeck(offered);
        // StS1 rarity switch (bytecode): SPECIAL and COMMON heal 5, UNCOMMON heals to full,
        // RARE gives +10 Max HP + full heal, BASIC does nothing, and every other rarity falls into
        // the default case (nothing). StS2's Status rarity is StS1's SPECIAL, so it heals 5 too.
        switch (offered.Rarity)
        {
            case CardRarity.Uncommon:
                await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
                SetEventFinished(PageDescription("FULL_HEAL"));
                break;
            case CardRarity.Rare:
                await CreatureCmd.GainMaxHp(Owner.Creature, 10);
                await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
                SetEventFinished(PageDescription("GREAT"));
                break;
            case CardRarity.Common:
            // StS1's single SPECIAL rarity is split in StS2 into Token, Event and Status.
            case CardRarity.Token:
            case CardRarity.Event:
            case CardRarity.Status:
                await CreatureCmd.Heal(Owner.Creature, 5);
                SetEventFinished(PageDescription("HEAL"));
                break;
            case CardRarity.Curse:
                await RelicCmd.Obtain(
                    Owner.GetRelic<SpiritPoop>() != null
                        ? ModelDb.Relic<Circlet>().ToMutable()
                        : ModelDb.Relic<SpiritPoop>().ToMutable(),
                    Owner);
                SetEventFinished(PageDescription("CURSE"));
                break;
            default:
                SetEventFinished(PageDescription("NOTHING"));
                break;
        }
    }
}
