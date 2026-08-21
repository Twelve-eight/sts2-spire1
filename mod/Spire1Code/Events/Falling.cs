using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Falling. Lose one card of a chosen type (Skill / Power / Attack) to survive the fall.
/// Deck queries mirror StS1's <c>CardHelper.hasCardWithType</c> / <c>returnCardOfType</c>:
/// the deck is scanned for cards whose <see cref="CardType"/> matches, and the random pick uses the event RNG.
/// </summary>
public class Falling : Spire1Event
{
    protected override string ShippedPortrait => "slippery_bridge";

    public override ActModel[] Acts => Act3;

    private bool _hasSkill;

    private bool _hasPower;

    private bool _hasAttack;

    private CardModel? _skillCard;

    private CardModel? _powerCard;

    private CardModel? _attackCard;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("SkillCard"),
        new StringVar("PowerCard"),
        new StringVar("AttackCard"),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO screen: "[Continue]" only.
        return [Option(Continue)];
    }

    private async Task Continue()
    {
        PickCards();
        var options = new List<EventOption>();
        if (!_hasSkill && !_hasPower && !_hasAttack)
        {
            // No card of any of the three types: a single "[Land] " option leads to the feather landing.
            options.Add(Option(Fall, "CHOICE"));
        }
        else
        {
            // StS1 slot order: Skill (Land), Power (Channel), Attack (Strike).
            options.Add(_hasSkill
                ? Option(Land, "CHOICE", HoverTipFactory.FromCard(_skillCard!))
                : LockedOption("LOCKED_SKILL", "CHOICE"));
            options.Add(_hasPower
                ? Option(Channel, "CHOICE", HoverTipFactory.FromCard(_powerCard!))
                : LockedOption("LOCKED_POWER", "CHOICE"));
            options.Add(_hasAttack
                ? Option(Strike, "CHOICE", HoverTipFactory.FromCard(_attackCard!))
                : LockedOption("LOCKED_ATTACK", "CHOICE"));
        }
        SetEventState(PageDescription("CHOICE"), options);
    }

    /// <summary>
    /// StS1 <c>CardHelper.hasCardWithType</c> + <c>returnCardOfType</c>: query the player's deck for each
    /// <see cref="CardType"/> and pick a random matching card with the event RNG.
    /// </summary>
    private void PickCards()
    {
        var deck = Owner.Deck.Cards;
        _hasSkill = deck.Any(c => c.Type == CardType.Skill);
        _hasPower = deck.Any(c => c.Type == CardType.Power);
        _hasAttack = deck.Any(c => c.Type == CardType.Attack);
        if (_hasSkill)
        {
            _skillCard = Rng.NextItem(deck.Where(c => c.Type == CardType.Skill))!;
            ((StringVar)DynamicVars["SkillCard"]).StringValue = _skillCard.Title;
        }
        if (_hasPower)
        {
            _powerCard = Rng.NextItem(deck.Where(c => c.Type == CardType.Power))!;
            ((StringVar)DynamicVars["PowerCard"]).StringValue = _powerCard.Title;
        }
        if (_hasAttack)
        {
            _attackCard = Rng.NextItem(deck.Where(c => c.Type == CardType.Attack))!;
            ((StringVar)DynamicVars["AttackCard"]).StringValue = _attackCard.Title;
        }
    }

    private async Task Land()
    {
        await CardPileCmd.RemoveFromDeck(_skillCard!);
        SetEventFinished(PageDescription("LAND"));
    }

    private async Task Channel()
    {
        await CardPileCmd.RemoveFromDeck(_powerCard!);
        SetEventFinished(PageDescription("CHANNEL"));
    }

    private async Task Strike()
    {
        await CardPileCmd.RemoveFromDeck(_attackCard!);
        SetEventFinished(PageDescription("STRIKE"));
    }

    private Task Fall()
    {
        SetEventFinished(PageDescription("FEATHER"));
        return Task.CompletedTask;
    }
}
