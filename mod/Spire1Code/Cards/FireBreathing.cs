using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Fire Breathing (Uncommon Power). Whenever you draw a Status or Curse card, deal 6 damage to ALL enemies (10 upgraded).</summary>
public class FireBreathing() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FireBreathingPower>(6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<FireBreathingPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<FireBreathingPower>().UpgradeValueBy(4m);
}
