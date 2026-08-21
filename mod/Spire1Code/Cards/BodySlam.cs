using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Body Slam (Common Attack). Deal damage equal to your Block.</summary>
public class BodySlam() : Spire1Card(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
        => [..CustomCardModel.MakeCalculatedDamage(0, (card, target) => card.Owner.Creature.Block)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
