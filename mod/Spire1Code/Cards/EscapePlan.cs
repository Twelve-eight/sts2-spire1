using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Escape Plan (Uncommon Skill). Draw 1 card. If you draw a Skill, gain 3 Block (5 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class EscapePlan() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new BlockVar(3, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var drawn = (await CommonActions.Draw(this, choiceContext)).ToList();
        if (drawn.Any(c => c.Type == CardType.Skill))
        {
            await CommonActions.CardBlock(this, DynamicVars.Block, play);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
