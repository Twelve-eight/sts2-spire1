using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Noxious Fumes (Uncommon Power). At the start of your turn, apply 2 Poison to ALL enemies (3 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class NoxiousFumes() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<NoxiousFumesPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<NoxiousFumesPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<NoxiousFumesPower>().UpgradeValueBy(1m);
}
