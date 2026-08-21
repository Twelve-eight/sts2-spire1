using System.Linq;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 - Toolbox (Shop). At the start of each combat, add a random Colorless card to your hand.</summary>
public class Toolbox : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Toolbox",
            "#At the start of each combat, add a random Colorless card to your hand.",
            "Always be prepared.");

    public override async Task BeforeCombatStart()
    {
        var pool = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint);
        var cards = CardFactory.GetDistinctForCombat(Owner, pool, 1, Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (cards.Count > 0)
        {
            Flash();
            await CardPileCmd.AddGeneratedCardToCombat(cards[0], PileType.Hand, Owner);
        }
    }
}
