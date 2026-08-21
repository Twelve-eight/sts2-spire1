using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Study (Uncommon Power). At the end of your turn, shuffle an Insight into your draw pile.
/// The upgrade only lowers the cost from 2 to 1.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Study() : Spire1Card(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StudyPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<StudyPower>(choiceContext, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
