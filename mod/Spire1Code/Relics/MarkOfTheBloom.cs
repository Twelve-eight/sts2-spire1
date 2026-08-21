using BaseLib.Abstracts;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Mark of the Bloom (event relic, from Mind Bloom's "Awake" branch). You can no longer heal.
///
/// StS1 (relics.json "MarkOfTheBloom"): onPlayerHeal(int) flashes and returns 0 unconditionally, so every heal routed
/// through AbstractPlayer.heal — combat heals, campfire Rest, event heals, potions, Burning Blood — is reduced to
/// zero for the rest of the run. Max-HP gains are untouched; only the heal amount is zeroed.
///
/// StS2 port: core has no general heal hook. CreatureCmd.Heal (CreatureCmd.cs:738-750) computes the amount and calls
/// Creature.HealInternal directly (Creature.cs:478-487) without a modify hook, and AbstractModel's only heal surface
/// is ModifyRestSiteHealAmount (AbstractModel.cs:1872), which covers Rest Sites alone. So this uses BaseLib's
/// IHealAmountModifier (research/BaseLib-StS2/Hooks/IHealAmountModifier.cs), which
/// research/BaseLib-StS2/Patches/Hooks/ModifyHealAmountPatches.cs transpiles into CreatureCmd.Heal itself — i.e.
/// into the single choke point every heal in the game passes through, exactly like StS1's onPlayerHeal. Max HP is a
/// different path (Creature.SetMaxHpInternal, Creature.cs:494), so it stays untouched as in StS1.
///
/// The interface has two members and only one of them is right here. ModifyHealAdditive's doc says "return the amount
/// to add", and ModifyHealAmountPatches.cs:60-70 runs every additive contribution first; the multiplicative pass then
/// multiplies that sum (ModifyHealAmountPatches.cs:72-83) and clamps at Math.Max(0m, ...). So the correct member is
/// ModifyHealMultiplicative returning 0m: it is applied after the additive pass, which means it also zeroes additive
/// heal bonuses from other sources, and being a product it wins over any other multiplier (e.g. this mod's
/// MagicFlower 1.5x). ModifyHealAdditive is deliberately left at its default 0m — a negative addend there would only
/// subtract a fixed amount and could not express "no longer heal".
/// </summary>
public class MarkOfTheBloom : Spire1Relic, IHealAmountModifier
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Mark of the Bloom",
            "#You can no longer heal.",
            "In the Beyond, thoughts and reality are one.");

    public decimal ModifyHealMultiplicative(Creature creature, decimal amount)
    {
        if (creature != Owner.Creature)
            return 1m;
        // CreatureCmd.Heal does not early-return on a zero amount, so only flash when a real heal was cancelled.
        if (amount > 0m)
            Flash();
        return 0m;
    }
}
