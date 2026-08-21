using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Unload (Rare Attack). Deal 14 damage (18 upgraded); discard all non-Attack cards in your hand.</summary>
[Pool(typeof(SilentCardPool))]
public class Unload() : Spire1Card(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var nonAttacks = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type != CardType.Attack).ToList();
        if (nonAttacks.Count > 0)
        {
            await CardCmd.Discard(choiceContext, nonAttacks);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
