using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Eruption (Basic). Deal 9 damage and enter Wrath; Eruption+ costs 1.
///
/// The stance half is real, not approximated: the mod ships its own stance subsystem
/// (Powers/StancePower.cs + CalmPower/WrathPower/DivinityPower, driven by Extensions/StanceCmd.cs),
/// which is what every other Watcher card in the pool uses (see Blasphemy.cs).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Eruption() : Spire1Card(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await StanceCmd.Enter<WrathPower>(choiceContext, Owner, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // vanilla Eruption+ costs 1 (damage unchanged)
}
