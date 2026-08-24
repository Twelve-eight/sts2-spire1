using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Entrench (Uncommon Skill). Double your current Block (cost 1 upgraded).</summary>
[Pool(typeof(Spire1CardPool))]
public class Entrench() : Spire1Card(2, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, Owner.Creature.Block, ValueProp.Move, null);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
