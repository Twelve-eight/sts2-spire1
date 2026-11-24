using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Streamline() : Spire1Card(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15, ValueProp.Move), new IntVar("CostReduction", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var mutation = Spire1EchoReplay.CaptureMutation(this);
        int reduction = DynamicVars["CostReduction"].IntValue;
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        foreach (var card in mutation.Members(this))
            card.EnergyCost.AddThisCombat(-reduction, reduceOnly: true);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}
