using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using Spire1.Spire1Code.Character;
using System.Linq;

using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Storm of Steel (Rare Skill). Discard your hand; add 1 Shiv into your hand for each card discarded (Shiv+ upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class StormOfSteel() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Cards content group gate (C18 r2, 2026-10-03): Shiv is granted directly by this Spire1 card, so the card pool filters cannot close the path; the gate also runs before the hand is discarded.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Storm of Steel Shiv grant skipped: cards content group is off");
            return;
        }

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
