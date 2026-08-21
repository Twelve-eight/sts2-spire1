using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(WatcherCardPool))]
public class PressurePoints() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MarkPower>(8)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<MarkPower>(choiceContext, play.Target!, this);
        foreach (var enemy in CombatState.HittableEnemies)
        {
            var mark = enemy.GetPower<MarkPower>();
            if (mark != null && mark.Amount > 0)
            {
                await CreatureCmd.Damage(choiceContext, enemy, mark.Amount,
                    ValueProp.Unblockable | ValueProp.Unpowered, this, play);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Power<MarkPower>().UpgradeValueBy(3m);
}
