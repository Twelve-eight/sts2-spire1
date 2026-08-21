using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(WatcherCardPool))]
public class Prostrate() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("MagicNumber", 2), new BlockVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await StanceCmd.GainMantra(
            choiceContext,
            Owner,
            DynamicVars["MagicNumber"].BaseValue,
            this);
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
    }

    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(1);
}
