using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Immolate (Rare). Deal 21 damage to ALL enemies; add a Burn into your discard pile (28 upgraded).</summary>
public class Immolate() : Spire1Card(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(21, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CardPileCmd.AddToCombatAndPreview<Burn>(Owner.Creature, PileType.Discard, 1, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(7m);
}
