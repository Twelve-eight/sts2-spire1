using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class RipAndTear() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            if (enemy != null)
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(enemy).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
