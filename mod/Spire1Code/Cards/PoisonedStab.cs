using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Poisoned Stab (Common). Deal 6 damage, apply 3 Poison (8 / 4 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class PoisonedStab() : Spire1Card(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6, ValueProp.Move), new PowerVar<PoisonPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<PoisonPower>(choiceContext, play.Target!, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        // StS1: upgradeMagicNumber(1) -> poison 3 -> 4 (NOT +2). Verified against the StS1
        // bytecode: PoisonedStab.<init> sets baseDamage 6 / baseMagicNumber 3, and upgrade()
        // calls upgradeDamage(2) then upgradeMagicNumber(1). This mod had +2, which produced
        // Poisoned Stab+ = 8 dmg / 5 poison instead of the correct 8 / 4.
        DynamicVars.Power<PoisonPower>().UpgradeValueBy(1m);
    }
}
