using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Black Blood (Boss). At the end of combat, heal 12 HP.</summary>
public class BlackBlood : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(12m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Black Blood",
            "#At the end of combat, heal !H! HP.",
            "A viscous black ichor that refuses to burn away.");

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead)
            return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}
