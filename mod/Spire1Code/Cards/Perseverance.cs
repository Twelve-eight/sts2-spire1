using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Perseverance (Uncommon Skill, Retain). Gain 5 Block (7 upgraded); each time this card is
/// Retained, its Block grows by 2 (3 upgraded) for the rest of the combat.
/// Retention is observed with the card's AfterFlush hook: CombatManager.FlushPlayerHand decides retention through
/// CardModel.ShouldRetainThisTurn and then hands the retained-card list to every combat hook listener, cards
/// included. The growth is stored in a DynamicVar so the calculated-block lambda stays static (no instance-field
/// capture), which keeps MutableClone during reward/preview generation safe — same shape as Rampage.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Perseverance() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("MagicNumber", 2),
        new IntVar("PerseveranceBonus", 0),
        ..CustomCardModel.MakeCalculatedBlock(5,
            static (card, target) => card.DynamicVars["PerseveranceBonus"].BaseValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardBlock(this, DynamicVars.CalculatedBlock, play);

    public override Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (retainedCards.Contains(this))
        {
            DynamicVars["PerseveranceBonus"].BaseValue += DynamicVars["MagicNumber"].BaseValue;
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(2m);
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}
