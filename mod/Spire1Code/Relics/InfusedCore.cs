using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Defect - Infused Core (Starter upgrade). At the start of combat, Channel 3 Lightning;
/// Lightning deals 1 additional damage.
/// ID = SPIRE1-INFUSED_CORE. Sits in the Defect relic pool (overrides the base Spire1Relic pool).
/// Reached only through Touch of Orobas upgrading <see cref="CrackedCore"/>; logic mirrors the
/// native StS2 InfusedCore exactly.
/// </summary>
[Pool(typeof(DefectRelicPool))]
public class InfusedCore : Spire1Relic
{
    /// <summary>RelicsEnabled gate (astra-advice SP1 findings): this relic is
    /// registered into an ENGINE character relic pool, so base-game runs could
    /// roll it. Starter-grant flow (direct grant) bypasses IsAllowed and is
    /// unaffected.</summary>
    public override bool IsAllowed(global::MegaCrit.Sts2.Core.Runs.IRunState runState)
        => base.IsAllowed(runState) && Spire1Config.RelicsEnabled;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Lightning", 3m), new DynamicVar("ExtraDamage", 1m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Infused Core",
            "#At the start of combat, Channel 3 Lightning. Lightning deals 1 additional damage.",
            "The Defect's cracked core, humming with stored lightning.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature) && Owner.PlayerCombatState.TurnNumber <= 1)
        {
            for (int i = 0; (decimal)i < DynamicVars["Lightning"].BaseValue; i++)
            {
                await OrbCmd.Channel<LightningOrb>(new BlockingPlayerChoiceContext(), Owner);
            }
        }
    }

    public override decimal ModifyOrbValue(OrbModel orb, decimal value)
    {
        if (orb.Owner != Owner)
            return value;
        if (orb is not LightningOrb)
            return value;
        return value + DynamicVars["ExtraDamage"].BaseValue;
    }
}
