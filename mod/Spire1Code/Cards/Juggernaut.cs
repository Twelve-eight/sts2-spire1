using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Juggernaut (Rare Power). Whenever you gain Block, deal 5 damage to a random enemy (7 upgraded).</summary>
public class Juggernaut() : Spire1Card(2, CardType.Power, CardRarity.Rare, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<JuggernautPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<JuggernautPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<JuggernautPower>().UpgradeValueBy(2m);
}
