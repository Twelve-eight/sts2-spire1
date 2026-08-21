using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Flex (Common Skill). Gain 2 Strength; at the end of your turn, lose 2 Strength (4 upgraded).</summary>
public class Flex() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FlexPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<FlexPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<FlexPower>().UpgradeValueBy(2m);
}
