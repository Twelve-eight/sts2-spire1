using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Powers;

using BaseLib.Utils;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher token - Omega. At the end of your turn, deal 50 damage to ALL enemies (60 upgraded). Power.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Omega() : Spire1Card(3, CardType.Power, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<OmegaPower>(1), new DamageVar(50, ValueProp.Unpowered)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var power = await CommonActions.ApplySelf<OmegaPower>(choiceContext, this);
        power?.SetDamage(DynamicVars.Damage.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10m);
}
