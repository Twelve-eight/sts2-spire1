using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Establishment (Rare Power). Whenever a card is Retained, reduce its cost by 1 this combat.
/// Upgrade adds Innate.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Establishment() : Spire1Card(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<EstablishmentPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<EstablishmentPower>(choiceContext, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
