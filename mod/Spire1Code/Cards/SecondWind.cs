using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Second Wind (Uncommon). Exhaust all non-Attack cards in hand; gain 5 Block each (7 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class SecondWind() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    private int _blockEach = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        foreach (var c in PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type != CardType.Attack).ToList())
        {
            await CardCmd.Exhaust(choiceContext, c);
            await CreatureCmd.GainBlock(Owner.Creature, _blockEach, ValueProp.Move, null);
        }
    }

    protected override void OnUpgrade() => _blockEach = 7;
}
