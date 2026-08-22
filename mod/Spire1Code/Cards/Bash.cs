using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Bash (Basic). Deal 8 damage, apply 2 Vulnerable (10 / 3 upgraded).</summary>
[Pool(typeof(Spire1CardPool))]
public class Bash() : Spire1Card(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8, ValueProp.Move), new PowerVar<VulnerablePower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<VulnerablePower>(choiceContext, play.Target!, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Power<VulnerablePower>().UpgradeValueBy(1m);
    }
}
