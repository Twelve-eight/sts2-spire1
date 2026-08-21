using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Claw() : Spire1Card(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private decimal _extraDamageFromClawPlays;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3, ValueProp.Move), new IntVar("Increase", 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        decimal increase = DynamicVars["Increase"].BaseValue;
        foreach (Claw claw in Owner.PlayerCombatState.AllCards.OfType<Claw>())
        {
            claw.DynamicVars.Damage.BaseValue += increase;
            claw._extraDamageFromClawPlays += increase;
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += _extraDamageFromClawPlays;
    }
}
