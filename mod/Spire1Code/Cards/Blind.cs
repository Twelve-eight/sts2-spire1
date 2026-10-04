using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless - Blind (Common Skill). Apply 2 Weak to the enemy (upgraded: to ALL enemies). 0 cost.
/// Upgrade changes target type (AnyEnemy -&gt; AllEnemies) via the TargetType override; CommonActions.Apply with the
/// card+play overload routes single-target (play.Target) vs all (card.GetTargets() -&gt; HittableEnemies).
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public class Blind() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override TargetType TargetType => IsUpgraded ? TargetType.AllEnemies : base.TargetType;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        await CommonActions.Apply<WeakPower>(choiceContext, this, play);
    }

    protected override void OnUpgrade()
    {
        // Upgrade effect is the target-type change to AllEnemies (handled by the TargetType override above).
    }
}
