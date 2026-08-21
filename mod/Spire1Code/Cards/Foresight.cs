using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Foresight (Uncommon Power, StS1 id Wireheading). At the start of your turn, Scry 3 (4 upgraded).</summary>
[Pool(typeof(WatcherCardPool))]
public class Foresight() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ForesightPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<ForesightPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<ForesightPower>().UpgradeValueBy(1m);
}
