using BaseLib.Abstracts;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 — Magic Flower (Rare). Healing is 50% more effective.</summary>
public class MagicFlower : Spire1Relic, IHealAmountModifier
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Magic Flower",
            "#Healing is 50% more effective.",
            "It never wilts.");

    public decimal ModifyHealMultiplicative(Creature creature, decimal amount)
        => creature == Owner.Creature ? 1.5m : 1m;
}
