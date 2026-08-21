using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Doppelganger (Rare Skill, X-cost). Next turn, draw X cards and gain X Energy (X+1 upgraded). Exhaust.</summary>
[Pool(typeof(SilentCardPool))]
public class Doppelganger() : Spire1Card(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        if (IsUpgraded)
        {
            x += 1;
        }
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, x, Owner.Creature, this);
    }
}
