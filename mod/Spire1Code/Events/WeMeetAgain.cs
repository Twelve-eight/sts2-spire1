using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — We Meet Again! Give Ranwid a potion, gold or a card and receive a random relic,
/// or attack him (he just runs away — no damage). Potion / gold / card offers are locked when the
/// player cannot pay them.
/// </summary>
public class WeMeetAgain : Spire1Event
{
    private const int _minGold = 50;

    private const int _maxGold = 150;

    private PotionModel? _potionOption;

    private CardModel? _cardOption;

    private int _goldAmount;

    protected override string ShippedPortrait => "relic_trader";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Gold", 0m), new StringVar("Potion"), new StringVar("Card")];

    public override void CalculateVars()
    {
        _potionOption = Rng.NextItem(Owner.Potions);
        if (_potionOption != null)
        {
            ((StringVar)DynamicVars["Potion"]).StringValue = _potionOption.Title.GetFormattedText();
        }
        // StS1: gold < 50 -> no offer; gold > 150 -> random(50, 150); else random(50, gold). Inclusive.
        _goldAmount = Owner.Gold < _minGold
            ? 0
            : Owner.Gold > _maxGold
                ? Rng.NextInt(_minGold, _maxGold + 1)
                : Rng.NextInt(_minGold, Owner.Gold + 1);
        if (_goldAmount > 0)
        {
            ((IntVar)DynamicVars["Gold"]).BaseValue = _goldAmount;
        }
        // StS1: random non-basic, non-curse deck card.
        List<CardModel> eligible = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c.Rarity != CardRarity.Basic && c.Type != CardType.Curse)
            .ToList();
        _cardOption = Rng.NextItem(eligible);
        if (_cardOption != null)
        {
            ((StringVar)DynamicVars["Card"]).StringValue = _cardOption.Title;
        }
    }

    protected override Task BeforeEventStarted(bool isPreFinished)
    {
        // Prevents the potion bar from letting the player chug the offered potion mid-event.
        Owner.CanUseOrRemovePotions = false;
        return Task.CompletedTask;
    }

    protected override void OnEventFinished()
    {
        Owner.CanUseOrRemovePotions = true;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            _potionOption != null ? Option(GivePotion) : LockedOption("POTION_LOCKED"),
            _goldAmount > 0 ? Option(GiveGold) : LockedOption("GOLD_LOCKED"),
            _cardOption != null ? Option(GiveCard) : LockedOption("CARD_LOCKED"),
            Option(Attack)
        ];
    }

    private async Task GivePotion()
    {
        if (_potionOption == null)
        {
            return;
        }
        await PotionCmd.Discard(_potionOption);
        await ObtainRandomRelic();
        SetEventFinished(PageDescription("POTION"));
    }

    private async Task GiveGold()
    {
        await PlayerCmd.LoseGold(_goldAmount, Owner, GoldLossType.Spent);
        await ObtainRandomRelic();
        SetEventFinished(PageDescription("GOLD"));
    }

    private async Task GiveCard()
    {
        if (_cardOption == null)
        {
            return;
        }
        await CardPileCmd.RemoveFromDeck(_cardOption);
        await ObtainRandomRelic();
        SetEventFinished(PageDescription("CARD"));
    }

    private async Task ObtainRandomRelic()
    {
        // StS1: random relic tier -> random screenless relic of that tier, obtained immediately.
        RelicModel relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
    }

    private Task Attack()
    {
        // StS1: he screams and runs away; no damage is dealt to the player.
        SetEventFinished(PageDescription("ATTACK"));
        return Task.CompletedTask;
    }
}
