using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;
using WraithFormPower = Spire1.Spire1Code.Powers.WraithFormPower;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Wraith Form (Rare Power). Gain 2 Intangible (3 upgraded); at the end of your turn, lose 1 Dexterity.</summary>
[Pool(typeof(SilentCardPool))]
public class WraithForm() : Spire1Card(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(2), new PowerVar<WraithFormPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<IntangiblePower>(choiceContext, this);
        await CommonActions.ApplySelf<WraithFormPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Power<IntangiblePower>().UpgradeValueBy(1m);
}
