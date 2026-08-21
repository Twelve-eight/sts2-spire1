using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Bullet Time (Rare Skill). You cannot draw additional cards this turn; all cards in your hand cost 0 this turn (2 cost upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class BulletTime() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Same approach as the game's own BulletTime card: X-cost cards keep their X cost.
        foreach (var card in PileType.Hand.GetPile(Owner).Cards)
        {
            if (!card.EnergyCost.CostsX)
            {
                card.SetToFreeThisTurn();
            }
        }
        await CommonActions.ApplySelf<NoDrawPower>(choiceContext, this, 1m);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
