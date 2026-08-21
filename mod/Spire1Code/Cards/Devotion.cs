using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Devotion (Rare Power). At the start of your turn, gain 2 Mantra (3 upgraded).</summary>
[Pool(typeof(WatcherCardPool))]
public class Devotion() : Spire1Card(1, CardType.Power, CardRarity.Rare, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DevotionPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<DevotionPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<DevotionPower>().UpgradeValueBy(1m);
}
