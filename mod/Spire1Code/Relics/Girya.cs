using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Girya (Rare). At the start of each combat, gain !StrengthPower!.</summary>
public class Girya : Spire1Relic
{
    public int StrengthBonus { get; set; } = 0;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(0)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Girya",
            "#At the start of each combat, gain !StrengthPower!. (Lift at rest sites to increase — rest-site option not yet wired.)",
            "A heavy dumbbell etched with ancient script.");

    public override async Task BeforeCombatStart()
    {
        // FLAG: rest-site lift option not wired (passive only — StrengthBonus has no in-game way to increase yet).
        if (StrengthBonus > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, StrengthBonus, Owner.Creature, null);
        }
    }
}
