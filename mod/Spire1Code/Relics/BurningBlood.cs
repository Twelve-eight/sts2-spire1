using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad - Burning Blood (Starter). At the end of combat, heal 6 HP.</summary>
public class BurningBlood : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(6m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Burning Blood",
            "#At the end of combat, heal !H! HP.",
            "Your body's own blood burns with an undying rage.");

    /// <summary>
    /// Touch of Orobas (and the relic-collection "upgraded starter" row) resolves the upgrade
    /// through BaseLib's sanctioned hook: <c>StarterUpgradePatches</c> prefixes
    /// <c>TouchOfOrobas.GetUpgradedStarterRelic</c> and returns <c>GetUpgradeReplacement()</c>
    /// whenever it is non-null (BaseLib 3.4.5, shipped). Without this override the engine's
    /// hardcoded <c>RefinementUpgrades</c> dictionary - keyed on the BASE-GAME BurningBlood id -
    /// misses our SPIRE1-* id and falls back to the placeholder <c>Circlet</c> ("头环").
    /// </summary>
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<BlackBlood>();

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead)
            return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}
