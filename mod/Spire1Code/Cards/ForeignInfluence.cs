using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Foreign Influence (Uncommon Skill, Exhaust). Choose 1 of 3 Attacks of any color to add into
/// your hand; when upgraded the chosen card costs 0 this turn.
/// "Any color" is every character card pool (ModelDb.AllCharacterCardPools), matching StS1's getAnyColorCard,
/// which draws from the character pools and not from the colorless pool. Generation and the 3-option screen are
/// the shipped AttackPotion idiom (CardFactory.GetDistinctForCombat + CardSelectCmd.FromChooseACardScreen).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class ForeignInfluence() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        IEnumerable<CardModel> anyColorAttacks = ModelDb.AllCharacterCardPools
            .SelectMany(pool => pool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint))
            .Where(c => c.Type == CardType.Attack);

        List<CardModel> options = CardFactory.GetDistinctForCombat(
            Owner,
            anyColorAttacks,
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (options.Count == 0)
        {
            return;
        }

        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);
        if (chosen == null)
        {
            return;
        }

        if (IsUpgraded)
        {
            chosen.SetToFreeThisTurn();
        }

        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
    }
}
