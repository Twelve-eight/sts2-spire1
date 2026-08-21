using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Dual Wield (Uncommon Skill). Choose an Attack or Power card; add a copy of it to your hand (2 copies upgraded).</summary>
public class DualWield() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    private int _copies = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, c => c.Type == CardType.Attack || c.Type == CardType.Power)).FirstOrDefault();
        if (picked != null)
        {
            for (int i = 0; i < _copies; i++)
                await CardPileCmd.AddGeneratedCardToCombat(picked.CreateClone(), PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade() => _copies = 2;
}
