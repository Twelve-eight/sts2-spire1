using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Mark of Pain (Boss). At the start of each combat, gain !E! Energy.</summary>
public class MarkOfPain : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Mark of Pain",
            "#At the start of each combat, gain !E! *Energy*. (Vanilla also adds 2 Wounds to your deck on pickup — not wired.)",
            "Pain is the only teacher.");

    public override async Task BeforeCombatStart()
    {
        // FLAG: deck-add-on-pickup (2 Wounds) not wired.
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
    }
}
