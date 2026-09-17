using BaseLib.Abstracts;
using Spire1.Spire1Code.Config;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Defect - Cracked Core (Starter). At the start of combat, Channel 1 Lightning.
/// ID = SPIRE1-CRACKED_CORE. Sits in the Defect relic pool (overrides the base Spire1Relic pool).
/// Logic mirrors the native StS2 CrackedCore exactly.
/// </summary>
[Pool(typeof(DefectRelicPool))]
public class CrackedCore : Spire1Relic
{

    /// <summary>RelicsEnabled gate (astra-advice SP1 findings): this relic is
    /// registered into an ENGINE character relic pool, so base-game runs could
    /// roll it. Starter-grant flow (direct grant) bypasses IsAllowed and is
    /// unaffected.</summary>
    public override bool IsAllowed(global::MegaCrit.Sts2.Core.Runs.IRunState runState)
        => base.IsAllowed(runState) && Spire1Config.RelicsEnabled;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Lightning", 1m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Cracked Core",
            "#At the start of combat, Channel 1 Lightning.",
            "The remains of the Defect's heart.");

    /// <summary>
    /// Touch of Orobas (and the relic-collection "upgraded starter" row) resolves the upgrade
    /// through BaseLib's sanctioned hook: <c>StarterUpgradePatches</c> prefixes
    /// <c>TouchOfOrobas.GetUpgradedStarterRelic</c> and returns <c>GetUpgradeReplacement()</c>
    /// whenever it is non-null (BaseLib 3.4.5, shipped). Without this override the engine's
    /// hardcoded <c>RefinementUpgrades</c> dictionary - keyed on the BASE-GAME CrackedCore id -
    /// misses our SPIRE1-* id and falls back to the placeholder <c>Circlet</c> ("头环").
    /// </summary>
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<InfusedCore>();

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature) && Owner.PlayerCombatState.TurnNumber <= 1)
        {
            for (int i = 0; (decimal)i < DynamicVars["Lightning"].BaseValue; i++)
            {
                await OrbCmd.Channel<LightningOrb>(new BlockingPlayerChoiceContext(), Owner);
            }
        }
    }
}
