using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Rampage (Uncommon Attack). Deal 8 damage; increase this card's damage by 5 each play this combat (8 upgraded).</summary>
public class Rampage() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // Accumulated bonus + per-play increment stored as card DynamicVars so the calc lambda stays STATIC
    // (no instance-field capture; MutableClone during reward generation re-evaluates CanonicalVars safely).
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("RampInc", 5),
        new IntVar("RampBonus", 0),
        ..CustomCardModel.MakeCalculatedDamage(8,
            static (card, target) => card.DynamicVars["RampBonus"].BaseValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        DynamicVars["RampBonus"].BaseValue += DynamicVars["RampInc"].BaseValue; // grows this combat
    }

    protected override void OnUpgrade() => DynamicVars["RampInc"].UpgradeValueBy(3); // 5 -> 8
}
