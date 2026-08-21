using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Well-Laid Plans (Uncommon Power). At the end of your turn, Retain up to 1 card (2 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class WellLaidPlans() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WellLaidPlansPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<WellLaidPlansPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<WellLaidPlansPower>().UpgradeValueBy(1m);
}
