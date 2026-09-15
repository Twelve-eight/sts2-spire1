using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Rampage (Uncommon Attack). Deal 8 damage; increase this card's damage by 5 each play this combat (8 upgraded).</summary>
public class Rampage() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // Accumulated bonus + per-play increment stored as card DynamicVars so the calc lambda
    // stays STATIC: the lambda lives inside the CalculatedDamageVar instance
    // (BaseLib CustomCardModel.MakeCalculatedDamage), so it must not capture card fields.
    // NOTE on the clone model (probe-verified 2026-09-15): MutableClone does NOT re-run
    // CanonicalVars - AbstractModel.MutableClone -> DeepCloneFields -> DynamicVarSet.Clone
    // copies the materialized VALUES (CardModel.cs:1202, DynamicVarSet.cs:153-158), which is
    // why the per-combat RampBonus accumulation is carried across a clone rather than reset.
    // An earlier version of this comment claimed the clone re-evaluates CanonicalVars safely;
    // that was wrong.
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
