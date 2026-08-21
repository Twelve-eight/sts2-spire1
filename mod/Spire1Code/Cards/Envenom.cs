using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Envenom (Rare Power). Whenever an Attack deals unblocked damage, apply 1 Poison (1 cost upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Envenom() : Spire1Card(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<EnvenomPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<EnvenomPower>(choiceContext, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
