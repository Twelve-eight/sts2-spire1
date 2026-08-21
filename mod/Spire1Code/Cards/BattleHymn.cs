using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Battle Hymn (Uncommon Power). At the start of each turn, add a Smite into your hand.
/// The upgrade only adds Innate; the Smite count stays 1.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class BattleHymn() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BattleHymnPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<BattleHymnPower>(choiceContext, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
