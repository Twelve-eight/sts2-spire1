using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Blur (Uncommon Skill). Gain 5 Block (8 upgraded). Block is not removed at the start of your next turn.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Blur() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move), new PowerVar<BlurPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        await CommonActions.ApplySelf<BlurPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
