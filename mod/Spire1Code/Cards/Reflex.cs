using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Reflex (Uncommon Skill). Unplayable. If this card is discarded from your hand, draw 2 cards (3 upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class Reflex() : Spire1Card(-2, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

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
        await CommonActions.Draw(this, choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
