using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Heatsinks() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<HeatsinksPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        await CommonActions.ApplySelf<HeatsinksPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Power<HeatsinksPower>().UpgradeValueBy(1);
}
