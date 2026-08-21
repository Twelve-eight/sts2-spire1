using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Like Water (Uncommon Power). At the end of your turn, if you are in Calm, gain 5 Block (7 upgraded).</summary>
[Pool(typeof(WatcherCardPool))]
public class LikeWater() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<LikeWaterPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<LikeWaterPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<LikeWaterPower>().UpgradeValueBy(2m);
}
