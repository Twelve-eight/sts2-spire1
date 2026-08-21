using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(WatcherCardPool))]
public class Halt() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3, ValueProp.Move), new IntVar("MagicNumber", 9)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        if (StanceCmd.IsIn<WrathPower>(Owner))
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                DynamicVars["MagicNumber"].BaseValue,
                ValueProp.Move,
                play);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1);
        DynamicVars["MagicNumber"].UpgradeValueBy(5);
    }
}
