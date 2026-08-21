using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Brutality (Uncommon Power). At the start of your turn, lose 1 HP and draw 1 card (Innate upgraded).</summary>
public class Brutality() : Spire1Card(0, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BrutalityPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<BrutalityPower>(choiceContext, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
