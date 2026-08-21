using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Master Reality (Rare Power, cost 1 / 0 upgraded). Whenever a card is created during combat,
/// Upgrade it.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class MasterReality() : Spire1Card(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<MasterRealityPower>(choiceContext, this, 1m);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
