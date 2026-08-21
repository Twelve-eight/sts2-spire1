using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Accuracy (Uncommon Power). Shivs deal 4 additional damage (6 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Accuracy() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<AccuracyPower>(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<AccuracyPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<AccuracyPower>().UpgradeValueBy(2m);
}
