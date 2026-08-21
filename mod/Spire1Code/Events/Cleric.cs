using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — The Cleric. Paid services: 35 Gold heals 25% of Max HP, 50 Gold removes a card.
/// Both options are locked when the player cannot afford them.
/// </summary>
public class Cleric : Spire1Event
{
    private const int _healCost = 35;

    private const int _purifyCost = 50;

    private const int _a15PurifyCost = 75;

    private const float _healPercent = 0.25f;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "tea_master";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Heal", 0),
        new IntVar("PurifyCost", _purifyCost),
    ];

    public override void CalculateVars()
    {
        // StS1: healAmt = (int)(AbstractDungeon.player.maxHealth * 0.25f).
        DynamicVars["Heal"].BaseValue = (int)(Owner.Creature.MaxHp * _healPercent);
        // StS1: purifyCost = 75 at Ascension 15+, else 50.
        DynamicVars["PurifyCost"].BaseValue = Owner.RunState.AscensionLevel >= 15 ? _a15PurifyCost : _purifyCost;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canAffordHeal = Owner.Gold >= _healCost;
        bool canAffordPurify = Owner.Gold >= DynamicVars["PurifyCost"].IntValue;
        return
        [
            canAffordHeal ? Option(Heal) : LockedOption("LOCKED_HEAL"),
            canAffordPurify ? Option(Purify) : LockedOption("LOCKED_PURIFY"),
            Option(Leave),
        ];
    }

    private async Task Heal()
    {
        await PlayerCmd.LoseGold(_healCost, Owner);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        SetEventFinished(PageDescription("HEALED"));
    }

    private async Task Purify()
    {
        // StS1 opens the purgeable-card grid; if the deck has no purgeable card the gold is not spent.
        var card = (await CardSelectCmd.FromDeckForRemoval(Owner,
            new CardSelectorPrefs(L10NLookup($"{Id.Entry}.pages.PURIFY.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (card != null)
        {
            await PlayerCmd.LoseGold(DynamicVars["PurifyCost"].BaseValue, Owner);
            await CardPileCmd.RemoveFromDeck(card);
        }
        SetEventFinished(PageDescription("PURIFIED"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
    }
}
