using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless - Apotheosis (Rare Skill). 2 cost. Upgrade ALL your cards for the rest of
/// combat. Exhaust. Upgrade: cost 1 (upgradeBaseCost(1), javap-colorless/Apotheosis.txt
/// L36-46; no keyword is added on upgrade).
/// R6 (2026-09-06): own class required - the shipped StS2 Apotheosis drifts on TWO fields:
/// it adds CardKeyword.Innate (dllsrc Apotheosis.cs L11-15) and lives in EventCardPool with
/// CardRarity.Ancient (L17-18), while StS1 is a plain RARE colorless with no Innate.
/// Upgrade scope matches the jar's ApotheosisAction: hand + draw + discard + exhaust piles,
/// each card only if it canUpgrade(). StS1 ApotheosisAction additionally skips nothing else.
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public class Apotheosis() : Spire1Card(2, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var card in Owner.PlayerCombatState.AllCards)
        {
            if (card != this && card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
