using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 Silent - Ring of the Serpent (Starter upgrade). At the start of the first 3 turns of
/// each combat, draw 2 additional cards.
/// ID = SPIRE1-RING_OF_THE_DRAKE. Sits in the Silent relic pool (overrides the base Spire1Relic pool).
/// Reached only through Touch of Orobas upgrading <see cref="RingOfTheSnake"/>; logic mirrors the
/// native StS2 RingOfTheDrake exactly.
/// </summary>
[Pool(typeof(SilentRelicPool))]
public class RingOfTheDrake : Spire1Relic
{
    /// <summary>RelicsEnabled gate (astra-advice SP1 findings): this relic is
    /// registered into an ENGINE character relic pool, so base-game runs could
    /// roll it. Starter-grant flow (direct grant) bypasses IsAllowed and is
    /// unaffected.</summary>
    public override bool IsAllowed(global::MegaCrit.Sts2.Core.Runs.IRunState runState)
        => base.IsAllowed(runState) && Spire1Config.RelicsEnabled;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2), new DynamicVar("Turns", 3m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Ring of the Drake",
            "#At the start of your first 3 turns, draw 2 additional cards.",
            "Your ring has morphed and changed forms.");

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner)
            return count;
        if (Owner.PlayerCombatState.TurnNumber > DynamicVars["Turns"].BaseValue)
            return count;
        return count + DynamicVars.Cards.BaseValue;
    }
}
