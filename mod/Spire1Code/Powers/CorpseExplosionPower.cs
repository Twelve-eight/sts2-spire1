using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent — Corpse Explosion. When the enemy dies, deal damage equal to its Max HP to ALL other enemies.</summary>
public class CorpseExplosionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Corpse Explosion",
            "#When the enemy dies, deal damage equal to its Max HP to ALL enemies.",
            "When the enemy dies, deal damage equal to its Max HP to ALL enemies.");

    // Death hook signature copied from the decompiled game powers (e.g. MagicBombPower.AfterDeath).
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner)
            return;
        Flash();
        var targets = CombatState.HittableEnemies.Where(e => e != Owner).ToList();
        if (targets.Count == 0)
            return;
        await CreatureCmd.Damage(choiceContext, targets, Owner.MaxHp, ValueProp.Unpowered, Owner, null, null);
    }
}
