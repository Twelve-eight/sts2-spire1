using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using System.Linq;

using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Distraction (Uncommon Skill). Add a random Skill into your hand; it costs 0 this turn. Exhaust (0 cost upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class Distraction() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Cards content group gate (C18 r2, 2026-10-03): this Spire1 card adds the chosen Skill directly through AddGeneratedCardToCombat, so the candidate pool gate cannot close the grant.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Distraction card grant skipped: cards content group is off");
            return;
        }

        // Pick a random Skill from the owner's card pool (game Distraction idiom).
        var skill = CardFactory.GetDistinctForCombat(Owner,
                Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                    .Where(c => c.Type == CardType.Skill),
                1, Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (skill == null)
        {
            return;
        }
        skill.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(skill, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
