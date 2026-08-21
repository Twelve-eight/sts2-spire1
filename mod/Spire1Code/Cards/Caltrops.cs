using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Caltrops (Uncommon Power). Whenever you are attacked, deal 3 damage back (5 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Caltrops() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ThornsPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<ThornsPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<ThornsPower>().UpgradeValueBy(2m);
}
