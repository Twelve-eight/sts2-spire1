using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Whirlwind (Uncommon Attack, X-cost). Deal 5 damage to ALL enemies X times (8 upgraded).</summary>
public class Whirlwind() : Spire1Card(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // X = energy spent this play (ResolveEnergyXValue includes X-value modifiers like Chemical X).
        int x = ResolveEnergyXValue();
        await CommonActions.CardAttack(this, play, hitCount: x).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
