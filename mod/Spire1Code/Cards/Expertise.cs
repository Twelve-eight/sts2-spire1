using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Expertise (Uncommon Skill). Draw cards until you have 6 in your hand (7 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class Expertise() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int want = DynamicVars.Cards.IntValue;
        int inHand = PileType.Hand.GetPile(Owner).Cards.Count;
        if (inHand < want)
        {
            await CardPileCmd.Draw(choiceContext, want - inHand, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
