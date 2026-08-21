using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Watcher — Pure Water (Starter). At the start of each combat, gain [E] [E].
/// ID = SPIRE1-PURE_WATER. Sits in the Watcher relic pool (overrides the base Spire1Relic pool).
/// The start-of-combat energy effect IS supported: it reuses the exact hook proven by Lantern
/// (AfterSideTurnStart on turn 1 + PlayerCmd.GainEnergy), so no invented effect was needed.
/// </summary>
[Pool(typeof(WatcherRelicPool))]
public class PureWater : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Pure Water",
            "#At the start of each combat, gain [E] [E].",
            "The purest water, blessed by the divine.");

    // Same hook as Lantern: grant energy on the first turn of each combat.
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState.TurnNumber > 1)
            return;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}
