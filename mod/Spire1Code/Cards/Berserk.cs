using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Berserk (Rare Power). Gain 2 Vulnerable. At the start of your turn, gain 1 Energy (1 Vulnerable upgraded).</summary>
public class Berserk() : Spire1Card(0, CardType.Power, CardRarity.Rare, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(2), new PowerVar<BerserkPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<VulnerablePower>(choiceContext, this);
        await CommonActions.ApplySelf<BerserkPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Power<VulnerablePower>().UpgradeValueBy(-1m);
}
