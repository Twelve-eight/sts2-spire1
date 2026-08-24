using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Clash (Common Attack). Can only be played if every card in your hand is an Attack; deal 14 damage (18 upgraded).</summary>
[Pool(typeof(Spire1CardPool))]
public class Clash() : Spire1Card(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14, ValueProp.Move)];

    // IsPlayable is read from CanPlay, which the hand UI re-evaluates on every cost/glow/end-turn refresh, so
    // this goes straight to the hand's backing List. CardPile.GetCards(Owner, PileType.Hand) would allocate a
    // params PileType[] plus a SelectMany enumerator on every one of those reads for the same answer.
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.All(c => c.Type == CardType.Attack);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
