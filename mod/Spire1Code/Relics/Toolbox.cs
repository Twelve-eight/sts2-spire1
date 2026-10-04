using System.Linq;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

using Spire1.Spire1Code.Config;

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
        // Cards content group gate (C18 r2, 2026-10-03): this Spire1 relic grants a card directly through AddGeneratedCardToCombat; the relic stays obtained and cleanup semantics are unchanged.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Toolbox card grant skipped: cards content group is off");
            return;
        }

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
