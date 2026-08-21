using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — DEPRECATED Discipline (Rare Power, cost 2 / 1 upgraded). If you end your turn with unused Energy,
/// draw that many additional cards next turn.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Discipline() : Spire1Card(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<DisciplinePower>(choiceContext, this, 1m);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
