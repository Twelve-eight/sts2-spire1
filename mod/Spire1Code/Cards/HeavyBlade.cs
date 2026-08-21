using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Heavy Blade (Common Attack). Deal 14 damage; Strength affects this card 3 times (5 upgraded).</summary>
public class HeavyBlade() : Spire1Card(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // Strength multiplier stored as a card DynamicVar so the calc lambda stays STATIC
    // (MakeCalculatedDamage rejects lambdas that capture instance fields; upgrade varies the var, not a field).
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("StrMult", 3),
        ..CustomCardModel.MakeCalculatedDamage(14,
            static (card, target) => card.Owner.Creature.GetPowerAmount<StrengthPower>() * card.DynamicVars["StrMult"].IntValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    protected override void OnUpgrade() => DynamicVars["StrMult"].UpgradeValueBy(2); // 3 -> 5
}
