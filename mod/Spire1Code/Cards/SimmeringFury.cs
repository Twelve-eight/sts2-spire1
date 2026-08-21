using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Simmering Fury (Uncommon Skill, StS1 id "Vengeance"). At the start of your next turn, enter Wrath
/// and draw 2 cards (3 upgraded).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class SimmeringFury() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SimmeringFuryPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<SimmeringFuryPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<SimmeringFuryPower>().UpgradeValueBy(1m);
}
