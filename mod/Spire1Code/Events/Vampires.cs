using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Vampires(?).
/// Accept: lose ceil(30% of Max HP) Max HP (capped at Max HP - 1), remove all Strikes and
/// receive 5 Bites. If you own the Blood Vial, you may offer it instead (no HP loss).
///
/// StS1 grants NUM_BITES = 5 Bites (`iconst_5` immediately preceding `new Bite` in every branch
/// of the jar bytecode); Bite is a mod card (SPIRE1-BITE).
/// StS1 constants: HP_DRAIN = 0.3f; the intro text depends on the character (brother / sister /
/// broken one).
/// </summary>
public class Vampires : Spire1Event
{
    /// <summary>StS1 Vampires grants exactly 5 Bites in every branch.</summary>
    private const int _numBites = 5;

    private const string _maxHpLossKey = "MaxHpLoss";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "spirit_grafter";

    public override LocString InitialDescription => L10NLookup($"{Id.Entry}.pages.{InitialPageKey}.description");

    private string InitialPageKey => Owner?.Character switch
    {
        Character.Ironclad => "INITIAL",      // "brother" (StS1 Ironclad)
        Character.Silent or Character.Watcher => "INITIAL_F", // "sister" (StS1 Silent / Watcher)
        _ => "INITIAL_N",                     // "broken one" (StS1 Defect / others)
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(_maxHpLossKey, 0)];

    public override void CalculateVars()
    {
        // StS1: maxHpLoss = ceil(maxHealth * 0.3), capped at maxHealth - 1.
        int maxHp = (int)Owner.Creature.MaxHp;
        int loss = (int)System.Math.Ceiling(maxHp * 0.3m);
        DynamicVars[_maxHpLossKey].BaseValue = loss >= maxHp ? maxHp - 1 : loss;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        List<EventOption> options =
        [
            Option(Accept).ThatDecreasesMaxHp(DynamicVars[_maxHpLossKey].BaseValue),
        ];
        if (Owner.Relics.Any(r => r is BloodVial))
        {
            options.Add(Option(LoseVial));
        }
        options.Add(Option(Refuse));
        return options;
    }

    private async Task Accept()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars[_maxHpLossKey].BaseValue, isFromCard: false);
        await RemoveAllStrikes();
        await GrantBites();
        SetEventFinished(PageDescription("ACCEPT"));
    }

    private async Task LoseVial()
    {
        RelicModel? vial = Owner.Relics.FirstOrDefault(r => r is BloodVial);
        if (vial != null)
        {
            await RelicCmd.Remove(vial);
        }
        await RemoveAllStrikes();
        await GrantBites();
        SetEventFinished(PageDescription("VIAL"));
    }

    private Task Refuse()
    {
        SetEventFinished(PageDescription("EXIT"));
        return Task.CompletedTask;
    }

    private async Task RemoveAllStrikes()
    {
        // StS1: removes every card carrying the STARTER_STRIKE tag.
        foreach (CardModel card in PileType.Deck.GetPile(Owner).Cards.Where(c => c.Tags.Contains(CardTag.Strike)).ToList())
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
    }

    private async Task GrantBites()
    {
        List<CardModel> bites = new(_numBites);
        for (int i = 0; i < _numBites; i++)
        {
            bites.Add(Owner.RunState.CreateCard<Bite>(Owner));
        }
        await CardPileCmd.Add(bites, PileType.Deck);
    }
}
