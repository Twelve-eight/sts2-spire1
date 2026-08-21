using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Glass Knife (Rare Attack). Deal 8 damage twice (12 upgraded); decrease this card's damage by 2 this combat each time it is played.</summary>
[Pool(typeof(SilentCardPool))]
public class GlassKnife() : Spire1Card(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // Per-combat penalty stored as card DynamicVars so the calc lambda stays STATIC (same approach as Rampage).
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("GKInc", 2),
        new IntVar("GKBonus", 0),
        ..CustomCardModel.MakeCalculatedDamage(8,
            static (card, target) => -card.DynamicVars["GKBonus"].BaseValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: 2).Execute(choiceContext);
        DynamicVars["GKBonus"].BaseValue += DynamicVars["GKInc"].BaseValue; // -2 damage per play this combat
    }

    protected override void OnUpgrade() => DynamicVars["CalculationBase"].UpgradeValueBy(4m); // 8 -> 12
}
