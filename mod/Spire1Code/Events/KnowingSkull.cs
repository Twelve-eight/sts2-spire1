using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Knowing Skull.
/// Repeatable trades: pay HP for a random potion, 90 gold, or a random colorless Uncommon card.
/// Every cost starts at 6 and increases by 1 each time that same trade is used; asking how to
/// leave costs HP too.
/// StS1 constants: GOLD_REWARD = 90, all costs start at 6 and step by +1.
/// </summary>
public class KnowingSkull : Spire1Event
{
    private const int _goldReward = 90;

    private const int _startingCost = 6;

    private const string _potionCostKey = "PotionCost";

    private const string _goldCostKey = "GoldCost";

    private const string _cardCostKey = "CardCost";

    private const string _leaveCostKey = "LeaveCost";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "brain_leech";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(_potionCostKey, _startingCost),
        new IntVar(_goldCostKey, _startingCost),
        new IntVar(_cardCostKey, _startingCost),
        new IntVar(_leaveCostKey, _startingCost),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        SetEventState(PageDescription("ASK"), AskOptions());
        return Task.CompletedTask;
    }

    private IReadOnlyList<EventOption> AskOptions()
    {
        return
        [
            Option(Potion, "ASK").ThatDoesDamage(DynamicVars[_potionCostKey].BaseValue),
            Option(Gold, "ASK").ThatDoesDamage(DynamicVars[_goldCostKey].BaseValue),
            Option(Card, "ASK").ThatDoesDamage(DynamicVars[_cardCostKey].BaseValue),
            Option(Leave, "ASK").ThatDoesDamage(DynamicVars[_leaveCostKey].BaseValue),
        ];
    }

    private async Task Potion()
    {
        decimal cost = DynamicVars[_potionCostKey].BaseValue;
        await LoseHp(cost);
        DynamicVars[_potionCostKey].BaseValue++;
        // StS1 flashes Sozu instead of giving a potion if the player owns it; the mod has no Sozu,
        // so the potion is always granted.
        await PotionCmd.TryToProcure(PotionFactory.CreateRandomPotionOutOfCombat(Owner, Rng), Owner);
        SetEventState(PageDescription("POTION"), AskOptions());
    }

    private async Task Gold()
    {
        decimal cost = DynamicVars[_goldCostKey].BaseValue;
        await LoseHp(cost);
        DynamicVars[_goldCostKey].BaseValue++;
        await PlayerCmd.GainGold(_goldReward, Owner);
        SetEventState(PageDescription("GOLD"), AskOptions());
    }

    private async Task Card()
    {
        decimal cost = DynamicVars[_cardCostKey].BaseValue;
        await LoseHp(cost);
        DynamicVars[_cardCostKey].BaseValue++;
        // StS1: AbstractDungeon.returnColorlessCard(UNCOMMON).
        CardCreationOptions options = CardCreationOptions.ForNonCombatWithDefaultOdds([ModelDb.CardPool<ColorlessCardPool>()])
            .WithFilter(c => c.Rarity == CardRarity.Uncommon)
            .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);
        CardModel card = CardFactory.CreateForReward(Owner, 1, options).First().Card;
        await CardPileCmd.Add(card, PileType.Deck);
        SetEventState(PageDescription("CARD"), AskOptions());
    }

    private async Task Leave()
    {
        decimal cost = DynamicVars[_leaveCostKey].BaseValue;
        await LoseHp(cost);
        SetEventFinished(PageDescription("LEAVE"));
    }

    private async Task LoseHp(decimal amount)
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
    }
}
