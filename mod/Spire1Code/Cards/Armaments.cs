using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Armaments (Common). Gain 5 Block; upgrade a card in your hand (all cards upgraded).</summary>
public class Armaments() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.None)
{
    private bool _all;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        if (_all)
        {
            foreach (var c in PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList())
                CardCmd.Upgrade(c);
        }
        else
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
            var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, c => c.IsUpgradable)).FirstOrDefault();
            if (picked != null)
                CardCmd.Upgrade(picked);
        }
    }

    protected override void OnUpgrade() => _all = true;
}
