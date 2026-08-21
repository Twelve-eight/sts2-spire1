using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Bouncing Flask (Uncommon Skill). Apply 3 Poison to a random enemy 3 times (4 times upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class BouncingFlask() : Spire1Card(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(3), new RepeatVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies == null)
        {
            return;
        }
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            // Each bounce picks a random hittable enemy (game BouncingFlask idiom).
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            if (enemy == null)
            {
                continue;
            }
            await CommonActions.Apply<PoisonPower>(choiceContext, enemy, this, DynamicVars.Power<PoisonPower>().IntValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}
