using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Flechettes (Uncommon Attack). Deal 4 damage for each Skill in your hand (6 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Flechettes() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Count Skills in hand at play time; this card is an Attack and sits in the Play pile, so it never counts itself.
        int skills = PileType.Hand.GetPile(Owner).Cards.Count(c => c.Type == CardType.Skill);
        await CommonActions.CardAttack(this, play, hitCount: skills).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
