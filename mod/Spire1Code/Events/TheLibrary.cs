using MegaCrit.Sts2.Core.Entities.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — The Library.
/// Read: choose 1 of 20 generated cards (character pool, normal rarity odds) to add to your deck.
/// Sleep: heal round(33% of Max HP) (20% at Ascension 15+).
/// StS1 constants: 20 cards, HP_HEAL_PERCENT = 0.33f (A_2: 0.2f).
/// </summary>
public class TheLibrary : Spire1Event
{
    private const string _healKey = "Heal";

    private const int _cardChoiceCount = 20;

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "waterlogged_scriptorium";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(_healKey, 0)];

    public override void CalculateVars()
    {
        // StS1: MathUtils.round(maxHealth * (Ascension >= 15 ? 0.2f : 0.33f)) — round half up.
        decimal heal = Owner.Creature.MaxHp * (Owner.RunState.AscensionLevel >= 15 ? 0.2m : 0.33m);
        DynamicVars[_healKey].BaseValue = (int)System.Math.Round(heal, System.MidpointRounding.AwayFromZero);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Read),
            Option(Sleep),
        ];
    }

    private async Task Read()
    {
        // StS1: rollRarity() + getCard(rarity) from the character's card pool, 20 unique cards.
        // StS2's FromChooseACardScreen only supports 3 or fewer cards, so the 1-of-20 grid
        // selector is used instead (identical player-facing behavior).
        CardCreationOptions options = CardCreationOptions.ForNonCombatWithDefaultOdds([Owner.Character.CardPool])
            .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);
        List<CardCreationResult> cards = CardFactory.CreateForReward(Owner, _cardChoiceCount, options).ToList();
        CardSelectorPrefs prefs = new(L10NLookup($"{Id.Entry}.pages.READ.selectionScreenPrompt"), 1)
        {
            Cancelable = false,
        };
        await SelectCardsToAddToDeckFromGrid(cards, prefs);
        // StS1 picks one of three random story texts for the outcome page.
        string bookKey = Rng.NextInt(3) switch
        {
            0 => "BOOK_1",
            1 => "BOOK_2",
            _ => "BOOK_3",
        };
        SetEventFinished(PageDescription(bookKey));
    }

    private async Task Sleep()
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars[_healKey].BaseValue);
        SetEventFinished(PageDescription("SLEEP"));
    }
}
