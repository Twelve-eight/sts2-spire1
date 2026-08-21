using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Wing Statue (Golden Wing). Praying removes a card at the cost of 7 HP; destroying the
/// statue is only possible when the deck contains an Attack with 10+ base damage, and pays 50-80 gold.
/// </summary>
public class GoldenWing : Spire1Event
{
    private const int _damage = 7;

    private const int _minGold = 50;

    private const int _maxGold = 80;

    private const int _requiredDamage = 10;

    private int _goldAmount;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "stone_of_all_time";

    public override void CalculateVars()
    {
        // StS1: goldAmount = miscRng.random(50, 80) (inclusive).
        _goldAmount = Rng.NextInt(_minGold, _maxGold + 1);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canDestroy = HasCardWithXDamage(_requiredDamage);
        return
        [
            Option(Pray).ThatDoesDamage(_damage),
            canDestroy ? Option(Destroy) : LockedOption("LOCKED_DESTROY"),
            Option(Leave),
        ];
    }

    /// <summary>
    /// StS1 CardHelper.hasCardWithXDamage: an Attack in the deck whose baseDamage is at least
    /// <paramref name="amount"/>.
    /// </summary>
    private bool HasCardWithXDamage(int amount)
    {
        return PileType.Deck.GetPile(Owner).Cards.Any(c =>
            c.Type == CardType.Attack && c.DynamicVars.TryGetValue("Damage", out DynamicVar damage) && damage.BaseValue >= amount);
    }

    private async Task Pray()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, _damage,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        var card = (await CardSelectCmd.FromDeckForRemoval(Owner,
            new CardSelectorPrefs(L10NLookup($"{Id.Entry}.pages.PRAY.selectionScreenPrompt"), 1))).FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
        SetEventFinished(PageDescription("PRAYED"));
    }

    private async Task Destroy()
    {
        await PlayerCmd.GainGold(_goldAmount, Owner);
        SetEventFinished(PageDescription("DESTROYED"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEFT"));
    }
}
