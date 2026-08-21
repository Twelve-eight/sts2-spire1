using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Nirvana (Uncommon Power). Whenever you Scry, gain 3 Block (4 upgraded).</summary>
[Pool(typeof(WatcherCardPool))]
public class Nirvana() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<NirvanaPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<NirvanaPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<NirvanaPower>().UpgradeValueBy(1m);
}
