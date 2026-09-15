using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Heavy Blade (Common Attack). Deal 14 damage; Strength affects this card 3 times (5 upgraded).</summary>
public class HeavyBlade() : Spire1Card(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // Strength multiplier stored as a card DynamicVar so the calc lambda stays STATIC.
    // The lambda is handed to CalculatedDamageVar.WithMultiplier and lives inside the var
    // instance (BaseLib CustomCardModel.MakeCalculatedDamage -> FinishMakeCalculatedVar),
    // so it must not close over per-instance card state; the upgrade varies the var instead.
    // NOTE on the clone model (probe-verified 2026-09-15): MutableClone does NOT re-run
    // CanonicalVars - AbstractModel.MutableClone -> DeepCloneFields ->
    // DynamicVarSet.Clone copies the materialized VALUES (CardModel.cs:1202,
    // DynamicVarSet.cs:153-158). An earlier version of this comment claimed the clone
    // re-evaluates CanonicalVars; that was wrong. The static lambda is still correct, for
    // the reason above rather than for that one.
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
