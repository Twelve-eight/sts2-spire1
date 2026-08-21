using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Spirit Poop (Event). The joke consolation prize from shrine/offering event branches.
/// It has no gameplay effect, and that is vanilla-correct, not an omission: the StS1 class overrides
/// only getUpdatedDescription() (returns DESCRIPTIONS[0]) and makeCopy(). It declares no constants, no
/// counter, and no AbstractRelic gameplay hook whatsoever, which the bytecode confirms.
/// Adding an effect here would be inventing content that does not exist in the original game.
/// </summary>
public class SpiritPoop : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Spirit Poop",
            "#It's unpleasant.",
            "The charred remains of your offering to the spirits.");
}
