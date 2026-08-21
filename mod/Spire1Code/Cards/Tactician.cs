using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Tactician (Uncommon Skill). Unplayable. If this card is discarded from your hand, gain 1 Energy (2 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class Tactician() : Spire1Card(-2, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    /// <summary>
    /// Discard hook: fired by CardCmd.Discard for every explicit discard (always from the hand), the same hook the
    /// game's own Tingsha/Tough Bandages relics use for "whenever you discard a card". End-of-turn hand flush does
    /// not go through CardCmd.Discard, matching how the game's own discard effects behave in StS2.
    /// </summary>
    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card != this)
            return;
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}
