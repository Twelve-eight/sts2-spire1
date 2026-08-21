using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — A Thousand Cuts (Rare Power). Whenever you play a card, deal 1 damage to ALL enemies (2 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class AThousandCuts() : Spire1Card(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<AThousandCutsPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<AThousandCutsPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<AThousandCutsPower>().UpgradeValueBy(1m);
}
