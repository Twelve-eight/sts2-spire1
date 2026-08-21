using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Collect (Uncommon Skill, X-cost, Exhaust). At the start of each of your next X turns, put an
/// upgraded Miracle into your hand (X+1 turns when upgraded).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Collect() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // X = energy spent this play (ResolveEnergyXValue includes X-value modifiers like Chemical X).
        int turns = ResolveEnergyXValue();
        if (IsUpgraded)
        {
            turns++;
        }

        if (turns <= 0)
        {
            return;
        }

        await PowerCmd.Apply<CollectPower>(choiceContext, Owner.Creature, turns, Owner.Creature, this);
    }
}
