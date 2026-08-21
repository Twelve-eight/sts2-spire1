using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Designer In-Spire. Pay for a service: Adjustments (upgrade 1 card, or 2 random
/// cards), Clean Up (remove 1 card, or transform 2), Full Service (remove 1 card + upgrade 1 random
/// card), or punch the designer. Which Adjustments/Clean Up variant is offered is rolled per event
/// (50/50). Prices and punch damage scale at Ascension 15+ (StS1 bytecode: 40/60/90/3 below A15,
/// 50/75/110/5 at A15+). Services are locked when the player cannot afford them or the deck cannot
/// fulfill them.
/// </summary>
public class Designer : Spire1Event
{
    private bool _adjustmentUpgradesOne;

    private bool _cleanUpRemovesCards;

    private int _adjustCost;

    private int _cleanUpCost;

    private int _fullServiceCost;

    protected override string ShippedPortrait => "colorful_philosophers";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("AdjustCost", 40m),
        new IntVar("CleanUpCost", 60m),
        new IntVar("FullServiceCost", 90m),
        new IntVar("HpLoss", 3m)
    ];

    public override void CalculateVars()
    {
        bool highAscension = Owner.RunState.AscensionLevel >= 15;
        _adjustCost = highAscension ? 50 : 40;
        _cleanUpCost = highAscension ? 75 : 60;
        _fullServiceCost = highAscension ? 110 : 90;
        DynamicVars["AdjustCost"].BaseValue = _adjustCost;
        DynamicVars["CleanUpCost"].BaseValue = _cleanUpCost;
        DynamicVars["FullServiceCost"].BaseValue = _fullServiceCost;
        DynamicVars["HpLoss"].BaseValue = highAscension ? 5 : 3;
        _adjustmentUpgradesOne = Rng.NextBool();
        _cleanUpRemovesCards = Rng.NextBool();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Continue)
        ];
    }

    private Task Continue()
    {
        int deckSize = PileType.Deck.GetPile(Owner).Cards.Count;
        bool hasUpgradable = PileType.Deck.GetPile(Owner).Cards.Any(c => c.IsUpgradable);
        List<EventOption> options =
        [
            _adjustmentUpgradesOne ? Option(AdjustmentsUpgradeOne, "MAIN") : Option(AdjustmentsUpgradeTwo, "MAIN"),
            _cleanUpRemovesCards ? Option(CleanUpRemove, "MAIN") : Option(CleanUpTransform, "MAIN"),
            Option(FullService, "MAIN"),
            Option(Punch, "MAIN")
        ];
        // StS1 greys out each service it cannot fulfill; the text stays the same, so the locked
        // option reuses the exact same loc keys.
        if (Owner.Gold < _adjustCost || !hasUpgradable)
        {
            options[0] = LockedOption(_adjustmentUpgradesOne ? "ADJUSTMENTS_UPGRADE_ONE" : "ADJUSTMENTS_UPGRADE_TWO", "MAIN");
        }
        if (Owner.Gold < _cleanUpCost || deckSize < (_cleanUpRemovesCards ? 1 : 2))
        {
            options[1] = LockedOption(_cleanUpRemovesCards ? "CLEAN_UP_REMOVE" : "CLEAN_UP_TRANSFORM", "MAIN");
        }
        if (Owner.Gold < _fullServiceCost || deckSize == 0)
        {
            options[2] = LockedOption("FULL_SERVICE", "MAIN");
        }
        SetEventState(PageDescription("MAIN"), options);
        return Task.CompletedTask;
    }

    private async Task AdjustmentsUpgradeOne()
    {
        await PlayerCmd.LoseGold(_adjustCost, Owner, GoldLossType.Spent);
        CardModel card = (await CardSelectCmd.FromDeckForUpgrade(Owner, new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1))).FirstOrDefault();
        if (card != null)
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("DONE"));
    }

    private async Task AdjustmentsUpgradeTwo()
    {
        await PlayerCmd.LoseGold(_adjustCost, Owner, GoldLossType.Spent);
        List<CardModel> upgradable = PileType.Deck.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        Rng.Shuffle(upgradable);
        // StS1 upgrades up to 2 random upgradable cards (1 if only one exists).
        foreach (CardModel card in upgradable.Take(2))
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("DONE"));
    }

    private async Task CleanUpRemove()
    {
        await PlayerCmd.LoseGold(_cleanUpCost, Owner, GoldLossType.Spent);
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        SetEventFinished(PageDescription("DONE"));
    }

    private async Task CleanUpTransform()
    {
        await PlayerCmd.LoseGold(_cleanUpCost, Owner, GoldLossType.Spent);
        List<CardModel> cards = (await CardSelectCmd.FromDeckForTransformation(Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2))).ToList();
        foreach (CardModel card in cards)
        {
            await CardCmd.TransformToRandom(card, Rng, CardPreviewStyle.EventLayout);
        }
        SetEventFinished(PageDescription("DONE"));
    }

    private async Task FullService()
    {
        await PlayerCmd.LoseGold(_fullServiceCost, Owner, GoldLossType.Spent);
        List<CardModel> removed = (await CardSelectCmd.FromDeckForRemoval(Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(removed);
        // StS1 upgrades one random upgradable card after the removal (none if nothing is left).
        List<CardModel> upgradable = PileType.Deck.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        Rng.Shuffle(upgradable);
        CardModel? toUpgrade = upgradable.FirstOrDefault();
        if (toUpgrade != null)
        {
            CardCmd.Upgrade(toUpgrade);
        }
        SetEventFinished(PageDescription("DONE"));
    }

    private async Task Punch()
    {
        // StS1 deals HP_LOSS damage, i.e. unblockable and unpowered.
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        SetEventFinished(PageDescription("PUNCHED"));
    }
}
