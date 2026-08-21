using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Runic Cube (Boss). Whenever you lose HP, draw 1 card.</summary>
public class RunicCube : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Runic Cube",
            "#Whenever you lose HP, draw !C! card.",
            "An ancient cube inscribed with runes that pulse with a faint light.");

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature && delta < 0)
        {
            Flash();
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, Owner);
        }
    }
}
