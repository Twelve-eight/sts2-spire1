using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Burning Pact (Uncommon). Exhaust 1 card. Draw 2 cards (3 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class BurningPact() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, null)).FirstOrDefault();
        if (picked != null)
            await CardCmd.Exhaust(choiceContext, picked);
        await CommonActions.Draw(this, choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
