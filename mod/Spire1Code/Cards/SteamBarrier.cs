using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class SteamBarrier() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("SteamReduction", 0),
        ..CustomCardModel.MakeCalculatedBlock(6,
            static (card, target) => -card.DynamicVars["SteamReduction"].BaseValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.CalculatedBlock, play);
        if (DynamicVars["SteamReduction"].BaseValue < DynamicVars.CalculationBase.BaseValue)
            DynamicVars["SteamReduction"].BaseValue += 1;
    }

    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(2m);
}
