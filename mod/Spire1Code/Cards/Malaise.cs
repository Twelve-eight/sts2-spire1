using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Malaise (Rare Skill, X-cost). Enemy loses X Strength and gains X Weak (X+1 upgraded). Exhaust.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Malaise() : Spire1Card(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        int x = ResolveEnergyXValue();
        if (IsUpgraded)
        {
            x += 1;
        }
        await PowerCmd.Apply<StrengthPower>(choiceContext, play.Target!, -x, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, play.Target!, x, Owner.Creature, this);
    }
}
