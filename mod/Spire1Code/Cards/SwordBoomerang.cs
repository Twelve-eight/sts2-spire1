using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Sword Boomerang (Common). Deal 3 damage to a random enemy 3 times (4 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class SwordBoomerang() : Spire1Card(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
{
    // Hit count is a RepeatVar (not a private field) so the description token !Repeat! shows the upgraded value.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move), new RepeatVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play, hitCount: DynamicVars.Repeat.IntValue).Execute(choiceContext);

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1); // 3 -> 4
}
