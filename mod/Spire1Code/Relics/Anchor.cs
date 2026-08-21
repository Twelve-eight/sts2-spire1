using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Anchor (Common). Start each combat with 10 Block.</summary>
public class Anchor : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10m, ValueProp.Unpowered)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Anchor",
            "#Start each combat with !B! Block.",
            "Holding this miniature trinket, you feel heavier and more stable.");

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }
}
