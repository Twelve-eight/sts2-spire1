using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Mental Fortress (Uncommon Power). Whenever you change Stances, gain 4 Block (6 upgraded).</summary>
[Pool(typeof(WatcherCardPool))]
public class MentalFortress() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MentalFortressPower>(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<MentalFortressPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<MentalFortressPower>().UpgradeValueBy(2m);
}
