using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Silent — Catalyst (Uncommon Skill). Double the enemy's Poison (Triple upgraded). Exhaust.
/// The description text swaps Double/Triple through the SimpleLoc upgrade-swap syntax.
/// </summary>
[Pool(typeof(SilentCardPool))]
public class Catalyst() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target!;
        int current = target.GetPowerAmount<PoisonPower>();
        if (current <= 0)
        {
            return;
        }
        // Base: apply 1x more so the total doubles. Upgraded: apply 2x more so the total triples.
        int add = IsUpgraded ? current * 2 : current;
        await CommonActions.Apply<PoisonPower>(choiceContext, target, this, add);
    }
}
