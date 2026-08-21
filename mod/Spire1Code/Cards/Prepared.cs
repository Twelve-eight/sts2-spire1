using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Prepared (Common). Draw 1 card, discard 1 card (2 / 2 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Prepared() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);

        var count = DynamicVars.Cards.IntValue;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, count);
        var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, null)).ToList();
        foreach (var card in picked)
        {
            await CardCmd.Discard(choiceContext, card);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
