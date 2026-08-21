using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Wheel of Change. The wheel has SIX equally weighted segments (StS1 rolls
/// miscRng.random(0, 5), i.e. uniform 1/6 each): gold (100/200/300 by act), a random relic,
/// full heal, the Decay curse, card removal, or 10% Max HP loss (15% at Ascension 15+).
/// </summary>
public class GremlinWheelGame : Spire1Event
{
    private const int _exordiumGold = 100;

    private const int _cityGold = 200;

    private const int _beyondGold = 300;

    private const decimal _hpLossPercent = 0.10m;

    private const decimal _a15HpLossPercent = 0.15m;

    private enum WheelResult
    {
        Gold,
        Relic,
        FullHeal,
        Curse,
        RemoveCard,
        HpLoss
    }

    private WheelResult _result;

    private int _goldAmount;

    protected override string ShippedPortrait => "endless_conveyor";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("HpLoss", 0m)];

    public override void CalculateVars()
    {
        // StS1: setGold() keyed off the act id; acts past the third keep the Beyond amount.
        _goldAmount = Owner.RunState.CurrentActIndex switch
        {
            0 => _exordiumGold,
            1 => _cityGold,
            _ => _beyondGold
        };
        decimal percent = Owner.RunState.AscensionLevel >= 15 ? _a15HpLossPercent : _hpLossPercent;
        // StS1: (int)(maxHealth * percent) — truncating conversion.
        DynamicVars["HpLoss"].BaseValue = (int)(Owner.Creature.MaxHp * percent);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Play)
        ];
    }

    private Task Play()
    {
        // StS1: result = miscRng.random(0, 5) — six uniform segments.
        _result = (WheelResult)Rng.NextInt(0, 6);
        string page = _result switch
        {
            WheelResult.Gold => "GOLD",
            WheelResult.Relic => "RELIC",
            WheelResult.FullHeal => "HEAL",
            WheelResult.Curse => "CURSE",
            WheelResult.RemoveCard => "REMOVE",
            _ => "DAMAGE"
        };
        SetEventState(PageDescription(page), [Option(ClaimPrize, page)]);
        return Task.CompletedTask;
    }

    private Task ClaimPrize()
    {
        return _result switch
        {
            WheelResult.Gold => TakeGold(),
            WheelResult.Relic => TakeRelic(),
            WheelResult.FullHeal => TakeFullHeal(),
            WheelResult.Curse => TakeCurse(),
            WheelResult.RemoveCard => TakeRemoveCard(),
            _ => TakeHpLoss()
        };
    }

    private async Task TakeGold()
    {
        await PlayerCmd.GainGold(_goldAmount, Owner);
        SetEventFinished(PageDescription("GOLD"));
    }

    private async Task TakeRelic()
    {
        // StS1 adds a random screenless relic to the rewards screen.
        await RewardsCmd.OfferCustom(Owner, [new RelicReward(Owner)]);
        SetEventFinished(PageDescription("RELIC"));
    }

    private async Task TakeFullHeal()
    {
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        SetEventFinished(PageDescription("HEAL"));
    }

    private async Task TakeCurse()
    {
        await CardPileCmd.AddCurseToDeck<Decay>(Owner);
        SetEventFinished(PageDescription("CURSE"));
    }

    private async Task TakeRemoveCard()
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1))).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        SetEventFinished(PageDescription("REMOVE"));
    }

    private async Task TakeHpLoss()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        SetEventFinished(PageDescription("DAMAGE"));
    }
}
