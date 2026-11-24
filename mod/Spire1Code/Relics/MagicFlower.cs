using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - Magic Flower (Rare). Healing is 50% more effective during combat.
///
/// StS1 (relics "MagicFlower"): onPlayerHeal multiplies the healed amount by 1.5 only while the current room's
/// phase is COMBAT, including victory callbacks but excluding campfire Rest and event heals.
/// StS2 clears IsInProgress before victory callbacks, so their exact dispatch has a scoped bridge.
/// </summary>
public class MagicFlower : Spire1Relic, IHealAmountModifier
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public decimal ModifyHealAdditive(Creature creature, decimal amount)
    {
        if (creature != Owner.Creature || amount <= 0m
            || (!CombatManager.Instance.IsInProgress && !MagicFlowerVictoryScopePatch.Includes(Owner)))
            return 0m;

        // StS1 rounds each positive heal after its 50% bonus. Express the exact integer
        // delta before BaseLib's multiplier pass so MarkOfTheBloom can still zero it.
        return decimal.Floor(amount * 1.5m + 0.5m) - amount;
    }
}
