using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Infinite Blades (Uncommon Power). At the start of your turn, add a Shiv into your hand (Innate upgraded).</summary>
[Pool(typeof(SilentCardPool))]
public class InfiniteBlades() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<InfiniteBladesPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<InfiniteBladesPower>(choiceContext, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
