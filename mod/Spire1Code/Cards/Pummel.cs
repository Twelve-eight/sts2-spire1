using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Pummel (Uncommon Attack). Deal 2 damage 4 times, Exhaust (5 times upgraded).</summary>
public class Pummel() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // Hit count is a RepeatVar so the description token !Repeat! shows the upgraded value.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2, ValueProp.Move), new RepeatVar(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play, hitCount: DynamicVars.Repeat.IntValue).Execute(choiceContext);

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1); // 4 -> 5
}
