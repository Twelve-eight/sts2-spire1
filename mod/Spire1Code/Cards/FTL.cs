using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(Spire1LegacyPool))]
public class FTL() : Spire1Card(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new IntVar("Threshold", 3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // The actual wrapper owns the effect until its real use boundary (including native
        // replay and cross-owner branching). The front must return before the wrapper's U.
        var effect = Spire1FtlPlayScope.Begin(this, play, choiceContext);
        await effect.Front(DynamicVars["Threshold"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars["Threshold"].UpgradeValueBy(1);
    }
}
