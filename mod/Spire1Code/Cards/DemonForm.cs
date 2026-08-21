using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Demon Form (Rare Power). At the start of your turn, gain 2 Strength (3 upgraded).</summary>
public class DemonForm() : Spire1Card(3, CardType.Power, CardRarity.Rare, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DemonFormPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<DemonFormPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<DemonFormPower>().UpgradeValueBy(1m);
}
