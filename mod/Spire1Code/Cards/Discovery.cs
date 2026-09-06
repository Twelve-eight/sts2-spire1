using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — Discovery (Uncommon Skill). 1 cost, Exhaust. Choose 1 of 3 random cards
/// to add into your hand; it costs 0 this turn. Upgrade: no longer Exhausts (jar
/// upgrade() sets exhaust=false, javap-colorless/Discovery.txt L36-52).
/// R6 (2026-09-06): own class required — the shipped StS2 Discovery generates its 3 options
/// from the CURRENT CHARACTER's card pool only (dllsrc Discovery.cs L27,
/// base.Owner.Character.CardPool.GetUnlockedCards(...)), while the jar's DiscoveryAction uses
/// returnTrulyRandomCardInCombat() which draws from srcCommon/srcUncommon/srcRareCardPool —
/// the player-character pools of ALL colors (AbstractDungeon.javap.txt L2707-2792), excluding
/// only HEALING-tagged cards. Here the "any color" scope follows ForeignInfluence's idiom
/// (ModelDb.AllCharacterCardPools). The engine's HEALING exclusion has no StS2 CardTag
/// equivalent (CardTag enum: Strike/Defend/Minion/OstyAttack/Shiv) and no shipped consumer,
/// so — like Bite.cs's HEALING tag — the filter is dropped as unrepresentable metadata.
/// The chosen card is made free this turn (setCostForTurn(0) both sides) and MasterReality
/// upgrading generated cards is engine hook behavior (CardSelectCmd path), matching StS1's
/// MasterRealityPower branch inside DiscoveryAction.update().
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public class Discovery() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        IEnumerable<CardModel> anyColorCards = ModelDb.AllCharacterCardPools
            .SelectMany(pool => pool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint));

        List<CardModel> options = CardFactory.GetDistinctForCombat(
            Owner,
            anyColorCards,
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (options.Count == 0)
        {
            return;
        }

        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner, canSkip: true);
        if (chosen == null)
        {
            return;
        }

        chosen.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => ResetKeywordCache();
}
