using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Inflame (Uncommon Power). Gain 2 Strength (3 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Inflame() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<StrengthPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<StrengthPower>().UpgradeValueBy(1m);
}
