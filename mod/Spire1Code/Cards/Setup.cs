using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Setup (Uncommon Skill). Put a card from your hand on top of your draw pile. It costs 0 until played (cost 0 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class Setup() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, null)).FirstOrDefault();
        if (picked != null)
        {
            // StS1 sets freeToPlayOnce only when the stored legacy cost is positive.
            // Check before moving the card so base-0, negative, and X cards are carried
            // without receiving a new free marker; an existing marker is preserved.
            if (LegacyRecallCost.ShouldGrantSetupFreeOnce(picked))
                picked.EnergyCost.SetUntilPlayed(0);
            await CardPileCmd.Add(picked, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
