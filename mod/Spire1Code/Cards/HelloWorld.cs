using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(Spire1LegacyPool))]
public class HelloWorld() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<HelloWorldPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<HelloWorldPower>(choiceContext, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
