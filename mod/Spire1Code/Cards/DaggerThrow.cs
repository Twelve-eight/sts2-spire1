using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Dagger Throw (Common). Deal 9 damage, draw 1 card, discard 1 card (12 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class DaggerThrow() : Spire1Card(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Draw(this, choiceContext);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1);
        var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, null)).FirstOrDefault();
        if (picked != null)
        {
            await CardCmd.Discard(choiceContext, picked);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
