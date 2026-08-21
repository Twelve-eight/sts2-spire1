using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Feel No Pain (Uncommon Power). Whenever a card is Exhausted, gain 3 Block (4 upgraded).</summary>
public class FeelNoPain() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FeelNoPainPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<FeelNoPainPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<FeelNoPainPower>().UpgradeValueBy(1m);
}
