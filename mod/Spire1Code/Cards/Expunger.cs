using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher token - Expunger. Deal 9 damage (15 upgraded) a number of times set by its creator.</summary>
public class Expunger() : Spire1Card(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move), new RepeatVar(1)];

    /// <summary>Sets the number of times this generated card repeats its attack.</summary>
    public void SetRepeats(decimal repeats)
    {
        AssertMutable();
        DynamicVars.Repeat.BaseValue = repeats;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play, hitCount: DynamicVars.Repeat.IntValue).Execute(choiceContext);

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
