using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Council of Ghosts.
/// Accept: lose ceil(50% of Max HP) Max HP (capped at Max HP - 1) and receive 5 Apparition
/// cards (3 at Ascension 15+). Refuse: leave.
///
/// Apparition is the card StS2 already ships, reused rather than reimplemented (see Accept).
/// StS1 constants: HP_DRAIN = 0.5f.
/// </summary>
public class Ghosts : Spire1Event
{
    private const string _hpLossKey = "HpLoss";

    private const string _apparitionCountKey = "ApparitionCount";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "reflections";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar(_hpLossKey, 0), new IntVar(_apparitionCountKey, 5)];

    public override void CalculateVars()
    {
        // StS1: hpLoss = ceil(maxHealth * 0.5), capped at maxHealth - 1.
        int maxHp = (int)Owner.Creature.MaxHp;
        int loss = (int)System.Math.Ceiling(maxHp * 0.5m);
        DynamicVars[_hpLossKey].BaseValue = loss >= maxHp ? maxHp - 1 : loss;
        DynamicVars[_apparitionCountKey].BaseValue = Owner.RunState.AscensionLevel >= 15 ? 3 : 5;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Accept).ThatDecreasesMaxHp(DynamicVars[_hpLossKey].BaseValue),
            Option(Refuse),
        ];
    }

    private async Task Accept()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars[_hpLossKey].BaseValue, isFromCard: false);
        // Apparition is not reimplemented: StS2 ships an identical one
        // (MegaCrit.Sts2.Core.Models.Cards.Apparition, cost 1 Skill/Self, Ethereal + Exhaust,
        // PowerVar<IntangiblePower>(1), upgrade removes Ethereal), already registered in
        // EventCardPool, so per the lean-code rule we add copies of the shipped card.
        int count = DynamicVars[_apparitionCountKey].IntValue;
        List<CardModel> apparitions = new(count);
        for (int i = 0; i < count; i++)
        {
            apparitions.Add(Owner.RunState.CreateCard<Apparition>(Owner));
        }
        await CardPileCmd.Add(apparitions, PileType.Deck);
        SetEventFinished(PageDescription("ACCEPT"));
    }

    private Task Refuse()
    {
        SetEventFinished(PageDescription("EXIT"));
        return Task.CompletedTask;
    }
}
