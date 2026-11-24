using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Bullet Time (Rare Skill). You cannot draw additional cards this turn; all cards in your hand cost 0 this turn (2 cost upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class BulletTime() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // StS1 keeps the energy reduction for repeated plays in this turn; X costs stay X.
        foreach (var card in PileType.Hand.GetPile(Owner).Cards)
        {
            if (!card.EnergyCost.CostsX)
            {
                card.EnergyCost.SetThisTurn(0);
                card.SetStarCostThisTurn(0);
            }
        }
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        await CommonActions.ApplySelf<NoDrawPower>(choiceContext, this, 1m);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
