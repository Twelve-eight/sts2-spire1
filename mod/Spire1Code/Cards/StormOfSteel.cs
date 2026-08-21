using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Storm of Steel (Rare Skill). Discard your hand; add 1 Shiv into your hand for each card discarded (Shiv+ upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class StormOfSteel() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        int count = hand.Count;
        await CardCmd.Discard(choiceContext, hand);
        var shivs = (await Shiv.CreateInHand(Owner, count, CombatState)).ToList();
        if (IsUpgraded)
        {
            foreach (var shiv in shivs)
            {
                CardCmd.Upgrade(shiv);
            }
        }
    }
}
