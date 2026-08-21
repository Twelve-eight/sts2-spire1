using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Ragnarok (Rare Attack). Deal 5 damage (6 upgraded) to a random enemy 5 times (6 upgraded).
/// Each hit re-rolls the target from the currently hittable enemies, like the mod's Rip and Tear.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Ragnarok() : Spire1Card(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move), new RepeatVar(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int hits = DynamicVars.Repeat.IntValue;
        for (int i = 0; i < hits; i++)
        {
            var enemies = CombatState?.HittableEnemies;
            if (enemies == null || enemies.Count == 0)
                return;
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
            if (enemy == null)
                return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(enemy).Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}
