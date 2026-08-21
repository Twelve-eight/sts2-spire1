using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Recycle() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, null)).FirstOrDefault();
        if (picked == null)
            return;
        int energy = picked.EnergyCost.CostsX ? 0 : picked.EnergyCost.GetAmountToSpend();
        await CardCmd.Exhaust(choiceContext, picked);
        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
