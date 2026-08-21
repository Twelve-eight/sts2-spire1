using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Havoc (Common). Play the top card of your draw pile and Exhaust it (1 cost upgraded).</summary>
public class Havoc() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var top = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault();
        if (top != null)
        {
            await CardCmd.AutoPlay(choiceContext, top, null);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
