using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class Aggregate() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("MagicNumber", 4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int divisor = DynamicVars["MagicNumber"].IntValue;
        int energy = PileType.Draw.GetPile(Owner).Cards.Count / divisor;
        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(-1m);
}
