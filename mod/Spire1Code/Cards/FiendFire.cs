using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Fiend Fire (Rare). Exhaust your hand; deal 7 damage per card exhausted (10 upgraded).</summary>
public class FiendFire() : Spire1Card(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var cards = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
        foreach (var c in cards)
            await CardCmd.Exhaust(choiceContext, c);
        await CommonActions.CardAttack(this, play, hitCount: cards.Count).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
