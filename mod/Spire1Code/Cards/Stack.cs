using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Stack() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CalculationBaseVar(0), new CalculationExtraVar(1),
         new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (card, target) =>
             PileType.Discard.GetPile(card.Owner).Cards.Count())];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardBlock(this, DynamicVars.CalculatedBlock, play);

    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(3m);
}
